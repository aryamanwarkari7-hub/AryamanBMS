(function () {
    if (!window.signalR) {
        return;
    }

    const preferences = window.aryamanNotificationPreferences || {
        toast: true,
        sound: false
    };

    const appBasePath = window.aryamanAppBasePath || "/";

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(appUrl("notificationHub"))
        .withAutomaticReconnect()
        .build();

    function logStatus(message, data) {
        if (window.console && window.console.debug) {
            window.console.debug(`[Notifications] ${message}`, data || "");
        }
    }

    function getValue(source, camelName, pascalName, fallback) {
        if (!source) {
            return fallback;
        }

        if (Object.prototype.hasOwnProperty.call(source, camelName)) {
            return source[camelName];
        }

        if (Object.prototype.hasOwnProperty.call(source, pascalName)) {
            return source[pascalName];
        }

        return fallback;
    }

    function appUrl(path) {
        const base = appBasePath.endsWith("/")
            ? appBasePath
            : `${appBasePath}/`;

        return `${base}${path.replace(/^\/+/, "")}`;
    }

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

        let headerSmall = document.querySelector(
            ".notification-menu-header small");

        if (!headerSmall && count > 0) {
            const headerTitle = document.querySelector(
                ".notification-menu-header .notification-header-row > div");

            if (headerTitle) {
                headerSmall = document.createElement("small");
                headerTitle.appendChild(headerSmall);
            }
        }

        if (headerSmall) {
            if (count > 0) {
                headerSmall.textContent = `${count} unread`;
            } else {
                headerSmall.remove();
            }
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
            LeaveCancellationRequested: "bi-calendar-minus",
            LeaveCancellationApproved: "bi-calendar2-x",
            LeaveCancellationRejected: "bi-calendar2-check",
            EmployeeBirthday: "bi-balloon",
            EmployeeBirthdaySelf: "bi-balloon-heart",
            CompOffRequested: "bi-clock-history",
            CompOffApproved: "bi-clock",
            CompOffRejected: "bi-clock-fill",
            Login: "bi-box-arrow-in-right",
            InvoiceDue: "bi-receipt",
            InvoiceDueTomorrow: "bi-calendar-event",
            InvoiceDueToday: "bi-receipt-cutoff",
            InvoiceOverdue: "bi-exclamation-octagon",
            InvoiceSettled: "bi-check-circle",
            ExpenseSubmitted: "bi-send",
            ExpenseApproved: "bi-check-circle",
            ExpenseRejected: "bi-x-circle",
            ExpensePosted: "bi-journal-check",
            ExpenseReversed: "bi-arrow-counterclockwise",
            OfficeAssetAssigned: "bi-pc-display",
            OfficeAssetReturned: "bi-arrow-return-left",
            OfficeAssetUnderRepair: "bi-tools",
            OfficeAssetMaintenanceCompleted: "bi-wrench-adjustable-circle",
            InvoiceIssued: "bi-send-check",
            InvoiceCancelled: "bi-file-earmark-x",
            PaymentReceiptCancelled: "bi-receipt-cutoff",
            PaymentReceived: "bi-cash-coin",
            PayslipReleased: "bi-file-earmark-person",
            SalaryPaid: "bi-wallet2",
            VendorPaymentMade: "bi-bank2",
            SalaryAdvanceCreated: "bi-cash-stack",
            FullAndFinalCreated: "bi-file-earmark-lock",
            ProjectMeetingCreated: "bi-people",
            MomActionAssigned: "bi-list-check",
            ProjectRiskAssigned: "bi-shield-exclamation",
            ProjectRiskStatusChanged: "bi-arrow-repeat",
            ProjectRiskSeverityChanged: "bi-exclamation-diamond",
            GstSnapshotGenerated: "bi-calculator",
            GstSnapshotRegenerated: "bi-arrow-repeat",
            GstSnapshotVerified: "bi-patch-check",
            Gstr1Filed: "bi-file-earmark-check",
            Gstr3BFiled: "bi-file-earmark-check-fill",
            GstChallanPaid: "bi-bank",
            GstSnapshotLocked: "bi-lock",
            GstSnapshotReopened: "bi-unlock"
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
        const notificationId = getValue(notification, "id", "Id", "");
        const notificationType = getValue(
            notification,
            "notificationType",
            "NotificationType",
            "");

        item.href = appUrl(`Notification/Open/${notificationId}`);

        item.innerHTML = `
            <div class="notification-icon">
                <i class="bi ${notificationIcon(notificationType)}"></i>
            </div>
            <div class="notification-content">
                <div class="notification-title"></div>
                <div class="notification-message"></div>
                <div class="notification-time"></div>
            </div>
            <span class="notification-unread-dot"></span>
        `;

        item.querySelector(".notification-title").textContent =
            getValue(notification, "title", "Title", "Notification");

        item.querySelector(".notification-message").textContent =
            getValue(notification, "message", "Message", "");

        item.querySelector(".notification-time").textContent =
            getValue(notification, "createdOn", "CreatedOn", "Just now");

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
            logStatus("Toast skipped because toast preference is disabled.");
            return;
        }

        const host = ensureToastHost();
        const toast = document.createElement("div");
        const notificationId = getValue(notification, "id", "Id", "");
        const notificationType = getValue(
            notification,
            "notificationType",
            "NotificationType",
            "");

        toast.className =
            notificationType === "EmployeeBirthdaySelf"
                ? "notification-toast notification-toast-birthday"
                : "notification-toast";
        toast.innerHTML = `
            <div class="notification-icon">
                <i class="bi ${notificationIcon(notificationType)}"></i>
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
            getValue(notification, "title", "Title", "Notification");

        toast.querySelector(".notification-message").textContent =
            getValue(notification, "message", "Message", "");

        toast.addEventListener("click", function (event) {
            if (event.target.closest(".notification-toast-close")) {
                toast.remove();
                return;
            }

            window.location.href = appUrl(`Notification/Open/${notificationId}`);
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

            oscillator.onended = function () {
                audioContext.close();
            };
        } catch {
            // Browser autoplay rules may block sound; notifications still work.
        }
    }

    connection.on("ReceiveNotification", function (notification) {
        logStatus("Live notification received.", notification);

        const unreadCount =
            getValue(notification, "unreadCount", "UnreadCount", 0);

        updateUnreadCount(unreadCount || 0);
        prependNotification(notification);
        showToast(notification);
        playNotificationSound();
    });

    window.aryamanTestNotificationToast = function () {
        showToast({
            id: 0,
            title: "Test notification",
            message: "Toast popup is working on this browser.",
            notificationType: "System",
            createdOn: "Just now"
        });
    };

    connection.start().then(function () {
        logStatus("SignalR connected.");
    }).catch(function (error) {
        logStatus("SignalR connection failed.", error);
        // Real-time updates are enhancement-only. Normal notification pages still work.
    });
})();
