import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { registrar, setAgua, setTerreno } from './colision.js';

// El pago: 3,2 × 2,4 km. Coordenadas del dibujo en km (este → derecha, sur → abajo) pasadas a metros del mundo.
export const K = (kx, ky) => [(kx - 1.6) * 1000, (ky - 1.2) * 1000];
const ANCHO = 3200, ALTO = 2400;
const Y = new THREE.Vector3(0, 1, 0);

const h21 = (x, y) => { const s = Math.sin(x * 127.1 + y * 311.7) * 43758.5453; return s - Math.floor(s); };
function ruido(x, y) {
  const ix = Math.floor(x), iy = Math.floor(y), fx = x - ix, fy = y - iy, ux = fx * fx * (3 - 2 * fx), uy = fy * fy * (3 - 2 * fy);
  const a = h21(ix, iy), b = h21(ix + 1, iy), c = h21(ix, iy + 1), d = h21(ix + 1, iy + 1);
  return a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy;
}
export const hs = (i) => h21(i * 12.9898, i * 4.1414 + 7);
export const mix = (a, b, t) => a + (b - a) * t;
const sstep = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

// ---------- trazados (en km) ----------
export const ARROYO = [[1.4, -0.05], [1.38, 0.2], [1.25, 0.55], [1.18, 0.8], [1.16, 1.05], [1.15, 1.3], [1.25, 1.6], [1.4, 1.85], [1.52, 2.0]];
export const CAMINO = [[-0.05, 1.5], [0.4, 1.46], [0.8, 1.38], [1.15, 1.3], [1.6, 1.27], [2.15, 1.25], [2.7, 1.15], [3.25, 1.0]];
export const SENDAS = [
  [[0.75, 0.8], [0.9, 0.92], [1.0, 1.0], [1.08, 1.15], [1.15, 1.3]],       // estancia → paso
  [[0.59, 0.9], [0.68, 0.85], [0.75, 0.8]],                                // rancho de peones → estancia
  [[2.15, 1.25], [2.0, 1.6], [1.85, 1.9], [1.78, 2.0]],                    // posta → laguna
  [[2.15, 1.25], [2.25, 0.9], [2.3, 0.6]],                                 // posta → monte
  [[0.75, 0.8], [0.45, 0.95], [0.2, 1.1], [0.12, 1.15]],                   // estancia → fortín
  [[0.75, 0.8], [0.62, 1.3], [0.57, 1.6], [0.55, 1.85]],                   // estancia → puesto del baqueano
  [[0.75, 0.8], [0.95, 0.62], [1.17, 0.5]],                                // estancia → rancho de Casiana
];
export const LAGUNA = { c: [1.7, 2.05], rx: 0.21, ry: 0.14 };
export const MONTE = { c: [2.3, 0.45], rx: 0.33, ry: 0.2 };

export function suavizar(pts, paso = 12) {
  const c = new THREE.CatmullRomCurve3(pts.map(([x, y]) => new THREE.Vector3(x, 0, y)), false, 'catmullrom', 0.5);
  return c.getPoints(Math.max(8, pts.length * paso)).map((v) => [v.x, v.z]);
}

// ---------- materiales y texturas ----------
const cargador = new THREE.TextureLoader();
async function tex(url, srgb) {
  const t = await cargador.loadAsync(url); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8;
  if (srgb) t.colorSpace = THREE.SRGBColorSpace; return t;
}

export function pieza(geo, color, pos = [0, 0, 0], esc = [1, 1, 1], rot = [0, 0, 0]) {
  const g = geo.index ? geo.toNonIndexed() : geo.clone();
  g.deleteAttribute('uv');
  g.applyMatrix4(new THREE.Matrix4().compose(new THREE.Vector3(...pos), new THREE.Quaternion().setFromEuler(new THREE.Euler(...rot)), new THREE.Vector3(...esc)));
  const c = new THREE.Color(color), n = g.attributes.position.count, col = new Float32Array(n * 3);
  for (let i = 0; i < n; i++) { col[i * 3] = c.r; col[i * 3 + 1] = c.g; col[i * 3 + 2] = c.b; }
  g.setAttribute('color', new THREE.BufferAttribute(col, 3));
  return g;
}
export const ensamblar = (piezas) => mergeGeometries(piezas);
export const verde = (h, s, l) => new THREE.Color().setHSL(h, s, l);
export const ICO = new THREE.IcosahedronGeometry(1, 1), ICO0 = new THREE.IcosahedronGeometry(1, 0);
export const CONO = (r, h, n = 5) => new THREE.ConeGeometry(r, h, n);
export const CIL = (rs, ri, h, n = 6) => new THREE.CylinderGeometry(rs, ri, h, n);

// especies autóctonas en low poly
const ESPECIES = {
  ombu: () => ensamblar([
    pieza(CIL(0.9, 1.5, 2.6, 7), '#7b6a55', [0, 1.3, 0]),
    ...[0, 1, 2, 3].map((i) => pieza(CONO(0.6, 1.5, 4), '#7b6a55', [Math.cos(i * 1.57) * 1.3, 0.55, Math.sin(i * 1.57) * 1.3], [1, 1, 1], [Math.sin(i * 1.57) * 0.5, 0, -Math.cos(i * 1.57) * 0.5])),
    pieza(ICO, verde(0.27, 0.45, 0.3), [0, 5.2, 0], [5.6, 2.4, 5.6]),
    pieza(ICO, verde(0.28, 0.5, 0.36), [2.8, 4.5, 1.2], [3.6, 1.8, 3.6]),
    pieza(ICO, verde(0.26, 0.45, 0.33), [-2.6, 4.7, -1.5], [3.4, 1.7, 3.4])]),
  tala: () => ensamblar([
    pieza(CIL(0.18, 0.28, 1.8, 5), '#6b5a46', [0, 0.9, 0]),
    pieza(ICO, verde(0.22, 0.38, 0.32), [0, 2.4, 0], [1.9, 0.9, 1.9]),
    pieza(ICO, verde(0.23, 0.4, 0.37), [0.6, 3.1, 0.3], [1.3, 0.8, 1.3]),
    pieza(ICO, verde(0.21, 0.36, 0.3), [-0.7, 2.8, -0.4], [1.2, 0.7, 1.2])]),
  espinillo: () => ensamblar([
    pieza(CIL(0.12, 0.2, 2.2, 5), '#5f4d3b', [0, 1.1, 0]),
    pieza(ICO, verde(0.2, 0.42, 0.34), [0, 3.0, 0], [2.3, 0.7, 2.3]),
    ...[0, 1, 2, 3, 4].map((i) => pieza(ICO0, '#e6c635', [Math.cos(i * 1.3) * 1.3, 3.5, Math.sin(i * 1.3) * 1.3], [0.22, 0.22, 0.22]))]),
  coronillo: () => ensamblar([
    pieza(CIL(0.2, 0.35, 2.2, 5), '#5c4a3a', [0, 1.1, 0]),
    pieza(ICO, verde(0.3, 0.5, 0.22), [0, 3.6, 0], [2.4, 2.0, 2.4]),
    pieza(ICO, verde(0.31, 0.5, 0.27), [0.9, 4.4, 0.4], [1.5, 1.3, 1.5])]),
  sauce: () => ensamblar([
    pieza(CIL(0.25, 0.4, 3.2, 5), '#5d4c3c', [0, 1.6, 0]),
    pieza(ICO, verde(0.19, 0.42, 0.5), [0, 4.3, 0], [2.6, 1.3, 2.6]),
    ...[0, 1, 2, 3, 4, 5].map((i) => pieza(CONO(0.35, 3, 4), verde(0.2, 0.45, 0.46), [Math.cos(i * 1.05) * 2.4, 2.9, Math.sin(i * 1.05) * 2.4], [1, 1, 1], [Math.PI, 0, 0]))]),
  ceibo: () => ensamblar([
    pieza(CIL(0.2, 0.32, 2.6, 5), '#5a463a', [0, 1.3, 0]),
    pieza(ICO, verde(0.27, 0.45, 0.3), [0, 3.4, 0], [1.9, 1.5, 1.9]),
    ...[0, 1, 2, 3, 4, 5].map((i) => pieza(ICO0, '#cc2a2a', [Math.cos(i * 1.0) * 1.3, 3.8 + (i % 2) * 0.4, Math.sin(i * 1.0) * 1.3], [0.28, 0.28, 0.28]))]),
  cortadera: () => ensamblar([
    ...[0, 1, 2, 3, 4, 5, 6].map((i) => pieza(CONO(0.16, 1.7 + (i % 3) * 0.3, 3), verde(0.2, 0.3, 0.52), [Math.cos(i * 0.9) * 0.45, 0.85, Math.sin(i * 0.9) * 0.45], [1, 1, 1], [Math.sin(i * 0.9) * 0.18, 0, -Math.cos(i * 0.9) * 0.18])),
    ...[0, 1, 2].map((i) => pieza(ICO0, '#e9e1c8', [Math.cos(i * 2.1) * 0.5, 2.1 + i * 0.15, Math.sin(i * 2.1) * 0.5], [0.18, 0.55, 0.18]))]),
  tuna: () => ensamblar([
    pieza(ICO, verde(0.27, 0.4, 0.3), [0, 0.5, 0], [0.7, 0.14, 0.5]),
    pieza(ICO, verde(0.28, 0.42, 0.33), [0.4, 1.0, 0.1], [0.55, 0.12, 0.4], [0, 0, 0.5]),
    pieza(ICO, verde(0.26, 0.4, 0.3), [-0.35, 1.05, -0.1], [0.5, 0.12, 0.38], [0, 0, -0.6])]),
};

