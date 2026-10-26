// Shared building blocks for the workshop scene: the renderer, maths helpers, the palette and
// the materials and mesh helpers every part of the room is built from.
import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/addons/geometries/RoundedBoxGeometry.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

export const V3 = THREE.Vector3;
export const clamp = (x, a, b) => Math.min(b, Math.max(a, x));
export const lerp = (a, b, t) => a + (b - a) * t;
export const smooth = (t) => t * t * (3 - 2 * t);
export const easeInOut = (t) => (t < .5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);
export const easeOutBack = (t) => { const c1 = 1.5, c3 = c1 + 1; return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2); };
export const damp = (dt, rate) => 1 - Math.exp(-dt * rate);
export function rnd(seed) { let s = seed >>> 0; return () => ((s = (s * 1664525 + 1013904223) >>> 0) / 4294967296); }

export const MONO = '"JetBrains Mono", ui-monospace, monospace';
export const C = { cyan: '#22d3ee', violet: '#a78bfa', green: '#34d399', amber: '#fbbf24' };
// Matches the page background (Tailwind `ink`), so the stage's faded edges blend into the page
export const BACKGROUND = '#0b1020';

// Throws Error('webgl-unavailable') when the browser can't give us WebGL2; the page keeps its still image
export function createRenderer(canvas) {
  let gl = null;
  try { gl = canvas.getContext('webgl2', { antialias: false, powerPreference: 'high-performance', alpha: false }); } catch { gl = null; }
  if (!gl) throw new Error('webgl-unavailable');
  const renderer = new THREE.WebGLRenderer({ canvas, context: gl });
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.2;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.localClippingEnabled = true;
  return renderer;
}

export function isSoftwareRenderer(renderer) {
  const gl = renderer.getContext();
  const ext = gl.getExtension('WEBGL_debug_renderer_info');
  return /swiftshader|llvmpipe|software|basic render/i.test(ext ? String(gl.getParameter(ext.UNMASKED_RENDERER_WEBGL)) : '');
}

// ---------------------------------------------------------------- shading: baked-looking AO, warm/cool wash, LED bounce
const VDECL = 'varying vec3 vWP; varying vec3 vWN;\n';
const VMAIN = `
  vec4 _wp = vec4(transformed, 1.0);
  #ifdef USE_INSTANCING
    _wp = instanceMatrix * _wp;
  #endif
  _wp = modelMatrix * _wp; vWP = _wp.xyz; vWN = normalize(mat3(modelMatrix) * objectNormal);`;
const ROOM_FRAG = `{
  vec3 an = abs(normalize(vWN));
  float ax = mix(.34, 1., smoothstep(0., 1.5, vWP.x + 3.2));
  float ay = mix(.34, 1., smoothstep(0., 1.5, vWP.y));
  float az = mix(.34, 1., smoothstep(0., 1.5, vWP.z + 4.2));
  float ao = an.y > .5 ? ax * az : (an.z > .5 ? ax * ay : ay * az);
  vec3 tint = mix(vec3(.80, .88, 1.16), vec3(1.2, 1.0, .82), smoothstep(-2.8, 3.4, vWP.x - vWP.z * .35));
  diffuseColor.rgb *= ao * tint;
}`;
const ROOM_EMIT = `{
  vec3 an2 = abs(normalize(vWN));
  float led = an2.y < .5 ? exp(-abs(4.2 - vWP.y) * 1.6) : 0.;
  float bounce = an2.y > .5 ? (exp(-max(vWP.z + 4.2, 0.) * 1.1) + exp(-max(vWP.x + 3.2, 0.) * 1.1)) : 0.;
  totalEmissiveRadiance += vec3(.62, .5, 1.0) * (led * .2 + bounce * .05);
}`;
const OBJ_FRAG = `{
  float nY = normalize(vWN).y;
  float ao = mix(.42, 1., smoothstep(0.02, .5, vWP.y));
  diffuseColor.rgb *= mix(ao, 1., smoothstep(.55, .9, nY));
}`;
export function shade(m, kind) {
  m.onBeforeCompile = (sh) => {
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\n' + VDECL).replace('#include <project_vertex>', '#include <project_vertex>\n' + VMAIN);
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\n' + VDECL)
      .replace('#include <color_fragment>', '#include <color_fragment>\n' + (kind === 'room' ? ROOM_FRAG : OBJ_FRAG))
      .replace('#include <emissivemap_fragment>', '#include <emissivemap_fragment>\n' + (kind === 'room' ? ROOM_EMIT : ''));
  };
  m.customProgramCacheKey = () => 'shade-' + kind;
  return m;
}

