(() => {
    const container = document.getElementById("communicationDocumentsContainer");
    const addButton = document.getElementById("addCommunicationDocument");

    if (!container || !addButton) return;

    const row = index => `
        <div class="border-bottom pb-3 mb-3 communication-document-row">
            <input name="uploadedDocuments[${index}].DocumentName" class="app-input mb-2" maxlength="200" placeholder="Document name, e.g. Client approval" />
            <input name="uploadedDocuments[${index}].File" type="file" accept=".pdf,.doc,.docx,.xlsx" class="app-input" />
            <button type="button" class="btn-app btn-danger-app btn-sm mt-2 remove-communication-document">Remove</button>
        </div>`;

    const renumberRows = () => {
        container.querySelectorAll(".communication-document-row").forEach((documentRow, index) => {
            documentRow.querySelectorAll("input[name]").forEach(input => {
                const property = input.name.endsWith(".File") ? "File" : "DocumentName";
                input.name = `uploadedDocuments[${index}].${property}`;
            });
        });
    };

    const showSelectedFile = input => {
        const rowElement = input.closest(".communication-document-row");
        const fileName = input.files?.[0]?.name;
        let label = rowElement?.querySelector(".communication-document-file-name");

        if (!fileName || !rowElement) return;
        if (!label) {
            label = document.createElement("small");
            label.className = "text-success d-block mt-1 communication-document-file-name";
            rowElement.appendChild(label);
        }
        label.textContent = `Selected: ${fileName}`;
    };

    addButton.addEventListener("click", () => {
        container.insertAdjacentHTML(
            "beforeend",
            row(container.querySelectorAll(".communication-document-row").length));
    });

    container.addEventListener("click", event => {
        if (event.target.closest(".remove-communication-document")) {
            event.target.closest(".communication-document-row")?.remove();
            renumberRows();
        }
    });

    container.addEventListener("change", event => {
        if (event.target.matches('input[type="file"]')) {
            showSelectedFile(event.target);
        }
    });
})();
