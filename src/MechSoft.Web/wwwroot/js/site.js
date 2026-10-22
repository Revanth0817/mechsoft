// Generic small helpers shared across pages.

function printDocument() {
    window.print();
}

document.addEventListener('click', function (e) {
    const btn = e.target.closest('[data-print]');
    if (btn) {
        printDocument();
    }
});
