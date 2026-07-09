(function () {
    window.openCreate = function () {
        $("#modalTitle").text("Add Company Document Category");
        $("#categoryForm")[0].reset();
        $("#DocumentCategoryId").val(0);
        $("#IsActive").prop("checked", true);
        $("#categoryModal").modal("show");
    };

    window.openEdit = function (id) {
        var modal = document.getElementById("categoryModal");
        var getUrl = modal ? modal.dataset.getUrl : "/CompanyDocumentCategory/Get";

        $.get(getUrl + "/" + id, function (data) {
            $("#modalTitle").text("Edit Company Document Category");

            $("#DocumentCategoryId").val(data.documentCategoryId);
            $("#CategoryCode").val(data.categoryCode);
            $("#CategoryName").val(data.categoryName);
            $("#Description").val(data.description);
            $("#DisplayOrder").val(data.displayOrder);
            $("#ExpiryReminderDays").val(data.expiryReminderDays);
            $("#AllowedExtensions").val(data.allowedExtensions);
            $("#MaxFileSizeMB").val(data.maxFileSizeMB);

            $("#IsMandatory").prop("checked", data.isMandatory);
            $("#HasExpiry").prop("checked", data.hasExpiry);
            $("#RequireDocumentNumber").prop("checked", data.requireDocumentNumber);
            $("#AllowMultipleDocuments").prop("checked", data.allowMultipleDocuments);
            $("#IsActive").prop("checked", data.isActive);

            $("#categoryModal").modal("show");
        });
    };

    window.deleteCategory = function (id) {
        window.submitStatusToggle(
            "categoryStatusForm",
            id,
            "Do you want to change this category status?");
    };
})();
