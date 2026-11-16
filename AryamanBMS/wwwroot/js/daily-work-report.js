(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var workDetailsBody = document.getElementById("workDetailsBody");
        var nextDayPlansBody = document.getElementById("nextDayPlansBody");
        var btnAddDetail = document.getElementById("btnAddDetail");
        var btnAddPlan = document.getElementById("btnAddPlan");
        var btnImportPrevious = document.getElementById("btnImportPrevious");
        var reportDateInput = document.getElementById("reportDateInput");

        // 1. Recalculate totals
        function recalculateTotals() {
            var plannedSum = 0;
            var actualSum = 0;
            var estimatedSum = 0;

            document.querySelectorAll(".planned-hours-input").forEach(function (inp) {
                plannedSum += parseFloat(inp.value) || 0;
            });
            document.querySelectorAll(".actual-hours-input").forEach(function (inp) {
                actualSum += parseFloat(inp.value) || 0;
            });
            document.querySelectorAll(".estimated-hours-input").forEach(function (inp) {
                estimatedSum += parseFloat(inp.value) || 0;
            });

            var totalPlannedEl = document.getElementById("totalPlannedDisplay");
            var totalActualEl = document.getElementById("totalActualDisplay");
            var totalEstimatedEl = document.getElementById("totalEstimatedDisplay");

            if (totalPlannedEl) totalPlannedEl.textContent = plannedSum.toFixed(2) + " hrs";
            if (totalActualEl) totalActualEl.textContent = actualSum.toFixed(2) + " hrs";
            if (totalEstimatedEl) totalEstimatedEl.textContent = estimatedSum.toFixed(2) + " hrs";
        }

        // 2. Add Row to Today's Work Details
        if (btnAddDetail && workDetailsBody) {
            btnAddDetail.addEventListener("click", function () {
                var index = workDetailsBody.querySelectorAll("tr").length;
                var row = document.createElement("tr");
                row.className = "work-detail-row";
                row.innerHTML = `
                    <td class="text-center align-middle sr-no">${index + 1}</td>
                    <td>
                        <input type="text" name="WorkDetails[${index}].ProjectName" class="app-input app-input-sm" placeholder="Project name" required />
                    </td>
                    <td>
                        <input type="text" name="WorkDetails[${index}].Activity" class="app-input app-input-sm" placeholder="Activity headline" required />
                    </td>
                    <td>
                        <input type="text" name="WorkDetails[${index}].Description" class="app-input app-input-sm col-description" placeholder="Description of work done" />
                    </td>
                    <td>
                        <input type="number" step="0.25" min="0" max="24" name="WorkDetails[${index}].PlannedEffortHours" class="app-input app-input-sm text-end planned-hours-input col-planned" placeholder="0.00" />
                    </td>
                    <td>
                        <input type="number" step="0.25" min="0" max="24" name="WorkDetails[${index}].ActualEffortHours" class="app-input app-input-sm text-end actual-hours-input" placeholder="0.00" required />
                    </td>
                    <td>
                        <input type="text" name="WorkDetails[${index}].Dependencies" class="app-input app-input-sm col-dependencies" placeholder="Dependencies" />
                    </td>
                    <td>
                        <input type="text" name="WorkDetails[${index}].Issues" class="app-input app-input-sm col-issues" placeholder="Blockers or issues" />
                    </td>
                    <td class="text-center align-middle">
                        <button type="button" class="btn-app btn-icon btn-secondary-app btn-delete-row" title="Delete row">
                            <i class="bi bi-trash text-danger"></i>
                        </button>
                    </td>
                `;
                workDetailsBody.appendChild(row);
                applyColumnVisibility();
            });
        }

        // 3. Add Row to Next Day's Plan
        if (btnAddPlan && nextDayPlansBody) {
            btnAddPlan.addEventListener("click", function () {
                var index = nextDayPlansBody.querySelectorAll("tr").length;
                var row = document.createElement("tr");
                row.className = "plan-row";
                row.innerHTML = `
                    <td class="text-center align-middle sr-no">${index + 1}</td>
                    <td>
                        <input type="text" name="NextDayPlans[${index}].ProjectName" class="app-input app-input-sm" placeholder="Project name" required />
                    </td>
                    <td>
                        <input type="text" name="NextDayPlans[${index}].Activity" class="app-input app-input-sm" placeholder="Task name" required />
                    </td>
                    <td>
                        <input type="text" name="NextDayPlans[${index}].Description" class="app-input app-input-sm col-description" placeholder="Plan details" />
                    </td>
                    <td>
                        <input type="number" step="0.25" min="0" max="24" name="NextDayPlans[${index}].EstimatedEffortHours" class="app-input app-input-sm text-end estimated-hours-input" placeholder="0.00" />
                    </td>
                    <td>
                        <select name="NextDayPlans[${index}].Priority" class="app-select app-select-sm">
                            <option value="High">High</option>
                            <option value="Medium" selected>Medium</option>
                            <option value="Low">Low</option>
                        </select>
                    </td>
                    <td>
                        <input type="text" name="NextDayPlans[${index}].Dependencies" class="app-input app-input-sm col-dependencies" placeholder="Dependencies" />
                    </td>
                    <td>
                        <input type="text" name="NextDayPlans[${index}].Issues" class="app-input app-input-sm col-issues" placeholder="Expected risks" />
                    </td>
                    <td class="text-center align-middle">
                        <button type="button" class="btn-app btn-icon btn-secondary-app btn-delete-row" title="Delete row">
                            <i class="bi bi-trash text-danger"></i>
                        </button>
                    </td>
                `;
                nextDayPlansBody.appendChild(row);
                applyColumnVisibility();
            });
        }

        // 4. Delete row handler
        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".btn-delete-row");
            if (btn) {
                var row = btn.closest("tr");
                var tbody = row.parentElement;
                row.remove();

                // Re-index row names and Sr numbers
                // Re-index row names and Sr numbers
                tbody.querySelectorAll("tr").forEach(function (tr, i) {
                    var sr = tr.querySelector(".sr-no");
                    if (sr) sr.textContent = i + 1;

                    tr.querySelectorAll("input, select").forEach(function (input) {
                        var name = input.getAttribute("name");
                        if (name) {
                            input.setAttribute("name", name.replace(/\[\d+\]/, "[" + i + "]"));
                        }
                    });
                });
                recalculateTotals();
            }
        });

        // 5. Input hours recalculation listener
        document.addEventListener("input", function (e) {
            if (e.target.matches(".planned-hours-input, .actual-hours-input, .estimated-hours-input")) {
                recalculateTotals();
            }
        });

        // 6. Import Previous Day's Plans into Today's Work Details
        if (btnImportPrevious && reportDateInput) {
            btnImportPrevious.addEventListener("click", function () {
                var date = reportDateInput.value;
                if (!date) return;

                btnImportPrevious.disabled = true;
                btnImportPrevious.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span> Importing...`;

                fetch(`/DailyWorkReport/GetPreviousPlans?date=${encodeURIComponent(date)}`)
                    .then(r => r.json())
                    .then(data => {
                        if (data.success && data.plans && data.plans.length > 0) {
                            data.plans.forEach(plan => {
                                var index = workDetailsBody.querySelectorAll("tr").length;
                                var row = document.createElement("tr");
                                row.className = "work-detail-row";
                                row.innerHTML = `
                                    <td class="text-center align-middle sr-no">${index + 1}</td>
                                    <td>
                                        <input type="text" name="WorkDetails[${index}].ProjectName" value="${plan.projectName || ''}" class="app-input app-input-sm" required />
                                    </td>
                                    <td>
                                        <input type="text" name="WorkDetails[${index}].Activity" value="${plan.activity || ''}" class="app-input app-input-sm" required />
                                    </td>
                                    <td>
                                        <input type="text" name="WorkDetails[${index}].Description" value="${plan.description || ''}" class="app-input app-input-sm col-description" />
                                    </td>
                                    <td>
                                        <input type="number" step="0.25" min="0" max="24" name="WorkDetails[${index}].PlannedEffortHours" value="${plan.estimatedEffortHours || 0}" class="app-input app-input-sm text-end planned-hours-input col-planned" />
                                    </td>
                                    <td>
                                        <input type="number" step="0.25" min="0" max="24" name="WorkDetails[${index}].ActualEffortHours" class="app-input app-input-sm text-end actual-hours-input" placeholder="0.00" required />
                                    </td>
                                    <td>
                                        <input type="text" name="WorkDetails[${index}].Dependencies" value="${plan.dependencies || ''}" class="app-input app-input-sm col-dependencies" />
                                    </td>
                                    <td>
                                        <input type="text" name="WorkDetails[${index}].Issues" class="app-input app-input-sm col-issues" placeholder="Issues if any" />
                                    </td>
                                    <td class="text-center align-middle">
                                        <button type="button" class="btn-app btn-icon btn-secondary-app btn-delete-row">
                                            <i class="bi bi-trash text-danger"></i>
                                        </button>
                                    </td>
                                `;
                                workDetailsBody.appendChild(row);
                            });
                            recalculateTotals();
                            applyColumnVisibility();
                        } else {
                            alert("No previous planned tasks were found before this date.");
                        }
                    })
                    .catch(() => alert("Error communicating with server."))
                    .finally(() => {
                        btnImportPrevious.disabled = false;
                        btnImportPrevious.innerHTML = `<i class="bi bi-arrow-down-left-square"></i> Import Yesterday's Plan`;
                    });
            });
        }

        // 7. Dynamic Column Customizer (Local Preferences)
        function applyColumnVisibility() {
            var prefs = JSON.parse(localStorage.getItem("bms_dwr_col_prefs") || "{}");
            var colKeys = ["description", "dependencies", "issues", "planned"];

            colKeys.forEach(function (key) {
                var isVisible = prefs[key] !== false; // default true
                document.querySelectorAll(".col-" + key).forEach(function (el) {
                    el.style.display = isVisible ? "" : "none";
                });
                var toggleCheck = document.getElementById("toggleCol_" + key);
                if (toggleCheck) toggleCheck.checked = isVisible;
            });
        }

        document.querySelectorAll(".col-toggle-checkbox").forEach(function (chk) {
            chk.addEventListener("change", function () {
                var key = this.dataset.col;
                var prefs = JSON.parse(localStorage.getItem("bms_dwr_col_prefs") || "{}");
                prefs[key] = this.checked;
                localStorage.setItem("bms_dwr_col_prefs", JSON.stringify(prefs));
                applyColumnVisibility();
            });
        });

        // Initialize state
        applyColumnVisibility();
        recalculateTotals();
    });
})();