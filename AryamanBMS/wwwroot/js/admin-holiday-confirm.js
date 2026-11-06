(function () {
    "use strict";

    // Inject styles for the confirmation modal
    const style = document.createElement("style");
    style.textContent = `
        .admin-holiday-modal-overlay {
            position: fixed;
            inset: 0;
            z-index: 100000;
            display: flex;
            align-items: center;
            justify-content: center;
            background: rgba(15, 23, 42, 0.65);
            backdrop-filter: blur(6px);
            opacity: 0;
            visibility: hidden;
            transition: opacity 0.25s ease, visibility 0.25s ease;
            padding: 20px;
        }
        .admin-holiday-modal-overlay.show {
            opacity: 1;
            visibility: visible;
        }
        .admin-holiday-modal-card {
            background: #ffffff;
            border-radius: 18px;
            max-width: 560px;
            width: 100%;
            padding: 28px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            position: relative;
            transform: scale(0.95);
            transition: transform 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
        }
        .admin-holiday-modal-overlay.show .admin-holiday-modal-card {
            transform: scale(1);
        }
        .admin-holiday-modal-header {
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 16px;
        }
        .admin-holiday-icon-box {
            width: 44px;
            height: 44px;
            border-radius: 12px;
            background: #e0f2fe;
            color: #0284c7;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 22px;
            flex-shrink: 0;
        }
        .admin-holiday-pill {
            display: inline-block;
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            padding: 3px 10px;
            border-radius: 9999px;
            background: #fef3c7;
            color: #b45309;
            margin-bottom: 4px;
        }
        .admin-holiday-title {
            font-size: 19px;
            font-weight: 700;
            color: #0f172a;
            margin: 0;
        }
        .admin-holiday-body {
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            padding: 16px;
            margin-bottom: 20px;
        }
        .admin-holiday-day-badge {
            font-size: 12px;
            font-weight: 600;
            color: #0369a1;
            background: #e0f2fe;
            padding: 4px 10px;
            border-radius: 6px;
            display: inline-block;
            margin-top: 6px;
        }
        .admin-holiday-actions {
            display: flex;
            flex-wrap: no-wrap;
            gap: 10px;
            justify-content: flex-end;
        }
    `;
    document.head.appendChild(style);

    // Build the modal markup
    const modalHtml = `
        <div id="adminHolidayModalOverlay" class="admin-holiday-modal-overlay">
            <div class="admin-holiday-modal-card">
                <div class="admin-holiday-modal-header">
                    <div class="admin-holiday-icon-box">
                        <i class="bi bi-calendar-check"></i>
                    </div>
                    <div>
                        <span class="admin-holiday-pill">Admin Review Required</span>
                        <h3 class="admin-holiday-title">Upcoming Holiday Confirmation</h3>
                    </div>
                </div>

                <div class="admin-holiday-body">
                    <div class="mb-2">
                        <span class="text-muted small">Holiday Name:</span>
                        <div id="adminModalHolidayName" class="fw-bold fs-5 text-dark"></div>
                    </div>
                    <div class="mb-3">
                        <span class="text-muted small">Scheduled Date:</span>
                        <div id="adminModalCurrentDate" class="text-secondary fw-semibold"></div>
                    </div>

                    <div class="border-top pt-2">
                        <label for="adminModalDatePicker" class="form-label small fw-semibold text-dark">
                            Verify or Change Date (if holiday shifted):
                        </label>
                                                <input type="date" id="adminModalDatePicker" class="app-input" />
                        <div id="adminModalSelectedDay" class="admin-holiday-day-badge"></div>
                    </div>
                </div>

                <div class="admin-holiday-actions">
                    <button type="button" id="adminModalBtnCancel" class="btn-app btn-danger-app btn-sm-app">
                        <i class="bi bi-x-circle"></i> Cancel Holiday
                    </button>
                    <button type="button" id="adminModalBtnDismiss" class="btn-app btn-secondary-app btn-sm-app">
                        <i class="bi bi-clock-history"></i> Decide Later
                    </button>
                    <button type="button" id="adminModalBtnConfirm" class="btn-app btn-success-app btn-sm-app">
                        <i class="bi bi-check-circle"></i> Confirm
                    </button>
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML("beforeend", modalHtml);

    const overlay = document.getElementById("adminHolidayModalOverlay");
    const nameEl = document.getElementById("adminModalHolidayName");
    const currentDisplayDateEl = document.getElementById("adminModalCurrentDate");
    const datePicker = document.getElementById("adminModalDatePicker");
    const dayBadge = document.getElementById("adminModalSelectedDay");
    const btnConfirm = document.getElementById("adminModalBtnConfirm");
    const btnCancel = document.getElementById("adminModalBtnCancel");
    const btnDismiss = document.getElementById("adminModalBtnDismiss");

    let currentHolidayId = 0;
    let originalDate = "";

    function updateDayBadge(dateString) {
        if (!dateString) {
            dayBadge.textContent = "";
            return;
        }
        const [y, m, d] = dateString.split("-").map(Number);
        const dt = new Date(y, m - 1, d);
        const dayName = dt.toLocaleDateString("en-US", { weekday: "long" });
        dayBadge.textContent = "Day: " + dayName;

        if (dateString !== originalDate) {
            btnConfirm.innerHTML = `<i class="bi bi-arrow-repeat"></i> Confirm Shifted Date`;
            btnConfirm.className = "btn-app btn-primary-app btn-sm-app";
        } else {
            btnConfirm.innerHTML = `<i class="bi bi-check-circle"></i> Confirm`;
            btnConfirm.className = "btn-app btn-success-app btn-sm-app";
        }
    }

    datePicker.addEventListener("change", function () {
        updateDayBadge(this.value);
    });

    function openModal(id, name, dateIso, displayDate) {
        currentHolidayId = id;
        originalDate = dateIso;
        nameEl.textContent = name;
        currentDisplayDateEl.textContent = displayDate;
        datePicker.value = dateIso;
        updateDayBadge(dateIso);
        overlay.classList.add("show");
    }

    function closeModal() {
        overlay.classList.remove("show");
    }

    btnDismiss.addEventListener("click", closeModal);

    overlay.addEventListener("click", function (e) {
        if (e.target === overlay) closeModal();
    });

    function getVerificationToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    async function sendHolidayAction(action, newDateVal) {
        const formData = new FormData();
        formData.append("HolidayId", currentHolidayId);
        formData.append("Action", action);
        if (newDateVal) {
            formData.append("NewDate", newDateVal);
        }
        formData.append("__RequestVerificationToken", getVerificationToken());

        try {
            btnConfirm.disabled = true;
            btnCancel.disabled = true;

            const res = await fetch("/Holiday/ConfirmOrShiftHoliday", {
                method: "POST",
                body: formData
            });

            const data = await res.json();
            if (data.success) {
                alert(data.message);
                closeModal();
                window.location.reload();
            } else {
                alert(data.message || "An error occurred.");
            }
        } catch (err) {
            alert("Failed to submit holiday confirmation.");
        } finally {
            btnConfirm.disabled = false;
            btnCancel.disabled = false;
        }
    }

    btnConfirm.addEventListener("click", function () {
        const selectedDate = datePicker.value;
        if (!selectedDate) {
            alert("Please choose a valid date.");
            return;
        }
        const action = (selectedDate !== originalDate) ? "shift" : "confirm";
        sendHolidayAction(action, selectedDate);
    });

    btnCancel.addEventListener("click", function () {
        if (confirm("Are you sure you want to cancel this office holiday?")) {
            sendHolidayAction("cancel", null);
        }
    });

    // 1. Auto-show modal on login if 2-day reminder data exists
    const autoData = document.getElementById("adminHolidayConfirmData");
    if (autoData) {
        const id = parseInt(autoData.dataset.id, 10);
        const name = autoData.dataset.name || "Office Holiday";
        const dateIso = autoData.dataset.date || "";
        const displayDate = autoData.dataset.displayDate || dateIso;

        if (id && dateIso) {
            setTimeout(() => {
                openModal(id, name, dateIso, displayDate);
            }, 600);
        }
    }

    // 2. Wire table row buttons on /Holiday/Index
    document.addEventListener("click", function (e) {
        const shiftBtn = e.target.closest(".js-open-shift-modal");
        if (shiftBtn) {
            const id = parseInt(shiftBtn.dataset.id, 10);
            const name = shiftBtn.dataset.name;
            const dateIso = shiftBtn.dataset.date;
            const displayDate = shiftBtn.dataset.displayDate;
            openModal(id, name, dateIso, displayDate);
            return;
        }

        const confirmBtn = e.target.closest(".js-quick-confirm-holiday");
        if (confirmBtn) {
            const id = parseInt(confirmBtn.dataset.id, 10);
            const name = confirmBtn.dataset.name;
            const displayDate = confirmBtn.dataset.date;

            if (confirm(`Confirm holiday '${name}' on ${displayDate}?`)) {
                currentHolidayId = id;
                sendHolidayAction("confirm", null);
            }
        }
    });
})();