
// LEAVE TYPE
document.addEventListener("DOMContentLoaded", function () {
    const checkbox = document.getElementById("IsCarryForward");
    const group = document.getElementById("maximumCarryForwardGroup");
    const input = document.getElementById("MaximumCarryForwardDays");

    if (!checkbox || !group || !input) {
        return;
    }

    function toggleMaximumCarryForward() {
        const enabled = checkbox.checked;

        group.style.display = enabled ? "block" : "none";

        if (!enabled) {
            input.value = "0";
        }
    }

    checkbox.addEventListener("change", toggleMaximumCarryForward);

    toggleMaximumCarryForward();
});