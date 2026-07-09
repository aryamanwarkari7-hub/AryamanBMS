(function () {
    function calculateTotal(inputSelector, totalSelector) {
        var total = 0;

        $(inputSelector).each(function () {
            total += parseFloat($(this).val()) || 0;
        });

        $(totalSelector).val(total.toFixed(2));
    }

    $(function () {
        $(".credit-amount").on("keyup change", function () {
            calculateTotal(".credit-amount", "#TotalCredit");
        });

        $(".debit-amount").on("keyup change", function () {
            calculateTotal(".debit-amount", "#TotalDebit");
        });

        if ($("#TotalCredit").length) {
            calculateTotal(".credit-amount", "#TotalCredit");
        }

        if ($("#TotalDebit").length) {
            calculateTotal(".debit-amount", "#TotalDebit");
        }
    });
})();
