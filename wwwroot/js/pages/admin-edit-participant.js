(() => {
    const evaluationSelect = document.getElementById('evaluationSelect');
    if (!evaluationSelect) {
        return;
    }

    const participantId = evaluationSelect.dataset.participantId;
    const ratersUrl = evaluationSelect.dataset.ratersUrl;
    const rateesUrl = evaluationSelect.dataset.rateesUrl;

    function loadEvaluationData() {
        const evalId = evaluationSelect.value;
        if (!evalId) {
            return;
        }

        document.getElementById('ratersContainer').innerHTML = '<div class="loading-panel">Loading raters...</div>';
        document.getElementById('rateesContainer').innerHTML = '<div class="loading-panel">Loading ratees...</div>';

        htmx.ajax('GET', `${ratersUrl}?evaluationId=${encodeURIComponent(evalId)}&participantId=${encodeURIComponent(participantId)}`, {
            target: '#ratersContainer',
            swap: 'innerHTML'
        });

        htmx.ajax('GET', `${rateesUrl}?evaluationId=${encodeURIComponent(evalId)}&participantId=${encodeURIComponent(participantId)}`, {
            target: '#rateesContainer',
            swap: 'innerHTML'
        });
    }

    const activeOption = Array.from(evaluationSelect.options)
        .find((option) => option.text.includes('Active'));
    if (activeOption) {
        evaluationSelect.value = activeOption.value;
    }

    evaluationSelect.addEventListener('change', loadEvaluationData);
    loadEvaluationData();
})();
