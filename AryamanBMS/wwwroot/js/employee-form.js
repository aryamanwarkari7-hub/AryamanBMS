

// Designation loader 

document.addEventListener("DOMContentLoaded", function () {
    initializeEmployeeDesignationLoader();
    initializeEmployeeDocumentValidation();
});

function initializeEmployeeDesignationLoader() {
    const departmentDropdown = document.getElementById("departmentDropdown");
    const designationDropdown = document.getElementById("designationDropdown");

    if (!departmentDropdown || !designationDropdown) {
        return;
    }

    const selectedDesignationId =
        Number(designationDropdown.dataset.selectedDesignationId || 0);

    async function loadDesignations(departmentId, selectedId) {
        designationDropdown.innerHTML =
            '<option value="">Select Designation</option>';

        if (!departmentId) {
            return;
        }

        const response = await fetch(
            `/Employee/GetDesignations?departmentId=${encodeURIComponent(departmentId)}`
        );

        if (!response.ok) {
            return;
        }

        const data = await response.json();

        data.forEach(function (designation) {
            const option = document.createElement("option");
            option.value = designation.id;
            option.textContent = designation.designationName;
            option.selected =
                Number(designation.id) === Number(selectedId);

            designationDropdown.appendChild(option);
        });
    }

    departmentDropdown.addEventListener("change", function () {
        loadDesignations(this.value, 0);
    });

    if (departmentDropdown.value) {
        loadDesignations(
            departmentDropdown.value,
            selectedDesignationId
        );
    }
}

// Document PDF validation
function initializeEmployeeDocumentValidation() {
    const maxFileSize = 5 * 1024 * 1024;

    document
        .querySelectorAll(".document-pdf-input")
        .forEach(function (input) {
            input.addEventListener("change", function () {
                const file = this.files[0];

                if (!file) {
                    return;
                }

                const extension = file.name
                    .split(".")
                    .pop()
                    .toLowerCase();

                const isPdf =
                    extension === "pdf" ||
                    file.type === "application/pdf";

                if (!isPdf) {
                    alert("Only PDF files are allowed.");
                    this.value = "";
                    return;
                }

                if (file.size > maxFileSize) {
                    alert("File size must not exceed 5 MB.");
                    this.value = "";
                }
            });
        });
}