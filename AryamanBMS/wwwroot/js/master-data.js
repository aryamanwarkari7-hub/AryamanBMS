

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