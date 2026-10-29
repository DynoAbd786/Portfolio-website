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

// Analytics: Umami counts visits by itself (its script is in App.razor, on the live site only, and it
// follows enhanced navigation); mkTrack sends what people do. It does nothing when Umami isn't there.
window.mkTrack = function (name, data) {
    try { window.umami?.track(name, data); } catch { }
};

// The clicks worth knowing about, from one listener: the CV, other PDFs, email, other sites, projects
document.addEventListener('click', (e) => {
    const a = e.target.closest?.('a[href]');
    if (!a) return;
    const href = a.getAttribute('href'), page = location.pathname;
    if (href.includes('Muhammad-Kashif-Khan-CV.pdf')) {
        mkTrack('cv-download', { from: a.closest('header') ? 'header' : a.closest('footer') ? 'footer' : a.closest('#hero') ? 'hero' : page });
    } else if (/\.pdf($|\?)/.test(href)) {
        mkTrack('pdf-download', { file: href.split('/').pop().split('?')[0], page });
    } else if (href.startsWith('mailto:')) {
        mkTrack('email-click', { page });
    } else if (a.host && a.host !== location.host) {
        mkTrack('outbound', { to: a.hostname.replace(/^www\./, ''), href: a.href.slice(0, 200), page });
    } else if (a.pathname.startsWith('/projects/') && a.pathname !== page) {
        const data = { project: a.pathname.split('/')[2], from: page };
        // on the homepage, which part they came from (the room, selected work, what I'm building…)
        if (page === '/') data.section = a.closest('#hero') ? 'room' : a.closest('section[id]')?.id;
        mkTrack('project-open', data);
    }
}, true);

// How far down the homepage people get: each section once per visit to the page
function mkSections() {
    mkSections.io?.disconnect();
    if (location.pathname !== '/' || !('IntersectionObserver' in window)) return;
    const seen = new Set();
    mkSections.io = new IntersectionObserver((entries) => entries.forEach((entry) => {
        const id = entry.target.id;
        if (entry.isIntersecting && !seen.has(id)) { seen.add(id); mkTrack('home-section', { section: id }); }
    }), { threshold: 0.25 });
    ['work', 'building', 'experience', 'contact'].forEach((id) => {
        const section = document.getElementById(id);
        if (section) mkSections.io.observe(section);
    });
}
document.addEventListener('DOMContentLoaded', () => {
    mkSections();
    if (window.Blazor) window.Blazor.addEventListener('enhancedload', mkSections);
});

// Inside the chat frame (/chat-panel): the assistant registers here, so the panel's header outside
// the frame can drive it, and its events go out through the page around it
window.mkChatFrame = {
    target: null,
    register: function (dotnet) { this.target = dotnet; },
    run: function (name) { this.target?.invokeMethodAsync('Command', name); },
    track: function (name, data) {
        try { window.parent?.mkTrack?.(name, { ...data, page: window.parent.location.pathname }); } catch { }
    }
};

