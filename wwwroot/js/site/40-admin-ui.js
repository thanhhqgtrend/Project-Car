document.querySelectorAll("[data-admin-menu-toggle]").forEach(function (button) {
    button.addEventListener("click", function () {
        document.body.classList.toggle("admin-menu-open");
    });
});

document.querySelectorAll("[data-theme-color-input]").forEach(function (input) {
    var key = input.dataset.themeColorInput;
    var picker = document.querySelector("[data-theme-color-picker='" + key + "']");
    var preview = document.querySelector("[data-theme-preview]");
    var cssVar = input.dataset.themeCssVar;
    var isHexColor = function (value) {
        return /^#[0-9a-fA-F]{6}$/.test(value || "");
    };
    var sync = function (value) {
        if (!isHexColor(value)) return;
        input.value = value.toLowerCase();
        if (picker) picker.value = input.value;
        if (preview && cssVar) preview.style.setProperty(cssVar, input.value);
    };

    input.addEventListener("input", function () {
        sync(input.value);
    });
    if (picker) {
        picker.addEventListener("input", function () {
            sync(picker.value);
        });
    }
});

document.querySelectorAll(".js-rich-email-form").forEach(function (form) {
    var editor = form.querySelector(".rich-editor");
    var body = form.querySelector(".js-rich-email-body");
    if (!editor || !body) return;

    form.querySelectorAll("[data-rich-command]").forEach(function (button) {
        button.addEventListener("click", function () {
            var command = button.dataset.richCommand;
            if (command === "createLink") {
                var url = window.prompt("Enter URL");
                if (url) document.execCommand(command, false, url);
            } else {
                document.execCommand(command, false, null);
            }
            editor.focus();
        });
    });

    form.addEventListener("submit", function () {
        body.value = editor.innerHTML.trim();
    });
});

document.querySelectorAll(".blog-editor-form").forEach(function (form) {
    var editor = form.querySelector(".js-rich-editor");
    var source = form.querySelector(".js-rich-editor-source");
    if (!editor || !source) return;

    form.querySelectorAll("[data-rich-command]").forEach(function (button) {
        button.addEventListener("click", function () {
            var command = button.dataset.richCommand;
            var value = button.dataset.richValue || null;
            if (command === "createLink") {
                value = window.prompt("Enter URL");
                if (!value) return;
            }
            if (command === "insertImage") {
                value = window.prompt("Enter image URL");
                if (!value) return;
            }
            document.execCommand(command, false, value);
            editor.focus();
        });
    });

    form.addEventListener("submit", function () {
        source.value = editor.innerHTML.trim();
    });
});

document.querySelectorAll(".js-admin-recalculate-total").forEach(function (button) {
    button.addEventListener("click", function () {
        var form = button.closest("form");
        if (!form) return;

        var value = function (selector) {
            var input = form.querySelector(selector);
            return Number(input && input.value || 0);
        };
        var total = Math.max(0,
            value("[name='BasePriceUsd']") +
            value("[name='AddonTotalUsd']") -
            value("[name='DiscountUsd']") +
            value("[name='TaxFeeUsd']")
        );
        var totalInput = form.querySelector("[name='TotalPriceUsd']");
        if (totalInput) totalInput.value = total.toFixed(2);
    });
});

(function () {
    var button = document.querySelector(".js-toggle-password");
    var password = document.getElementById("Password");
    if (!button || !password) {
        return;
    }

    button.addEventListener("click", function () {
        var showing = password.type === "text";
        password.type = showing ? "password" : "text";
        button.setAttribute("aria-label", showing ? "Show password" : "Hide password");
        button.innerHTML = showing ? '<i class="bi bi-eye"></i>' : '<i class="bi bi-eye-slash"></i>';
    });
})();

(function () {
    var fields = {
        name: document.getElementById("Name"),
        description: document.getElementById("Description"),
        pax: document.getElementById("PassengerCapacity"),
        bags: document.getElementById("LuggageCapacity"),
        baseFare: document.getElementById("BaseFareUsd"),
        priceKm: document.getElementById("PricePerKmUsd"),
        active: document.getElementById("IsActive")
    };
    var previewName = document.getElementById("vehiclePreviewName");
    if (!previewName) return;

    function money(value) {
        var number = Number(value || 0);
        return "$" + number.toFixed(2);
    }

    function updatePreview() {
        previewName.textContent = fields.name.value || "Vehicle name";
        document.getElementById("vehiclePreviewDescription").textContent = fields.description.value || "Short description for customers and admin users.";
        document.getElementById("vehiclePreviewPax").textContent = fields.pax.value || "0";
        document.getElementById("vehiclePreviewBags").textContent = fields.bags.value || "0";
        document.getElementById("vehiclePreviewBase").textContent = money(fields.baseFare.value);
        document.getElementById("vehiclePreviewKm").textContent = money(fields.priceKm.value);
        document.getElementById("vehiclePreviewActive").textContent = fields.active.checked ? "Active" : "Inactive";
    }

    Object.keys(fields).forEach(function (key) {
        var field = fields[key];
        if (!field) return;
        field.addEventListener("input", updatePreview);
        field.addEventListener("change", updatePreview);
    });
})();

