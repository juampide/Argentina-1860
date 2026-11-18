import * as THREE from 'three';
import { registrar, rect, ocupado, registrarAltura } from './colision.js';

const hash = (i) => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
const lerp2 = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t];
const dist2 = (a, b) => Math.hypot(b[0] - a[0], b[1] - a[1]);

// ---------- Texturas ----------
const cargador = new THREE.TextureLoader();
const cargar = (url, srgb) => cargador.loadAsync(url).then((t) => {
  t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8;
  if (srgb) t.colorSpace = THREE.SRGBColorSpace; return t;
});
function lienzo(w, h, dibujar) {
  const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
  dibujar(cv.getContext('2d'), w, h);
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8;
  t.wrapS = t.wrapT = THREE.RepeatWrapping; return t;
}

let _texturas = null;
export const cargarTexturas = () => (_texturas ??= cargarTexturasReal());
async function cargarTexturasReal() {
  const [tejaC, tejaN, revC, revN, tieC, tieN, pasto, patio, calleC, calleN, vereC, vereN, ladC, ladN, sillC, sillN, traC, traN] = await Promise.all([
    cargar('./tex/teja_c.jpg', true), cargar('./tex/teja_n.jpg'), cargar('./tex/revoque_c.jpg', true), cargar('./tex/revoque_n.jpg'),
    cargar('./tex/tierra_c.jpg', true), cargar('./tex/tierra_n.jpg'), cargar('./tex/pasto_c.jpg', true), cargar('./tex/patio_c.jpg', true),
    cargar('./tex/calle_c.jpg', true), cargar('./tex/calle_n.jpg'), cargar('./tex/vereda_c.jpg', true), cargar('./tex/vereda_n.jpg'),
    cargar('./tex/ladrillo_c.jpg', true), cargar('./tex/ladrillo_n.jpg'), cargar('./tex/sillar_c.jpg', true), cargar('./tex/sillar_n.jpg'),
    cargar('./tex/travertino_c.jpg', true), cargar('./tex/travertino_n.jpg')]);
  return { tejaC, tejaN, revC, revN, tieC, tieN, pasto, patio, calleC, calleN, vereC, vereN, ladC, ladN, sillC, sillN, traC, traN };
}

// Ventanas con postigos, puerta y balcón: atlas de 4 celdas (cada una 256x512)
function atlasVanos() {
  return lienzo(1024, 512, (c) => {
    const celda = (n) => n * 256;
    // 0: ventana con reja y postigos abiertos
    const ventana = (x0, balcon) => {
      c.fillStyle = '#efe6d0'; c.fillRect(x0 + 30, 70, 196, 420);                   // marco claro
      c.fillStyle = '#1f2a2b'; c.fillRect(x0 + 56, 96, 144, 368);                    // vidrio
      c.strokeStyle = '#6b7a78'; c.lineWidth = 3; c.beginPath();
      c.moveTo(x0 + 128, 96); c.lineTo(x0 + 128, 464); c.moveTo(x0 + 56, 230); c.lineTo(x0 + 200, 230); c.stroke();
      for (const sx of [x0 + 56, x0 + 150]) {                                         // postigos verdes
        c.fillStyle = '#3f6049'; c.fillRect(sx, 96, 50, 368);
        c.strokeStyle = '#2a4233'; c.lineWidth = 2;
        for (let y = 104; y < 460; y += 12) { c.beginPath(); c.moveTo(sx + 4, y); c.lineTo(sx + 46, y); c.stroke(); }
      }
      c.fillStyle = '#d9cdb2'; c.fillRect(x0 + 18, 56, 220, 18);                      // cornisa
      c.fillRect(x0 + 22, 490, 212, 14);                                             // alféizar
      c.strokeStyle = '#22201d'; c.lineWidth = 3;
      if (balcon) {                                                                   // baranda de hierro
        c.fillStyle = '#2b2824'; c.fillRect(x0 + 0, 400, 256, 12);
        for (let x = 6; x < 256; x += 14) { c.beginPath(); c.moveTo(x0 + x, 340); c.lineTo(x0 + x, 400); c.stroke(); }
        c.beginPath(); c.moveTo(x0, 340); c.lineTo(x0 + 256, 340); c.stroke();
      } else {                                                                        // reja
        for (let x = 66; x < 200; x += 18) { c.beginPath(); c.moveTo(x0 + x, 240); c.lineTo(x0 + x, 490); c.stroke(); }
        c.beginPath(); c.moveTo(x0 + 56, 330); c.lineTo(x0 + 200, 330); c.stroke();
      }
    };
    ventana(celda(0), false);
    // 1: puerta de madera
    const x1 = celda(1);
    c.fillStyle = '#e9dfc7'; c.fillRect(x1 + 20, 30, 216, 482);
    c.fillStyle = '#4a3220'; c.fillRect(x1 + 40, 60, 176, 452);
    c.strokeStyle = '#2d1e12'; c.lineWidth = 3;
    for (const [px, py, pw, ph] of [[54, 76, 70, 170], [132, 76, 70, 170], [54, 266, 70, 220], [132, 266, 70, 220]]) c.strokeRect(x1 + px, py, pw, ph);
    c.fillStyle = '#b08a3e'; c.beginPath(); c.arc(x1 + 118, 300, 6, 0, 7); c.fill();
    c.fillStyle = '#d6cbb0'; c.fillRect(x1 + 10, 20, 236, 16);
    // 2: ventana de piso alto con balcón
    ventana(celda(2), true);
  });
}

// Arco de recova (una crujía). pisos = 1 o 2
function texArcada(pisos) {
  const W = 288, Hp = 400;
  return lienzo(W, Hp * pisos, (c, w, h) => {
    c.fillStyle = '#eadfc6'; c.fillRect(0, 0, w, h);
    c.fillStyle = '#d4c7a8'; c.fillRect(0, 0, w, 14);
    for (let p = 0; p < pisos; p++) {
      const y0 = (pisos - 1 - p) * Hp;                       // el piso 0 es el de abajo
      c.save(); c.globalCompositeOperation = 'destination-out';
      c.beginPath(); c.moveTo(48, y0 + Hp - (p === 0 ? 0 : 30)); c.lineTo(48, y0 + 150); c.arc(144, y0 + 150, 96, Math.PI, 0);
      c.lineTo(240, y0 + Hp - (p === 0 ? 0 : 30)); c.closePath(); c.fill(); c.restore();
      c.fillStyle = '#cdbf9f'; c.fillRect(0, y0 + 20, w, 8);  // imposta
      if (p > 0) {                                           // balaustrada en galería alta
        c.fillStyle = '#e3d8bd'; c.fillRect(46, y0 + Hp - 52, 196, 10);
        for (let x = 52; x < 240; x += 18) c.fillRect(x, y0 + Hp - 44, 8, 30);
        c.fillRect(46, y0 + Hp - 16, 196, 16);
      }
    }
  });
}

// ---------- Buffers ----------
class Buf {
  constructor() { this.p = []; this.u = []; this.c = []; }
  tri(a, b, c, ua, ub, uc, col) {
    this.p.push(a[0], a[1], a[2], b[0], b[1], b[2], c[0], c[1], c[2]);
    this.u.push(ua[0], ua[1], ub[0], ub[1], uc[0], uc[1]);
    for (let i = 0; i < 3; i++) this.c.push(col.r, col.g, col.b);
  }
  quad(a, b, c, d, ua, ub, uc, ud, col) { this.tri(a, b, c, ua, ub, uc, col); this.tri(a, c, d, ua, uc, ud, col); }
  malla(mat, sombra = true) {
    if (!this.p.length) return null;
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(this.p, 3));
    g.setAttribute('uv', new THREE.Float32BufferAttribute(this.u, 2));
    g.setAttribute('color', new THREE.Float32BufferAttribute(this.c, 3));
    g.computeVertexNormals(); g.computeBoundingSphere();
    const m = new THREE.Mesh(g, mat); m.castShadow = sombra; m.receiveShadow = true; return m;
  }
}

