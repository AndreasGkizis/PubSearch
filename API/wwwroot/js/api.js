/* ── API helpers ──────────────────────────────────────────────────
   Thin wrappers around fetch() and shared query-building logic.
   Load after shared.js.
──────────────────────────────────────────────────────────────────── */

/** GET JSON from `url`. Returns the parsed body. */
async function apiGet(url, options) {
  const res = await fetch(url, options);
  if (!res.ok) throw new Error(`GET ${url}→ ${res.status}`);
  return res.json();
}

/** POST JSON to `url`. Returns the parsed body. */
async function apiPost(url, body) {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw Object.assign(new Error(err?.detail || 'Request failed.'), { status: res.status, body: err });
  }
  return res.json();
}

/** PUT JSON to `url`. Returns the parsed body (or null for 204). */
async function apiPut(url, body) {
  const res = await fetch(url, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw Object.assign(new Error(err?.detail || 'Request failed.'), { status: res.status, body: err });
  }
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

/** DELETE `url`. */
async function apiDelete(url) {
  const res = await fetch(url, { method: 'DELETE' });
  if (!res.ok) throw new Error(`DELETE ${url}→ ${res.status}`);
}

/**
 * Fetch the four filter-option lists (authors, keywords, languages,
 * publication types) in parallel.  Returns an object with those four arrays.
 */
async function loadFilterOptions() {
  const [authors, keywords, languages, publicationTypes] = await Promise.all([
    apiGet('/api/authors/filter-options', { cache: 'no-store' }),
    apiGet('/api/keywords/filter-options', { cache: 'no-store' }),
    apiGet('/api/languages/filter-options', { cache: 'no-store' }),
    apiGet('/api/publication-types/filter-options', { cache: 'no-store' }),
  ]);
  return { authors, keywords, languages, publicationTypes };
}

/**
 * Append year-range + multi-select filter values onto a URLSearchParams
 * instance.  `filters` is expected to have the shape:
 *
 *   { yearFrom, yearTo, authors[], keywords[], languages[], publicationTypes[] }
 */
function buildFilterParams(params, filters) {
  if (filters.yearFrom) params.append('yearFrom', filters.yearFrom);
  if (filters.yearTo)   params.append('yearTo',   filters.yearTo);
  (filters.authors  || []).forEach((a)  => params.append('authors',  a));
  (filters.keywords || []).forEach((k)  => params.append('keywords', k));
  (filters.languages || []).forEach((l) => params.append('languages', l));
  (filters.publicationTypes || []).forEach((pt) => params.append('publicationTypes', pt));
}
