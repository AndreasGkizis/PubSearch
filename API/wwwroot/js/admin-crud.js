/* ── Generic CRUD mixin factory for admin entity tabs ────────────
   Each call to `createEntityCrud()` returns a plain object of Alpine
   reactive properties + methods that can be spread into an Alpine
   component via Object.defineProperty / direct assignment in `init()`.

   This eliminates the 4× repeated load / search / paginate / open /
   save / delete patterns for Authors, Keywords, Languages, and
   Publication Types.

   Naming conventions (example: key="authors", statePrefix="author",
   methodSuffix="Author"):

     Collection state (plural key):
       authorsList, authorsTotal, authorsPage, authorsPageSize,
       authorsSearch, authorsTotalPages (getter)

     Entity state (singular statePrefix):
       authorForm, authorFormError, showAuthorModal, savingAuthor,
       deleteAuthorTarget, deletingAuthor

     Collection methods (capitalised key):
       loadAuthors(), changeAuthorsPage(p)

     Entity methods (methodSuffix):
       openNewAuthor(), openEditAuthor(item), saveAuthor(),
       confirmDeleteAuthor(item), doDeleteAuthor()
──────────────────────────────────────────────────────────────────── */

/**
 * @param {object}   cfg
 * @param {string}   cfg.key          — lowercase plural, e.g. "authors"
 * @param {string}   cfg.statePrefix  — lowercase singular for entity state, e.g. "author"
 * @param {string}   cfg.methodSuffix — PascalCase singular for methods, e.g. "Author"
 * @param {string}   cfg.apiBase      — API path prefix, e.g. "/api/authors"
 * @param {string}   cfg.entityName   — human label singular, e.g. "Author"
 * @param {number}   [cfg.pageSize]   — items per page (default 20)
 * @param {object}   cfg.emptyForm    — blank form template
 * @param {Function} cfg.toPayload    — (form) => request body for save
 * @param {Function} [cfg.editFromItem] — (item) => form for editing (defaults to shallow copy)
 */
function createEntityCrud(cfg) {
  const K      = cfg.key.charAt(0).toUpperCase() + cfg.key.slice(1);  // e.g. "Authors"
  const S      = cfg.statePrefix;                                      // e.g. "author"
  const SP     = S.charAt(0).toUpperCase() + S.slice(1);              // e.g. "Author"
  const M      = cfg.methodSuffix;                                     // e.g. "Author"
  const size   = cfg.pageSize || 20;
  const editFn = cfg.editFromItem || ((item) => {
    const f = {};
    for (const k of Object.keys(cfg.emptyForm)) f[k] = item[k] ?? '';
    return f;
  });

  const mixin = {};

  // ── Collection state (plural key) ─────────────────────────────
  mixin[`${cfg.key}List`]     = [];
  mixin[`${cfg.key}Total`]    = 0;
  mixin[`${cfg.key}Page`]     = 1;
  mixin[`${cfg.key}PageSize`] = size;
  mixin[`${cfg.key}Search`]   = '';

  // ── Entity state (singular statePrefix) ───────────────────────
  mixin[`${S}Form`]           = { ...cfg.emptyForm };
  mixin[`${S}FormError`]      = null;
  mixin[`show${SP}Modal`]     = false;
  mixin[`saving${SP}`]        = false;
  mixin[`delete${SP}Target`]  = null;
  mixin[`deleting${SP}`]      = false;

  // ── Computed ──────────────────────────────────────────────────
  mixin[`${cfg.key}TotalPages`] = {
    get() { return Math.max(1, Math.ceil(this[`${cfg.key}Total`] / size)); }
  };

  // ── Collection methods ────────────────────────────────────────
  mixin[`load${K}`] = async function () {
    try {
      let url = `${cfg.apiBase}?page=${this[`${cfg.key}Page`]}&pageSize=${size}`;
      const q = this[`${cfg.key}Search`];
      if (q) url += `&q=${encodeURIComponent(q)}`;
      const data = await apiGet(url);
      this[`${cfg.key}List`]  = data.items || [];
      this[`${cfg.key}Total`] = data.total ?? 0;
    } catch {
      this[`${cfg.key}List`] = [];
    }
  };

  mixin[`change${K}Page`] = async function (p) {
    if (p < 1 || p > this[`${cfg.key}TotalPages`]) return;
    this[`${cfg.key}Page`] = p;
    this.loading = true;
    try { await this[`load${K}`](); } finally { this.loading = false; }
  };

  // ── Entity methods ────────────────────────────────────────────
  mixin[`openNew${M}`] = function () {
    this[`${S}Form`]      = { ...cfg.emptyForm };
    this[`${S}FormError`] = null;
    this[`show${M}Modal`] = true;
  };

  mixin[`openEdit${M}`] = function (item) {
    this[`${S}Form`]      = editFn(item);
    this[`${S}FormError`] = null;
    this[`show${M}Modal`] = true;
  };

  mixin[`save${M}`] = async function () {
    this[`${S}FormError`] = null;
    this[`saving${M}`]    = true;
    const form    = this[`${S}Form`];
    const payload = cfg.toPayload(form);
    try {
      if (form.id) {
        await apiPut(`${cfg.apiBase}/${form.id}`, payload);
      } else {
        await apiPost(cfg.apiBase, payload);
      }
      this.showToast(form.id ? `${cfg.entityName} updated.` : `${cfg.entityName} created.`);
      this[`show${M}Modal`] = false;
      this.loading = true;
      await this[`load${K}`]();
      this.loading = false;
    } catch (e) {
      this[`${S}FormError`] = e.message || 'Network error — please try again.';
    } finally {
      this[`saving${M}`] = false;
    }
  };

  mixin[`confirmDelete${M}`] = function (item) {
    this[`delete${M}Target`] = item;
  };

  mixin[`doDelete${M}`] = async function () {
    const target = this[`delete${M}Target`];
    if (!target) return;
    this[`deleting${M}`] = true;
    try {
      await apiDelete(`${cfg.apiBase}/${target.id}`);
      this.showToast(`${cfg.entityName} deleted.`);
      this[`delete${M}Target`] = null;
      this.loading = true;
      await this[`load${K}`]();
      this.loading = false;
    } catch {} finally {
      this[`deleting${M}`] = false;
    }
  };

  return mixin;
}
