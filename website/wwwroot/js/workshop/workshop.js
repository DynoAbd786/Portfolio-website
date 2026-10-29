// The homepage's 3D workshop. site.js mounts it on #workshop (and disposes it when enhanced
// navigation leaves the page); the server-rendered still image stays until the first frame.
import * as THREE from 'three';
import { V3, clamp, lerp, easeInOut, easeOutBack, damp, rnd, BACKGROUND, createRenderer, createKit, isSoftwareRenderer, disposeScene } from './kit.js';
import { buildRoom, XL, FX1, FZ1, S, RS, BAND } from './room.js';
import { buildProps } from './props.js';
import { SW, SH, crt, createSynergyScreen, createTerminal } from './screens.js';
import { createPost } from './post.js';
import { bindHotspots } from './hotspots.js';
import { createUI } from './ui.js';
import { createQuality, measureRefresh } from './quality.js';

const ROBOT_LINES = ['“ask me about Synergy”', '“try the left monitor”', '“I know where the CV is”', '“ask me about the thesis”'];
// How much of the stage the room fills: the prototype's camera distances times this
const FRAME = .74;
const HOME = { t: new V3(.35, 1.45, -1.05), az: .34, polar: 1.12, r: 21.8 * FRAME };
const INTRO_FROM = { t: new V3(.35, .7, -.8), az: .34 + .8, polar: .55, r: 38 * FRAME };
const INTRO_DUR = 2.2;

