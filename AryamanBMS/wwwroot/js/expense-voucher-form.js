(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var expenseForm = document.getElementById("expenseForm");
        var categorySelect = document.getElementById("expenseCategorySelect");
        var gstRateInput = document.getElementById("gstRateInput");
        var itcEligibleInput = document.getElementById("itcEligibleInput");
        var vendorSelect = document.getElementById("vendorSelect");
        var vendorNameInput = document.getElementById("vendorNameInput");
        var vendorGstinInput = document.querySelector('input[name="VendorGSTIN"]');
        var vendorStateCodeInput = document.getElementById("vendorStateCodeInput");
        var expensePartyType = document.getElementById("expensePartyType");
        var vendorGroup = vendorSelect ? vendorSelect.closest(".form-group") : null;

        if (!expenseForm || !categorySelect || !gstRateInput) {
            return;
        }

        function isRegisteredVendor() {
            return !expensePartyType || expensePartyType.value === "Registered Vendor";
        }

        function setText(id, value) {
            var element = document.getElementById(id);
            if (element) {
                element.textContent = value;
            }
        }

        function calculateAmounts() {
            var amountInput = document.querySelector('input[name="Amount"]');
            var amount = parseFloat(amountInput ? amountInput.value : "0") || 0;
            var gstRate = parseFloat(gstRateInput.value) || 0;

            if (gstRate === 0) {
                setText("cgstAmount", "0.00");
                setText("sgstAmount", "0.00");
                setText("totalGst", "0.00");
                setText("totalAmount", amount.toFixed(2));
                return;
            }

            var totalGst = (amount * gstRate) / 100;
            var cgst = totalGst / 2;
            var sgst = totalGst / 2;

            setText("cgstAmount", cgst.toFixed(2));
            setText("sgstAmount", sgst.toFixed(2));
            setText("totalGst", totalGst.toFixed(2));
            setText("totalAmount", (amount + totalGst).toFixed(2));
        }

        function applyPartyTypeRules() {
            var isRegistered = isRegisteredVendor();

            if (vendorGroup) {
                vendorGroup.style.display = isRegistered ? "" : "none";
            }

            if (!isRegistered && vendorSelect) {
                vendorSelect.value = "";
            }

            if (vendorGstinInput) {
                vendorGstinInput.readOnly = !isRegistered;
                if (!isRegistered) {
                    vendorGstinInput.value = "";
                }
            }

            if (gstRateInput) {
                gstRateInput.readOnly = !isRegistered;
                if (!isRegistered) {
                    gstRateInput.value = "0";
                }
            }

            if (itcEligibleInput) {
                itcEligibleInput.disabled = !isRegistered;
                if (!isRegistered) {
                    itcEligibleInput.checked = false;
                }
            }

            calculateAmounts();
        }

        categorySelect.addEventListener("change", function () {
            var selected = this.options[this.selectedIndex];
            var classificationSelect = document.querySelector('select[name="ExpenseClassification"]');

            if (!selected || !selected.value) {
                return;
            }

            if (isRegisteredVendor()) {
                gstRateInput.value = selected.dataset.gst || 0;

                if (itcEligibleInput) {
                    itcEligibleInput.checked = selected.dataset.itc === "true";
                }
            }

            if (classificationSelect) {
                classificationSelect.value = selected.dataset.type || "General";
            }

            applyPartyTypeRules();
        });

        if (vendorSelect) {
            vendorSelect.addEventListener("change", function () {
                var selected = this.options[this.selectedIndex];

                if (vendorNameInput) {
                    vendorNameInput.value = selected.dataset.name || "";
                }

                if (vendorGstinInput) {
                    vendorGstinInput.value = selected.dataset.gstin || "";
                }

                if (vendorStateCodeInput) {
                    vendorStateCodeInput.value = selected.dataset.statecode || "";
                }
            });
        }

        if (expensePartyType) {
            expensePartyType.addEventListener("change", applyPartyTypeRules);
        }

        expenseForm.addEventListener("input", function (event) {
            if (event.target.name === "Amount" || event.target.name === "GSTRate") {
                calculateAmounts();
            }
        });

        applyPartyTypeRules();
    });
})();