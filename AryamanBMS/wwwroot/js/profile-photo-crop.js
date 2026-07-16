(function () {
    const input = document.getElementById("profilePhotoInput");
    const image = document.getElementById("profileCropImage");
    const output = document.getElementById("croppedProfilePhoto");
    const applyButton = document.getElementById("applyProfileCrop");
    const modalElement = document.getElementById("profileCropModal");

    if (!input || !image || !output || !applyButton || !modalElement || !window.Cropper) {
        return;
    }

    let cropper = null;
    const modal = new bootstrap.Modal(modalElement);

    input.addEventListener("change", function () {
        const file = input.files && input.files[0];

        if (!file) {
            return;
        }

        const reader = new FileReader();

        reader.onload = function (event) {
            image.src = event.target.result;
            modal.show();
        };

        reader.readAsDataURL(file);
    });

    modalElement.addEventListener("shown.bs.modal", function () {
        if (cropper) {
            cropper.destroy();
        }

        cropper = new Cropper(image, {
            aspectRatio: 1,
            viewMode: 1,
            dragMode: "move",
            autoCropArea: 1,
            responsive: true,
            background: false
        });
    });

    modalElement.addEventListener("hidden.bs.modal", function () {
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }
    });

    applyButton.addEventListener("click", function () {
        if (!cropper) {
            return;
        }

        const canvas = cropper.getCroppedCanvas({
            width: 512,
            height: 512,
            imageSmoothingQuality: "high"
        });

        output.value = canvas.toDataURL("image/jpeg", 0.9);
        modal.hide();
    });
})();