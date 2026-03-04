(() => {
    const tableElement = document.getElementById('remindersTable');
    const participantForm = document.getElementById('participantForm');
    const submitButton = document.getElementById('submitButton');
    const selectAll = document.getElementById('selectAll');

    if (!tableElement || !participantForm || !submitButton || !selectAll) {
        return;
    }

    function getVisibleRowCheckboxes() {
        return Array.from(tableElement.querySelectorAll('tbody tr input.select-row'))
            .filter((checkbox) => checkbox.closest('tr')?.offsetParent !== null);
    }

    function toggleSubmitButton() {
        const anyChecked = getVisibleRowCheckboxes().some((checkbox) => checkbox.checked);
        submitButton.disabled = !anyChecked;
    }

    selectAll.addEventListener('change', () => {
        const visibleCheckboxes = getVisibleRowCheckboxes();
        visibleCheckboxes.forEach((checkbox) => {
            checkbox.checked = selectAll.checked;
        });
        toggleSubmitButton();
    });

    tableElement.addEventListener('change', (event) => {
        if (event.target.matches('input.select-row')) {
            toggleSubmitButton();
        }
    });

    toggleSubmitButton();

    participantForm.addEventListener('submit', (event) => {
        const participantIds = getVisibleRowCheckboxes()
            .filter((checkbox) => checkbox.checked)
            .map((checkbox) => checkbox.dataset.rateeId);

        if (participantIds.length === 0) {
            event.preventDefault();
            window.AppNotify?.warning('No participants selected.');
            return;
        }

        participantForm.querySelectorAll('input[name="participantIds"]').forEach((input) => {
            input.remove();
        });

        participantIds.forEach((id) => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'participantIds';
            input.value = id;
            participantForm.appendChild(input);
        });
    });

    document.body.addEventListener('htmx:afterRequest', (event) => {
        const path = event.detail?.pathInfo?.requestPath || '';
        if (!path.includes('/RemindRateeAjax')) {
            return;
        }

        const trigger = event.detail?.elt;
        const row = trigger?.closest('tr');
        if (!event.detail.successful || !row) {
            window.AppNotify?.error(event.detail?.xhr?.responseText || 'Failed to send reminder.');
            return;
        }

        const lastReminderCell = row.children[5];
        if (lastReminderCell) {
            lastReminderCell.textContent = 'Just now';
        }
        window.AppNotify?.success('Reminder sent.');
    });
})();
