(() => {
    const tableElement = document.getElementById('invitationsTable');
    const processButton = document.getElementById('processInvitationsBtn');

    if (!tableElement || !processButton) {
        return;
    }

    function updateProcessButtonState() {
        const hasRows = tableElement.querySelectorAll('tbody tr').length > 0;
        processButton.disabled = !hasRows;
    }

    document.body.addEventListener('htmx:afterRequest', (event) => {
        const path = event.detail?.pathInfo?.requestPath || '';
        if (!path.includes('/InviteParticipantAjax')) {
            return;
        }

        const trigger = event.detail?.elt;
        const row = trigger?.closest('tr');
        if (!event.detail.successful || !row) {
            window.AppNotify?.error(event.detail?.xhr?.responseText || 'Failed to send invitation.');
            return;
        }

        row.remove();
        updateProcessButtonState();
        window.AppNotify?.success('Invitation sent.');
    });

    updateProcessButtonState();
})();
