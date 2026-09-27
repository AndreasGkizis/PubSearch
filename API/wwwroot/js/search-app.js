/* ── Search page Alpine component (index.html) ──────────────────
   Depends on: shared.js, api.js
──────────────────────────────────────────────────────────────────── */

function searchApp() {
  return {
    // ── Reactive state ──────────────────────────────────────────
    query: '',
    filterYearFrom: '',
    filterYearTo: '',
    selectedAuthors: [],
    selectedKeywords: [],
    selectedLanguages: [],
    selectedPublicationTypes: [],
    availableAuthors: [],
    availableKeywords: [],
    availableLanguages: [],
    availablePublicationTypes: [],
    authorSearch: '',
    keywordSearch: '',
    languageSearch: '',
    publicationTypeSearch: '',
    _filteredAuthors: [],
    _filteredKeywords: [],
    _filteredLanguages: [],
    _filteredPublicationTypes: [],
    results: [],
    total: 0,
    page: 1,
    pageSize: 20,
    loading: false,
    isSearchMode: false,
    lastQuery: '',
    lastElapsedMs: null,

    // ── Computed ─────────────────────────────────────────────────
    get totalPages() {
      return Math.max(1, Math.ceil(this.total / this.pageSize));
    },

    get activeFilterCount() {
      let n = 0;
      if (this.filterYearFrom) n++;
      if (this.filterYearTo)   n++;
      n += this.selectedAuthors.length;
      n += this.selectedKeywords.length;
      n += this.selectedLanguages.length;
      n += this.selectedPublicationTypes.length;
      return n;
    },

    get filteredAuthors() {
      if (!this.authorSearch) return this.availableAuthors;
      return this._filteredAuthors;
    },

    get filteredKeywords() {
      if (!this.keywordSearch) return this.availableKeywords;
      return this._filteredKeywords;
    },

    get filteredLanguages() {
      if (!this.languageSearch) return this.availableLanguages;
      return this._filteredLanguages;
    },

    get filteredPublicationTypes() {
      if (!this.publicationTypeSearch) return this.availablePublicationTypes;
      return this._filteredPublicationTypes;
    },

    // ── Lifecycle ───────────────────────────────────────────────
    async init() {
      await this.loadAll();
      this._loadFilterOptions();
    },

    // ── Data loading ────────────────────────────────────────────
    async _loadFilterOptions() {
      try {
        const opts = await loadFilterOptions();
        this.availableAuthors          = opts.authors;
        this.availableKeywords         = opts.keywords;
        this.availableLanguages        = opts.languages;
        this.availablePublicationTypes = opts.publicationTypes;
      } catch {}
    },

    async facetSearch(field, query, targetProp) {
      if (!query.trim()) return;
      try {
        const params = new URLSearchParams({ field, q: query });
        const res = await fetch('/api/search/facets?' + params);
        const data = await res.json();
        this[targetProp] = data.map((d) => ({ name: d.name, count: d.count, highlighted: d.highlighted }));
      } catch {}
    },

    onFilterSearch(field, query, targetProp) {
      if (query.trim()) {
        this.facetSearch(field, query, targetProp);
      }
    },

    _getFilters() {
      return {
        yearFrom: this.filterYearFrom,
        yearTo: this.filterYearTo,
        authors: this.selectedAuthors,
        keywords: this.selectedKeywords,
        languages: this.selectedLanguages,
        publicationTypes: this.selectedPublicationTypes,
      };
    },

    async loadAll() {
      this.loading = true;
      this.isSearchMode = false;
      try {
        const params = new URLSearchParams({ page: this.page, pageSize: this.pageSize });
        buildFilterParams(params, this._getFilters());
        const res  = await fetch('/api/publications?' + params);
        const data = await res.json();
        this.results = data.items || [];
        this.total   = data.total ?? 0;
      } catch {
        this.results = [];
      } finally {
        this.loading = false;
      }
    },

    onQueryInput() {
      this.page = 1;
      if (!this.query.trim()) {
        this.loadAll();
      } else {
        this.search();
      }
    },

    async search() {
      if (!this.query.trim()) { this.page = 1; await this.loadAll(); return; }
      this.loading = true;
      this.isSearchMode = true;
      this.lastQuery = this.query;
      try {
        const params = new URLSearchParams({
          q: this.query, page: this.page, pageSize: this.pageSize,
        });
        buildFilterParams(params, this._getFilters());
        const res  = await fetch('/api/search?' + params);
        const data = await res.json();
        this.results       = data.items || [];
        this.total         = data.total ?? 0;
        this.lastElapsedMs = data.elapsedMs ?? null;
      } catch {
        this.results = [];
      } finally {
        this.loading = false;
      }
    },

    applyFilters() {
      this.page = 1;
      if (this.query.trim()) { this.search(); } else { this.loadAll(); }
    },

    async clearFilters() {
      this.query          = '';
      this.filterYearFrom = '';
      this.filterYearTo   = '';
      this.selectedAuthors  = [];
      this.selectedKeywords = [];
      this.selectedLanguages = [];
      this.selectedPublicationTypes = [];
      this.authorSearch   = '';
      this.keywordSearch  = '';
      this.languageSearch = '';
      this.publicationTypeSearch = '';
      this.page = 1;
      await this.loadAll();
    },

    async changePage(newPage) {
      if (newPage < 1 || newPage > this.totalPages) return;
      this.page = newPage;
      if (this.isSearchMode) { await this.search(); } else { await this.loadAll(); }
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    // ── Display helpers (delegate to shared.js) ─────────────────
    highlight(text) {
      return highlightText(text, this.lastQuery, this.isSearchMode);
    },

    highlightAuthors(pub) {
      const authors = pub.authors || [];
      if (!this.isSearchMode || !this.lastQuery.trim()) {
        return escapeHtml(authors.join(', '));
      }
      const highlighted = pub.highlightedAuthors && pub.highlightedAuthors.length > 0
        ? pub.highlightedAuthors
        : authors;
      return highlighted.map((a) => this.highlight(a)).join(', ');
    },

    highlightFilter(text, filterQuery) {
      return highlightFilter(text, filterQuery);
    },

    renderMarks(text) {
      return renderMarks(text);
    },

    splitKeywords(kw) {
      return splitCsv(kw);
    },
  };
}
