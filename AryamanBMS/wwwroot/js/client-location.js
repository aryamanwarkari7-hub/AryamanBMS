(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var stateDropdown = document.getElementById("stateDropdown");
        var cityDropdown = document.getElementById("cityDropdown");

        if (!stateDropdown || !cityDropdown) {
            return;
        }

        var currentCity = cityDropdown.dataset.currentCity || "";
        var citiesUrl = cityDropdown.dataset.citiesUrl || "/Location/GetCities";

        async function loadCities(selectedCity) {
            cityDropdown.innerHTML = '<option value="">Select City</option>';
            cityDropdown.disabled = true;

            var selectedState = stateDropdown.options[stateDropdown.selectedIndex];
            var stateId = selectedState ? selectedState.dataset.stateId : "";

            if (!stateId) {
                return;
            }

            var response = await fetch(citiesUrl + "?stateId=" + encodeURIComponent(stateId));

            if (!response.ok) {
                alert("Cities could not be loaded.");
                return;
            }

            var cities = await response.json();

            cities.forEach(function (city) {
                var option = document.createElement("option");

                option.value = city.name;
                option.textContent = city.name;
                option.selected = city.name === selectedCity;

                cityDropdown.appendChild(option);
            });

            cityDropdown.disabled = false;
        }

        stateDropdown.addEventListener("change", function () {
            loadCities("");
        });

        if (stateDropdown.value) {
            loadCities(currentCity);
        }
    });
})();
