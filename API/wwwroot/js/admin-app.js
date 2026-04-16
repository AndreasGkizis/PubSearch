/* ── Admin page Alpine component (admin.html) ───────────────────
   Depends on: shared.js, api.js, admin-crud.js
──────────────────────────────────────────────────────────────────── */

// ── Entity CRUD mixins (Authors, Keywords, Languages, Pub Types) ──

const _authorsCrud = createEntityCrud({
  key: 'authors',
  statePrefix: 'author',
  methodSuffix: 'Author',
  apiBase: '/api/authors',
  entityName: 'Author',
  emptyForm: { id: null, firstName: '', middleName: '', lastName: '', email: '' },
  toPayload: (f) => ({
    firstName:  f.firstName || '',
    middleName: f.middleName || null,
    lastName:   f.lastName || '',
    email:      f.email || null,
  }),
  editFromItem: (a) => ({
    id:         a.id,
    firstName:  a.firstName ?? '',
    middleName: a.middleName ?? '',
    lastName:   a.lastName ?? '',
    email:      a.email ?? '',
  }),
});

const _keywordsCrud = createEntityCrud({
  key: 'keywords',
  statePrefix: 'keyword',
  methodSuffix: 'Keyword',
  apiBase: '/api/keywords',
  entityName: 'Keyword',
  emptyForm: { id: null, value: '' },
  toPayload: (f) => ({ value: f.value }),
});

const _languagesCrud = createEntityCrud({
  key: 'languages',
  statePrefix: 'language',
  methodSuffix: 'Language',
  apiBase: '/api/languages',
  entityName: 'Language',
  emptyForm: { id: null, value: '' },
  toPayload: (f) => ({ value: f.value }),
});

const _pubTypesCrud = createEntityCrud({
  key: 'pubTypes',
  statePrefix: 'pubType',
  methodSuffix: 'PublicationType',
  apiBase: '/api/publication-types',
  entityName: 'Publication Type',
  emptyForm: { id: null, value: '' },
  toPayload: (f) => ({ value: f.value }),
});

// ────────────────────────────────────────────────────────────────

