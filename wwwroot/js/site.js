(() => {
    'use strict';

    const modalOpeners = new WeakMap();

    function getFocusableElements(container) {
        if (!container) {
            return [];
        }

        return Array.from(container.querySelectorAll(
            'a[href], button:not([disabled]), textarea:not([disabled]), input:not([type="hidden"]):not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )).filter((element) => !element.hasAttribute('disabled') && element.offsetParent !== null);
    }

    function focusFirstInteractive(container) {
        const target = container?.querySelector('[data-autofocus], [autofocus]') || getFocusableElements(container)[0];
        if (target && typeof target.focus === 'function') {
            target.focus();
        }
    }

    function setFormBusyState(form, busy, submitter = null) {
        const submitButtons = Array.from(form.querySelectorAll('button[type="submit"], input[type="submit"]'));
        submitButtons.forEach((button) => {
            if (!button.dataset.wasDisabled) {
                button.dataset.wasDisabled = button.disabled ? 'true' : 'false';
            }

            if (busy) {
                button.disabled = true;
                return;
            }

            const wasDisabled = button.dataset.wasDisabled === 'true';
            button.disabled = wasDisabled;
            delete button.dataset.wasDisabled;
        });

        if (!(submitter instanceof HTMLElement)) {
            return;
        }

        if (busy) {
            const busyText = submitter.getAttribute('data-busy-text') || form.getAttribute('data-busy-text') || 'Working...';
            if (submitter.tagName === 'INPUT') {
                if (!submitter.dataset.originalValue) {
                    submitter.dataset.originalValue = submitter.value;
                }
                submitter.value = busyText;
            } else {
                if (!submitter.dataset.originalHtml) {
                    submitter.dataset.originalHtml = submitter.innerHTML;
                }
                submitter.innerHTML = `<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>${busyText}`;
            }
            submitter.classList.add('is-busy');
            submitter.setAttribute('aria-busy', 'true');
            return;
        }

        if (submitter.dataset.originalHtml) {
            submitter.innerHTML = submitter.dataset.originalHtml;
            delete submitter.dataset.originalHtml;
        }
        if (submitter.dataset.originalValue) {
            submitter.value = submitter.dataset.originalValue;
            delete submitter.dataset.originalValue;
        }
        submitter.classList.remove('is-busy');
        submitter.removeAttribute('aria-busy');
    }

    const storageKey = 'theme';
    const root = document.documentElement;

    const getStoredTheme = () => localStorage.getItem(storageKey);
    const setStoredTheme = (theme) => localStorage.setItem(storageKey, theme);

    const prefersDark = () => window.matchMedia('(prefers-color-scheme: dark)').matches;

    const resolveTheme = () => {
        const stored = getStoredTheme();
        if (stored === 'light' || stored === 'dark') {
            return stored;
        }
        return prefersDark() ? 'dark' : 'light';
    };

    const applyTheme = (theme) => {
        root.setAttribute('data-bs-theme', theme);
    };

    const toggleTheme = () => {
        const nextTheme = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        applyTheme(nextTheme);
        setStoredTheme(nextTheme);
    };

    applyTheme(resolveTheme());

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        if (!getStoredTheme()) {
            applyTheme(resolveTheme());
        }
    });

    window.addEventListener('DOMContentLoaded', () => {
        const toggle = document.getElementById('themeToggle');
        if (!toggle) {
            return;
        }

        toggle.addEventListener('click', toggleTheme);
    });
})();

