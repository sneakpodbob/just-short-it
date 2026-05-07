// Dismiss notification using event delegation — replaces inline onclick handlers
document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-dismiss-parent]');
    if (btn) {
        btn.parentElement.remove();
    }

    var copyButton = e.target.closest('[data-copy-url]');
    if (copyButton) {
        var value = copyButton.getAttribute('data-copy-url');
        if (value && navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(value);
        }
    }

    var confirmButton = e.target.closest('[data-confirm-message]');
    if (confirmButton) {
        var message = confirmButton.getAttribute('data-confirm-message') || 'Are you sure?';
        if (!window.confirm(message)) {
            e.preventDefault();
        }
    }
});