// Paleta del pueblo: paredes pastel encaladas y techos de teja en tonos cálidos (colores planos, estilo low poly)
const PAREDES = ['#f4ead2', '#f1dcae', '#ebbfa0', '#c4d8d8', '#f2d28f', '#dcc7a4', '#f6dfce', '#ccd9bb', '#e9c9c2', '#d6e0ea'].map(h => new THREE.Color(h));
const TEJAS = ['#ffffff', '#f6dccf', '#e8c4b0', '#f9e9de', '#ffe7d6'].map(h => new THREE.Color(h).multiplyScalar(1.5));   // tintes sobre la textura de teja
const BLANCO = new THREE.Color('#ffffff');
const ZOCALO = new THREE.Color('#9a8566');

export async function construirCiudad(ctx) {
  const { scene, manzanas, iglesias, aMundo, ESC } = ctx;
  const T = await cargarTexturas();
  const atlas = atlasVanos();
  const arco1 = texArcada(1), arco2 = texArcada(2);

  const mats = {
    pared: new THREE.MeshStandardMaterial({ map: T.revC, normalMap: T.revN, normalScale: new THREE.Vector2(0.9, 0.9), vertexColors: true, flatShading: true, roughness: 0.95, side: THREE.DoubleSide }),
    techo: new THREE.MeshStandardMaterial({ map: T.tejaC, normalMap: T.tejaN, normalScale: new THREE.Vector2(1.2, 1.2), vertexColors: true, flatShading: true, roughness: 0.85, side: THREE.DoubleSide }),
    vano: new THREE.MeshStandardMaterial({ map: atlas, alphaTest: 0.5, roughness: 0.8, side: THREE.DoubleSide, polygonOffset: true, polygonOffsetFactor: -3, polygonOffsetUnits: -3 }),
    patio: new THREE.MeshStandardMaterial({ map: T.patio, roughness: 1, vertexColors: true, flatShading: true, side: THREE.DoubleSide }),
    vereda: new THREE.MeshStandardMaterial({ map: T.vereC, normalMap: T.vereN, normalScale: new THREE.Vector2(1, 1), vertexColors: true, flatShading: true, roughness: 0.95, side: THREE.DoubleSide }),
    calle: new THREE.MeshStandardMaterial({ map: T.calleC, normalMap: T.calleN, normalScale: new THREE.Vector2(1.2, 1.2), vertexColors: true, roughness: 1, side: THREE.DoubleSide, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -1 }),
    ladrillo: new THREE.MeshStandardMaterial({ map: T.ladC, normalMap: T.ladN, normalScale: new THREE.Vector2(1.3, 1.3), vertexColors: true, flatShading: true, roughness: 0.95, side: THREE.DoubleSide }),
    sillar: new THREE.MeshStandardMaterial({ map: T.sillC, normalMap: T.sillN, normalScale: new THREE.Vector2(1.3, 1.3), vertexColors: true, flatShading: true, roughness: 0.95, side: THREE.DoubleSide }),
    arco1: new THREE.MeshStandardMaterial({ map: arco1, alphaTest: 0.5, roughness: 0.9, side: THREE.DoubleSide }),
    arco2: new THREE.MeshStandardMaterial({ map: arco2, alphaTest: 0.5, roughness: 0.9, side: THREE.DoubleSide }),
    piedra: new THREE.MeshStandardMaterial({ color: '#efe3c8', roughness: 1, flatShading: true }),
    plomo: new THREE.MeshStandardMaterial({ color: '#7f9099', roughness: 0.8, flatShading: true }),
    oscuro: new THREE.MeshStandardMaterial({ color: '#3a302a', roughness: 1, side: THREE.DoubleSide, flatShading: true }),
  };

  // fragmentos para poder descartar lo que no se ve
  const trozos = new Map();
  const trozo = (x, z) => {
    const k = Math.floor(x / 350) * 1000 + Math.floor(z / 350);
    if (!trozos.has(k)) trozos.set(k, { pared: new Buf(), techo: new Buf(), vano: new Buf(), patio: new Buf(), vereda: new Buf(), calle: new Buf(), ladrillo: new Buf(), sillar: new Buf() });
    return trozos.get(k);
  };

  const W3 = (a, y) => [a[0], y, a[1]];

  // pared vertical entre p y q (puntos [x,z]) de y0 a y1
  const pared = (B, p, q, y0, y1, col, off = 0, esc = 3.2) => {
    const L = dist2(p, q) / esc;
    B.quad(W3(p, y0), W3(q, y0), W3(q, y1), W3(p, y1), [off, y0 / esc], [off + L, y0 / esc], [off + L, y1 / esc], [off, y1 / esc], col);
  };
  const planoTecho = (B, A, Bp, C, D, col) => {      // A-B alero, D-C cumbrera (puntos 3D)
    const lab = Math.hypot(Bp[0] - A[0], Bp[2] - A[2]) / 1.5;
    const s = Math.hypot(D[0] - A[0], D[1] - A[1], D[2] - A[2]) / 1.5;
    B.quad(A, Bp, C, D, [0, 0], [lab, 0], [lab, s], [0, s], col);
  };
  const triPared = (B, a, b, c, col) => B.tri(a, b, c, [0, a[1] / 3.2], [dist2([a[0], a[2]], [b[0], b[2]]) / 3.2, b[1] / 3.2], [dist2([a[0], a[2]], [b[0], b[2]]) / 6.4, c[1] / 3.2], col);

  // Un ala de edificio: a-b es el frente (hacia afuera), d-c el fondo. Puntos [x,z].
  const ala = (B, a, b, c, d, h, tipo, colP, colT, o) => {
    const { izqH = 0, derH = 0, decal = false, k = 0, pisos = 1, sinFrente = false, ladrillo = false } = o || {};
    const dep = dist2(a, d);
    const out = [(a[0] - d[0]) / dep, (a[1] - d[1]) / dep];
    const off = hash(k + 31) * 6;
    const WB = ladrillo ? B.ladrillo : B.pared, esc = ladrillo ? 1.3 : 3.2;
    const cW = ladrillo ? new THREE.Color().setScalar(0.8 + hash(k + 3) * 0.35) : colP;
    if (!sinFrente) {
      pared(WB, a, b, 0, h, cW, off, esc);
      const e = 0.03;                                     // zócalo de sillares al pie de la fachada
      pared(B.sillar, [a[0] + out[0] * e, a[1] + out[1] * e], [b[0] + out[0] * e, b[1] + out[1] * e], 0, 0.75, BLANCO, off, 1.5);
    }
    if (h > izqH) pared(WB, d, a, izqH, h, cW, off + 1, esc);
    if (h > derH) pared(WB, b, c, derH, h, cW, off + 2, esc);
    if (tipo === 'plano') {
      const e = [out[0] * 0.15, out[1] * 0.15];
      const r = (p, y) => [p[0], y, p[1]];
      B.techo.quad(r(a, h), r(b, h), r(c, h), r(d, h), [a[0] / 1.5, a[1] / 1.5], [b[0] / 1.5, b[1] / 1.5], [c[0] / 1.5, c[1] / 1.5], [d[0] / 1.5, d[1] / 1.5], colT);
      const a2 = [a[0] + e[0], a[1] + e[1]], b2 = [b[0] + e[0], b[1] + e[1]];
      pared(B.pared, a2, b2, h, h + 0.65, colP, off);
      if (!sinFrente) {                                   // cornisa que sobresale: da sombra y relieve a la fachada
        const ce = 0.16, lc = colP.clone().multiplyScalar(1.06);
        const p1 = [a[0] + out[0] * ce, a[1] + out[1] * ce], q1 = [b[0] + out[0] * ce, b[1] + out[1] * ce];
        pared(B.pared, p1, q1, h - 0.42, h - 0.04, lc, off);
        B.pared.quad(W3(a, h - 0.04), W3(b, h - 0.04), W3(q1, h - 0.04), W3(p1, h - 0.04), [0, 0], [1, 0], [1, 0.1], [0, 0.1], lc);
        B.pared.quad(W3(a, h - 0.42), W3(b, h - 0.42), W3(q1, h - 0.42), W3(p1, h - 0.42), [0, 0], [1, 0], [1, 0.1], [0, 0.1], lc.clone().multiplyScalar(0.7));
      }
      pared(B.pared, d, a2, h, h + 0.55, colP, off);
      pared(B.pared, b2, c, h, h + 0.55, colP, off);
    } else {
      const rise = dep * 0.5 * 0.5;                       // pendiente ~27°
      const ra = lerp2(a, d, 0.5), rb = lerp2(b, c, 0.5);
      const voz = 0.45;
      const ap = [a[0] + out[0] * voz, h - 0.12, a[1] + out[1] * voz], bp = [b[0] + out[0] * voz, h - 0.12, b[1] + out[1] * voz];
      const dp = [d[0] - out[0] * voz, h - 0.12, d[1] - out[1] * voz], cp = [c[0] - out[0] * voz, h - 0.12, c[1] - out[1] * voz];
      const Ra = [ra[0], h + rise, ra[1]], Rb = [rb[0], h + rise, rb[1]];
      planoTecho(B.techo, ap, bp, Rb, Ra, colT);
      planoTecho(B.techo, dp, cp, Rb, Ra, colT);
      if (h > izqH || izqH === 0) triPared(B.pared, W3(a, h), W3(d, h), Ra, colP);
      if (h > derH || derH === 0) triPared(B.pared, W3(b, h), W3(c, h), Rb, colP);
    }
    if (decal) {
      const ancho = dist2(a, b);
      const bahias = Math.max(1, Math.floor(ancho / 3.3));
      const puerta = Math.floor(hash(k + 5) * bahias);
      for (let i = 0; i < bahias; i++) {
        const t = (i + 0.5) / bahias, cpt = lerp2(a, b, t);
        const dir = [(b[0] - a[0]) / ancho, (b[1] - a[1]) / ancho];
        const px = cpt[0] + out[0] * 0.05, pz = cpt[1] + out[1] * 0.05;
        const celda = (n) => [n * 0.25, (n + 1) * 0.25];
        const poner = (y0, y1, hw, n) => {
          const [u0, u1] = celda(n);
          const l = [px - dir[0] * hw, pz - dir[1] * hw], r = [px + dir[0] * hw, pz + dir[1] * hw];
          B.vano.quad([l[0], y0, l[1]], [r[0], y0, r[1]], [r[0], y1, r[1]], [l[0], y1, l[1]], [u0, 0], [u1, 0], [u1, 1], [u0, 1], BLANCO);
        };
        if (i === puerta && ancho > 4) poner(0, 3.3, 0.85, 1); else poner(0.55, 3.2, 0.7, 0);
        if (pisos > 1 || h > 6.8) poner(4.3, 7.0, 0.7, 2);
      }
    }
  };

  // ---------- Manzana con casas ----------
  const PROF = 24, FRENTE = 9.5, FONDO = 7;
  const lotes = (pts, i, lu, lv, cercaDeco, cent) => {
    const P = (u, v) => [
      (1 - v) * ((1 - u) * pts[0][0] + u * pts[1][0]) + v * ((1 - u) * pts[3][0] + u * pts[2][0]),
      (1 - v) * ((1 - u) * pts[0][1] + u * pts[1][1]) + v * ((1 - u) * pts[3][1] + u * pts[2][1])];
    const du = PROF / lu, dv = PROF / lv;
    const B = trozo((pts[0][0] + pts[2][0]) / 2, (pts[0][1] + pts[2][1]) / 2);
    const rnd = (k) => hash(i * 37 + k);
    const altura = (k) => (cent && rnd(k + 500) > 0.66) ? 7.4 + rnd(k + 900) * 1.2 : 4.2 + rnd(k) * 1.0;
    const franja = (Q, a0, a1, fOut, fIn, base, decor) => {
      const long = dist2(Q(a0, fOut), Q(a1, fOut));
      const n = Math.max(1, Math.round(long / (9 + rnd(base) * 5)));
      const hs = []; for (let t = 0; t < n; t++) hs.push(altura(base + t * 7));
      const fF = fOut + (fIn - fOut) * (FRENTE / PROF), fB = fIn - (fIn - fOut) * (FONDO / PROF);
      for (let t = 0; t < n; t++) {
        const k = base + t * 7, s0 = a0 + (a1 - a0) * t / n, s1 = a0 + (a1 - a0) * (t + 1) / n;
        const cen = Q((s0 + s1) / 2, (fOut + fIn) / 2); if (enReserva(cen[0], cen[1])) continue;   // lo ocupa un monumento
        const h = hs[t], colP = PAREDES[Math.floor(rnd(k + 77) * PAREDES.length)];
        const colT = TEJAS[Math.floor(rnd(k + 33) * TEJAS.length)];
        const tipo = rnd(k + 11) > 0.35 ? 'dos' : 'plano';
        const izqH = t > 0 ? hs[t - 1] : 0, derH = t + 1 < n ? hs[t + 1] : 0;
        ala(B, Q(s0, fOut), Q(s1, fOut), Q(s1, fF), Q(s0, fF), h, tipo, colP, colT, { izqH, derH, decal: decor, k, pisos: h > 6.8 ? 2 : 1, ladrillo: rnd(k + 19) > 0.78 });
        const hf = 3.6 + rnd(k + 3) * 0.8;
        ala(B, Q(s0, fB), Q(s1, fB), Q(s1, fIn), Q(s0, fIn), hf, rnd(k + 8) > 0.5 ? 'dos' : 'plano', colP, colT, { k: k + 1, sinFrente: false });
      }
    };
    const dec = cercaDeco;
    franja((s, f) => P(s, f), 0, 1, 0, dv, 1, dec);
    franja((s, f) => P(s, f), 0, 1, 1, 1 - dv, 100, dec);
    franja((s, f) => P(f, s), dv, 1 - dv, 0, du, 200, dec);
    franja((s, f) => P(f, s), dv, 1 - dv, 1, 1 - du, 300, dec);
    // patio / huerta del centro de la manzana (tierra oscura)
    const col = new THREE.Color('#ffffff').multiplyScalar(1.1 + rnd(1) * 0.25);
    const q = [P(du, dv), P(1 - du, dv), P(1 - du, 1 - dv), P(du, 1 - dv)];
    B.patio.quad(W3(q[0], 0.04), W3(q[1], 0.04), W3(q[2], 0.04), W3(q[3], 0.04),
      [q[0][0] / 6, q[0][1] / 6], [q[1][0] / 6, q[1][1] / 6], [q[2][0] / 6, q[2][1] / 6], [q[3][0] / 6, q[3][1] / 6], col);
    // vereda elevada con cordón, y calzada empedrada hasta la mitad de la calle
    const mv = 1.6 / lu, mh = 1.6 / lv, HV = 0.16, uvs = (p) => [p[0] / 2.2, p[1] / 2.2];
    for (const [u0, v0, u1, v1] of [[-mv, -mh, 1 + mv, 0], [-mv, 1, 1 + mv, 1 + mh], [-mv, 0, 0, 1], [1, 0, 1 + mv, 1]]) {
      const a = P(u0, v0), b = P(u1, v0), c = P(u1, v1), d = P(u0, v1);
      B.vereda.quad(W3(a, HV), W3(b, HV), W3(c, HV), W3(d, HV), uvs(a), uvs(b), uvs(c), uvs(d), BLANCO);
    }
    const oc = [P(-mv, -mh), P(1 + mv, -mh), P(1 + mv, 1 + mh), P(-mv, 1 + mh)];
    for (let j = 0; j < 4; j++) {
      const p = oc[j], q2 = oc[(j + 1) % 4], Lr = dist2(p, q2) / 2.2;
      B.vereda.quad(W3(p, 0), W3(q2, 0), W3(q2, HV), W3(p, HV), [0, 0], [Lr, 0], [Lr, 0.07], [0, 0.07], new THREE.Color('#c9c2b2'));
    }
    const cu = 7.5 / lu, cv = 7.5 / lv, cc = [P(-cu, -cv), P(1 + cu, -cv), P(1 + cu, 1 + cv), P(-cu, 1 + cv)];
    B.calle.quad(W3(cc[0], 0.03), W3(cc[1], 0.03), W3(cc[2], 0.03), W3(cc[3], 0.03), ...cc.map(p => [p[0] / 2.4, p[1] / 2.4]), new THREE.Color('#ffffff').multiplyScalar(1.15 + hash(i) * 0.2));
    registrarAltura(oc, HV);
  };

  // Manzana lejana: volumen simple
  const cajaSimple = (pts, i) => {
    const B = trozo((pts[0][0] + pts[2][0]) / 2, (pts[0][1] + pts[2][1]) / 2);
    const h = 4.6 + hash(i) * 1.2, colP = PAREDES[Math.floor(hash(i + 9) * PAREDES.length)], colT = TEJAS[Math.floor(hash(i + 3) * TEJAS.length)];
    for (let j = 0; j < 4; j++) pared(B.pared, pts[j], pts[(j + 1) % 4], 0, h, colP, hash(i + j) * 5);
    const r = (p) => [p[0], h, p[1]];
    B.techo.quad(r(pts[0]), r(pts[1]), r(pts[2]), r(pts[3]), ...pts.map(p => [p[0] / 1.5, p[1] / 1.5]), colT.clone().multiplyScalar(0.9));
  };

  // ---------- Reservas para monumentos ----------
  const reservas = [
    { x0: -62, x1: 62, z0: -140, z1: -62 },       // Cabildo (al norte de la plaza)
  ];
  const enReserva = (x, z) => reservas.some(r => x > r.x0 && x < r.x1 && z > r.z0 && z < r.z1);

  // ---------- Recorrido de manzanas ----------
  const N = manzanas.length / 10;
  const RADIO_LOTES = 1500, RADIO_DECOR = 520;
  let detalle = 0, simples = 0;
  const bloques = [];
  for (let i = 0; i < N; i++) {
    const f = i * 10, pts = [];
    for (let k = 0; k < 4; k++) pts.push(aMundo(manzanas[f + k * 2], manzanas[f + k * 2 + 1]));
    const xs = pts.map(p => p[0]), zs = pts.map(p => p[1]);
    if (Math.max(...xs) - Math.min(...xs) > 400 || Math.max(...zs) - Math.min(...zs) > 400) continue;
    registrar(pts); bloques.push(pts);
    const cx = (pts[0][0] + pts[2][0]) / 2, cz = (pts[0][1] + pts[2][1]) / 2, d = Math.hypot(cx, cz);
    const lu = dist2(pts[0], pts[1]), lv = dist2(pts[0], pts[3]);
    if (d < RADIO_LOTES && lu > 2 * PROF + 14 && lv > 2 * PROF + 14) { lotes(pts, i, lu, lv, d < RADIO_DECOR, d < 700); detalle++; }
    else { cajaSimple(pts, i); simples++; }
  }

  // ---------- Monumentos ----------
  const grupoMon = new THREE.Group(); scene.add(grupoMon);
  const agregar = (geo, mat, x, y, z) => { const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); m.castShadow = m.receiveShadow = true; grupoMon.add(m); return m; };

  // Pirámide de Mayo: pedestal escalonado de sillares, fuste de travertino, remate con estatua y reja de hierro
  {
    const conTex = (tc, tn, rx, ry) => {
      const c = tc.clone(), n = tn.clone(); for (const t of [c, n]) { t.repeat.set(rx, ry); t.needsUpdate = true; }
      return new THREE.MeshStandardMaterial({ map: c, normalMap: n, normalScale: new THREE.Vector2(1.3, 1.3), roughness: 0.9, flatShading: true });
    };
    const sill = (rx, ry) => conTex(T.sillC, T.sillN, rx, ry), trav = (rx, ry) => conTex(T.traC, T.traN, rx, ry);
    agregar(new THREE.BoxGeometry(7.2, 0.35, 7.2), sill(5, 0.3), 0, 0.175, 0);
    agregar(new THREE.BoxGeometry(6.0, 0.35, 6.0), sill(4, 0.3), 0, 0.525, 0);
    agregar(new THREE.BoxGeometry(4.8, 0.35, 4.8), sill(3.4, 0.3), 0, 0.875, 0);
    agregar(new THREE.BoxGeometry(3.6, 1.6, 3.6), sill(2.5, 1.1), 0, 1.85, 0);
    agregar(new THREE.BoxGeometry(4.0, 0.24, 4.0), trav(2.5, 0.2), 0, 2.77, 0);
    for (const [sx, sz] of [[1, 1], [-1, 1], [1, -1], [-1, -1]]) {
      agregar(new THREE.BoxGeometry(0.45, 0.4, 0.45), trav(0.4, 0.4), sx * 1.65, 3.09, sz * 1.65);
      agregar(new THREE.ConeGeometry(0.34, 0.9, 4).rotateY(Math.PI / 4), trav(0.5, 0.6), sx * 1.65, 3.75, sz * 1.65);
    }
    for (const [sx, sz, ry] of [[0, 1.81, 0], [0, -1.81, Math.PI], [1.81, 0, Math.PI / 2], [-1.81, 0, -Math.PI / 2]]) {
      const pl = agregar(new THREE.BoxGeometry(1.5, 0.9, 0.05), mats.oscuro, sx, 1.9, sz); pl.rotation.y = ry;
    }
    agregar(new THREE.CylinderGeometry(0.5, 1.15, 8.4, 4, 1).rotateY(Math.PI / 4), trav(1, 2.6), 0, 7.2, 0);
    agregar(new THREE.CylinderGeometry(0.05, 0.5, 1.1, 4, 1).rotateY(Math.PI / 4), trav(0.5, 0.5), 0, 11.95, 0);
    agregar(new THREE.CylinderGeometry(0.16, 0.24, 0.9, 6), mats.plomo, 0, 12.95, 0);
    agregar(new THREE.SphereGeometry(0.17, 6, 5), mats.plomo, 0, 13.55, 0);
    const brazo = agregar(new THREE.BoxGeometry(0.07, 0.6, 0.07), mats.plomo, 0.2, 13.55, 0); brazo.rotation.z = -0.5;
    const fe = new THREE.MeshStandardMaterial({ color: '#2a2724', flatShading: true, roughness: 0.6, metalness: 0.4 });
    const lado = 10.4, np = 11, postes = [];
    for (let i = 0; i < np * 4; i++) { const t = (i % np) / np * lado - lado / 2, k = Math.floor(i / np); postes.push(k === 0 ? [t, -lado / 2] : k === 1 ? [lado / 2, t] : k === 2 ? [-t, lado / 2] : [-lado / 2, -t]); }
    const iP = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.035, 0.045, 1.5, 5).translate(0, 0.75, 0), fe, postes.length);
    const iC = new THREE.InstancedMesh(new THREE.ConeGeometry(0.07, 0.2, 4).translate(0, 1.6, 0), fe, postes.length);
    const mm = new THREE.Matrix4(); postes.forEach(([x, z], i) => { mm.makeTranslation(x, 0, z); iP.setMatrixAt(i, mm); iC.setMatrixAt(i, mm); });
    iP.castShadow = iC.castShadow = true; grupoMon.add(iP, iC);
    for (const [x, z, sx, sz] of [[0, -lado / 2, lado, 0.05], [0, lado / 2, lado, 0.05], [-lado / 2, 0, 0.05, lado], [lado / 2, 0, 0.05, lado]])
      for (const y of [0.45, 1.25]) agregar(new THREE.BoxGeometry(sx, 0.05, sz), fe, x, y, z);
    rect(-lado / 2 - 0.3, -lado / 2 - 0.3, lado / 2 + 0.3, lado / 2 + 0.3);
  }

  // zE: borde de la manzana al norte de la plaza (primer punto ocupado yendo hacia -z)
  let zE = -68; for (let z = -58; z > -140; z -= 0.5) if (ocupado(0, z)) { zE = z; break; }
  const zS = (() => { for (let z = 58; z < 200; z += 0.5) if (ocupado(0, z)) return z; return 120; })();

  // arcada: plano con arcos recortados (alpha). x0..x1 en el eje x, a z fijo, mirando hacia 'cara'
  const arcada = (x0, x1, z, h, pisos, cara) => {
    const bahias = Math.round((x1 - x0) / 4);
    const t = (pisos === 1 ? arco1 : arco2).clone(); t.needsUpdate = true; t.repeat.set(bahias, 1); t.wrapS = THREE.RepeatWrapping;
    const mat = (pisos === 1 ? mats.arco1 : mats.arco2).clone(); mat.map = t;
    const m = new THREE.Mesh(new THREE.PlaneGeometry(x1 - x0, h), mat);
    m.position.set((x0 + x1) / 2, h / 2, z); if (cara < 0) m.rotation.y = Math.PI;
    m.castShadow = m.receiveShadow = true; grupoMon.add(m);
  };
  const cajaTexturada = (x0, x1, z0, z1, h, colP) => {
    const B = new Buf(); const p = [[x0, z0], [x1, z0], [x1, z1], [x0, z1]];
    for (let j = 0; j < 4; j++) pared(B, p[j], p[(j + 1) % 4], 0, h, colP, 0);
    const m = B.malla(mats.pared); if (m) grupoMon.add(m);
  };
  const techoDosAguas = (x0, x1, z0, z1, h, colT, colP) => {
    const B = { pared: new Buf(), techo: new Buf(), vano: new Buf(), sillar: new Buf(), ladrillo: new Buf() };
    ala(B, [x0, z1], [x1, z1], [x1, z0], [x0, z0], h, 'dos', colP, colT, { sinFrente: true, izqH: h, derH: h });
    const a = B.techo.malla(mats.techo); if (a) grupoMon.add(a);
    const b = B.pared.malla(mats.pared); if (b) grupoMon.add(b);
  };

  // Cabildo: dos plantas de arcos, torre central y techo de teja
  {
    const x0 = -34, x1 = 34, hCab = 11, prof = 22;
    const colCab = new THREE.Color('#f6eed8'), colT = new THREE.Color('#f6dccf').multiplyScalar(1.5);
    cajaTexturada(x0, x1, zE - prof, zE - 2.6, hCab, new THREE.Color('#d6c8aa'));          // volumen al fondo de la galería
    cajaTexturada(x0, x0 + 0.3, zE - 2.6, zE, hCab, colCab); cajaTexturada(x1 - 0.3, x1, zE - 2.6, zE, hCab, colCab);
    arcada(x0, x1, zE - 0.2, hCab, 2, 1);
    const piso = new THREE.Mesh(new THREE.PlaneGeometry(x1 - x0, 2.6), mats.oscuro); piso.rotation.x = Math.PI / 2; piso.position.set(0, 5.6, zE - 1.4); grupoMon.add(piso);
    const techoC = new THREE.Mesh(new THREE.PlaneGeometry(x1 - x0, 2.6), mats.oscuro); techoC.rotation.x = Math.PI / 2; techoC.position.set(0, hCab - 0.05, zE - 1.4); grupoMon.add(techoC);
    techoDosAguas(x0, x1, zE - prof, zE, hCab, colT, colCab);
    // torre
    cajaTexturada(-3.8, 3.8, zE - 9, zE - 1.2, 21, colCab);
    const tz = zE - 5.1;
    const pir = new THREE.ConeGeometry(6.2, 5, 4).rotateY(Math.PI / 4);
    agregar(pir, new THREE.MeshStandardMaterial({ color: '#8a8f94', roughness: 0.55, metalness: 0.4 }), 0, 23.5, tz);
    agregar(new THREE.BoxGeometry(2.4, 0.4, 0.1), mats.piedra, 0, 17, zE - 1.1);
    const reloj = agregar(new THREE.CircleGeometry(0.9, 24), new THREE.MeshStandardMaterial({ color: '#f4efe0' }), 0, 15.5, zE - 1.14); reloj.castShadow = false;
    rect(x0, zE - prof, x1, zE);
  }

  // Recova Vieja: galería larga de arcos que divide las dos plazas
  {
    const zR = zS - 14; const x0 = -58, x1 = 58, h = 6.8;
    const colT = new THREE.Color('#f2d6c6').multiplyScalar(1.5);
    arcada(x0, x1, zR + 3.2, h, 1, 1); arcada(x0, x1, zR - 3.2, h, 1, -1);
    const sl = new THREE.Mesh(new THREE.PlaneGeometry(x1 - x0, 6.4), mats.oscuro); sl.rotation.x = Math.PI / 2; sl.position.set(0, h - 0.1, zR); grupoMon.add(sl);
    techoDosAguas(x0 - 0.3, x1 + 0.3, zR - 3.6, zR + 3.6, h, colT, new THREE.Color('#eadfc6'));
    // portal central más alto
    cajaTexturada(-6, 6, zR - 3.8, zR + 3.8, h + 3.2, new THREE.Color('#e6dcc3'));
    const arcoPortal = new THREE.Mesh(new THREE.CircleGeometry(2.6, 24, 0, Math.PI), mats.oscuro);
    arcoPortal.position.set(0, 3.2, zR + 3.85); arcoPortal.castShadow = false; grupoMon.add(arcoPortal);
    const arcoPortalB = arcoPortal.clone(); arcoPortalB.position.z = zR - 3.85; arcoPortalB.rotation.y = Math.PI; grupoMon.add(arcoPortalB);
    const cuerpo = new THREE.Mesh(new THREE.BoxGeometry(11.9, 3.2, 7.4), mats.oscuro); cuerpo.position.set(0, 1.6, zR); grupoMon.add(cuerpo);
    rect(x0, zR - 3.6, -3, zR + 3.6); rect(3, zR - 3.6, x1, zR + 3.6);
  }

  // Iglesias
  iglesias.forEach((g, idx) => {
    const [cx, cz] = aMundo(g.x, g.y);
    const ux = g.ux, uz = g.uy, vx = -uz, vz = ux;
    const L = g.largo * ESC / 2, A = g.ancho * ESC / 2;
    const pt = (u, v) => [cx + ux * u + vx * v, cz + uz * u + vz * v];
    const poligono = [pt(L, A), pt(L, -A), pt(-L, -A), pt(-L, A)];
    registrar(poligono);
    const esCatedral = idx === 11;
    const B = trozo(cx, cz);
    const colP = new THREE.Color(esCatedral ? '#f3ead4' : '#efe6cf'), colT = new THREE.Color('#ecccb8').multiplyScalar(1.5);
    const hN = esCatedral ? 15 : 10.5;
    // nave
    ala(B, pt(L, A), pt(-L, A), pt(-L, -A), pt(L, -A), hN, 'dos', colP, colT, { izqH: 0, derH: 0, k: idx });
    // frente (frente=±1 sobre el eje u)
    const fr = g.frente;
    if (esCatedral) {
      const Wp = A * 2 - 1, prof = 8;
      const uF = fr * L, uP = fr * (L + prof);
      // pórtico
      ala(B, pt(uF, Wp / 2), pt(uP, Wp / 2), pt(uP, -Wp / 2), pt(uF, -Wp / 2), 12, 'dos', colP, colT, { izqH: 12, derH: 12, k: 99, sinFrente: true });
      for (let c = 0; c < 12; c++) {
        const v = -Wp / 2 + 1.3 + c * (Wp - 2.6) / 11, [x, z] = pt(uP, v);
        agregar(new THREE.CylinderGeometry(0.62, 0.7, 11.2, 14), mats.piedra, x, 5.6, z);
      }
      // cúpula
      const [dx, dz] = pt(fr * (L - 14), 0);
      agregar(new THREE.CylinderGeometry(6.6, 6.8, 7, 20), mats.piedra, dx, hN + 3.4, dz);
      agregar(new THREE.SphereGeometry(6.7, 20, 10, 0, Math.PI * 2, 0, Math.PI / 2), mats.plomo, dx, hN + 6.9, dz);
      agregar(new THREE.CylinderGeometry(1.1, 1.3, 2.4, 10), mats.piedra, dx, hN + 14, dz);
      registrar([pt(uF, Wp / 2), pt(uP, Wp / 2), pt(uP, -Wp / 2), pt(uF, -Wp / 2)]);
    } else {
      // torre con techo piramidal al frente
      const tw = Math.min(4.2, A * 0.6), uT = fr * (L - tw);
      const base = [pt(uT + tw, tw), pt(uT + tw, -tw), pt(uT - tw, -tw), pt(uT - tw, tw)];
      for (let j = 0; j < 4; j++) pared(B.pared, base[j], base[(j + 1) % 4], 0, 22, colP, j);
      const [tx, tz] = pt(uT, 0);
      agregar(new THREE.ConeGeometry(tw * 1.5, 6, 4).rotateY(Math.atan2(uz, ux) + Math.PI / 4), new THREE.MeshStandardMaterial({ color: '#7d858a', roughness: 0.55, metalness: 0.4 }), tx, 25, tz);
      agregar(new THREE.CylinderGeometry(0.1, 0.1, 2.2), mats.plomo, tx, 29, tz);
    }
  });

  // ---------- Farolas en las esquinas de las manzanas cercanas ----------
  {
    const postes = [];
    bloques.forEach((pts, bi) => {
      const cx = (pts[0][0] + pts[2][0]) / 2, cz = (pts[0][1] + pts[2][1]) / 2;
      if (Math.hypot(cx, cz) > 650) return;
      pts.forEach((p, k) => {
        if (hash(bi * 7 + k * 13) < 0.45) return;
        const dx = p[0] - cx, dz = p[1] - cz, l = Math.hypot(dx, dz) || 1;
        postes.push([p[0] + dx / l * 1.6, p[1] + dz / l * 1.6]);
      });
    });
    const n = postes.length;
    const poste = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.05, 0.08, 4.2, 5).translate(0, 2.1, 0), new THREE.MeshStandardMaterial({ color: '#2d2a28', flatShading: true }), n);
    const farol = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.2, 0.13, 0.42, 4).translate(0, 4.45, 0), new THREE.MeshStandardMaterial({ color: '#ffd98a', emissive: '#ffb347', emissiveIntensity: 0.55, flatShading: true }), n);
    const sombrero = new THREE.InstancedMesh(new THREE.ConeGeometry(0.28, 0.2, 4).translate(0, 4.78, 0), new THREE.MeshStandardMaterial({ color: '#2d2a28', flatShading: true }), n);
    const m = new THREE.Matrix4();
    postes.forEach(([x, z], i) => { m.makeTranslation(x, 0, z); poste.setMatrixAt(i, m); farol.setMatrixAt(i, m); sombrero.setMatrixAt(i, m); });
    poste.castShadow = true; sombrero.castShadow = true;
    scene.add(poste, farol, sombrero);
  }

  // ---------- Armar mallas por fragmento ----------
  for (const t of trozos.values()) {
    for (const [clave, mat, sombra] of [['pared', mats.pared, true], ['techo', mats.techo, true], ['vano', mats.vano, false], ['patio', mats.patio, false], ['vereda', mats.vereda, true], ['calle', mats.calle, false], ['ladrillo', mats.ladrillo, true], ['sillar', mats.sillar, true]]) {
      const m = t[clave].malla(mat, sombra); if (m) { if (clave === 'pared' || clave === 'techo' || clave === 'vano') m.userData.sinXray = true; scene.add(m); }
    }
  }
  console.log('manzanas con casas', detalle, 'simples', simples);
}

