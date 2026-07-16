(function () {
    function getNumber(id) {
        const element = document.getElementById(id);

        if (!element) {
            return 0;
        }

        const value = parseFloat(element.value);

        return Number.isFinite(value) ? value : 0;
    }

    function setNumber(id, value) {
        const element = document.getElementById(id);

        if (!element) {
            return;
        }

        element.value = value.toFixed(2);
        element.dispatchEvent(new Event("input", { bubbles: true }));
        element.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function calculateBreakup() {
        const actualSalary = getNumber("ActualSalary");

        if (actualSalary <= 0) {
            alert("Enter Actual Salary before calculating breakup.");
            return;
        }

        const basicSalary = actualSalary * 0.50;
        const hra = basicSalary * 0.40;

        const da = 0;
        const conveyance = 0;
        const medicalAllowance = 0;
        const educationAllowance = 0;
        const otherAllowances = 0;

        const specialAllowance =
            actualSalary -
            basicSalary -
            hra -
            da -
            conveyance -
            medicalAllowance -
            educationAllowance -
            otherAllowances;

        setNumber("BasicSalary", basicSalary);
        setNumber("HRA", hra);
        setNumber("DA", da);
        setNumber("Conveyance", conveyance);
        setNumber("MedicalAllowance", medicalAllowance);
        setNumber("EducationAllowance", educationAllowance);
        setNumber("OtherAllowances", otherAllowances);
        setNumber("SpecialAllowance", specialAllowance);
    }

    document.addEventListener("DOMContentLoaded", function () {
        const button = document.getElementById("calculateSalaryBreakup");

        if (!button) {
            return;
        }

        button.addEventListener("click", calculateBreakup);
    });
})();