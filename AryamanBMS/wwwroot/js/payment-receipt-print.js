(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var printButton = document.getElementById("printButton");
        var backLink = document.getElementById("receiptBackLink");

        if (printButton) {
            printButton.addEventListener("click", function () {
                window.print();
            });
        }

        if (backLink) {
            backLink.addEventListener("click", function (event) {
                event.preventDefault();
                history.back();
            });
        }
    });
})();
