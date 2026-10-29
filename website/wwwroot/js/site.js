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

// Fades .reveal elements up as they scroll into view. Runs on the first load and after
// every enhanced navigation, which swaps the page's content without a reload.
function mkReveal() {
    const items = document.querySelectorAll('.reveal:not(.is-in)');
    if (!('IntersectionObserver' in window) || matchMedia('(prefers-reduced-motion: reduce)').matches) {
        items.forEach((el) => el.classList.add('is-in'));
        return;
    }
    const io = new IntersectionObserver((entries) => {
        entries.forEach((e) => {
            if (e.isIntersecting) {
                e.target.classList.add('is-in');
                io.unobserve(e.target);
            }
        });
    }, { rootMargin: '0px 0px -8% 0px' });
    items.forEach((el) => io.observe(el));
}

document.addEventListener('DOMContentLoaded', () => {
    mkReveal();
    // blazor.web.js loads after this file, so Blazor exists by DOMContentLoaded
    if (window.Blazor) window.Blazor.addEventListener('enhancedload', mkReveal);
});

// The homepage's 3D workshop (js/workshop). Mounted when #workshop appears and disposed when
// enhanced navigation removes it, so only one scene (and one WebGL context) ever exists.
window.mkWorkshop = {
    active: false,
    mounts: 0,
    disposes: 0,
    _instance: null,
    _root: null,
    _pending: null,

    async sync() {
        // a mount still loading finishes first, so it can be disposed below if we've left the page
        while (this._pending) await this._pending;

        if (this._instance && !this._root.isConnected) {
            this._instance.dispose();
            this._instance = this._root = null;
            this.active = false;
            this.disposes++;
        }

        const root = document.getElementById('workshop');
        if (!root || this._instance || root.dataset.state === 'failed' || this._tooSlow()) return;

        this._pending = (async () => {
            try {
                const data = JSON.parse(root.querySelector('#workshop-data').textContent);
                const { mount } = await import('/js/workshop/workshop.js');
                this._instance = await mount(root, data);
                this._root = root;
                // the scene gives up on devices too slow for it (or a lost GPU context)
                root.addEventListener('workshop-still', () => this._giveUp(root), { once: true });
                this.active = true;
                this.mounts++;
            } catch (e) {
                // no WebGL, a failed download or a bug: the still image and its links stay
                root.dataset.state = 'failed';
                console.warn('[workshop]', e && e.message);
            }
        })();
        await this._pending;
        this._pending = null;
        // we may have navigated away while it loaded
        if (this._instance && !this._root.isConnected) await this.sync();
    },

    _giveUp(root) {
        if (this._root !== root) return;
        this._instance.dispose();
        this._instance = this._root = null;
        this.active = false;
        this.disposes++;
        root.dataset.state = 'still';
        try { sessionStorage.setItem('mk-workshop-slow', '1'); } catch { }
    },

    // a device that was too slow once this session gets the still image straight away
    _tooSlow() {
        try { return sessionStorage.getItem('mk-workshop-slow') === '1'; } catch { return false; }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    mkWorkshop.sync();
    if (window.Blazor) window.Blazor.addEventListener('enhancedload', () => mkWorkshop.sync());
});