// ---------- Árboles ----------
export function construirArboles(scene, arboles, aMundo) {
  const n = arboles.length / 3;
  // copa facetada: icosaedro deformado con pocas caras, más claro arriba
  const geo = new THREE.IcosahedronGeometry(1, 1);
  const pos = geo.attributes.position; const col = [];
  for (let i = 0; i < pos.count; i++) {
    const x = pos.getX(i), y = pos.getY(i), z = pos.getZ(i);
    const r = 1 + 0.16 * Math.sin(x * 5.1 + z * 3.3) * Math.cos(y * 4.7);
    pos.setXYZ(i, x * r, (y * 0.88 + 0.12) * r, z * r);
    const luz = 0.7 + 0.3 * ((y + 1) / 2);
    col.push(luz, luz, luz);
  }
  geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3)); geo.computeVertexNormals();
  const copa = new THREE.InstancedMesh(geo, new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 1, flatShading: true }), n);
  const tronco = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.14, 0.24, 1, 5).translate(0, 0.5, 0), new THREE.MeshStandardMaterial({ color: '#7a5535', roughness: 1, flatShading: true }), n);
  const m = new THREE.Matrix4(), q = new THREE.Quaternion(), s = new THREE.Vector3(), p = new THREE.Vector3();
  const c = new THREE.Color();
  for (let i = 0; i < n; i++) {
    const [x, z] = aMundo(arboles[i * 3], arboles[i * 3 + 1]);
    const r = Math.max(1.3, Math.min(arboles[i * 3 + 2] * 0.4, 2.6)) * (0.85 + hash(i) * 0.35);
    q.setFromAxisAngle(new THREE.Vector3(0, 1, 0), hash(i + 7) * 6.28);
    p.set(x, r * 0.9 + 1.7, z); s.set(r, r, r); m.compose(p, q, s); copa.setMatrixAt(i, m);
    c.setHSL(0.27 + hash(i + 3) * 0.07, 0.45 + hash(i + 5) * 0.15, 0.42 + hash(i + 9) * 0.1); copa.setColorAt(i, c);
    p.set(x, 0, z); s.set(1, r * 0.9 + 1.9, 1); m.compose(p, q, s); tronco.setMatrixAt(i, m);
  }
  copa.castShadow = tronco.castShadow = true; copa.receiveShadow = true;
  copa.frustumCulled = false; tronco.frustumCulled = false;
  scene.add(copa, tronco);
}

