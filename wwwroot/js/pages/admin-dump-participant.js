(() => {
    document.addEventListener('click', (event) => {
        const toggle = event.target.closest('.toggle-table');
        if (!toggle) {
            return;
        }

        event.preventDefault();
        const targetId = toggle.dataset.target;
        if (!targetId) {
            return;
        }

        const container = document.getElementById(`table-${targetId}`);
        if (!container) {
            return;
        }

        const isVisible = container.style.display !== 'none';
        container.style.display = isVisible ? 'none' : 'block';

        const icon = toggle.querySelector('b');
        if (icon) {
            icon.textContent = isVisible ? '[+]' : '[-]';
        }
    });
})();
