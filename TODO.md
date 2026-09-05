TODO:
    1. keyword aware ai generated summaries.
    2. Keep Typesense synchronized after related-entity changes.
    3. Handle Typesense failures gracefully and provide a manual rebuild option.
    4. Delete orphaned PDFs when publications or attachments are removed.
    5. Add authentication/authorization if the app is deployed beyond a trusted environment.
    6. Restrict CORS and validate uploaded PDF file signatures.
    7. Add basic DTO validation for required fields, lengths, years, and uploads.
    8. Return 400/409 responses for invalid input and duplicate entities instead of 500.
    9. Fix author full-name search when the middle name is missing.
   10. Decide whether MSSQL search should remain a basic fallback.
   11. Add deterministic tie-breakers to paginated queries.
   12. Optimize case-insensitive searches only if profiling shows a need.
   13. Add focused tests for stale indexes, PDF cleanup, validation, and authorization.
