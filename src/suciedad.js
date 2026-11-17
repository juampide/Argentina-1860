// Mugre y desgaste: manchas de barro y hollín, chorreaduras de lluvia y grano fino, calculados en el espacio del mundo
// sobre cualquier material estándar, más una corrección de color general: menos saturado, tono polvoriento.
import * as THREE from 'three';

let gradeListo = false;
export function instalarGrade() {
  if (gradeListo) return; gradeListo = true;
  THREE.ShaderChunk.dithering_fragment += `
    {
      float lum = dot(gl_FragColor.rgb, vec3(0.299, 0.587, 0.114));
      vec3 c = mix(vec3(lum), gl_FragColor.rgb, 0.74);        // colores apagados
      c *= vec3(1.04, 0.99, 0.90);                             // polvo cálido
      c = (c - 0.5) * 1.08 + 0.48;                             // un poco más de contraste, algo más oscuro
      gl_FragColor.rgb = clamp(c, 0.0, 1.0);
    }`;
}

const COMUN = `
  varying vec3 vGW;
  float sH(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
  float sN(vec2 p){ vec2 i = floor(p), f = fract(p); f = f * f * (3.0 - 2.0 * f);
    return mix(mix(sH(i), sH(i + vec2(1.0, 0.0)), f.x), mix(sH(i + vec2(0.0, 1.0)), sH(i + vec2(1.0, 1.0)), f.x), f.y); }
  float sV(vec2 p){ vec2 i = floor(p), f = fract(p); float d1 = 8.0, d2 = 8.0;
    for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) { vec2 g = vec2(float(x), float(y)); vec2 o = vec2(sH(i + g), sH(i + g + 17.3)); vec2 r = g + o - f; float d = dot(r, r);
      if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d; }
    return sqrt(d2) - sqrt(d1); }
  float sF(vec2 p){ return 0.55 * sN(p) + 0.3 * sN(p * 2.13 + 3.1) + 0.15 * sN(p * 4.37 + 7.7); }`;

export function ensuciar(mat, fuerza = 1) {
  if (!mat || !mat.isMeshStandardMaterial || mat.userData.sucio) return;
  mat.userData.sucio = true;
  const gr = mat.userData.grietas || 0;
  mat.customProgramCacheKey = () => 'sucio' + fuerza + '_' + gr;
  const previo = mat.onBeforeCompile;
  mat.onBeforeCompile = (sh, r) => {
    if (previo) previo.call(mat, sh, r);
    sh.vertexShader = sh.vertexShader
      .replace('#include <common>', '#include <common>\nvarying vec3 vGW;')
      .replace('#include <begin_vertex>', `#include <begin_vertex>
        { vec4 gw = vec4(transformed, 1.0);
          #ifdef USE_INSTANCING
            gw = instanceMatrix * gw;
          #endif
          vGW = (modelMatrix * gw).xyz; }`);
    sh.fragmentShader = sh.fragmentShader
      .replace('#include <common>', '#include <common>' + COMUN)
      .replace('#include <color_fragment>', `#include <color_fragment>
        {
          vec3 g = vGW;
          float b1 = sF(g.xz * 0.9 + g.y * 0.8), b2 = sF(g.xz * 0.16 + g.y * 0.35 + 7.0);
          vec3 gn = normalize(cross(dFdx(vGW), dFdy(vGW))); float vert = 1.0 - abs(gn.y);   // 1 en paredes, 0 en el suelo
          float chor = sN(vec2((g.x + g.z) * 2.4, g.y * 0.3)) * vert;                 // chorreaduras verticales
          float d = clamp(b1 * 0.55 + b2 * 0.7 + chor * 0.3 - 0.62, 0.0, 1.0) * ${fuerza.toFixed(2)};
          vec3 mugre = vec3(0.34, 0.26, 0.18);
          diffuseColor.rgb = mix(diffuseColor.rgb, diffuseColor.rgb * mugre * 1.5, clamp(d * 1.5, 0.0, 0.85));
          diffuseColor.rgb *= 0.8 + 0.2 * sN(g.xz * 7.0 + g.y * 6.0);              // grano fino
          ${gr > 0 ? `{ vec2 cp = vec2(g.x + g.z, g.y) * 3.4; cp += 0.35 * vec2(sN(cp * 1.7), sN(cp * 1.7 + 9.0)); float cr = 1.0 - smoothstep(0.0, 0.05, sV(cp)); cr = max(cr, (1.0 - smoothstep(0.0, 0.04, sV(cp * 2.3 + 5.0))) * 0.5); diffuseColor.rgb *= 1.0 - ${gr.toFixed(2)} * cr * vert; }` : ''}
          float desgaste = smoothstep(0.78, 0.9, sF(g.xz * 1.7 + g.y * 1.3 + 21.0));      // raspones claros, como cal caída
          diffuseColor.rgb = mix(diffuseColor.rgb, diffuseColor.rgb * 1.25 + 0.03, desgaste * 0.4 * ${fuerza.toFixed(2)});
        }`);
  };
  mat.needsUpdate = true;
}

// recorre la escena y ensucia todo menos los seres vivos (la mugre fija al mundo "nadaría" sobre algo que se mueve)
export function ensuciarEscena(scene) {
  const visitar = (o, saltar) => {
    const u = o.userData || {};
    if (u.sucioForzado) saltar = false; else if (u.animar || u.vivo || u.noSucio) saltar = true;
    if (!saltar && o.isMesh && o.material) (Array.isArray(o.material) ? o.material : [o.material]).forEach((m) => ensuciar(m));
    o.children.forEach((c) => visitar(c, saltar));
  };
  scene.children.forEach((c) => visitar(c, false));
}