// holdIntro: keep the entrance (objects popping in, monitors booting, the camera's flight) until
// startIntro(), since the homepage's name build plays over it first (js/intro). Nothing is drawn
// until firstFrame() either: the first frame links the shaders and holds the main thread, so it waits
// until the build has finished drawing.
export async function mount(root, data, { holdIntro = false } = {}) {
  const canvas = root.querySelector('canvas.workshop-canvas');
  const section = root.closest('section') || root.parentElement;
  // ?still=1 (tools/render-workshop-still.py): no motion and the home pose, for rendering the still image
  const STILL = new URLSearchParams(location.search).get('still') === '1';
  const REDUCED = STILL || matchMedia('(prefers-reduced-motion: reduce)').matches;
  // ?debug=workshop (tests): exposes root.__workshop; the scene only gives up when told to via forceFps,
  // since headless test browsers render in software and would otherwise fall back mid-test
  const DEBUG = new URLSearchParams(location.search).get('debug') === 'workshop';
  const TOUCH = matchMedia('(hover: none)').matches;
  let MOBILE = innerWidth < 900;
  // the stacked layout (phones, tablets, short landscape screens); the room sits beside the text otherwise
  const stackedQuery = matchMedia('(max-width: 899px), (max-height: 499px)');
  let W = Math.max(1, root.clientWidth), H = Math.max(1, root.clientHeight);

  const renderer = createRenderer(canvas); // throws 'webgl-unavailable'
  root.dataset.state = 'loading';
  const DEVICE_DPR = devicePixelRatio || 1;
  const BASE_DPR = Math.min(DEVICE_DPR, 1.5); // a large desktop stage: bloom and outline passes make 1.5 plenty
  // Phones and tablets (the stacked layout): pixels the scene may draw at each quality tier. Their
  // stage is small, so within that budget they render at the screen's own density instead of a
  // blurry fixed scale. Desktop keeps the fixed scale.
  const PIXEL_BUDGET = [1.0e6, .7e6, .45e6, .28e6, .16e6]; // the lightest stays animated on modest phones
  const dprFor = (q) => {
    const fixed = q >= 4 ? .8 : q >= 3 ? Math.min(BASE_DPR, 1) : q >= 2 ? Math.min(BASE_DPR, 1.15) : BASE_DPR;
    return MOBILE ? Math.min(DEVICE_DPR, Math.max(fixed, Math.sqrt(PIXEL_BUDGET[q] / (W * H)))) : fixed;
  };
  let DPR = dprFor(0);
  renderer.setPixelRatio(DPR);
  renderer.setSize(W, H, false);

  const scene = new THREE.Scene();
  scene.background = new THREE.Color(BACKGROUND);
  const camera = new THREE.PerspectiveCamera(30, W / H, 0.1, 300);
  const kit = createKit(renderer);
  const R = rnd(7);
  const room = buildRoom(scene, kit, { reduced: REDUCED, mobile: MOBILE, R });
  const props = buildProps(room.world, kit);
  Object.assign(room, props);
  room.pops.push(...[[props.stent, .55], [props.afm, .8], [props.azure, 1.05]].map(([g, delay]) => ({ g, delay, baseScale: 1 })));
  const { lights, monA, monB } = room;
  const SYN = createSynergyScreen(R), TERM = createTerminal();
  const post = createPost(renderer, scene, camera, { width: W, height: H, dpr: DPR, mobile: MOBILE });
  const hs = bindHotspots(data, room);

  let disposed = false, dirty = true, hfT0 = 0, T = 0, intro = REDUCED ? 99 : 0, lampOn = REDUCED ? 1 : 0, frameNo = 0;
  let held = holdIntro && !REDUCED, paused = false, framesAllowed = !held;
  // resolves once the entrance has played out (the camera at rest, everything popped in)
  let entered; const entrance = new Promise((resolve) => { entered = resolve; });
  if (REDUCED) entered();
  const ui = createUI({
    root, section, data,
    onOpen: (k) => { if (k === 'hireforge') hfT0 = T; dirty = true; },
    onClose: () => { dirty = true; },
  });
  document.fonts.ready.then(() => { if (!disposed) { room.fontJobs.forEach((f) => f()); dirty = true; } });

  const listeners = [];
  const on = (target, type, fn, opts) => { target.addEventListener(type, fn, opts); listeners.push(() => target.removeEventListener(type, fn, opts)); };

  // ---------------------------------------------------------------- camera rig
  const cam = { t: HOME.t.clone(), az: HOME.az, polar: HOME.polar, r: HOME.r };
  // Framing beside the text (wide layout): the room fits the stage height, or the free space right of
  // the hero text if that is tighter (camera.zoom), and is centred in that space (camera.filmOffset).
  // A clicked object is centred in the space right of its card instead.
  const roomBox = new THREE.Box3().setFromObject(room.room); // before the entrance moves the world
  const corner = new V3();
  let zoomHome = 1, skewHome = 0, zoomNow = 1, skewNow = 0, fxOverride = null, stillRoom = 0, stillPins = [], parOx = 0;
  // and moved up or down (ndc) so it sits in the middle of the stage's height
  let liftHome = 0, liftNow = 0;
  function applyProjection() {
    camera.zoom = zoomNow; camera.filmOffset = skewNow; camera.updateProjectionMatrix();
    if (liftNow) { camera.projectionMatrix.elements[9] -= liftNow; camera.projectionMatrixInverse.copy(camera.projectionMatrix).invert(); }
  }
  // the film offset that moves ndc x `from` (at zoom 1) to ndc x `to` at the given zoom
  const skewFor = (from, to, zoom) => (from * zoom - to) * camera.getFilmWidth() * camera.aspect * Math.tan(THREE.MathUtils.degToRad(camera.fov / 2)) / zoom;
  // the room's outline in ndc for the current camera
  function roomNdc() {
    let x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
    for (let i = 0; i < 8; i++) {
      corner.set(i & 1 ? roomBox.max.x : roomBox.min.x, i & 2 ? roomBox.max.y : roomBox.min.y, i & 4 ? roomBox.max.z : roomBox.min.z).project(camera);
      x0 = Math.min(x0, corner.x); x1 = Math.max(x1, corner.x); y0 = Math.min(y0, corner.y); y1 = Math.max(y1, corner.y);
    }
    return { x0, x1, y0, y1 };
  }
  function frameHome() {
    if (stackedQuery.matches && fxOverride == null) {
      // the phone stage (4:3) is narrow enough that the room's left wall would touch the edge: pan to centre it
      zoomHome = 1; skewHome = W / H < 1.45 ? -1 : 0; liftHome = 0; parOx = 0;
      return;
    }
    const fx = fxOverride ?? (window.mkHeroFrame?.() ?? .62), freeW = 2 * (1 - fx) * W;
    parOx = 2 * fx - 1; // the pointer's parallax is neutral over the room, not the middle of the hero
    camera.zoom = 1; camera.filmOffset = 0; camera.updateProjectionMatrix();
    placeCamera(HOME.az, HOME.polar, HOME.r, HOME.t); camera.updateMatrixWorld();
    const b = roomNdc();
    const wPx = (b.x1 - b.x0) / 2 * W, hPx = (b.y1 - b.y0) / 2 * H;
    zoomHome = Math.min(.92 * H / hPx, .88 * freeW / wPx);
    skewHome = skewFor((b.x0 + b.x1) / 2, 2 * fx - 1, zoomHome);
    liftHome = -(b.y0 + b.y1) / 2 * zoomHome;
    applyProjection();
  }
  function layout() {
    W = Math.max(1, root.clientWidth); H = Math.max(1, root.clientHeight); MOBILE = innerWidth < 900;
    DPR = dprFor(quality.level); // the budget depends on the stage's size
    camera.aspect = W / H;
    frameHome();
    if (frameNo === 0) { zoomNow = zoomHome; skewNow = skewHome; liftNow = liftHome; }
    applyProjection();
    room.cloudMat.uniforms.uPx.value = H * DPR * .5;
  }
  function placeCamera(az, polar, r, t) {
    camera.position.set(t.x + r * Math.sin(polar) * Math.sin(az), t.y + r * Math.cos(polar), t.z + r * Math.sin(polar) * Math.cos(az));
    camera.lookAt(t);
  }

  // ---------------------------------------------------------------- quality
  let dirEvery = 1;
  function sizeAll() { renderer.setPixelRatio(DPR); renderer.setSize(W, H, false); post.setSize(W, H, DPR); }
  const quality = createQuality((q) => {
    DPR = dprFor(q);
    const spot = lights.spot;
    const wantSpot = q < 3 && !MOBILE; if (spot.castShadow !== wantSpot) spot.castShadow = wantSpot;
    const sz = q >= 2 ? 512 : 1024; if (spot.shadow.mapSize.x !== sz) { spot.shadow.mapSize.set(sz, sz); spot.shadow.map?.dispose(); spot.shadow.map = null; }
    dirEvery = q >= 1 ? 3 : 1;
    post.setBloom(q >= 3 ? .9 : 1, q >= 3 ? .4 : (q >= 1 || MOBILE ? .6 : 1));
    // multisampling: desktop at its best tier; phones (tile-based GPUs, where it is cheap) at the top two
    sizeAll(); post.setMSAA(DPR < 2 && (q === 0 || (MOBILE && q <= 1)) ? 4 : 0);
    room.cloudMat.uniforms.uPx.value = H * DPR * .5; dirty = true;
  }, 60);

  // ---------------------------------------------------------------- pointer
  const ndc = new THREE.Vector2(0, 0); const clock = new THREE.Clock();
  let hasPointer = false, onCanvas = false, lastMove = -99, pointerDirty = false, downAt = null, hit = null, lastPick = -1, hoverKey = null, shownTag = null, userHovered = false;
  const par = { x: 0, y: 0 };
  function setNdc(e) { const r = canvas.getBoundingClientRect(); ndc.set((e.clientX - r.left) / r.width * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1); }
  on(window, 'pointermove', (e) => { setNdc(e); hasPointer = true; onCanvas = e.target === canvas; lastMove = clock.elapsedTime; pointerDirty = true; dirty = true; }, { passive: true });
  on(document, 'pointerleave', () => { onCanvas = false; pointerDirty = true; });
  on(canvas, 'pointerdown', (e) => { downAt = [e.clientX, e.clientY]; setNdc(e); hasPointer = true; onCanvas = true; lastMove = clock.elapsedTime; });
  // the hotspot under a page point; a tolerant (touch) pick that misses tries rings of points around
  // it, nearest first, since a fingertip covers more than the small objects do on a phone
  function keyAt(x, y, tolerant) {
    const at = (px, py) => { setNdc({ clientX: px, clientY: py }); const h = hs.pick(ndc, camera); return (h && h.object.userData.hs) || null; };
    const k = at(x, y);
    if (k || !tolerant) return k;
    for (const r of [12, 24]) for (let i = 0; i < 8; i++) {
      const a = i * Math.PI / 4, ring = at(x + r * Math.cos(a), y + r * Math.sin(a));
      if (ring) return ring;
    }
    return null;
  }
  on(canvas, 'pointerup', (e) => {
    if (!downAt || Math.hypot(e.clientX - downAt[0], e.clientY - downAt[1]) > 8) return; downAt = null;
    const k = keyAt(e.clientX, e.clientY, e.pointerType === 'touch');
    setNdc(e);
    if (k) { if (k !== ui.state.focusKey) ui.openCard(k); } else if (ui.state.focusKey) ui.closeCard();
  });

  // ---------------------------------------------------------------- one frame
  const ray = new THREE.Raycaster(), floorPlane = new THREE.Plane(new V3(0, 1, 0), 0);
  const aimPos = new V3(.6, 1.5, -3.2), aimGoal = new V3(), tmpV = new V3(), tmpQ = new THREE.Quaternion(), dummy = new THREE.Object3D(), tmpC = new THREE.Color();
  const frustum = new THREE.Frustum(), pv = new THREE.Matrix4();
  let blinkT = 2, scrAcc = 1, oledAcc = 1, liftMoving = true;
  room.pops.forEach((p) => { if (!REDUCED) p.g.scale.setScalar(.0001); });

  function updateIntro() {
    if (intro > 4.5) return;
    for (const p of room.pops) { const k = clamp((intro - p.delay) / .65, 0, 1); p.g.scale.setScalar(Math.max(.0001, easeOutBack(k)) * p.baseScale); }
    const e = 1 - Math.pow(1 - clamp(intro / 1.0, 0, 1), 3);
    room.world.position.y = -1.6 * (1 - e);
    room.grid.material.uniforms.uFade.value = clamp(intro / 1.2, 0, 1);
    monA.boot = clamp((intro - 1.1) / .5, 0, 1); monB.boot = clamp((intro - 1.25) / .5, 0, 1);
    const lw = clamp((intro - 1.5) / .3, 0, 1); lampOn = lw < 1 ? (lw > 0 ? (Math.sin(intro * 70) > -.2 ? lw : .1) : 0) : 1;
    if (intro < 2.6) lights.moon.shadow.needsUpdate = true;
    if (intro < 3) hs.markDirty();
  }

  function frame(raw) {
    const dt = Math.min(raw, .05); const adt = REDUCED ? 0 : dt; const now = clock.elapsedTime;
    const focusKey = ui.state.focusKey;
    T += adt; if (!held) intro += Math.min(raw, .1); frameNo++;
    if (intro >= 2.4) entered();
    updateIntro();

    // --- pointer: raycast only when the pointer moved (or a slow refresh for moving targets)
    if (hasPointer && (pointerDirty || (onCanvas && now - lastPick > .15))) { hit = hs.pick(ndc, camera); pointerDirty = false; lastPick = now; }
    const hk = hit && onCanvas && !TOUCH ? hit.object.userData.hs || null : null;
    if (hk) userHovered = true;
    const demo = !REDUCED && !STILL && !userHovered && !focusKey && intro > 2.7 && intro < 5.4;
    const showKey = hk || (demo ? 'synergy' : null);
    if (showKey !== hoverKey) { hoverKey = showKey; canvas.style.cursor = hk ? 'pointer' : 'default'; }
    const visibleTag = hoverKey && hoverKey !== focusKey ? hoverKey : null;
    if (visibleTag !== shownTag) { shownTag = visibleTag; ui.setTag(visibleTag, TOUCH ? 'tap' : 'click'); }
    post.setOutline(visibleTag, visibleTag ? hs.byKey[visibleTag].meshes : null, TOUCH);
    // held under the intro (nothing on screen): draw the lamp's motes and an outline once, so their
    // shaders are compiled now rather than stalling the entrance later
    if (held) post.setOutline('synergy', hs.byKey.synergy.meshes, false);
    const say = visibleTag === 'robot';
    ui.setTagStyle(demo && !hk, say);
    if (say) ui.setTagSub(ROBOT_LINES[Math.floor(now / 2.2) % ROBOT_LINES.length]);

    // --- lamp aim
    const idle = !hasPointer || now - lastMove > 6;
    if (intro < 2.5 && !REDUCED) { if (intro < 2.0) room.keyboardTop.getWorldPosition(aimGoal); else aimGoal.copy(room.wbFace.getWorldPosition(tmpV)); }
    else if (!idle && hit) aimGoal.copy(hit.point);
    else if (!idle) { ray.setFromCamera(ndc, camera); if (ray.ray.intersectPlane(floorPlane, aimGoal)) { aimGoal.x = clamp(aimGoal.x, XL + .1, FX1 - .1); aimGoal.z = clamp(aimGoal.z, -4.1, FZ1 - .1); } }
    else { const tt = REDUCED ? 1.3 : T; aimGoal.set(1.3 + Math.sin(tt * .35) * 1.6, .04, .5 + Math.sin(tt * .47 + 1) * 1.1); }
    aimPos.lerp(aimGoal, damp(dt, idle && intro > 2.5 ? 2 : 7));
    lights.aim.position.copy(aimPos);
    const lwp = room.lamp.getWorldPosition(tmpV); const yawGoal = Math.atan2(aimPos.x - lwp.x, aimPos.z - lwp.z);
    let dy = yawGoal - room.lampYaw.rotation.y; dy = Math.atan2(Math.sin(dy), Math.cos(dy)); room.lampYaw.rotation.y += dy * (REDUCED ? 1 : damp(dt, 10));
    room.lampHead.lookAt(aimPos);
    room.lampHead.getWorldPosition(lights.spot.position);
    const dist = lights.spot.position.distanceTo(aimPos);
    room.beam.scale.setScalar(Math.max(.5, dist - .2) / S);
    lights.spot.intensity = 85 * lampOn; room.lampBulb.material.color.setRGB(3.2, 2.5, 1.7).multiplyScalar(lampOn);
    room.beam.material.uniforms.uOp.value = .13 * lampOn; room.motes.material.opacity = .75 * lampOn; room.motes.visible = lampOn > .01 || held;
    if (!REDUCED) {
      const a = room.motes.geometry.attributes.position, sd = room.motes.userData.seeds, ta = Math.tan(.38);
      for (let i = 0; i < sd.length; i++) { const s = sd[i]; const z = .12 + ((s[0] + T * .02 * (.5 + s[2])) % 1) * .8; const ang = s[1] * 6.283 + T * .1 * (s[2] - .5); const rr = Math.sqrt(s[2]) * z * ta; a.setXYZ(i, Math.cos(ang) * rr, Math.sin(ang) * rr, z); }
      a.needsUpdate = true;
    }

    // --- robot watches the light spot; looks at you when hovered
    const happy = hoverKey === 'robot' || focusKey === 'robot';
    const head = room.robotHead;
    dummy.position.copy(head.getWorldPosition(tmpV)); dummy.lookAt(happy ? camera.position : aimPos); head.parent.getWorldQuaternion(tmpQ);
    head.quaternion.slerp(tmpQ.invert().multiply(dummy.quaternion), REDUCED ? 1 : damp(dt, 6));
    blinkT -= dt;
    const ey = blinkT < 0 ? .12 : (happy ? .45 : 1); room.eyes.forEach((e) => { e.scale.y += (ey - e.scale.y) * damp(dt, 30); });
    if (blinkT < -.13) blinkT = 2.5 + R() * 3;
    room.armR.rotation.z = happy && !REDUCED ? 2.3 + Math.sin(T * 9) * .35 : lerp(room.armR.rotation.z, .3, damp(dt, 6));
    head.userData.tip.material.color.setRGB(.2, .83 * 2.2, .6 * 2.2).multiplyScalar(.4 + .6 * (Math.sin(T * 3) > 0 ? 1 : .3));

    // --- thesis rig: the scan plane sweeps the subject, solid on one side and points on the other
    const pw = monA.boot;
    room.turntable.rotation.y += adt * .4;
    const sx = REDUCED ? 0 : room.cutLocal(T);
    const rp = room.rig.getWorldPosition(tmpV).x;
    room.cutU.value = rp + sx * RS; const bandEnd = room.cutU.value + BAND * RS;
    room.solidPlane.constant = -bandEnd; room.wireLo.constant = -room.cutU.value; room.wireHi.constant = bandEnd; room.sheet.position.x = sx;
    room.sheet.userData.mat.opacity = .14 * pw;

    // --- rack LEDs and the plant
    if (!REDUCED || frameNo < 3) {
      room.ledState.forEach((s, i) => { const lit = Math.sin(T * s.rate * 3 + s.ph) > -.2 + (s.rate > 2.5 ? .6 : 0); room.leds.setColorAt(i, tmpC.copy(s.col).multiplyScalar(lit ? 2.4 * Math.max(pw, .02) : .08)); });
      room.leds.instanceColor.needsUpdate = true;
    }
    props.animate(T, REDUCED);
    if (!REDUCED) room.leaves.forEach((l) => { l.stalk.rotation.x = l.base.x + Math.sin(T * 1.1 + l.ph) * .025; l.stalk.rotation.z = l.base.z + Math.cos(T * .9 + l.ph) * .025; });

    // --- hover lift
    liftMoving = false;
    for (const k of hs.order) {
      const h = hs.byKey[k]; if (!h.g) continue;
      const goal = k === hoverKey || k === focusKey ? 1 : 0; const prev = h.liftK;
      h.liftK += (goal - h.liftK) * (REDUCED ? 1 : damp(dt, 10)); if (Math.abs(h.liftK - goal) < 1e-3) h.liftK = goal;
      if (h.liftK !== prev) { liftMoving = true; h.boxDirty = true; }
      h.g.position.y = h.baseY + h.liftK * h.lift;
    }

    // --- camera
    const goal = focusKey && hs.byKey[focusKey].focus ? hs.byKey[focusKey].focus : HOME;
    const gt = goal === HOME ? HOME.t : goal.t();
    if (intro < INTRO_DUR && goal === HOME) {
      const e = easeInOut(clamp(intro / INTRO_DUR, 0, 1));
      cam.t.lerpVectors(INTRO_FROM.t, gt, e); cam.az = lerp(INTRO_FROM.az, goal.az, e); cam.polar = lerp(INTRO_FROM.polar, goal.polar, e); cam.r = lerp(INTRO_FROM.r, goal.r, e);
    } else {
      const k = REDUCED ? 1 : damp(dt, 2.6);
      cam.t.lerp(gt, k); cam.az = lerp(cam.az, goal.az, k); cam.polar = lerp(cam.polar, goal.polar, k); cam.r = lerp(cam.r, goal.r, k);
    }
    const pk = REDUCED || TOUCH ? 0 : 1;
    const px = hasPointer ? clamp(ndc.x - parOx, -1.5, 1.5) : 0, py = hasPointer ? clamp(ndc.y, -1.5, 1.5) : 0;
    par.x = lerp(par.x, px, damp(dt, 3)); par.y = lerp(par.y, py, damp(dt, 3));
    // framing: zoom and film offset ease towards the room's (or the clicked object's) place
    const fxCard = focusKey ? ui.frameX() : null;
    const zoomGoal = fxCard != null ? 1 : zoomHome, skewGoal = fxCard != null ? skewFor(0, 2 * fxCard - 1, 1) : skewHome, liftGoal = fxCard != null ? 0 : liftHome;
    const fk = REDUCED || frameNo < 2 ? 1 : damp(dt, 2.6);
    if (Math.abs(zoomNow - zoomGoal) > 1e-4 || Math.abs(skewNow - skewGoal) > 1e-4 || Math.abs(liftNow - liftGoal) > 1e-4 || camera.zoom !== zoomNow) {
      zoomNow = lerp(zoomNow, zoomGoal, fk); skewNow = lerp(skewNow, skewGoal, fk); liftNow = lerp(liftNow, liftGoal, fk);
      applyProjection();
    }
    const pf = focusKey ? .3 : 1;
    const breathe = REDUCED ? 0 : Math.sin(T * .16) * .025;
    placeCamera(cam.az + (par.x * .13 * pf + breathe) * pk, cam.polar + par.y * .05 * pf * pk, cam.r, cam.t);
    camera.updateMatrixWorld(); pv.multiplyMatrices(camera.projectionMatrix, camera.matrixWorldInverse); frustum.setFromProjectionMatrix(pv);

    // --- screens: ~20 Hz (30 Hz close up), skipped when off-screen
    scrAcc += dt; oledAcc += dt;
    const scrHit = hit && onCanvas && hit.object === monA.screen && hit.uv ? { x: hit.uv.x * SW, y: (1 - hit.uv.y) * SH } : null;
    if (scrHit || SYN.cursor) dirty = true;
    SYN.cursor = scrHit;
    const rate = focusKey === 'synergy' || focusKey === 'hireforge' || scrHit || hk === 'synergy' ? 30 : 20;
    const booting = monB.boot < 1 && intro > 1;
    if (scrAcc > 1 / rate && (!REDUCED || frameNo < 3 || scrHit || dirty || booting)) {
      scrAcc = 0;
      const ST = REDUCED ? 3 : T;
      if (frustum.intersectsObject(monA.screen) || booting || frameNo < 3) { SYN.step(ST); SYN.draw(monA.ct.ctx, ST); crt(monA.ct.ctx, monA.boot); monA.ct.tex.needsUpdate = true; }
      if (frustum.intersectsObject(monB.screen) || booting || frameNo < 3) { const hf = focusKey === 'hireforge'; TERM.draw(monB.ct.ctx, hf ? T - hfT0 : T, REDUCED, hf); crt(monB.ct.ctx, monB.boot); monB.ct.tex.needsUpdate = true; }
    }
    if (oledAcc > .12 && (!REDUCED || frameNo < 3)) { oledAcc = 0; room.drawOled(T); }
    lights.lightA.intensity = 6 * monA.boot; lights.lightB.intensity = 6 * monB.boot;

    // --- hover tag position (stage pixels)
    if (visibleTag) {
      tmpV.copy(hs.byKey[visibleTag].anchor()).project(camera);
      ui.placeTag((tmpV.x + 1) / 2 * W, (1 - tmpV.y) / 2 * H, W);
    }

    // --- shadows: the lamp's spot map updates every frame; the room key light less often on lower tiers
    if (frameNo % dirEvery === 0 || liftMoving) lights.moon.shadow.needsUpdate = true;

    // --- render on demand in reduced motion
    const moving = Math.abs(cam.r - goal.r) > .01 || cam.t.distanceToSquared(gt) > 1e-5 || liftMoving || Math.abs(zoomNow - zoomGoal) > 1e-3 || Math.abs(skewNow - skewGoal) > 1e-3 || Math.abs(liftNow - liftGoal) > 1e-3;
    const need = !REDUCED || dirty || moving || intro < 3 || frameNo < 4;
    if (need) { if (REDUCED) lights.moon.shadow.needsUpdate = true; dirty = false; }
    if (!REDUCED && intro > 2.6) quality.adapt(raw, now);
    if (!STILL && (!DEBUG || quality.forced) && intro > 2.6 && quality.tooSlow(raw, now)) giveUp();
    return need;
  }

  // hand the stage back to the still image (site.js disposes us on this event)
  function giveUp() {
    if (disposed) return;
    renderer.setAnimationLoop(null); running = false;
    root.dispatchEvent(new CustomEvent('workshop-still'));
  }
  on(canvas, 'webglcontextlost', (e) => { e.preventDefault(); giveUp(); });

  function loop() {
    const need = frame(clock.getDelta());
    if (disposed) return; // gave up during the frame
    if (need) post.render();
    if (root.dataset.state !== 'live') root.dataset.state = 'live';
    if (held && frameNo >= 3) updateLoop(); // live and hidden under the intro: wait for startIntro()
  }

  // ---------------------------------------------------------------- lifecycle: pause when hidden or scrolled out of view
  let running = false, inView = true;
  function updateLoop() {
    const run = inView && !paused && framesAllowed && !(held && frameNo >= 3) && !document.hidden && !disposed;
    if (run && !running) { clock.getDelta(); quality.reset(clock.elapsedTime); renderer.setAnimationLoop(loop); running = true; }
    else if (!run && running) { renderer.setAnimationLoop(null); running = false; }
  }
  on(document, 'visibilitychange', updateLoop);
  const resizeObserver = new ResizeObserver(() => { layout(); sizeAll(); hs.markDirty(); dirty = true; });
  const viewObserver = new IntersectionObserver(([e]) => { inView = e.isIntersecting; updateLoop(); });

  layout();
  quality.set(isSoftwareRenderer(renderer) ? 3 : 0); // a software rasteriser (no GPU): start lighter
  placeCamera(HOME.az, HOME.polar, HOME.r, HOME.t);
  // compile the shaders without blocking where the browser can (KHR_parallel_shader_compile)
  await renderer.compileAsync(scene, camera);
  quality.refreshHz = await measureRefresh();
  resizeObserver.observe(root);
  viewObserver.observe(root);
  updateLoop();

  if (DEBUG) {
    root.__workshop = {
      pins: () => hs.pins(camera),
      forceFps: (n) => quality.forceFps(n),
      introT: () => (held ? 0 : intro),
      frames: () => frameNo,
      // the clicked-object camera target of a hotspot, in stage pixels
      project(key) { tmpV.copy(hs.byKey[key].focus.t()).project(camera); return { x: (tmpV.x + 1) / 2 * W, y: (1 - tmpV.y) / 2 * H }; },
      // the room's outline in stage pixels
      roomRect() { const b = roomNdc(); return { left: (b.x0 + 1) / 2 * W, right: (b.x1 + 1) / 2 * W, top: (1 - b.y1) / 2 * H, bottom: (1 - b.y0) / 2 * H }; },
      // the room's width as a fraction of the last snapshot's width (css: --still-room)
      stillRoom: () => stillRoom,
      // the pins (as % of the picture) for the last snapshot
      stillPins: () => stillPins,
      tier: () => quality.level,
      setTier: (n) => quality.set(n),
      // the hotspot exactly under a page point, without the touch tolerance
      keyAt: (x, y) => keyAt(x, y, false),
      // renders the current frame at width x height and returns it as a data URL (for the still image)
      // (framed as if the whole picture were free space; css centres the picture on --frame-x)
      snapshot(width, height, type = 'image/webp', q = .8) {
        quality.set(0);
        W = width; H = height; fxOverride = .5; camera.aspect = W / H; frameHome(); zoomNow = zoomHome; skewNow = skewHome; liftNow = liftHome; applyProjection();
        renderer.setPixelRatio(1); renderer.setSize(width, height, false); post.setSize(width, height, 1);
        room.cloudMat.uniforms.uPx.value = height * .5;
        frame(1 / 60); post.render();
        const url = canvas.toDataURL(type, q);
        const b = roomNdc(); stillRoom = +((b.x1 - b.x0) / 2).toFixed(4); stillPins = hs.pins(camera);
        fxOverride = null; layout(); sizeAll();
        return url;
      },
    };
  }

  function instance() {
    return {
      firstFrame() { framesAllowed = true; updateLoop(); },
      entrance,
      startIntro() { held = false; framesAllowed = true; dirty = true; updateLoop(); },
      setPaused(p) { paused = !!p; updateLoop(); },
      dispose() {
        if (disposed) return;
        disposed = true;
        renderer.setAnimationLoop(null); running = false;
        resizeObserver.disconnect(); viewObserver.disconnect();
        listeners.forEach((off) => off());
        ui.dispose();
        post.dispose();
        disposeScene(scene);
        kit.ENV.dispose();
        renderer.dispose();
        if (!renderer.getContext().isContextLost()) renderer.forceContextLoss();
        delete root.__workshop;
        if (root.dataset.state !== 'failed') root.dataset.state = 'still';
      },
    };
  }
  return instance();
}

