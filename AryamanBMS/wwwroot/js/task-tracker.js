

// TASK TRACKER

document.addEventListener("DOMContentLoaded", function () {
    const projectFilter = document.getElementById("projectFilter");
    const taskFilter = document.getElementById("taskFilter");

    if (!projectFilter || !taskFilter) {
        return;
    }

    const taskOptions = Array.from(taskFilter.options);

    projectFilter.addEventListener("change", function () {
        const selectedProjectId = this.value;

        taskFilter.innerHTML = "";

        taskOptions.forEach(function (option) {
            const optionProjectId =
                option.getAttribute("data-project-id");

            if (
                !option.value ||
                !selectedProjectId ||
                optionProjectId === selectedProjectId
            ) {
                taskFilter.appendChild(option);
            }
        });

        taskFilter.value = "";
    });
});