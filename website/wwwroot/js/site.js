// Small DOM helpers for the server-rendered layout and the interactive chat.

function toggleMobileMenu() {
    const mobileMenu = document.getElementById('mobile-menu');
    const menuIcon = document.getElementById('menu-icon');
    if (!mobileMenu || !menuIcon) return;

    const opening = mobileMenu.classList.contains('hidden');
    mobileMenu.classList.toggle('hidden', !opening);
    menuIcon.classList.toggle('fa-bars', !opening);
    menuIcon.classList.toggle('fa-times', opening);
}

// The chat side panel opens and closes without waiting for WebAssembly;
// only the terminal inside it is interactive.
window.mkChat = {
    toggle: function () {
        const panel = document.getElementById('chat-panel');
        const backdrop = document.getElementById('chat-backdrop');
        if (!panel || !backdrop) return;

        const opening = panel.classList.contains('translate-x-full');
        panel.classList.toggle('translate-x-full', !opening);
        panel.classList.toggle('translate-x-0', opening);
        backdrop.classList.toggle('hidden', !opening);
    },
    toggleFullScreen: function () {
        const panel = document.getElementById('chat-panel');
        const button = document.getElementById('chat-fullscreen');
        if (!panel || !button) return;

        const full = panel.classList.toggle('sm:w-[450px]') === false;
        const icon = button.querySelector('i');
        icon.classList.toggle('fa-expand', !full);
        icon.classList.toggle('fa-compress', full);
        button.title = full ? 'Exit Full Screen' : 'Full Screen';
    }
};

window.messageUtils = {
    scrollToBottom: function (elementId) {
        var element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },
    highlightAll: function () {
        if (window.hljs) {
            document.querySelectorAll('pre code').forEach((el) => {
                if (!el.dataset.highlighted) {
                    window.hljs.highlightElement(el);
                    el.dataset.highlighted = 'true';
                }
            });
        }
    },
    renderMath: function (elementId) {
        var element = document.getElementById(elementId);
        if (element && window.renderMathInElement) {
            window.renderMathInElement(element, {
                delimiters: [
                    { left: '$$', right: '$$', display: true },
                    { left: '$', right: '$', display: false },
                    { left: '\\(', right: '\\)', display: false },
                    { left: '\\[', right: '\\]', display: true }
                ],
                throwOnError: false
            });
        }
    }
};
