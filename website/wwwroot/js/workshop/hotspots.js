// Binds the homepage's hotspot data (HomeContent.Workshop) to objects in the scene, and picks
// which object is under the pointer.
import * as THREE from 'three';
import { V3 } from './kit.js';

const wp = (o, x = 0, y = 0, z = 0) => o.getWorldPosition(new V3()).add(new V3(x, y, z));

// Where each object is in the scene: its group, where its hover tag sits, how high it lifts on
// hover and the camera pose that frames it when clicked
const SPOTS = {
  synergy: (r) => ({ g: r.monA.g, screen: r.monA.screen, anchor: () => wp(r.monA.screen, 0, .75, 0), focus: { t: () => wp(r.monA.screen, -.05, .02, 0), az: .24, polar: 1.5, r: 4.3 } }),
  hireforge: (r) => ({ g: r.monB.g, anchor: () => wp(r.monB.screen, 0, .75, 0), focus: { t: () => wp(r.monB.screen, -.05, .02, 0), az: -.08, polar: 1.5, r: 4.3 } }),
  thesis: (r) => ({ g: r.rig, anchor: () => wp(r.rig, 0, 2.55, 0), focus: { t: () => wp(r.rig, -.1, 1.6, 0), az: .5, polar: 1.28, r: 4.4 } }),
  whiteboard: (r) => ({ g: r.wb, anchor: () => wp(r.wb, 0, 2.2, 0), focus: { t: () => wp(r.wb, -.15, 1.38, 0), az: .36, polar: 1.42, r: 4.5 } }),
  stent: (r) => ({ g: r.stent, lift: .06, anchor: () => wp(r.stent, 0, .42, 0), focus: { t: () => wp(r.stent, 0, .2, 0), az: .3, polar: 1.2, r: 2.1 } }),
  afm: (r) => ({ g: r.afm, lift: .06, anchor: () => wp(r.afm, 0, 1.5, 0), focus: { t: () => wp(r.afm, 0, 1.0, 0), az: .35, polar: 1.25, r: 2.8 } }),
  azure: (r) => ({ g: r.azure, anchor: () => wp(r.azure, 0, .5, 0), focus: { t: () => wp(r.azure, 0, -.15, 0), az: .95, polar: 1.3, r: 3.6 } }),
  homelab: (r) => ({ g: r.rack, anchor: () => wp(r.rack, 0, 2.45, 0), focus: { t: () => wp(r.rack, .3, 1.25, 0), az: 1.15, polar: 1.33, r: 5.6 } }),
  certificate: (r) => ({ g: r.certificate, lift: .06, anchor: () => wp(r.certificate, 0, .62, 0), focus: { t: () => wp(r.certificate, .1, 0, 0), az: 1.42, polar: 1.48, r: 3.4 } }),
  bookcase: (r) => ({ g: r.bookcase, lift: .06, anchor: () => wp(r.bookcase, 0, 3.15, 0), focus: { t: () => wp(r.bookcase, 0, 1.6, .2), az: .12, polar: 1.45, r: 5.6 } }),
  robot: (r) => ({ g: r.robot, anchor: () => wp(r.robot, 0, 1.05, 0), focus: { t: () => wp(r.robot, -.05, .5, 0), az: -.18, polar: 1.32, r: 3.3 } }),
};

export function bindHotspots(data, room) {
  const order = data.map((d) => d.key);
  const byKey = {};
  for (const d of data) {
    const make = SPOTS[d.key];
    const h = { ...d, ...(make ? make(room) : {}) };
    h.lift = h.lift ?? .11; h.liftK = 0; h.meshes = []; h.box = new THREE.Box3(); h.boxDirty = true;
    if (h.g) {
      h.baseY = h.g.position.y;
      h.g.traverse((o) => { if (o.isMesh && !o.userData.noRay) { o.userData.hs = d.key; h.meshes.push(o); } });
    }
    byKey[d.key] = h;
  }
  // surfaces in front of objects: a hit on these means "nothing", not the object behind
  const surfaces = [];
  [room.room, room.desk, room.rugG, room.plant].forEach((g) => g.traverse((o) => { if (o.isMesh && !o.userData.noRay && !o.userData.hs) surfaces.push(o); }));

  const ray = new THREE.Raycaster();
  const boxOf = (h) => { if (h.boxDirty) { h.box.setFromObject(h.g); h.boxDirty = false; } return h.box; };

  // nearest hit under ndc: objects first by bounding box, then their meshes; surfaces can occlude
  function pick(ndc, camera) {
    ray.setFromCamera(ndc, camera);
    let best = null;
    for (const k of order) {
      const h = byKey[k]; if (!h.g || !ray.ray.intersectsBox(boxOf(h))) continue;
      const hit = ray.intersectObjects(h.meshes, false)[0]; if (hit && (!best || hit.distance < best.distance)) best = hit;
    }
    const s = ray.intersectObjects(surfaces, false)[0]; if (s && (!best || s.distance < best.distance)) best = s;
    return best;
  }

  // For each object, a point (as % of the stage) where the pointer actually hits it, nearest the
  // middle of its outline. Used for the still image's link pins and by the tests.
  function pins(camera) {
    const out = [], ndc = new THREE.Vector2(), corner = new V3();
    for (const k of order) {
      const h = byKey[k]; if (!h.g) continue;
      const b = boxOf(h);
      let x0 = 1, x1 = -1, y0 = 1, y1 = -1;
      for (let i = 0; i < 8; i++) {
        corner.set(i & 1 ? b.max.x : b.min.x, i & 2 ? b.max.y : b.min.y, i & 4 ? b.max.z : b.min.z).project(camera);
        x0 = Math.min(x0, corner.x); x1 = Math.max(x1, corner.x); y0 = Math.min(y0, corner.y); y1 = Math.max(y1, corner.y);
      }
      x0 = Math.max(x0, -.98); x1 = Math.min(x1, .98); y0 = Math.max(y0, -.98); y1 = Math.min(y1, .98);
      const cx = (x0 + x1) / 2, cy = (y0 + y1) / 2;
      // sample a grid over the outline; prefer points deep inside the object (all neighbours hit
      // it too), so a pin still lands when the object lifts on hover; then the one nearest the middle
      const N = 16, hits = [];
      for (let i = 0; i <= N; i++) { hits.push([]); for (let j = 0; j <= N; j++) {
        ndc.set(x0 + (x1 - x0) * i / N, y0 + (y1 - y0) * j / N);
        const hit = pick(ndc, camera);
        hits[i].push(!!hit && hit.object.userData.hs === k);
      } }
      let best = null, bestScore = -Infinity;
      for (let i = 0; i <= N; i++) for (let j = 0; j <= N; j++) {
        if (!hits[i][j]) continue;
        let around = 0;
        for (let di = -1; di <= 1; di++) for (let dj = -1; dj <= 1; dj++) if (hits[i + di]?.[j + dj]) around++;
        const x = x0 + (x1 - x0) * i / N, y = y0 + (y1 - y0) * j / N;
        const score = around * 10 - ((x - cx) ** 2 + (y - cy) ** 2);
        if (score > bestScore) { bestScore = score; best = new THREE.Vector2(x, y); }
      }
      if (best) out.push({ key: k, x: +((best.x + 1) / 2 * 100).toFixed(2), y: +((1 - best.y) / 2 * 100).toFixed(2) });
    }
    return out;
  }

  return { order, byKey, pick, pins, markDirty() { for (const k of order) byKey[k].boxDirty = true; } };
}
