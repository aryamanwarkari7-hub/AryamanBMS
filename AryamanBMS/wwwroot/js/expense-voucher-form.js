(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var expenseForm = document.getElementById("expenseForm");
        var categorySelect = document.getElementById("expenseCategorySelect");
        var expensePartyType = document.getElementById("expensePartyType");
        var gstLiabilityType = document.getElementById("gstLiabilityType");

        var vendorSelect = document.getElementById("vendorSelect");
        var vendorNameInput = document.getElementById("vendorNameInput");
        var vendorGstinInput = document.getElementById("vendorGstinInput");
        var vendorStateCodeInput = document.getElementById("vendorStateCodeInput");

        var registeredVendorRow = document.getElementById("registeredVendorRow");
        var vendorSelectGroup = document.getElementById("vendorSelectGroup");
        var vendorGstinGroup = document.getElementById("vendorGstinGroup");
        var gstStateFields = document.getElementById("gstStateFields");

        var amountInput = document.querySelector('input[name="Amount"]');
        var amountInputLabel = document.getElementById("amountInputLabel");
        var amountCardSubtitle = document.getElementById("amountCardSubtitle");
        var gstRateInput = document.getElementById("gstRateInput");
        var gstRateGroup = document.getElementById("gstRateGroup");
        var gstFieldsContainer = document.getElementById("gstFieldsContainer");
        var isInterStateInput = document.getElementById("isInterStateInput");
        var itcEligibleInput = document.getElementById("itcEligibleInput");

        var rcmNoticeBanner = document.getElementById("rcmNoticeBanner");
        var reimbursementNoticeBanner = document.getElementById("reimbursementNoticeBanner");
        var rcmSummaryBox = document.getElementById("rcmSummaryBox");
        var payableSummaryLabel = document.getElementById("payableSummaryLabel");

        var cgstGroup = document.getElementById("cgstGroup");
        var sgstGroup = document.getElementById("sgstGroup");
        var igstGroup = document.getElementById("igstGroup");

        if (!expenseForm || !categorySelect || !gstRateInput) {
            return;
        }

        function setText(id, value) {
            var el = document.getElementById(id);
            if (el) el.textContent = value;
        }

        function calculateAmounts() {
            var amount = parseFloat(amountInput ? amountInput.value : "0") || 0;
            var liability = gstLiabilityType ? gstLiabilityType.value : "Forward Charge";
            var isNoGst = liability === "No GST";
            var isRcm = liability === "Reverse Charge (RCM)";

            if (isNoGst) {
                setText("cgstAmount", "0.00");
                setText("sgstAmount", "0.00");
                setText("igstAmount", "0.00");
                setText("totalGst", "0.00");
                setText("totalAmount", amount.toFixed(2));
                return;
            }

            var gstRate = parseFloat(gstRateInput.value) || 0;
            var isInterstate = isInterStateInput ? isInterStateInput.checked : false;
            var totalGst = (amount * gstRate) / 100;

            if (isInterstate) {
                if (cgstGroup) cgstGroup.style.display = "none";
                if (sgstGroup) sgstGroup.style.display = "none";
                if (igstGroup) igstGroup.style.display = "";
                setText("cgstAmount", "0.00");
                setText("sgstAmount", "0.00");
                setText("igstAmount", totalGst.toFixed(2));
            } else {
                if (cgstGroup) cgstGroup.style.display = "";
                if (sgstGroup) sgstGroup.style.display = "";
                if (igstGroup) igstGroup.style.display = "none";
                var half = totalGst / 2;
                setText("cgstAmount", half.toFixed(2));
                setText("sgstAmount", half.toFixed(2));
                setText("igstAmount", "0.00");
            }

            setText("totalGst", totalGst.toFixed(2));

            if (isRcm) {
                // In RCM, Vendor receives Base Amount only; GST goes to Govt
                setText("totalAmount", amount.toFixed(2));
                setText("rcmGstAmount", totalGst.toFixed(2));
                if (rcmSummaryBox) rcmSummaryBox.style.display = "";
                if (payableSummaryLabel) payableSummaryLabel.textContent = "Vendor Payable (Base Amount):";
            } else {
                // Forward Charge: Vendor receives Base + GST
                setText("totalAmount", (amount + totalGst).toFixed(2));
                if (rcmSummaryBox) rcmSummaryBox.style.display = "none";
                if (payableSummaryLabel) payableSummaryLabel.textContent = "Total Payable to Vendor (Base + GST):";
            }
        }

        function applyRules(isPartyTypeChanged) {
            var party = expensePartyType ? expensePartyType.value : "Registered Vendor";
            var isRegistered = party === "Registered Vendor";

            // Default GST liability when party type changes
            if (isPartyTypeChanged && gstLiabilityType) {
                if (isRegistered) {
                    gstLiabilityType.value = "Forward Charge";
                } else {
                    gstLiabilityType.value = "No GST";
                }
            }

            var liability = gstLiabilityType ? gstLiabilityType.value : "Forward Charge";
            var isNoGst = liability === "No GST";
            var isRcm = liability === "Reverse Charge (RCM)";

            // 1. Vendor Selection row
            if (registeredVendorRow) {
                registeredVendorRow.style.display = isRegistered ? "" : "none";
            }
            if (vendorSelectGroup) {
                vendorSelectGroup.style.display = isRegistered ? "" : "none";
            }

            if (!isRegistered && vendorSelect) {
                vendorSelect.value = "";
            }

            // 2. GSTIN input
            if (vendorGstinInput) {
                vendorGstinInput.readOnly = isRegistered;
                if (isNoGst) {
                    vendorGstinInput.value = "";
                }
            }

            // 3. Notices
            if (rcmNoticeBanner) {
                rcmNoticeBanner.style.display = isRcm ? "" : "none";
            }
            if (reimbursementNoticeBanner) {
                reimbursementNoticeBanner.style.display = (party === "Employee Reimbursement" || party === "Petty Cash") ? "" : "none";
            }

            // 4. GST State fields (Company State, Vendor State, Place of Supply)
            if (gstStateFields) {
                gstStateFields.style.display = isNoGst ? "none" : "";
            }

            // 5. Amount Card Adaptations
            if (isNoGst) {
                if (gstFieldsContainer) gstFieldsContainer.style.display = "none";
                if (gstRateGroup) gstRateGroup.style.display = "none";
                if (amountInputLabel) amountInputLabel.textContent = "Total Expense Amount (₹)";
                if (amountCardSubtitle) amountCardSubtitle.textContent = "Enter total expense amount (No GST applicable).";
                if (gstRateInput) gstRateInput.value = "0";
                if (itcEligibleInput) itcEligibleInput.checked = false;
            } else {
                if (gstFieldsContainer) gstFieldsContainer.style.display = "";
                if (gstRateGroup) gstRateGroup.style.display = "";
                if (amountInputLabel) amountInputLabel.textContent = "Taxable Amount Before GST (₹)";
                if (amountCardSubtitle) amountCardSubtitle.textContent = isRcm
                    ? "Enter taxable base amount. RCM GST will be calculated for deposit to Government."
                    : "Enter taxable amount and GST breakdown.";

                // Pull category default if rate was 0
                if (parseFloat(gstRateInput.value) === 0 && categorySelect) {
                    var selected = categorySelect.options[categorySelect.selectedIndex];
                    if (selected && selected.dataset.gst) {
                        gstRateInput.value = selected.dataset.gst;
                    }
                }
            }

            calculateAmounts();
        }

        // Category change
        categorySelect.addEventListener("change", function () {
            var selected = this.options[this.selectedIndex];
            var classificationSelect = document.querySelector('select[name="ExpenseClassification"]');

            if (!selected || !selected.value) return;

            if (gstLiabilityType && gstLiabilityType.value !== "No GST") {
                gstRateInput.value = selected.dataset.gst || 0;
                if (itcEligibleInput) {
                    itcEligibleInput.checked = selected.dataset.itc === "true";
                }
            }

            if (classificationSelect) {
                classificationSelect.value = selected.dataset.type || "General";
            }

            calculateAmounts();
        });

        // Vendor dropdown change
        if (vendorSelect) {
            vendorSelect.addEventListener("change", function () {
                var selected = this.options[this.selectedIndex];
                if (vendorNameInput) vendorNameInput.value = selected.dataset.name || "";
                if (vendorGstinInput) vendorGstinInput.value = selected.dataset.gstin || "";
                if (vendorStateCodeInput) vendorStateCodeInput.value = selected.dataset.statecode || "";
            });
        }

        // Party type change
        if (expensePartyType) {
            expensePartyType.addEventListener("change", function () {
                applyRules(true);
            });
        }

        // Liability type change
        if (gstLiabilityType) {
            gstLiabilityType.addEventListener("change", function () {
                applyRules(false);
            });
        }

        // Amount & GST rate changes
        expenseForm.addEventListener("input", function (event) {
            if (event.target.name === "Amount" || event.target.name === "GSTRate") {
                calculateAmounts();
            }
        });

        if (isInterStateInput) {
            isInterStateInput.addEventListener("change", calculateAmounts);
        }

        applyRules(false);
    });
})();