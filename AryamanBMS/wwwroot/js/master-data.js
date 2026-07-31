// ==========================================
// DEPARTMENT
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const departmentSelection = document.getElementById("departmentSelection");
    const departmentName = document.getElementById("departmentName");
    const departmentGroup = document.getElementById("newDepartmentGroup");
    const departmentCode = document.getElementById("displayCode");

    if (departmentSelection) {

        const departmentCodes = {
            "Human Resources": "HR",
            "Information Technology": "IT",
            "Accounts & Finance": "ACC",
            "Project Management": "PMO",
            "Administration": "ADM",
            "Sales": "SAL",
            "Marketing": "MKT",
            "Operations": "OPS",
            "Quality Assurance": "QA",
            "Customer Support": "SUP"
        };

        function generateDepartmentCode(value) {

            value = value.trim();

            if (departmentCodes[value]) {
                return departmentCodes[value];
            }

            const words = value.split(/\s+/).filter(w => w.length);

            if (words.length === 1) {
                return words[0]
                    .substring(0, 3)
                    .toUpperCase();
            }

            return words
                .map(w => w[0])
                .join("")
                .toUpperCase();
        }

        // Restore state after validation

        if (departmentName.value.trim() !== "") {

            if (departmentCodes[departmentName.value]) {

                departmentSelection.value = departmentName.value;
                departmentCode.value = generateDepartmentCode(departmentName.value);

            }
            else {

                departmentSelection.value = "__NEW__";

                departmentGroup.classList.remove("d-none");

                departmentCode.value = generateDepartmentCode(departmentName.value);

            }

        }

        departmentSelection.addEventListener("change", function () {

            if (this.value === "__NEW__") {

                departmentGroup.classList.remove("d-none");

                departmentName.value = "";
                departmentCode.value = "";

                departmentName.focus();
            }
            else {

                departmentGroup.classList.add("d-none");

                departmentName.value = this.value;
                departmentCode.value = generateDepartmentCode(this.value);
            }

        });

        departmentName.addEventListener("input", function () {

            if (departmentSelection.value === "__NEW__") {
                departmentCode.value = generateDepartmentCode(this.value);
            }

        });

        const form = departmentSelection.closest("form");

        form.addEventListener("submit", function () {

            if (departmentSelection.value !== "__NEW__") {
                departmentName.value = departmentSelection.value;
            }

        });

    }

});

// ==========================================
// DESIGNATION
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const designationSelection = document.getElementById("designationSelection");
    const designationName = document.getElementById("designationName");
    const designationGroup = document.getElementById("newDesignationGroup");
    const designationCode = document.getElementById("designationCode");

    if (!designationSelection)
        return;

    const designationCodes = {
        "Software Developer": "SD",
        "Senior Software Engineer": "SSE",
        "Team Lead": "TL",
        "Project Manager": "PM",
        "Business Analyst": "BA",
        "QA Engineer": "QA",
        "HR Executive": "HRE",
        "HR Manager": "HRM",
        "Accountant": "ACC",
        "Administrator": "ADM",
        "System Administrator": "SA"
    };

    function generateDesignationCode(value) {

        value = value.trim();

        if (designationCodes[value]) {
            return designationCodes[value];
        }

        const words = value.split(/\s+/).filter(w => w.length);

        if (words.length === 1) {
            return words[0]
                .substring(0, 3)
                .toUpperCase();
        }

        return words
            .map(w => w[0])
            .join("")
            .toUpperCase();
    }

    // Restore state after validation

    if (designationName.value.trim() !== "") {

        if (designationCodes[designationName.value]) {

            designationSelection.value = designationName.value;
            designationCode.value = generateDesignationCode(designationName.value);

        }
        else {

            designationSelection.value = "__NEW__";

            designationGroup.classList.remove("d-none");

            designationCode.value = generateDesignationCode(designationName.value);

        }

    }

    designationSelection.addEventListener("change", function () {

        if (this.value === "__NEW__") {

            designationGroup.classList.remove("d-none");

            designationName.value = "";
            designationCode.value = "";

            designationName.focus();

        }
        else {

            designationGroup.classList.add("d-none");

            designationName.value = this.value;
            designationCode.value = generateDesignationCode(this.value);

        }

    });

    designationName.addEventListener("input", function () {

        if (designationSelection.value === "__NEW__") {

            designationCode.value = generateDesignationCode(this.value);

        }

    });

    const form = designationSelection.closest("form");

    form.addEventListener("submit", function () {

        if (designationSelection.value !== "__NEW__") {
            designationName.value = designationSelection.value;
        }

    });

});