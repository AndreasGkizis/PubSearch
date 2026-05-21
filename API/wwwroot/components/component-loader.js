(function () {
  const includeCache = new Map();
  const loaderScript = document.currentScript;
  const loaderUrl = loaderScript && loaderScript.src
    ? new URL(loaderScript.src, document.baseURI)
    : new URL('components/component-loader.js', document.baseURI);
  const componentsBaseUrl = new URL('./', loaderUrl);

  function resolveIncludeUrl(path) {
    const rawPath = (path || '').trim();
    if (!rawPath) throw new Error('Empty component include path.');

    // Already absolute URL
    if (/^[a-zA-Z][a-zA-Z\d+\-.]*:/.test(rawPath)) {
      return rawPath;
    }

    // Keep includes stable regardless of page route (e.g., /admin/, /index.html, /foo/bar/)
    // by resolving from the loader script's /components/ directory.
    const normalized = rawPath.startsWith('components/')
      ? rawPath.slice('components/'.length)
      : rawPath.replace(/^\.\/+/, '');

    return new URL(normalized, componentsBaseUrl).toString();
  }

  async function fetchInclude(path) {
    const includeUrl = resolveIncludeUrl(path);
    if (includeCache.has(includeUrl)) return includeCache.get(includeUrl);

    const response = await fetch(includeUrl, { cache: 'no-cache' });
    if (!response.ok) {
      throw new Error(
        `Failed to load component include: "${path}" resolved to "${includeUrl}" (status ${response.status}).`
      );
    }

    const html = await response.text();
    includeCache.set(includeUrl, html);
    return html;
  }

  async function includeNode(node) {
    const path = node.getAttribute('data-include');
    if (!path) return;

    const html = await fetchInclude(path);
    const template = document.createElement('template');
    template.innerHTML = html.trim();
    node.replaceWith(template.content.cloneNode(true));
  }

  async function includeAll(root) {
    let includes = Array.from((root || document).querySelectorAll('[data-include]'));
    while (includes.length > 0) {
      for (const node of includes) {
        try {
          await includeNode(node);
        } catch (error) {
          const includePath = node.getAttribute('data-include');
          console.error('Component include failed; using inline fallback.', {
            includePath,
            pageUrl: window.location.href,
            error,
          });
          // Prevent infinite retry loops; keep existing inline fallback content.
          node.removeAttribute('data-include');
          node.setAttribute('data-include-error', 'true');
        }
      }
      includes = Array.from((root || document).querySelectorAll('[data-include]'));
    }
  }

  function applyModeSwitchState(root) {
    const pathname = window.location.pathname.toLowerCase();
    const activeMode = pathname.endsWith('admin.html') ? 'admin' : 'search';
    const activeClasses = ['bg-indigo-600', 'text-white', 'shadow-sm'];
    const inactiveClasses = ['text-gray-500', 'hover:text-gray-700', 'hover:bg-gray-200'];

    const switches = (root || document).querySelectorAll('[data-mode-switch]');
    for (const switchEl of switches) {
      const links = switchEl.querySelectorAll('[data-mode-link]');
      for (const link of links) {
        const linkMode = (link.getAttribute('data-mode-link') || '').toLowerCase();
        const isActive = linkMode === activeMode;

        for (const cls of activeClasses) link.classList.remove(cls);
        for (const cls of inactiveClasses) link.classList.remove(cls);
        link.removeAttribute('aria-current');

        if (isActive) {
          for (const cls of activeClasses) link.classList.add(cls);
          link.setAttribute('aria-current', 'page');
        } else {
          for (const cls of inactiveClasses) link.classList.add(cls);
        }
      }
    }
  }

  async function renderComponents() {
    await includeAll(document);
    applyModeSwitchState(document);
  }

  window.ComponentLoader = {
    includeAll: renderComponents,
    applyModeSwitchState,
  };

  let alpineBootstrapped = false;

  window.deferLoadingAlpine = function (startAlpine) {
    const runInit = async function () {
      if (alpineBootstrapped) return;
      alpineBootstrapped = true;

      try {
        await renderComponents();
      } catch (error) {
        console.error('Failed to render UI components.', {
          error,
          pageUrl: window.location.href,
          includePlaceholders: Array.from(document.querySelectorAll('[data-include]')).map((n) => n.getAttribute('data-include')),
        });
      }
      startAlpine();
    };

    // If DOM is already loaded, run immediately; otherwise wait for DOMContentLoaded
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', function () {
        void runInit();
      }, { once: true });
    } else {
      void runInit();
    }
  };
})();
