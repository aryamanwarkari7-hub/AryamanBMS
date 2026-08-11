document.addEventListener("DOMContentLoaded", function () {
    const calendarElement = document.getElementById("employeeCalendar");

    if (!calendarElement) {
        return;
    }

    const canManageManualEvents =
        document.getElementById("calendarManualEventModal") !== null;

    const activeFilters = new Set([
        "Holiday",
        "WeeklyOff",
        "Leave",
        "Attendance",
        "Task",
        "Meeting",
        "Billing",
        "Manual",
        "Reminder",
        "Training",
        "Review",
        "Company Event"
    ]);

    const calendar = new FullCalendar.Calendar(calendarElement, {
        initialView: "dayGridMonth",
        height: "auto",
        nowIndicator: true,
        navLinks: true,
        selectable: canManageManualEvents,
        dayMaxEvents: 2,
        eventDisplay: "block",
        displayEventTime: false,
        displayEventEnd: true,
        headerToolbar: {
            left: "prev,next today",
            center: "title",
            right: "dayGridMonth,timeGridWeek,timeGridDay,listWeek"
        },
        events: function (fetchInfo, successCallback, failureCallback) {
            const url =
                `/Calendar/Events?start=${encodeURIComponent(fetchInfo.startStr)}&end=${encodeURIComponent(fetchInfo.endStr)}`;

            fetch(url)
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("Calendar events could not be loaded.");
                    }

                    return response.json();
                })
                .then(function (events) {
                    const filteredEvents = events.filter(function (event) {
                        const type = event.extendedProps?.type || event.type;
                        return activeFilters.has(type);
                    });

                    successCallback(filteredEvents);
                })
                .catch(function (error) {
                    console.error(error);
                    failureCallback(error);
                });
        },
        dateClick: function (info) {
            if (!canManageManualEvents) {
                return;
            }

            openManualEventModal({
                start: info.date,
                allDay: info.allDay
            });
        },
        eventClick: function (info) {
            const isManual =
                info.event.extendedProps.isManual === true ||
                info.event.extendedProps.isManual === "true";

            if (isManual && canManageManualEvents) {
                info.jsEvent.preventDefault();
                loadManualEvent(info.event.extendedProps.id);
                return;
            }

            if (info.event.url) {
                info.jsEvent.preventDefault();
                window.location.href = info.event.url;
            }
        },
        eventDidMount: function (info) {
            const status = info.event.extendedProps.status || "";
            const type = info.event.extendedProps.type || "";
            const textColor = info.event.textColor || "";

            info.el.title = `${type} - ${status}`;

            if (textColor) {
                info.el.style.color = textColor;

                info.el
                    .querySelectorAll(".fc-event-title, .fc-event-time")
                    .forEach(function (element) {
                        element.style.color = textColor;
                    });
            }
        }
    });

    calendar.render();
    wireCalendarFilters(calendar, activeFilters);
    wireManualEventForm(calendar);

    if (typeof createCalendarMonthPicker === "function") {
        createCalendarMonthPicker(calendar);
    }
});

/* Calendar event filter controls. */
function wireCalendarFilters(calendar, activeFilters) {
    document
        .querySelectorAll("[data-calendar-filters] [data-filter]")
        .forEach(function (button) {
            button.addEventListener("click", function () {
                const filter = button.dataset.filter;

                if (filter === "All") {
                    const shouldActivateAll =
                        !button.classList.contains("active");

                    document
                        .querySelectorAll("[data-calendar-filters] [data-filter]")
                        .forEach(function (filterButton) {
                            filterButton.classList.toggle("active", shouldActivateAll);

                            const filterValue = filterButton.dataset.filter;

                            if (filterValue !== "All") {
                                if (shouldActivateAll) {
                                    activeFilters.add(filterValue);
                                } else {
                                    activeFilters.delete(filterValue);
                                }
                            }
                        });

                    calendar.refetchEvents();
                    return;
                }

                button.classList.toggle("active");

                if (button.classList.contains("active")) {
                    activeFilters.add(filter);
                } else {
                    activeFilters.delete(filter);
                }

                const allButton =
                    document.querySelector('[data-calendar-filters] [data-filter="All"]');

                if (allButton) {
                    allButton.classList.toggle(
                        "active",
                        activeFilters.size >= 7
                    );
                }

                calendar.refetchEvents();
            });
        });
}

/* Create, edit, and delete manually scheduled calendar events. */
function wireManualEventForm(calendar) {
    const form = document.getElementById("calendarManualEventForm");

    if (!form) {
        return;
    }

    const saveButton = form.querySelector('button[type="submit"]');
    const deleteButton = document.getElementById("calendarDeleteManualEventButton");

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        event.stopPropagation();
        saveManualEvent(calendar);
    });

    if (saveButton) {
        saveButton.addEventListener("click", function (event) {
            event.preventDefault();
            event.stopPropagation();
            saveManualEvent(calendar);
        });
    }

    if (deleteButton) {
        deleteButton.addEventListener("click", function () {
            deleteManualEvent(calendar);
        });
    }
}

