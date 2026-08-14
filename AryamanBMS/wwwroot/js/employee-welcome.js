(function () {
    "use strict";

    const data = document.getElementById("employeeWelcomeData");

    if (!data) {
        return;
    }

    if (document.getElementById("birthdayCelebrationData")) {
        return;
    }

    const employeeName = data.dataset.name || "there";
    const message = data.dataset.message || "Welcome back to your workspace.";
    const photoPath = data.dataset.photo || "";

    function closeOverlay(overlay) {
        overlay.classList.remove("show");

        window.setTimeout(function () {
            overlay.remove();
        }, 220);
    }

    function showWelcome() {
        const overlay = document.createElement("div");
        overlay.className = "employee-welcome-overlay";

        overlay.innerHTML = `
            <div class="employee-welcome-card"
                 role="dialog"
                 aria-modal="true"
                 aria-label="Welcome message">
                <button type="button"
                        class="employee-welcome-close"
                        aria-label="Close welcome message">
                    <i class="bi bi-x-lg"></i>
                </button>

                <div class="employee-welcome-visual">
                </div>

                <div class="employee-welcome-kicker">Welcome</div>
                <h2></h2>
                <p></p>
            </div>
        `;

        const visual = overlay.querySelector(".employee-welcome-visual");

        if (photoPath.trim()) {
            const image = document.createElement("img");
            image.src = photoPath;
            image.alt = employeeName;
            image.className = "employee-welcome-photo";

            visual.appendChild(image);
        } else {
            visual.classList.add("employee-welcome-icon");
            visual.innerHTML = '<i class="bi bi-person-workspace"></i>';
        }

        overlay.querySelector("h2").textContent = `Hello, ${employeeName}`;
        overlay.querySelector("p").textContent = message;

        overlay.addEventListener("click", function (event) {
            if (
                event.target === overlay ||
                event.target.closest(".employee-welcome-close")
            ) {
                closeOverlay(overlay);
            }
        });

        document.body.appendChild(overlay);

        window.requestAnimationFrame(function () {
            overlay.classList.add("show");
        });

        window.setTimeout(function () {
            if (document.body.contains(overlay)) {
                closeOverlay(overlay);
            }
        }, 5200);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", showWelcome);
    } else {
        showWelcome();
    }
})();
