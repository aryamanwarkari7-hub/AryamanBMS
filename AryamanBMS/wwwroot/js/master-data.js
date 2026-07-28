

// DEPARTMENT & DESIGNATION 
document.addEventListener("DOMContentLoaded", function () {
    const departmentName = document.getElementById("departmentName");
    const departmentCode = document.getElementById("displayCode");

    if (departmentName && departmentCode) {
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

        departmentName.addEventListener("change", function () {
            departmentCode.value = departmentCodes[this.value] || "";
        });
    }

    const designationName = document.getElementById("designationName");
    const designationCode = document.getElementById("designationCode");

    if (designationName && designationCode) {
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

        designationName.addEventListener("change", function () {
            designationCode.value = designationCodes[this.value] || "";
        });
    }
});

// ==========================================
// QUICK ADD DEPARTMENT
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const departmentName = document.getElementById("newDepartmentName");
    const departmentCode = document.getElementById("newDepartmentCode");

    if (!departmentName || !departmentCode)
        return;

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

    departmentName.addEventListener("input", function () {

        const value = this.value.trim();

        if (departmentCodes[value]) {
            departmentCode.value = departmentCodes[value];
            return;
        }

        const words = value.split(/\s+/).filter(w => w.length);

        if (words.length === 1) {
            departmentCode.value = words[0]
                .substring(0, 3)
                .toUpperCase();
        }
        else {
            departmentCode.value = words
                .map(w => w[0])
                .join("")
                .toUpperCase();
        }
    });

});

// ==========================================
// SAVE DEPARTMENT FROM MODAL
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const saveButton = document.getElementById("saveDepartmentBtn");

    if (!saveButton)
        return;

    saveButton.addEventListener("click", async function () {

        const departmentName = document.getElementById("newDepartmentName").value.trim();
        const displayCode = document.getElementById("newDepartmentCode").value.trim();
        const isActive = document.getElementById("newDepartmentStatus").value === "true";

        if (departmentName === "") {
            alert("Please enter Department Name.");
            return;
        }

        const response = await fetch("/Department/QuickCreate", {

            method: "POST",

            headers: {
                "Content-Type": "application/json"
            },

            body: JSON.stringify({
                DepartmentName: departmentName,
                DisplayCode: displayCode,
                IsActive: isActive
            })

        });

        if (!response.ok) {

            alert(await response.text());
            return;

        }

        const department = await response.json();

        await refreshDepartments(department.Id);

        document.getElementById("newDepartmentName").value = "";
        document.getElementById("newDepartmentCode").value = "";
        document.getElementById("newDepartmentStatus").value = "true";

        const modalElement = document.getElementById("departmentModal");

        let modal = bootstrap.Modal.getInstance(modalElement);

        if (!modal) {
            modal = new bootstrap.Modal(modalElement);
        }

        modal.hide();

    });

});

async function refreshDepartments(selectedId) {

    const response = await fetch("/Department/GetDepartments");

    const departments = await response.json();

    const dropdown = document.getElementById("departmentDropdown");

    dropdown.innerHTML = "";

    const first = document.createElement("option");

    first.value = "";
    first.text = "Select Department";

    dropdown.appendChild(first);

    departments.forEach(function (department) {

        const option = document.createElement("option");

        option.value = department.id;
        option.text = department.departmentName;

        if (department.id === selectedId)
            option.selected = true;

        dropdown.appendChild(option);

    });

}