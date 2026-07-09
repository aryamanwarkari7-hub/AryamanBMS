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

        if (!expenseForm || !categorySelect || !gstRateInput) {
            return;
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
            var totalAmount = amount + totalGst;

            setText("cgstAmount", cgst.toFixed(2));
            setText("sgstAmount", sgst.toFixed(2));
            setText("totalGst", totalGst.toFixed(2));
            setText("totalAmount", totalAmount.toFixed(2));
        }

        categorySelect.addEventListener("change", function () {
            var selected = this.options[this.selectedIndex];
            var classificationSelect = document.querySelector('select[name="ExpenseClassification"]');

            if (!selected || !selected.value) {
                return;
            }

            gstRateInput.value = selected.dataset.gst || 0;

            if (itcEligibleInput) {
                itcEligibleInput.checked = selected.dataset.itc === "true";
            }

            if (classificationSelect) {
                classificationSelect.value = selected.dataset.type || "General";
            }

            calculateAmounts();
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

        expenseForm.addEventListener("input", function (event) {
            if (event.target.name === "Amount" || event.target.name === "GSTRate") {
                calculateAmounts();
            }
        });

        calculateAmounts();
    });
})();
