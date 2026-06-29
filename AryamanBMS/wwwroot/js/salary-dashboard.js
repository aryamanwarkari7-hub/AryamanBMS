document.addEventListener("DOMContentLoaded", function () {
    const viewType = document.getElementById("viewType");
    const monthFilter = document.getElementById("monthFilter");

    if (!viewType || !monthFilter) {
        return;
    }

    function toggleMonthFilter() {
        monthFilter.style.display =
            viewType.value === "Monthly"
                ? "block"
                : "none";
    }

    viewType.addEventListener("change", toggleMonthFilter);
    toggleMonthFilter();
});