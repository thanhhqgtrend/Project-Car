function syncBookingTripFields() {
    var selected = document.querySelector("input[name='TripType']:checked");
    if (!selected) return;

    var type = selected.value;
    var airport = document.querySelector(".js-airport-field");
    var origin = document.querySelector(".js-origin-field");
    var destination = document.querySelector(".js-destination-field");

    if (airport) airport.classList.toggle("d-none", type === "PointToPoint" || type === "Hire" || type === "4" || type === "5");
    if (origin) origin.classList.toggle("d-none", type === "AirportPickup" || type === "0");
    if (destination) destination.classList.toggle("d-none", type === "AirportDropoff" || type === "Hire" || type === "1" || type === "5");
}

document.querySelectorAll("input[name='TripType']").forEach(function (input) {
    input.addEventListener("change", syncBookingTripFields);
});
syncBookingTripFields();

function syncHomeBookingTabs(tripType) {
    var hidden = document.querySelector(".js-home-trip-type");
    if (!hidden) return;

    var type = tripType || hidden.value || "AirportPickup";
    hidden.value = type;

    document.querySelectorAll(".js-home-trip-tab").forEach(function (button) {
        var isActive = button.dataset.trip === type;
        button.classList.toggle("active", isActive);
        button.setAttribute("aria-selected", isActive ? "true" : "false");
    });

    var airport = document.querySelector(".js-home-airport-field");
    var origin = document.querySelector(".js-home-origin-field");
    var destination = document.querySelector(".js-home-destination-field");
    var originLabel = document.querySelector(".js-home-origin-label");
    var destinationLabel = document.querySelector(".js-home-destination-label");

    if (airport) airport.classList.toggle("d-none", type === "PointToPoint" || type === "Hire");
    if (origin) origin.classList.toggle("d-none", type === "AirportPickup");
    if (destination) destination.classList.toggle("d-none", type === "AirportDropoff" || type === "Hire");

    setFieldsetDisabled(airport, type === "PointToPoint" || type === "Hire");
    setFieldsetDisabled(origin, type === "AirportPickup");
    setFieldsetDisabled(destination, type === "AirportDropoff" || type === "Hire");

    var form = hidden.closest(".js-booking-search-form");
    if (originLabel) originLabel.textContent = type === "PointToPoint" ? (form && form.dataset.pointALabel || "Point A") : (form && form.dataset.pickupLabel || "Pickup point");
    if (destinationLabel) destinationLabel.textContent = type === "PointToPoint" ? (form && form.dataset.pointBLabel || "Point B") : (form && form.dataset.dropoffLabel || "Dropoff point");
}

function setFieldsetDisabled(container, disabled) {
    if (!container) return;
    container.querySelectorAll("input, select, textarea").forEach(function (field) {
        field.disabled = disabled;
    });
}

document.querySelectorAll(".js-home-trip-tab").forEach(function (button) {
    button.addEventListener("click", function () {
        syncHomeBookingTabs(button.dataset.trip);
    });
});
syncHomeBookingTabs();

document.querySelectorAll(".js-airport-autocomplete").forEach(function (input) {
    input.addEventListener("input", function () {
        var hidden = input.closest(".js-airport-field").querySelector(".js-airport-id");
        var option = Array.from(document.querySelectorAll("#airportOptions option")).find(function (item) {
            return item.value === input.value;
        });
        if (hidden) hidden.value = option ? option.dataset.id : "";
    });
});
var form = document.querySelector('.js-booking-search-form');
var tripTypeInput = document.querySelector('.js-home-trip-type');
if (form && tripTypeInput) {
    var observer = new MutationObserver(function () {
        if (tripTypeInput.value === 'Hire') {
            form.action = form.dataset.hireAction || '/hire';
        } else {
            form.action = '/booking/search';
        }
    });
    observer.observe(tripTypeInput, { attributes: true, attributeFilter: ['value'] });
}
