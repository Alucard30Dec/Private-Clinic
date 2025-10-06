// Scripts/Home/index.js
// Validate form "Đăng kí khám không chờ đợi" (Home Hero)

(function () {
    var form = document.getElementById("quick-register-form");
    if (!form) return;

    form.addEventListener("submit", function (e) {
        if (!form.checkValidity()) {
            e.preventDefault();

            var invalid = form.querySelector(":invalid");
            if (invalid) invalid.focus();

            var email = form.querySelector('input[name="Email"]');
            if (email && email.validity.typeMismatch) {
                alert("Email không hợp lệ (phải chứa @@).");
                return;
            }

            var phone = form.querySelector('input[name="Phone"]');
            if (phone && !/^0\d{9,10}$/.test(phone.value)) {
                alert("Số điện thoại phải bắt đầu bằng 0 và có 10-11 số.");
                return;
            }

            alert("Vui lòng điền đầy đủ thông tin.");
        }
    }, false);


})();