// The chat side panel opens and closes without waiting for WebAssembly;
// only the terminal inside it is interactive.
window.mkChat = {
    toggle: function () {
        const panel = document.getElementById('chat-panel');
        const backdrop = document.getElementById('chat-backdrop');
        if (!panel || !backdrop) return;

        const opening = panel.classList.contains('translate-x-full');
        if (opening) { this._load(panel); mkTrack('ask-ai-open', { page: location.pathname }); }
        panel.classList.toggle('translate-x-full', !opening);
        panel.classList.toggle('translate-x-0', opening);
        backdrop.classList.toggle('hidden', !opening);
    },
    // The assistant runs on .NET WebAssembly (about 3 MB), so it lives on its own page and is only
    // loaded, in a frame, the first time the panel opens. The panel is data-permanent, so the
    // frame (and the conversation) survives navigating around the site.
    _load: function (panel) {
        const slot = panel.querySelector('#chat-slot');
        if (!slot || slot.querySelector('iframe')) return;
        const frame = document.createElement('iframe');
        frame.src = '/chat-panel';
        frame.title = 'AI assistant';
        frame.className = 'absolute inset-0 h-full w-full border-0 bg-transparent';
        slot.append(frame);
    },
    // the header's past-chats and new-chat buttons: passed to the assistant in the frame (mkChatFrame)
    command: function (name) {
        const frame = document.querySelector('#chat-slot iframe');
        frame?.contentWindow?.mkChatFrame?.run(name);
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
    _settled: null,
    _settle: null,
    _wantIntro: false,
    _paused: false,

    // Resolves 'live' once the scene draws its first frame, or 'still' if it won't run here (no WebGL,
    // a failed load, too slow); js/intro waits on it before landing the name
    settled() {
        if (!this._settled) this._settled = new Promise((resolve) => { this._settle = resolve; });
        const state = document.getElementById('workshop')?.dataset.state;
        if (!document.getElementById('workshop') || state === 'failed' || this._tooSlow()) this._settle('still');
        else if (state === 'live') this._settle('live');
        return this._settled;
    },

    // Resolves once the scene's script has loaded and built the room (the part that holds the main
    // thread), or if it won't run; js/intro starts the name build after it so the build never stalls
    loaded() {
        if (!this._loaded) this._loaded = new Promise((resolve) => { this._load = resolve; });
        return this._loaded;
    },

    // the held entrance (see mount's holdIntro) starts; remembered if the scene is still loading
    startIntro() {
        this._wantIntro = true;
        this._instance?.startIntro();
    },

    // Resolves once the scene's entrance has played out, or at once if it isn't running (yet)
    entrance() {
        const live = document.getElementById('workshop')?.dataset.state === 'live';
        return live && this._instance ? this._instance.entrance : Promise.resolve();
    },

    // a held scene may draw its first frame (js/intro calls this once the name build has finished)
    firstFrame() {
        this._wantFrame = true;
        this._instance?.firstFrame();
    },

    setPaused(paused) {
        this._paused = !!paused;
        this._instance?.setPaused(this._paused);
    },

    async sync() {
        // a mount still loading finishes first, so it can be disposed below if we've left the page
        while (this._pending) await this._pending;

        if (this._instance && !this._root.isConnected) {
            this._instance.dispose();
            this._instance = this._root = null;
            this.active = false;
            this.disposes++;
            this._settled = this._settle = null;
        }

        const root = document.getElementById('workshop');
        if (!root || this._instance || root.dataset.state === 'failed' || this._tooSlow()) { this.settled(); this.loaded(); this._load(); return; }
        this.loaded();

        this._pending = (async () => {
            try {
                const data = JSON.parse(root.querySelector('#workshop-data').textContent);
                const { mount } = await import('/js/workshop/workshop.js');
                // the homepage's intro plays first; tests hold it with ?hold=1
                const holdIntro = document.documentElement.classList.contains('intro-pending') || new URLSearchParams(location.search).get('hold') === '1';
                this._wantIntro = this._wantFrame = false;
                const mounting = mount(root, data, { holdIntro }); // builds the room before its first await
                this._load();
                this._instance = await mounting;
                this._root = root;
                if (this._wantIntro) this._instance.startIntro();
                else if (this._wantFrame) this._instance.firstFrame();
                if (this._paused) this._instance.setPaused(true);
                new MutationObserver((_, mo) => {
                    if (root.dataset.state === 'live') { this.settled(); mo.disconnect(); }
                }).observe(root, { attributes: true, attributeFilter: ['data-state'] });
                if (root.dataset.state === 'live') this.settled();
                // the scene gives up on devices too slow for it (or a lost GPU context)
                root.addEventListener('workshop-still', () => this._giveUp(root), { once: true });
                this.active = true;
                this.mounts++;
            } catch (e) {
                // no WebGL, a failed download or a bug: the still image and its links stay
                root.dataset.state = 'failed';
                mkTrack('scene-fallback', { reason: 'failed' });
                console.warn('[workshop]', e && e.message);
                this.settled();
                this._load();
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
        mkTrack('scene-fallback', { reason: 'slow' });
        try { sessionStorage.setItem('mk-workshop-slow', '1'); } catch { }
        this.settled();
    },

    // a device that was too slow once this session gets the still image straight away
    _tooSlow() {
        try { return sessionStorage.getItem('mk-workshop-slow') === '1'; } catch { return false; }
    }
};

// The name build's dependencies (js/intro): Delaunator, a classic script that sets window.Delaunator,
// and the self-hosted Inter 800. Rejects if either fails to load.
window.mkIntroDeps = function () {
    return this._introDeps ??= Promise.all([
        window.Delaunator ? null : new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = '/lib/delaunator/delaunator.min.js';
            script.onload = resolve;
            script.onerror = () => reject(new Error('could not load Delaunator'));
            document.head.append(script);
        }),
        document.fonts.load('800 100px "Inter Build"').then((faces) => { if (!faces.length) throw new Error('could not load Inter Build'); }),
    ]);
};

// ?debug=namebuild (tests): exposes the name build on its own
if (new URLSearchParams(location.search).get('debug') === 'namebuild') {
    window.__nameBuildReady = (async () => {
        await mkIntroDeps();
        const { createNameBuild } = await import('/js/intro/name-build.js');
        window.__nameBuild = { create: createNameBuild };
    })();
}

// The homepage hero, side by side: --frame-x is the middle of the space right of the hero text, as a
// fraction of the hero's width. The room (css/app.css and the scene) centres itself there. Returns it.
window.mkHeroFrame = function () {
    const hero = document.getElementById('hero'), copy = hero?.querySelector('.hero-copy');
    if (!hero || !copy) return .5;
    const wide = matchMedia('(min-width: 900px) and (min-height: 500px)').matches;
    const width = hero.clientWidth;
    // offsetLeft ignores the transform .hero-copy slides out with while a card is open
    const fx = wide && width ? (copy.offsetLeft + copy.offsetWidth + width) / 2 / width : .5;
    hero.style.setProperty('--frame-x', fx.toFixed(4));
    return fx;
};

// Desktop: as the page slides up over the hero, --cover runs 0 to 1 (css/app.css dims the room with
// it), and the scene stops drawing once it is fully covered
window.mkHeroCover = function () {
    const hero = document.getElementById('hero');
    const wide = matchMedia('(min-width: 900px) and (min-height: 500px)').matches;
    const cover = hero && wide ? Math.min(1, Math.max(0, scrollY / hero.offsetHeight)) : 0;
    hero?.style.setProperty('--cover', cover.toFixed(3));
    if ((cover >= 1) !== mkWorkshop._paused) mkWorkshop.setPaused(cover >= 1);
};

// The hero's See my work and Get in touch glide down to their section
document.addEventListener('click', (e) => {
    const a = e.target.closest?.('.hero-buttons a[href^="#"]');
    const target = a && document.getElementById(a.getAttribute('href').slice(1));
    if (!target || e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
    e.preventDefault(); e.stopPropagation(); // Blazor would jump instead
    history.pushState(history.state, '', a.getAttribute('href'));
    target.scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
}, true);

// The homepage intro (js/intro/intro.js): runs when App.razor's head script marked the page .intro-pending
window.mkIntro = {
    running: false,
    _control: null,

    async start() {
        const html = document.documentElement, hero = document.getElementById('hero');
        if (!html.classList.contains('intro-pending') || this.running) return;
        this.running = true;
        // until intro.js has loaded: hold scrolling already, and remember a Tab or click that should end it
        this._endEarly = false;
        const early = [
            ['wheel', (e) => e.preventDefault(), { passive: false }],
            ['touchmove', (e) => e.preventDefault(), { passive: false }],
            ['keydown', (e) => { if (e.key === 'Tab') this._endEarly = true; else if (['ArrowDown', 'PageDown', ' ', 'End'].includes(e.key)) e.preventDefault(); }, true],
            ['click', (e) => { if (e.target.closest?.('a, button')) this._endEarly = true; }, true],
        ];
        early.forEach(([type, fn, opts]) => addEventListener(type, fn, opts));
        const dropEarly = this._dropEarly = () => early.forEach(([type, fn, opts]) => removeEventListener(type, fn, opts));
        try {
            if (!hero) throw new Error('no hero');
            const { runIntro } = await import('/js/intro/intro.js');
            dropEarly();
            this._control = runIntro({
                hero,
                deps: () => mkIntroDeps(),
                loadBuild: () => import('/js/intro/name-build.js'),
                settled: mkWorkshop.settled(),
                startScene: () => mkWorkshop.startIntro(),
                firstFrame: () => mkWorkshop.firstFrame(),
                sceneLoaded: mkWorkshop.loaded(),
                sceneEntered: () => mkWorkshop.entrance(),
            });
            if (this._endEarly) this._control.finish('early');
            await this._control.done;
        } catch (e) {
            dropEarly();
            console.warn('[intro]', e && e.message);
            html.classList.remove('intro-pending', 'intro-flying');
            mkWorkshop.startIntro();
        }
        this._control = null;
        this.running = false;
    },

    // lands at once (leaving the page mid-intro, for one)
    finish() {
        if (this._control) this._control.finish('left');
        else if (this.running) { this._endEarly = true; this._dropEarly?.(); }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    mkHeroFrame();
    mkWorkshop.sync();
    mkIntro.start();
    mkHeroCover();
    addEventListener('resize', () => { mkHeroFrame(); mkHeroCover(); });
    addEventListener('scroll', () => mkHeroCover(), { passive: true });
    if (window.Blazor) window.Blazor.addEventListener('enhancedload', () => { mkIntro.finish(); mkHeroFrame(); mkHeroCover(); mkWorkshop.sync(); });
});

// Moving between pages: Blazor's enhanced navigation fetches the next page and patches it in, so a
// view transition around that crossfades the content (app.css; the header and chat panel stay put).
// Not for a form's post (the contact form), not with reduced motion; browsers without view
// transitions just change page as before.
const mkPageFade = {
    release: null,
    posting: false,
    start() {
        const posting = this.posting; this.posting = false;
        if (posting || !document.startViewTransition || matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        this.end();
        const ready = new Promise((resolve) => { this.release = resolve; });
        setTimeout(() => this.end(), 2500); // a slow page never keeps the old one frozen for long
        document.startViewTransition(() => ready);
    },
    end() { if (this.release) { this.release(); this.release = null; } },
};

// a post's navigation starts right after its submit event; the flag is used up by then
document.addEventListener('submit', () => { mkPageFade.posting = true; setTimeout(() => { mkPageFade.posting = false; }, 1000); }, true);
document.addEventListener('DOMContentLoaded', () => {
    if (!window.Blazor) return;
    window.Blazor.addEventListener('enhancednavigationstart', () => mkPageFade.start());
    window.Blazor.addEventListener('enhancedload', () => mkPageFade.end());
});
