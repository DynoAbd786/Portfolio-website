// The objects that stand for one project each and aren't part of the original workshop:
// the stent model, the microscope and the Azure cloud above the rack.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { V3, C } from './kit.js';
import { mergeStatic } from './room.js';

export function buildProps(world, kit) {
  const { M, B, rbox, cyl, at, blob, mat, glow } = kit;

  // ---------------------------------------------------------------- stent: a flow diverter inside a vessel with an aneurysm (desk, left end)
  const stent = new THREE.Group(); stent.position.set(-2.15, 1.77, -2.72); stent.rotation.y = .55; world.add(stent);
  {
    stent.add(blob(.62, .34, .004, true));
    stent.add(at(rbox(.52, .035, .2, .015, mat.dark), 0, .018, 0));
    for (const x of [-.19, .19]) stent.add(at(cyl(.011, .011, .17, mat.metal, 8), x, .12, 0));
    const curve = new THREE.CatmullRomCurve3([new V3(-.27, .2, 0), new V3(-.12, .215, .01), new V3(.04, .19, -.01), new V3(.27, .205, 0)]);
    const vesselMat = M('#d9534f', { transparent: true, opacity: .5, roughness: .3, depthWrite: false, side: THREE.DoubleSide });
    const vessel = new THREE.Mesh(new THREE.TubeGeometry(curve, 40, .045, 14, false), vesselMat); vessel.renderOrder = 2; stent.add(vessel);
    const sac = at(new THREE.Mesh(new THREE.SphereGeometry(.055, 16, 12), vesselMat), -.03, .262, 0); sac.renderOrder = 2; stent.add(sac);
    const lattice = new THREE.Mesh(new THREE.TubeGeometry(curve, 26, .037, 9, false), B(C.cyan, 1.2, { wireframe: true }));
    stent.add(lattice);
  }

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
    const k = Math.sin(T * 1.6);
    azure.userData.cloud.scale.setScalar(1 + k * .03);
    azure.userData.cloud.rotation.y = T * .15;
    azureMats[0].opacity = .3 + k * .06; azureMats[1].opacity = .26 + k * .06;
  }

  return { stent, afm, azure, animate };
}
