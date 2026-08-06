
// LEAVE TYPE
document.addEventListener("DOMContentLoaded", function () {
    const checkbox = document.getElementById("IsCarryForward");
    const group = document.getElementById("maximumCarryForwardGroup");
    const input = document.getElementById("MaximumCarryForwardDays");
    const leaveCode = document.getElementById("LeaveCode");
    const leaveName = document.getElementById("LeaveName");
    const daysGroup = document.getElementById("daysPerYearGroup");
    const daysInput = document.getElementById("DaysPerYear");
    const compOffDaysNote = document.getElementById("compOffDaysNote");

    if (!checkbox || !group || !input) {
        return;
    }

    function isCompOff() {
        return (leaveCode && leaveCode.value.trim().toUpperCase() === "COMP") ||
            (leaveName && leaveName.value.trim().toUpperCase() === "COMP OFF");
    }

    function toggleMaximumCarryForward() {
        const compOff = isCompOff();
        const enabled = checkbox.checked && !compOff;

        group.style.display = enabled ? "block" : "none";

        if (!enabled) {
            input.value = "0";
        }

        checkbox.disabled = compOff;
    }

    function toggleCompOffDays() {
        const compOff = isCompOff();

        if (daysGroup && daysInput && compOffDaysNote) {
            daysInput.value = compOff ? "0" : daysInput.value;
            daysInput.readOnly = compOff;
            compOffDaysNote.style.display = compOff ? "block" : "none";
        }

        toggleMaximumCarryForward();
    }

    checkbox.addEventListener("change", toggleMaximumCarryForward);
    if (leaveCode) {
        leaveCode.addEventListener("input", toggleCompOffDays);
    }
    if (leaveName) {
        leaveName.addEventListener("input", toggleCompOffDays);
    }

    toggleCompOffDays();
});
