(function () {
    window.changeDocumentStatus = function (id) {
        window.submitStatusToggle(
            "financialAuditDocumentStatusForm",
            id,
            "Do you want to change this document status?");
    };
})();
