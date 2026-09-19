# Thesis Handoff: Research Publications Search Engine

## Purpose and scope

- A web application for organizing, maintaining, searching, and accessing academic publications relevant to conservation and cultural-heritage research.
- Intended as a searchable digital bibliography and literature-discovery prototype.
- It supports research information management; it does not assess artefacts, diagnose condition, recommend treatment, or replace conservation expertise.

## Implemented functionality

- Search across publication titles, abstracts, authors, keywords, and stored body text.
- Filter by year, author, keyword, language, and publication type.
- Browse relevance-ranked results with highlights, snippets, pagination, detail pages, DOI links, and PDF downloads.
- Create, edit, and delete publications, authors, keywords, languages, and publication types.
- Associate publications with reusable authors and classification values.
- Upload PDF files up to 50 MB.
- Search the text stored in publication records; uploaded PDF contents are not extracted or searched.

## Technical overview

- Built with ASP.NET Core, SQL Server, Entity Framework Core, Typesense, Alpine.js, and Tailwind CSS.
- SQL Server stores publication records and relationships.
- Typesense provides the primary indexed search experience, including relevance ranking, typo tolerance, highlighting, and facets.
- A database-backed search provider is also available as a basic alternative.

## Thesis wording

- Present it as a working research-information management and literature-discovery prototype.
- Emphasize structured metadata, reusable relationships, search, filtering, and document access.
- Describe the academic context as conservation and cultural-heritage research, while keeping the software objective focused on publication management and retrieval.
- Do not claim that it provides conservation decision support, assesses artefacts, extracts PDF text, or implements AI-generated summaries.
