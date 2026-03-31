# Research Publications Search Engine

A .NET 10 clean-architecture web application for managing and searching academic publications, backed by SQL Server and served with a lightweight Alpine.js + Tailwind CSS frontend.

---

## Architecture

```
Domain (no dependencies)
  └── Application (→ Domain)
        └── Infrastructure (→ Domain + Application)
              └── API (→ Application + Infrastructure)
```

| Layer | Responsibility |
|-------|---------------|
| **Domain** | Entities (`Publication`, `Author`, `Keyword`), repository interfaces, value objects |
| **Application** | Services, DTOs, business logic, search interface |
| **Infrastructure** | EF Core DbContext & migrations, repository implementations, search service, file storage, database seeder |
| **API** | Controllers, middleware, static frontend (`wwwroot/`) |

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0+ |
| Docker Desktop | Latest |

---

## Quick Start

### 1. Start the SQL Server container

```bash
docker compose up -d
```

This starts a SQL Server 2022 instance on `localhost:1433` with full-text search enabled.

### 2. Run the API

```bash
dotnet run --project API
```

On startup the application will automatically:
- Apply EF Core migrations to create/update the database schema
- Seed the database with **50,000 publications**, **1,000 authors**, and **25 keywords** (via [Bogus](https://github.com/bchavez/Bogus) with a fixed seed — skipped if data already exists)
- Create the PDF storage directory if configured

Then open [https://localhost:5001](https://localhost:5001) (or the port shown in console output).

---

## Features

### Public Frontend (`index.html`)
- Full-text search with debounced input
- Filter sidebar: year range, searchable keyword checkboxes, searchable author checkboxes
- Publication cards with title, authors, year, keyword tags, abstract snippet
- Single publication detail view with full body text, DOI links, and PDF download
- Pagination

### Admin Panel (`admin.html`)
- **Publications** — Create, edit, delete publications with title, year, DOI, abstract, body, keywords, authors, and PDF upload (50 MB limit)
- **Authors** — CRUD with full name, first/last name, email; shows publication count
- **Keywords** — CRUD with duplicate detection; shows publication count
- Toast notifications, delete confirmation modals, pagination on all tabs

### API Endpoints

| Route | Verbs | Description |
|-------|-------|-------------|
| `api/publications` | GET, POST | List (paged, filterable) / Create |
| `api/publications/{id}` | GET, PUT, DELETE | Detail / Update / Delete |
| `api/publications/{id}/download` | GET | Download PDF |
| `api/publications/upload` | POST | Upload PDF |
| `api/authors` | GET, POST | List (paged) / Create |
| `api/authors/{id}` | GET, PUT, DELETE | Detail / Update / Delete |
| `api/authors/filter-options` | GET | Author names with publication counts (cached) |
| `api/keywords` | GET, POST | List (paged) / Create |
| `api/keywords/{id}` | GET, PUT, DELETE | Detail / Update / Delete |
| `api/keywords/filter-options` | GET | Keyword values with publication counts (cached) |
| `api/search` | GET | Search with query, pagination, and year/author/keyword filters |

---

## Configuration

Development settings live in `API/appsettings.Development.json`:

```json
{
  "PdfStorage": {
    "Path": "../pdfs"
  },
  "SqlSettings": {
    "Server": "localhost",
    "Port": 1433,
    "DbName": "ResearchPublications",
    "UserId": "sa",
    "Password": "YourStrong!Passw0rd"
  }
}
```

PDF files are stored in the `pdfs/` directory at the repo root by default.

---

## EF Core Migrations

The app uses EF Core code-first migrations. To add a new migration:

```bash
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project API --output-dir Persistence/Migrations
```

Migrations are applied automatically on startup.

---

## Tests

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a real SQL Server instance in Docker.

```bash
dotnet test Tests/IntegrationTests
```

Test coverage:
- **PublicationCrudTests** — Create, read, list (paged), delete
- **PublicationEditTests** — Update fields, add/remove/change keywords, keyword deduplication, author deduplication
- **AuthorCrudTests** — Full CRUD, unlinking from publications on delete, publication count
- **KeywordCrudTests** — Full CRUD, duplicate detection, unlinking from publications on delete

---

## Project Structure

```
├── API/                          # ASP.NET Core Web API
│   ├── Controllers/              # Publications, Authors, Keywords, Search
│   ├── Middleware/                # Global exception handling
│   └── wwwroot/                  # Alpine.js + Tailwind CSS frontend
├── Application/                  # Business logic layer
│   ├── DTOs/                     # Data transfer objects
│   ├── Interfaces/               # Service contracts
│   └── Services/                 # Publication, Author, Keyword services
├── Domain/                       # Core domain
│   ├── Entities/                 # Publication, Author, Keyword
│   ├── Interfaces/               # Repository contracts
│   └── ValueObjects/             # Keyword value object
├── Infrastructure/               # Data access & external services
│   ├── Persistence/              # DbContext, configs, migrations, repositories, seeder
│   ├── Search/                   # MSSQL search service
│   ├── Files/                    # Local PDF file service
│   └── Settings/                 # SqlSettings
├── Tests/
│   └── IntegrationTests/         # xUnit + Testcontainers
├── pdfs/                         # PDF file storage
├── docker-compose.yml            # SQL Server 2022 container
└── ResearchPublications.slnx     # Solution file
```

---

## Tech Stack

- **.NET 10** / ASP.NET Core
- **EF Core 10** (code-first, SQL Server provider)
- **SQL Server 2022** (Docker)
- **Alpine.js** + **Tailwind CSS** (CDN, no build step)
- **Bogus** (database seeding)
- **xUnit** + **Testcontainers** (integration tests)

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
