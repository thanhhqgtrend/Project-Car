(function () {
    function updateVietnamClock() {
        var target = document.getElementById("vietnamClock");
        if (!target) return;

        var now = new Date();
        target.textContent = new Intl.DateTimeFormat("en-GB", {
            timeZone: "Asia/Ho_Chi_Minh",
            year: "numeric",
            month: "2-digit",
            day: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit",
            hour12: false
        }).format(now);
    }

    updateVietnamClock();
    window.setInterval(updateVietnamClock, 1000);
})();
