
// RISKS

document.addEventListener("DOMContentLoaded", function () {
    const probability = document.getElementById("Probability");
    const impact = document.getElementById("Impact");
    const riskScoreDisplay = document.getElementById("RiskScoreDisplay");
    const severityDisplay = document.getElementById("SeverityDisplay");

    if (probability && impact && riskScoreDisplay && severityDisplay) {
        function calculateRisk() {
            const probabilityValue = parseInt(probability.value, 10) || 1;
            const impactValue = parseInt(impact.value, 10) || 1;
            const score = probabilityValue * impactValue;

            let severity = "Low";

            if (score <= 4) {
                severity = "Low";
            } else if (score <= 9) {
                severity = "Medium";
            } else if (score <= 16) {
                severity = "High";
            } else {
                severity = "Critical";
            }

            riskScoreDisplay.value = score;
            severityDisplay.value = severity;
        }

        probability.addEventListener("change", calculateRisk);
        impact.addEventListener("change", calculateRisk);

        calculateRisk();
    }

    const projectSelect = document.getElementById("RiskProjectId");
    const ownerSelect = document.getElementById("RiskOwnerEmployeeId");

    if (projectSelect && ownerSelect) {
        projectSelect.addEventListener("change", async function () {
            const projectId = projectSelect.value;

            ownerSelect.innerHTML =
                '<option value="">Not Assigned</option>';

            if (!projectId) {
                return;
            }

            const response =
                await fetch(`/Risk/GetProjectMembers?projectId=${projectId}`);

            if (!response.ok) {
                return;
            }

            const members = await response.json();

            members.forEach(function (member) {
                const option = document.createElement("option");
                option.value = member.id;
                option.textContent = member.name;
                ownerSelect.appendChild(option);
            });
        });
    }
});
