function updateCheckoutSummary() {
    var summary = document.querySelector(".js-price-summary");
    if (!summary) return;

    var form = document.querySelector(".checkout-main");
    var base = parseFloat(summary.dataset.base) || 0;
    var taxRate = parseFloat(summary.dataset.taxRate) || 0.08;
    var passengerCount = Number(form && form.dataset.passengerCount || 1);
    var addonTotal = 0;
    document.querySelectorAll(".js-addon-price:checked").forEach(function (input) {
        var unitPrice = Number(input.dataset.price || 0);
        var pricingMode = input.dataset.pricingMode || "Fixed";
        var includedQuantity = Number(input.dataset.includedQuantity || 0);
        var quantity = 1;

        if (pricingMode === "PerPassenger") {
            quantity = passengerCount;
        } else if (pricingMode === "Quantity") {
            var quantityInput = document.getElementById(input.dataset.quantityInput);
            quantity = Math.max(1, Number(quantityInput && quantityInput.value || 1));
            quantity = Math.max(0, quantity - includedQuantity);
        }

        addonTotal += unitPrice * quantity;
    });

    var coupon = document.querySelector(".js-coupon-code");
    var subtotal = base + addonTotal;
    var discount = coupon && coupon.value.trim().toUpperCase() === "LUXURY10" ? subtotal * 0.10 : 0;
    var tax = Math.max(0, subtotal - discount) * taxRate;
    var total = Math.max(0, subtotal - discount + tax);
    var money = function (value) { return "$" + value.toFixed(2); };

    var addonNode = summary.querySelector(".js-addon-total");
    var discountNode = summary.querySelector(".js-discount-total");
    var taxNode = summary.querySelector(".js-tax-total");
    var totalNode = summary.querySelector(".js-grand-total");
    if (addonNode) addonNode.textContent = money(addonTotal);
    if (discountNode) discountNode.textContent = "-" + money(discount);
    if (taxNode) taxNode.textContent = money(tax);
    if (totalNode) totalNode.textContent = money(total);
}

document.querySelectorAll(".js-addon-price").forEach(function (input) {
    input.addEventListener("change", updateCheckoutSummary);
});
document.querySelectorAll(".js-addon-quantity").forEach(function (input) {
    input.addEventListener("click", function (event) {
        event.stopPropagation();
    });
    input.addEventListener("input", updateCheckoutSummary);
    input.addEventListener("change", updateCheckoutSummary);
});
document.querySelectorAll(".js-coupon-code").forEach(function (input) {
    input.addEventListener("input", updateCheckoutSummary);
});
updateCheckoutSummary();
