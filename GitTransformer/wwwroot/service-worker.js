// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

self.addEventListener("DOMContentLoaded", (event) => {
    let theme = localStorage.getItem('RadzenTheme');
    if (!theme) {
        const darkThemeMq = window.matchMedia("(prefers-color-scheme: dark)");
        if (darkThemeMq.matches) {
            theme = 'dark';
        } else {
            theme = 'default';
        }
        localStorage.setItem('RadzenTheme', theme);
    }
    let themeLink = document.getElementById('theme');
    themeLink.href = `_content/Radzen.Blazor/css/${theme}.css`;
});

