(function () {
    if (!window.signalR) {
        return;
    }

    const preferences = window.aryamanNotificationPreferences || {
        toast: true,
        sound: false
    };

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

    function formatCount(count) {
        if (!count || count <= 0) {
            return "";
        }

        return count > 99 ? "99+" : count.toString();
    }

    function updateUnreadCount(count) {
        const bellButton = document.querySelector(".notification-bell-button");

        if (!bellButton) {
            return;
        }

        let countBadge = bellButton.querySelector(".notification-count");

        if (count > 0) {
            if (!countBadge) {
                countBadge = document.createElement("span");
                countBadge.className = "notification-count";
                bellButton.appendChild(countBadge);
            }

            countBadge.textContent = formatCount(count);
        } else if (countBadge) {
            countBadge.remove();
        }

        const headerSmall = document.querySelector(
            ".notification-menu-header small");

        if (headerSmall) {
            headerSmall.textContent = count > 0 ? `${count} unread` : "";
        }
    }

    function notificationIcon(type) {
        const iconMap = {
            TaskAssigned: "bi-person-check",
            TaskDueTomorrow: "bi-calendar-event",
            TaskDueToday: "bi-alarm",
            TaskOverdue: "bi-exclamation-triangle",
            LeaveRequest: "bi-calendar-plus",
            LeaveApproved: "bi-calendar-check",
            LeaveRejected: "bi-calendar-x",
            ExpenseSubmitted: "bi-send",
            ExpenseApproved: "bi-check-circle",
            ExpenseRejected: "bi-x-circle",
            PaymentReceived: "bi-cash-coin",
            PayslipReleased: "bi-file-earmark-person",
            SalaryPaid: "bi-wallet2",
            ProjectMeetingCreated: "bi-people",
            MomActionAssigned: "bi-list-check",
            ProjectRiskAssigned: "bi-shield-exclamation"
        };

        return iconMap[type] || "bi-info-circle";
    }

    function prependNotification(notification) {
        const list = document.querySelector(".notification-list");

        if (!list) {
            return;
        }

        const emptyState = list.querySelector(".notification-empty");

        if (emptyState) {
            emptyState.remove();
        }

        const item = document.createElement("a");
        item.className = "notification-item notification-unread notification-live-new";
        item.href = `/Notification/Open/${notification.id}`;

        item.innerHTML = `
            <div class="notification-icon">
                <i class="bi ${notificationIcon(notification.notificationType)}"></i>
            </div>
            <div class="notification-content">
                <div class="notification-title"></div>
                <div class="notification-message"></div>
                <div class="notification-time"></div>
            </div>
            <span class="notification-unread-dot"></span>
        `;

        item.querySelector(".notification-title").textContent =
            notification.title || "Notification";

        item.querySelector(".notification-message").textContent =
            notification.message || "";

        item.querySelector(".notification-time").textContent =
            notification.createdOn || "Just now";

        list.prepend(item);

        const items = list.querySelectorAll(".notification-item");

        if (items.length > 10) {
            items[items.length - 1].remove();
        }
    }

    function ensureToastHost() {
        let host = document.querySelector(".notification-toast-host");

        if (!host) {
            host = document.createElement("div");
            host.className = "notification-toast-host";
            document.body.appendChild(host);
        }

        return host;
    }

    function showToast(notification) {
        if (!preferences.toast) {
            return;
        }

        const host = ensureToastHost();
        const toast = document.createElement("div");

        toast.className = "notification-toast";
        toast.innerHTML = `
            <div class="notification-icon">
                <i class="bi ${notificationIcon(notification.notificationType)}"></i>
            </div>
            <div class="notification-content">
                <div class="notification-title"></div>
                <div class="notification-message"></div>
            </div>
            <button type="button"
                    class="notification-toast-close"
                    aria-label="Dismiss notification">
                <i class="bi bi-x-lg"></i>
            </button>
        `;

        toast.querySelector(".notification-title").textContent =
            notification.title || "Notification";

        toast.querySelector(".notification-message").textContent =
            notification.message || "";

        toast.addEventListener("click", function (event) {
            if (event.target.closest(".notification-toast-close")) {
                toast.remove();
                return;
            }

            window.location.href = `/Notification/Open/${notification.id}`;
        });

        host.appendChild(toast);

        window.requestAnimationFrame(function () {
            toast.classList.add("show");
        });

        window.setTimeout(function () {
            toast.classList.remove("show");

            window.setTimeout(function () {
                toast.remove();
            }, 220);
        }, 6000);
    }

    function playNotificationSound() {
        if (!preferences.sound) {
            return;
        }

        const AudioContext =
            window.AudioContext || window.webkitAudioContext;

        if (!AudioContext) {
            return;
        }

        try {
            const audioContext = new AudioContext();
            const oscillator = audioContext.createOscillator();
            const gain = audioContext.createGain();

            oscillator.type = "sine";
            oscillator.frequency.setValueAtTime(
                740,
                audioContext.currentTime);

            gain.gain.setValueAtTime(0.0001, audioContext.currentTime);
            gain.gain.exponentialRampToValueAtTime(
                0.08,
                audioContext.currentTime + 0.02);
            gain.gain.exponentialRampToValueAtTime(
                0.0001,
                audioContext.currentTime + 0.35);

            oscillator.connect(gain);
            gain.connect(audioContext.destination);

            oscillator.start();
            oscillator.stop(audioContext.currentTime + 0.36);
        } catch {
            // Browser autoplay rules may block sound; notifications still work.
        }
    }

    connection.on("ReceiveNotification", function (notification) {
        updateUnreadCount(notification.unreadCount || 0);
        prependNotification(notification);
        showToast(notification);
        playNotificationSound();
    });

    connection.start().catch(function () {
        // Real-time updates are enhancement-only. Normal notification pages still work.
    });
})();
