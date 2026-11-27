/**
 * Monaco Editor JS interop for Blazor.
 * Manages multiple editor instances keyed by element ID.
 * Called from BattlePage.razor and QuestionsAdmin.razor via IJSRuntime.
 *
 * IMPORTANT: Monaco is loaded via CDN (cdn.jsdelivr.net).
 * The app requires internet access at runtime for the editor to work.
 */
window.monacoInterop = {
    editors: {},
    failed: false,  // set to true if Monaco CDN fails to load

    /**
     * Initialise a Monaco editor inside the element with the given ID.
     * Safe to call multiple times — disposes existing instance first.
     * Returns true on success, false if Monaco is unavailable.
     * @param {string} elementId  - id attribute of the container div
     * @param {string} language   - Monaco language id, e.g. 'csharp'
     * @param {string} value      - initial code content
     */
    init(elementId, language, value) {
        // If require (Monaco loader) is not defined, the CDN failed
        if (typeof require === 'undefined') {
            this.failed = true;
            const el = document.getElementById(elementId);
            if (el) {
                el.innerHTML = '<div style="padding:1rem;color:#f5a623;background:#1e1e1e;border-radius:4px;font-family:monospace;">Code editor could not load (CDN unreachable). Use the text box below.</div>';
            }
            return false;
        }

        // Dispose any previous instance in this slot
        if (this.editors[elementId]) {
            this.editors[elementId].dispose();
            delete this.editors[elementId];
        }

        require.config({
            paths: { vs: 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.0/min/vs' }
        });

        require(['vs/editor/editor.main'], () => {
            const el = document.getElementById(elementId);
            if (!el) {
                console.warn('monacoInterop.init: element not found:', elementId);
                return;
            }
            this.editors[elementId] = monaco.editor.create(el, {
                value: value || '',
                language: language || 'csharp',
                theme: 'vs-dark',
                minimap: { enabled: false },
                fontSize: 14,
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
                automaticLayout: true,
                wordWrap: 'on'
            });
        });
        return true;
    },

    /**
     * Returns the current content of the editor.
     * Returns empty string if the editor hasn't initialised yet.
     */
    getValue(elementId) {
        return this.editors[elementId]?.getValue() ?? '';
    },

    /**
     * Replaces the editor content programmatically.
     */
    setValue(elementId, value) {
        this.editors[elementId]?.setValue(value ?? '');
    },

    /**
     * Disposes the editor and frees its resources.
     * Call this when the Blazor component is disposed.
     */
    dispose(elementId) {
        if (this.editors[elementId]) {
            this.editors[elementId].dispose();
            delete this.editors[elementId];
        }
    }
};
