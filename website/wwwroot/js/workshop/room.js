// The workshop itself: lights, the room shell and every object in it. Built once per mount;
// returns the pieces the frame loop animates.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { MeshSurfaceSampler } from 'three/addons/math/MeshSurfaceSampler.js';
import { V3, C, MONO, rnd, lerp, smooth } from './kit.js';
import { SW, SH, SS } from './screens.js';

// room shell: back wall inner face z=-4.2, left wall inner face x=-3.2, floor x[-3.2,3.95] z[-4.2,2.75]
export const XL = -3.2, FX1 = 3.95, FZ1 = 2.75;
const SZ0 = -4.5, SZ1 = 2.9, SX0 = -3.5, SX1 = 4.1;
// the studio (desk, monitors, lamp, robot) is built at desk scale and enlarged as one unit
export const S = 1.22;
// the thesis rig's scale and the width of its wireframe band
export const RS = 1.3, BAND = .12;

export function buildRoom(scene, kit, { reduced, mobile, R }) {
  const { M, E, MR, B, rbox, box, cyl, at, canvasTex, strut, paint, blob, DOT, mat, glow } = kit;
  const pops = [];
  const pop = (g, delay) => { pops.push({ g, delay, baseScale: g.scale.x }); return g; };
  const fontJobs = []; // canvases redrawn once web fonts arrive

  // ---------------------------------------------------------------- lights
  const hemi = new THREE.HemisphereLight('#7c8bc6', '#2b2220', 1.3); scene.add(hemi);
  const moon = new THREE.DirectionalLight('#b9c5ff', 1.0);
  moon.position.set(9, 15, 12); moon.castShadow = true;
  moon.shadow.mapSize.set(mobile ? 512 : 1024, mobile ? 512 : 1024);
  Object.assign(moon.shadow.camera, { left: -7.5, right: 7.5, top: 7.5, bottom: -7.5, near: 1, far: 40 });
  moon.shadow.bias = -.0006; moon.shadow.normalBias = .03; moon.shadow.autoUpdate = false;
  scene.add(moon);
  const spot = new THREE.SpotLight('#ffab5c', 0, 18, .5, .66, 1.3); // ~2700K desk lamp
  spot.castShadow = !mobile; spot.shadow.mapSize.set(1024, 1024);
  spot.shadow.bias = -.0008; spot.shadow.normalBias = .025; spot.shadow.camera.near = .1;
  scene.add(spot);
  const aim = new THREE.Object3D(); scene.add(aim); spot.target = aim;
  const lightA = new THREE.PointLight(C.cyan, 0, 5.5, 1.5), lightB = new THREE.PointLight('#8b7bff', 0, 5.5, 1.5);
  const winLight = new THREE.PointLight('#8f7dff', 6, 8, 1.4);
  const rackLight = new THREE.PointLight(C.green, 1.3, 2.6, 2);
  scene.add(lightA, lightB, winLight, rackLight);

  const world = new THREE.Group(); scene.add(world);

  // blueprint grid under the island
  const grid = new THREE.Mesh(new THREE.PlaneGeometry(120, 120).rotateX(-Math.PI / 2), new THREE.ShaderMaterial({
    transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
    uniforms: { uFade: { value: reduced ? 1 : 0 } },
    vertexShader: `varying vec3 vW; void main(){ vec4 w = modelMatrix*vec4(position,1.); vW=w.xyz; gl_Position=projectionMatrix*viewMatrix*w; }`,
    fragmentShader: `varying vec3 vW; uniform float uFade;
      void main(){ vec2 p=vW.xz; vec2 q=p/1.3; vec2 g=abs(fract(q-.5)-.5)/fwidth(q); float line=1.-min(min(g.x,g.y),1.);
        float d=length(p-vec2(.4,-.8)); float fade=exp(-d*.11); float glow=exp(-d*.16);
        vec3 c=vec3(.13,.83,.93)*line*fade*.07 + vec3(.25,.3,.55)*glow*.06;
        gl_FragColor=vec4(c*uFade,1.); }`,
  }));
  grid.position.y = -.75; grid.userData.noRay = true; grid.renderOrder = -1; scene.add(grid);

  // ---------------------------------------------------------------- room shell
  const room = new THREE.Group(); world.add(room);
  {
    const slab = at(rbox(SX1 - SX0, .7, SZ1 - SZ0, .1, mat.slabEdge, 3), (SX0 + SX1) / 2, -.35, (SZ0 + SZ1) / 2); slab.receiveShadow = true; room.add(slab);
    // planks
    const { ctx, tex } = canvasTex(1024, 1024);
    ctx.fillStyle = '#2b2528'; ctx.fillRect(0, 0, 1024, 1024);
    const pr = rnd(3);
    for (let i = 0; i < 16; i++) {
      let y = 0;
      while (y < 1024) {
        const len = 180 + pr() * 340, l = 24 + pr() * 10;
        ctx.fillStyle = `hsl(${20 + pr() * 10},${14 + pr() * 8}%,${l * .74}%)`;
        ctx.fillRect(i * 64 + 1, y + 1, 62, len - 2);
        for (let k = 0; k < 6; k++) { ctx.fillStyle = `rgba(0,0,0,${pr() * .08})`; ctx.fillRect(i * 64 + 4 + pr() * 54, y + 2, 1.5, len - 4); }
        y += len;
      }
    }
    tex.anisotropy = 8;
    const fw = FX1 - XL, fd = FZ1 + 4.2;
    const floor = new THREE.Mesh(new THREE.PlaneGeometry(fw, fd).rotateX(-Math.PI / 2), MR('#ffffff', { map: tex, roughness: .6 }));
    floor.position.set((FX1 + XL) / 2, .002, (FZ1 - 4.2) / 2); floor.receiveShadow = true; room.add(floor);

    const LZ = SZ1 - SZ0, LZC = (SZ0 + SZ1) / 2, BW = SX1 - SX0, BXC = (SX0 + SX1) / 2;
    room.add(at(box(BW, 4.5, .3, mat.wall), BXC, 2.25, -4.35), at(box(BW + .06, .07, .36, mat.cap), BXC, 4.535, -4.35));
    // left wall with a window opening
    const wx = XL - .15, fz = -2.2, fy = 2.55, z0 = fz - 1.4, z1 = fz + 1.4;
    room.add(at(box(.3, 1.6, LZ, mat.wall), wx, .8, LZC), at(box(.3, 1.0, LZ, mat.wall), wx, 4.0, LZC));
    room.add(at(box(.3, 1.9, z0 - SZ0, mat.wall), wx, 2.55, (SZ0 + z0) / 2), at(box(.3, 1.9, SZ1 - z1, mat.wall), wx, 2.55, (z1 + SZ1) / 2));
    room.add(at(box(.36, .07, LZ + .06, mat.cap), wx, 4.535, LZC));
    room.add(at(box(BW - .3, .14, .05, mat.skirt), BXC + .15, .07, -4.18), at(box(.05, .14, LZ - .3, mat.skirt), XL + .02, .07, LZC + .15));
    const strip = B(C.violet, 1.6); room.add(at(box(BW - .5, .05, .05, strip), BXC + .2, 4.2, -4.16), at(box(.05, .05, LZ - .5, strip), XL + .04, 4.2, LZC + .2));
    // window frame
    room.add(at(box(.12, .1, 2.8, mat.frame), XL + .03, fy + 1.0, fz), at(rbox(.42, .1, 2.95, .03, mat.frame), XL + .15, fy - 1.0, fz));
    room.add(at(box(.12, 2.1, .1, mat.frame), XL + .03, fy, fz - 1.35), at(box(.12, 2.1, .1, mat.frame), XL + .03, fy, fz + 1.35));
    room.add(at(box(.06, 1.9, .05, mat.frame), XL, fy, fz), at(box(.06, .05, 2.6, mat.frame), XL, fy + .15, fz));
    // night sky with a city skyline
    const sky = canvasTex(512, 384); const s = sky.ctx;
    const gr = s.createLinearGradient(0, 0, 0, 384);
    gr.addColorStop(0, '#090a24'); gr.addColorStop(.45, '#24185a'); gr.addColorStop(.78, '#5a3488'); gr.addColorStop(1, '#a8579a');
    s.fillStyle = gr; s.fillRect(0, 0, 512, 384);
    const sr = rnd(11);
    for (let i = 0; i < 90; i++) { s.fillStyle = `rgba(255,255,255,${.2 + sr() * .6})`; const r = sr() * 1.3 + .3; s.beginPath(); s.arc(sr() * 512, sr() * 230, r, 0, 7); s.fill(); }
    const mg = s.createRadialGradient(370, 90, 0, 370, 90, 70); mg.addColorStop(0, 'rgba(255,240,220,.35)'); mg.addColorStop(1, 'rgba(255,240,220,0)');
    s.fillStyle = mg; s.fillRect(280, 0, 200, 200);
    s.fillStyle = '#fff4e6'; s.beginPath(); s.arc(370, 90, 22, 0, 7); s.fill();
    s.fillStyle = '#2a1f5c'; s.beginPath(); s.arc(380, 84, 19, 0, 7); s.fill();
    let x = 0; const bx = rnd(5);
    while (x < 512) {
      const w = 26 + bx() * 46, h = 50 + bx() * 120; s.fillStyle = '#0b0b1f'; s.fillRect(x, 384 - h, w, h);
      for (let yy = 384 - h + 8; yy < 380; yy += 11) for (let xx = x + 5; xx < x + w - 5; xx += 9) if (bx() < .22) { s.fillStyle = bx() < .7 ? 'rgba(255,200,130,.85)' : 'rgba(34,211,238,.8)'; s.fillRect(xx, yy, 4, 5); }
      x += w + 2;
    }
    sky.tex.needsUpdate = true;
    room.add(at(new THREE.Mesh(new THREE.PlaneGeometry(2.7, 1.95).rotateY(Math.PI / 2), B('#ffffff', 1.05, { map: sky.tex })), XL - .2, fy, fz));
    winLight.position.set(XL + .8, 2.7, fz);
    // pinboard (left wall, above the rack)
    const pin = new THREE.Group(); pin.position.set(XL + .03, 3.2, .35); room.add(pin);
    pin.add(rbox(.05, .8, 1.15, .02, mat.woodLight));
    [[C.cyan, -.32, .15, .1], [C.violet, .05, .2, -.08], [C.green, .33, -.05, .06], ['#fbbf24', -.1, -.2, -.05], ['#f9a8d4', .28, .23, .12]].forEach(([col, z, y, r]) => {
      const n = new THREE.Mesh(new THREE.PlaneGeometry(.22, .22).rotateY(Math.PI / 2), M(col, { roughness: .9 })); n.position.set(.03, y, z); n.rotation.x = r; pin.add(n);
    });
  }

  // ---------------------------------------------------------------- framed degree (left wall, front section)
  const certificate = new THREE.Group(); certificate.position.set(XL + .02, 2.7, 1.85); certificate.rotation.y = Math.PI / 2; world.add(certificate);
  {
    certificate.add(rbox(1.0, .74, .05, .015, mat.woodDark));
    const dc = canvasTex(320, 236); const d = dc.ctx;
    const drawDeg = () => {
      d.fillStyle = '#ece6d8'; d.fillRect(0, 0, 320, 236); d.strokeStyle = '#b39a62'; d.lineWidth = 4; d.strokeRect(12, 12, 296, 212);
      d.fillStyle = '#2b2b3a'; d.textAlign = 'center'; d.font = `700 18px ${MONO}`; d.fillText('UNIVERSITY OF LEEDS', 160, 62);
      d.font = '700 34px Inter, sans-serif'; d.fillText('MEng', 160, 112); d.font = `500 15px ${MONO}`; d.fillText('Computer Science & AI', 160, 142);
      d.fillStyle = '#8a6d2f'; d.font = `700 15px ${MONO}`; d.fillText('FIRST-CLASS HONOURS', 160, 180);
      d.fillStyle = '#b8904a'; d.beginPath(); d.arc(266, 196, 13, 0, 7); d.fill(); dc.tex.needsUpdate = true;
    };
    drawDeg(); fontJobs.push(drawDeg);
    certificate.add(at(new THREE.Mesh(new THREE.PlaneGeometry(.88, .64), M('#ffffff', { map: dc.tex, roughness: .7 })), 0, 0, .027));
  }

  // ---------------------------------------------------------------- the studio: desk, monitors, lamp and robot
  const studio = new THREE.Group(); studio.position.set(-.25, 0, -4.2); studio.scale.setScalar(S); world.add(studio);
  const desk = new THREE.Group(); desk.position.set(0, 0, .75); studio.add(desk); pop(desk, .25);
  const keyboardTop = new THREE.Object3D();
  {
    desk.add(at(rbox(4.4, .1, 1.5, .035, mat.wood, 3), 0, 1.4, 0));
    for (const z of [-.62, .62]) desk.add(at(box(.07, 1.35, .07, mat.metal), -2.05, .675, z));
    desk.add(at(box(.05, .05, 1.3, mat.metal), -2.05, .2, 0));
    desk.add(at(rbox(.92, 1.34, 1.34, .03, mat.woodDark), 1.62, .67, 0));
    for (let i = 0; i < 3; i++) { desk.add(at(rbox(.84, .38, .02, .01, mat.wood), 1.62, .26 + i * .43, .68)); desk.add(at(rbox(.26, .03, .03, .012, mat.metal), 1.62, .38 + i * .43, .7)); }
    desk.add(blob(4.9, 2.0, .006));
    // keyboard
    desk.add(at(rbox(1.15, .04, .38, .015, mat.dark), .05, 1.47, .4));
    const keyGeo = rbox(.07, .025, .07, .01, mat.dark, 1).geometry;
    const keys = new THREE.InstancedMesh(keyGeo, M('#262d3c', { roughness: .5 }), 60);
    let k = 0; const mtx = new THREE.Matrix4();
    for (let r = 0; r < 4; r++) for (let q = 0; q < 15; q++) { mtx.makeTranslation(.05 - .53 + q * .0757, 1.495, .4 - .12 + r * .08); keys.setMatrixAt(k++, mtx); }
    desk.add(keys);
    keyboardTop.position.set(.05, 1.5, .4); desk.add(keyboardTop);
    desk.add(at(rbox(.12, .035, .19, .03, mat.dark), .85, 1.47, .42));
    desk.add(at(blob(1.5, .7, 1.452, true), .05, 0, .4));
    // mug and notebook
    const mug = new THREE.Group(); mug.position.set(-1.25, 1.45, .45); desk.add(mug);
    mug.add(at(cyl(.08, .07, .19, M('#e7e2d8', { roughness: .4 }), 18), 0, .095, 0));
    mug.add(at(cyl(.07, .07, .01, M('#2b1a10', { roughness: .2 }), 16), 0, .18, 0));
    const handle = new THREE.Mesh(new THREE.TorusGeometry(.05, .014, 6, 12, Math.PI), M('#e7e2d8')); handle.position.set(.08, .1, 0); handle.rotation.z = -Math.PI / 2; mug.add(handle);
    mug.add(blob(.3, .3, .004, true));
    const nb = at(rbox(.42, .03, .55, .01, M('#1f3b4d')), -.72, 1.465, .45); nb.rotation.y = .25; desk.add(nb);
    const pen = at(box(.012, .012, .4, M('#e5e7eb')), -.67, 1.49, .45); pen.rotation.y = .4; desk.add(pen);
  }

  // ---------------------------------------------------------------- monitors with live canvas screens
  function makeMonitor(x, rotY) {
    const g = new THREE.Group(); g.position.set(x, 1.45, .42); g.rotation.y = rotY;
    g.add(at(rbox(.52, .035, .3, .015, mat.metal), 0, .02, .02));
    g.add(at(box(.07, .52, .05, mat.metal), 0, .28, -.07));
    g.add(at(rbox(1.62, 1.0, .06, .025, mat.bezel), 0, .97, -.02));
    g.add(at(rbox(.7, .45, .08, .03, mat.bezel), 0, .97, -.08));
    g.add(blob(.85, .55, .004, true));
    const ct = canvasTex(SW * SS, SH * SS); ct.ctx.setTransform(SS, 0, 0, SS, 0, 0);
    const screen = at(new THREE.Mesh(new THREE.PlaneGeometry(1.53, .93), B('#ffffff', 1.32, { map: ct.tex })), 0, .97, .0115);
    g.add(screen);
    g.add(at(new THREE.Mesh(new THREE.SphereGeometry(.008, 6, 4), glow.green), .74, .49, .012));
    studio.add(g);
    return { g, screen, ct, boot: reduced ? 1 : 0 };
  }
  const monA = makeMonitor(-.84, .16), monB = makeMonitor(.84, -.16);
  pop(monA.g, .5); pop(monB.g, .58);
  lightA.position.set(-1.3, 2.75, -2.75); lightB.position.set(.8, 2.75, -2.75);

  // ---------------------------------------------------------------- desk lamp, aims at the cursor
  const lamp = new THREE.Group(); lamp.position.set(-2.05, 1.45, .25); studio.add(lamp); pop(lamp, .62);
  const lampYaw = new THREE.Group(); lampYaw.userData.dyn = true; lamp.add(lampYaw);
  const lampHead = new THREE.Group(); lampHead.userData.dyn = true;
  let lampBulb, beam, motes;
  {
    lamp.add(at(cyl(.18, .2, .05, mat.cream, 24), 0, .025, 0));
    lamp.add(blob(.6, .6, .004));
    const p0 = new V3(0, .07, 0), p1 = new V3(0, .78, -.26), p2 = new V3(0, 1.1, .3);
    lampYaw.add(at(new THREE.Mesh(new THREE.SphereGeometry(.045, 12, 8), mat.metal), p0.x, p0.y, p0.z));
    lampYaw.add(strut(p0, p1, .019, mat.cream), strut(p1, p2, .017, mat.cream));
    lampYaw.add(strut(p0.clone().add(new V3(.04, 0, 0)), p1.clone().add(new V3(.04, 0, 0)), .006, mat.metal));
    lampYaw.add(strut(p1.clone().add(new V3(.04, 0, 0)), p2.clone().add(new V3(.04, 0, 0)), .006, mat.metal));
    lampYaw.add(at(new THREE.Mesh(new THREE.SphereGeometry(.04, 12, 8), mat.metal), p1.x, p1.y, p1.z));
    lampHead.position.copy(p2); lampYaw.add(lampHead);
    lampHead.add(new THREE.Mesh(new THREE.SphereGeometry(.035, 10, 8), mat.metal));
    lampHead.add(new THREE.Mesh(new THREE.ConeGeometry(.17, .3, 28, 1, true).rotateX(-Math.PI / 2).translate(0, 0, .12), M('#e4ddcf', { side: THREE.DoubleSide, roughness: .45 })));
    lampHead.add(at(new THREE.Mesh(new THREE.SphereGeometry(.06, 14, 10), mat.cream), 0, 0, -.03));
    lampBulb = at(new THREE.Mesh(new THREE.SphereGeometry(.065, 16, 10), B('#ffd9a6', 0)), 0, 0, .19); lampHead.add(lampBulb);
    const bg = new THREE.ConeGeometry(Math.tan(.42), 1, 40, 1, true).translate(0, -.5, 0).rotateX(-Math.PI / 2);
    beam = new THREE.Mesh(bg, new THREE.ShaderMaterial({
      transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
      uniforms: { uOp: { value: 0 }, uCol: { value: new THREE.Color('#ffb066') } },
      vertexShader: `varying float vZ; varying vec3 vN; varying vec3 vV;
        void main(){ vZ=position.z; vec4 mv=modelViewMatrix*vec4(position,1.); vV=normalize(-mv.xyz); vN=normalize(normalMatrix*normal); gl_Position=projectionMatrix*mv; }`,
      fragmentShader: `varying float vZ; varying vec3 vN; varying vec3 vV; uniform float uOp; uniform vec3 uCol;
        void main(){ float e=pow(abs(dot(normalize(vN),normalize(vV))),2.); float f=smoothstep(0.,.1,vZ)*(1.-smoothstep(.2,1.,vZ));
          gl_FragColor=vec4(uCol*uOp*e*f,1.); }`,
    }));
    beam.position.z = .2; beam.userData.noRay = true; beam.renderOrder = 5; lampHead.add(beam);
    const N = 70, pos = new Float32Array(N * 3), seeds = [];
    for (let i = 0; i < N; i++) seeds.push([R(), R(), R()]);
    const mg = new THREE.BufferGeometry(); mg.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    motes = new THREE.Points(mg, new THREE.PointsMaterial({ size: .045, map: DOT, color: '#ffd2a0', transparent: true, opacity: 0, depthWrite: false, blending: THREE.AdditiveBlending }));
    motes.userData = { seeds, noRay: true }; motes.frustumCulled = false; beam.add(motes);
  }

  // ---------------------------------------------------------------- robot (the on-site assistant)
  const robot = new THREE.Group(); robot.position.set(1.88, 1.45, 1.12); robot.rotation.y = -.5; studio.add(robot); pop(robot, .95);
  const robotHead = new THREE.Group(); robotHead.userData.dyn = true; const eyes = [];
  const armR = new THREE.Group(); armR.userData.dyn = true;
  {
    robot.add(blob(.62, .55, .004));
    robot.add(at(rbox(.36, .08, .3, .035, mat.dark), 0, .04, 0));
    for (const z of [-.1, .1]) { const tr = at(cyl(.045, .045, .38, mat.metal, 12), 0, .045, z); tr.rotation.z = Math.PI / 2; robot.add(tr); }
    robot.add(at(rbox(.32, .27, .25, .07, mat.robot, 3), 0, .215, 0));
    robot.add(at(cyl(.035, .035, .012, glow.cyan, 16).rotateX(Math.PI / 2), 0, .23, .126));
    robot.add(at(cyl(.04, .04, .06, mat.metal, 10), 0, .37, 0));
    const armL = new THREE.Mesh(new THREE.CapsuleGeometry(.035, .12, 4, 8), mat.robot); armL.position.set(-.2, .2, 0); armL.rotation.z = -.3; robot.add(armL);
    armR.position.set(.18, .27, 0); robot.add(armR);
    const ar = new THREE.Mesh(new THREE.CapsuleGeometry(.035, .12, 4, 8), mat.robot); ar.position.set(.02, -.08, 0); armR.add(ar); armR.rotation.z = .3;
    robotHead.position.y = .4; robot.add(robotHead);
    robotHead.add(at(rbox(.4, .27, .3, .08, mat.robot, 3), 0, .135, 0));
    robotHead.add(at(rbox(.31, .17, .02, .04, mat.dark), 0, .14, .148));
    for (const x of [-.07, .07]) { const e = at(rbox(.05, .075, .012, .022, glow.cyan), x, .145, .16); eyes.push(e); robotHead.add(e); }
    robotHead.add(at(cyl(.006, .006, .13, mat.metal, 6), .09, .33, 0));
    const tip = at(new THREE.Mesh(new THREE.SphereGeometry(.026, 10, 8), B(C.green, 2.2)), .09, .4, 0); robotHead.add(tip); robotHead.userData.tip = tip;
    for (const x of [-.205, .205]) robotHead.add(at(cyl(.04, .04, .03, mat.metal, 12).rotateZ(Math.PI / 2), x, .14, 0));
  }

  // ---------------------------------------------------------------- bookcase: projects and experience on labelled spines
  const bookcase = new THREE.Group(); bookcase.position.set(3.22, 0, -3.92); world.add(bookcase); pop(bookcase, .72);
  {
    const Wb = 1.24, Hb = 2.78, Db = .5, t = .05;
    for (const sx of [-1, 1]) bookcase.add(at(rbox(t, Hb, Db, .012, mat.woodDark), sx * (Wb / 2 - t / 2), Hb / 2, 0));
    bookcase.add(at(rbox(Wb, t, Db, .012, mat.woodDark), 0, Hb - t / 2, 0));
    bookcase.add(at(box(Wb - 2 * t, Hb - t, .02, M('#241c18')), 0, (Hb - t) / 2, -Db / 2 + .012));
    for (const y of [.06, .74, 1.42, 2.1]) bookcase.add(at(rbox(Wb - 2 * t, .04, Db - .03, .01, mat.wood), 0, y, .01));
    bookcase.add(blob(1.7, .95, .008));
    const titles = ['SYNERGY', 'HIREFORGE', '4D RECON', 'STENT SIM', 'pNanoLocz', 'THIS SITE', 'AZURE', 'LOCAL LLMs', 'MEng CS+AI', 'LEEDS'];
    const cols = ['#155e75', '#4c3a8a', '#1f5c50', '#6b4330', '#2e3a5c', '#5a2b58', '#1e4a7a', '#335a2f', '#6b5822', '#3a4058'];
    const N = titles.length, CW = 72, CH = 320;
    const sp = canvasTex(CW * N, CH);
    const drawSpines = () => {
      const s = sp.ctx;
      titles.forEach((tt, i) => {
        const x = i * CW; s.fillStyle = cols[i]; s.fillRect(x, 0, CW, CH);
        s.fillStyle = 'rgba(255,255,255,.16)'; s.fillRect(x, 16, CW, 4); s.fillRect(x, CH - 20, CW, 4);
        s.fillStyle = 'rgba(0,0,0,.2)'; s.fillRect(x, 0, 6, CH);
        s.save(); s.translate(x + CW / 2 + 8, CH / 2); s.rotate(-Math.PI / 2);
        s.fillStyle = '#f3ede0'; s.font = `700 25px ${MONO}`; s.textAlign = 'center'; s.fillText(tt, 0, 0); s.restore();
      });
      sp.tex.needsUpdate = true;
    };
    drawSpines(); fontJobs.push(drawSpines);
    const spineMat = M('#ffffff', { map: sp.tex, roughness: .75 });
    const br = rnd(21);
    [[1.44, 0], [2.12, 5]].forEach(([y0, first]) => {
      let x = -.53;
      for (let i = first; i < first + 5; i++) {
        const w = .15 + br() * .04, h = .5 + br() * .1, d = .34;
        const b = paint(at(rbox(w, h, d, .012, mat.books), x + w / 2, y0 + h / 2, .03), cols[i]);
        const pg = new THREE.PlaneGeometry(w * .92, h * .96); const uv = pg.attributes.uv;
        const u0 = i / N, u1 = (i + 1) / N; uv.setXY(0, u0, 1); uv.setXY(1, u1, 1); uv.setXY(2, u0, 0); uv.setXY(3, u1, 0);
        bookcase.add(b, at(new THREE.Mesh(pg, spineMat), x + w / 2, y0 + h / 2, .03 + d / 2 + .002));
        x += w + .012;
      }
      const lean = paint(at(rbox(.13, .5, .32, .012, mat.books), x + .16, y0 + .24, .03), y0 < 2 ? '#2a3550' : '#4a2a2a'); lean.rotation.z = -.3; bookcase.add(lean);
    });
    // middle shelf: storage crates; bottom shelf: a stack of lab notebooks
    bookcase.add(paint(at(rbox(.5, .4, .4, .03, mat.books), -.27, .96, .02), '#2c3a52'), paint(at(rbox(.46, .34, .4, .03, mat.books), .28, .93, .02), '#4a3a2e'));
    bookcase.add(at(box(.2, .06, .005, B(C.cyan, 1.4)), -.27, 1.0, .225));
    for (let i = 0; i < 4; i++) { const nbk = paint(at(rbox(.56, .07, .38, .01, mat.books), -.18 + (i % 2) * .03, .12 + i * .075, .02), ['#1f3b4d', '#3b2f5a', '#1f4d45', '#5b3a2a'][i]); nbk.rotation.y = (i % 2 ? .06 : -.05); bookcase.add(nbk); }
    // graduation cap and trophy on top
    const cap = new THREE.Group(); cap.position.set(-.25, Hb, .02); bookcase.add(cap);
    cap.add(at(cyl(.11, .12, .09, M('#151821'), 18), 0, .045, 0));
    const board = at(box(.3, .018, .3, M('#151821')), 0, .1, 0); board.rotation.y = .6; cap.add(board);
    const gold = M('#d4a54a', { metalness: .8, roughness: .25, ...E(1) });
    cap.add(at(new THREE.Mesh(new THREE.SphereGeometry(.016, 8, 6), gold), 0, .112, 0));
    cap.add(strut(new V3(0, .11, 0), new V3(.13, .1, .08), .004, gold), strut(new V3(.13, .1, .08), new V3(.13, 0, .08), .006, gold));
    const tro = new THREE.Group(); tro.position.set(.3, Hb, .02); bookcase.add(tro);
    tro.add(at(box(.12, .05, .12, M('#151821')), 0, .025, 0), at(cyl(.015, .015, .1, gold, 8), 0, .1, 0), at(cyl(.07, .03, .1, gold, 16), 0, .2, 0));
  }

  // ---------------------------------------------------------------- server rack (home lab, local LLMs)
  const rack = new THREE.Group(); rack.position.set(-2.72, 0, .3); rack.rotation.y = Math.PI / 2; world.add(rack); pop(rack, .45);
  const ledState = [];
  let leds, oled;
  {
    rack.add(blob(1.6, 1.6, .008));
    rack.add(at(rbox(.92, 2.15, .92, .04, M('#141a29', { roughness: .5, metalness: .3 })), 0, 1.075, 0));
    rack.add(at(rbox(.96, .06, .96, .02, mat.metal), 0, 2.17, 0));
    const unit = M('#1e2637', { roughness: .45, metalness: .35 });
    const NU = 6, per = 8; leds = new THREE.InstancedMesh(new THREE.BoxGeometry(.034, .022, .012), new THREE.MeshBasicMaterial({ color: '#ffffff' }), NU * per);
    const m4 = new THREE.Matrix4(); let k = 0;
    for (let u = 0; u < NU; u++) {
      const y = .35 + u * .29;
      rack.add(at(rbox(.82, .24, .03, .012, unit), 0, y, .462));
      for (let v = 0; v < 5; v++) rack.add(at(box(.08, .16, .01, mat.dark), -.33 + v * .1, y, .48));
      for (let l = 0; l < per; l++) { m4.makeTranslation(.12 + (l % 4) * .065, y + (l < 4 ? .05 : -.04), .48); leds.setMatrixAt(k, m4); ledState.push({ ph: R() * 10, rate: .5 + R() * 3, col: new THREE.Color(R() < .7 ? C.green : (R() < .6 ? C.cyan : C.violet)) }); leds.setColorAt(k, new THREE.Color(0, 0, 0)); k++; }
    }
    leds.userData.noRay = true; rack.add(leds);
    oled = canvasTex(256, 96);
    rack.add(at(new THREE.Mesh(new THREE.PlaneGeometry(.5, .19), B('#ffffff', 1.25, { map: oled.tex })), -.05, 2.0, .465));
    rack.add(at(rbox(.6, .25, .02, .01, mat.dark), -.05, 2.0, .455));
    rackLight.position.set(-1.9, 1.2, .3);
  }
  function drawOled(T) {
    const s = oled.ctx; s.fillStyle = '#05070c'; s.fillRect(0, 0, 256, 96);
    s.font = `600 17px ${MONO}`; s.fillStyle = '#34d399'; s.fillText('● llm-node  ready', 14, 32);
    const tps = 38 + Math.round(Math.sin(T * 1.7) * 4 + Math.sin(T * 4.3) * 2);
    s.fillStyle = '#a7f3d0'; s.font = `500 15px ${MONO}`; s.fillText(`${tps} tok/s  · gpu 71%`, 14, 62);
    s.fillStyle = '#123'; s.fillRect(14, 74, 228, 6); s.fillStyle = '#22d3ee'; s.fillRect(14, 74, 228 * (.55 + .2 * Math.sin(T * .9)), 6);
    oled.tex.needsUpdate = true;
  }

  // ---------------------------------------------------------------- plant (under the window)
  const plant = new THREE.Group(); plant.position.set(-2.72, 0, -1.55); world.add(plant); pop(plant, .8);
  const leaves = [];
  {
    plant.add(blob(1.0, 1.0, .008));
    plant.add(at(cyl(.3, .22, .56, mat.pot, 14), 0, .28, 0));
    const rim = new THREE.Mesh(new THREE.TorusGeometry(.3, .03, 6, 18).rotateX(Math.PI / 2), mat.pot); rim.position.y = .56; plant.add(rim);
    plant.add(at(cyl(.28, .28, .02, mat.soil, 14), 0, .54, 0));
    const greens = ['#1f8a5e', '#2aa572', '#167048', '#34b37e'].map((c) => M(c, { flatShading: true, roughness: .7 }));
    const stem = M('#1b5e3f');
    const lg = new THREE.SphereGeometry(1, 7, 5);
    for (let i = 0; i < 11; i++) {
      const a = i / 11 * Math.PI * 2 + R() * .4, tilt = .25 + R() * .75, len = .55 + R() * .55;
      const stalk = new THREE.Group(); stalk.userData.dyn = true; stalk.position.y = .55; plant.add(stalk);
      stalk.rotation.set(Math.sin(a) * tilt, 0, -Math.cos(a) * tilt, 'XYZ');
      stalk.add(at(cyl(.008, .012, len, stem, 5), 0, len / 2, 0));
      const leaf = new THREE.Mesh(lg, greens[i % 4]);
      leaf.scale.set(.15, .26, .035); leaf.position.y = len + .16; leaf.rotation.x = .4 * (R() - .5); stalk.add(leaf);
      leaves.push({ stalk, base: stalk.rotation.clone(), ph: R() * 6 });
    }
  }

  // ---------------------------------------------------------------- thesis rig: a turntable whose subject is reconstructed into a point cloud
  const rig = new THREE.Group(); rig.position.set(-1.3, 0, 1.45); rig.scale.setScalar(RS); world.add(rig); pop(rig, .88);
  // cloud (x < cut) | wireframe band (cut .. cut+BAND) | shaded surface (x > cut+BAND)
  const solidPlane = new THREE.Plane(new V3(1, 0, 0), 0), wireLo = new THREE.Plane(new V3(1, 0, 0), 0), wireHi = new THREE.Plane(new V3(-1, 0, 0), 0);
  const cutU = { value: 0 };
  const turntable = new THREE.Group(); turntable.userData.dyn = true;
  const sheet = new THREE.Group(); sheet.userData.dyn = true;
  let cloudMat;
  {
    rig.add(blob(1.3, 1.3, .006));
    rig.add(at(cyl(.3, .34, .04, mat.metal, 24), 0, .02, 0));
    rig.add(at(cyl(.035, .035, .86, mat.metal, 10), 0, .45, 0));
    rig.add(at(cyl(.6, .6, .06, mat.wood, 32), 0, .9, 0));
    turntable.position.y = .93; rig.add(turntable);
    turntable.add(at(cyl(.46, .48, .05, M('#141925', { roughness: .4 }), 32), 0, .025, 0));
    const ring = new THREE.Mesh(new THREE.TorusGeometry(.465, .007, 6, 64).rotateX(Math.PI / 2), glow.cyan); ring.position.y = .05; turntable.add(ring);
    const kg = new THREE.TorusKnotGeometry(.2, .066, 160, 16, 2, 3);
    const knotMat = new THREE.MeshStandardMaterial({ color: '#aab4c6', roughness: .38, metalness: .15, ...E(.6), side: THREE.DoubleSide, clippingPlanes: [solidPlane], clipShadows: true });
    knotMat.onBeforeCompile = (sh) => {
      sh.uniforms.uCut = cutU;
      sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vWK;').replace('#include <project_vertex>', '#include <project_vertex>\nvWK = (modelMatrix * vec4(transformed, 1.0)).xyz;');
      sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\nvarying vec3 vWK; uniform float uCut;')
        .replace('#include <color_fragment>', '#include <color_fragment>\nif (!gl_FrontFacing) diffuseColor.rgb *= .18;')
        .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>\n{ float e = 1. - smoothstep(0., .025, vWK.x - uCut - ${(BAND * RS).toFixed(3)}); totalEmissiveRadiance += vec3(.13,.83,.93) * e * 1.6; }`);
    };
    knotMat.customProgramCacheKey = () => 'knot';
    const knot = new THREE.Mesh(kg, knotMat); knot.position.y = .4; turntable.add(knot);
    const wire = new THREE.Mesh(new THREE.TorusKnotGeometry(.2, .066, 90, 8, 2, 3), new THREE.MeshBasicMaterial({ color: new THREE.Color(C.cyan).multiplyScalar(1.1), wireframe: true, transparent: true, opacity: .75, clippingPlanes: [wireLo, wireHi] }));
    wire.position.y = .4; wire.scale.setScalar(1.006); wire.userData.noRay = true; turntable.add(wire);
    // sampled point cloud, with normals so points can "snap" onto the surface as the scan plane passes
    const sampler = new MeshSurfaceSampler(new THREE.Mesh(kg)).build();
    const NP = mobile ? 2200 : 4200, pp = new Float32Array(NP * 3), pn = new Float32Array(NP * 3), pc = new Float32Array(NP * 3), prr = new Float32Array(NP);
    const tmp = new V3(), nrm = new V3(), col = new THREE.Color();
    for (let i = 0; i < NP; i++) {
      sampler.sample(tmp, nrm); pp.set([tmp.x, tmp.y, tmp.z], i * 3); pn.set([nrm.x, nrm.y, nrm.z], i * 3); prr[i] = R();
      const q = R(); col.set(q < .8 ? C.cyan : (q < .93 ? C.violet : '#ffffff')); pc.set([col.r, col.g, col.b], i * 3);
    }
    const cg = new THREE.BufferGeometry();
    cg.setAttribute('position', new THREE.BufferAttribute(pp, 3)); cg.setAttribute('normal', new THREE.BufferAttribute(pn, 3));
    cg.setAttribute('aCol', new THREE.BufferAttribute(pc, 3)); cg.setAttribute('aRnd', new THREE.BufferAttribute(prr, 1));
    cloudMat = new THREE.ShaderMaterial({
      transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
      uniforms: { uCut: cutU, uPx: { value: 400 }, uSize: { value: .026 }, uMap: { value: DOT }, uOn: { value: 1 } },
      vertexShader: `attribute vec3 aCol; attribute float aRnd; uniform float uCut, uPx, uSize; varying vec3 vC; varying float vA;
        void main(){
          vec4 wp = modelMatrix * vec4(position, 1.);
          float d = uCut - wp.x;                       // > 0 on the reconstructed side
          float k = 1. - smoothstep(0., .16, d);       // fresh points near the scan plane
          vec3 p = position + normal * k * (.05 + .09 * aRnd);
          vec4 mv = modelViewMatrix * vec4(p, 1.);
          gl_Position = projectionMatrix * mv;
          float vis = step(0., d);
          gl_PointSize = vis * uSize * uPx / -mv.z * (1. + k * 1.3);
          vC = mix(aCol * .95, vec3(1.5), k * .75); vA = vis;
        }`,
      fragmentShader: `uniform sampler2D uMap; uniform float uOn; varying vec3 vC; varying float vA;
        void main(){ if (vA < .5) discard; float a = texture2D(uMap, gl_PointCoord).r; gl_FragColor = vec4(vC * a * uOn, 1.); }`,
    });
    const cloud = new THREE.Points(cg, cloudMat); cloud.position.y = .4; cloud.userData.noRay = true; cloud.frustumCulled = false; turntable.add(cloud);
    // laser sheet
    sheet.position.y = 1.36; rig.add(sheet);
    const sm = new THREE.Mesh(new THREE.PlaneGeometry(1.0, .9).rotateY(Math.PI / 2), new THREE.MeshBasicMaterial({ color: new THREE.Color(C.cyan).multiplyScalar(.7), transparent: true, opacity: .14, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide }));
    sm.userData.noRay = true; sheet.add(sm); sheet.userData.mat = sm.material;
    for (const z of [.5, -.5]) { const sl = at(new THREE.Mesh(new THREE.BoxGeometry(.006, .9, .006), glow.cyan), 0, 0, z); sl.userData.noRay = true; sheet.add(sl); }
    const st = at(new THREE.Mesh(new THREE.BoxGeometry(.006, .006, 1.0), glow.cyan), 0, .45, 0); st.userData.noRay = true; sheet.add(st);
    // tripod camera front-left, looking at the subject
    const tp = new THREE.Group(); tp.position.set(-.82, 0, .5); rig.add(tp);
    tp.add(blob(.8, .8, .006, true));
    const head = new V3(0, 1.32, 0);
    for (let i = 0; i < 3; i++) { const a = i / 3 * Math.PI * 2 + .4; tp.add(strut(new V3(Math.cos(a) * .32, 0, Math.sin(a) * .32), head, .012, mat.metal, 6)); }
    const rigCam = new THREE.Group(); rigCam.position.copy(head).add(new V3(0, .07, 0)); tp.add(rigCam);
    rigCam.add(rbox(.18, .12, .22, .03, mat.dark));
    rigCam.add(at(cyl(.05, .055, .12, mat.metal, 16).rotateX(Math.PI / 2), 0, 0, .15));
    rigCam.add(at(new THREE.Mesh(new THREE.SphereGeometry(.012, 8, 6), glow.red), .06, .045, .06));
    world.updateMatrixWorld(true);
    rigCam.lookAt(rig.localToWorld(new V3(0, 1.3, 0)));
    rig.updateMatrixWorld(true);
    const lens = rigCam.localToWorld(new V3(0, 0, .21)); rig.worldToLocal(lens);
    const pts = []; for (const [y, z] of [[.98, .5], [.98, -.5], [1.75, .5], [1.75, -.5]]) pts.push(lens.clone(), new V3(0, y, z));
    const fl = new THREE.LineSegments(new THREE.BufferGeometry().setFromPoints(pts), new THREE.LineBasicMaterial({ color: C.cyan, transparent: true, opacity: .3 }));
    fl.userData.noRay = true; rig.add(fl);
  }
  // scan-plane keyframes: sweep the full width, pausing at the 50/50 split
  const SCAN = [[0, -.6], [1.5, -.06], [2.5, -.06], [4.0, .42], [4.4, .42], [5.9, -.06], [6.9, -.06], [8.4, -.6], [8.8, -.6]];
  function cutLocal(t) {
    const tt = t % 8.8;
    for (let i = 1; i < SCAN.length; i++) if (tt <= SCAN[i][0]) { const [t0, a] = SCAN[i - 1], [t1, b] = SCAN[i]; return lerp(a, b, smooth((tt - t0) / (t1 - t0))); }
    return -.6;
  }

  // ---------------------------------------------------------------- rug: a hand-tufted map of my projects, seen from above
  const rugG = new THREE.Group(); world.add(rugG); pop(rugG, .7);
  {
    const RUG = { x: 1.3, z: .5, w: 3.6, d: 2.6 };
    const cw = 1024, ch = Math.round(1024 * RUG.d / RUG.w);
    const { ctx: s, tex } = canvasTex(cw, ch);
    const rr = 46;
    const drawRug = () => {
      s.clearRect(0, 0, cw, ch);
      s.save(); s.beginPath(); s.roundRect(3, 3, cw - 6, ch - 6, rr); s.clip();
      s.fillStyle = '#182035'; s.fillRect(0, 0, cw, ch);
      const nr = rnd(41);
      for (let i = 0; i < 11000; i++) { s.fillStyle = nr() < .5 ? 'rgba(255,255,255,.035)' : 'rgba(0,0,0,.11)'; s.fillRect(nr() * cw, nr() * ch, 1 + nr() * 3, 1.2); }
      // borders: a dark binding, a teal stripe and a thin brass stripe with a woven tick pattern between them
      s.strokeStyle = '#0d1220'; s.lineWidth = 52; s.beginPath(); s.roundRect(0, 0, cw, ch, rr); s.stroke();
      s.strokeStyle = 'rgba(96,170,184,.55)'; s.lineWidth = 6; s.beginPath(); s.roundRect(44, 44, cw - 88, ch - 88, rr - 20); s.stroke();
      s.strokeStyle = 'rgba(184,158,112,.5)'; s.lineWidth = 3; s.beginPath(); s.roundRect(64, 64, cw - 128, ch - 128, rr - 30); s.stroke();
      s.fillStyle = 'rgba(184,158,112,.32)';
      for (let x = 90; x < cw - 90; x += 22) { s.fillRect(x, 52, 9, 4); s.fillRect(x, ch - 56, 9, 4); }
      for (let y = 90; y < ch - 90; y += 22) { s.fillRect(52, y, 4, 9); s.fillRect(cw - 56, y, 4, 9); }
      // the map: one block per project, far = top of the rug, dashed threads between related ones
      const k = Math.min((cw - 240) / 34, (ch - 230) / 21.5), ox = cw / 2, oy = ch / 2 + 6;
      const P = (u, v) => [ox + u * k, oy - (v - 5.4) * k];
      const byName = Object.fromEntries(RUG_PROJECTS.map((p) => [p.name, p]));
      s.setLineDash([12, 10]); s.lineWidth = 3.2; s.strokeStyle = 'rgba(196,172,128,.4)'; s.lineCap = 'round';
      RUG_LINKS.forEach(([a, b]) => {
        const A = byName[a], Bp = byName[b]; const [x0, y0] = P(A.u, A.v), [x1, y1] = P(Bp.u, Bp.v);
        const mx = (x0 + x1) / 2, my = (y0 + y1) / 2, nx = -(y1 - y0) * .18, ny = (x1 - x0) * .18;
        s.beginPath(); s.moveTo(x0, y0); s.quadraticCurveTo(mx + nx, my + ny, x1, y1); s.stroke();
      });
      s.setLineDash([]);
      RUG_PROJECTS.forEach((p, index) => {
        const [x, y] = P(p.u, p.v), w = p.w * k, h = p.d * k;
        s.fillStyle = hexA(p.color, .09); s.strokeStyle = hexA(p.color, .62); s.lineWidth = 4.5;
        s.beginPath(); s.roundRect(x - w / 2, y - h / 2, w, h, 6); s.fill(); s.stroke();
        const n = Math.max(2, Math.round(p.w / 1.1)), m = Math.max(2, Math.round(p.d / 1.1)), cwp = (w - 18) / n, chp = (h - 18) / m;
        const cr = rnd(index * 7 + 3);
        for (let i = 0; i < n; i++) for (let j = 0; j < m; j++) {
          if (cr() < .2) continue; const q = .55 + cr() * .3;
          s.fillStyle = hexA(p.color, .1 + cr() * .16);
          s.fillRect(x - w / 2 + 9 + i * cwp + cwp * (1 - q) / 2, y - h / 2 + 9 + j * chp + chp * (1 - q) / 2, cwp * q, chp * q);
        }
        s.fillStyle = 'rgba(214,196,160,.5)'; s.font = `600 15px ${MONO}`; s.textAlign = 'center';
        s.fillText(p.name.toLowerCase(), x, y + h / 2 + 22);
      });
      s.textAlign = 'left'; s.fillStyle = 'rgba(214,196,160,.42)'; s.font = `600 15px ${MONO}`; s.fillText('portfolio.graph', 98, ch - 88);
      s.restore();
      tex.needsUpdate = true;
    };
    drawRug(); fontJobs.push(drawRug);
    tex.anisotropy = 8;
    const sh = new THREE.Shape(), w = RUG.w, h = RUG.d, r = .16;
    sh.moveTo(-w / 2 + r, -h / 2); sh.lineTo(w / 2 - r, -h / 2); sh.quadraticCurveTo(w / 2, -h / 2, w / 2, -h / 2 + r); sh.lineTo(w / 2, h / 2 - r); sh.quadraticCurveTo(w / 2, h / 2, w / 2 - r, h / 2);
    sh.lineTo(-w / 2 + r, h / 2); sh.quadraticCurveTo(-w / 2, h / 2, -w / 2, h / 2 - r); sh.lineTo(-w / 2, -h / 2 + r); sh.quadraticCurveTo(-w / 2, -h / 2, -w / 2 + r, -h / 2);
    const base = new THREE.Mesh(new THREE.ExtrudeGeometry(sh, { depth: .03, bevelEnabled: false, curveSegments: 6 }).rotateX(Math.PI / 2).translate(RUG.x, .032, RUG.z), mat.rug);
    rugG.add(base);
    const top = new THREE.Mesh(new THREE.PlaneGeometry(w, h).rotateX(-Math.PI / 2), M('#ffffff', { map: tex, roughness: 1, alphaTest: .5 }));
    top.position.set(RUG.x, .0335, RUG.z); rugG.add(top);
  }

  // ---------------------------------------------------------------- whiteboard on a stand: marker sketches from the two internships
  const wb = new THREE.Group(); wb.position.set(3.02, 0, 1.55); wb.rotation.y = .12; world.add(wb); pop(wb, 1.0);
  let wbFace;
  {
    const BW = 1.36, BH = .92, CY = 1.42;
    wb.add(blob(1.9, .95, .008));
    for (const sx of [-.74, .74]) {
      wb.add(at(cyl(.022, .022, 1.86, mat.metal, 10), sx, .97, 0));
      wb.add(at(rbox(.06, .05, .66, .02, mat.metal), sx, .085, 0));
      for (const sz of [-.29, .29]) wb.add(at(new THREE.Mesh(new THREE.SphereGeometry(.036, 10, 8), mat.dark), sx, .036, sz));
    }
    wb.add(at(cyl(.016, .016, 1.48, mat.metal, 8).rotateZ(Math.PI / 2), 0, .5, 0));
    wb.add(at(rbox(BW + .07, BH + .07, .045, .016, M('#c9d0db', { roughness: .32, metalness: .55, ...E(.6) })), 0, CY, 0));
    const wc = canvasTex(680, 460); const s = wc.ctx;
    const drawWB = () => {
      s.setTransform(1, 0, 0, 1, 0, 0);
      s.fillStyle = '#eef0ec'; s.fillRect(0, 0, 680, 460);
      s.lineCap = 'round'; s.lineJoin = 'round';
      // ghosts of erased sketches
      s.strokeStyle = 'rgba(110,120,135,.08)'; s.lineWidth = 16;
      s.beginPath(); s.moveTo(60, 330); s.bezierCurveTo(140, 290, 200, 380, 300, 340); s.stroke();
      s.beginPath(); s.moveTo(430, 70); s.lineTo(610, 110); s.stroke();
      // left: flow-diverter stent in a vessel with an aneurysm
      s.fillStyle = '#1d4ed8'; s.font = `italic 700 25px Inter, sans-serif`; s.fillText('flow-diverter deploy', 26, 48);
      s.font = `500 15px ${MONO}`; s.fillText('GPU sim · patient-specific vessel', 28, 72);
      const top = (t) => [30 + t * 270, 168 + Math.sin(t * 3.1) * 26 - t * 10], bot = (t) => [30 + t * 270, 236 + Math.sin(t * 3.1) * 26 - t * 10];
      s.strokeStyle = '#1f2937'; s.lineWidth = 4.5;
      for (const f of [top, bot]) { s.beginPath(); for (let i = 0; i <= 40; i++) { const [x, y] = f(i / 40); if (i) s.lineTo(x, y); else s.moveTo(x, y); } s.stroke(); }
      { const [ax, ay] = top(.5); s.beginPath(); s.ellipse(ax, ay - 2, 36, 38, 0, Math.PI * 1.06, Math.PI * 1.94); s.stroke(); }
      s.strokeStyle = '#dc2626'; s.lineWidth = 2.4;
      for (const ph of [0, 1]) {
        s.beginPath();
        for (let i = 0; i <= 18; i++) { const t = .12 + i / 18 * .74, [x0, y0] = top(t), [, y1] = bot(t); const up = (i + ph) % 2 === 0; const y = up ? y0 + 9 : y1 - 9; if (i) s.lineTo(x0, y); else s.moveTo(x0, y); }
        s.stroke();
      }
      s.strokeStyle = '#1d4ed8'; s.lineWidth = 3; s.beginPath(); s.moveTo(34, 300); s.lineTo(118, 300); s.lineTo(106, 291); s.moveTo(118, 300); s.lineTo(106, 309); s.stroke();
      s.fillStyle = '#1d4ed8'; s.font = `italic 600 18px Inter, sans-serif`; s.fillText('flow', 128, 306);
      s.strokeStyle = '#dc2626'; s.lineWidth = 2.5; s.beginPath(); s.ellipse(236, 318, 64, 22, -.05, 0, 7); s.stroke();
      s.fillStyle = '#dc2626'; s.font = `600 16px ${MONO}`; s.fillText('deploy ✓', 196, 324);
      // sticky note
      s.save(); s.translate(64, 352); s.rotate(-.06); s.fillStyle = '#fde68a'; s.fillRect(0, 0, 118, 84); s.fillStyle = 'rgba(0,0,0,.08)'; s.fillRect(0, 76, 118, 8);
      s.fillStyle = '#7c4a03'; s.font = `italic 600 16px Inter, sans-serif`; s.fillText('profile the', 10, 30); s.fillText('mesh step', 10, 52); s.restore();
      // divider
      s.strokeStyle = 'rgba(31,41,55,.25)'; s.lineWidth = 2; s.setLineDash([8, 9]); s.beginPath(); s.moveTo(344, 30); s.lineTo(344, 430); s.stroke(); s.setLineDash([]);
      // right: pNanoLocz, AFM particles and the speed-up
      s.fillStyle = '#15803d'; s.font = `italic 700 25px Inter, sans-serif`; s.fillText('pNanoLocz · AFM', 366, 48);
      s.font = `500 15px ${MONO}`; s.fillText('detect → track → stats', 368, 72);
      s.strokeStyle = '#15803d'; s.lineWidth = 2.2;
      for (const [x, y, n] of [[408, 128, 3], [470, 150, 4], [432, 192, 2], [520, 112, 2]]) for (let i = 1; i <= n; i++) { s.beginPath(); s.ellipse(x, y, i * 8, i * 6.5, .3, 0, 7); s.stroke(); }
      s.strokeStyle = '#0f766e'; s.setLineDash([5, 6]); s.beginPath(); s.moveTo(408, 128); s.lineTo(470, 150); s.lineTo(520, 112); s.stroke(); s.setLineDash([]);
      s.strokeStyle = '#1f2937'; s.lineWidth = 3; s.beginPath(); s.moveTo(380, 250); s.lineTo(380, 392); s.lineTo(640, 392); s.stroke();
      s.fillStyle = 'rgba(107,114,128,.55)'; s.fillRect(410, 252, 72, 140);
      s.fillStyle = 'rgba(22,163,74,.7)'; s.fillRect(530, 322, 72, 70);
      s.fillStyle = '#1f2937'; s.font = `600 15px ${MONO}`; s.fillText('MATLAB', 414, 418); s.fillText('pNanoLocz', 520, 418);
      s.strokeStyle = '#dc2626'; s.lineWidth = 3; s.beginPath(); s.moveTo(490, 262); s.quadraticCurveTo(560, 262, 566, 312); s.stroke();
      s.beginPath(); s.moveTo(566, 314); s.lineTo(557, 301); s.moveTo(566, 314); s.lineTo(573, 300); s.stroke();
      s.fillStyle = '#dc2626'; s.font = `italic 800 24px Inter, sans-serif`; s.fillText('50% faster', 504, 250);
      wc.tex.needsUpdate = true;
    };
    drawWB(); fontJobs.push(drawWB);
    wbFace = at(new THREE.Mesh(new THREE.PlaneGeometry(BW, BH), M('#ffffff', { map: wc.tex, roughness: .3, emissive: '#ffffff', emissiveMap: wc.tex, emissiveIntensity: .14 })), 0, CY, .024);
    wb.add(wbFace);
    wb.add(at(rbox(BW * .72, .03, .09, .01, mat.metal), 0, CY - BH / 2 - .055, .055));
    ['#2563eb', '#dc2626', '#16a34a', '#111827'].forEach((c, i) => wb.add(at(cyl(.014, .014, .13, M(c, { roughness: .4 }), 8).rotateZ(Math.PI / 2), -.32 + i * .17, CY - BH / 2 - .025, .06)));
    wb.add(at(rbox(.14, .045, .06, .012, mat.dark), .4, CY - BH / 2 - .02, .06));
  }

  // ---------------------------------------------------------------- shadows
  world.traverse((o) => { if (o.isMesh && !o.userData.noRay && o.material && o.material.isMeshStandardMaterial) { o.castShadow = true; o.receiveShadow = true; } });
  room.traverse((o) => { if (o.isMesh) o.castShadow = false; });
  rugG.traverse((o) => { if (o.isMesh) o.castShadow = false; });
  certificate.traverse((o) => { if (o.isMesh) o.castShadow = false; });

  // merge static geometry per object (cuts draw calls roughly in half)
  [room, desk, monA.g, monB.g, lamp, lampYaw, robot, bookcase, rack, plant, rig, ...rig.children.filter((c) => c.isGroup && !c.userData.dyn), wb, certificate].forEach(mergeStatic);

  return {
    world, grid, pops, fontJobs,
    lights: { moon, spot, aim, lightA, lightB },
    desk, keyboardTop, monA, monB,
    lamp, lampYaw, lampHead, lampBulb, beam, motes,
    robot, robotHead, eyes, armR,
    bookcase, rack, leds, ledState, drawOled, plant, leaves,
    rig, turntable, sheet, cloudMat, cutU, solidPlane, wireLo, wireHi, cutLocal,
    rugG, room, wb, wbFace, certificate,
  };
}

// The rug's map: where each project sits (u = right, v = away from the viewer) and its colour
const RUG_PROJECTS = [
  { name: 'Synergy', color: '#8eeefe', u: 0, v: 0, w: 8.2, d: 8.2 },
  { name: 'HireForge', color: '#22d3ee', u: 9.6, v: 2.8, w: 5.6, d: 5.6 },
  { name: '3D Reconstruction', color: '#34d399', u: -9.6, v: 3.4, w: 5.6, d: 5.0 },
  { name: 'Stent Simulation', color: '#a78bfa', u: -4.0, v: 12.0, w: 4.8, d: 4.6 },
  { name: 'pNanoLocz', color: '#a78bfa', u: 4.4, v: 11.4, w: 5.0, d: 4.2 },
  { name: 'This site', color: '#22d3ee', u: 12.6, v: 10.4, w: 3.8, d: 3.8 },
  { name: 'Azure Inventory', color: '#34d399', u: -12.6, v: 11.2, w: 4.6, d: 3.8 },
];
const RUG_LINKS = [
  ['This site', 'Synergy'], ['HireForge', 'Synergy'], ['Synergy', '3D Reconstruction'], ['HireForge', 'Azure Inventory'],
  ['pNanoLocz', '3D Reconstruction'], ['3D Reconstruction', 'Stent Simulation'], ['This site', 'HireForge'], ['Azure Inventory', 'This site'],
  ['Stent Simulation', 'pNanoLocz'],
];
const hexA = (hex, a) => { const c = new THREE.Color(hex); return `rgba(${Math.round(c.r * 255)},${Math.round(c.g * 255)},${Math.round(c.b * 255)},${a})`; };

export function mergeStatic(root) {
  root.updateMatrixWorld(true);
  const inv = root.matrixWorld.clone().invert(), buckets = new Map();
  const walk = (o) => {
    for (const c of o.children) {
      if (c.userData.dyn) continue;
      const ok = c.isMesh && !c.isInstancedMesh && c.children.length === 0 && !c.userData.keep && c.material && !Array.isArray(c.material)
        && (c.material.isMeshStandardMaterial || c.material.isMeshBasicMaterial) && !c.material.clippingPlanes;
      if (ok) { const key = `${c.material.uuid}|${c.castShadow}|${c.receiveShadow}|${c.renderOrder}`; if (!buckets.has(key)) buckets.set(key, []); buckets.get(key).push(c); }
      else walk(c);
    }
  };
  walk(root);
  const mtx = new THREE.Matrix4();
  for (const list of buckets.values()) {
    if (list.length < 2) continue;
    const hasCol = list.some((m) => m.geometry.attributes.color);
    const geos = list.map((m) => {
      const g = m.geometry.index ? m.geometry.toNonIndexed() : m.geometry.clone();
      for (const n of Object.keys(g.attributes)) if (!['position', 'normal', 'uv', 'color'].includes(n)) g.deleteAttribute(n);
      if (!g.attributes.uv) g.setAttribute('uv', new THREE.BufferAttribute(new Float32Array(g.attributes.position.count * 2), 2));
      if (hasCol && !g.attributes.color) g.setAttribute('color', new THREE.BufferAttribute(new Float32Array(g.attributes.position.count * 3).fill(1), 3));
      if (!hasCol && g.attributes.color) g.deleteAttribute('color');
      g.morphAttributes = {}; g.clearGroups();
      g.applyMatrix4(mtx.multiplyMatrices(inv, m.matrixWorld));
      return g;
    });
    const merged = mergeGeometries(geos, false);
    geos.forEach((g) => g.dispose());
    if (!merged) continue;
    const mm = new THREE.Mesh(merged, list[0].material);
    mm.castShadow = list[0].castShadow; mm.receiveShadow = list[0].receiveShadow; mm.renderOrder = list[0].renderOrder; mm.userData = { ...list[0].userData };
    list.forEach((m) => { m.parent.remove(m); m.geometry.dispose(); });
    root.add(mm);
  }
}
