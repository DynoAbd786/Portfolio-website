// The objects that stand for one project each and aren't part of the original workshop:
// the stent model, the microscope and the Azure cloud above the rack.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { V3, C } from './kit.js';
import { mergeStatic } from './room.js';

export function buildProps(world, kit) {
  const { M, B, rbox, cyl, at, blob, mat, glow } = kit;

  // ---------------------------------------------------------------- stent: a flow diverter inside a vessel with an aneurysm (desk, right
  // of the keyboard, clear of the lamp's glare). A glassy vessel wall over a column of blood, a fine braided stent across the
  // aneurysm's neck, darker still blood in the sac (the diverter slows the flow into it), and cells flowing past.
  const stent = new THREE.Group(); stent.position.set(1.28, 1.77, -2.95); stent.rotation.y = -.15; world.add(stent);
  let cells = null, cellU = null;
  const vesselCurve = new THREE.CatmullRomCurve3([new V3(-.29, .2, 0), new V3(-.13, .215, .012), new V3(.05, .19, -.01), new V3(.29, .205, 0)]);
  {
    stent.add(blob(.68, .36, .004, true));
    stent.add(at(rbox(.58, .035, .22, .015, mat.dark), 0, .018, 0));
    for (const x of [-.21, .21]) stent.add(at(cyl(.011, .011, .17, mat.metal, 8), x, .12, 0));
    const curve = vesselCurve, R = .045;
    // the blood inside, then the wall over it
    const blood = new THREE.Mesh(new THREE.TubeGeometry(curve, 64, R * .62, 16, false), M('#8e1b28', { roughness: .45, emissive: '#3a0610', emissiveIntensity: .6, transparent: true, opacity: .45, depthWrite: false }));
    blood.renderOrder = 1; stent.add(blood);
    const wallMat = M('#d9545f', { transparent: true, opacity: .3, roughness: .2, metalness: 0, depthWrite: false, side: THREE.DoubleSide, ...kit.E(.5) });
    const wall = new THREE.Mesh(new THREE.TubeGeometry(curve, 64, R, 24, false), wallMat); wall.renderOrder = 2; stent.add(wall);
    // the aneurysm: a sac bulging from the outer bend, its neck sunk into the wall, still dark blood inside
    const neck = curve.getPointAt(.43);
    const sac = new THREE.Mesh(new THREE.SphereGeometry(.06, 28, 20), wallMat); sac.scale.set(1.15, .78, 1.05);
    sac.position.set(neck.x, neck.y + R * .3 + .03, neck.z); sac.renderOrder = 2; stent.add(sac);
    const still = new THREE.Mesh(new THREE.SphereGeometry(.045, 20, 14), M('#5a0d18', { roughness: .6, transparent: true, opacity: .7, depthWrite: false }));
    still.scale.set(1.1, .72, 1); still.renderOrder = 1; stent.add(at(still, neck.x, neck.y + R * .3 + .032, neck.z));
    // the flow diverter: 16 wires braided in opposite helices, a fine metal mesh across the neck
    const frames = curve.computeFrenetFrames(200, false), wires = [], WIRES = 8, TURNS = 5, rS = R * .93;
    for (let i = 0; i < WIRES * 2; i++) {
      const dir = i < WIRES ? 1 : -1, ph = (i % WIRES) / WIRES * Math.PI * 2, pts = [];
      for (let k = 0; k <= 200; k++) {
        const u = .12 + k / 200 * .76, f = Math.round(u * 200), th = ph + dir * TURNS * Math.PI * 2 * (u - .12);
        pts.push(curve.getPointAt(u).addScaledVector(frames.normals[f], Math.cos(th) * rS).addScaledVector(frames.binormals[f], Math.sin(th) * rS));
      }
      wires.push(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(pts), 200, .0013, 4, false));
    }
    const braid = new THREE.Mesh(mergeGeometries(wires), M('#9fb4c2', { roughness: .38, metalness: .9, ...kit.E(.45) }));
    wires.forEach((g) => g.dispose());
    stent.add(braid);
    // blood cells flowing through (animate moves them along the vessel)
    const N = 26;
    cells = new THREE.InstancedMesh(new THREE.SphereGeometry(.0085, 8, 6).scale(1, .45, 1), M('#d23a46', { roughness: .4, emissive: '#5a0c16', emissiveIntensity: .8 }), N);
    cellU = Array.from({ length: N }, (_, i) => [i / N, Math.random() * Math.PI * 2, .35 + Math.random() * .45, Math.random() * 6]);
    cells.userData.noRay = true; cells.renderOrder = 1; stent.add(cells);
  }
  const cellM = new THREE.Matrix4(), cellQ = new THREE.Quaternion(), cellP = new V3(), cellT = new V3(), cellS = new V3(1, 1, 1), cellUp = new V3(0, 1, 0);
  const cellFrames = vesselCurve.computeFrenetFrames(100, false);
  function placeCells(T) {
    for (let i = 0; i < cellU.length; i++) {
      const [u0, a, rr, spin] = cellU[i], u = (u0 + T * .07) % 1, f = Math.round(u * 100);
      cellP.copy(vesselCurve.getPointAt(u))
        .addScaledVector(cellFrames.normals[f], Math.cos(a + T * .4) * rr * .026)
        .addScaledVector(cellFrames.binormals[f], Math.sin(a + T * .4) * rr * .026);
      vesselCurve.getTangentAt(u, cellT);
      cellQ.setFromUnitVectors(cellUp, cellT).multiply(new THREE.Quaternion().setFromAxisAngle(cellUp, spin + T));
      cells.setMatrixAt(i, cellM.compose(cellP, cellQ, cellS));
    }
    cells.instanceMatrix.needsUpdate = true;
  }
  placeCells(0);

  // ---------------------------------------------------------------- microscope on a lab cart (beside the bookcase)
  const afm = new THREE.Group(); afm.position.set(3.32, 0, -2.62); afm.rotation.y = -.55; world.add(afm);
  {
    afm.add(blob(.9, .75, .008));
    const H = .78;
    afm.add(at(rbox(.62, .05, .5, .02, mat.metal), 0, H, 0));
    afm.add(at(rbox(.56, .035, .44, .015, mat.metal), 0, .18, 0));
    for (const [x, z] of [[-.27, -.21], [.27, -.21], [-.27, .21], [.27, .21]]) afm.add(at(cyl(.014, .014, H - .02, mat.metal, 8), x, H / 2, z));
    const body = M('#d9dee7', { roughness: .4, ...kit.E(.4) });
    afm.add(at(rbox(.36, .06, .3, .02, body), 0, H + .055, 0));                     // base
    afm.add(at(rbox(.07, .42, .08, .02, body), 0, H + .29, -.1));                  // pillar
    afm.add(at(rbox(.09, .07, .3, .025, body), 0, H + .5, .02));                   // arm
    afm.add(at(cyl(.045, .038, .14, mat.dark, 16), 0, H + .41, .12));             // scanner head
    afm.add(at(cyl(.1, .1, .025, mat.dark, 24), 0, H + .11, .12));                // sample stage
    afm.add(at(cyl(.06, .06, .008, B(C.violet, 1.6), 20), 0, H + .127, .12));     // sample
    afm.add(at(new THREE.Mesh(new THREE.ConeGeometry(.012, .05, 8).rotateX(Math.PI), glow.green), 0, H + .315, .12)); // probe tip
    afm.add(at(cyl(.003, .003, .17, B(C.green, 1.4), 4), 0, H + .21, .12));      // laser
    // a small readout on the cart: the sample's height map
    const rd = kit.canvasTex(128, 96); const s = rd.ctx;
    s.fillStyle = '#05070c'; s.fillRect(0, 0, 128, 96);
    for (let y = 0; y < 96; y += 4) for (let x = 0; x < 128; x += 4) {
      const v = .5 + .5 * Math.sin(x * .09) * Math.cos(y * .11) + .3 * Math.exp(-((x - 80) ** 2 + (y - 40) ** 2) / 300);
      s.fillStyle = `hsl(${270 - v * 110}, 80%, ${18 + v * 38}%)`; s.fillRect(x, y, 4, 4);
    }
    rd.tex.needsUpdate = true;
    const screen = at(rbox(.22, .16, .02, .01, mat.dark), .2, H + .11, -.1); screen.rotation.x = -.35; afm.add(screen);
    const face = at(new THREE.Mesh(new THREE.PlaneGeometry(.19, .13), B('#ffffff', 1.15, { map: rd.tex })), .2, H + .113, -.088); face.rotation.x = -.35; afm.add(face);
  }

  // ---------------------------------------------------------------- Azure: a cloud hologram projected above the rack
  const azure = new THREE.Group(); azure.position.set(-2.72, 2.78, .3); world.add(azure);
  const azureMats = [];
  {
    const puffs = [[0, 0, 0, .24], [.24, -.04, .02, .19], [-.25, -.05, 0, .18], [.1, .14, -.02, .17], [-.1, .1, .03, .16]]
      .map(([x, y, z, r]) => new THREE.SphereGeometry(r, 18, 12).translate(x, y, z));
    const cloudGeo = mergeGeometries(puffs, false); puffs.forEach((g) => g.dispose());
    cloudGeo.scale(1, .8, .75);
    const fill = new THREE.MeshBasicMaterial({ color: new THREE.Color('#38bdf8').multiplyScalar(1.3), transparent: true, opacity: .32, depthWrite: false, blending: THREE.AdditiveBlending });
    const wire = new THREE.MeshBasicMaterial({ color: new THREE.Color(C.cyan).multiplyScalar(1.5), wireframe: true, transparent: true, opacity: .28, depthWrite: false, blending: THREE.AdditiveBlending });
    const cloud = new THREE.Group(); cloud.userData.dyn = true; azure.add(cloud);
    cloud.add(new THREE.Mesh(cloudGeo, fill));
    const shell = new THREE.Mesh(cloudGeo.clone(), wire); shell.scale.setScalar(1.02); cloud.add(shell);
    azureMats.push(fill, wire);
    // the projector on the rack's top and its beam
    const disc = at(cyl(.09, .1, .03, mat.dark, 20), 0, -.56, 0); azure.add(disc);
    azure.add(at(cyl(.06, .06, .006, glow.cyan, 20), 0, -.542, 0));
    const beam = at(new THREE.Mesh(new THREE.CylinderGeometry(.36, .07, .4, 24, 1, true), new THREE.MeshBasicMaterial({ color: '#38bdf8', transparent: true, opacity: .08, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide })), 0, -.33, 0);
    beam.userData.noRay = true; azure.add(beam);
    azure.userData.cloud = cloud;
  }

  world.updateMatrixWorld(true);
  [afm].forEach(mergeStatic);
  afm.traverse((o) => { if (o.isMesh && o.material.isMeshStandardMaterial) { o.castShadow = true; o.receiveShadow = true; } });
  stent.traverse((o) => { if (o.isMesh && o.material.isMeshStandardMaterial && !o.material.transparent) o.castShadow = true; });

  // gentle pulse of the cloud (static when motion is reduced)
  function animate(T, reduced) {
    if (reduced) return;
    placeCells(T);
    const k = Math.sin(T * 1.6);
    azure.userData.cloud.scale.setScalar(1 + k * .03);
    azure.userData.cloud.rotation.y = T * .15;
    azureMats[0].opacity = .3 + k * .06; azureMats[1].opacity = .26 + k * .06;
  }

  return { stent, afm, azure, animate };
}