(function () {
    var fields = {
        name: document.getElementById("Name"),
        description: document.getElementById("Description"),
        price: document.getElementById("PriceUsd"),
        mode: document.getElementById("PricingMode"),
        included: document.getElementById("IncludedQuantity"),
        order: document.getElementById("DisplayOrder"),
        active: document.getElementById("IsActive"),
        imageFile: document.getElementById("ImageFile")
    };
    var previewName = document.getElementById("addonPreviewName");
    if (!previewName) return;

    function money(value) {
        var number = Number(value || 0);
        return "$" + number.toFixed(2);
    }

    function updatePreview() {
        previewName.textContent = fields.name.value || "Addon name";
        document.getElementById("addonPreviewDescription").textContent = fields.description.value || "Short description for customers.";
        document.getElementById("addonPreviewPrice").textContent = money(fields.price.value);
        document.getElementById("addonPreviewMode").textContent = fields.mode.value || "Fixed";
        document.getElementById("addonPreviewIncluded").textContent = fields.included.value || "0";
        document.getElementById("addonPreviewOrder").textContent = fields.order.value || "0";
        document.getElementById("addonPreviewActive").textContent = fields.active.checked ? "Active" : "Inactive";
    }

    if (fields.imageFile) {
        fields.imageFile.addEventListener("change", function () {
            var file = fields.imageFile.files && fields.imageFile.files[0];
            if (!file) return;
            var preview = document.getElementById("addonPreviewImage");
            var icon = document.getElementById("addonPreviewIcon");
            if (!preview) {
                preview = document.createElement("img");
                preview.id = "addonPreviewImage";
                preview.decoding = "async";
                fields.imageFile.closest(".vehicle-edit-grid").querySelector(".addon-preview-image").appendChild(preview);
            }
            if (icon) icon.remove();
            preview.src = URL.createObjectURL(file);
        });
    }

    Object.keys(fields).forEach(function (key) {
        var field = fields[key];
        if (!field) return;
        field.addEventListener("input", updatePreview);
        field.addEventListener("change", updatePreview);
    });
})();

(function () {
    var fields = {
        title: document.getElementById("Title"),
        description: document.getElementById("Description"),
        price: document.getElementById("PriceLabel"),
        order: document.getElementById("DisplayOrder"),
        active: document.getElementById("IsActive"),
        imageFile: document.getElementById("ImageFile")
    };
    var previewTitle = document.getElementById("routePreviewTitle");
    if (!previewTitle) return;

    function updatePreview() {
        previewTitle.textContent = fields.title.value || "Route title";
        document.getElementById("routePreviewDescription").textContent = fields.description.value || "Short description for this popular route.";
        document.getElementById("routePreviewPrice").textContent = fields.price.value || "Price label";
        document.getElementById("routePreviewOrder").textContent = fields.order.value || "0";
        document.getElementById("routePreviewActive").textContent = fields.active.checked ? "Active" : "Inactive";
    }

    if (fields.imageFile) {
        fields.imageFile.addEventListener("change", function () {
            var file = fields.imageFile.files && fields.imageFile.files[0];
            if (!file) return;
            var preview = document.getElementById("routePreviewImage");
            var icon = document.getElementById("routePreviewIcon");
            if (!preview) {
                preview = document.createElement("img");
                preview.id = "routePreviewImage";
                preview.decoding = "async";
                fields.imageFile.closest(".vehicle-edit-grid").querySelector(".addon-preview-image").appendChild(preview);
            }
            if (icon) icon.remove();
            preview.src = URL.createObjectURL(file);
        });
    }

    Object.keys(fields).forEach(function (key) {
        var field = fields[key];
        if (!field) return;
        field.addEventListener("input", updatePreview);
        field.addEventListener("change", updatePreview);
    });
})();
