(function () {
    $(function () {
        const orderDropdown =
            $("#PurchaseWorkOrderId");

        const projectDropdown =
            $("#ProjectId");

        const initialProjectId =
            String(
                projectDropdown.data("selected-project-id") || ""
            );

        function resetProjects() {
            projectDropdown
                .empty()
                .append(
                    '<option value="">Select Project</option>'
                )
                .prop("disabled", true);
        }

        function loadProjects(
            purchaseOrderId,
            selectedProjectId) {

            resetProjects();

            if (!purchaseOrderId) {
                return;
            }

            const url =
                orderDropdown.data("project-url");

            $.get(url, {
                purchaseOrderId: purchaseOrderId
            })
                .done(function (projects) {
                    projects.forEach(function (project) {
                        projectDropdown.append(
                            $("<option></option>")
                                .val(project.id)
                                .text(
                                    project.projectCode +
                                    " - " +
                                    project.projectName
                                )
                        );
                    });

                    projectDropdown.prop(
                        "disabled",
                        projects.length === 0
                    );

                    if (selectedProjectId) {
                        projectDropdown.val(
                            String(selectedProjectId)
                        );
                    }
                })
                .fail(function () {
                    resetProjects();

                    alert(
                        "Projects could not be loaded for the selected PO / WO."
                    );
                });
        }

        orderDropdown.on("change", function () {
            loadProjects(
                $(this).val(),
                ""
            );
        });

        if (orderDropdown.val()) {
            loadProjects(
                orderDropdown.val(),
                initialProjectId
            );
        }
    });
})();