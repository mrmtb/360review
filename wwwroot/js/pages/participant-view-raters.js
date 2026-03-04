(() => {
    async function postFormWithAntiForgery(url, payload) {
        const token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content');
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'RequestVerificationToken': token || '',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: new URLSearchParams(payload).toString()
        });

        if (!response.ok) {
            const message = await response.text();
            throw new Error(message || 'Request failed.');
        }
    }

    const modalElement = document.getElementById('addRaterModal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const searchInput = document.getElementById('raterSearchInput');
    const raterList = document.getElementById('raterList');
    const selectedRaterCard = document.getElementById('selectedRaterCard');
    const addSelectedRaterForm = document.getElementById('addSelectedRaterForm');
    const addSelectedRaterButton = document.getElementById('addSelectedRaterButton');
    const selectedRaterEvaluationRateeId = document.getElementById('selectedRaterEvaluationRateeId');
    const selectedRaterParticipantId = document.getElementById('selectedRaterParticipantId');
    const selectedRaterRoleName = document.getElementById('selectedRaterRoleName');
    const showNewRaterFormButton = document.getElementById('showNewRaterFormButton');
    const newRaterForm = document.getElementById('newRaterForm');
    const addNewRaterForm = document.getElementById('addNewRaterForm');
    const newRaterEvaluationRateeId = document.getElementById('newRaterEvaluationRateeId');
    const newRaterRoleInput = document.getElementById('newRaterRoleInput');
    const sendInvitationsForm = document.getElementById('sendInvitationsForm');
    const sendInvitationsButton = document.getElementById('sendInvitationsButton');
    const sendInvitationsPayload = document.getElementById('sendInvitationsPayload');
    const sendInvitationsIsTest = document.getElementById('sendInvitationsIsTest');
    const raterPickerLabel = document.getElementById('raterPickerLabel');
    const modalTitle = document.getElementById('addRaterModalLabel');
    const newRaterRoleName = document.getElementById('newRaterRoleName');

    if (!modalElement || !searchInput || !raterList) {
        return;
    }

    const state = {
        evaluationRateeId: '',
        roleName: '',
        eligibleRaters: [],
        selectedRater: null
    };

    function renderRaters(filterText) {
        const query = (filterText || '').trim().toLowerCase();
        const matches = state.eligibleRaters.filter((rater) => {
            const fullName = `${rater.participantNameFirst} ${rater.participantNameLast}`.toLowerCase();
            const email = (rater.participantEmail || '').toLowerCase();
            return !query || fullName.includes(query) || email.includes(query);
        });

        if (matches.length === 0) {
            raterList.innerHTML = '<div class="list-group-item text-muted">No existing match. Use "Add New Person".</div>';
            return;
        }

        raterList.innerHTML = matches.map((rater, index) => `
            <button type="button" class="list-group-item list-group-item-action js-rater-option" data-index="${index}">
                <div class="fw-semibold">${rater.participantNameFirst} ${rater.participantNameLast}</div>
                <div class="text-muted">${rater.participantEmail}</div>
            </button>
        `).join('');
    }

    function resetModalState() {
        state.eligibleRaters = [];
        state.selectedRater = null;
        searchInput.value = '';
        raterList.innerHTML = '<div class="list-group-item text-muted">Loading eligible raters...</div>';
        selectedRaterCard.classList.add('d-none');
        selectedRaterCard.textContent = '';
        addSelectedRaterButton.disabled = true;
        selectedRaterEvaluationRateeId.value = '';
        selectedRaterParticipantId.value = '';
        selectedRaterRoleName.value = '';
        newRaterForm.classList.add('d-none');
        addNewRaterForm.reset();
        newRaterEvaluationRateeId.value = '';
        newRaterRoleInput.value = '';
    }

    async function loadEligibleRaters(evaluationRateeId) {
        const response = await fetch(`/Participant/GetEligibleRaters/${evaluationRateeId}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        if (!response.ok) {
            throw new Error('Failed to load eligible raters');
        }

        state.eligibleRaters = await response.json();
        renderRaters(searchInput.value);
    }

    function buildInvitationPayload() {
        const participantTuples = [];

        ['Self', 'Manager', 'Peer', 'DirectReport'].forEach((tableId) => {
            const table = document.getElementById(tableId);
            if (!table) {
                return;
            }

            table.querySelectorAll('tbody tr').forEach((row) => {
                const evaluationRateeId = row.dataset.evaluationrateeid;
                const participantId = row.dataset.participantid;
                const statusId = Number(row.dataset.evaluationparticipantstatusid);

                if (statusId === 1 && evaluationRateeId && participantId) {
                    participantTuples.push({ evaluationRateeId, participantId });
                }
            });
        });

        return participantTuples;
    }

    document.querySelectorAll('[data-open-rater-modal="true"]').forEach((button) => {
        button.addEventListener('click', async () => {
            state.evaluationRateeId = button.dataset.evaluationrateeid;
            state.roleName = button.dataset.rolename;
            modalTitle.textContent = `Select ${state.roleName}`;
            raterPickerLabel.textContent = `Search for your ${state.roleName}`;
            newRaterRoleName.textContent = state.roleName;
            resetModalState();
            selectedRaterEvaluationRateeId.value = state.evaluationRateeId;
            selectedRaterRoleName.value = state.roleName;
            newRaterEvaluationRateeId.value = state.evaluationRateeId;
            newRaterRoleInput.value = state.roleName;

            try {
                await loadEligibleRaters(state.evaluationRateeId);
                setTimeout(() => searchInput.focus(), 120);
            } catch (error) {
                raterList.innerHTML = `<div class="list-group-item text-danger">${error.message}</div>`;
            }
        });
    });

    raterList.addEventListener('click', (event) => {
        const option = event.target.closest('.js-rater-option');
        if (!option) {
            return;
        }

        const index = Number(option.dataset.index);
        const filtered = state.eligibleRaters.filter((rater) => {
            const query = searchInput.value.trim().toLowerCase();
            const fullName = `${rater.participantNameFirst} ${rater.participantNameLast}`.toLowerCase();
            const email = (rater.participantEmail || '').toLowerCase();
            return !query || fullName.includes(query) || email.includes(query);
        });

        const selected = filtered[index];
        if (!selected) {
            return;
        }

        state.selectedRater = selected;
        selectedRaterCard.classList.remove('d-none');
        selectedRaterCard.innerHTML = `<strong>Selected:</strong> ${selected.participantNameFirst} ${selected.participantNameLast} (${selected.participantEmail})`;
        addSelectedRaterButton.disabled = false;
        selectedRaterParticipantId.value = selected.participantId;
    });

    searchInput.addEventListener('input', () => renderRaters(searchInput.value));

    addSelectedRaterForm.addEventListener('submit', (event) => {
        if (!state.selectedRater) {
            event.preventDefault();
        }
    });

    showNewRaterFormButton.addEventListener('click', () => {
        newRaterForm.classList.toggle('d-none');
    });

    sendInvitationsForm?.addEventListener('submit', (event) => {
        const participantTuples = buildInvitationPayload();
        if (participantTuples.length === 0) {
            event.preventDefault();
            window.AppNotify?.warning('There are no pending invitations to send.');
            return;
        }
        if (!confirm('Are you sure you want to send invitations to these raters?')) {
            event.preventDefault();
            return;
        }
        sendInvitationsPayload.value = JSON.stringify(participantTuples);
        sendInvitationsIsTest.value = sendInvitationsButton.dataset.isTest === 'true' ? 'true' : 'false';
    });

    document.body.addEventListener('htmx:afterRequest', (event) => {
        const path = event.detail?.pathInfo?.requestPath || '';
        if (!path.startsWith('/Participant/')) {
            return;
        }
        if (event.detail.successful) {
            if (path.includes('/DeleteEvaluationParticipant')) {
                const row = event.detail?.elt?.closest('tr');
                if (row) {
                    const undoPayload = {
                        evaluationRateeId: row.dataset.evaluationrateeid || '',
                        participantId: row.dataset.participantid || '',
                        rolename: row.dataset.rolename || ''
                    };
                    row.remove();
                    window.AppNotify?.action('Rater removed.', 'Undo', async () => {
                        if (!undoPayload.evaluationRateeId || !undoPayload.participantId || !undoPayload.rolename) {
                            throw new Error('Unable to restore this rater.');
                        }
                        await postFormWithAntiForgery('/Participant/AddRater', undoPayload);
                        window.location.reload();
                    }, 'warning', 9000);
                }
                updateInvitationButtonVisibility();
                return;
            }

            if (path.includes('/AddRater') || path.includes('/AddNewRater')) {
                modal.hide();
                window.AppNotify?.success('Rater added.');
                window.location.reload();
                return;
            }

            if (path.includes('/ProcessInvitationsForParticipant')) {
                window.AppNotify?.success('Invitations sent.');
                window.location.reload();
            }
        }
    });

    document.body.addEventListener('htmx:responseError', (event) => {
        const responseText = event.detail?.xhr?.responseText;
        window.AppNotify?.error(responseText || 'Request failed.');
    });

    function updateInvitationButtonVisibility() {
        const hasPendingParticipants = ['Manager', 'Peer', 'DirectReport'].some((tableId) => {
            const table = document.getElementById(tableId);
            if (!table) {
                return false;
            }
            return Array.from(table.querySelectorAll('tbody tr'))
                .some((row) => Number(row.dataset.evaluationparticipantstatusid) === 1);
        });

        if (sendInvitationsButton) {
            sendInvitationsButton.hidden = !hasPendingParticipants;
        }
    }

    updateInvitationButtonVisibility();
})();