// ---------- caja con UV a escala de textura, techo a dos aguas ----------
function cajaUV(w, h, l, mat, esc = 3) {
  const g = new THREE.BoxGeometry(w, h, l), uv = g.attributes.uv;
  const dims = [[l, h], [l, h], [w, l], [w, l], [w, h], [w, h]];
  for (let f = 0; f < 6; f++) for (let k = 0; k < 4; k++) { const i = f * 4 + k; uv.setXY(i, uv.getX(i) * dims[f][0] / esc, uv.getY(i) * dims[f][1] / esc); }
  const m = new THREE.Mesh(g, mat); m.castShadow = m.receiveShadow = true; return m;
}
function geoTecho(a, l, rise, vol, esc = 1.6) {
  const hw = a / 2 + vol, hl = l / 2 + vol * 0.7, sl = Math.hypot(hw, rise), P = [], U = [];
  const quad = (p0, p1, p2, p3, u1, v1) => { P.push(...p0, ...p1, ...p2, ...p0, ...p2, ...p3); U.push(0, 0, u1, 0, u1, v1, 0, 0, u1, v1, 0, v1); };
  quad([-hw, 0, -hl], [-hw, 0, hl], [0, rise, hl], [0, rise, -hl], 2 * hl / esc, sl / esc);
  quad([hw, 0, hl], [hw, 0, -hl], [0, rise, -hl], [0, rise, hl], 2 * hl / esc, sl / esc);
  const rg = rise * (a / 2) / hw;
  for (const z of [l / 2, -l / 2]) { P.push(-a / 2, 0, z, a / 2, 0, z, 0, rg, z); U.push(0, 0, a / esc, 0, a / esc / 2, rg / esc); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(P, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(U, 2)); g.computeVertexNormals(); return g;
}

export async function construirCampo({ scene, T }) {
  // ---------- texturas propias del campo ----------
  const [pajaC, pajaN, maderaC, maderaN] = await Promise.all([tex('./tex/paja_c.jpg', true), tex('./tex/paja_n.jpg'), tex('./tex/madera_c.jpg', true), tex('./tex/madera_n.jpg')]);
  const std = (o) => new THREE.MeshStandardMaterial({ roughness: 1, flatShading: true, side: THREE.DoubleSide, ...o });
  const mats = {
    adobe: std({ map: T.revC, normalMap: T.revN, color: '#e0bf94' }),
    blanco: std({ map: T.revC, normalMap: T.revN, color: '#f1e8d4' }),
    paja: std({ map: pajaC, normalMap: pajaN, color: new THREE.Color('#e8cf8a').multiplyScalar(2.3) }),
    teja: std({ map: T.tejaC, normalMap: T.tejaN, color: new THREE.Color('#f2d9cc').multiplyScalar(1.5) }),
    madera: std({ map: maderaC, normalMap: maderaN, color: '#d8c4a8' }),
    oscuro: std({ color: '#2b2118' }),
    postigo: std({ color: '#3f6049' }),
    hierro: std({ color: '#26231f', metalness: 0.3 }),
    piedra: std({ color: '#8d8478' }),
    veg: new THREE.MeshStandardMaterial({ vertexColors: true, flatShading: true, roughness: 1 }),
  };
  mats.adobe.userData.grietas = 0.4; mats.blanco.userData.grietas = 0.2;

  // ---------- máscara del mapa: rojo = agua, verde = caminos ----------
  const W = 2048, H = 1536, pxm = (kx, ky) => [kx / 3.2 * W, ky / 2.4 * H];
  const cv = document.createElement('canvas'); cv.width = W; cv.height = H;
  const cx = cv.getContext('2d', { willReadFrequently: true });
  cx.fillStyle = '#000'; cx.fillRect(0, 0, W, H);
  const trazo = (pts, color, ancho_m) => {
    cx.strokeStyle = color; cx.lineWidth = ancho_m * (W / ANCHO); cx.lineCap = 'round'; cx.lineJoin = 'round'; cx.beginPath();
    suavizar(pts).forEach(([kx, ky], i) => { const [x, y] = pxm(kx, ky); i ? cx.lineTo(x, y) : cx.moveTo(x, y); }); cx.stroke();
  };
  trazo(ARROYO, '#ff0000', 9);
  { const [x, y] = pxm(...LAGUNA.c); cx.fillStyle = '#ff0000'; cx.beginPath(); cx.ellipse(x, y, LAGUNA.rx * 1000 * W / ANCHO, LAGUNA.ry * 1000 * W / ANCHO, 0, 0, 7); cx.fill(); }
  trazo(CAMINO, '#00ff00', 7);
  SENDAS.forEach((s) => trazo(s, '#00ff00', 3.6));
  const datos = cx.getImageData(0, 0, W, H).data;
  const cb = document.createElement('canvas'); cb.width = W; cb.height = H;
  const cbx = cb.getContext('2d', { willReadFrequently: true }); cbx.filter = 'blur(7px)'; cbx.drawImage(cv, 0, 0);
  const blur = cbx.getImageData(0, 0, W, H).data;
  const muestra = (arr, x, z, canal) => {
    const ix = Math.floor((x + ANCHO / 2) / ANCHO * W), iy = Math.floor((z + ALTO / 2) / ALTO * H);
    if (ix < 0 || iy < 0 || ix >= W || iy >= H) return -1;
    return arr[(iy * W + ix) * 4 + canal] / 255;
  };
  const enAgua = (x, z) => { const r = muestra(datos, x, z, 0); if (r < 0) return true; return r > 0.55 && muestra(datos, x, z, 1) < 0.4; };
  const enCamino = (x, z) => muestra(datos, x, z, 1) > 0.5;
  setAgua(enAgua);

  // ---------- terreno con lomas suaves, aplanado en los sitios ----------
  const SITIOS = [
    { p: K(0.75, 0.75), r: 75 }, { p: K(0.59, 0.9), r: 40 }, { p: K(2.15, 1.25), r: 80 },
    { p: K(1.0, 1.0), r: 25 }, { p: K(0.12, 1.15), r: 45 }, { p: K(0.55, 1.85), r: 28 }, { p: K(1.17, 0.5), r: 30 },
  ];
  const puestoP = K(0.55, 1.85);
  const base = (x, z) => {
    let h = 2.2 * ruido(x * 0.0032 + 10, z * 0.0032) + 0.9 * ruido(x * 0.011, z * 0.011 + 5) + 0.25 * ruido(x * 0.05, z * 0.05) - 1.2;
    const d2 = (x - puestoP[0]) ** 2 + (z - puestoP[1]) ** 2; h += 3.2 * Math.exp(-d2 / (2 * 70 * 70));
    return h;
  };
  SITIOS.forEach((s) => { s.h = base(s.p[0], s.p[1]); });
  const altura = (x, z) => {
    let h = base(x, z);
    for (const s of SITIOS) { const w = sstep(s.r * 1.7, s.r, Math.hypot(x - s.p[0], z - s.p[1])); if (w > 0) h = mix(h, s.h, w); }
    const c = muestra(blur, x, z, 0); if (c > 0) h -= 1.5 * Math.min(1, c * 1.4);
    return h;
  };
  setTerreno(altura);

  const geo = new THREE.PlaneGeometry(ANCHO, ALTO, 320, 240); geo.rotateX(-Math.PI / 2);
  { const p = geo.attributes.position; for (let i = 0; i < p.count; i++) p.setY(i, altura(p.getX(i), p.getZ(i))); geo.computeVertexNormals(); }
  const maskTex = new THREE.CanvasTexture(cv); maskTex.colorSpace = THREE.NoColorSpace; maskTex.generateMipmaps = true;
  const sueloMat = new THREE.MeshStandardMaterial({ roughness: 1 });
  sueloMat.onBeforeCompile = (sh) => {
    sh.uniforms.mascara = { value: maskTex }; sh.uniforms.tierra = { value: T.tieC }; sh.uniforms.pasto = { value: T.pasto };
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec2 vMk; varying vec3 vWp;').replace('#include <uv_vertex>', '#include <uv_vertex>\nvMk = uv; vWp = (modelMatrix * vec4(position, 1.0)).xyz;');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', `#include <common>
      varying vec2 vMk; varying vec3 vWp; uniform sampler2D mascara; uniform sampler2D tierra; uniform sampler2D pasto;
      float h21(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
      float ruido(vec2 p){ vec2 i = floor(p), f = fract(p); f = f * f * (3.0 - 2.0 * f);
        return mix(mix(h21(i), h21(i + vec2(1.0, 0.0)), f.x), mix(h21(i + vec2(0.0, 1.0)), h21(i + vec2(1.0, 1.0)), f.x), f.y); }`)
      .replace('#include <map_fragment>', `
        vec2 w = vWp.xz;
        float n1 = ruido(w * 0.012), n2 = ruido(w * 0.06 + 3.0);
        vec3 pas = texture2D(pasto, w / 5.0).rgb * vec3(0.82, 1.0, 0.52) * 1.12;
        vec3 sec = texture2D(tierra, w / 4.0).rgb * 1.35;
        vec3 verde = mix(pas, pas * vec3(1.18, 1.1, 0.8), smoothstep(0.3, 0.7, n2));
        vec3 paja = vec3(0.56, 0.47, 0.27);
        vec3 base = mix(verde, mix(verde, paja, 0.7), smoothstep(0.52, 0.82, n1));
        base = mix(base, sec, smoothstep(0.78, 0.95, n1) * 0.6);
        vec4 mk = texture2D(mascara, vMk);
        float agua = mk.r * (1.0 - mk.g);
        base = mix(base, sec * 0.95, clamp(mk.g * 1.3, 0.0, 1.0));
        base *= 0.9 + 0.2 * ruido(w * 0.35);
        base = mix(base, sec * 0.5, smoothstep(0.7, 0.95, ruido(w * 0.09 + 11.0)) * 0.45);
        base = mix(base, sec * 0.55, smoothstep(0.0, 0.5, mk.r) * (1.0 - agua));
        base = mix(base, vec3(0.13, 0.36, 0.46), clamp(agua * 1.6, 0.0, 1.0));
        diffuseColor.rgb *= base;`);
  };
  const suelo = new THREE.Mesh(geo, sueloMat); suelo.receiveShadow = true; suelo.userData.noXray = true; scene.add(suelo);

  // ---------- utilidades de colocación ----------
  const libreDeSitios = (x, z, margen = 1) => !SITIOS.some((s) => Math.hypot(x - s.p[0], z - s.p[1]) < s.r * margen);
  const puedeCrecer = (x, z) => !enAgua(x, z) && !enCamino(x, z);
  const poly = (cxm, czm, w, l, rot) => {
    const c = Math.cos(rot), s = Math.sin(rot);
    return [[-w / 2, -l / 2], [w / 2, -l / 2], [w / 2, l / 2], [-w / 2, l / 2]].map(([lx, lz]) => [cxm + lx * c + lz * s, czm - lx * s + lz * c]);
  };
  const colocar = (geom, lista, castShadow = true) => {
    const im = new THREE.InstancedMesh(geom, mats.veg, Math.max(1, lista.length)); im.count = lista.length;
    const m = new THREE.Matrix4(), q = new THREE.Quaternion(), s = new THREE.Vector3(), p = new THREE.Vector3();
    lista.forEach((t, i) => { q.setFromAxisAngle(Y, t.r); s.setScalar(t.s); p.set(t.x, altura(t.x, t.z), t.z); m.compose(p, q, s); im.setMatrixAt(i, m); });
    im.castShadow = castShadow; im.frustumCulled = false; scene.add(im); return im;
  };
  let sem = 1;
  const dispersar = (n, enRegion, { margenSitio = 1, escala = [0.8, 1.3], filtro = puedeCrecer } = {}) => {
    const lista = []; let intentos = 0;
    while (lista.length < n && intentos++ < n * 30) {
      const [x, z] = enRegion(hs(sem++), hs(sem++), hs(sem++));
      if (!filtro(x, z) || !libreDeSitios(x, z, margenSitio)) continue;
      lista.push({ x, z, s: mix(escala[0], escala[1], hs(sem++)), r: hs(sem++) * 6.28 });
    }
    return lista;
  };
  const elipse = (c, rx, ry) => (a, b, c2) => { const t = a * 6.28, r = Math.sqrt(b) * (0.9 + 0.1 * c2); const [mx, mz] = K(...c); return [mx + Math.cos(t) * rx * 1000 * r, mz + Math.sin(t) * ry * 1000 * r]; };
  const rectKm = (x0, y0, x1, y1) => (a, b) => K(mix(x0, x1, a), mix(y0, y1, b));

  // ---------- flora autóctona ----------
  colocar(ESPECIES.tala(), dispersar(170, elipse(MONTE.c, MONTE.rx, MONTE.ry), { escala: [0.8, 1.5] }));
  colocar(ESPECIES.coronillo(), dispersar(45, elipse(MONTE.c, MONTE.rx, MONTE.ry), { escala: [0.8, 1.3] }));
  colocar(ESPECIES.espinillo(), dispersar(60, elipse(MONTE.c, MONTE.rx * 1.3, MONTE.ry * 1.3), { escala: [0.8, 1.3] }));
  colocar(ESPECIES.espinillo(), dispersar(40, rectKm(0.0, 1.55, 1.5, 2.4), { escala: [0.8, 1.2] }));
  const ribera = [];
  suavizar(ARROYO, 30).forEach(([kx, ky], i) => {
    if (i % 2) return; const [x, z] = K(kx, ky), lado = hs(i + 5) > 0.5 ? 1 : -1, d = 9 + hs(i + 9) * 14;
    const t = { x: x + lado * d, z: z + (hs(i + 11) - 0.5) * 12, s: 0.8 + hs(i + 13) * 0.6, r: hs(i + 15) * 6.28 };
    if (puedeCrecer(t.x, t.z) && libreDeSitios(t.x, t.z, 1)) ribera.push(t);
  });
  colocar(ESPECIES.sauce(), ribera.filter((_, i) => i % 3 !== 2)); colocar(ESPECIES.ceibo(), ribera.filter((_, i) => i % 3 === 2));
  colocar(ESPECIES.cortadera(), dispersar(260, elipse(LAGUNA.c, LAGUNA.rx * 1.7, LAGUNA.ry * 1.9), { filtro: (x, z) => !enAgua(x, z) && !enCamino(x, z) }));
  colocar(ESPECIES.cortadera(), dispersar(380, rectKm(0.0, 1.55, 1.5, 2.4), { escala: [0.8, 1.5] }));
  colocar(ESPECIES.cortadera(), dispersar(120, rectKm(0.0, 0.0, 3.2, 2.4), { escala: [0.7, 1.2] }));
  // ombúes solitarios y de referencia (se ven desde lejos)
  const ombuPos = [[K(0.75, 0.75), -17, -15], [K(2.15, 1.25), -30, 24], [K(1.0, 1.0), 12, -10], [K(1.9, 1.32), 0, 0], [K(0.59, 0.9), 14, -10], [K(1.4, 0.4), 0, 0], [K(2.7, 0.9), 0, 0], [K(0.9, 2.2), 0, 0]];
  colocar(ESPECIES.ombu(), ombuPos.map(([p, dx, dz], i) => ({ x: p[0] + dx, z: p[1] + dz, s: 1 + hs(i + 3) * 0.3, r: hs(i) * 6.28 })));
  ombuPos.forEach(([p, dx, dz]) => registrar(poly(p[0] + dx, p[1] + dz, 2.6, 2.6, 0)));

  // ---------- cercos: postes y rieles ----------
  const postes = [], rieles = [];
  const cerco = (pts, { cerrado = true, hueco = -1, alto = 1.4, paso = 3 } = {}) => {
    const n = cerrado ? pts.length : pts.length - 1;
    if (cerrado) cercos.push(pts);
    for (let i = 0; i < n; i++) {
      const [x0, z0] = pts[i], [x1, z1] = pts[(i + 1) % pts.length], L = Math.hypot(x1 - x0, z1 - z0), ang = Math.atan2(x1 - x0, z1 - z0);
      const np = Math.max(1, Math.round(L / paso));
      for (let k = 0; k <= np; k++) { const t = k / np; postes.push({ x: mix(x0, x1, t), z: mix(z0, z1, t), alto }); }
      // la puerta es un hueco de 4 m en el medio del lado; el resto del lado lleva rieles y bloquea el paso
      const tramos = i === hueco ? [[0, (1 - 4 / L) / 2], [(1 + 4 / L) / 2, 1]] : [[0, 1]];
      for (const [ta, tb] of tramos) {
        const ax = mix(x0, x1, ta), az = mix(z0, z1, ta), bx = mix(x0, x1, tb), bz = mix(z0, z1, tb), Lt = L * (tb - ta);
        for (const f of [0.45, 0.85]) rieles.push({ x: (ax + bx) / 2, z: (az + bz) / 2, L: Lt, ang, y: alto * f });
        registrar(poly((ax + bx) / 2, (az + bz) / 2, 0.5, Lt, ang));
      }
    }
  };
  const rect4 = (cxm, czm, w, l) => [[cxm - w / 2, czm - l / 2], [cxm + w / 2, czm - l / 2], [cxm + w / 2, czm + l / 2], [cxm - w / 2, czm + l / 2]];

  // ---------- construcciones ----------
  const grupo = new THREE.Group(); scene.add(grupo);
  const edificios = [], cercos = [], interiores = [];
  // varas de madera que atan el adobe y postes en las esquinas (como en las fotos de época)
  const varas = (g, w, h, l) => {
    const m = std({ color: '#7d5f40' });
    for (const y of [0.75, 1.65]) { if (y > h - 0.3) continue;
      for (const sz of [-1, 1]) { const b = new THREE.Mesh(new THREE.BoxGeometry(w + 0.12, 0.05, 0.05), m); b.position.set(0, y, sz * (l / 2 + 0.03)); g.add(b); }
      for (const sx of [-1, 1]) { const b = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.05, l + 0.12), m); b.position.set(sx * (w / 2 + 0.03), y, 0); g.add(b); } }
    for (const sx of [-1, 1]) for (const sz of [-1, 1]) { const b = new THREE.Mesh(new THREE.BoxGeometry(0.13, h, 0.13), m); b.position.set(sx * (w / 2 + 0.02), h / 2, sz * (l / 2 + 0.02)); g.add(b); }
  };
  const casa = ({ x, z, rot = 0, w = 7, l = 5, h = 2.6, rise = 1.6, pared = mats.adobe, techo = mats.paja, galeria = false, ruina = false, puerta = true, sinMuro = false }) => {
    const g = new THREE.Group();
    if (!sinMuro) { const muro = cajaUV(w, h, l, pared); muro.position.y = h / 2; g.add(muro); if (pared === mats.adobe) varas(g, w, h, l); }
    if (techo === mats.paja && rise < 2) rise = 2.2;
    const largo = Math.max(w, l), ancho = Math.min(w, l);
    const tm = new THREE.Mesh(geoTecho(ancho, largo, rise, 0.55), techo); tm.castShadow = tm.receiveShadow = true; tm.position.y = h; if (w > l) tm.rotation.y = Math.PI / 2;
    if (ruina) { tm.scale.set(1, 0.7, 0.55); tm.position.z = largo * 0.18; tm.rotation.z = 0.08; }
    if (techo === mats.paja) { const vig = new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.16, largo + 1.9), mats.madera); vig.position.y = rise + 0.03; vig.castShadow = true; tm.add(vig); }
    g.add(tm); g.userData.techo = tm;
    const caja = (bw, bh, bd, m, px, py, pz) => { const b = cajaUV(bw, bh, bd, m, 1.5); b.position.set(px, py, pz); g.add(b); return b; };
    if (puerta && !sinMuro) caja(0.95, 1.9, 0.14, mats.oscuro, 0, 0.95, l / 2 + 0.04);
    for (const sx of [-1, 1]) { if (w > 5 && !sinMuro) { caja(0.8, 0.9, 0.12, mats.oscuro, sx * w * 0.3, 1.5, l / 2 + 0.04); caja(0.2, 1.0, 0.16, mats.postigo, sx * w * 0.3 - 0.5, 1.5, l / 2 + 0.06); caja(0.2, 1.0, 0.16, mats.postigo, sx * w * 0.3 + 0.5, 1.5, l / 2 + 0.06); } }
    if (galeria) {
      const gz = l / 2 + 1.9;
      for (let k = 0; k <= Math.round(w / 2.6); k++) { const px = -w / 2 + k * (w / Math.round(w / 2.6)); if (sinMuro && Math.abs(px) < 1.3) continue; const p = new THREE.Mesh(CIL(0.1, 0.12, h + 0.2, 6), mats.madera); p.position.set(px, (h + 0.2) / 2, gz); p.castShadow = true; g.add(p); }
      const al = cajaUV(w + 0.8, 0.14, 2.8, mats.madera, 2); al.position.set(0, h + 0.15, l / 2 + 1.3); al.rotation.x = 0.1; g.add(al); g.userData.alero = al;
    }
    g.position.set(x, altura(x, z), z); g.rotation.y = rot; g.userData.sinXray = true; grupo.add(g);
    edificios.push([x, z, w, l, rot]);
    if (!sinMuro) registrar(poly(x, z, w + 0.4, l + 0.4, rot));
    return g;
  };
  const cartel = (texto, x, y, z, rot, w = 4.2) => {
    const c = document.createElement('canvas'); c.width = 512; c.height = 128; const k = c.getContext('2d');
    k.fillStyle = '#e8dcc0'; k.fillRect(0, 0, 512, 128); k.strokeStyle = '#4a3220'; k.lineWidth = 8; k.strokeRect(4, 4, 504, 120);
    k.fillStyle = '#7a1d14'; k.font = 'bold 50px Georgia, serif'; k.textAlign = 'center'; k.textBaseline = 'middle'; k.fillText(texto, 256, 66);
    const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace;
    const m = new THREE.Mesh(new THREE.PlaneGeometry(w, w / 4), new THREE.MeshStandardMaterial({ map: t, roughness: 1, side: THREE.DoubleSide }));
    m.position.set(x, y, z); m.rotation.y = rot; m.castShadow = true; grupo.add(m); return m;
  };
  const fogon = (x, z) => {
    const g = new THREE.Group();
    for (let i = 0; i < 7; i++) { const a = i * 0.9; const p = new THREE.Mesh(ICO0, mats.piedra); p.scale.setScalar(0.17); p.position.set(Math.cos(a) * 0.55, 0.1, Math.sin(a) * 0.55); g.add(p); }
    for (let i = 0; i < 4; i++) { const l = new THREE.Mesh(CIL(0.05, 0.05, 0.9, 5), mats.madera); l.position.y = 0.18; l.rotation.set(1.3, i * 0.8, 0); g.add(l); }
    const llama = new THREE.Mesh(CONO(0.28, 0.8, 5), new THREE.MeshStandardMaterial({ color: '#ff9a2a', emissive: '#ff6a10', emissiveIntensity: 1.4, flatShading: true }));
    llama.position.y = 0.5; llama.userData.noXray = true; g.add(llama);
    g.position.set(x, altura(x, z), z); g.userData.sinXray = true; grupo.add(g);
  };
  const palenque = (x, z, rot, n = 4) => {
    const g = new THREE.Group();
    for (let i = 0; i < n; i++) { const p = new THREE.Mesh(CIL(0.1, 0.12, 1.6, 6), mats.madera); p.position.set(-(n - 1) * 0.9 + i * 1.8, 0.8, 0); p.castShadow = true; g.add(p); }
    const v = cajaUV((n - 1) * 1.8 + 0.4, 0.12, 0.14, mats.madera, 1.5); v.position.y = 1.35; g.add(v);
    g.position.set(x, altura(x, z), z); g.rotation.y = rot; grupo.add(g);
  };
  const pozo = (x, z) => {
    const g = new THREE.Group(); const b = new THREE.Mesh(CIL(0.7, 0.8, 0.7, 8), mats.piedra); b.position.y = 0.35; g.add(b);
    for (const sx of [-1, 1]) { const p = new THREE.Mesh(CIL(0.07, 0.07, 1.7, 5), mats.madera); p.position.set(sx * 0.75, 1.0, 0); g.add(p); }
    const t = new THREE.Mesh(CIL(0.05, 0.05, 1.6, 5), mats.madera); t.rotation.z = Math.PI / 2; t.position.y = 1.8; g.add(t);
    g.position.set(x, altura(x, z), z); grupo.add(g); registrar(poly(x, z, 1.6, 1.6, 0));
  };
  const rep = (...a) => a;

  // Habitación con interior: techo y paredes propias (con puerta al frente), se baja lo que da a la cámara cuando entrás
  const habitacion = ({ x, z, rot = 0, w, l, h = 2.6, rise = 2.2, pared = mats.adobe, techo = mats.paja, amueblar }) => {
    const T = 0.35, PU = 1.4, c = Math.cos(rot), s = Math.sin(rot), lado = w / 2 - PU / 2;
    const g = casa({ x, z, rot, w, l, h, rise, pared, techo, sinMuro: true });
    const loc = (lx, lz) => [x + lx * c + lz * s, z - lx * s + lz * c];
    const obst = (lx, lz, pw, pl) => { const [wx, wz] = loc(lx, lz); registrar(poly(wx, wz, pw, pl, rot)); };
    const paredes = [], vm = std({ color: '#7d5f40' });
    const mkPared = (nx, nz, px, pz, piezas, vara) => {
      const grp = new THREE.Group(); grp.position.set(px, 0, pz);
      for (const [pw, ph, pl, qx, qy, qz] of piezas) { const m = cajaUV(pw, ph, pl, pared); m.position.set(qx, qy, qz); grp.add(m); }
      if (vara) for (const [len, qx, horiz] of vara) for (const y of [0.75, 1.65]) { if (y > h - 0.3) continue; const b = new THREE.Mesh(horiz ? new THREE.BoxGeometry(len, 0.05, 0.05) : new THREE.BoxGeometry(0.05, 0.05, len), vm); b.position.set(horiz ? qx : (nx > 0 ? 1 : -1) * (T / 2 + 0.03), y, horiz ? (nz > 0 ? 1 : -1) * (T / 2 + 0.03) : qx); grp.add(b); }
      g.add(grp); paredes.push({ w: grp, nx, nz, k: 1 }); return grp;
    };
    const frente = mkPared(0, 1, 0, l / 2 - T / 2, [[lado, h, T, -(PU / 2 + lado / 2), h / 2, 0], [lado, h, T, PU / 2 + lado / 2, h / 2, 0], [PU, h - 2.0, T, 0, 2.0 + (h - 2.0) / 2, 0]],
      [[lado, -(PU / 2 + lado / 2), true], [lado, PU / 2 + lado / 2, true]]);
    { const v = cajaUV(0.55, 0.6, 0.1, mats.oscuro); v.position.set(PU / 2 + lado * 0.5, 1.5, T / 2 + 0.03); frente.add(v); const p = cajaUV(0.6, 0.64, 0.06, mats.madera, 1.5); p.position.set(PU / 2 + lado * 0.5, 1.5, T / 2 + 0.07); frente.add(p); }
    mkPared(0, -1, 0, -(l / 2 - T / 2), [[w, h, T, 0, h / 2, 0]], [[w, 0, true]]);
    mkPared(-1, 0, -(w / 2 - T / 2), 0, [[T, h, l - 2 * T, 0, h / 2, 0]], [[l - 2 * T, 0, false]]);
    mkPared(1, 0, w / 2 - T / 2, 0, [[T, h, l - 2 * T, 0, h / 2, 0]], [[l - 2 * T, 0, false]]);
    obst(0, -(l / 2 - T / 2), w, T); obst(-(w / 2 - T / 2), 0, T, l); obst(w / 2 - T / 2, 0, T, l);
    obst(-(PU / 2 + lado / 2), l / 2 - T / 2, lado, T); obst(PU / 2 + lado / 2, l / 2 - T / 2, lado, T);
    const add = (m, px, py, pz) => { m.position.set(px, py, pz); g.add(m); return m; };
    const caja = (bw, bh, bd, mat, px, py, pz, esc = 1.5) => add(cajaUV(bw, bh, bd, mat, esc), px, py, pz);
    const col = (cc, o = {}) => std({ color: cc, ...o });
    caja(w - 2 * T, 0.08, l - 2 * T, col('#7a6246'), 0, 0, 0, 2);                      // piso de tierra apisonada
    if (amueblar) amueblar({ g, add, caja, col, obst, w, l, h, T });
    interiores.push({
      actualizar(dt, p, yaw) {
        const dx = p.x - x, dz = p.z - z, lx = dx * c - dz * s, lz = dx * s + dz * c;
        const dentro = Math.abs(lx) < w / 2 - T / 2 && lz > -l / 2 && lz < l / 2 + 0.25;
        const cam = [Math.cos(yaw), Math.sin(yaw)];
        for (const q of paredes) { const wx = q.nx * c + q.nz * s, wz = -q.nx * s + q.nz * c; const obj = dentro && (wx * cam[0] + wz * cam[1]) > 0.25 ? 0.1 : 1; q.k += (obj - q.k) * Math.min(1, dt * 7); q.w.scale.y = q.k; }
        g.userData.techo.visible = !dentro; this.dentro = dentro;
      },
    });
    return g;
  };
  const amueblarRancho = ({ add, caja, col, obst }) => {
    const oscuro = col('#3a2a1c'), cal = col('#e9e2cf'), hierro = col('#26231f', { metalness: 0.3 }), barro = col('#a8663a');
    // fogón con campana y olla colgada, como en las fotos
    caja(1.5, 0.8, 0.9, cal, -2.35, 0.4, -1.45, 2); caja(1.2, 0.05, 0.7, col('#3a3530'), -2.35, 0.82, -1.45);
    { const campana = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.75, 0.8, 4).rotateY(Math.PI / 4), cal); add(campana, -2.35, 1.4, -1.55); }
    caja(0.4, 1.2, 0.4, cal, -2.35, 2.2, -1.65, 2);
    { const brasas = new THREE.Mesh(new THREE.CircleGeometry(0.22, 8).rotateX(-Math.PI / 2), std({ color: '#ff9a2a', emissive: '#ff5a10', emissiveIntensity: 1.2 })); brasas.userData.noXray = true; add(brasas, -2.35, 0.86, -1.45); }
    { const cad = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.8, 4), hierro); add(cad, -2.35, 1.5, -1.45); const olla = new THREE.Mesh(new THREE.CylinderGeometry(0.26, 0.2, 0.28, 8), hierro); add(olla, -2.35, 1.0, -1.45); }
    obst(-2.35, -1.45, 1.5, 0.9);
    // catre con manta, mesa con banquito, tinaja y baúl
    caja(1.9, 0.12, 0.9, oscuro, 2.2, 0.38, -1.35); for (const sx of [-0.85, 0.85]) for (const sz of [-0.4, 0.4]) caja(0.08, 0.38, 0.08, oscuro, 2.2 + sx, 0.19, -1.35 + sz);
    caja(1.8, 0.1, 0.82, col('#a8553a'), 2.2, 0.49, -1.35); caja(0.5, 0.12, 0.6, col('#d9cfb2'), 2.9, 0.58, -1.35); obst(2.2, -1.35, 1.95, 0.95);
    caja(1.1, 0.07, 0.7, col('#b08a5c'), 0.4, 0.78, -1.3); for (const sx of [-0.45, 0.45]) for (const sz of [-0.28, 0.28]) caja(0.07, 0.78, 0.07, oscuro, 0.4 + sx, 0.39, -1.3 + sz);
    obst(0.4, -1.3, 1.1, 0.7); caja(0.45, 0.4, 0.35, col('#b08a5c'), 0.4, 0.2, -0.5); obst(0.4, -0.5, 0.45, 0.35);
    { const mate = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.04, 0.1, 6), col('#6a8a52')); add(mate, 0.2, 0.86, -1.3); const pava = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.13, 0.18, 7), hierro); add(pava, 0.65, 0.88, -1.3); }
    { const tin = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.22, 0.72, 9), barro); add(tin, -0.95, 0.36, -1.65); obst(-0.95, -1.65, 0.6, 0.6); }
    caja(0.85, 0.5, 0.5, col('#7a4a2a'), 2.7, 0.25, 1.4); obst(2.7, 1.4, 0.85, 0.5);
    // palas, cucharón y ristra colgados de la pared del fondo
    for (const [hx, hy] of [[-0.3, 1.5], [0.1, 1.6], [1.0, 1.7]]) { caja(0.18, 0.4, 0.03, col('#8a6a44'), hx, hy, -1.95); caja(0.04, 0.5, 0.04, oscuro, hx, hy + 0.4, -1.95); }
    { const ristra = new THREE.Mesh(new THREE.ConeGeometry(0.12, 0.7, 5).rotateX(Math.PI), col('#b3342b')); add(ristra, 1.7, 1.9, -1.9); }
  };

  // Estancia La Soledad
  { const [ex, ez] = K(0.75, 0.75);
    casa({ x: ex, z: ez, rot: 0, w: 16, l: 8, h: 3.4, rise: 2.4, pared: mats.blanco, techo: mats.teja, galeria: true });
    casa({ x: ex + 24, z: ez - 8, rot: 0.1, w: 14, l: 7, h: 3.2, rise: 2.2, pared: mats.adobe, techo: mats.paja, puerta: false });
    casa({ x: ex - 22, z: ez - 14, rot: -0.2, w: 6, l: 5, h: 2.5, rise: 1.5, techo: mats.paja });
    cerco(rect4(ex - 26, ez + 22, 18, 18), { hueco: 0 });                  // corral de doma
    cerco(rect4(ex + 12, ez + 34, 32, 22), { hueco: 0 });                   // corral grande
    palenque(ex - 6, ez + 12, 0.1); pozo(ex + 14, ez + 8); fogon(ex - 14, ez + 10);
    const [p0x, p0z] = K(0.17, 0.08), [p1x, p1z] = K(0.86, 0.42);            // potreros: gran cerco
    cerco([[p0x, p0z], [p1x, p0z], [p1x, p1z], [p0x, p1z]], { hueco: 2, paso: 6, alto: 1.3 });
    cartel('LA SOLEDAD', ex - 4, 2.2, ez + 38, 0, 3.4); }

  // Rancho de peones (inicio del jugador)
  { const [rx, rz] = K(0.59, 0.9);
    habitacion({ x: rx - 8, z: rz, rot: 0.15, w: 7, l: 4.5, amueblar: amueblarRancho });
    casa({ x: rx + 9, z: rz + 4, rot: -0.1, w: 6, l: 4.5, techo: mats.paja });
    fogon(rx, rz + 8); palenque(rx - 2, rz + 14, 0.2, 3); }

  // Rancho abandonado (será el rancho propio, etapa 4)
  { const [ax, az] = K(1.0, 1.0); casa({ x: ax, z: az, rot: 0.3, w: 6, l: 4.5, h: 2.4, rise: 1.4, techo: mats.paja, ruina: true }); }

  // Posta y pulpería, con interior: las paredes son propias (con puerta) y se bajan las que dan a la cámara cuando entrás
  { const [px, pz] = K(2.15, 1.25);
    // Pulpería de la posta: frente recto con frontón alto y cartel pintado, ladrillo a la vista donde cayó el revoque,
    // alero de vigas, puertas dobles oscuras, ventanas con rejas; adentro mostrador de madera, balanza, estantes hasta el techo.
    const cx0 = px, cz0 = pz - 12, W = 22, L = 9, H = 3.6, T = 0.4, iw = W / 2 - T, il = L / 2 - T, PUERTA = 1.8, HD = 2.5;
    const g = new THREE.Group(); g.position.set(cx0, altura(cx0, cz0), cz0); g.userData.sinXray = true; grupo.add(g);
    edificios.push([cx0, cz0, W, L, 0]);
    const col = (c, o = {}) => std({ color: c, ...o });
    const oscuro = col('#3a2a1c'), puertaM = col('#2c2017'), claro = col('#b08a5c'), ladri = col('#9a5638'), ladri2 = col('#7a4630'), humedad = col('#6c685a'), hierroM = col('#26231f', { metalness: 0.3 });
    const add = (m, x, y, z, par = g) => { m.position.set(x, y, z); par.add(m); return m; };
    const caja = (w, h, l, mat, x, y, z, par = g, esc = 1.5) => add(cajaUV(w, h, l, mat, esc), x, y, z, par);
    const fino = (geo, mat, x, y, z, par = g) => add(new THREE.Mesh(geo, mat), x, y, z, par);   // piezas chicas, sin sombra propia
    const obst = (x, z, w, l) => registrar(poly(cx0 + x, cz0 + z, w, l, 0));

    // ---------- paredes (cada una se baja cuando mira a la cámara y estás adentro) ----------
    const paredes = [];
    const pared = (nx, nz, x, z, piezas) => { const grp = new THREE.Group(); grp.position.set(x, 0, z); piezas.forEach(([pw, ph, pl, qx, qy, qz]) => caja(pw, ph, pl, mats.blanco, qx, qy, qz, grp)); g.add(grp); paredes.push({ w: grp, nx, nz, k: 1 }); return grp; };
    const lado = W / 2 - PUERTA / 2;
    const frente = pared(0, 1, 0, L / 2 - T / 2, [[lado, H, T, -(PUERTA / 2 + lado / 2), H / 2, 0], [lado, H, T, PUERTA / 2 + lado / 2, H / 2, 0], [PUERTA, H - HD, T, 0, HD + (H - HD) / 2, 0]]);
    const fondo = pared(0, -1, 0, -(L / 2 - T / 2), [[W, H, T, 0, H / 2, 0]]);
    const izq = pared(-1, 0, -(W / 2 - T / 2), 0, [[T, H, L - 2 * T, 0, H / 2, 0]]);
    const der = pared(1, 0, W / 2 - T / 2, 0, [[T, H, L - 2 * T, 0, H / 2, 0]]);
    obst(0, -(L / 2 - T / 2), W, T); obst(-(W / 2 - T / 2), 0, T, L); obst(W / 2 - T / 2, 0, T, L);
    obst(-(PUERTA / 2 + lado / 2), L / 2 - T / 2, lado, T); obst(PUERTA / 2 + lado / 2, L / 2 - T / 2, lado, T);

    // ladrillo a la vista donde se cayó el revoque, y manchas de humedad al pie
    let sd = 7;
    const parchear = (grp, eje, signo, largo, n, evitarPuerta) => {
      const off = signo * (T / 2 + 0.012);
      for (let i = 0; i < n; i++) {
        const pw = 0.35 + hs(sd++) * 1.3, ph = 0.3 + hs(sd++) * 0.8, u = (hs(sd++) - 0.5) * (largo - pw);
        const y = hs(sd++) < 0.55 ? ph / 2 + hs(sd++) * 1.0 : ph / 2 + hs(sd++) * (H - ph - 0.1);
        if (evitarPuerta && Math.abs(u) < PUERTA / 2 + pw / 2 + 0.15 && y < HD + ph / 2) continue;
        const m = new THREE.Mesh(eje === 'z' ? new THREE.BoxGeometry(pw, ph, 0.03) : new THREE.BoxGeometry(0.03, ph, pw), hs(sd++) < 0.5 ? ladri : ladri2);
        add(m, eje === 'z' ? u : off, y, eje === 'z' ? off : u, grp);
      }
      for (let i = 0; i < 3; i++) {
        const pw = 1.5 + hs(sd++) * 2.2, ph = 0.35 + hs(sd++) * 0.4, u = (hs(sd++) - 0.5) * (largo - pw);
        if (evitarPuerta && Math.abs(u) < PUERTA / 2 + pw / 2) continue;
        const m = new THREE.Mesh(eje === 'z' ? new THREE.BoxGeometry(pw, ph, 0.02) : new THREE.BoxGeometry(0.02, ph, pw), humedad);
        add(m, eje === 'z' ? u : signo * (T / 2 + 0.008), ph / 2, eje === 'z' ? signo * (T / 2 + 0.008) : u, grp);
      }
    };
    parchear(frente, 'z', 1, W, 18, true); parchear(izq, 'x', -1, L, 8, false); parchear(der, 'x', 1, L, 8, false); parchear(fondo, 'z', -1, W, 8, false);

    // marco y puertas dobles oscuras, abiertas hacia afuera
    for (const sx of [-1, 1]) caja(0.14, HD, T + 0.1, puertaM, sx * (PUERTA / 2 + 0.07), HD / 2, 0, frente);
    caja(PUERTA + 0.4, 0.2, T + 0.12, puertaM, 0, HD + 0.05, 0, frente);
    for (const sx of [-1, 1]) {
      const pivote = new THREE.Group(); pivote.position.set(sx * PUERTA / 2, 0, T / 2); pivote.rotation.y = sx * 1.3; frente.add(pivote);
      caja(0.88, HD - 0.05, 0.06, puertaM, -sx * 0.44, (HD - 0.05) / 2, 0, pivote);
      for (const y of [0.75, 1.75]) caja(0.62, y < 1 ? 0.9 : 0.55, 0.02, col('#20170f'), -sx * 0.44, y, 0.04, pivote);
      fino(new THREE.BoxGeometry(0.05, 0.12, 0.05), col('#b08a3a', { metalness: 0.5 }), -sx * 0.1, 1.1, 0.05, pivote);
    }
    // ventanas con rejas y postigos entornados
    for (const vx of [-9, -5.5, 5.5, 9]) {
      caja(1.0, 1.6, 0.12, col('#14100c'), vx, 1.85, T / 2 + 0.02, frente);
      caja(1.2, 0.1, 0.2, claro, vx, 1.02, T / 2 + 0.06, frente); caja(1.2, 0.1, 0.16, puertaM, vx, 2.7, T / 2 + 0.05, frente);
      for (let i = 0; i < 5; i++) fino(new THREE.BoxGeometry(0.03, 1.6, 0.03), col('#1a1a1a'), vx - 0.4 + i * 0.2, 1.85, T / 2 + 0.1, frente);
      for (const sx of [-1, 1]) { const p = new THREE.Group(); p.position.set(vx + sx * 0.52, 1.85, T / 2 + 0.03); p.rotation.y = -sx * 1.15; frente.add(p); caja(0.5, 1.6, 0.04, col('#3b2a1c'), sx * 0.25, 0, 0, p); }
    }
    for (const [y, c] of [[1.55, '#8a8a82'], [1.9, '#262626']]) fino(new THREE.BoxGeometry(0.34, 0.24, 0.02), col(c), -1.7, y, T / 2 + 0.03, frente);    // placas junto a la puerta

    // ---------- techo plano con frontón alto y cartel pintado ----------
    const techoG = new THREE.Group(); g.add(techoG);
    caja(W + 0.5, 0.3, L + 0.5, col('#6b5f55'), 0, H + 0.1, 0, techoG);
    caja(W + 0.5, 0.55, 0.28, mats.blanco, 0, H + 0.45, -(L / 2 + 0.11), techoG);
    for (const sx of [-1, 1]) caja(0.28, 0.55, L + 0.5, mats.blanco, sx * (W / 2 + 0.11), H + 0.45, 0, techoG);
    caja(W + 0.5, 2.1, 0.34, mats.blanco, 0, H + 1.1, L / 2 + 0.1, techoG);
    caja(W + 0.8, 0.22, 0.66, mats.blanco, 0, H + 0.08, L / 2 + 0.2, techoG);
    caja(W + 0.7, 0.2, 0.56, mats.blanco, 0, H + 2.2, L / 2 + 0.15, techoG);
    for (let i = 0; i < 9; i++) {                                                               // ladrillo a la vista en el frontón
      const pw = 0.4 + hs(sd++) * 1.3, ph = 0.3 + hs(sd++) * 0.5;
      fino(new THREE.BoxGeometry(pw, ph, 0.03), hs(sd++) < 0.5 ? ladri : ladri2, (hs(sd++) - 0.5) * (W - pw), H + 0.5 + hs(sd++) * 1.2, L / 2 + 0.28, techoG);
    }
    {
      const cv = document.createElement('canvas'); cv.width = 1024; cv.height = 180; const k = cv.getContext('2d');
      k.fillStyle = '#1f2a4a'; k.font = 'bold 150px Georgia, serif'; k.textAlign = 'center'; k.textBaseline = 'middle'; if ('letterSpacing' in k) k.letterSpacing = '14px';
      k.fillText('PULPERIA', 512, 98);
      k.globalCompositeOperation = 'destination-out';                                          // pintura descascarada
      for (let i = 0; i < 260; i++) { k.globalAlpha = 0.25 + Math.random() * 0.6; k.fillRect(Math.random() * 1024, Math.random() * 180, 3 + Math.random() * 16, 2 + Math.random() * 9); }
      const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace;
      const letrero = new THREE.Mesh(new THREE.PlaneGeometry(11, 11 * 180 / 1024), new THREE.MeshStandardMaterial({ map: t, transparent: true, alphaTest: 0.3, roughness: 1 }));
      letrero.position.set(0, H + 1.1, L / 2 + 0.31); techoG.add(letrero);
    }
    fino(new THREE.CylinderGeometry(0.02, 0.02, 0.5, 4).rotateZ(Math.PI / 2), hierroM, 0, H + 0.3, L / 2 + 0.55, techoG);   // pescante del farol

    // ---------- alero de vigas sobre la vereda de ladrillo ----------
    const aleroG = new THREE.Group(); g.add(aleroG);
    const zA = L / 2 + 2.6;
    caja(W + 1.0, 0.06, 3.2, col('#8d6b58'), 0, 0.01, L / 2 + 1.7);
    const nP = 7;
    for (let i = 0; i < nP; i++) { const x = -W / 2 + 0.4 + i * (W - 0.8) / (nP - 1); if (Math.abs(x) < 1.2) continue; caja(0.14, 3.05, 0.14, oscuro, x, 1.52, zA, aleroG); }
    caja(W + 0.4, 0.18, 0.2, oscuro, 0, 3.1, zA, aleroG);
    for (let x = -W / 2 + 0.3; x < W / 2; x += 0.9) { const v = caja(0.1, 0.12, 2.75, oscuro, x, 3.27, L / 2 + 1.3, aleroG); v.rotation.x = 0.134; }
    { const c = cajaUV(W + 0.6, 0.07, 2.9, mats.teja, 2); add(c, 0, 3.4, L / 2 + 1.35, aleroG); c.rotation.x = 0.134; }
    for (const [txt, bx] of [['YERBA · CAÑA', -5.3], ['TABACO', 5.3]]) {                      // tablas pintadas a mano colgadas de la viga
      const b = cartel(txt, 0, 0, 0, 0, 2.4); grupo.remove(b); b.position.set(bx, 2.45, zA + 0.12); aleroG.add(b);
    }
    // ---------- interior ----------
    caja(W - 2 * T, 0.08, L - 2 * T, col('#8f6e49'), 0, 0, 0, g, 1.4);
    for (let i = -4; i <= 4; i++) caja(0.04, 0.02, L - 2 * T, oscuro, i * 2.4, 0.045, 0);
    const CZ = -0.9;                                                                          // mostrador de madera con paneles, y reja arriba
    caja(12, 1.05, 0.9, oscuro, 0, 0.525, CZ); caja(12.2, 0.07, 1.1, claro, 0, 1.08, CZ); obst(0, CZ, 12.2, 1.1);
    for (let i = 0; i < 8; i++) { caja(1.25, 0.75, 0.03, col('#241911'), -5.25 + i * 1.5, 0.55, CZ + 0.46); caja(1.05, 0.55, 0.03, col('#34261a'), -5.25 + i * 1.5, 0.55, CZ + 0.48); }
    for (let i = 0; i <= 33; i++) { const x = -5.8 + i * (11.6 / 33); if (Math.abs(x) < 1.0) continue; fino(new THREE.BoxGeometry(0.04, 1.1, 0.04), oscuro, x, 1.66, CZ); }
    caja(4.8, 0.06, 0.08, oscuro, -3.45, 2.22, CZ); caja(4.8, 0.06, 0.08, oscuro, 3.45, 2.22, CZ);
    // balanza de hierro colorado
    { const bx = -4.2, by = 1.12;
      fino(new THREE.BoxGeometry(0.55, 0.1, 0.38), col('#6e2518'), bx, by + 0.05, CZ);
      fino(new THREE.CylinderGeometry(0.12, 0.17, 0.5, 8), col('#8a2e1c'), bx, by + 0.35, CZ);
      fino(new THREE.CylinderGeometry(0.2, 0.2, 0.07, 14).rotateX(Math.PI / 2), col('#efe6d0'), bx, by + 0.62, CZ + 0.15);
      fino(new THREE.BoxGeometry(0.02, 0.15, 0.02), col('#222'), bx, by + 0.65, CZ + 0.2);
      fino(new THREE.BoxGeometry(0.95, 0.03, 0.03), hierroM, bx, by + 0.78, CZ);
      for (const sx of [-1, 1]) { fino(new THREE.CylinderGeometry(0.01, 0.01, 0.28, 4), hierroM, bx + sx * 0.45, by + 0.64, CZ); fino(new THREE.CylinderGeometry(0.17, 0.15, 0.025, 12), col('#b8a070', { metalness: 0.4 }), bx + sx * 0.45, by + 0.5, CZ); } }
    // vitrina de vidrio con mercadería
    { const vx = 3.5, vy = 1.12; fino(new THREE.BoxGeometry(1.8, 0.5, 0.7), new THREE.MeshStandardMaterial({ color: '#cfe6e8', transparent: true, opacity: 0.22, roughness: 0.1, depthWrite: false }), vx, vy + 0.25, CZ);
      for (const dx of [-0.9, 0.9]) for (const dz of [-0.35, 0.35]) fino(new THREE.BoxGeometry(0.04, 0.5, 0.04), oscuro, vx + dx, vy + 0.25, CZ + dz);
      caja(1.84, 0.04, 0.74, oscuro, vx, vy + 0.52, CZ);
      for (let i = 0; i < 6; i++) fino(new THREE.BoxGeometry(0.22, 0.16, 0.2), col(['#d9c48a', '#a8483a', '#486a8a', '#efe6d0'][i % 4]), vx - 0.7 + i * 0.28, vy + 0.1, CZ + (i % 2 ? 0.12 : -0.12)); }
    // estantes del fondo hasta el techo, con fondo de madera oscura y mercadería a rebosar
    const fondoZ = -il + 0.28;
    caja(16.4, 3.4, 0.06, col('#2a1e14'), 0, 1.7, -il + 0.03);
    const NIV = [0.45, 1.0, 1.55, 2.1, 2.65, 3.2];
    for (const y of NIV) caja(16, 0.07, 0.5, claro, 0, y, fondoZ);
    for (const sx of [-8, -4, 0, 4, 8]) caja(0.08, 3.3, 0.5, claro, sx, 1.65, fondoZ);
    let q = 0;
    for (const y of NIV) for (let x = -7.8; x < 7.8; x += 0.3 + hs(q + 3) * 0.3) {
      q++; const t = hs(q * 7.3), izqZona = x < -2.2;
      if (izqZona && t < 0.6) fino(new THREE.CylinderGeometry(0.12, 0.12, 0.42, 7).rotateZ(Math.PI / 2), col(['#a8483a', '#486a8a', '#d9d0b8', '#6a8a52', '#c9a84a'][q % 5]), x, y + 0.2, fondoZ);       // rollos de tela
      else if (t < 0.35) fino(new THREE.CylinderGeometry(0.07, 0.09, 0.4, 6), col(['#2f6b3a', '#7a4a22', '#cfe0d4'][q % 3]), x, y + 0.24, fondoZ);                                                         // botellas
      else if (t < 0.65) fino(new THREE.CylinderGeometry(0.1, 0.1, 0.18, 7), col(['#b8b8b0', '#8a8a82', '#a8483a'][q % 3]), x, y + 0.13, fondoZ);                                                         // latas
      else if (t < 0.9) fino(new THREE.BoxGeometry(0.34, 0.28, 0.26), col(['#d9c48a', '#c2a46a', '#e8e0cc'][q % 3]), x, y + 0.18, fondoZ);                                                                  // cajas y paquetes
    }
    // barriles, bolsas y cajones
    for (const [bx, bz, r, h] of [[-9.2, -3.1, 0.45, 1.0], [-8.2, -3.5, 0.4, 0.9], [9.2, -3.2, 0.45, 1.0], [8.2, -3.5, 0.4, 0.9], [9.3, -1.7, 0.4, 0.9]]) {
      const bar = new THREE.Mesh(new THREE.CylinderGeometry(r, r * 0.9, h, 9), mats.madera); add(bar, bx, h / 2, bz); bar.castShadow = true;
      for (const hy of [0.25, 0.75]) fino(new THREE.CylinderGeometry(r * 1.02, r * 1.02, 0.05, 9), mats.hierro, bx, h * hy, bz);
      obst(bx, bz, r * 2, r * 2);
    }
    for (const [bx, bz, e] of [[-9.0, -0.2, 1], [-8.0, 0.4, 0.9], [-9.1, 1.0, 0.95], [-7.6, -0.9, 0.8]]) { const sa = fino(new THREE.IcosahedronGeometry(1, 0), col('#cdb98c'), bx, 0.3 * e, bz); sa.scale.set(0.55 * e, 0.42 * e, 0.4 * e); sa.castShadow = true; obst(bx, bz, 0.9, 0.7); }
    caja(1.1, 0.7, 0.8, claro, -6.6, 0.35, -3.5); caja(0.9, 0.55, 0.7, oscuro, -6.5, 0.98, -3.5); obst(-6.6, -3.5, 1.2, 0.9);
    // mesas con sillas
    const silla = (sx, sz, rot, par = g, y0 = 0) => {
      const s = new THREE.Group(); s.position.set(sx, y0, sz); s.rotation.y = rot; par.add(s);
      caja(0.42, 0.05, 0.42, claro, 0, 0.46, 0, s); caja(0.42, 0.5, 0.04, oscuro, 0, 0.75, -0.2, s);
      for (const dx of [-0.19, 0.19]) for (const dz of [-0.19, 0.19]) caja(0.04, 0.46, 0.04, oscuro, dx, 0.23, dz, s);
      return s;
    };
    for (const [mx, mz] of [[7.3, 1.1], [7.3, 3.0]]) {
      caja(1.6, 0.07, 1.0, claro, mx, 0.8, mz); for (const sx of [-0.7, 0.7]) for (const sz of [-0.38, 0.38]) caja(0.07, 0.8, 0.07, oscuro, mx + sx, 0.4, mz + sz);
      obst(mx, mz, 1.6, 1.0);
      silla(mx - 1.05, mz, Math.PI / 2); silla(mx + 1.05, mz, -Math.PI / 2);
      fino(new THREE.CylinderGeometry(0.07, 0.06, 0.16, 7), col('#8a4a2a'), mx - 0.4, 0.92, mz);
    }
    // sillas colgadas de la pared izquierda, como en las pulperías de la época, y un reloj en la derecha
    for (let i = 0; i < 4; i++) silla(-iw + 0.22, -2.4 + i * 1.6, Math.PI / 2, g, 1.5);
    { const rl = fino(new THREE.CylinderGeometry(0.26, 0.26, 0.06, 16).rotateZ(Math.PI / 2), col('#efe6d0'), iw - 0.05, 2.5, 0.2); fino(new THREE.CylinderGeometry(0.29, 0.29, 0.04, 16).rotateZ(Math.PI / 2), oscuro, iw - 0.02, 2.5, 0.2);
      fino(new THREE.BoxGeometry(0.02, 0.2, 0.025), col('#111'), iw - 0.1, 2.56, 0.2); fino(new THREE.BoxGeometry(0.02, 0.025, 0.15), col('#111'), iw - 0.1, 2.5, 0.27); }
    // percha de apero junto a la puerta
    caja(0.1, 1.5, 0.1, oscuro, -5.2, 0.75, 3.6); caja(0.1, 1.5, 0.1, oscuro, -3.6, 0.75, 3.6); caja(1.7, 0.1, 0.1, oscuro, -4.4, 1.5, 3.6);
    { const ap = fino(new THREE.CylinderGeometry(0.28, 0.28, 0.9, 7).rotateZ(Math.PI / 2), col('#6a3a22'), -4.4, 1.7, 3.6); fino(new THREE.BoxGeometry(0.9, 0.12, 0.6), col('#a8553a'), -4.4, 1.5, 3.6); }
    obst(-4.4, 3.6, 1.8, 0.3);
    // faroles de pared con brillo
    for (const lx of [-6, 0, 6]) { fino(new THREE.BoxGeometry(0.06, 0.06, 0.3), hierroM, lx, 2.95, -il + 0.5); const f = fino(new THREE.CylinderGeometry(0.1, 0.12, 0.26, 6), std({ color: '#ffd28a', emissive: '#ff9a2a', emissiveIntensity: 1.2 }), lx, 2.8, -il + 0.62); f.userData.noXray = true; }

    g.userData.techo = techoG; g.userData.alero = aleroG;
    interiores.push({
      actualizar(dt, p, yaw) {
        const dentro = Math.abs(p.x - cx0) < W / 2 - T / 2 && p.z > cz0 - L / 2 && p.z < cz0 + L / 2 + 0.25;
        const cam = [Math.cos(yaw), Math.sin(yaw)];
        for (const w of paredes) { const objetivo = dentro && (w.nx * cam[0] + w.nz * cam[1]) > 0.25 ? 0.1 : 1; w.k += (objetivo - w.k) * Math.min(1, dt * 7); w.w.scale.y = w.k; }
        techoG.visible = !dentro; aleroG.visible = !dentro;
        this.dentro = dentro;
      },
    });
    cerco(rect4(px - 6, pz - 40, 26, 16), { hueco: 2 });                    // corral de remuda
    casa({ x: px + 34, z: pz - 20, rot: 0.2, w: 7, l: 5, techo: mats.paja });
    casa({ x: px + 40, z: pz + 6, rot: -0.15, w: 6, l: 4.5, techo: mats.paja });
    casa({ x: px - 36, z: pz + 12, rot: 0.1, w: 7, l: 5, techo: mats.paja });
    palenque(px - 4, pz + 6, 0.05, 5); pozo(px + 22, pz - 2); fogon(px + 14, pz + 8); }

  // Fortín de la frontera
  { const [fx, fz] = K(0.12, 1.15);
    cerco(rect4(fx, fz, 30, 30), { hueco: 2, alto: 3, paso: 1.6 });
    casa({ x: fx - 7, z: fz - 6, rot: 0, w: 8, l: 5, techo: mats.paja }); casa({ x: fx + 7, z: fz + 5, rot: 0.1, w: 7, l: 5, techo: mats.paja });
    const m = new THREE.Mesh(CIL(0.12, 0.2, 9, 6), mats.madera); m.position.set(fx + 4, altura(fx, fz) + 4.5, fz - 8); grupo.add(m);
    for (const [i, c] of ['#79b7e6', '#ffffff', '#79b7e6'].entries()) { const b = new THREE.Mesh(new THREE.BoxGeometry(2.4, 0.5, 0.05), std({ color: c })); b.position.set(fx + 5.3, altura(fx, fz) + 8.5 - i * 0.5, fz - 8); grupo.add(b); }
    for (const [sx, sz] of [[-1, -1], [1, -1], [-1, 1], [1, 1]]) { const p = new THREE.Mesh(CIL(0.15, 0.2, 6, 5), mats.madera); p.position.set(fx - 12 + (sx + 1) * 1.4 + 3, altura(fx, fz) + 3, fz + 12 + (sz) * 1.4); }
    registrar(poly(fx + 4, fz - 8, 1, 1, 0)); fogon(fx, fz + 1); }

  // Puesto del baqueano (sobre la loma)
  { const [bx, bz] = K(0.55, 1.85);
    casa({ x: bx, z: bz, rot: 0.4, w: 6, l: 4.5, techo: mats.paja });
    cerco(rect4(bx + 14, bz + 4, 12, 9), { hueco: 0 }); fogon(bx - 8, bz + 6); }

  // Rancho de Casiana (curandera), junto al arroyo
  { const [cx, cz] = K(1.17, 0.5); casa({ x: cx, z: cz, rot: 0.5, w: 6, l: 4.5, techo: mats.paja }); fogon(cx - 6, cz + 5); }

  // dibujar cercos
  { const geoP = CIL(0.09, 0.11, 1, 5).translate(0, 0.5, 0);
    const im = new THREE.InstancedMesh(geoP, mats.madera, Math.max(1, postes.length)); im.count = postes.length;
    const m = new THREE.Matrix4(), q = new THREE.Quaternion(), s = new THREE.Vector3(), p = new THREE.Vector3();
    postes.forEach((t, i) => { p.set(t.x, altura(t.x, t.z), t.z); s.set(1, t.alto, 1); m.compose(p, q.identity(), s); im.setMatrixAt(i, m); });
    im.castShadow = true; im.frustumCulled = false; grupo.add(im);
    const geoR = new THREE.BoxGeometry(0.07, 0.08, 1);
    const ir = new THREE.InstancedMesh(geoR, mats.madera, Math.max(1, rieles.length)); ir.count = rieles.length;
    rieles.forEach((t, i) => { p.set(t.x, altura(t.x, t.z) + t.y, t.z); q.setFromAxisAngle(Y, t.ang); s.set(1, 1, t.L); m.compose(p, q, s); ir.setMatrixAt(i, m); });
    ir.castShadow = true; ir.frustumCulled = false; grupo.add(ir); }

  // ---------- pasto alrededor del jugador (se regenera al caminar) ----------
  // Pampa de 1867: matas altas y duras de paja que se mecen con el viento y cubren todo ("mar de pasto"); en el pajonal llegan a dos metros.
  // Cada mata es un manojo de once hojas; las que quedan junto al jugador se aplastan para que no lo tape.
  const geoPasto = (() => {
    const P = [], n = 11;
    for (let i = 0; i < n; i++) {
      const ang = i * 2.399, r = 0.05 + 0.32 * Math.sqrt((i + 0.5) / n), h = 0.75 + 0.55 * hs(i * 3.7 + 1), lean = 0.12 + 0.25 * hs(i * 5.1 + 2);
      P.push(pieza(CONO(0.075, h, 3), '#ffffff', [Math.cos(ang) * r, h / 2, Math.sin(ang) * r], [1, 1, 1], [Math.sin(ang) * lean, hs(i) * 3, -Math.cos(ang) * lean]));
    }
    const g = ensamblar(P), pos = g.attributes.position, cl = g.attributes.color;
    for (let i = 0; i < pos.count; i++) { const t = Math.min(1, Math.max(0, pos.getY(i) / 1.2)), v = 0.45 + 0.65 * t; cl.setXYZ(i, v, v, v * 0.95); }   // base oscura, punta clara
    return g;
  })();
  const uTP = { value: 0 }, uJP = { value: new THREE.Vector3(1e9, 0, 1e9) };
  const matPasto = new THREE.MeshStandardMaterial({ vertexColors: true, flatShading: true, roughness: 1, side: THREE.DoubleSide });
  matPasto.onBeforeCompile = (sh) => {
    sh.uniforms.uTP = uTP; sh.uniforms.uJP = uJP;
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nuniform float uTP; uniform vec3 uJP;').replace('#include <begin_vertex>', `#include <begin_vertex>
      { vec3 ip = vec3(instanceMatrix[3].x, 0.0, instanceMatrix[3].z);
        float hh = transformed.y;
        float w = sin(uTP * 1.7 + ip.x * 0.21 + ip.z * 0.17) * 0.5 + sin(uTP * 2.9 + ip.x * 0.5 - ip.z * 0.4) * 0.25;
        transformed.x += w * 0.09 * hh * hh; transformed.z += w * 0.05 * hh * hh;
        float dj = distance(ip.xz, uJP.xz);
        transformed.y *= mix(0.3, 1.0, smoothstep(0.8, 2.8, dj));
      }`);
  };
  const CAP = 26000, pasto = new THREE.InstancedMesh(geoPasto, matPasto, CAP);
  pasto.castShadow = false; pasto.frustumCulled = false; pasto.userData.noXray = true; scene.add(pasto);
  let ultimo = [1e9, 1e9];
  const regenerarPasto = (px, pz) => {
    const CELDA = 6, R = 78, m = new THREE.Matrix4(), q = new THREE.Quaternion(), s = new THREE.Vector3(), p = new THREE.Vector3(), c = new THREE.Color();
    let n = 0;
    for (let gx = Math.floor((px - R) / CELDA); gx <= Math.floor((px + R) / CELDA) && n < CAP; gx++)
      for (let gz = Math.floor((pz - R) / CELDA); gz <= Math.floor((pz + R) / CELDA) && n < CAP; gz++) {
        const ccx = (gx + 0.5) * CELDA, ccz = (gz + 0.5) * CELDA, d = Math.hypot(ccx - px, ccz - pz);
        if (d > R) continue;
        const kx = (ccx + 1600) / 1000, ky = (ccz + 1200) / 1000;
        const enPajonal = kx < 1.55 && ky > 1.5;
        const parche = mix(0.35, 1.5, ruido(ccx * 0.02, ccz * 0.02)) * mix(0.7, 1.3, ruido(ccx * 0.09 + 5, ccz * 0.09));      // manchones más ralos y más tupidos
        const dens = (enPajonal ? 0.75 : 0.3) * parche * (d > R * 0.7 ? 1 - (d - R * 0.7) / (R * 0.3) : 1);
        const k = Math.round(dens * CELDA * CELDA);
        for (let i = 0; i < k && n < CAP; i++) {
          const seed = gx * 7919 + gz * 104729 + i * 13;
          const x = (gx + hs(seed)) * CELDA, z = (gz + hs(seed + 1)) * CELDA;
          if (!puedeCrecer(x, z) || !libreDeSitios(x, z, 0.8)) continue;
          const alto = (enPajonal ? 1.45 : 0.8) * mix(0.75, 1.45, hs(seed + 2)), ancho = mix(0.85, 1.45, hs(seed + 7));
          q.setFromAxisAngle(Y, hs(seed + 3) * 6.28); s.set(ancho, alto, ancho); p.set(x, altura(x, z), z); m.compose(p, q, s);
          pasto.setMatrixAt(n, m);
          c.setHSL(mix(0.11, 0.24, hs(seed + 4) * (enPajonal ? 0.7 : 1)), mix(0.3, 0.6, hs(seed + 5)), mix(0.34, 0.54, hs(seed + 6))); pasto.setColorAt(n, c); n++;
        }
      }
    pasto.count = n; pasto.instanceMatrix.needsUpdate = true; if (pasto.instanceColor) pasto.instanceColor.needsUpdate = true;
    ultimo = [px, pz];
  };

  const inicio = K(0.59, 0.9); inicio[0] += 3; inicio[1] += 12;
  return {
    inicio, altura, enAgua, enCamino, K, edificios, cercos,
    interiores,
    actualizar(dt, p, frame, yaw = 0) { uTP.value += dt; uJP.value.set(p.x, 0, p.z); if (Math.hypot(p.x - ultimo[0], p.z - ultimo[1]) > 14) regenerarPasto(p.x, p.z); for (const i of interiores) i.actualizar(dt, p, yaw); },
    iniciar() { regenerarPasto(inicio[0], inicio[1]); },
  };
}