// Everything that needs the renderer (the environment map) or is shared between parts of the room
export function createKit(renderer) {
  const pmrem = new THREE.PMREMGenerator(renderer);
  // IBL only where reflections read (metals, bezels, robot); walls, floor and wood use the lights
  const envScene = new RoomEnvironment(renderer);
  const ENV = pmrem.fromScene(envScene, 0.04).texture;
  pmrem.dispose();
  envScene.traverse((o) => { if (o.isMesh) { o.geometry.dispose(); o.material.dispose(); } });

  const M = (color, o = {}) => shade(new THREE.MeshStandardMaterial({ color, roughness: .78, metalness: .04, ...o }), 'obj');
  const E = (k) => ({ envMap: ENV, envMapIntensity: k });
  const MR = (color, o = {}) => shade(new THREE.MeshStandardMaterial({ color, roughness: .9, metalness: 0, ...o }), 'room');
  const B = (color, k = 1, o = {}) => { const m = new THREE.MeshBasicMaterial({ color, ...o }); m.color.multiplyScalar(k); return m; };
  const rbox = (w, h, d, r, mat, seg = 2) => { const rr = Math.min(r, Math.min(w, h, d) * .49); return new THREE.Mesh(new RoundedBoxGeometry(w, h, d, seg, rr), mat); };
  const box = (w, h, d, mat) => new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
  const cyl = (rt, rb, h, mat, seg = 16, open = false) => new THREE.Mesh(new THREE.CylinderGeometry(rt, rb, h, seg, 1, open), mat);
  const at = (o, x, y, z) => { o.position.set(x, y, z); return o; };
  const canvasTex = (w, h) => {
    const c = document.createElement('canvas'); c.width = w; c.height = h;
    const ctx = c.getContext('2d'); const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace; tex.anisotropy = 4;
    return { c, ctx, tex };
  };
  const strut = (a, b, r, mat, seg = 8) => {
    const d = new V3().subVectors(b, a); const m = cyl(r, r, d.length(), mat, seg);
    m.position.copy(a).addScaledVector(d, .5); m.quaternion.setFromUnitVectors(new V3(0, 1, 0), d.normalize()); return m;
  };
  // per-mesh vertex colour, so many meshes can share one material (and merge)
  const paint = (mesh, color) => {
    const g = mesh.geometry, n = g.attributes.position.count, c = new THREE.Color(color), a = new Float32Array(n * 3);
    for (let i = 0; i < n; i++) a.set([c.r, c.g, c.b], i * 3);
    g.setAttribute('color', new THREE.BufferAttribute(a, 3)); return mesh;
  };
  const radial = (size, stops) => {
    const { ctx, tex } = canvasTex(size, size);
    const g = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
    stops.forEach(([k, c]) => g.addColorStop(k, c));
    ctx.fillStyle = g; ctx.fillRect(0, 0, size, size); tex.colorSpace = THREE.NoColorSpace; return tex;
  };
  const DOT = radial(64, [[0, 'rgba(255,255,255,1)'], [.35, 'rgba(255,255,255,.55)'], [1, 'rgba(255,255,255,0)']]);
  // soft contact-shadow blob
  const BLOB = radial(128, [[0, 'rgba(0,0,0,1)'], [.45, 'rgba(0,0,0,.6)'], [1, 'rgba(0,0,0,0)']]);
  const blobMat = new THREE.MeshBasicMaterial({ color: '#000000', map: BLOB, transparent: true, opacity: .62, depthWrite: false });
  const blobMatSoft = new THREE.MeshBasicMaterial({ color: '#000000', map: BLOB, transparent: true, opacity: .4, depthWrite: false });
  const blob = (w, d, y = .008, soft = false) => {
    const m = new THREE.Mesh(new THREE.PlaneGeometry(w, d).rotateX(-Math.PI / 2), soft ? blobMatSoft : blobMat);
    m.position.y = y; m.userData.noRay = true; m.renderOrder = 1; return m;
  };

  const mat = {
    slabEdge: M('#283146', { roughness: .8 }),
    wall: MR('#2b3245', { roughness: .95 }),
    cap: MR('#3a4459', { roughness: .7 }),
    skirt: MR('#232a3b', { roughness: .8 }),
    wood: M('#6a4a33', { roughness: .55 }),
    woodDark: M('#3a2a20', { roughness: .7 }),
    woodLight: M('#866345', { roughness: .6 }),
    metal: M('#2e3546', { roughness: .4, metalness: .65, ...E(.55) }),
    dark: M('#0e1119', { roughness: .55 }),
    bezel: M('#10141d', { roughness: .3, metalness: .4, ...E(.7) }),
    cream: M('#e4ddcf', { roughness: .45, ...E(.35) }),
    robot: M('#c3cad7', { roughness: .45, ...E(.4) }),
    frame: M('#3a4459', { roughness: .6 }),
    rug: M('#18202f', { roughness: 1 }),
    pot: M('#a4654c', { roughness: .85 }),
    soil: M('#1c1512', { roughness: 1 }),
    books: M('#ffffff', { vertexColors: true, roughness: .78 }),
  };
  const glow = { cyan: B(C.cyan, 2.2), green: B(C.green, 2.2), red: B('#f87171', 2.4) };

  return { ENV, M, E, MR, B, rbox, box, cyl, at, canvasTex, strut, paint, blob, DOT, mat, glow };
}

// Frees every GPU resource reachable from the scene: geometries, materials and their textures
export function disposeScene(scene) {
  const textures = new Set(), materials = new Set();
  const collect = (m) => {
    if (!m || materials.has(m)) return;
    materials.add(m);
    for (const v of Object.values(m)) if (v && v.isTexture) textures.add(v);
    if (m.uniforms) for (const u of Object.values(m.uniforms)) if (u && u.value && u.value.isTexture) textures.add(u.value);
  };
  scene.traverse((o) => {
    if (o.isInstancedMesh) o.dispose();
    if (o.geometry) o.geometry.dispose();
    if (Array.isArray(o.material)) o.material.forEach(collect); else collect(o.material);
    if (o.isLight && o.shadow && o.shadow.map) o.shadow.map.dispose();
  });
  materials.forEach((m) => m.dispose());
  textures.forEach((t) => t.dispose());
}
