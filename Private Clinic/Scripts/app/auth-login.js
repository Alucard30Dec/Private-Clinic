// Mở modal đăng nhập khi bấm .btn-login + validate form login
(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.btn-login');
        if (!btn) return;

        e.preventDefault();

        var navCollapse = document.getElementById('mainNav');
        if (navCollapse && navCollapse.classList.contains('show') && window.bootstrap) {
            bootstrap.Collapse.getOrCreateInstance(navCollapse).hide();
        }

        var modalEl = document.getElementById('loginModal');
        if (modalEl && window.bootstrap) {
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }
    });

    // validate đơn giản form login
    var f = document.getElementById('login-form');
    if (f) {
        f.addEventListener('submit', function (e) {
            if (!f.checkValidity()) {
                e.preventDefault();
                var invalid = f.querySelector(':invalid');
                if (invalid) invalid.focus();
            }
        });
    }
})();
