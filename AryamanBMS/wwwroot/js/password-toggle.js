(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var toggle = document.getElementById("passwordToggle");
        var passwordInput = document.querySelector('input[name="Password"]');

        if (!toggle || !passwordInput) {
            return;
        }

        toggle.addEventListener("click", function () {
            var toggleIcon = toggle.querySelector("i");

            if (passwordInput.type === "password") {
                passwordInput.type = "text";
                toggleIcon.classList.replace("bi-eye", "bi-eye-slash");
            }
            else {
                passwordInput.type = "password";
                toggleIcon.classList.replace("bi-eye-slash", "bi-eye");
            }
        });
    });
})();
