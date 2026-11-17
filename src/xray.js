import * as THREE from 'three';

// Transparencia de lo que queda entre la cámara y el personaje: un hueco tramado (dither) alrededor
// de la línea de visión cámara→jugador. Solo recorta lo que está delante del personaje y por encima del suelo.
export const xray = {
  uJug: { value: new THREE.Vector3() },       // posición del jugador (a la altura del pecho)
  uDir: { value: new THREE.Vector3(0, -1, 0) }, // dirección de la cámara hacia el jugador
  uRad: { value: 5.2 },                       // radio del hueco en metros
};

export function aplicarXray(mat) {
  if (!mat || !mat.isMeshStandardMaterial || mat.userData.xray) return;
  mat.userData.xray = true;
  if (mat.side === THREE.FrontSide) mat.side = THREE.DoubleSide;   // para que el hueco muestre el interior de la cáscara y no el vacío
  const previo = mat.onBeforeCompile;
  mat.onBeforeCompile = (sh, r) => {
    if (previo) previo.call(mat, sh, r);
    sh.uniforms.uJug = xray.uJug; sh.uniforms.uDir = xray.uDir; sh.uniforms.uRad = xray.uRad;
    sh.vertexShader = sh.vertexShader
      .replace('#include <common>', '#include <common>\nvarying vec3 vXW;')
      .replace('#include <begin_vertex>', `#include <begin_vertex>
        {
          vec4 xw = vec4(transformed, 1.0);
          #ifdef USE_INSTANCING
            xw = instanceMatrix * xw;
          #endif
          vXW = (modelMatrix * xw).xyz;
        }`);
    sh.fragmentShader = sh.fragmentShader
      .replace('#include <common>', '#include <common>\nvarying vec3 vXW; uniform vec3 uJug; uniform vec3 uDir; uniform float uRad;')
      .replace('void main() {', `void main() {
        float xh = 0.0;
        {
          vec3 rel = vXW - uJug;
          float ad = dot(rel, -uDir);                 // cuánto está hacia la cámara
          vec3 lat = rel + uDir * ad;                 // distancia al rayo cámara→jugador
          float dd = length(lat);
          float hueco = (1.0 - smoothstep(uRad * 0.55, uRad, dd)) * smoothstep(0.4, 2.0, ad) * smoothstep(0.7, 1.7, vXW.y);
          float ruido = fract(52.9829189 * fract(dot(gl_FragCoord.xy, vec2(0.06711056, 0.00583715))));
          xh = hueco;
          if (hueco > ruido) discard;
        }`)
      .replace('#include <dithering_fragment>', `#include <dithering_fragment>
        if (!gl_FrontFacing && xh > 0.02) {                 // interior de las paredes y techos vistos por el hueco: oscuro, como una sección
          float k = clamp(vXW.y / 3.2, 0.0, 1.0);
          gl_FragColor = vec4(mix(vec3(0.075, 0.055, 0.04), vec3(0.2, 0.15, 0.11), k), 1.0);
        }`);
  };
  mat.needsUpdate = true;
}

// recorre la escena y aplica el efecto a todo menos el suelo y los personajes
export function aplicarXrayEscena(scene) {
  const visitar = (o) => {
    if (o.userData && (o.userData.sinXray || o.userData.noXray)) return;
    if (o.isMesh && o.material) (Array.isArray(o.material) ? o.material : [o.material]).forEach(aplicarXray);
    o.children.forEach(visitar);
  };
  scene.children.forEach(visitar);
}
