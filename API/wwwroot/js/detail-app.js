/* ── Publication detail page Alpine component (publication.html) ─
   Depends on: shared.js
──────────────────────────────────────────────────────────────────── */

function detailApp() {
  return {
    pub: null,
    loading: false,
    error: null,

    async init() {
      const params = new URLSearchParams(window.location.search);
      const id = parseInt(params.get('id'));
      if (!id || isNaN(id)) { this.error = 'No publication ID provided.'; return; }

      this.loading = true;
      try {
        const res = await fetch(`/api/publications/${id}`);
        if (!res.ok) {
          this.error = res.status === 404 ? 'Publication not found.' : 'Failed to load publication.';
          return;
        }
        this.pub = await res.json();
      } catch {
        this.error = 'Network error — please try again.';
      } finally {
        this.loading = false;
      }
    },

    splitCsv(val) {
      return splitCsv(val);
    },
  };
}
