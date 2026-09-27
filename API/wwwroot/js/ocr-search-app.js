function ocrSearchApp() {
  return {
    query: '',
    results: [],
    total: 0,
    page: 1,
    pageSize: 20,
    elapsedMs: 0,
    loading: false,
    searched: false,
    error: null,

    get totalPages() {
      return Math.max(1, Math.ceil(this.total / this.pageSize));
    },

    async search(resetPage = true) {
      const query = this.query.trim();
      if (!query) return;
      if (resetPage) this.page = 1;

      this.loading = true;
      this.error = null;
      this.searched = true;
      try {
        const params = new URLSearchParams({
          q: query,
          page: String(this.page),
          pageSize: String(this.pageSize),
        });
        const response = await fetch(`/api/ocr-search?${params}`);
        if (!response.ok) throw new Error('OCR search failed.');
        const data = await response.json();
        this.results = data.items;
        this.total = data.total;
        this.elapsedMs = data.elapsedMs;
      } catch (error) {
        this.results = [];
        this.total = 0;
        this.error = error.message || 'OCR search failed.';
      } finally {
        this.loading = false;
      }
    },

    changePage(page) {
      if (page < 1 || page > this.totalPages || page === this.page) return;
      this.page = page;
      this.search(false);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },
  };
}
