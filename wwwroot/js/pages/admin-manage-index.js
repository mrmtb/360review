(() => {
    const modelSelect = document.getElementById('modelSelect');
    const createNewBtn = document.getElementById('createNewBtn');
    const detailsContainer = document.getElementById('detailsContainer');

    if (!modelSelect || !createNewBtn || !detailsContainer) {
        return;
    }

    function loadSelectedTable() {
        detailsContainer.innerHTML = '';
        document.getElementById('manageContainer').innerHTML = '<div class="loading-panel">Loading records...</div>';
        htmx.ajax('GET', '/Admin/' + modelSelect.value, {
            target: '#manageContainer',
            swap: 'innerHTML'
        });
    }

    function initializeDetailChangeTracking() {
        const partialViewContainer = document.getElementById('detailsContainer');
        if (!partialViewContainer || partialViewContainer.innerHTML.trim() === '') {
            return;
        }

        function highlightChangedField(event) {
            const input = event.target;
            const initialInput = document.querySelector('#initial' + input.id);
            if (!initialInput) {
                return;
            }

            if (input.type === 'checkbox') {
                input.classList.toggle('highlight', input.checked.toString() !== initialInput.value);
            } else {
                input.classList.toggle('highlight', input.value !== initialInput.value);
            }
        }

        const inputs = partialViewContainer.querySelectorAll('input, select, textarea');
        inputs.forEach((input) => {
            input.addEventListener('input', highlightChangedField);
            input.addEventListener('change', highlightChangedField);
            highlightChangedField({ target: input });
        });

        const selects = partialViewContainer.querySelectorAll('select[id]');
        selects.forEach((select) => {
            const hiddenInput = partialViewContainer.querySelector('#initial' + select.id);
            if (hiddenInput) {
                select.value = hiddenInput.value;
            }
        });

        const firstInput = partialViewContainer.querySelector('input:not([type="hidden"]), select, textarea');
        firstInput?.focus();
    }

    modelSelect.addEventListener('change', loadSelectedTable);

    createNewBtn.addEventListener('click', () => {
        detailsContainer.innerHTML = '<div class="loading-panel">Loading editor...</div>';
        htmx.ajax('GET', '/Admin/CreateItem?selectedModel=' + encodeURIComponent(modelSelect.value), {
            target: '#detailsContainer',
            swap: 'innerHTML'
        });
    });

    detailsContainer.addEventListener('click', (event) => {
        const cancelButton = event.target.closest('.cancel-details');
        if (!cancelButton) {
            return;
        }

        event.preventDefault();
        detailsContainer.innerHTML = '';
    });

    document.body.addEventListener('htmx:afterSwap', (event) => {
        if (event.detail && event.detail.target && event.detail.target.id === 'detailsContainer') {
            initializeDetailChangeTracking();
        }
    });

    loadSelectedTable();
})();