function openManualEventModal(options) {
    const modalElement = document.getElementById("calendarManualEventModal");

    if (!modalElement) {
        return;
    }

    resetManualEventForm();

    document.getElementById("calendarManualEventModalTitle").textContent =
        options.id ? "Edit Calendar Event" : "Create Calendar Event";

    document.getElementById("manualEventId").value = options.id || "";
    document.getElementById("manualEventTitle").value = options.title || "";
    document.getElementById("manualEventDescription").value = options.description || "";
    document.getElementById("manualEventType").value = options.eventType || "Manual";
    document.getElementById("manualEventVisibilityScope").value =
        options.visibilityScope || "All";

    document.getElementById("manualEventIsAllDay").checked =
        options.isAllDay === true || options.allDay === true;

    document.getElementById("manualEventStart").value =
        toDateTimeLocalValue(options.start || new Date());

    document.getElementById("manualEventEnd").value =
        options.end ? toDateTimeLocalValue(options.end) : "";

    const deleteButton =
        document.getElementById("calendarDeleteManualEventButton");

    if (deleteButton) {
        deleteButton.classList.toggle("d-none", !options.id);
    }

    bootstrap.Modal
        .getOrCreateInstance(modalElement)
        .show();
}

function resetManualEventForm() {
    const form = document.getElementById("calendarManualEventForm");
    const error = document.getElementById("calendarManualEventError");

    if (form) {
        form.reset();
    }

    if (error) {
        error.textContent = "";
    }
}

function loadManualEvent(id) {
    if (!id) {
        return;
    }

    fetch(`/Calendar/ManualEvent/${encodeURIComponent(id)}`)
        .then(function (response) {
            if (!response.ok) {
                throw new Error("Could not load manual calendar event.");
            }

            return response.json();
        })
        .then(function (event) {
            openManualEventModal({
                id: event.id,
                title: event.title,
                description: event.description,
                start: event.startDateTime,
                end: event.endDateTime,
                isAllDay: event.isAllDay,
                eventType: event.eventType,
                visibilityScope: event.visibilityScope
            });
        })
        .catch(function (error) {
            showManualEventError(error.message);
        });
}

function saveManualEvent(calendar) {
    const form = document.getElementById("calendarManualEventForm");

    if (!form) {
        return;
    }

    const formData = new FormData();
    const token = form
        .querySelector('input[name="__RequestVerificationToken"]')
        ?.value;

    formData.append("Id", document.getElementById("manualEventId")?.value || "");
    formData.append("Title", document.getElementById("manualEventTitle")?.value || "");
    formData.append("Description", document.getElementById("manualEventDescription")?.value || "");
    formData.append("EventType", document.getElementById("manualEventType")?.value || "Manual");
    formData.append("VisibilityScope", document.getElementById("manualEventVisibilityScope")?.value || "All");
    formData.append("StartDateTime", document.getElementById("manualEventStart")?.value || "");
    formData.append("EndDateTime", document.getElementById("manualEventEnd")?.value || "");
    formData.append("IsAllDay", document.getElementById("manualEventIsAllDay")?.checked ? "true" : "false");

    if (token) {
        formData.append("__RequestVerificationToken", token);
    }

    fetch("/Calendar/SaveManualEvent", {
        method: "POST",
        body: formData,
        credentials: "same-origin"
    })
        .then(function (response) {
            if (!response.ok) {
                return response.text().then(function (message) {
                    throw new Error(message || "Could not save calendar event.");
                });
            }

            return response.json();
        })
        .then(function () {
            closeManualEventModal();
            calendar.refetchEvents();
        })
        .catch(function (error) {
            showManualEventError(error.message);
        });
}

function deleteManualEvent(calendar) {
    const id = document.getElementById("manualEventId")?.value;

    if (!id) {
        return;
    }

    if (!confirm("Delete this calendar event?")) {
        return;
    }

    const token = document
        .querySelector('#calendarManualEventForm input[name="__RequestVerificationToken"]')
        ?.value;

    const formData = new FormData();
    formData.append("id", id);

    if (token) {
        formData.append("__RequestVerificationToken", token);
    }

    fetch("/Calendar/DeleteManualEvent", {
        method: "POST",
        body: formData
    })
        .then(function (response) {
            if (!response.ok) {
                throw new Error("Could not delete calendar event.");
            }

            return response.json();
        })
        .then(function () {
            closeManualEventModal();
            calendar.refetchEvents();
        })
        .catch(function (error) {
            showManualEventError(error.message);
        });
}

function closeManualEventModal() {
    const modalElement = document.getElementById("calendarManualEventModal");

    if (!modalElement) {
        return;
    }

    bootstrap.Modal
        .getOrCreateInstance(modalElement)
        .hide();
}

function showManualEventError(message) {
    const error = document.getElementById("calendarManualEventError");

    if (error) {
        error.textContent = message;
    }
}