// ---------- Carros con caballo (low poly angular, mismo lenguaje que los personajes) ----------
export function construirCarros(scene, libre, aMundo) {
  const Mt = (c, o = {}) => new THREE.MeshStandardMaterial({ color: c, flatShading: true, roughness: 1, ...o });
  const sombra = (m) => { m.castShadow = true; return m; };
  const caja = (x, y, z, mat) => sombra(new THREE.Mesh(new THREE.BoxGeometry(x, y, z), mat));
  const poner = (m, x, y, z, rx = 0, ry = 0, rz = 0) => { m.position.set(x, y, z); m.rotation.set(rx, ry, rz); return m; };
  const tronco = (rs, ri, h, mat, seg = 5) => sombra(new THREE.Mesh(new THREE.CylinderGeometry(rs, ri, h, seg), mat));
  const cono = (r, h, mat, seg = 4) => sombra(new THREE.Mesh(new THREE.ConeGeometry(r, h, seg), mat));
  const octa = (r, mat, sx, sy, sz) => { const m = sombra(new THREE.Mesh(new THREE.OctahedronGeometry(r, 0), mat)); m.scale.set(sx, sy, sz); return m; };

  // Cuerpo "loft": anillos de pocos lados a lo largo de un eje, unidos en facetas (silueta continua y angular)
  function loft(sec, lados, mat) {
    const pos = [], idx = [], n = lados;
    for (const q of sec) for (let i = 0; i < n; i++) { const t = (i / n) * Math.PI * 2; pos.push((q.x || 0) + Math.cos(t) * q.rx, q.y + Math.sin(t) * q.ry, q.z); }
    for (let k = 0; k < sec.length - 1; k++) for (let i = 0; i < n; i++) {
      const A = k * n + i, B = k * n + (i + 1) % n, C = (k + 1) * n + (i + 1) % n, D = (k + 1) * n + i; idx.push(A, B, C, A, C, D);
    }
    const tapa = (q, k, inv) => { const c = pos.length / 3; pos.push(q.x || 0, q.y, q.z); for (let i = 0; i < n; i++) { const A = k * n + i, B = k * n + (i + 1) % n; if (inv) idx.push(c, B, A); else idx.push(c, A, B); } };
    tapa(sec[0], 0, false); tapa(sec[sec.length - 1], sec.length - 1, true);
    const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setIndex(idx); g.computeVertexNormals();
    return sombra(new THREE.Mesh(g, mat));
  }

  // Caballo mirando hacia +z. Devuelve el grupo y una función de animación en reposo.
  function caballo(color, calcetines) {
    const g = new THREE.Group();
    const c0 = new THREE.Color(color), D = THREE.DoubleSide;
    const pel = Mt(color, { side: D }), oscuro = Mt(c0.clone().multiplyScalar(0.6), { side: D }), crin = Mt(c0.clone().multiplyScalar(0.3), { side: D });
    const pezu = Mt('#2a2420'), cuero = Mt('#4a3220'), blanco = Mt('#efe6d2'), hocico = Mt(c0.clone().multiplyScalar(0.42), { side: D });
    // cuerpo: grupa, lomo, pecho (secciones de 7 lados)
    g.add(loft([
      { z: -0.98, rx: 0.1, ry: 0.15, y: 1.22 }, { z: -0.82, rx: 0.25, ry: 0.32, y: 1.2 }, { z: -0.46, rx: 0.3, ry: 0.37, y: 1.17 },
      { z: -0.05, rx: 0.27, ry: 0.33, y: 1.1 }, { z: 0.36, rx: 0.29, ry: 0.37, y: 1.14 }, { z: 0.7, rx: 0.22, ry: 0.34, y: 1.2 }, { z: 0.9, rx: 0.12, ry: 0.22, y: 1.26 },
    ], 7, pel));
    // patas: muslo, caña con jarrete, cuartilla y casco
    const patas = [];
    for (const [x, z, trasera] of [[-0.17, 0.55, false], [0.17, 0.55, false], [-0.2, -0.58, true], [0.2, -0.58, true]]) {
      const cad = new THREE.Group(); cad.position.set(x, 0.92, z);
      cad.add(poner(tronco(trasera ? 0.11 : 0.085, 0.055, 0.46, pel), 0, -0.21, 0));
      const rod = new THREE.Group(); rod.position.y = -0.42; cad.add(rod);
      rod.add(poner(tronco(0.045, 0.036, 0.38, oscuro), 0, -0.18, 0));
      rod.add(poner(octa(0.05, oscuro, 1, 0.8, 1), 0, -0.02, 0));                          // rodilla/jarrete
      if (calcetines) rod.add(poner(tronco(0.055, 0.052, 0.13, blanco, 5), 0, -0.3, 0));
      rod.add(poner(tronco(0.058, 0.075, 0.1, pezu, 6), 0, -0.43, 0.01));
      if (trasera) { cad.rotation.x = -0.3; rod.rotation.x = 0.5; }                          // jarrete hacia atrás
      g.add(cad); patas.push(cad);
    }
    // cuello (grupo con pivote en la cruz)
    const cuello = new THREE.Group(); cuello.position.set(0, 1.3, 0.58); cuello.rotation.x = -0.85; g.add(cuello);
    cuello.add(loft([
      { z: -0.05, rx: 0.19, ry: 0.23, y: 0 }, { z: 0.3, rx: 0.14, ry: 0.2, y: 0.01 }, { z: 0.55, rx: 0.115, ry: 0.16, y: 0.0 }, { z: 0.75, rx: 0.1, ry: 0.13, y: -0.01 },
    ], 7, pel));
    const collar = new THREE.Mesh(new THREE.TorusGeometry(0.2, 0.05, 4, 7), Mt('#6b4528')); collar.position.z = 0.1; collar.scale.y = 1.15; cuello.add(sombra(collar));   // collerón
    cuello.add(loft([                                                                       // crin: tira oscura continua sobre la nuca
      { z: 0.0, rx: 0.03, ry: 0.07, y: 0.25 }, { z: 0.3, rx: 0.028, ry: 0.07, y: 0.23 }, { z: 0.55, rx: 0.025, ry: 0.065, y: 0.19 }, { z: 0.74, rx: 0.022, ry: 0.05, y: 0.15 },
    ], 4, crin));
    // cabeza (grupo con pivote en la punta del cuello)
    const cabeza = new THREE.Group(); cabeza.position.set(0, -0.01, 0.75); cabeza.rotation.x = 1.3; cuello.add(cabeza);
    cabeza.add(loft([
      { z: -0.03, rx: 0.1, ry: 0.13, y: 0 }, { z: 0.22, rx: 0.08, ry: 0.105, y: -0.01 }, { z: 0.42, rx: 0.058, ry: 0.07, y: -0.025 }, { z: 0.52, rx: 0.052, ry: 0.058, y: -0.03 },
    ], 6, pel));
    cabeza.add(poner(octa(0.055, hocico, 1, 0.9, 1.2), 0, -0.03, 0.5));                      // hocico oscuro
    if (calcetines) cabeza.add(poner(caja(0.045, 0.014, 0.34, blanco), 0, 0.115, 0.16, 0.14));   // mancha blanca en la cara
    for (const sx of [-1, 1]) {
      cabeza.add(poner(octa(0.017, pezu, 1, 1, 0.7), sx * 0.093, 0.035, 0.1));              // ojos
      cabeza.add(poner(cono(0.042, 0.18, pel, 3), sx * 0.06, 0.15, -0.0, -0.1, 0, -sx * 0.2)); // orejas
    }
    cabeza.add(poner(cono(0.055, 0.14, crin, 3), 0, 0.15, 0.08, 0.5));                      // copete
    // arneses
    const cincha = new THREE.Mesh(new THREE.TorusGeometry(0.31, 0.022, 4, 7), cuero); cincha.scale.y = 1.2; cincha.position.set(0, 1.13, 0.15); g.add(sombra(cincha));
    g.add(poner(caja(0.6, 0.09, 0.05, cuero), 0, 0.98, 0.84));
    // cola
    const cola = new THREE.Group(); cola.position.set(0, 1.3, -0.9); g.add(cola);
    cola.add(poner(cono(0.09, 0.85, crin, 4), 0, -0.4, -0.2, -0.32));
    cola.add(poner(octa(0.1, crin, 1, 1.6, 1), 0, -0.85, -0.32));
    const animar = (t) => {                                                                  // reposo: cabeceo, cola, a veces rasca el suelo
      cuello.rotation.x = -0.85 + Math.sin(t * 0.5) * 0.05; cuello.rotation.y = Math.sin(t * 0.23) * 0.12;
      cabeza.rotation.x = 1.3 + Math.sin(t * 0.7 + 1) * 0.06;
      cola.rotation.z = Math.sin(t * 0.9) * 0.18; cola.rotation.x = Math.sin(t * 0.6) * 0.06;
      patas[1].rotation.x = Math.max(0, Math.sin(t * 0.4 - 1)) * 0.35 * Math.max(0, Math.sin(t * 0.13));
    };
    g.userData.sinXray = true; g.userData.vivo = true; return { g, animar };
  }

  // Carro de madera con ruedas facetadas. Su frente mira hacia +z (varas hacia el caballo).
  function carro(carga) {
    const g = new THREE.Group(), mad = Mt('#8a5a34'), mad2 = Mt('#6f4527'), rue = Mt('#3f2b1c'), hierro = Mt('#2a2724');
    g.add(poner(caja(1.35, 0.14, 2.1, mad), 0, 0.86, -2.1));
    for (const x of [-0.69, 0.69]) g.add(poner(caja(0.08, 0.38, 2.1, mad2), x, 1.08, -2.1));
    g.add(poner(caja(1.35, 0.38, 0.08, mad2), 0, 1.08, -3.14));
    g.add(poner(caja(1.35, 0.3, 0.08, mad2), 0, 1.04, -1.08));
    for (const x of [-0.82, 0.82]) {
      const r = tronco(0.58, 0.58, 0.09, rue, 8); r.position.set(x, 0.58, -2.1); r.rotation.z = Math.PI / 2; g.add(r);
      g.add(poner(caja(0.11, 0.22, 0.22, hierro), x, 0.58, -2.1));
      for (let k = 0; k < 4; k++) g.add(poner(caja(0.05, 1.1, 0.06, rue), x, 0.58, -2.1, k * Math.PI / 4, 0, 0));      // rayos
    }
    g.add(poner(caja(1.95, 0.07, 0.07, hierro), 0, 0.58, -2.1));                              // eje
    for (const x of [-0.46, 0.46]) g.add(poner(caja(0.07, 0.07, 2.0, mad), x, 0.95, -0.2, -0.06, 0, 0));   // varas hacia el caballo
    if (carga === 'heno') {
      g.add(poner(caja(1.2, 0.75, 1.8, Mt('#e0c46a')), 0, 1.5, -2.1));
      g.add(poner(octa(0.5, Mt('#d4b85c'), 1.2, 0.6, 1.5), 0, 2.0, -2.1));
    } else if (carga === 'barriles') {
      for (const [x, z] of [[-0.32, -1.7], [0.32, -1.7], [0, -2.6]]) {
        const b = tronco(0.3, 0.27, 0.75, Mt('#9a6a3c'), 7); b.position.set(x, 1.32, z); g.add(b);
        for (const y of [-0.2, 0.2]) { const a = new THREE.Mesh(new THREE.TorusGeometry(0.29, 0.012, 4, 7), hierro); a.rotation.x = Math.PI / 2; a.position.set(x, 1.32 + y, z); g.add(a); }
      }
    } else {
      g.add(poner(caja(0.85, 0.55, 0.85, Mt('#b58a52')), -0.22, 1.25, -1.75));
      g.add(poner(caja(0.65, 0.45, 0.65, Mt('#a67a44')), 0.27, 1.2, -2.6));
      g.add(poner(caja(0.5, 0.3, 0.5, Mt('#c29a62')), -0.2, 1.7, -1.75));
    }
    g.add(poner(caja(1.35, 0.1, 0.42, Mt('#3a5a7a')), 0, 1.5, -1.3));                         // asiento
    return g;
  }

  const lista = [[52, 48, 0.4, '#7a4a2a', 'heno', true], [-54, -58, 2.6, '#2a2018', 'barriles', false], [-125, 30, 1.2, '#a8754a', 'cajas', true],
    [92, -22, 4.2, '#5a3a24', 'heno', false], [10, 90, 3.3, '#c9a779', 'barriles', true]];
  const animadores = [];
  for (const [x, z, rot, color, carga, calcetines] of lista) {
    const libreTodo = (px, pz) => libre(px, pz) && libre(px + Math.sin(rot) * 4, pz + Math.cos(rot) * 4) && libre(px - Math.sin(rot) * 3, pz - Math.cos(rot) * 3);
    let px = x, pz = z;
    if (!libreTodo(px, pz)) { let ok = false; for (let r = 3; r < 70 && !ok; r += 3) for (let a = 0; a < 12 && !ok; a++) { const xx = x + Math.cos(a * Math.PI / 6) * r, zz = z + Math.sin(a * Math.PI / 6) * r; if (libreTodo(xx, zz)) { px = xx; pz = zz; ok = true; } } }
    const grupo = new THREE.Group();
    const cb = caballo(color, calcetines); grupo.add(cb.g);
    const car = carro(carga); car.position.z = -0.5; grupo.add(car);
    grupo.position.set(px, 0, pz); grupo.rotation.y = rot; scene.add(grupo);
    // el conjunto es un obstáculo: rectángulo orientado (x ±1.05, z de -3.7 a 1.7)
    const co = Math.cos(rot), si = Math.sin(rot);
    const mundo = (lx, lz) => [px + lx * co + lz * si, pz - lx * si + lz * co];
    registrar([mundo(-1.05, -3.7), mundo(1.05, -3.7), mundo(1.05, 1.7), mundo(-1.05, 1.7)]);
    animadores.push({ x: px, z: pz, animar: cb.animar, fase: Math.random() * 20 });
  }
  return animadores;
}
