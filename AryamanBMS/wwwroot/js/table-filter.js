(function () {
    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("[data-table-filter-target]").forEach(function (input) {
            var targetSelector = input.dataset.tableFilterTarget;
            var rows = document.querySelectorAll(targetSelector + " tbody tr");

            input.addEventListener("keyup", function () {
                var value = input.value.toLowerCase();

                rows.forEach(function (row) {
                    row.style.display = row.innerText.toLowerCase().includes(value)
                        ? ""
                        : "none";
                });
            });
        });
    });
})();
