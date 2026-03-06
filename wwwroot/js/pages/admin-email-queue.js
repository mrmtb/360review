(() => {
    async function postWithAntiForgery(url) {
        const token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content');
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token || '',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            const message = await response.text();
            throw new Error(message || 'Request failed.');
        }
    }

    document.body.addEventListener('htmx:afterRequest', (event) => {
        const path = event.detail?.pathInfo?.requestPath || '';
        if (!path.includes('/RetryEmailAjax') && !path.includes('/CancelEmailAjax')) {
            return;
        }

        const trigger = event.detail?.elt;
        const row = trigger?.closest('tr');
        if (!event.detail.successful || !row) {
            window.AppNotify?.error(event.detail?.xhr?.responseText || 'Email action failed.');
            return;
        }

        if (path.includes('/CancelEmailAjax')) {
            const retryButton = row.querySelector('.js-email-action[data-action="retry"]');
            const retryUrl = retryButton?.getAttribute('hx-post');
            row.remove();
            window.AppNotify?.action('Email cancelled.', 'Undo', async () => {
                if (!retryUrl) {
                    throw new Error('Unable to restore this email.');
                }
                await postWithAntiForgery(retryUrl);
                window.location.reload();
            }, 'warning', 9000);
            return;
        }

        const statusCell = row.children[2];
        if (statusCell) {
            statusCell.textContent = 'Pending';
        }
        window.AppNotify?.success('Email retry queued.');
    });
})();