function adminApp() {
  const app = {
    activeTab: 'publications',

    // ── Publications state ──
    publications: [],
    total: 0,
    page: 1,
    pageSize: 20,
    loading: false,
    showModal: false,
    saving: false,
    formError: null,
    deleteTarget: null,
    deleting: false,
    toast: null,

    // ── Search & filter state (publications tab) ──
    query: '',
    lastQuery: '',
    isSearchMode: false,
    filterYearFrom: '',
    filterYearTo: '',
    filterSelectedAuthors: [],
    filterSelectedKeywords: [],
    filterSelectedLanguages: [],
    filterSelectedPublicationTypes: [],
    availableAuthors: [],
    availableKeywords: [],
    availableLanguages: [],
    availablePublicationTypes: [],
    authorFilterSearch: '',
    keywordFilterSearch: '',
    languageFilterSearch: '',
    publicationTypeFilterSearch: '',

    form: {
      id: null, title: '', year: null, doi: '', keywords: '', authorsText: '',
      abstract: '', body: '', pdfFileName: '', languages: '', publicationTypes: '',
    },
    uploadingPdf: false,
    uploadError: null,
    pdfPreview: { show: false, fileName: '', extractedText: '' },
    _pendingPdfFileName: '', // tracks a confirmed-but-not-yet-saved upload for cleanup on cancel

    // ── Searchable dropdown state (publication form modal) ──
    selectedKeywords: [],
    kwSearch: '',
    kwSearchResults: [],
    kwDropdownOpen: false,

    selectedAuthors: [],
    authorSearch: '',
    authorSearchResults: [],
    authorDropdownOpen: false,
    showInlineAuthorForm: false,
    inlineAuthor: { firstName: '', middleName: '', lastName: '', email: '' },
    inlineAuthorError: null,
    creatingInlineAuthor: false,

    selectedLanguages: [],
    langSearch: '',
    langSearchResults: [],
    langDropdownOpen: false,

    selectedPubTypes: [],
    ptSearch: '',
    ptSearchResults: [],
    ptDropdownOpen: false,

    // ── Computed ─────────────────────────────────────────────────
    get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); },

    get activeFilterCount() {
      let n = 0;
      if (this.filterYearFrom) n++;
      if (this.filterYearTo)   n++;
      n += this.filterSelectedAuthors.length;
      n += this.filterSelectedKeywords.length;
      n += this.filterSelectedLanguages.length;
      n += this.filterSelectedPublicationTypes.length;
      return n;
    },

    get filteredFilterAuthors() {
      if (!this.authorFilterSearch) return this.availableAuthors;
      const q = this.authorFilterSearch.toLowerCase();
      return this.availableAuthors.filter((a) => a.name.toLowerCase().includes(q));
    },

    get filteredFilterKeywords() {
      if (!this.keywordFilterSearch) return this.availableKeywords;
      const q = this.keywordFilterSearch.toLowerCase();
      return this.availableKeywords.filter((k) => k.name.toLowerCase().includes(q));
    },

    get filteredFilterLanguages() {
      if (!this.languageFilterSearch) return this.availableLanguages;
      const q = this.languageFilterSearch.toLowerCase();
      return this.availableLanguages.filter((l) => l.name.toLowerCase().includes(q));
    },

    get filteredFilterPublicationTypes() {
      if (!this.publicationTypeFilterSearch) return this.availablePublicationTypes;
      const q = this.publicationTypeFilterSearch.toLowerCase();
      return this.availablePublicationTypes.filter((pt) => pt.name.toLowerCase().includes(q));
    },

    // ── Lifecycle ───────────────────────────────────────────────
    async init() {
      await this.loadAll();
      this._loadFilterOptions();
    },

    async switchTab(tab) {
      this.activeTab = tab;
      this.loading = true;
      try {
        if (tab === 'publications') {
          if (this.isSearchMode) { await this.searchPublications(); }
          else { await this.loadAll(); }
        }
        else if (tab === 'authors')          await this.loadAuthors();
        else if (tab === 'keywords')         await this.loadKeywords();
        else if (tab === 'languages')        await this.loadLanguages();
        else if (tab === 'publicationTypes') await this.loadPubTypes();
      } finally { this.loading = false; }
    },

    // ── Filter options ──────────────────────────────────────────
    async _loadFilterOptions() {
      try {
        const opts = await loadFilterOptions();
        this.availableAuthors          = opts.authors;
        this.availableKeywords         = opts.keywords;
        this.availableLanguages        = opts.languages;
        this.availablePublicationTypes = opts.publicationTypes;
      } catch {}
    },

    _getFilters() {
      return {
        yearFrom: this.filterYearFrom,
        yearTo: this.filterYearTo,
        authors: this.filterSelectedAuthors,
        keywords: this.filterSelectedKeywords,
        languages: this.filterSelectedLanguages,
        publicationTypes: this.filterSelectedPublicationTypes,
      };
    },

    // ── Publications CRUD ───────────────────────────────────────
    async loadAll() {
      this.loading = true;
      this.isSearchMode = false;
      try {
        const params = new URLSearchParams({ page: this.page, pageSize: this.pageSize });
        buildFilterParams(params, this._getFilters());
        const res  = await fetch('/api/publications?' + params);
        const data = await res.json();
        this.publications = data.items || [];
        this.total = data.total ?? 0;
      } catch { this.publications = []; }
      finally { this.loading = false; }
    },

    onQueryInput() {
      this.page = 1;
      if (!this.query.trim()) { this.loadAll(); } else { this.searchPublications(); }
    },

    async searchPublications() {
      if (!this.query.trim()) { this.page = 1; await this.loadAll(); return; }
      this.loading = true;
      this.isSearchMode = true;
      this.lastQuery = this.query;
      try {
        const params = new URLSearchParams({ q: this.query, page: this.page, pageSize: this.pageSize });
        buildFilterParams(params, this._getFilters());
        const res  = await fetch('/api/search?' + params);
        const data = await res.json();
        this.publications = data.items || [];
        this.total = data.total ?? 0;
      } catch { this.publications = []; }
      finally { this.loading = false; }
    },

    applyFilters() {
      this.page = 1;
      if (this.query.trim()) { this.searchPublications(); } else { this.loadAll(); }
    },

    async clearFilters() {
      this.query = '';
      this.filterYearFrom = '';
      this.filterYearTo   = '';
      this.filterSelectedAuthors  = [];
      this.filterSelectedKeywords = [];
      this.filterSelectedLanguages = [];
      this.filterSelectedPublicationTypes = [];
      this.authorFilterSearch   = '';
      this.keywordFilterSearch  = '';
      this.languageFilterSearch = '';
      this.publicationTypeFilterSearch = '';
      this.page = 1;
      await this.loadAll();
    },

    async changePage(p) {
      if (p < 1 || p > this.totalPages) return;
      this.page = p;
      if (this.isSearchMode) { await this.searchPublications(); } else { await this.loadAll(); }
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    // ── Display helpers ─────────────────────────────────────────
    splitKeywords(kw) { return splitCsv(kw); },
    highlight(text) { return highlightText(text, this.lastQuery, this.isSearchMode); },

    // ── Publication form: open / edit / save / delete ───────────
    _resetFormDropdowns() {
      this.selectedKeywords = [];
      this.kwSearch = '';  this.kwSearchResults = [];  this.kwDropdownOpen = false;
      this.selectedAuthors = [];
      this.authorSearch = '';  this.authorSearchResults = [];  this.authorDropdownOpen = false;
      this.showInlineAuthorForm = false;
      this.inlineAuthor = { firstName: '', middleName: '', lastName: '', email: '' };
      this.inlineAuthorError = null;
      this.selectedLanguages = [];
      this.langSearch = '';  this.langSearchResults = [];  this.langDropdownOpen = false;
      this.selectedPubTypes = [];
      this.ptSearch = '';  this.ptSearchResults = [];  this.ptDropdownOpen = false;
    },

    openNew() {
      this.form = {
        id: null, title: '', year: null, doi: '', keywords: '', authorsText: '',
        abstract: '', body: '', pdfFileName: '', languages: '', publicationTypes: '',
      };
      this._resetFormDropdowns();
      this.formError  = null;
      this.uploadError = null;
      this._pendingPdfFileName = '';
      this.showModal  = true;
    },

    async openEdit(id) {
      try {
        const res = await fetch(`/api/publications/${id}`);
        if (!res.ok) return;
        const pub = await res.json();
        this.form = {
          id: pub.id,
          title: pub.title ?? '',
          year: pub.year ?? null,
          doi: pub.doi ?? '',
          keywords: pub.keywords ?? '',
          authorsText: '',
          authorObjects: pub.authors || [],
          abstract: pub.abstract ?? '',
          body: pub.body ?? '',
          pdfFileName: pub.pdfFileName ?? '',
          languages: pub.languages ?? '',
          publicationTypes: pub.publicationTypes ?? '',
        };

        // Authors
        this.selectedAuthors = (pub.authors || []).map((a) => ({
          id: a.id, firstName: a.firstName, middleName: a.middleName,
          lastName: a.lastName, email: a.email, publicationCount: 0,
        }));
        this.authorSearch = '';  this.authorSearchResults = [];
        this.authorDropdownOpen = false;  this.showInlineAuthorForm = false;
        this.inlineAuthor = { firstName: '', middleName: '', lastName: '', email: '' };
        this.inlineAuthorError = null;

        // Keywords
        this.selectedKeywords = await this._resolveEntities(pub.keywords, '/api/keywords/search');
        this.kwSearch = '';  this.kwSearchResults = [];  this.kwDropdownOpen = false;

        // Languages
        this.selectedLanguages = await this._resolveEntities(pub.languages, '/api/languages/search');
        this.langSearch = '';  this.langSearchResults = [];  this.langDropdownOpen = false;

        // Publication Types
        this.selectedPubTypes = await this._resolveEntities(pub.publicationTypes, '/api/publication-types/search');
        this.ptSearch = '';  this.ptSearchResults = [];  this.ptDropdownOpen = false;

        this.formError  = null;
        this.uploadError = null;
        this._pendingPdfFileName = '';
        this.showModal  = true;
      } catch {}
    },

    /** Parse a comma-separated string and resolve real IDs by searching the API. */
    async _resolveEntities(csv, searchUrl) {
      if (!csv) return [];
      const items = csv.split(',').map((v, i) => ({
        id: -(i + 1), value: v.trim(), publicationCount: 0,
      })).filter((x) => x.value.length > 0);
      for (let i = 0; i < items.length; i++) {
        try {
          const sr = await fetch(`${searchUrl}?q=${encodeURIComponent(items[i].value)}&limit=5`);
          if (sr.ok) {
            const results = await sr.json();
            const exact = results.find((r) => r.value.toLowerCase() === items[i].value.toLowerCase());
            if (exact) { items[i].id = exact.id; items[i].publicationCount = exact.publicationCount; }
          }
        } catch {}
      }
      return items;
    },

    async uploadPdf(event) {
      const file = event.target.files?.[0];
      if (!file) return;
      this.uploadingPdf = true;
      this.uploadError  = null;
      try {
        const fd = new FormData();
        fd.append('file', file);
        const res = await fetch('/api/publications/upload', { method: 'POST', body: fd });
        if (!res.ok) {
          const err = await res.json().catch(() => ({}));
          this.uploadError = err?.error || 'Upload failed.';
          return;
        }
        const data = await res.json();
        // Show preview modal — user must confirm before the file is committed to the form
        this.pdfPreview = { show: true, fileName: data.fileName, extractedText: data.extractedText || '' };
      } catch {
        this.uploadError = 'Network error during upload.';
      } finally {
        this.uploadingPdf = false;
        event.target.value = '';
      }
    },

    confirmPdfPreview() {
      this.form.pdfFileName = this.pdfPreview.fileName;
      this._pendingPdfFileName = this.pdfPreview.fileName;
      if (this.pdfPreview.extractedText && !this.form.body)
        this.form.body = this.pdfPreview.extractedText;
      this.pdfPreview = { show: false, fileName: '', extractedText: '' };
    },

    async discardPdfPreview() {
      try {
        await fetch(`/api/publications/files/${encodeURIComponent(this.pdfPreview.fileName)}`, { method: 'DELETE' });
      } catch { /* best-effort cleanup */ }
      this.pdfPreview = { show: false, fileName: '', extractedText: '' };
    },

    // ── Searchable dropdown helpers (Keywords) ──────────────────
    async searchKeywordsDropdown() {
      const q = this.kwSearch.trim();
      if (!q) { this.kwSearchResults = []; this.kwDropdownOpen = false; return; }
      try {
        const res = await fetch(`/api/keywords/search?q=${encodeURIComponent(q)}&limit=20`);
        if (res.ok) this.kwSearchResults = await res.json();
      } catch { this.kwSearchResults = []; }
      this.kwDropdownOpen = true;
    },

    selectKeyword(kw) {
      if (!this.selectedKeywords.some((s) => s.id === kw.id)) this.selectedKeywords.push({ ...kw });
      this.kwSearch = '';  this.kwSearchResults = [];  this.kwDropdownOpen = false;
    },

    removeKeyword(id) { this.selectedKeywords = this.selectedKeywords.filter((k) => k.id !== id); },

    async createAndSelectKeyword() {
      await this._createAndSelect('keywords', '/api/keywords', this.kwSearch, 'selectedKeywords', () => {
        this.kwSearch = '';  this.kwSearchResults = [];  this.kwDropdownOpen = false;
      });
    },

    // ── Searchable dropdown helpers (Languages) ─────────────────
    async searchLanguagesDropdown() {
      const q = this.langSearch.trim();
      if (!q) { this.langSearchResults = []; this.langDropdownOpen = false; return; }
      try {
        const res = await fetch(`/api/languages/search?q=${encodeURIComponent(q)}&limit=20`);
        if (res.ok) this.langSearchResults = await res.json();
      } catch { this.langSearchResults = []; }
      this.langDropdownOpen = true;
    },

    selectLanguage(lang) {
      if (!this.selectedLanguages.some((s) => s.id === lang.id)) this.selectedLanguages.push({ ...lang });
      this.langSearch = '';  this.langSearchResults = [];  this.langDropdownOpen = false;
    },

    removeLanguage(id) { this.selectedLanguages = this.selectedLanguages.filter((l) => l.id !== id); },

    async createAndSelectLanguage() {
      await this._createAndSelect('languages', '/api/languages', this.langSearch, 'selectedLanguages', () => {
        this.langSearch = '';  this.langSearchResults = [];  this.langDropdownOpen = false;
      });
    },

    // ── Searchable dropdown helpers (Publication Types) ─────────
    async searchPubTypesDropdown() {
      const q = this.ptSearch.trim();
      if (!q) { this.ptSearchResults = []; this.ptDropdownOpen = false; return; }
      try {
        const res = await fetch(`/api/publication-types/search?q=${encodeURIComponent(q)}&limit=20`);
        if (res.ok) this.ptSearchResults = await res.json();
      } catch { this.ptSearchResults = []; }
      this.ptDropdownOpen = true;
    },

    selectPubType(pt) {
      if (!this.selectedPubTypes.some((s) => s.id === pt.id)) this.selectedPubTypes.push({ ...pt });
      this.ptSearch = '';  this.ptSearchResults = [];  this.ptDropdownOpen = false;
    },

    removePubType(id) { this.selectedPubTypes = this.selectedPubTypes.filter((p) => p.id !== id); },

    async createAndSelectPubType() {
      await this._createAndSelect('publication-types', '/api/publication-types', this.ptSearch, 'selectedPubTypes', () => {
        this.ptSearch = '';  this.ptSearchResults = [];  this.ptDropdownOpen = false;
      });
    },

    // ── Searchable dropdown helpers (Authors) ───────────────────
    async searchAuthorsDropdown() {
      const q = this.authorSearch.trim();
      if (!q) { this.authorSearchResults = []; this.authorDropdownOpen = false; return; }
      try {
        const res = await fetch(`/api/authors/search?q=${encodeURIComponent(q)}&limit=20`);
        if (res.ok) this.authorSearchResults = await res.json();
      } catch { this.authorSearchResults = []; }
      this.authorDropdownOpen = true;
    },

    selectAuthor(a) {
      if (!this.selectedAuthors.some((s) => s.id === a.id)) this.selectedAuthors.push({ ...a });
      this.authorSearch = '';  this.authorSearchResults = [];
      this.authorDropdownOpen = false;  this.showInlineAuthorForm = false;
    },

    removeAuthor(id) { this.selectedAuthors = this.selectedAuthors.filter((a) => a.id !== id); },

    async createAndSelectAuthor() {
      this.inlineAuthorError = null;
      if (!this.inlineAuthor.firstName?.trim() || !this.inlineAuthor.lastName?.trim()) {
        this.inlineAuthorError = 'First name and last name are required.';
        return;
      }
      this.creatingInlineAuthor = true;
      try {
        const payload = {
          firstName:  this.inlineAuthor.firstName.trim(),
          middleName: this.inlineAuthor.middleName?.trim() || null,
          lastName:   this.inlineAuthor.lastName.trim(),
          email:      this.inlineAuthor.email?.trim() || null,
        };
        const data = await apiPost('/api/authors', payload);
        this.selectedAuthors.push({
          id: data.id, firstName: payload.firstName, middleName: payload.middleName,
          lastName: payload.lastName, email: payload.email, publicationCount: 0,
        });
        this.inlineAuthor = { firstName: '', middleName: '', lastName: '', email: '' };
        this.showInlineAuthorForm = false;
        this.authorSearch = '';  this.authorSearchResults = [];  this.authorDropdownOpen = false;
      } catch (e) {
        this.inlineAuthorError = e.message || 'Network error.';
      } finally { this.creatingInlineAuthor = false; }
    },

    /**
     * Generic "create entity and add to selection" helper.
     * Used by keywords, languages, and publication types dropdowns.
     */
    async _createAndSelect(apiSlug, apiBase, searchModel, selectedProp, resetFn) {
      const value = searchModel.trim();
      if (!value) return;
      try {
        const data = await apiPost(apiBase, { value });
        this[selectedProp].push({ id: data.id, value, publicationCount: 0 });
        resetFn();
      } catch (e) {
        if (e.status === 500) {
          // Might already exist — try to find it
          try {
            const sr = await fetch(`${apiBase}/search?q=${encodeURIComponent(value)}&limit=5`);
            if (sr.ok) {
              const results = await sr.json();
              const exact = results.find((r) => r.value.toLowerCase() === value.toLowerCase());
              if (exact && !this[selectedProp].some((s) => s.id === exact.id)) {
                this[selectedProp].push(exact);
                resetFn();
              }
            }
          } catch {}
        }
      }
    },

    // ── Save / delete publications ──────────────────────────────
    async saveForm() {
      this.formError = null;
      this.saving = true;
      const payload = {
        title:            this.form.title,
        year:             this.form.year || null,
        doi:              this.form.doi || null,
        keywords:         this.selectedKeywords.map((k) => k.value).join(', ') || null,
        languages:        this.selectedLanguages.map((l) => l.value).join(', ') || null,
        publicationTypes: this.selectedPubTypes.map((pt) => pt.value).join(', ') || null,
        abstract:         this.form.abstract || null,
        body:             this.form.body || null,
        pdfFileName:      this.form.pdfFileName || null,
        authors:          this.selectedAuthors.map((a) => ({
          id: a.id, firstName: a.firstName, middleName: a.middleName || null,
          lastName: a.lastName, email: a.email || null,
        })),
      };
      try {
        const url    = this.form.id ? `/api/publications/${this.form.id}` : '/api/publications';
        const method = this.form.id ? 'PUT' : 'POST';
        const res    = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
        if (!res.ok) { const err = await res.json().catch(() => ({})); this.formError = err?.detail || 'Save failed.'; return; }
        this.showToast(this.form.id ? 'Publication updated.' : 'Publication created.');
        this._pendingPdfFileName = '';
        this.closeModal();
        if (this.isSearchMode) { await this.searchPublications(); } else { await this.loadAll(); }
      } catch {
        this.formError = 'Network error — please try again.';
      } finally { this.saving = false; }
    },

    closeModal() {
      // Discard any PDF that was uploaded+confirmed but the publication was never saved
      if (this.pdfPreview.show) this.discardPdfPreview();
      if (this._pendingPdfFileName) {
        fetch(`/api/publications/files/${encodeURIComponent(this._pendingPdfFileName)}`, { method: 'DELETE' }).catch(() => {});
        this._pendingPdfFileName = '';
      }
      this.showModal = false;
      this.uploadError = null;
    },
    confirmDelete(pub) { this.deleteTarget = pub; },

    async doDelete() {
      if (!this.deleteTarget) return;
      this.deleting = true;
      try {
        await apiDelete(`/api/publications/${this.deleteTarget.id}`);
        this.showToast('Publication deleted.');
        this.deleteTarget = null;
        if (this.isSearchMode) { await this.searchPublications(); } else { await this.loadAll(); }
      } catch {} finally { this.deleting = false; }
    },

    // ── Toast ───────────────────────────────────────────────────
    showToast(msg) {
      this.toast = msg;
      setTimeout(() => { this.toast = null; }, 3000);
    },
  };

  // ── Merge entity CRUD mixins ──────────────────────────────────
  // Each mixin contributes state + methods for one admin tab.
  // Computed getters (e.g. authorsTotalPages) are defined as
  // { get() {} } objects, so we install them via defineProperty.

  for (const mixin of [_authorsCrud, _keywordsCrud, _languagesCrud, _pubTypesCrud]) {
    for (const [key, val] of Object.entries(mixin)) {
      if (val && typeof val === 'object' && typeof val.get === 'function') {
        Object.defineProperty(app, key, { get: val.get, configurable: true, enumerable: true });
      } else {
        app[key] = val;
      }
    }
  }

  return app;
}
