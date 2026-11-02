/**
 * CodeQuest Sound Engine
 * Manages one-shot sound effects and looping ambience tracks.
 * Called from Blazor components via IJSRuntime.
 */
window.soundInterop = (() => {
    // Volume levels (0.0 - 1.0)
    const SFX_VOLUME   = 0.6;
    const THEME_VOLUME = 0.35;

    // Preloaded Audio objects for one-shot effects
    const _sfx = {};

    // Currently playing ambience track
    let _currentTheme = null;
    let _currentThemeSrc = null;

    // Muted state (persisted in localStorage)
    let _muted = localStorage.getItem("cq_muted") === "true";

    /** Preload a sound effect so first play has no delay */
    function preload(name, src) {
        if (_sfx[name]) return;
        const a = new Audio(src);
        a.preload = "auto";
        a.volume = SFX_VOLUME;
        _sfx[name] = a;
    }

    /** Play a one-shot sound effect by name */
    function play(name) {
        if (_muted) return;
        const a = _sfx[name];
        if (!a) return;
        // Clone so overlapping plays work
        const clone = a.cloneNode();
        clone.volume = SFX_VOLUME;
        clone.play().catch(() => {}); // ignore autoplay policy errors silently
    }

    /** Start a looping ambience track. Fades in. */
    function startTheme(src) {
        if (_currentThemeSrc === src && _currentTheme && !_currentTheme.paused) return;
        stopTheme();
        if (_muted) return;
        _currentTheme = new Audio(src);
        _currentTheme.loop = true;
        _currentTheme.volume = 0;
        _currentTheme.play().catch(() => {});
        _currentThemeSrc = src;
        // Fade in over 1.5s
        let vol = 0;
        const fade = setInterval(() => {
            vol = Math.min(vol + 0.02, THEME_VOLUME);
            if (_currentTheme) _currentTheme.volume = vol;
            if (vol >= THEME_VOLUME) clearInterval(fade);
        }, 30);
    }

    /** Stop the current ambience track. Fades out. */
    function stopTheme() {
        if (!_currentTheme) return;
        const track = _currentTheme;
        _currentTheme = null;
        _currentThemeSrc = null;
        // Fade out over 1s
        let vol = track.volume;
        const fade = setInterval(() => {
            vol = Math.max(vol - 0.03, 0);
            track.volume = vol;
            if (vol <= 0) { clearInterval(fade); track.pause(); }
        }, 30);
    }

    /** Toggle mute on/off. Returns new muted state. */
    function toggleMute() {
        _muted = !_muted;
        localStorage.setItem("cq_muted", _muted);
        if (_muted) {
            stopTheme();
        }
        return _muted;
    }

    /** Returns current muted state */
    function isMuted() { return _muted; }

    return { preload, play, startTheme, stopTheme, toggleMute, isMuted };
})();
