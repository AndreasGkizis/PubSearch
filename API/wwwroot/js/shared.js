/* ── Shared utilities ─────────────────────────────────────────────
   Used across index.html, publication.html, and admin.html.
   Load this script BEFORE any page-specific scripts.
──────────────────────────────────────────────────────────────────── */

/** Apply the persisted theme (or system preference) on page load. */
function initTheme() {
  if (
    localStorage.getItem('theme') === 'dark' ||
    (!localStorage.getItem('theme') &&
      window.matchMedia('(prefers-color-scheme: dark)').matches)
  ) {
    document.documentElement.classList.add('dark');
  }
}

/** Toggle dark / light mode and persist the choice. */
function toggleDarkMode() {
  document.documentElement.classList.toggle('dark');
  localStorage.setItem(
    'theme',
    document.documentElement.classList.contains('dark') ? 'dark' : 'light',
  );
}

/** Escape a string for safe insertion into HTML. */
function escapeHtml(text) {
  const div = document.createElement('div');
  div.appendChild(document.createTextNode(text));
  return div.innerHTML;
}

/**
 * Convert Typesense `<mark>…</mark>` snippets into styled highlights,
 * escaping all surrounding text.
 */
function renderMarks(text) {
  if (!text) return '';
  return text
    .split(/(<mark>|<\/mark>)/g)
    .map((part) => {
      if (part === '<mark>') return '<mark class="bg-yellow-200 rounded px-0.5">';
      if (part === '</mark>') return '</mark>';
      return escapeHtml(part);
    })
    .join('');
}

/**
 * Highlight occurrences of `query` inside `text`.
 *
 * - If the text already contains Typesense `<mark>` tags, those are styled and
 *   everything else is escaped.
 * - Otherwise a client-side regex match is used (SQL / fallback provider).
 * - When `isSearchMode` is false or `query` is empty the text is just escaped.
 */
function highlightText(text, query, isSearchMode) {
  if (!text) return '';
  if (!isSearchMode || !query || !query.trim()) return escapeHtml(text);

  // Typesense returns <mark>…</mark> in highlight snippets
  if (text.includes('<mark>')) return renderMarks(text);

  // Fallback: client-side highlighting (SQL provider)
  const safe = escapeHtml(text);
  const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const re = new RegExp('(' + escaped + ')', 'gi');
  return safe.replace(re, '<mark class="bg-yellow-200 rounded px-0.5">$1</mark>');
}

/**
 * Highlight a filter-sidebar value while the user types in the filter
 * search box.  Pure client-side — no Typesense marks involved.
 */
function highlightFilter(text, filterQuery) {
  if (!text) return '';
  if (!filterQuery || !filterQuery.trim()) return escapeHtml(text);
  const safe = escapeHtml(text);
  const escaped = filterQuery.trim().replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const re = new RegExp('(' + escaped + ')', 'gi');
  return safe.replace(re, '<mark class="bg-yellow-200 rounded px-0.5">$1</mark>');
}

/** Split a comma-separated string into trimmed, non-empty parts. */
function splitCsv(val) {
  if (!val) return [];
  return val.split(',').map((s) => s.trim()).filter(Boolean);
}
