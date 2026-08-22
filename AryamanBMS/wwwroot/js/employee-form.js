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


// Document upload / PDF validation
function initializeEmployeeDocumentValidation() {

    const maxFileSize = 5 * 1024 * 1024;

    document
        .querySelectorAll(".document-pdf-input, .document-upload-input")
        .forEach(function (input) {

            if (input.dataset.uploadInitialized === "true") {
                return;
            }

            input.dataset.uploadInitialized = "true";

            // The existing file-input container
            const inputContainer = input.parentElement;

            // The actual document row
            const documentRow = inputContainer.parentElement;

            // Existing "No document uploaded" text
            const emptyText = documentRow.querySelector(
                ".document-empty-text"
            );

            // Temporary filename display
            const fileName = document.createElement("span");

            fileName.className = "document-upload-filename";
            fileName.style.display = "none";
            fileName.style.position = "absolute";
            fileName.style.left = "calc(66.666667% + 10px)";
            fileName.style.top = "50%";
            fileName.style.transform = "translateY(-50%)";
            fileName.style.color = "#299b68";
            fileName.style.whiteSpace = "nowrap";
            fileName.style.overflow = "hidden";
            fileName.style.textOverflow = "ellipsis";
            fileName.style.maxWidth = "220px";

            

            // Create temporary Delete button
            const deleteButton = document.createElement("button");

            deleteButton.type = "button";
            deleteButton.className =
                "btn-app btn-danger-app btn-icon document-upload-delete";

            deleteButton.title = "Remove selected file";

            deleteButton.innerHTML =
                '<i class="bi bi-trash"></i>';

            deleteButton.style.display = "none";

            /*
             * Put the temporary button into the document row,
             * NOT inside the file-input container.
             */
            documentRow.style.position = "relative";
            documentRow.appendChild(fileName);
            documentRow.appendChild(deleteButton);

            /*
             * Position it immediately to the right of
             * the file-input container.
             */
            deleteButton.style.position = "absolute";
            deleteButton.style.left =                 "calc(66.666667% + 230px)";
            deleteButton.style.top = "50%";
            deleteButton.style.transform = "translateY(-50%)";

            input.addEventListener("change", function () {

                const file = this.files[0];

                if (!file) {
                    deleteButton.style.display = "none";

                    if (emptyText) {
                        emptyText.style.display = "";
                    }

                    return;
                }

                // PDF validation
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
                    deleteButton.style.display = "none";

                    if (emptyText) {
                        emptyText.style.display = "";
                    }

                    return;
                }

                // File size validation
                if (file.size > maxFileSize) {
                    alert("File size must not exceed 5 MB.");

                    this.value = "";
                    deleteButton.style.display = "none";

                    if (emptyText) {
                        emptyText.style.display = "";
                    }

                    return;
                }

                // Show temporary filename
                fileName.textContent = file.name;
                fileName.style.display = "inline-flex";

                // Hide "No document uploaded"
                if (emptyText) {
                    emptyText.style.display = "none";
                }

                // Show temporary Delete
                deleteButton.style.display = "inline-flex";
            });

            // Temporary delete
            deleteButton.addEventListener("click", function () {

                input.value = "";

                fileName.textContent = "";
                fileName.style.display = "none";

                deleteButton.style.display = "none";

                if (emptyText) {
                    emptyText.style.display = "";
                }
            });
        });
}