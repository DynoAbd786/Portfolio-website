// Post-processing: the scene, an outline around the hovered object, bloom, then tone mapping.
import * as THREE from 'three';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { OutlinePass } from 'three/addons/postprocessing/OutlinePass.js';
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js';
import { C } from './kit.js';

export function createPost(renderer, scene, camera, { width, height, dpr, mobile }) {
  const rt = new THREE.WebGLRenderTarget(width * dpr, height * dpr, { type: THREE.HalfFloatType, samples: 0 });
  const comp = new EffectComposer(renderer, rt);
  comp.addPass(new RenderPass(scene, camera));
  const outline = new OutlinePass(new THREE.Vector2(width, height), scene, camera);
  Object.assign(outline, { edgeStrength: 4.5, edgeGlow: .5, edgeThickness: 1.4, pulsePeriod: 0, enabled: false });
  outline.visibleEdgeColor.set(C.cyan); outline.hiddenEdgeColor.set('#0d3b48');
  comp.addPass(outline);
  const baseBloom = mobile ? .55 : .68;
  const bloom = new UnrealBloomPass(new THREE.Vector2(width, height), baseBloom, .55, .82);
  // bloom is a blur: on lower quality tiers its mip chain runs at reduced resolution
  let bloomScale = 1;
  const bloomSize = bloom.setSize.bind(bloom);
  bloom.setSize = (w, h) => bloomSize(Math.max(2, Math.round(w * bloomScale)), Math.max(2, Math.round(h * bloomScale)));
  comp.addPass(bloom);
  comp.addPass(new OutputPass());
  comp.setPixelRatio(dpr); comp.setSize(width, height);

  let selected = null;
  return {
    render() { renderer.toneMappingExposure = 1.2; comp.render(); },
    setSize(w, h, pixelRatio) { comp.setPixelRatio(pixelRatio); comp.setSize(w, h); },
    // outline one object (or nothing); no outlines on touch screens, where there is no hover
    setOutline(object, touch) {
      if (object === selected) return; selected = object;
      outline.selectedObjects = object ? [object] : []; outline.enabled = !!object && !touch;
    },
    setMSAA(n) { for (const t of [comp.renderTarget1, comp.renderTarget2]) if (t.samples !== n) { t.samples = n; t.dispose(); } },
    setBloom(strengthScale, scale) { bloom.strength = baseBloom * strengthScale; bloomScale = scale; },
    dispose() {
      for (const t of [comp.renderTarget1, comp.renderTarget2]) t.dispose();
      for (const pass of comp.passes) {
        for (const v of Object.values(pass)) {
          if (v && v.isWebGLRenderTarget) v.dispose();
          else if (Array.isArray(v)) v.forEach((x) => x && x.isWebGLRenderTarget && x.dispose());
          else if (v && v.isMaterial) v.dispose();
        }
        pass.dispose?.();
      }
      comp.dispose?.();
    },
  };
}
