// Dismiss notification using event delegation — replaces inline onclick handlers
document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-dismiss-parent]');
    if (btn) {
        btn.parentElement.remove();
    }
});
