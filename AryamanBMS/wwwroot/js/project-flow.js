// PROJECT FLOW / MILESTONES

document.addEventListener("DOMContentLoaded", function () {
    const projectSelect = document.getElementById("milestoneProjectSelect");
    const sequenceInput = document.getElementById("milestoneSequenceInput");

    if (!projectSelect || !sequenceInput) {
        return;
    }

    projectSelect.addEventListener("change", async function () {
        const projectId = this.value;

        if (!projectId) {
            sequenceInput.value = 1;
            return;
        }

        const response = await fetch(
            `/ProjectFlow/GetNextSequence?projectId=${encodeURIComponent(projectId)}`
        );

        if (!response.ok) {
            return;
        }

        const data = await response.json();

        sequenceInput.value = data.sequence;
    });
});