window.changeTheme = function (theme) {
    const link = document.getElementById('theme-stylesheet');
    if (link) {
        link.setAttribute('href', `css/themes/${theme}.css`);
    }
};

// Apply the saved theme as early as possible to avoid a flash of the default theme.
(function () {
    try {
        const saved = localStorage.getItem('selectedTheme');
        if (saved) {
            window.changeTheme(saved);
        }
    } catch { }
})();
