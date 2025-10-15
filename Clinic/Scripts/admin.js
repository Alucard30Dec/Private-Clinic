(function () {
    // Toggle sidebar on mobile
    var toggle = document.getElementById('sidebarToggle');
    var sidebar = document.querySelector('.admin-sidebar');
    if (toggle && sidebar) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });
    }
})();