(() => {
    'use strict';

    const modalOpeners = new WeakMap();

    function getFocusableElements(container) {
        if (!container) {
            return [];
        }

        return Array.from(container.querySelectorAll(
            'a[href], button:not([disabled]), textarea:not([disabled]), input:not([type="hidden"]):not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )).filter((element) => !element.hasAttribute('disabled') && element.offsetParent !== null);
    }

    function focusFirstInteractive(container) {
        const target = container?.querySelector('[data-autofocus], [autofocus]') || getFocusableElements(container)[0];
        if (target && typeof target.focus === 'function') {
            target.focus();
        }
    }

    function setFormBusyState(form, busy, submitter = null) {
        const submitButtons = Array.from(form.querySelectorAll('button[type="submit"], input[type="submit"]'));
        submitButtons.forEach((button) => {
            if (!button.dataset.wasDisabled) {
                button.dataset.wasDisabled = button.disabled ? 'true' : 'false';
            }

            if (busy) {
                button.disabled = true;
                return;
            }

            const wasDisabled = button.dataset.wasDisabled === 'true';
            button.disabled = wasDisabled;
            delete button.dataset.wasDisabled;
        });

        if (!(submitter instanceof HTMLElement)) {
            return;
        }

        if (busy) {
            const busyText = submitter.getAttribute('data-busy-text') || form.getAttribute('data-busy-text') || 'Working...';
            if (submitter.tagName === 'INPUT') {
                if (!submitter.dataset.originalValue) {
                    submitter.dataset.originalValue = submitter.value;
                }
                submitter.value = busyText;
            } else {
                if (!submitter.dataset.originalHtml) {
                    submitter.dataset.originalHtml = submitter.innerHTML;
                }
                submitter.innerHTML = `<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>${busyText}`;
            }
            submitter.classList.add('is-busy');
            submitter.setAttribute('aria-busy', 'true');
            return;
        }

        if (submitter.dataset.originalHtml) {
            submitter.innerHTML = submitter.dataset.originalHtml;
            delete submitter.dataset.originalHtml;
        }
        if (submitter.dataset.originalValue) {
            submitter.value = submitter.dataset.originalValue;
            delete submitter.dataset.originalValue;
        }
        submitter.classList.remove('is-busy');
        submitter.removeAttribute('aria-busy');
    }

    function getToastContainer() {
        let container = document.getElementById('appToastStack');
        if (container) {
            container.classList.remove('top-0');
            container.style.top = `calc(var(--app-topbar-offset, 76px) + 8px)`;
            container.style.right = '0.75rem';
            container.style.zIndex = '2000';
            return container;
        }

        container = document.createElement('div');
        container.id = 'appToastStack';
        container.className = 'toast-container position-fixed end-0 p-3';
        container.style.top = `calc(var(--app-topbar-offset, 76px) + 8px)`;
        container.style.right = '0.75rem';
        container.style.zIndex = '2000';
        document.body.appendChild(container);
        return container;
    }

    function showToast(message, variant = 'info', delay = 3200) {
        if (!message) {
            return;
        }

        if (typeof bootstrap === 'undefined' || !bootstrap.Toast) {
            // Fallback for views that don't load Bootstrap JS.
            window.alert(message);
            return;
        }

        const styles = {
            success: 'bg-success text-white',
            error: 'bg-danger text-white',
            warning: 'bg-warning text-dark',
            info: 'bg-primary text-white'
        };

        const headerClass = styles[variant] || styles.info;
        const container = getToastContainer();
        const wrapper = document.createElement('div');

        wrapper.className = 'toast';
        wrapper.setAttribute('role', 'alert');
        wrapper.setAttribute('aria-live', 'assertive');
        wrapper.setAttribute('aria-atomic', 'true');
        wrapper.innerHTML = `
            <div class="toast-header ${headerClass}">
                <strong class="me-auto text-capitalize">${variant === 'error' ? 'Error' : variant}</strong>
                <button type="button" class="btn-close ${variant === 'warning' ? '' : 'btn-close-white'}" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">${message}</div>
        `;

        container.appendChild(wrapper);
        const toast = new bootstrap.Toast(wrapper, { delay });
        wrapper.addEventListener('hidden.bs.toast', () => wrapper.remove());
        toast.show();
    }

    function showActionToast(message, options = {}) {
        if (!message) {
            return null;
        }

        const {
            variant = 'warning',
            delay = 8000,
            actionLabel = '',
            onAction = null
        } = options;

        if (typeof bootstrap === 'undefined' || !bootstrap.Toast) {
            if (actionLabel && typeof onAction === 'function' && window.confirm(`${message}\n\n${actionLabel}?`)) {
                onAction();
            } else {
                window.alert(message);
            }
            return null;
        }

        const styles = {
            success: 'bg-success text-white',
            error: 'bg-danger text-white',
            warning: 'bg-warning text-dark',
            info: 'bg-primary text-white'
        };

        const headerClass = styles[variant] || styles.warning;
        const container = getToastContainer();
        const wrapper = document.createElement('div');
        const actionButtonHtml = actionLabel
            ? `<button type="button" class="btn btn-sm btn-outline-light ms-2 js-toast-action">${actionLabel}</button>`
            : '';

        wrapper.className = 'toast';
        wrapper.setAttribute('role', 'alert');
        wrapper.setAttribute('aria-live', 'assertive');
        wrapper.setAttribute('aria-atomic', 'true');
        wrapper.innerHTML = `
            <div class="toast-header ${headerClass}">
                <strong class="me-auto text-capitalize">${variant === 'error' ? 'Error' : variant}</strong>
                <button type="button" class="btn-close ${variant === 'warning' ? '' : 'btn-close-white'}" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body d-flex align-items-center justify-content-between gap-2">
                <span>${message}</span>
                ${actionButtonHtml}
            </div>
        `;

        container.appendChild(wrapper);
        const toast = new bootstrap.Toast(wrapper, { delay });
        wrapper.addEventListener('hidden.bs.toast', () => wrapper.remove());

        const actionButton = wrapper.querySelector('.js-toast-action');
        if (actionButton && typeof onAction === 'function') {
            actionButton.addEventListener('click', async () => {
                actionButton.disabled = true;
                try {
                    await onAction();
                    toast.hide();
                } catch (error) {
                    window.AppNotify?.error(error?.message || 'Undo failed.');
                    actionButton.disabled = false;
                }
            });
        }

        toast.show();
        return toast;
    }

    function getFilterPresetStorageKey(selector) {
        return `table-filter-presets:${window.location.pathname}:${selector}`;
    }

    function readFilterPresets(selector) {
        const key = getFilterPresetStorageKey(selector);
        try {
            const parsed = JSON.parse(localStorage.getItem(key) || '[]');
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    function writeFilterPresets(selector, presets) {
        const key = getFilterPresetStorageKey(selector);
        localStorage.setItem(key, JSON.stringify(presets.slice(0, 8)));
    }

    function initTableFilterPresets(options) {
        const { selector, table, searchPlaceholder = '' } = options || {};
        if (!selector || !table) {
            return;
        }

        const tableElement = document.querySelector(selector);
        if (!tableElement || tableElement.dataset.filterPresetsInit === 'true') {
            return;
        }

        tableElement.dataset.filterPresetsInit = 'true';
        const host = tableElement.parentElement;
        if (!host) {
            return;
        }

        const wrapper = document.createElement('div');
        wrapper.className = 'table-filter-presets';
        wrapper.innerHTML = `
            <input type="text" class="form-control form-control-sm table-filter-presets-input" placeholder="Save current filter as..." aria-label="Filter preset name">
            <button type="button" class="btn btn-sm btn-outline-secondary">Save Filter</button>
            <div class="filter-chip-list" aria-label="Saved filters"></div>
        `;

        host.insertAdjacentElement('beforebegin', wrapper);

        const nameInput = wrapper.querySelector('input');
        const saveButton = wrapper.querySelector('button');
        const chipList = wrapper.querySelector('.filter-chip-list');

        function getCurrentSearch() {
            const value = table.search ? table.search() : '';
            return (value || '').trim();
        }

        function applyPreset(preset) {
            if (!preset || typeof preset.query !== 'string') {
                return;
            }
            table.search(preset.query).draw();
            const dataTableInput = tableElement.closest('.dt-container, .dataTables_wrapper')?.querySelector('input[type="search"]');
            if (dataTableInput) {
                dataTableInput.value = preset.query;
            }
        }

        function renderPresets() {
            const presets = readFilterPresets(selector);
            if (presets.length === 0) {
                chipList.innerHTML = `<span class="small text-muted">${searchPlaceholder || 'Save common table searches for faster reuse.'}</span>`;
                return;
            }

            chipList.innerHTML = presets.map((preset) => `
                <span class="filter-chip">
                    <button type="button" class="js-apply-filter-chip" data-id="${preset.id}" title="Apply filter">${preset.name}</button>
                    <button type="button" class="js-remove-filter-chip" data-id="${preset.id}" aria-label="Remove ${preset.name}">&times;</button>
                </span>
            `).join('');
        }

        saveButton.addEventListener('click', () => {
            const name = (nameInput.value || '').trim();
            const query = getCurrentSearch();
            if (!name) {
                window.AppNotify?.warning('Enter a short name for this filter.');
                nameInput.focus();
                return;
            }
            if (!query) {
                window.AppNotify?.warning('Apply a table search before saving a filter.');
                return;
            }

            const presets = readFilterPresets(selector).filter((preset) => preset.name.toLowerCase() !== name.toLowerCase());
            presets.unshift({
                id: `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
                name,
                query
            });
            writeFilterPresets(selector, presets);
            nameInput.value = '';
            renderPresets();
            window.AppNotify?.success('Filter saved.');
        });

        wrapper.addEventListener('click', (event) => {
            const applyButton = event.target.closest('.js-apply-filter-chip');
            if (applyButton) {
                const presets = readFilterPresets(selector);
                const preset = presets.find((item) => item.id === applyButton.dataset.id);
                applyPreset(preset);
                return;
            }

            const removeButton = event.target.closest('.js-remove-filter-chip');
            if (!removeButton) {
                return;
            }

            const presets = readFilterPresets(selector).filter((item) => item.id !== removeButton.dataset.id);
            writeFilterPresets(selector, presets);
            renderPresets();
        });

        renderPresets();
    }

    function initCommandPalette() {
        const modalElement = document.getElementById('commandPaletteModal');
        const toggleButton = document.getElementById('commandPaletteToggle');
        const input = document.getElementById('commandPaletteInput');
        const list = document.getElementById('commandPaletteList');

        if (!modalElement || !input || !list || typeof bootstrap === 'undefined') {
            return;
        }

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
        let commands = [];
        let activeIndex = 0;

        function collectCommands() {
            const links = Array.from(document.querySelectorAll('.app-nav a.nav-link[href]'));
            const map = new Map();
            links.forEach((link) => {
                const href = link.getAttribute('href');
                const label = link.textContent?.trim().replace(/\s+/g, ' ');
                if (!href || !label || map.has(href)) {
                    return;
                }

                map.set(href, { href, label });
            });
            commands = Array.from(map.values());
        }

        function renderCommands(filterText = '') {
            const query = filterText.trim().toLowerCase();
            const visible = commands.filter((command) => !query || command.label.toLowerCase().includes(query));
            if (visible.length === 0) {
                list.innerHTML = '<div class="list-group-item text-muted">No matching actions.</div>';
                return [];
            }

            list.innerHTML = visible.map((command, index) => `
                <button type="button" class="list-group-item list-group-item-action command-palette-item ${index === activeIndex ? 'active' : ''}" data-index="${index}" data-href="${command.href}">
                    ${command.label}
                </button>
            `).join('');
            return visible;
        }

        function openPalette() {
            collectCommands();
            activeIndex = 0;
            input.value = '';
            renderCommands('');
            modal.show();
            setTimeout(() => input.focus(), 80);
        }

        toggleButton?.addEventListener('click', openPalette);

        input.addEventListener('input', () => {
            activeIndex = 0;
            renderCommands(input.value);
        });

        input.addEventListener('keydown', (event) => {
            const items = Array.from(list.querySelectorAll('.command-palette-item'));
            if (items.length === 0) {
                return;
            }

            if (event.key === 'ArrowDown') {
                event.preventDefault();
                activeIndex = (activeIndex + 1) % items.length;
                renderCommands(input.value);
                return;
            }

            if (event.key === 'ArrowUp') {
                event.preventDefault();
                activeIndex = (activeIndex - 1 + items.length) % items.length;
                renderCommands(input.value);
                return;
            }

            if (event.key === 'Enter') {
                event.preventDefault();
                const item = items[activeIndex];
                const href = item?.dataset.href;
                if (href) {
                    window.location.href = href;
                }
            }
        });

        list.addEventListener('click', (event) => {
            const button = event.target.closest('.command-palette-item');
            if (!button) {
                return;
            }
            const href = button.dataset.href;
            if (href) {
                window.location.href = href;
            }
        });

        document.addEventListener('keydown', (event) => {
            const tagName = document.activeElement?.tagName;
            const isTextInput = tagName === 'INPUT' || tagName === 'TEXTAREA' || document.activeElement?.isContentEditable;
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
                event.preventDefault();
                openPalette();
                return;
            }

            if (!isTextInput && event.key === '/' && !event.metaKey && !event.ctrlKey && !event.altKey) {
                event.preventDefault();
                openPalette();
            }
        });
    }

    function initDataTable(options) {
        const {
            selector,
            pageLength = 25,
            saveState = false,
            order = [[0, 'asc']],
            columnDefs = [],
            searchPlaceholder = '',
            momentFormat = null,
            momentLocale = 'en'
        } = options || {};

        if (!selector || typeof DataTable === 'undefined') {
            return null;
        }

        if (momentFormat && window.jQuery && $.fn?.dataTable?.moment) {
            $.fn.dataTable.moment(momentFormat, momentLocale);
        }

        return new DataTable(selector, {
            responsive: true,
            pageLength,
            saveState,
            order,
            columnDefs,
            language: {
                search: '_INPUT_',
                searchPlaceholder
            }
        });
    }

    window.AppNotify = {
        success: (message, delay) => showToast(message, 'success', delay),
        error: (message, delay) => showToast(message, 'error', delay),
        warning: (message, delay) => showToast(message, 'warning', delay),
        info: (message, delay) => showToast(message, 'info', delay),
        action: (message, actionLabel, onAction, variant = 'warning', delay = 8000) => showActionToast(message, { actionLabel, onAction, variant, delay })
    };

    window.AppUI = {
        initDataTable,
        initTableFilterPresets
    };

    document.body.addEventListener('htmx:configRequest', (event) => {
        const method = (event.detail.verb || '').toUpperCase();
        if (method !== 'POST' && method !== 'PUT' && method !== 'PATCH' && method !== 'DELETE') {
            return;
        }

        const token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content');
        if (token) {
            event.detail.headers['RequestVerificationToken'] = token;
        }
        
        // Add X-Requested-With header so ASP.NET Core controllers recognize HTMX as AJAX
        event.detail.headers['X-Requested-With'] = 'XMLHttpRequest';
    });

    document.addEventListener('click', (event) => {
        const trigger = event.target.closest('[data-confirm]');
        if (!trigger) {
            return;
        }

        const message = trigger.getAttribute('data-confirm');
        if (message && !window.confirm(message)) {
            event.preventDefault();
            event.stopPropagation();
        }
    });

    document.addEventListener('click', (event) => {
        const modalTrigger = event.target.closest('[data-bs-toggle="modal"][data-bs-target], [data-bs-toggle="modal"][href^="#"]');
        if (!modalTrigger) {
            return;
        }

        const targetSelector = modalTrigger.getAttribute('data-bs-target') || modalTrigger.getAttribute('href');
        if (!targetSelector || !targetSelector.startsWith('#')) {
            return;
        }

        const modal = document.querySelector(targetSelector);
        if (modal) {
            modalOpeners.set(modal, modalTrigger);
        }
    });

    document.addEventListener('submit', (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches('[data-busy-submit]')) {
            return;
        }

        if (form.dataset.submitting === 'true') {
            event.preventDefault();
            return;
        }

        form.dataset.submitting = 'true';
        setFormBusyState(form, true, event.submitter);
    });

    document.body.addEventListener('htmx:afterRequest', (event) => {
        const form = event.detail?.elt?.closest?.('form[data-busy-submit]');
        if (!form) {
            return;
        }

        setFormBusyState(form, false, form.querySelector('.is-busy'));
        form.dataset.submitting = 'false';
    });

    document.body.addEventListener('htmx:afterSwap', (event) => {
        const target = event.detail?.target;
        if (!(target instanceof HTMLElement) || !target.hasAttribute('data-autofocus-scope')) {
            return;
        }

        focusFirstInteractive(target);
    });

    document.addEventListener('shown.bs.modal', (event) => {
        focusFirstInteractive(event.target);
    });

    document.addEventListener('hidden.bs.modal', (event) => {
        const opener = modalOpeners.get(event.target);
        if (opener && document.contains(opener)) {
            opener.focus();
        }
    });

    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Tab') {
            return;
        }

        const modal = event.target instanceof HTMLElement ? event.target.closest('.modal.show') : null;
        if (!modal) {
            return;
        }

        const focusables = getFocusableElements(modal);
        if (focusables.length === 0) {
            return;
        }

        const first = focusables[0];
        const last = focusables[focusables.length - 1];
        const active = document.activeElement;

        if (event.shiftKey && active === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && active === last) {
            event.preventDefault();
            first.focus();
        }
    });

    window.addEventListener('pageshow', () => {
        document.querySelectorAll('form[data-busy-submit]').forEach((form) => {
            setFormBusyState(form, false, form.querySelector('.is-busy'));
            form.dataset.submitting = 'false';
        });
    });

    document.addEventListener('DOMContentLoaded', () => {
        const topbar = document.querySelector('.app-topbar');
        if (topbar) {
            const setTopbarOffset = () => {
                const offset = Math.ceil(topbar.getBoundingClientRect().height) + 8;
                document.documentElement.style.setProperty('--app-topbar-offset', `${offset}px`);
            };
            setTopbarOffset();
            window.addEventListener('resize', setTopbarOffset);
        }

        initCommandPalette();
    });
})();
