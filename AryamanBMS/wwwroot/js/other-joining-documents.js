(() => {
    const container = document.getElementById("otherJoiningDocumentsContainer");
    const addButton = document.getElementById("addOtherJoiningDocument");

    if (!container || !addButton) return;

    const row = index => `
        <div class="row align-items-center border-bottom py-3 other-joining-document-row">
            <div class="col-md-4">
                <label class="form-label mb-0">Other Joining Document Name</label>
            </div>
            <div class="col-md-8">
                <input
                    name="OtherJoiningDocuments[${index}].DocumentType"
                    class="form-control mb-2"
                    maxlength="70"
                    placeholder="e.g. Background verification" />

                <div class="other-joining-file-inputs">
                    <input
                        name="OtherJoiningDocuments[${index}].Files"
                        type="file"
                        multiple
                        accept=".pdf,.jpg,.jpeg,.png"
                        class="form-control mb-2" />
                </div>

                <button
                    type="button"
                    class="btn-app btn-secondary-app btn-sm mt-1 add-another-file-selection">
                    + Add another file selection
                </button>

                <button
                    type="button"
                    class="btn-app btn-danger-app btn-sm mt-1 remove-other-joining-document">
                    Remove
                </button>
            </div>
        </div>`;

    const renumberRows = () => {
        container.querySelectorAll(".other-joining-document-row")
            .forEach((documentRow, index) => {
                const nameInput = documentRow.querySelector(
                    'input[name$=".DocumentType"]'
                );

                if (nameInput) {
                    nameInput.name =
                        `OtherJoiningDocuments[${index}].DocumentType`;
                }

                documentRow.querySelectorAll(
                    '.other-joining-file-inputs input[type="file"]'
                ).forEach(input => {
                    input.name =
                        `OtherJoiningDocuments[${index}].Files`;
                });
            });
    };

    addButton.addEventListener("click", () => {
        const index = container.querySelectorAll(
            ".other-joining-document-row"
        ).length;

        container.insertAdjacentHTML("beforeend", row(index));
    });

    container.addEventListener("click", event => {
        const addFileButton = event.target.closest(
            ".add-another-file-selection"
        );

        if (addFileButton) {
            const documentRow = addFileButton.closest(
                ".other-joining-document-row"
            );

            const fileInputsContainer = documentRow.querySelector(
                ".other-joining-file-inputs"
            );

            const newInput = document.createElement("input");

            newInput.type = "file";
            newInput.multiple = true;
            newInput.accept = ".pdf,.jpg,.jpeg,.png";
            newInput.className = "form-control mb-2";
            newInput.name = documentRow.querySelector(
                'input[name$=".DocumentType"]'
            ).name.replace(".DocumentType", ".Files");

            fileInputsContainer.appendChild(newInput);
            return;
        }

        const removeButton = event.target.closest(
            ".remove-other-joining-document"
        );

        if (removeButton) {
            removeButton.closest(
                ".other-joining-document-row"
            )?.remove();

            renumberRows();
        }
    });
})();