

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


// Project and Task Select
document.addEventListener("DOMContentLoaded", function () {
    const projectSelect = document.getElementById("taskUpdateProjectSelect");
    const taskSelect = document.getElementById("taskUpdateTaskSelect");

    if (!projectSelect || !taskSelect) {
        return;
    }

    projectSelect.addEventListener("change", async function () {
        const projectId = this.value;

        taskSelect.innerHTML =
            '<option value="">Select Project Task</option>';

        taskSelect.disabled = true;

        if (!projectId) {
            taskSelect.innerHTML =
                '<option value="">Select Project First</option>';
            return;
        }

        const response = await fetch(
            `/TaskTracker/GetProjectTasks?projectId=${encodeURIComponent(projectId)}`
        );

        if (!response.ok) {
            taskSelect.innerHTML =
                '<option value="">Unable to load tasks</option>';
            return;
        }

        const tasks = await response.json();

        if (!tasks.length) {
            taskSelect.innerHTML =
                '<option value="">No active tasks found</option>';
            return;
        }

        tasks.forEach(function (task) {
            const option = document.createElement("option");
            option.value = task.id;
            option.textContent = task.name;
            taskSelect.appendChild(option);
        });

        taskSelect.disabled = false;
    });
});