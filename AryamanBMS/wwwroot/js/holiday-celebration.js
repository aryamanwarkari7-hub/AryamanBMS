(function () {
    "use strict";

    const data = document.getElementById("holidayCelebrationData");
    if (!data) return;

    const holidayName = data.dataset.name || "Office Holiday";
    const holidayDate = data.dataset.date || "";
    const holidayType = data.dataset.type || "Office Holiday";

    // Inject Rainbow Glow & Card Styles
    const style = document.createElement("style");
    style.textContent = `
        .holiday-popup-overlay {
            position: fixed;
            inset: 0;
            z-index: 99999;
            display: flex;
            align-items: center;
            justify-content: center;
            background: rgba(15, 23, 42, 0.65);
            backdrop-filter: blur(8px);
            opacity: 0;
            visibility: hidden;
            transition: opacity 0.3s ease, visibility 0.3s ease;
            padding: 20px;
        }
        .holiday-popup-overlay.show {
            opacity: 1;
            visibility: visible;
        }
        .holiday-glow-wrapper {
            position: relative;
            max-width: 440px;
            width: 100%;
        }
        .holiday-rainbow-glow {
            position: absolute;
            inset: -6px;
            border-radius: 26px;
            background: linear-gradient(
                45deg,
                #ff0080,
                #ff8c00,
                #ffd700,
                #00dfa2,
                #0070f3,
                #7928ca,
                #ff0080
            );
            background-size: 300% 300%;
            filter: blur(16px);
            opacity: 0.85;
            animation: rainbowAuraAnim 5s ease infinite;
            z-index: 1;
        }
        @keyframes rainbowAuraAnim {
            0% { background-position: 0% 50%; }
            50% { background-position: 100% 50%; }
            100% { background-position: 0% 50%; }
        }
        .holiday-popup-card {
            position: relative;
            z-index: 2;
            background: #ffffff;
            border-radius: 20px;
            padding: 32px 24px 24px;
            text-align: center;
            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
            transform: scale(0.92);
            transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
        }
        .holiday-popup-overlay.show .holiday-popup-card {
            transform: scale(1);
        }
        .holiday-popup-close {
            position: absolute;
            top: 14px;
            right: 14px;
            width: 32px;
            height: 32px;
            border-radius: 50%;
            border: none;
            background: #f1f5f9;
            color: #64748b;
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            transition: background 0.2s, color 0.2s;
        }
        .holiday-popup-close:hover {
            background: #e2e8f0;
            color: #0f172a;
        }
        .holiday-badge-icon {
            width: 64px;
            height: 64px;
            margin: 0 auto 14px;
            border-radius: 50%;
            background: linear-gradient(135deg, #fef3c7, #fde68a);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.8rem;
            color: #d97706;
            box-shadow: 0 4px 12px rgba(217, 119, 6, 0.25);
        }
        .holiday-tag {
            display: inline-block;
            font-size: 0.72rem;
            font-weight: 700;
            letter-spacing: 0.8px;
            text-transform: uppercase;
            color: #d97706;
            background: #fffbeb;
            border: 1px solid #fef3c7;
            padding: 3px 12px;
            border-radius: 20px;
            margin-bottom: 10px;
        }
        .holiday-popup-title {
            font-size: 1.4rem;
            font-weight: 800;
            color: #0f172a;
            margin-bottom: 6px;
        }
        .holiday-name-highlight {
            font-size: 1.25rem;
            font-weight: 700;
            color: #2563eb;
            margin-bottom: 12px;
        }
        .holiday-date-pill {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 14px;
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            font-size: 0.85rem;
            font-weight: 600;
            color: #334155;
            margin-bottom: 14px;
        }
        .holiday-note {
            font-size: 0.86rem;
            color: #64748b;
            line-height: 1.5;
            margin-bottom: 20px;
        }
        .holiday-btn-dismiss {
            padding: 9px 24px;
            border-radius: 8px;
            border: none;
            background: linear-gradient(135deg, #2563eb, #1d4ed8);
            color: #ffffff;
            font-weight: 600;
            font-size: 0.9rem;
            cursor: pointer;
            box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3);
            transition: transform 0.2s, box-shadow 0.2s;
        }
        .holiday-btn-dismiss:hover {
            transform: translateY(-1px);
            box-shadow: 0 6px 16px rgba(37, 99, 235, 0.4);
        }
    `;
    document.head.appendChild(style);

    // Helper to get holiday-specific icon and color for the popup
    function getHolidayTheme(name) {
        const n = (name || "").toLowerCase();
        if (n.includes("diwali") || n.includes("deepawali"))
            return { icon: "bi-lamp-fill", bg: "linear-gradient(135deg, #fef3c7, #fde68a)", color: "#d97706" };
        if (n.includes("holi") || n.includes("dhulivandan"))
            return { icon: "bi-palette-fill", bg: "linear-gradient(135deg, #fce7f3, #fbcfe8)", color: "#db2777" };
        if (n.includes("eid") || n.includes("ramzan") || n.includes("bakri"))
            return { icon: "bi-moon-stars-fill", bg: "linear-gradient(135deg, #dcfce7, #bbf7d0)", color: "#16a34a" };
        if (n.includes("republic") || n.includes("independence") || n.includes("gandhi"))
            return { icon: "bi-flag-fill", bg: "linear-gradient(135deg, #ffedd5, #fed7aa)", color: "#ea580c" };
        if (n.includes("christmas"))
            return { icon: "bi-tree-fill", bg: "linear-gradient(135deg, #fee2e2, #fecaca)", color: "#dc2626" };
        if (n.includes("new year"))
            return { icon: "bi-stars", bg: "linear-gradient(135deg, #ede9fe, #ddd6fe)", color: "#7c3aed" };
        if (n.includes("ganesh"))
            return { icon: "bi-sun-fill", bg: "linear-gradient(135deg, #ffedd5, #fde68a)", color: "#d97706" };
        if (n.includes("labor") || n.includes("labour") || n.includes("may day"))
            return { icon: "bi-hammer", bg: "linear-gradient(135deg, #f1f5f9, #e2e8f0)", color: "#475569" };
        if (n.includes("mahavir") || n.includes("buddha") || n.includes("purnima"))
            return { icon: "bi-flower1", bg: "linear-gradient(135deg, #ecfdf5, #a7f3d0)", color: "#059669" };

        return { icon: "bi-calendar-heart-fill", bg: "linear-gradient(135deg, #fef3c7, #fde68a)", color: "#d97706" };
    }

    function showHolidayPopup() {
        const theme = getHolidayTheme(holidayName);
        const overlay = document.createElement("div");
        overlay.className = "holiday-popup-overlay";

        overlay.innerHTML = `
            <div class="holiday-glow-wrapper">
                <div class="holiday-rainbow-glow"></div>
                <!-- Clean White Card Background preserved -->
                <div class="holiday-popup-card" role="dialog" aria-modal="true" aria-label="Upcoming Holiday">
                    <button type="button" class="holiday-popup-close" aria-label="Close">
                        <i class="bi bi-x-lg"></i>
                    </button>

                    <!-- Dynamic Holiday Icon -->
                    <div class="holiday-badge-icon" style="background: ${theme.bg}; color: ${theme.color};">
                        <i class="bi ${theme.icon}"></i>
                    </div>

                    <div class="holiday-tag">${holidayType}</div>
                    
                    <div class="holiday-popup-title" style="letter-spacing: -0.3px;">
                        Tomorrow is an Office Holiday!
                    </div>
                    
                    <div class="holiday-name-highlight" style="font-size: 1.35rem; color: #0284c7; margin-bottom: 14px;">
                        ${holidayName}
                    </div>

                    <div class="holiday-date-pill" style="border-radius: 9999px; padding: 7px 18px; font-weight: 700; background: #f1f5f9; border-color: #e2e8f0;">
                        <i class="bi bi-calendar3 me-2 text-primary"></i> ${holidayDate}
                    </div>

                    <div class="holiday-note" style="max-width: 340px; margin: 0 auto 22px; font-size: 0.9rem; color: #64748b; line-height: 1.55;">
                        The office will remain closed tomorrow in celebration of <strong>${holidayName}</strong>. Wishing you and your family a restful and wonderful day!
                    </div>

                    <button type="button" class="holiday-btn-dismiss" style="border-radius: 9999px; padding: 11px 32px; font-weight: 700;">
                        <i class="bi bi-check2-circle me-1"></i> Thank You, Got It!
                    </button>
                </div>
            </div>
        `;

        function close() {
            overlay.classList.remove("show");
            setTimeout(() => overlay.remove(), 250);
        }

        overlay.addEventListener("click", function (e) {
            if (e.target === overlay || e.target.closest(".holiday-popup-close") || e.target.closest(".holiday-btn-dismiss")) {
                close();
            }
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && document.body.contains(overlay)) {
                close();
            }
        });

        document.body.appendChild(overlay);

        requestAnimationFrame(() => {
            overlay.classList.add("show");
        });

        // Auto dismiss after 10 seconds
        setTimeout(() => {
            if (document.body.contains(overlay)) {
                close();
            }
        }, 10000);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", showHolidayPopup);
    } else {
        showHolidayPopup();
    }
})();