function toDateTimeLocalValue(value) {
    const date = value instanceof Date
        ? value
        : new Date(value);

    if (Number.isNaN(date.getTime())) {
        return "";
    }

    const offsetDate = new Date(
        date.getTime() - date.getTimezoneOffset() * 60000);

    return offsetDate.toISOString().slice(0, 16);
}

/* Month picker: arrow navigation stays inside the popup. */
function createCalendarMonthPicker(calendar) {
    let picker = null;

    function bindTitle() {
        const titleElement = document.querySelector(".fc .fc-toolbar-title");

        if (!titleElement ||
            titleElement.classList.contains("calendar-title-picker")) {
            return;
        }

        titleElement.classList.add("calendar-title-picker");
        titleElement.setAttribute("role", "button");
        titleElement.setAttribute("tabindex", "0");
        titleElement.title = "Change month";

        titleElement.addEventListener("click", function (event) {
            event.stopPropagation();
            togglePicker(titleElement);
        });

        titleElement.addEventListener("keydown", function (event) {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                togglePicker(titleElement);
            }
        });
    }

    function togglePicker(anchor) {
        if (picker) {
            closePicker();
            return;
        }

        picker = document.createElement("div");
        picker.className = "calendar-month-picker";
        document.body.appendChild(picker);

        picker.addEventListener("click", function (event) {
            event.stopPropagation();
        });

        renderPicker(calendar.getDate());
        positionPicker(anchor);
    }

    function renderPicker(currentDate) {
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();
        const firstDate = new Date(year, month, 1);
        const startDate = new Date(firstDate);
        startDate.setDate(firstDate.getDate() - firstDate.getDay());

        picker.innerHTML = `
            <div class="calendar-month-picker-header">
                <strong>${currentDate.toLocaleDateString(undefined, {
                    month: "long",
                    year: "numeric"
                })}</strong>
                <div class="calendar-month-picker-actions">
                    <button type="button" data-picker-nav="-1" aria-label="Previous month">
                        <i class="bi bi-chevron-left"></i>
                    </button>
                    <button type="button" data-picker-nav="1" aria-label="Next month">
                        <i class="bi bi-chevron-right"></i>
                    </button>
                </div>
            </div>
            <div class="calendar-month-picker-weekdays">
                <span>S</span><span>M</span><span>T</span><span>W</span><span>T</span><span>F</span><span>S</span>
            </div>
            <div class="calendar-month-picker-grid"></div>
        `;

        const grid = picker.querySelector(".calendar-month-picker-grid");
        const today = new Date();
        const selected = calendar.getDate();

        for (let i = 0; i < 42; i++) {
            const date = new Date(startDate);
            date.setDate(startDate.getDate() + i);

            const button = document.createElement("button");
            button.type = "button";
            button.textContent = date.getDate();
            button.dataset.date = toDateOnlyValue(date);

            if (date.getMonth() !== month) {
                button.classList.add("muted");
            }

            if (isSameDate(date, today)) {
                button.classList.add("today");
            }

            if (isSameDate(date, selected)) {
                button.classList.add("selected");
            }

            button.addEventListener("click", function () {
                const selectedDate =
                    new Date(button.dataset.date + "T00:00:00");

                calendar.gotoDate(selectedDate);
                calendar.select(selectedDate);

                closePicker();
                window.setTimeout(bindTitle, 50);
            });

            grid.appendChild(button);
        }

        picker
            .querySelectorAll("[data-picker-nav]")
            .forEach(function (button) {
                button.addEventListener("click", function (event) {
                    event.preventDefault();
                    event.stopPropagation();

                    const nextDate = new Date(
                        year,
                        month + Number(button.dataset.pickerNav),
                        1
                    );

                    renderPicker(nextDate);
                    window.setTimeout(bindTitle, 50);
                });
            });
    }

    function positionPicker(anchor) {
        const rect = anchor.getBoundingClientRect();
        const pickerWidth = picker.offsetWidth || 348;
        const left = Math.min(
            Math.max(12, rect.left + rect.width / 2 - pickerWidth / 2),
            window.innerWidth - pickerWidth - 12);

        picker.style.top = `${rect.bottom + window.scrollY + 10}px`;
        picker.style.left = `${left + window.scrollX}px`;
    }

    function closePicker() {
        if (picker) {
            picker.remove();
            picker = null;
        }
    }

    document.addEventListener("click", function (event) {
        if (picker && !picker.contains(event.target)) {
            closePicker();
        }
    });

    window.addEventListener("resize", closePicker);

    bindTitle();

    calendar.on("datesSet", function () {
        window.setTimeout(bindTitle, 0);
    });
}

/* Date formatting helpers used by the month picker and forms. */
function toDateOnlyValue(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");

    return `${year}-${month}-${day}`;
}

function isSameDate(first, second) {
    return first.getFullYear() === second.getFullYear() &&
        first.getMonth() === second.getMonth() &&
        first.getDate() === second.getDate();
}
