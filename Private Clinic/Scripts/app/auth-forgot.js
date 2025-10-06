// Toggle hiện/ẩn mật khẩu + mở modal Quên MK + validate confirm password
(function () {
    // Toggle hiển/ẩn mật khẩu
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.toggle-pass');
        if (!btn) return;
        var input = btn.parentElement.querySelector('input[type="password"], input[type="text"]');
        var icon = btn.querySelector('i');
        if (!input) return;

        var isPw = input.type === 'password';
        input.type = isPw ? 'text' : 'password';
        if (icon) {
            icon.classList.toggle('bi-eye');
            icon.classList.toggle('bi-eye-slash');
        }
    });

    // Mở modal Quên MK từ modal Đăng nhập (TRA CỨU LAI MỖI LẦN CLICK)
    document.addEventListener('click', function (e) {
        var link = e.target.closest('.js-open-forgot');
        if (!link) return;
        e.preventDefault();

        // lấy element TẠI THỜI ĐIỂM CLICK (đảm bảo không null)
        var loginEl = document.getElementById('loginModal');
        var forgotEl = document.getElementById('forgotModal');

        if (loginEl && window.bootstrap) {
            var loginIns = bootstrap.Modal.getOrCreateInstance(loginEl);
            loginEl.addEventListener('hidden.bs.modal', function handler() {
                loginEl.removeEventListener('hidden.bs.modal', handler);
                if (forgotEl) bootstrap.Modal.getOrCreateInstance(forgotEl).show();
            });
            loginIns.hide();
        } else if (forgotEl && window.bootstrap) {
            bootstrap.Modal.getOrCreateInstance(forgotEl).show();
        }
    });

    // Validate: ConfirmPassword phải khớp NewPassword
    var ff = document.getElementById('forgot-form');
    if (ff) {
        ff.addEventListener('submit', function (e) {
            var pw = ff.querySelector('input[name="NewPassword"]');
            var cf = ff.querySelector('input[name="ConfirmPassword"]');
            if (pw && cf && pw.value !== cf.value) {
                e.preventDefault();
                alert('Mật khẩu xác nhận không khớp!');
                cf.focus();
            }
        });
    }
})();
