
// RISKS

document.addEventListener("DOMContentLoaded", function () {
    const probability = document.getElementById("Probability");
    const impact = document.getElementById("Impact");
    const riskScoreDisplay = document.getElementById("RiskScoreDisplay");
    const severityDisplay = document.getElementById("SeverityDisplay");

    if (!probability || !impact || !riskScoreDisplay || !severityDisplay) {
        return;
    }

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
});