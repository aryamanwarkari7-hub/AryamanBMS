(function () {
    "use strict";

    const heartbeatUrl = "/Account/ActivityHeartbeat";

    // Production inactivity timeout: 10 minutes
    const idleTimeoutMs = 10 * 60 * 1000;

    const heartbeatIntervalMs = 60 * 1000;

    // Shared between all AryamanBMS tabs
    const activityStorageKey = "aryaman-last-user-activity";

    let requestInProgress = false;
    let lastSentState = null;

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector(
            'input[name="__RequestVerificationToken"]'
        );

        if (tokenInput) {
            return tokenInput.value;
        }

        const tokenMeta = document.querySelector(
            'meta[name="csrf-token"]'
        );

        return tokenMeta?.getAttribute("content") ?? "";
    }

    function getLastActivityTime() {
        const storedValue =
            localStorage.getItem(activityStorageKey);

        const parsedValue = Number(storedValue);

        if (Number.isFinite(parsedValue) && parsedValue > 0) {
            return parsedValue;
        }

        return Date.now();
    }

    function registerActivity() {
        localStorage.setItem(
            activityStorageKey,
            Date.now().toString()
        );

        if (lastSentState === false) {
            sendHeartbeat(true);
        }
    }

    async function sendHeartbeat(isActive, force = false) {
        if (requestInProgress) {
            return;
        }

        if (!force && lastSentState === isActive) {
            return;
        }

        const token = getAntiForgeryToken();

        if (!token) {
            console.warn(
                "Activity heartbeat antiforgery token was not found."
            );

            return;
        }

        requestInProgress = true;

        try {
            const response = await fetch(
                `${heartbeatUrl}?isActive=${isActive}`,
                {
                    method: "POST",
                    headers: {
                        "RequestVerificationToken": token
                    },
                    credentials: "same-origin"
                }
            );

            if (response.ok) {
                lastSentState = isActive;
            }
            else {
                console.warn(
                    `Activity heartbeat failed: ${response.status}`
                );
            }
        }
        catch (error) {
            console.warn(
                "Activity heartbeat request failed.",
                error
            );
        }
        finally {
            requestInProgress = false;
        }
    }

    function checkActivityStatus() {
        const inactiveForMs =
            Date.now() - getLastActivityTime();

        const isActive =
            inactiveForMs < idleTimeoutMs;

        sendHeartbeat(isActive);
    }

    const activityEvents = [
        "mousedown",
        "keydown",
        "touchstart",
        "scroll",
        "click"
    ];

    activityEvents.forEach(function (eventName) {
        document.addEventListener(
            eventName,
            registerActivity,
            { passive: true }
        );
    });

    // Receives activity updates from other browser tabs
    window.addEventListener("storage", function (event) {
        if (event.key === activityStorageKey) {
            checkActivityStatus();
        }
    });

    document.addEventListener(
        "visibilitychange",
        function () {
            if (document.visibilityState === "visible") {
                registerActivity();
                sendHeartbeat(true, true);
            }
        }
    );

    if (!localStorage.getItem(activityStorageKey)) {
        registerActivity();
    }

    setInterval(
        checkActivityStatus,
        heartbeatIntervalMs
    );

    checkActivityStatus();
})();