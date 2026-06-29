

// ACADEMIC 
    let academicIndex =
        Number(document.getElementById("addAcademic")?.dataset.initialIndex || 0);

    document.getElementById('addAcademic')
        .addEventListener('click', function () {

            const container =
                document.getElementById('academicContainer');

            const index = academicIndex++;

            const html = `
                <div class="academic-item border rounded p-3 mb-3">

                    <input type="hidden"
                           name="Academics.Index"
                           value="${index}" />

                    <div class="form-grid-2">

                        <div class="form-group">
                            <label class="form-label">Qualification Level</label>
                            <select name="Academics[${index}].QualificationLevel"
                                    class="app-select">
                                <option value="">Select Qualification</option>
                                <option>10th</option>
                                <option>12th</option>
                                <option>Diploma</option>
                                <option>Graduation</option>
                                <option>Post Graduation</option>
                                <option>Doctorate</option>
                                <option>Certification</option>
                                <option>Other</option>
                            </select>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Course Name</label>
                            <input name="Academics[${index}].CourseName"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Institute</label>
                            <input name="Academics[${index}].InstituteName"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Passing Year</label>
                            <input name="Academics[${index}].PassingYear"
                                   type="number"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Document Type</label>
                            <select name="Academics[${index}].DocumentType"
                                    class="app-select">
                                <option>Marksheet</option>
                                <option>Degree Certificate</option>
                                <option>Passing Certificate</option>
                                <option>Other Academic Document</option>
                            </select>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Documents</label>
                            <input name="Academics[${index}].Documents"
                                   type="file"
                                   multiple
                                   accept=".pdf,.jpg,.jpeg,.png"
                                   class="form-control" />
                        </div>

                    </div>

                    <button type="button"
                            class="btn-app btn-danger-app remove-academic">
                        Remove
                    </button>

                </div>`;

            container.insertAdjacentHTML(
                'beforeend',
                html);
        });

    document.addEventListener('click', function (event) {

        if (event.target.closest('.remove-academic')) {

            const items =
                document.querySelectorAll('.academic-item');

            if (items.length > 1) {
                event.target.closest('.academic-item').remove();
            }
        }
    });


    document.addEventListener("DOMContentLoaded", async function () {
        const addressConfig =
            document.getElementById("employeeAddressConfig");

        const currentPermanentCity =
            addressConfig?.dataset.currentPermanentCity || "";

        const currentPermanentPin =
            addressConfig?.dataset.currentPermanentPin || "";

        const currentLocalCity =
            addressConfig?.dataset.currentLocalCity || "";

        const currentLocalPin =
            addressConfig?.dataset.currentLocalPin || "";

        async function loadCities(stateDropdown, cityDropdown, selectedCity = "") {
            cityDropdown.innerHTML = '<option value="">Select City</option>';
            cityDropdown.disabled = true;

            const selectedState =
                stateDropdown.options[stateDropdown.selectedIndex];
            const stateId = selectedState?.dataset.stateId;

            if (!stateId) {
                return;
            }

            const response = await fetch(
                `/Location/GetCities?stateId=${stateId}`
            );

            if (!response.ok) {
                throw new Error("Unable to load cities.");
            }

            const cities = await response.json();

            cities.forEach(city => {
                const option = document.createElement("option");
                option.value = city.name;
                option.textContent = city.name;
                option.dataset.cityId = city.id;
                option.selected = city.name === selectedCity;
                cityDropdown.appendChild(option);
            });

            cityDropdown.disabled = false;
        }

        async function loadPincodes(cityDropdown, pinList, pinInput, selectedPin = "") {
            pinList.innerHTML = "";

            const selectedCity =
                cityDropdown.options[cityDropdown.selectedIndex];
            const cityId = selectedCity?.dataset.cityId;

            if (!cityId) {
                if (selectedPin) {
                    pinInput.value = selectedPin;
                }
                return;
            }

            const response = await fetch(
                `/Location/GetPincodes?cityId=${cityId}`
            );

            if (!response.ok) {
                throw new Error("Unable to load PIN codes.");
            }

            const pincodes = await response.json();

            pincodes.forEach(pin => {
                const option = document.createElement("option");
                option.value = pin.value;
                option.label = pin.text;
                pinList.appendChild(option);
            });

            if (selectedPin) {
                pinInput.value = selectedPin;
            }
        }

        function setupLocationSection(stateDropdown, cityDropdown, pinInput, pinList) {
            stateDropdown.addEventListener("change", async function () {
                cityDropdown.innerHTML = '<option value="">Select City</option>';
                cityDropdown.disabled = true;
                pinList.innerHTML = "";
                pinInput.value = "";

                try {
                    await loadCities(stateDropdown, cityDropdown);
                }
                catch (error) {
                    console.error(error);
                    alert("Cities could not be loaded.");
                }
            });

            cityDropdown.addEventListener("change", async function () {
                pinList.innerHTML = "";
                pinInput.value = "";

                try {
                    await loadPincodes(cityDropdown, pinList, pinInput);
                }
                catch (error) {
                    console.error(error);
                    alert("PIN codes could not be loaded.");
                }
            });

            pinInput.addEventListener("input", function () {
                this.value = this.value.replace(/\D/g, "").slice(0, 6);
            });
        }

        const permanentAddress = document.getElementById("permanentAddress");
        const permanentState = document.getElementById("permanentStateDropdown");
        const permanentCity = document.getElementById("permanentCityDropdown");
        const permanentPin = document.getElementById("permanentPinCodeInput");
        const permanentPinList = document.getElementById("permanentPinCodeOptions");

        const localAddress = document.getElementById("localAddress");
        const localState = document.getElementById("localStateDropdown");
        const localCity = document.getElementById("localCityDropdown");
        const localPin = document.getElementById("localPinCodeInput");
        const localPinList = document.getElementById("localPinCodeOptions");

        setupLocationSection(
            permanentState,
            permanentCity,
            permanentPin,
            permanentPinList
        );

        setupLocationSection(
            localState,
            localCity,
            localPin,
            localPinList
        );

        try {
            if (permanentState.value) {
                await loadCities(
                    permanentState,
                    permanentCity,
                    currentPermanentCity
                );
                await loadPincodes(
                    permanentCity,
                    permanentPinList,
                    permanentPin,
                    currentPermanentPin
                );
            }

            if (localState.value) {
                await loadCities(
                    localState,
                    localCity,
                    currentLocalCity
                );
                await loadPincodes(
                    localCity,
                    localPinList,
                    localPin,
                    currentLocalPin
                );
            }
        }
        catch (error) {
            console.error("Saved address values could not be restored.", error);
        }

        document.getElementById("sameAsPermanent")
            .addEventListener("change", async function () {
                if (!this.checked) {
                    document.getElementById("localAddress").value = "";
                    localState.value = "";

                    localCity.innerHTML =
                        '<option value="">Select City</option>';
                    localCity.disabled = true;

                    localPin.value = "";
                    localPinList.innerHTML = "";

                    return;
                }

                localAddress.value = permanentAddress.value;
                localState.value = permanentState.value;

                try {
                    await loadCities(
                        localState,
                        localCity,
                        permanentCity.value
                    );

                    await loadPincodes(
                        localCity,
                        localPinList,
                        localPin,
                        permanentPin.value
                    );
                }
                catch (error) {
                    console.error(error);
                    alert("The permanent address could not be copied completely.");
                }
            });
    });




    // PREVIOUS EMPLOYMENT
    let previousEmploymentIndex =
        Number(document.getElementById("addPreviousEmployment")?.dataset.initialIndex || 0);

    document
        .getElementById("addPreviousEmployment")
        .addEventListener("click", function () {

            const index = previousEmploymentIndex++;

            const html = `
                <div class="previous-employment-item border rounded p-3 mb-3">

                    <input type="hidden"
                           name="PreviousEmployments.Index"
                           value="${index}" />

                    <div class="form-grid-2">

                        <div class="form-group">
                            <label class="form-label">Company Name</label>

                            <input name="PreviousEmployments[${index}].CompanyName"
                                   class="app-input" />

                            <span class="validation-message"
                                  data-valmsg-for="PreviousEmployments[${index}].CompanyName"
                                  data-valmsg-replace="true"></span>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Designation</label>

                            <input name="PreviousEmployments[${index}].Designation"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Department</label>

                            <input name="PreviousEmployments[${index}].Department"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Employment Type</label>

                            <select name="PreviousEmployments[${index}].EmploymentType"
                                    class="app-select">

                                <option value="">
                                    Select Employment Type
                                </option>

                                <option value="Permanent">
                                    Permanent
                                </option>

                                <option value="Contract">
                                    Contract
                                </option>

                                <option value="Intern">
                                    Intern
                                </option>

                                <option value="Consultant">
                                    Consultant
                                </option>

                            </select>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Start Date</label>

                            <input name="PreviousEmployments[${index}].StartDate"
                                   type="date"
                                   class="app-input" />

                            <span class="validation-message"
                                  data-valmsg-for="PreviousEmployments[${index}].StartDate"
                                  data-valmsg-replace="true"></span>
                        </div>

                        <div class="form-group">
                            <label class="form-label">End Date</label>

                            <input name="PreviousEmployments[${index}].EndDate"
                                   type="date"
                                   class="app-input" />

                            <span class="validation-message"
                                  data-valmsg-for="PreviousEmployments[${index}].EndDate"
                                  data-valmsg-replace="true"></span>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Last Salary</label>

                            <input name="PreviousEmployments[${index}].LastSalary"
                                   type="number"
                                   min="0"
                                   step="0.01"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Reason For Leaving</label>

                            <input name="PreviousEmployments[${index}].ReasonForLeaving"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Company Website</label>

                            <input name="PreviousEmployments[${index}].CompanyWebsite"
                                   type="url"
                                   class="app-input"
                                   placeholder="https://example.com" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">Company Address</label>

                            <textarea name="PreviousEmployments[${index}].CompanyAddress"
                                      rows="2"
                                      class="app-textarea"></textarea>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Company State</label>

                            <select name="PreviousEmployments[${index}].CompanyState"
        class="app-select previous-company-state">
    ${getCompanyStateOptionsHtml()}
</select>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Company City</label>

                            <select name="PreviousEmployments[${index}].CompanyCity"
        class="app-select previous-company-city"
        disabled>
    <option value="">Select City</option>
</select>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Company PIN Code</label>

                            <input name="PreviousEmployments[${index}].CompanyPinCode"
                                   maxlength="6"
                                   inputmode="numeric"
                                   class="app-input previous-company-pin" />
                        </div>

                    </div>

                    <h6 class="mt-4 mb-3">
                        HR Contact Information
                    </h6>

                    <div class="form-grid-2">

                        <div class="form-group">
                            <label class="form-label">HR Contact Name</label>

                            <input name="PreviousEmployments[${index}].HRContactName"
                                   class="app-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">HR Contact Email</label>

                            <input name="PreviousEmployments[${index}].HRContactEmail"
                                   type="email"
                                   class="app-input" />

                            <span class="validation-message"
                                  data-valmsg-for="PreviousEmployments[${index}].HRContactEmail"
                                  data-valmsg-replace="true"></span>
                        </div>

                        <div class="form-group">
                            <label class="form-label">HR Contact Number</label>

                            <input name="PreviousEmployments[${index}].HRContactNumber"
                                   maxlength="20"
                                   class="app-input" />
                        </div>

                    </div>

                    <h6 class="mt-4 mb-3">
                        Employment Documents
                    </h6>

                    <div class="form-grid-2">

                        <div class="form-group">
                            <label class="form-label">Experience Letter</label>

                            <input name="PreviousEmployments[${index}].ExperienceLetter"
                                   type="file"
                                   accept=".pdf,application/pdf"
                                   class="form-control previous-employment-pdf" />

                            <span class="validation-message"
                                  data-valmsg-for="PreviousEmployments[${index}].ExperienceLetter"
                                  data-valmsg-replace="true"></span>
                        </div>

                        <div class="form-group">
                            <label class="form-label">Relieving Letter</label>

                            <input name="PreviousEmployments[${index}].RelievingLetter"
                                   type="file"
                                   accept=".pdf,application/pdf"
                                   class="form-control previous-employment-pdf" />

                            <span class="validation-message"
                                  data-valmsg-for="PreviousEmployments[${index}].RelievingLetter"
                                  data-valmsg-replace="true"></span>
                        </div>

                    </div>

                    <button type="button"
                            class="btn-app btn-danger-app remove-previous-employment mt-3">

                        <i class="bi bi-trash"></i>
                        Remove Previous Employment

                    </button>

                </div>`;

            document
                .getElementById("previousEmploymentContainer")
                .insertAdjacentHTML("beforeend", html);
        });

    document.addEventListener("click", function (event) {

        const button =
            event.target.closest(
                ".remove-previous-employment"
            );

        if (!button) {
            return;
        }

        button
            .closest(".previous-employment-item")
            .remove();
    });

    document.addEventListener("input", function (event) {

        if (event.target.classList.contains(
            "previous-company-pin")) {

            event.target.value =
                event.target.value
                    .replace(/\D/g, "")
                    .slice(0, 6);
        }
    });

    document.addEventListener("change", function (event) {

        if (!event.target.classList.contains(
            "previous-employment-pdf")) {
            return;
        }

        const file = event.target.files[0];

        if (!file) {
            return;
        }

        const maximumFileSize =
            5 * 1024 * 1024;

        const isPdf =
            file.type === "application/pdf" ||
            file.name.toLowerCase().endsWith(".pdf");

        if (!isPdf) {
            alert("Only PDF files are allowed.");
            event.target.value = "";
            return;
        }

        if (file.size > maximumFileSize) {
            alert("PDF file cannot exceed 5 MB.");
            event.target.value = "";
        }
    });

