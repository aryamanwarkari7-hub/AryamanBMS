// PROJECT TASKS

document.addEventListener("DOMContentLoaded", function () {
    const projectSelect = document.getElementById("projectSelect");
    const employeeSelect = document.getElementById("employeeSelect");

    if (!projectSelect || !employeeSelect) {
        return;
    }

    projectSelect.addEventListener("change", async function () {
        const projectId = this.value;

        employeeSelect.innerHTML =
            '<option value="">Select Project Member</option>';

        if (!projectId) {
            return;
        }

        const response = await fetch(
            `/ProjectTask/GetProjectMembers?projectId=${encodeURIComponent(projectId)}`
        );

        const members = await response.json();

        members.forEach(function (member) {
            const option = document.createElement("option");
            option.value = member.id;
            option.textContent = member.name;
            employeeSelect.appendChild(option);
        });
    });
});