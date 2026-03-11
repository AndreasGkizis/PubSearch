using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResearchPublications.Infrastructure.Constants;

namespace ResearchPublications.Infrastructure.Persistence;

/// <summary>
/// Runs on application start-up to ensure the database, schema, stored procedures
/// and initial seed data are all in place. All statements are idempotent.
/// </summary>
public class DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
{
    // private const string DatabaseName = "ResearchPublications";

    // ── Public entry point ────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        var connectionString = configuration.GetConnectionString(ConfigKeys.DefaultConnection)
            ?? throw new InvalidOperationException($"Connection string '{ConfigKeys.DefaultConnection}' is not configured.");

        await EnsureDatabaseAsync(connectionString);

        var dbConnectionString = SwapDatabase(connectionString, DatabaseName);
        await EnsureSchemaAsync(dbConnectionString);
        await EnsureStoredProceduresAsync(dbConnectionString);
        await SeedIfEmptyAsync(dbConnectionString);

        logger.LogInformation("Database initialisation complete.");
    }

    // ── Steps ─────────────────────────────────────────────────────────────

    private async Task EnsureDatabaseAsync(string connectionString)
    {
        var masterCs = SwapDatabase(connectionString, "master");
        await using var conn = new SqlConnection(masterCs);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{DatabaseName}')
                CREATE DATABASE [{DatabaseName}];
            """;
        await cmd.ExecuteNonQueryAsync();
        logger.LogInformation("Database '{Database}' verified.", DatabaseName);
    }

    private async Task EnsureSchemaAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // AuthorTableType (TVP used by create/update SPs)
        await ExecuteBatchAsync(conn, """
            IF TYPE_ID('dbo.AuthorTableType') IS NULL
            BEGIN
                CREATE TYPE dbo.AuthorTableType AS TABLE (
                    FullName NVARCHAR(200) NOT NULL,
                    Email    NVARCHAR(200) NULL
                );
            END
            """);

        // Authors table
        await ExecuteBatchAsync(conn, """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Authors')
            CREATE TABLE Authors (
                Id       INT           PRIMARY KEY IDENTITY,
                FullName NVARCHAR(200) NOT NULL,
                Email    NVARCHAR(200) NULL
            );
            """);

        // Publications table
        await ExecuteBatchAsync(conn, """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Publications')
            CREATE TABLE Publications (
                Id            INT            PRIMARY KEY IDENTITY,
                Title         NVARCHAR(500)  NOT NULL,
                Abstract      NVARCHAR(MAX)  NULL,
                Body          NVARCHAR(MAX)  NULL,
                Keywords      NVARCHAR(1000) NULL,
                Year          INT            NULL,
                DOI           NVARCHAR(200)  NULL,
                CitationCount INT            NOT NULL DEFAULT 0,
                PdfFileName   NVARCHAR(500)  NULL,
                CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                LastModified  DATETIME2      NOT NULL DEFAULT GETUTCDATE()
            );
            """);

        // PublicationAuthors join table
        await ExecuteBatchAsync(conn, """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PublicationAuthors')
            CREATE TABLE PublicationAuthors (
                PublicationId INT NOT NULL REFERENCES Publications(Id) ON DELETE CASCADE,
                AuthorId      INT NOT NULL REFERENCES Authors(Id)      ON DELETE CASCADE,
                PRIMARY KEY (PublicationId, AuthorId)
            );
            """);

        // Full-text catalog
        await ExecuteBatchAsync(conn, """
            IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'PublicationsCatalog')
                CREATE FULLTEXT CATALOG PublicationsCatalog AS DEFAULT;
            """);

        // Full-text index (dynamic — resolves the real PK name at runtime)
        await ExecuteBatchAsync(conn, """
            IF NOT EXISTS (
                SELECT 1 FROM sys.fulltext_indexes fi
                JOIN sys.objects o ON fi.object_id = o.object_id
                WHERE o.name = 'Publications')
            BEGIN
                DECLARE @pkName NVARCHAR(200) =
                    (SELECT name FROM sys.indexes
                     WHERE object_id = OBJECT_ID('Publications') AND is_primary_key = 1);

                EXEC('CREATE FULLTEXT INDEX ON Publications(Title, Abstract, Keywords, Body)
                      KEY INDEX [' + @pkName + '] ON PublicationsCatalog');
            END
            """);

        logger.LogInformation("Schema verified.");
    }

    private async Task EnsureStoredProceduresAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // Each SP uses CREATE OR ALTER PROCEDURE so it is inherently idempotent.
        await ExecuteBatchAsync(conn, SpGetPublicationById);
        await ExecuteBatchAsync(conn, SpGetAllPublications);
        await ExecuteBatchAsync(conn, SpCreatePublication);
        await ExecuteBatchAsync(conn, SpUpdatePublication);
        await ExecuteBatchAsync(conn, SpDeletePublication);
        await ExecuteBatchAsync(conn, SpSearchPublications);

        logger.LogInformation("Stored procedures verified.");
    }

    private async Task SeedIfEmptyAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Publications;";
        var count = (int)(await checkCmd.ExecuteScalarAsync())!;

        if (count > 0)
        {
            logger.LogInformation("Seed skipped — {Count} publication(s) already exist.", count);
            return;
        }

        await ExecuteBatchAsync(conn, SeedAuthors);
        await ExecuteBatchAsync(conn, SeedPublications);
        await ExecuteBatchAsync(conn, SeedPublicationAuthors);

        logger.LogInformation("Seed data inserted.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task ExecuteBatchAsync(SqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Replaces the Initial Catalog in the connection string.</summary>
    private static string SwapDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }

    // ── Stored procedure bodies ───────────────────────────────────────────

    private const string SpGetPublicationById = """
        CREATE OR ALTER PROCEDURE sp_GetPublicationById
            @Id INT
        AS
        BEGIN
            SET NOCOUNT ON;
            SELECT Id, Title, Abstract, Body, Keywords, Year, DOI,
                   CitationCount, PdfFileName, CreatedAt, LastModified
            FROM Publications WHERE Id = @Id;

            SELECT a.Id, a.FullName, a.Email
            FROM Authors a
            JOIN PublicationAuthors pa ON pa.AuthorId = a.Id
            WHERE pa.PublicationId = @Id;
        END
        """;

    private const string SpGetAllPublications = """
        CREATE OR ALTER PROCEDURE sp_GetAllPublications
            @Page     INT = 1,
            @PageSize INT = 20
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Offset INT = (@Page - 1) * @PageSize;

            SELECT COUNT(*) AS TotalCount FROM Publications;

            SELECT
                p.Id, p.Title, p.Abstract, p.Body, p.Keywords, p.Year, p.DOI,
                p.CitationCount, p.PdfFileName, p.CreatedAt, p.LastModified,
                STUFF((
                    SELECT ', ' + a.FullName
                    FROM PublicationAuthors pa2
                    JOIN Authors a ON pa2.AuthorId = a.Id
                    WHERE pa2.PublicationId = p.Id
                    FOR XML PATH(''), TYPE
                ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AuthorNames
            FROM Publications p
            ORDER BY p.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        END
        """;

    private const string SpCreatePublication = """
        CREATE OR ALTER PROCEDURE sp_CreatePublication
            @Title         NVARCHAR(500),
            @Abstract      NVARCHAR(MAX)  = NULL,
            @Body          NVARCHAR(MAX)  = NULL,
            @Keywords      NVARCHAR(1000) = NULL,
            @Year          INT            = NULL,
            @DOI           NVARCHAR(200)  = NULL,
            @CitationCount INT            = 0,
            @PdfFileName   NVARCHAR(500)  = NULL,
            @Authors       dbo.AuthorTableType READONLY
        AS
        BEGIN
            SET NOCOUNT ON;
            BEGIN TRANSACTION;
            BEGIN TRY
                INSERT INTO Publications (Title, Abstract, Body, Keywords, Year, DOI, CitationCount, PdfFileName)
                VALUES (@Title, @Abstract, @Body, @Keywords, @Year, @DOI, @CitationCount, @PdfFileName);

                DECLARE @NewId INT = SCOPE_IDENTITY();
                DECLARE @FullName NVARCHAR(200), @Email NVARCHAR(200), @AuthorId INT;

                DECLARE authorCursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT FullName, Email FROM @Authors;
                OPEN authorCursor;
                FETCH NEXT FROM authorCursor INTO @FullName, @Email;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SELECT @AuthorId = Id FROM Authors WHERE FullName = @FullName;
                    IF @AuthorId IS NULL
                    BEGIN
                        INSERT INTO Authors (FullName, Email) VALUES (@FullName, @Email);
                        SET @AuthorId = SCOPE_IDENTITY();
                    END
                    IF NOT EXISTS (SELECT 1 FROM PublicationAuthors WHERE PublicationId = @NewId AND AuthorId = @AuthorId)
                        INSERT INTO PublicationAuthors (PublicationId, AuthorId) VALUES (@NewId, @AuthorId);
                    SET @AuthorId = NULL;
                    FETCH NEXT FROM authorCursor INTO @FullName, @Email;
                END
                CLOSE authorCursor;
                DEALLOCATE authorCursor;
                COMMIT TRANSACTION;
                SELECT @NewId AS Id;
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END
        """;

    private const string SpUpdatePublication = """
        CREATE OR ALTER PROCEDURE sp_UpdatePublication
            @Id            INT,
            @Title         NVARCHAR(500),
            @Abstract      NVARCHAR(MAX)  = NULL,
            @Body          NVARCHAR(MAX)  = NULL,
            @Keywords      NVARCHAR(1000) = NULL,
            @Year          INT            = NULL,
            @DOI           NVARCHAR(200)  = NULL,
            @CitationCount INT            = 0,
            @PdfFileName   NVARCHAR(500)  = NULL,
            @Authors       dbo.AuthorTableType READONLY
        AS
        BEGIN
            SET NOCOUNT ON;
            BEGIN TRANSACTION;
            BEGIN TRY
                UPDATE Publications
                SET Title = @Title, Abstract = @Abstract, Body = @Body,
                    Keywords = @Keywords, Year = @Year, DOI = @DOI,
                    CitationCount = @CitationCount, PdfFileName = @PdfFileName,
                    LastModified = GETUTCDATE()
                WHERE Id = @Id;

                DELETE FROM PublicationAuthors WHERE PublicationId = @Id;

                DECLARE @FullName NVARCHAR(200), @Email NVARCHAR(200), @AuthorId INT;
                DECLARE authorCursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT FullName, Email FROM @Authors;
                OPEN authorCursor;
                FETCH NEXT FROM authorCursor INTO @FullName, @Email;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SELECT @AuthorId = Id FROM Authors WHERE FullName = @FullName;
                    IF @AuthorId IS NULL
                    BEGIN
                        INSERT INTO Authors (FullName, Email) VALUES (@FullName, @Email);
                        SET @AuthorId = SCOPE_IDENTITY();
                    END
                    IF NOT EXISTS (SELECT 1 FROM PublicationAuthors WHERE PublicationId = @Id AND AuthorId = @AuthorId)
                        INSERT INTO PublicationAuthors (PublicationId, AuthorId) VALUES (@Id, @AuthorId);
                    SET @AuthorId = NULL;
                    FETCH NEXT FROM authorCursor INTO @FullName, @Email;
                END
                CLOSE authorCursor;
                DEALLOCATE authorCursor;
                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END
        """;

    private const string SpDeletePublication = """
        CREATE OR ALTER PROCEDURE sp_DeletePublication
            @Id INT
        AS
        BEGIN
            SET NOCOUNT ON;
            DELETE FROM Publications WHERE Id = @Id;
        END
        """;

    private const string SpSearchPublications = """
        CREATE OR ALTER PROCEDURE sp_SearchPublications
            @Query   NVARCHAR(500),
            @Year    INT           = NULL,
            @Author  NVARCHAR(200) = NULL,
            @Keyword NVARCHAR(200) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;

            DECLARE @FtsQuery NVARCHAR(600) = '"' + REPLACE(LTRIM(RTRIM(@Query)), ' ', '" OR "') + '"';

            IF (SELECT COUNT(*) FROM CONTAINSTABLE(Publications, (Title, Abstract, Keywords, Body), @FtsQuery)) > 0
            BEGIN
                SELECT
                    p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName,
                    ct.[RANK] AS Rank,
                    STUFF((
                        SELECT ', ' + a.FullName
                        FROM PublicationAuthors pa2
                        JOIN Authors a ON pa2.AuthorId = a.Id
                        WHERE pa2.PublicationId = p.Id
                        FOR XML PATH(''), TYPE
                    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AuthorNames
                FROM Publications p
                INNER JOIN CONTAINSTABLE(Publications, (Title, Abstract, Keywords, Body), @FtsQuery) ct ON p.Id = ct.[KEY]
                LEFT JOIN PublicationAuthors pa ON pa.PublicationId = p.Id
                LEFT JOIN Authors a             ON a.Id = pa.AuthorId
                WHERE (@Year    IS NULL OR p.Year = @Year)
                  AND (@Keyword IS NULL OR p.Keywords LIKE '%' + @Keyword + '%')
                  AND (@Author  IS NULL OR EXISTS (
                          SELECT 1 FROM PublicationAuthors pa3
                          JOIN Authors a3 ON pa3.AuthorId = a3.Id
                          WHERE pa3.PublicationId = p.Id AND a3.FullName LIKE '%' + @Author + '%'))
                GROUP BY p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName, ct.[RANK]
                ORDER BY ct.[RANK] DESC;
                RETURN;
            END

            SELECT
                p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName,
                0.0 AS Rank,
                STUFF((
                    SELECT ', ' + a.FullName
                    FROM PublicationAuthors pa2
                    JOIN Authors a ON pa2.AuthorId = a.Id
                    WHERE pa2.PublicationId = p.Id
                    FOR XML PATH(''), TYPE
                ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AuthorNames
            FROM Publications p
            LEFT JOIN PublicationAuthors pa ON pa.PublicationId = p.Id
            LEFT JOIN Authors a             ON a.Id = pa.AuthorId
            WHERE (  p.Title    LIKE '%' + @Query + '%'
                  OR p.Abstract LIKE '%' + @Query + '%'
                  OR p.Keywords LIKE '%' + @Query + '%'
                  OR p.Body     LIKE '%' + @Query + '%')
              AND (@Year    IS NULL OR p.Year = @Year)
              AND (@Keyword IS NULL OR p.Keywords LIKE '%' + @Keyword + '%')
              AND (@Author  IS NULL OR EXISTS (
                      SELECT 1 FROM PublicationAuthors pa3
                      JOIN Authors a3 ON pa3.AuthorId = a3.Id
                      WHERE pa3.PublicationId = p.Id AND a3.FullName LIKE '%' + @Author + '%'))
            GROUP BY p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName
            ORDER BY p.Id DESC;
        END
        """;

    // ── Seed data ─────────────────────────────────────────────────────────

    private const string SeedAuthors = """
        INSERT INTO Authors (FullName, Email) VALUES
            ('Yann LeCun',      'lecun@fb.com'),
            ('Geoffrey Hinton', 'hinton@google.com'),
            ('Yoshua Bengio',   'bengio@mila.ca'),
            ('Andrej Karpathy', 'karpathy@openai.com'),
            ('Fei-Fei Li',      'feifeili@stanford.edu'),
            ('Ian Goodfellow',  'goodfellow@apple.com'),
            ('Demis Hassabis',  'demis@deepmind.com'),
            ('Ilya Sutskever',  'ilyasu@openai.com'),
            ('Pieter Abbeel',   'abbeel@berkeley.edu'),
            ('Chelsea Finn',    'cfinn@stanford.edu');
        """;

    private const string SeedPublications = """
        INSERT INTO Publications (Title, Abstract, Body, Keywords, Year, DOI, CitationCount, PdfFileName) VALUES
        ('Deep Residual Learning for Image Recognition',
         'We present a residual learning framework to ease the training of networks that are substantially deeper than those used previously.',
         'Deep neural networks are more difficult to train. We present a residual learning framework to ease the training of networks that are substantially deeper. We explicitly reformulate the layers as learning residual functions with reference to the layer inputs. These residual networks are easier to optimize and can gain accuracy from considerably increased depth.',
         'deep learning, residual networks, image recognition, CNN', 2016, '10.1109/CVPR.2016.90', 120000, 'paper-01.pdf'),

        ('Attention Is All You Need',
         'The dominant sequence transduction models are based on complex recurrent or convolutional neural networks. We propose a new simple network architecture, the Transformer, based solely on attention mechanisms.',
         'We propose the Transformer, a model architecture eschewing recurrence and instead relying entirely on an attention mechanism to draw global dependencies between input and output. The Transformer allows for significantly more parallelization and reaches a new state of the art in translation quality.',
         'transformers, attention mechanism, NLP, sequence models', 2017, '10.48550/arXiv.1706.03762', 95000, 'paper-02.pdf'),

        ('Generative Adversarial Nets',
         'We propose a new framework for estimating generative models via an adversarial process, in which we simultaneously train two models: a generative model G and a discriminative model D.',
         'The generative model can be thought of as analogous to a team of counterfeiters, trying to produce fake currency and use it without detection, while the discriminative model is analogous to the police, trying to detect the counterfeit currency. Competition in this game drives both teams to improve their methods until the counterfeits are indistinguishable from the genuine articles.',
         'GANs, generative models, adversarial training, deep learning', 2014, '10.48550/arXiv.1406.2661', 85000, 'paper-03.pdf'),

        ('BERT: Pre-training of Deep Bidirectional Transformers for Language Understanding',
         'We introduce BERT, a new language representation model which stands for Bidirectional Encoder Representations from Transformers.',
         'Unlike recent language representation models, BERT is designed to pre-train deep bidirectional representations from unlabeled text by jointly conditioning on both left and right context in all layers. The pre-trained BERT model can be fine-tuned with just one additional output layer to create state-of-the-art models for a wide range of NLP tasks.',
         'BERT, NLP, transformers, pre-training, language model', 2019, '10.48550/arXiv.1810.04805', 75000, 'paper-04.pdf'),

        ('ImageNet Large Scale Visual Recognition Challenge',
         'The ImageNet Large Scale Visual Recognition Challenge is a benchmark in object category classification and detection on hundreds of object categories and millions of images.',
         'This paper describes the creation of the ImageNet dataset and the ILSVRC benchmark. We report on the results of the ILSVRC 2010 through 2014 competitions, where hundreds of teams competed on tasks of object classification and detection using hundreds of gigabytes of images.',
         'ImageNet, object recognition, benchmark, computer vision', 2015, '10.1007/s11263-015-0816-y', 65000, 'paper-05.pdf'),

        ('Playing Atari with Deep Reinforcement Learning',
         'We present the first deep learning model to successfully learn control policies directly from high-dimensional sensory input using reinforcement learning.',
         'The model is a convolutional neural network, trained with a variant of Q-learning, whose input is raw pixels and whose output is a value function estimating future rewards. We tested this approach on seven Atari 2600 games and exceeded expert human performance on three of them.',
         'reinforcement learning, deep Q-network, Atari, game playing', 2013, '10.48550/arXiv.1312.5602', 55000, NULL),

        ('Model-Agnostic Meta-Learning for Fast Adaptation of Deep Networks',
         'We propose an algorithm for meta-learning that is model-agnostic, in the sense that it is compatible with any model trained with gradient descent.',
         'The goal of meta-learning is to train a model on a variety of learning tasks, such that it can solve new learning tasks using only a small number of training samples. Our approach trains the model parameters such that a small number of gradient steps with a small amount of training data from a new task will produce good generalization performance on that task.',
         'meta-learning, MAML, few-shot learning, gradient descent', 2017, '10.48550/arXiv.1703.03400', 45000, NULL),

        ('Mastering the Game of Go with Deep Neural Networks and Tree Search',
         'The game of Go has long been viewed as the most challenging of classic games for artificial intelligence owing to its enormous search space and the difficulty of evaluating board positions.',
         'We introduce a new approach to computer Go that uses value networks to evaluate board positions and policy networks to select moves. These deep neural networks are trained by a combination of supervised learning from human expert games, and reinforcement learning from games of self-play. Without any lookahead search, the neural networks play Go at the level of state-of-the-art Monte Carlo tree search programs.',
         'AlphaGo, reinforcement learning, game playing, neural networks', 2016, '10.1038/nature16961', 40000, NULL),

        ('An Introduction to Variational Autoencoders',
         'Variational autoencoders provide a principled framework for learning deep latent-variable models and corresponding inference models using stochastic gradient descent.',
         'The variational autoencoder (VAE) is a type of generative model that provides a probabilistic manner for describing an observation in latent space. The encoder maps input to a distribution over latent space, and the decoder maps from latent space back to the data space.',
         'VAE, variational inference, generative models, latent space', 2019, '10.48550/arXiv.1906.02691', 30000, NULL),

        ('Dropout: A Simple Way to Prevent Neural Networks from Overfitting',
         'We describe a technique called dropout that addresses the problem of overfitting in neural networks. The key idea is to randomly drop units during training.',
         'Dropout refers to dropping out units in a neural network. By dropping a unit out, we mean temporarily removing it from the network along with all its incoming and outgoing connections. During training, dropout samples from an exponential number of different thinned networks. At test time, it is easy to approximate the effect of averaging the predictions of all these thinned networks by simply using a single unthinned network.',
         'dropout, regularization, overfitting, deep learning', 2014, '10.5555/2627435.2670313', 50000, NULL);
        """;

    private const string SeedPublicationAuthors = """
        -- Link publications to authors by position (1-based INSERT order)
        DECLARE @pub1  INT = (SELECT MIN(Id) FROM Publications);
        DECLARE @pub2  INT = @pub1 + 1;
        DECLARE @pub3  INT = @pub1 + 2;
        DECLARE @pub4  INT = @pub1 + 3;
        DECLARE @pub5  INT = @pub1 + 4;
        DECLARE @pub6  INT = @pub1 + 5;
        DECLARE @pub7  INT = @pub1 + 6;
        DECLARE @pub8  INT = @pub1 + 7;
        DECLARE @pub9  INT = @pub1 + 8;
        DECLARE @pub10 INT = @pub1 + 9;

        DECLARE @a1  INT = (SELECT MIN(Id) FROM Authors);
        DECLARE @a2  INT = @a1 + 1;
        DECLARE @a3  INT = @a1 + 2;
        DECLARE @a4  INT = @a1 + 3;
        DECLARE @a5  INT = @a1 + 4;
        DECLARE @a6  INT = @a1 + 5;
        DECLARE @a7  INT = @a1 + 6;
        DECLARE @a8  INT = @a1 + 7;
        DECLARE @a9  INT = @a1 + 8;
        DECLARE @a10 INT = @a1 + 9;

        INSERT INTO PublicationAuthors VALUES
            (@pub1, @a1), (@pub1, @a2),   -- Deep Residual — LeCun & Hinton
            (@pub2, @a8), (@pub2, @a3),   -- Attention     — Sutskever & Bengio
            (@pub3, @a6),                  -- GANs          — Goodfellow
            (@pub4, @a3), (@pub4, @a2),   -- BERT          — Bengio & Hinton
            (@pub5, @a5),                  -- ImageNet      — Fei-Fei Li
            (@pub6, @a7), (@pub6, @a9),   -- Atari DQN     — Hassabis & Abbeel
            (@pub7, @a10),                 -- MAML          — Finn
            (@pub8, @a7),                  -- AlphaGo       — Hassabis
            (@pub9, @a3),                  -- VAE           — Bengio
            (@pub10, @a2), (@pub10, @a4); -- Dropout       — Hinton & Karpathy
        """;
}