// ADRESS DROPDOWNS

function getCompanyStateOptionsHtml() {
    const template =
        document.getElementById("companyStateOptionsTemplate");

    return template
        ? template.innerHTML
        : '<option value="">Select State</option>';
}

async function loadCompanyCities(stateSelect, citySelect, selectedCity) {
    citySelect.innerHTML =
        '<option value="">Select City</option>';
    citySelect.disabled = true;

    const selectedState =
        stateSelect.options[stateSelect.selectedIndex];

    const stateId =
        selectedState?.dataset.stateId;

    if (!stateId) {
        return;
    }

    const response = await fetch(
        `/Location/GetCities?stateId=${encodeURIComponent(stateId)}`
    );

    if (!response.ok) {
        alert("Cities could not be loaded.");
        return;
    }

    const cities = await response.json();

    cities.forEach(function (city) {
        const option = document.createElement("option");
        option.value = city.name;
        option.textContent = city.name;
        option.selected = city.name === selectedCity;
        citySelect.appendChild(option);
    });

    citySelect.disabled = false;
}

function initializePreviousEmploymentLocations() {
    document
        .querySelectorAll(".previous-employment-item")
        .forEach(function (item) {
            const stateSelect =
                item.querySelector(".previous-company-state");

            const citySelect =
                item.querySelector(".previous-company-city");

            if (!stateSelect || !citySelect) {
                return;
            }

            const selectedCity =
                citySelect.dataset.selectedCity || "";

            if (stateSelect.value) {
                loadCompanyCities(
                    stateSelect,
                    citySelect,
                    selectedCity
                );
            }
        });
}

document.addEventListener("change", function (event) {
    if (!event.target.classList.contains("previous-company-state")) {
        return;
    }

    const item =
        event.target.closest(".previous-employment-item");

    const citySelect =
        item?.querySelector(".previous-company-city");

    if (!citySelect) {
        return;
    }

    loadCompanyCities(event.target, citySelect, "");
});

initializePreviousEmploymentLocations();
