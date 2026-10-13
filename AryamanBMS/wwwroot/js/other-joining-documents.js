(() => {
    const container = document.getElementById("otherJoiningDocumentsContainer");
    const addButton = document.getElementById("addOtherJoiningDocument");
    if (!container || !addButton) return;

    const row = index => `
        <div class="row align-items-center border-bottom py-3 other-joining-document-row">
            <div class="col-md-4"><label class="form-label mb-0">Other Joining Document Name</label></div>
            <div class="col-md-8">
                <input name="OtherJoiningDocuments[${index}].DocumentType" class="form-control mb-2" maxlength="70" placeholder="e.g. Background verification" />
                <input name="OtherJoiningDocuments[${index}].File" type="file" accept=".pdf,application/pdf" class="form-control document-pdf-input" />
                <button type="button" class="btn-app btn-danger-app btn-sm mt-2 remove-other-joining-document">Remove</button>
            </div>
        </div>`;

    const renumberRows = () => {
        container.querySelectorAll(".other-joining-document-row").forEach((documentRow, index) => {
            documentRow.querySelectorAll("input[name]").forEach(input => {
                const property = input.name.endsWith(".File") ? "File" : "DocumentType";
                input.name = `OtherJoiningDocuments[${index}].${property}`;
            });
        });
    };

    addButton.addEventListener("click", () => {
        const index = container.querySelectorAll(".other-joining-document-row").length;
        container.insertAdjacentHTML("beforeend", row(index));
    });

    container.addEventListener("click", event => {
        if (event.target.closest(".remove-other-joining-document")) {
            event.target.closest(".other-joining-document-row")?.remove();
            renumberRows();
        }
    });
})();
