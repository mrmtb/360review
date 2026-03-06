(() => {
    const table = window.AppUI?.initDataTable({
        selector: '#peopleTable',
        pageLength: 25,
        saveState: true,
        order: [[3, 'desc'], [1, 'desc'], [0, 'asc']],
        columnDefs: [
            { targets: 6, orderable: false },
            { type: 'date', targets: [4, 5] }
        ],
        searchPlaceholder: 'Search people...',
        momentFormat: 'M/D/YYYY LT',
        momentLocale: 'en'
    });

    const filterContainer = document.getElementById('peopleQuickFilters');
    if (!table || !filterContainer) {
        return;
    }

    const buttons = Array.from(filterContainer.querySelectorAll('[data-filter]'));
    const columnIndexes = {
        activeEval: 1,
        ratee: 2,
        openItems: 3
    };

    function clearFlagFilters() {
        table.column(columnIndexes.activeEval).search('');
        table.column(columnIndexes.ratee).search('');
        table.column(columnIndexes.openItems).search('');
    }

    function activateButton(activeButton) {
        buttons.forEach((button) => {
            button.classList.toggle('active', button === activeButton);
            button.classList.toggle('btn-secondary', button === activeButton);
            button.classList.toggle('btn-outline-secondary', button !== activeButton);
        });
    }

    filterContainer.addEventListener('click', (event) => {
        const button = event.target.closest('[data-filter]');
        if (!button) {
            return;
        }

        clearFlagFilters();
        const filter = button.dataset.filter;

        if (filter === 'ratee') {
            table.column(columnIndexes.ratee).search('true');
        } else if (filter === 'active-eval') {
            table.column(columnIndexes.activeEval).search('true');
        } else if (filter === 'open-items') {
            table.column(columnIndexes.openItems).search('true');
        }

        table.draw();
        activateButton(button);
    });
})();
