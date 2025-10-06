// Smooth scroll + auto close mobile menu + bù trừ navbar khi mở trang kèm hash
(function () {
    var nav = document.querySelector('.navbar-clinic');
    function offsetY() { return (nav ? nav.offsetHeight : 64) + 8; }

    document.addEventListener('click', function (e) {
        var a = e.target.closest('a[href*="#"]');
        if (!a) return;

        var href = a.getAttribute('href') || '';
        var url;
        try { url = new URL(href, location.href); } catch { return; }

        // Chỉ cuộn khi cùng origin + path + query
        if (url.origin !== location.origin) return;
        if (url.pathname !== location.pathname || url.search !== location.search) return;

        var hash = (url.hash || '').slice(1);
        if (!hash) return;

        var target = document.getElementById(hash);
        if (!target) return;

        e.preventDefault();
        var y = target.getBoundingClientRect().top + window.pageYOffset - offsetY();
        window.scrollTo({ top: y, behavior: 'smooth' });

        // Đóng menu mobile nếu đang mở
        var navCollapse = document.getElementById('mainNav');
        if (navCollapse && navCollapse.classList.contains('show') && window.bootstrap) {
            bootstrap.Collapse.getOrCreateInstance(navCollapse).hide();
        }
    });

    // Bù trừ navbar nếu load trang kèm hash
    window.addEventListener('load', function () {
        if (location.hash) {
            var t = document.getElementById(location.hash.substring(1));
            if (t) {
                var y = t.getBoundingClientRect().top + window.pageYOffset - offsetY();
                window.scrollTo(0, y);
            }
        }
    });
})();
