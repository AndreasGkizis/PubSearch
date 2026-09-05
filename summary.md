# Research Publications Search Engine

## Purpose

Research Publications Search Engine is a web application for building, maintaining, and searching a catalog of academic publications. Its purpose is to make research records easy to discover through full-text search and structured filters, while providing administrators with the tools needed to keep publication metadata and related entities up to date.

The application combines a public search and browsing interface with an administrative management interface. Publication records can contain descriptive text, bibliographic metadata, relationships to authors and classification data, and an optional PDF document.

## What the application does

### Public search and browsing

The public interface allows users to:

- Search publications by text across titles, abstracts, keywords, authors, and publication body text.
- Browse publications with pagination when no text query is being used.
- Filter results by year range, author, keyword, language, and publication type.
- Search within filter values to find relevant authors, keywords, languages, and publication types.
- View result counts, relevance ordering, snippets, and highlighted matching text.
- Open a publication’s detail page to read its complete metadata and body text.
- Follow a DOI link when one is available.
- Download an associated PDF when one has been uploaded.

The search page can switch between the Typesense search provider and the MSSQL search provider. Typesense provides the primary search experience, including relevance scoring, typo tolerance, highlighting, and facet queries. MSSQL is available as an alternative database-backed search implementation.

### Administration

The administration interface provides CRUD operations for:

- Publications
- Authors
- Keywords
- Languages
- Publication types

Administrators can create, edit, and delete publications; assign authors and classification values; and upload PDFs up to 50 MB. Author, keyword, language, and publication-type records can be searched and managed independently. Related publication counts are displayed for managed entities, and the application handles relationship updates when records are edited or removed.

## Publication records

A publication may contain:

- Title
- Abstract
- Full body text
- Publication year
- DOI
- Authors, including first name, optional middle name, last name, and optional email
- Keywords
- Languages
- Publication types
- An optional PDF filename stored in local file storage
- Creation and last-modified timestamps

Authors and the classification entities are reusable records connected to publications through many-to-many relationships. Keywords are represented as normalized value objects in the domain model.

## Application behavior and data flow

SQL Server is the system of record for publication metadata and relationships. On application startup, the API:

1. Applies pending Entity Framework Core migrations.
2. Seeds the database with generated sample data when appropriate.
3. Preloads cached filter options.
4. Ensures the Typesense `publications` collection exists.
5. Indexes the stored publications in Typesense.
6. Creates the configured PDF storage directory.

When a publication is created or updated, its search document is indexed. When it is deleted, its Typesense document is removed. PDFs are stored separately from the SQL data and are streamed through the API for downloads.

## Main HTTP API

| Route | Purpose |
| --- | --- |
| `GET /api/search` | Full-text search with provider selection, pagination, year filters, and entity filters |
| `GET /api/search/facets` | Searchable facet values and counts for authors, keywords, languages, or publication types |
| `GET /api/publications` | Paginated publication summaries with structured filters |
| `GET /api/publications/{id}` | Retrieve complete publication details |
| `POST /api/publications` | Create a publication |
| `PUT /api/publications/{id}` | Update a publication |
| `DELETE /api/publications/{id}` | Delete a publication |
| `POST /api/publications/upload` | Upload a PDF and receive its stored filename |
| `GET /api/publications/{id}/download` | Download a publication PDF |
| `/api/authors` | List, search, create, update, and delete authors |
| `/api/keywords` | List, search, create, update, and delete keywords |
| `/api/languages` | List, search, create, update, and delete languages |
| `/api/publication-types` | List, search, create, update, and delete publication types |

The entity endpoints also expose cached filter-option lists used by the public and administrative interfaces.

## Architecture

The solution follows a clean-architecture dependency direction:

```text
Domain
  ↓
Application
  ↓
Infrastructure
  ↓
API
```

- **Domain** contains entities, value objects, repository contracts, and core interfaces.
- **Application** contains DTOs, services, search contracts, caching, and business workflows.
- **Infrastructure** contains Entity Framework Core persistence, migrations, repositories, SQL Server configuration, Typesense integration, database seeding, and local PDF storage.
- **API** contains ASP.NET Core controllers, exception handling middleware, configuration, and the static frontend in `wwwroot`.

The frontend is a lightweight Alpine.js application styled with Tailwind CSS and served directly by ASP.NET Core. It does not require a separate frontend build step.

## Technology and local services

- .NET 10 and ASP.NET Core
- Entity Framework Core 10 with SQL Server
- SQL Server 2022, provided through Docker Compose
- Typesense for indexed search and facets
- Alpine.js and Tailwind CSS for the frontend
- Bogus for deterministic sample-data generation
- xUnit and Testcontainers for integration tests

The development Docker Compose configuration provides SQL Server on port `1433` and Typesense on port `8108`. Application settings define the database connection details, Typesense connection, and local PDF storage path.

## User-facing pages

| Page | Default path | Description |
| --- | --- | --- |
| Search and browse | `/` | Public publication search, filters, results, and pagination |
| Publication detail | `/publication.html?id={id}` | Full details and optional PDF download |
| Administration | `/admin.html` | Publication and reference-data management |

## Current scope and future direction

The current application focuses on catalog management, discovery, and document access. The repository TODO identifies keyword-aware, AI-generated publication summaries as a possible future enhancement.
