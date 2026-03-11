# Research Publications Search Engine

A .NET 10 DDD web application for managing and searching academic publications, backed by MSSQL and served with a lightweight Alpine.js + Tailwind frontend.

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0+ |
| Docker Desktop | Latest |
| SQL client (optional) | e.g. `sqlcmd`, Azure Data Studio, DBeaver |

---

## Quick Start

### 1. Start the MSSQL container

```bash
docker compose up -d
```

Wait ~15 seconds for SQL Server to become healthy before running scripts.

---

### 2. Run Schema Scripts

Connect to `localhost,1433` with username `sa` / password `YourStrong!Passw0rd` and execute in order:

```bash
# Using sqlcmd (adjust path as needed)
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -i Infrastructure/Persistence/Scripts/Schema/001_CreateTables.sql
```

Then create the full-text index. First find the primary key constraint name:

```sql
USE ResearchPublications;
SELECT name FROM sys.indexes
WHERE object_id = OBJECT_ID('Publications') AND is_primary_key = 1;
```

Update the `KEY INDEX` value in `002_FullTextIndex.sql` if needed, then run:

```bash
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -i Infrastructure/Persistence/Scripts/Schema/002_FullTextIndex.sql
```

> **Tip:** The dynamic block at the bottom of `002_FullTextIndex.sql` can create the index without needing the exact name — just uncomment it.

---

### 3. Run Stored Procedures

Execute each file in `Infrastructure/Persistence/Scripts/StoredProcedures/`:

```bash
for f in Infrastructure/Persistence/Scripts/StoredProcedures/*.sql; do
  sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -i "$f"
done
```

Or on Windows PowerShell:

```powershell
Get-ChildItem Infrastructure\Persistence\Scripts\StoredProcedures\*.sql | ForEach-Object {
    sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -i $_.FullName
}
```

> **Note:** `sp_CreatePublication` and `sp_UpdatePublication` require `dbo.AuthorTableType` to exist first — this is created by `sp_CreatePublication.sql` automatically.

---

### 4. Seed the Database

```bash
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -i Infrastructure/Persistence/Scripts/Seed/SeedData.sql
```

This inserts 10 AI/ML publications (5 with `PdfFileName` set, 5 with `NULL`) and links them to authors.

---

### 5. Configure PDF Storage (optional)

To serve real PDFs, place them in `C:\Publications\PDFs\` (or change the path in `API/appsettings.Development.json`):

```json
{
  "PdfStorage": {
    "Path": "C:\\Publications\\PDFs"
  }
}
```

File names must match the `PdfFileName` column values in the database (e.g. `paper-01.pdf`).

---

### 6. Start the API

```bash
dotnet run --project API
```

The API starts on `http://localhost:5000` (or `https://localhost:5001`).

---

### 7. Open the Frontend

| Page | URL |
|------|-----|
| Search & Browse | http://localhost:5000 |
| Publication Detail | http://localhost:5000/publication.html?id=1 |
| Admin (CRUD) | http://localhost:5000/admin.html |

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/search?q=&year=&author=&keyword=` | Full-text search with filters |
| `GET` | `/api/publications?page=&pageSize=` | Paginated list |
| `GET` | `/api/publications/{id}` | Full detail |
| `POST` | `/api/publications` | Create publication |
| `PUT` | `/api/publications/{id}` | Update publication |
| `DELETE` | `/api/publications/{id}` | Delete publication |
| `GET` | `/api/publications/{id}/download` | Stream PDF file |

---

## Architecture

```
ResearchPublications/
├── Domain/               ← Entities, value objects, interfaces (no dependencies)
├── Application/          ← DTOs, PublicationService, ISearchService interface
├── Infrastructure/       ← Dapper + SP repos, MssqlSearchService, LocalFileService
└── API/                  ← ASP.NET Core controllers, middleware, static frontend
```

**Dependency direction:** API → Infrastructure → Application → Domain

---

## Swapping Implementations

### Swap the search engine

In `API/Program.cs`, replace:

```csharp
builder.Services.AddScoped<ISearchService, MssqlSearchService>();
```

with:

```csharp
builder.Services.AddScoped<ISearchService, TypesenseSearchService>();
```

Implement `TypesenseSearchService : ISearchService` in Infrastructure — no other changes needed.

### Swap file storage

In `API/Program.cs`, replace:

```csharp
builder.Services.AddScoped<IFileService, LocalFileService>();
```

with:

```csharp
builder.Services.AddScoped<IFileService, AzureBlobFileService>();
```

---

## Configuration Reference

`appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ResearchPublications;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
  },
  "PdfStorage": {
    "Path": "C:\\Publications\\PDFs"
  }
}
```

---

## Stop the Container

```bash
docker compose down
```

Use `docker compose down -v` to also delete the data volume.
