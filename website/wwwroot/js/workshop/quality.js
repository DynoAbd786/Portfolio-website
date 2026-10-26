// Adaptive quality: watches frame times over a rolling window, relative to the display's own
// refresh rate, and steps between tiers 0 (best) and 4 (lightest). apply(level) does the work.
export const MAX_TIER = 4;

export function createQuality(apply, refreshHz) {
  const QS = { level: 0, floor: 0, lastChange: -99, trial: false, win: [], winStart: 0 };
  let forced = null; // frames per second to pretend we get (debug hook)
  let hz = refreshHz;

  function set(level) { QS.level = level; apply(level); }

  // Too slow even at the lightest tier: 5 s of frames at a median under 20 fps. The page then
  // shows the still image instead of a stuttering scene.
  const slow = { samples: [], since: null };
  function tooSlow(raw, now) {
    if (forced) raw = 1 / forced;
    if (QS.level < MAX_TIER) { slow.samples.length = 0; slow.since = null; return false; }
    if (slow.since === null) slow.since = now;
    if (raw > .25 && !forced) return false; // a hitch or a tab switch, not the device
    slow.samples.push([now, raw]);
    while (slow.samples.length && now - slow.samples[0][0] > 5) slow.samples.shift();
    if (now - slow.since < 5) return false;
    const d = slow.samples.map((x) => x[1]).sort((a, b) => a - b);
    return d.length > 5 && 1 / d[d.length >> 1] < 20;
  }

  function adapt(raw, now) {
    if (forced) raw = 1 / forced;
    else if (raw > .25) { QS.win.length = 0; QS.winStart = now; return; } // tab switch / hitch: start a fresh window
    QS.win.push(raw);
    if (now - QS.winStart < 2.5 || QS.win.length < 8) return;
    const d = QS.win.slice().sort((a, b) => a - b); QS.win.length = 0; QS.winStart = now;
    const fps = 1 / d[d.length >> 1], p90 = d[Math.floor(d.length * .9)];
    const target = Math.min(hz, 60);
    if (fps < target * .8 && QS.level < MAX_TIER) {
      if (QS.trial) QS.floor = QS.level + 1; // the last upgrade did not hold: never retry it this session
      QS.trial = false; QS.lastChange = now; set(Math.min(MAX_TIER, QS.level + (fps < target * .4 ? 2 : 1)));
    } else if (QS.level > QS.floor && p90 < 1.12 / target && now - QS.lastChange > 6) {
      QS.trial = true; QS.lastChange = now; set(QS.level - 1);
    } else if (QS.trial && now - QS.lastChange > 6) QS.trial = false;
  }

  return {
    get level() { return QS.level; },
    set refreshHz(v) { hz = v; },
    set,
    adapt,
    tooSlow,
    // after a pause, start measuring afresh
    reset(now) { QS.win.length = 0; QS.winStart = now; slow.samples.length = 0; slow.since = null; },
    forceFps(n) { forced = n || null; },
    get forced() { return forced !== null; },
  };
}

// A few empty frames before the first render tell us the display's own rate (e.g. 30 Hz battery saver)
export function measureRefresh() {
  return new Promise((resolve) => {
    const ts = []; let done = false;
    const finish = () => {
      if (done) return; done = true;
      const d = []; for (let i = 1; i < ts.length; i++) d.push(ts[i] - ts[i - 1]);
      d.sort((a, b) => a - b); resolve(d.length > 3 ? 1000 / d[d.length >> 1] : 60);
    };
    const f = (t) => { ts.push(t); if (ts.length < 12) requestAnimationFrame(f); else finish(); };
    requestAnimationFrame(f); setTimeout(finish, 500);
  });
}
