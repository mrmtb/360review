(() => {
    const evaluationSelect = document.getElementById('evaluationSelect');
    if (!evaluationSelect) {
        return;
    }

    const participantId = evaluationSelect.dataset.participantId;
    const ratersUrl = evaluationSelect.dataset.ratersUrl;
    const rateesUrl = evaluationSelect.dataset.rateesUrl;
    const movePhaseForm = document.getElementById('movePhaseForm');
    const fromEvaluationInput = document.getElementById('fromEvaluationId');
    const toEvaluationSelect = document.getElementById('toEvaluationId');
    const moveModalElement = document.getElementById('moveRateeConfirmModal');
    const moveFromEvalName = document.getElementById('moveFromEvalName');
    const moveToEvalName = document.getElementById('moveToEvalName');
    const confirmMoveRateeBtn = document.getElementById('confirmMoveRateeBtn');
    const moveModal = moveModalElement && window.bootstrap ? new bootstrap.Modal(moveModalElement) : null;
    let lastRequestId = 0;
    let bypassMoveConfirm = false;

    function syncMoveForm() {
        const currentEvaluationId = evaluationSelect.value;
        if (fromEvaluationInput) {
            fromEvaluationInput.value = currentEvaluationId;
        }

        if (!toEvaluationSelect) {
            return;
        }

        Array.from(toEvaluationSelect.options).forEach((option) => {
            if (!option.value) {
                return;
            }

            option.disabled = option.value === currentEvaluationId;
        });

        if (toEvaluationSelect.value === currentEvaluationId) {
            toEvaluationSelect.value = '';
        }
    }

    function loadEvaluationData() {
        const evalId = evaluationSelect.value;
        if (!evalId) {
            return;
        }

        syncMoveForm();

        const ratersContainer = document.getElementById('ratersContainer');
        const rateesContainer = document.getElementById('rateesContainer');
        if (!ratersContainer || !rateesContainer) {
            return;
        }

        const requestId = ++lastRequestId;
        ratersContainer.innerHTML = '<div class="loading-panel">Loading raters...</div>';
        rateesContainer.innerHTML = '<div class="loading-panel">Loading ratees...</div>';

        const query = `evaluationId=${encodeURIComponent(evalId)}&participantId=${encodeURIComponent(participantId)}`;

        fetch(`${ratersUrl}?${query}`, { credentials: 'same-origin' })
            .then((response) => response.ok ? response.text() : Promise.reject(new Error('Failed to load raters')))
            .then((html) => {
                if (requestId === lastRequestId) {
                    ratersContainer.innerHTML = html;
                }
            })
            .catch(() => {
                if (requestId === lastRequestId) {
                    ratersContainer.innerHTML = '<div class="text-danger small">Unable to load raters.</div>';
                }
            });

        fetch(`${rateesUrl}?${query}`, { credentials: 'same-origin' })
            .then((response) => response.ok ? response.text() : Promise.reject(new Error('Failed to load reviewees')))
            .then((html) => {
                if (requestId === lastRequestId) {
                    rateesContainer.innerHTML = html;
                }
            })
            .catch(() => {
                if (requestId === lastRequestId) {
                    rateesContainer.innerHTML = '<div class="text-danger small">Unable to load assigned reviews.</div>';
                }
            });
    }

    const activeOption = Array.from(evaluationSelect.options)
        .find((option) => option.dataset.isActive === 'true');
    if (activeOption) {
        evaluationSelect.value = activeOption.value;
    }

    evaluationSelect.addEventListener('change', loadEvaluationData);

    if (movePhaseForm) {
        movePhaseForm.addEventListener('submit', (event) => {
            if (bypassMoveConfirm) {
                bypassMoveConfirm = false;
                return;
            }

            if (!toEvaluationSelect || !toEvaluationSelect.value) {
                return;
            }

            if (!moveModal) {
                return;
            }

            event.preventDefault();

            const selectedFrom = evaluationSelect.options[evaluationSelect.selectedIndex];
            const selectedTo = toEvaluationSelect.options[toEvaluationSelect.selectedIndex];

            if (moveFromEvalName) {
                moveFromEvalName.textContent = selectedFrom ? selectedFrom.textContent.trim() : '-';
            }
            if (moveToEvalName) {
                moveToEvalName.textContent = selectedTo ? selectedTo.textContent.trim() : '-';
            }

            moveModal.show();
        });
    }

    if (confirmMoveRateeBtn && movePhaseForm && moveModal) {
        confirmMoveRateeBtn.addEventListener('click', () => {
            moveModal.hide();
            bypassMoveConfirm = true;
            movePhaseForm.requestSubmit();
        });
    }

    syncMoveForm();
    loadEvaluationData();
})();
