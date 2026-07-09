(function () {
    window.submitStatusToggle = function (formId, id, message) {
        if (!confirm(message || "Do you want to change this status?")) {
            return;
        }

        var form = document.getElementById(formId);

        if (!form) {
            return;
        }

        var idInput = form.querySelector("input[name='id']");

        if (idInput) {
            idInput.value = id;
        }

        form.submit();
    };
})();
