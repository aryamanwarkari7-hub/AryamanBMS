(function () {
    "use strict";

    const data = document.getElementById("birthdayCelebrationData");

    if (!data) {
        return;
    }

    const message =
        data.dataset.message ||
        "Happy birthday. Wishing you a wonderful day.";

    function createConfetti(container) {
        const colors = [
            "#2072af",
            "#db2777",
            "#f59e0b",
            "#22c55e",
            "#8b5cf6",
            "#ef4444"
        ];

        for (let index = 0; index < 86; index++) {
            const piece = document.createElement("span");
            const angle = -112 + Math.random() * 224;
            const distance = 170 + Math.random() * 430;
            const delay = Math.random() * 0.72;
            const size = 5 + Math.random() * 12;
            const type = index % 5 === 0
                ? "spark"
                : index % 4 === 0
                    ? "ribbon"
                    : "paper";

            piece.className = `birthday-confetti birthday-confetti-${type}`;
            piece.style.setProperty("--angle", `${angle}deg`);
            piece.style.setProperty("--distance", `${distance}px`);
            piece.style.setProperty("--delay", `${delay}s`);
            piece.style.setProperty("--size", `${size}px`);
            piece.style.setProperty(
                "--spin",
                `${360 + Math.random() * 760}deg`);
            piece.style.backgroundColor =
                colors[index % colors.length];

            container.appendChild(piece);
        }
    }

    function createBalloons(container) {
        const colors = ["#2072af", "#db2777", "#f59e0b", "#22c55e", "#8b5cf6"];

        for (let index = 0; index < 9; index++) {
            const balloon = document.createElement("span");

            balloon.className = "birthday-balloon";
            balloon.style.setProperty("--balloon-x", `${8 + index * 11}%`);
            balloon.style.setProperty("--balloon-delay", `${index * 0.18}s`);
            balloon.style.setProperty("--balloon-size", `${24 + (index % 3) * 8}px`);
            balloon.style.backgroundColor = colors[index % colors.length];

            container.appendChild(balloon);
        }
    }

    function closeOverlay(overlay) {
        overlay.classList.remove("show");

        window.setTimeout(function () {
            overlay.remove();
        }, 220);
    }

    function showCelebration() {
        const overlay = document.createElement("div");
        overlay.className = "birthday-celebration-overlay";

        overlay.innerHTML = `
            <div class="birthday-ambient birthday-ambient-one"></div>
            <div class="birthday-ambient birthday-ambient-two"></div>
            <div class="birthday-balloon-field"></div>

            <div class="birthday-celebration-card"
                 role="dialog"
                 aria-modal="true"
                 aria-label="Birthday celebration">
                <button type="button"
                        class="birthday-celebration-close"
                        aria-label="Close birthday celebration">
                    <i class="bi bi-x-lg"></i>
                </button>

                <div class="birthday-party-stage">
                    <div class="birthday-ring birthday-ring-one"></div>
                    <div class="birthday-ring birthday-ring-two"></div>
                    <div class="birthday-confetti-burst"></div>
                    <div class="birthday-light-beam"></div>
                    <div class="birthday-party-cone">
                        <div class="birthday-popper-mouth">
                            <span class="birthday-streamer birthday-streamer-one"></span>
                            <span class="birthday-streamer birthday-streamer-two"></span>
                            <span class="birthday-streamer birthday-streamer-three"></span>
                        </div>
                        <span></span>
                    </div>
                </div>

                <div class="birthday-celebration-content">
                    <div class="birthday-celebration-kicker">
                        Birthday Celebration
                    </div>
                    <h2>Happy Birthday</h2>
                    <p></p>
                </div>
            </div>
        `;

        overlay
            .querySelector(".birthday-celebration-content p")
            .textContent = message;

        createConfetti(
            overlay.querySelector(".birthday-confetti-burst"));

        createBalloons(
            overlay.querySelector(".birthday-balloon-field"));

        overlay.addEventListener("click", function (event) {
            if (event.target === overlay ||
                event.target.closest(".birthday-celebration-close")) {
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
        }, 9000);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", showCelebration);
    } else {
        showCelebration();
    }
})();
