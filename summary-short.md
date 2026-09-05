# Thesis Project Summary: Research Publications Search Engine

## Project context

This project is a web-based research-publication catalog developed in the context of a degree in the Conservation of Antiquities and Works of Art. Its role is to support the organization, preservation, and retrieval of scholarly information relevant to the study, documentation, and conservation of cultural heritage. The application is a research-information management tool; it does not perform the physical conservation, scientific examination, or condition assessment of artefacts itself.

## Purpose

The purpose of the application is to provide a structured repository in which academic publications can be stored, described, classified, searched, and accessed. By bringing publication metadata, full-text content, author information, subject keywords, languages, publication types, and optional PDF files into one system, it helps students, researchers, and professionals locate relevant literature more efficiently.

The system addresses a common research problem: information about conservation, archaeology, materials, techniques, restoration, and related cultural-heritage subjects can be difficult to discover when it is distributed across unstructured files or inadequately indexed records. The application improves discoverability through consistent metadata and full-text search.

## Main functionality

The public interface enables users to:

- Search publication titles, abstracts, keywords, authors, and full body text.
- Filter results by publication year, author, keyword, language, and publication type.
- Browse paginated publication results and inspect relevance-ranked matches.
- Use searchable filter lists and view highlighted search terms and text snippets.
- Open a detailed publication record, follow its DOI, and download its associated PDF.

The administration interface enables authorized users of the application to:

- Create, edit, and delete publication records.
- Maintain reusable author, keyword, language, and publication-type records.
- Associate publications with their authors and descriptive classifications.
- Upload PDF documents, subject to a 50 MB size limit.
- Correct and update bibliographic information as the research collection develops.

## Information represented by the system

Each publication can include a title, abstract, full text, year, DOI, authors, keywords, languages, publication types, timestamps, and an optional PDF. The related entities are maintained as reusable records, allowing the same author or classification value to be associated with multiple publications. This structure supports consistent cataloging and more precise retrieval.

## Technical implementation

The application is implemented as a .NET 10 ASP.NET Core web application using a clean-architecture structure:

- **Domain layer:** publication, author, keyword, language, and publication-type entities, value objects, and interfaces.
- **Application layer:** business services, data-transfer objects, search contracts, and caching.
- **Infrastructure layer:** SQL Server persistence through Entity Framework Core, database migrations, sample-data seeding, local PDF storage, and Typesense indexing.
- **API and presentation layer:** REST endpoints, error handling, and a static Alpine.js/Tailwind CSS frontend.

SQL Server stores the authoritative publication data and relationships. Typesense creates a searchable index that supports relevance ranking, typo tolerance, highlighted matches, and facet-based filter discovery. The system can also execute searches through an MSSQL provider. Publication changes are reflected in the search index, while PDF files are stored separately and served through the API.

## Application outcome

The result is a functional prototype of a searchable digital bibliography for conservation and cultural-heritage research. It demonstrates how a structured metadata model, full-text indexing, and a user-friendly web interface can support literature review and knowledge organization within the field of conservation of antiquities and works of art.

The current scope is focused on publication management and discovery. A possible future extension identified in the project is the generation of keyword-aware, AI-assisted summaries of publications.

## Guidance for thesis composition

When writing about this project, describe it as a digital information-management and literature-discovery system supporting conservation research. The thesis should distinguish between:

1. The **academic context**, which is the conservation of antiquities and works of art.
2. The **software objective**, which is the structured management and retrieval of research publications.
3. The **implemented result**, which is a working web application with administration, search, filtering, metadata management, and PDF access.

Do not claim that the application diagnoses objects, evaluates conservation conditions, recommends treatments, or replaces expert conservation judgment, because those functions are outside its implemented scope.
