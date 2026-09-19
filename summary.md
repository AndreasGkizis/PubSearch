# Thesis Project Summary: Research Publications Search Engine

## Project context

Research Publications Search Engine is a web-based catalog developed in the context of a degree in the Conservation of Antiquities and Works of Art. It supports the organization, discovery, and retrieval of scholarly information relevant to conservation and cultural-heritage research.

The application is a research-information management and literature-discovery tool. It does not inspect artefacts, assess their condition, recommend conservation treatments, or replace professional conservation judgment.

## Problem and objective

Relevant literature can be difficult to find when bibliographic records, descriptive metadata, research text, and documents are distributed across unstructured files or inconsistently classified collections. The project addresses this problem through a structured catalog that supports:

- Publication metadata and stored body text.
- Reusable authors, keywords, languages, and publication types.
- Text search and structured filtering.
- Maintenance of records and relationships.
- Access to associated PDF files.

The intended outcome is a functional prototype of a searchable digital bibliography for conservation and cultural-heritage research.

## Implemented user functionality

### Search and browsing

Users can:

- Search titles, abstracts, authors, keywords, and stored publication body text.
- Filter results by year range, author, keyword, language, and publication type.
- Search within available filter values.
- Browse paginated, relevance-ranked results with highlighted terms and snippets.
- Open publication detail pages, follow DOI links, and download associated PDFs.

Search applies to the information stored in publication records. Uploaded PDF contents are not extracted or searched.

### Record management

The application provides management functions for:

- Publications.
- Authors.
- Keywords.
- Languages.
- Publication types.

Publications can be associated with reusable authors and classification values. PDF files can be uploaded up to 50 MB and made available for download.

## Information model

A publication can contain a title, abstract, body text, year, DOI, authors, keywords, languages, publication types, timestamps, and an optional PDF. Authors and classification values are reusable entities connected to publications through many-to-many relationships. This supports consistent cataloging and more precise retrieval.

## Architecture and technology

The application follows a clean-architecture structure with Domain, Application, Infrastructure, and API/presentation layers.

- ASP.NET Core provides the API and serves the frontend.
- Entity Framework Core and SQL Server store publication data and relationships.
- Typesense provides indexed search, facets, relevance ranking, typo tolerance, highlights, and snippets.
- A database-backed search provider is available as a basic alternative.
- Alpine.js and Tailwind CSS provide a lightweight frontend.
- Docker Compose supports local development services.

SQL Server stores the primary publication data. Typesense provides the main indexed search experience. Publication records and their related metadata are represented in the search index so that users can search and filter the catalog effectively.

## Application outcome

The result is a working prototype of a searchable digital bibliography. It demonstrates how structured metadata, reusable relationships, indexed search, filtering, and a web interface can support literature review and knowledge organization in conservation and cultural-heritage research.

## Scope boundaries and future direction

The current scope is publication management, discovery, and document access. It does not provide conservation assessment or treatment recommendations.

Possible future work includes keyword-aware, AI-assisted publication summaries. This is not implemented in the current application.

## Guidance for thesis composition

Describe the project as:

1. A digital information-management and literature-discovery system supporting conservation and cultural-heritage research.
2. A structured bibliographic catalog with search, filtering, record management, and document access.
3. A working prototype that demonstrates how software can support the organization and retrieval of research literature.

Keep these boundaries explicit:

- Do not claim that the system assesses artefacts, diagnoses conservation conditions, recommends treatments, or replaces expert judgment.
- Do not claim that uploaded PDFs are parsed or searched.
- Do not describe AI-generated summaries as implemented.
