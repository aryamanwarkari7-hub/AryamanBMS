// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ALERT TIME

document.addEventListener("DOMContentLoaded", function () {
    setTimeout(function () {
        document
            .querySelectorAll(".alert-message-app")
            .forEach(function (alert) {
                alert.classList.add("alert-hide");

                setTimeout(function () {
                    alert.remove();
                }, 500);
            });
    }, 3000);
});


document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.querySelector(".sidebar");
    const toggle = document.querySelector("[data-sidebar-toggle]");
    const backdrop = document.querySelector("[data-sidebar-close]");

    if (!sidebar || !toggle || !backdrop) {
        return;
    }

    function openSidebar() {
        sidebar.classList.add("is-open");
        backdrop.classList.add("is-open");
        toggle.setAttribute("aria-expanded", "true");
    }

    function closeSidebar() {
        sidebar.classList.remove("is-open");
        backdrop.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
    }

    toggle.addEventListener("click", function () {
        if (sidebar.classList.contains("is-open")) {
            closeSidebar();
        } else {
            openSidebar();
        }
    });

    backdrop.addEventListener("click", closeSidebar);

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeSidebar();
        }
    });
});