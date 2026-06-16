function initGeoapifyAutocomplete() {
    document.querySelectorAll(".js-place-autocomplete").forEach(function (input) {
        if (input.dataset.geoapifyReady === "true") return;

        var form = input.closest(".js-booking-search-form");
        var apiKey = form ? form.dataset.geoapifyKey : "";
        if (!apiKey) return;

        input.dataset.geoapifyReady = "true";

        var menu = document.createElement("div");
        menu.className = "geoapify-suggestions d-none";
        input.parentElement.classList.add("position-relative");
        input.parentElement.appendChild(menu);

        var debounceTimer;
        var lastController;

        input.addEventListener("input", function () {
            window.clearTimeout(debounceTimer);
            debounceTimer = window.setTimeout(function () {
                loadGeoapifySuggestions(input, menu, apiKey, lastController, function (controller) {
                    lastController = controller;
                });
            }, 220);
        });

        input.addEventListener("focus", function () {
            if (menu.children.length) {
                menu.classList.remove("d-none");
            }
        });

        document.addEventListener("click", function (event) {
            if (!menu.contains(event.target) && event.target !== input) {
                menu.classList.add("d-none");
            }
        });
    });
}

function loadGeoapifySuggestions(input, menu, apiKey, lastController, setController) {
    var query = input.value.trim();
    menu.innerHTML = "";

    if (query.length < 2) {
        menu.classList.add("d-none");
        return;
    }

    if (lastController) {
        lastController.abort();
    }

    var controller = new AbortController();
    setController(controller);

    var url = "https://api.geoapify.com/v1/geocode/autocomplete?text="
        + encodeURIComponent(query)
        + "&filter=countrycode:vn&format=json&limit=6&lang=en&apiKey="
        + encodeURIComponent(apiKey);

    fetch(url, { signal: controller.signal })
        .then(function (response) {
            if (!response.ok) throw new Error("Geoapify autocomplete failed");
            return response.json();
        })
        .then(function (data) {
            var results = data && Array.isArray(data.results) ? data.results : [];
            menu.innerHTML = "";

            if (!results.length) {
                menu.classList.add("d-none");
                return;
            }

            results.forEach(function (result) {
                var label = toEnglishGeoapifyLabel(result.formatted || result.address_line1 || result.name);
                if (!label) return;

                var option = document.createElement("button");
                option.type = "button";
                option.className = "geoapify-suggestion";
                option.textContent = label;
                option.addEventListener("click", function () {
                    input.value = label;
                    input.dispatchEvent(new Event("change", { bubbles: true }));
                    menu.classList.add("d-none");
                });
                menu.appendChild(option);
            });

            menu.classList.toggle("d-none", !menu.children.length);
        })
        .catch(function (error) {
            if (error.name !== "AbortError") {
                menu.classList.add("d-none");
            }
        });
}

function toEnglishGeoapifyLabel(label) {
    if (!label) return "";

    return label
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/g, "d")
        .replace(/Đ/g, "D")
        .replace(/\bHa Noi\b/g, "Hanoi")
        .replace(/\bHo Chi Minh City\b/g, "Ho Chi Minh City")
        .replace(/\bHo Chi Minh\b/g, "Ho Chi Minh")
        .replace(/\bDa Nang\b/g, "Da Nang")
        .replace(/\bViet Nam\b/g, "Vietnam")
        .replace(/\s+/g, " ")
        .trim();
}

initGeoapifyAutocomplete();
