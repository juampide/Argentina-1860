import * as THREE from 'three';
import { pieza, ensamblar, ICO, ICO0, CONO, CIL, verde, hs, mix, K, CAMINO, LAGUNA, MONTE, suavizar } from './campo.js';
import { fabricaCaballos } from './animales.js';
import { libre, personas, choca, registrar } from './colision.js';
import { crearPersonaje } from './personaje.js';

const matAni = new THREE.MeshStandardMaterial({ vertexColors: true, flatShading: true, roughness: 1, side: THREE.DoubleSide });
const matPatas = {};
const patasMat = (c) => (matPatas[c] ??= new THREE.MeshStandardMaterial({ color: c, flatShading: true, roughness: 1 }));
const oscurecer = (c, f = 0.6) => '#' + new THREE.Color(c).multiplyScalar(f).getHexString();

// ---------- cuadrúpedos: cuerpo facetado de una pieza + 4 patas que se mueven ----------
function cuadrupedo(s) {
  const base = s.pat, A = s.A, L = s.L, gr = s.grosor, cu = s.cuello || 0, cab = s.cab, P = [];
  P.push(pieza(ICO, s.color, [0, base + A * 0.55, 0], [gr, A * 0.55, L * 0.5]));
  P.push(pieza(ICO, s.claro || s.color, [0, base + A * 0.28, 0], [gr * 0.92, A * 0.3, L * 0.42]));
  const zc0 = L * 0.4;
  if (cu > 0) P.push(pieza(CIL(gr * 0.34, gr * 0.52, cu, 5), s.color, [0, base + A * 0.8 + cu * 0.38, zc0 + cu * 0.22], [1, 1, 1], [0.65, 0, 0]));
  const yc = base + A * 0.8 + cu * 0.78 + cab * 0.05, zc = zc0 + cu * 0.5 + cab * 0.2;
  P.push(pieza(ICO, s.color, [0, yc, zc], [cab * 0.26, cab * 0.28, cab * 0.5]));
  P.push(pieza(CONO(cab * 0.17, cab * 0.5, 4), s.hocico || s.color, [0, yc - cab * 0.06, zc + cab * 0.55], [1, 1, 1], [Math.PI / 2, 0, 0]));
  if (s.orejas) for (const sx of [-1, 1]) P.push(pieza(CONO(cab * 0.09, cab * 0.4, 3), s.color, [sx * cab * 0.2, yc + cab * 0.3, zc - cab * 0.12], [1, 1, 1], [0, 0, -sx * 0.3]));
  if (s.astas) for (const sx of [-1, 1]) { P.push(pieza(CONO(0.02, 0.35, 3), '#cdb98e', [sx * 0.07, yc + 0.28, zc - 0.05], [1, 1, 1], [0.2, 0, -sx * 0.35])); P.push(pieza(CONO(0.015, 0.2, 3), '#cdb98e', [sx * 0.15, yc + 0.4, zc], [1, 1, 1], [0.5, 0, -sx * 0.5])); }
  if (s.cuernos) for (const sx of [-1, 1]) P.push(pieza(CONO(0.04, 0.34, 4), '#eae2cc', [sx * cab * 0.26, yc + cab * 0.3, zc - cab * 0.05], [1, 1, 1], [0, 0, -sx * 1.1]));
  if (s.parches) for (const [px, py, pz, sx, sy, sz, c] of s.parches) P.push(pieza(ICO0, c || '#1d1d20', [px, py, pz], [sx, sy, sz]));
  if (s.manchas) for (let i = 0; i < 4; i++) P.push(pieza(ICO0, s.manchas, [(i % 2 ? 1 : -1) * gr * 0.8, base + A * (0.45 + (i % 3) * 0.12), (i - 1.5) * L * 0.2], [gr * 0.4, A * 0.22, L * 0.1]));
  if (s.cola) P.push(pieza(CONO(Math.max(0.03, gr * 0.2), s.cola, 4), s.colaColor || s.color, [0, base + A * 0.75, -L * 0.5 - s.cola * 0.28], [1, 1, 1], [-2.0, 0, 0]));
  const g = new THREE.Group(), cuerpo = new THREE.Mesh(ensamblar(P), matAni); cuerpo.castShadow = true; g.add(cuerpo);
  const patas = [], rg = Math.max(0.03, gr * 0.17), alto = base + A * 0.2;
  for (const [sx, sz] of [[-1, 1], [1, -1], [1, 1], [-1, -1]]) {      // las diagonales se mueven juntas
    const p = new THREE.Group(); p.position.set(sx * gr * 0.62, alto, sz * L * 0.3);
    const m = new THREE.Mesh(new THREE.CylinderGeometry(rg, rg * 0.75, alto, 5).translate(0, -alto / 2, 0), patasMat(s.patas || oscurecer(s.color)));
    m.castShadow = true; p.add(m);
    if (s.pezunas) p.add(new THREE.Mesh(new THREE.CylinderGeometry(rg * 1.15, rg * 1.05, 0.1, 5).translate(0, -alto + 0.05, 0), patasMat(s.pezunas)));
    g.add(p); patas.push(p);
  }
  return { g, patas };
}

// Vaca holando: cuerpo blanco con grandes manchas negras, hocico negro, cuernos claros, patas blancas con pezuñas
function vacaHolando() {
  const j = () => (Math.random() - 0.5) * 0.14, E = Math.random() < 0.5 ? 1 : -1;
  const parches = [
    [0.34 * E + j(), 1.35, -0.15 + j(), 0.14, 0.38, 0.55], [-0.36 * E + j(), 1.3, 0.12 + j(), 0.14, 0.34, 0.42],
    [0.02 + j(), 1.63, -0.35 + j(), 0.34, 0.12, 0.42], [-0.35 * E, 1.2, -0.72 + j(), 0.14, 0.35, 0.3],
    [0.35 * E, 1.25, 0.62 + j(), 0.14, 0.45, 0.17], [0.0, 1.7, 0.45 + j(), 0.25, 0.08, 0.22],
    [0.13 * E, 1.78, 1.16, 0.06, 0.1, 0.15], [-0.14 * E, 1.74, 1.1, 0.05, 0.08, 0.1],
    [0.2 * E, 1.5, 0.9, 0.1, 0.22, 0.2],
  ];
  return cuadrupedo({ L: 2.1, A: 0.9, pat: 0.7, grosor: 0.45, cuello: 0.4, cab: 0.55, cola: 0.7, color: '#f1efe9', claro: '#f1efe9', patas: '#ecebe5', hocico: '#1d1d20', colaColor: '#1d1d20', orejas: true, cuernos: true, pezunas: '#8a8460', parches });
}

// ---------- aves ----------
function ave(s) {
  const base = s.pat, P = [];
  P.push(pieza(ICO, s.color, [0, base + s.A * 0.5, 0], [s.gr, s.A * 0.5, s.L * 0.5]));
  P.push(pieza(ICO, s.claro || s.color, [0, base + s.A * 0.35, s.L * 0.1], [s.gr * 0.85, s.A * 0.32, s.L * 0.4]));
  P.push(pieza(CIL(s.gr * 0.2, s.gr * 0.32, s.cuello, 5), s.color, [0, base + s.A * 0.85 + s.cuello * 0.45, s.L * 0.35], [1, 1, 1], [0.35, 0, 0]));
  const yc = base + s.A * 0.85 + s.cuello * 0.95, zc = s.L * 0.35 + s.cuello * 0.2;
  P.push(pieza(ICO, s.cabColor || s.color, [0, yc, zc], [s.cab * 0.3, s.cab * 0.3, s.cab * 0.45]));
  P.push(pieza(CONO(s.cab * 0.14, s.cab * 0.55, 4), s.pico || '#c8b080', [0, yc - s.cab * 0.04, zc + s.cab * 0.55], [1, 1, 1], [Math.PI / 2, 0, 0]));
  P.push(pieza(CONO(s.gr * 0.5, s.L * 0.5, 4), s.color, [0, base + s.A * 0.6, -s.L * 0.5], [1, 1, 1], [-1.9, 0, 0]));
  const g = new THREE.Group(), cuerpo = new THREE.Mesh(ensamblar(P), matAni); cuerpo.castShadow = true; g.add(cuerpo);
  const patas = [];
  for (const sx of [-1, 1]) { const p = new THREE.Group(); p.position.set(sx * s.gr * 0.35, base, 0); const m = new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.02, base, 4).translate(0, -base / 2, 0), patasMat(s.patas || '#8a7a60')); p.add(m); g.add(p); patas.push(p); }
  return { g, patas };
}

function carancho() {
  const g = new THREE.Group();
  const cuerpo = new THREE.Mesh(ensamblar([pieza(ICO, '#2b2622', [0, 0, 0], [0.16, 0.14, 0.34]), pieza(ICO, '#ebe5d4', [0, -0.02, 0.1], [0.14, 0.1, 0.18]), pieza(ICO, '#171310', [0, 0.06, 0.34], [0.09, 0.09, 0.12]), pieza(CONO(0.04, 0.12, 4), '#d8b84a', [0, 0.03, 0.5], [1, 1, 1], [Math.PI / 2, 0, 0]), pieza(CONO(0.1, 0.3, 4), '#2b2622', [0, 0, -0.38], [1, 1, 1], [-1.6, 0, 0])]), matAni);
  g.add(cuerpo);
  const alas = [];
  for (const sx of [-1, 1]) { const a = new THREE.Group(); a.position.set(sx * 0.12, 0.04, 0.02); const m = new THREE.Mesh(ensamblar([pieza(ICO, '#2b2622', [sx * 0.42, 0, 0], [0.5, 0.025, 0.2])]), matAni); a.add(m); g.add(a); alas.push(a); }
  return { g, alas };
}

function yarara() {
  const P = [];
  for (let i = 0; i < 9; i++) P.push(pieza(ICO0, i % 2 ? '#2e2519' : '#76623f', [Math.sin(i * 0.9) * 0.18, 0.05, -i * 0.13], [0.06 - i * 0.003, 0.05, 0.08]));
  P.push(pieza(ICO0, '#76623f', [Math.sin(-0.9) * 0.18, 0.07, 0.12], [0.07, 0.05, 0.1]));
  const g = new THREE.Group(); const m = new THREE.Mesh(ensamblar(P), matAni); m.castShadow = true; g.add(m); return { g, patas: [] };
}

function peludo() {
  const P = [pieza(ICO, '#a39178', [0, 0.2, 0], [0.22, 0.17, 0.34]), pieza(ICO, '#7d6d58', [0, 0.26, 0.05], [0.2, 0.1, 0.06]), pieza(ICO, '#7d6d58', [0, 0.26, -0.1], [0.2, 0.1, 0.06]),
    pieza(ICO, '#a39178', [0, 0.18, 0.36], [0.1, 0.09, 0.14]), pieza(CONO(0.035, 0.12, 3), '#c4b69c', [0, 0.17, 0.48], [1, 1, 1], [Math.PI / 2, 0, 0]), pieza(CONO(0.05, 0.24, 4), '#8a7a64', [0, 0.14, -0.4], [1, 1, 1], [-1.9, 0, 0])];
  const g = new THREE.Group(), m = new THREE.Mesh(ensamblar(P), matAni); m.castShadow = true; g.add(m);
  const patas = [];
  for (const [sx, sz] of [[-1, 1], [1, -1], [1, 1], [-1, -1]]) { const p = new THREE.Group(); p.position.set(sx * 0.13, 0.12, sz * 0.18); p.add(new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.02, 0.12, 4).translate(0, -0.06, 0), patasMat('#6e604c'))); g.add(p); patas.push(p); }
  return { g, patas };
}

// ---------- especies: modelo + comportamiento ----------
const ESP = {
  venado:   { m: () => cuadrupedo({ L: 1.2, A: 0.55, pat: 0.6, grosor: 0.26, cuello: 0.55, cab: 0.3, cola: 0.14, color: '#b9803f', claro: '#efe3cf', orejas: true, astas: true }), vel: 1.4, huida: 14, velH: 8.5, radio: 40, huye: true },
  zorro:    { m: () => cuadrupedo({ L: 0.75, A: 0.26, pat: 0.26, grosor: 0.13, cuello: 0.12, cab: 0.24, cola: 0.5, color: '#8c8a85', claro: '#d8d4cc', orejas: true, colaColor: '#6e6b66' }), vel: 1.6, huida: 14, velH: 7.5, radio: 55, huye: true },
  vizcacha: { m: () => cuadrupedo({ L: 0.55, A: 0.36, pat: 0.14, grosor: 0.2, cab: 0.26, cola: 0.18, color: '#8a7d68', claro: '#cfc7b6' }), vel: 0.7, huida: 9, velH: 5, radio: 8, huye: true },
  carpincho:{ m: () => cuadrupedo({ L: 1.1, A: 0.5, pat: 0.2, grosor: 0.3, cab: 0.38, color: '#6b4a30', claro: '#8a6445', orejas: true }), vel: 0.9, huida: 10, velH: 5, radio: 24, huye: true },
  coipo:    { m: () => cuadrupedo({ L: 0.62, A: 0.3, pat: 0.1, grosor: 0.17, cab: 0.24, cola: 0.5, color: '#6a4a32', claro: '#8c6a4c', orejas: true }), vel: 0.8, huida: 9, velH: 5, radio: 16, huye: true },
  puma:     { m: () => cuadrupedo({ L: 1.45, A: 0.6, pat: 0.46, grosor: 0.22, cuello: 0.3, cab: 0.34, cola: 1.0, color: '#c8a060', claro: '#eadcc0', orejas: true }), vel: 0.9, huida: 8, velH: 6, radio: 20, huye: false },
  vaca:     { m: () => vacaHolando(), vel: 0.6, huida: 5, velH: 3, radio: 30, huye: true },
  nandu:    { m: () => ave({ L: 0.95, A: 0.55, pat: 0.9, gr: 0.28, cuello: 0.85, cab: 0.22, color: '#9a8b78', claro: '#cfc3b0', pico: '#6a5a48', patas: '#7a6c58' }), vel: 1.4, huida: 16, velH: 10, radio: 60, huye: true },
  perdiz:   { m: () => ave({ L: 0.36, A: 0.2, pat: 0.14, gr: 0.11, cuello: 0.1, cab: 0.1, color: '#b07a3d', claro: '#e4c89a', pico: '#8a6a3a' }), vel: 1.1, huida: 8, velH: 6, radio: 20, huye: true },
  peludo:   { m: () => peludo(), vel: 0.8, huida: 7, velH: 4.5, radio: 20, huye: true },
  yarara:   { m: () => yarara(), vel: 0, huida: 0, velH: 0, radio: 0, huye: false },
};

export async function construirFauna({ scene, campo }) {
  const { altura } = campo;
  const animales = [], carretas = [], vuelos = [], idleCaballos = [];
  const grupo = new THREE.Group(); grupo.userData.sinXray = true; grupo.userData.vivo = true; scene.add(grupo);   // animales, jinetes y corral nunca se recortan
  const enMapa = (x, z) => Math.abs(x) < 1560 && Math.abs(z) < 1160;

  const RCOL = { vaca: 0.95, venado: 0.55, nandu: 0.5, carpincho: 0.5, puma: 0.5 };       // radio del collider de cada especie grande
  const poner = (esp, x, z, o = {}) => {
    const sp = ESP[esp], m = sp.m(), a = { esp, sp, g: m.g, patas: m.patas, hx: x, hz: z, estado: 'pastar', t: hs(x * 3.1 + z) * 4, ang: hs(x + z * 2.3) * 6.28, fase: hs(x) * 6, deriva: (hs(z) - 0.5) * 1.2, tx: x, tz: z, escala: o.escala || 1 };
    if (RCOL[esp]) { a.q = { x, z, r: RCOL[esp], activo: true }; personas.push(a.q); }
    m.g.position.set(x, altura(x, z), z); m.g.rotation.y = a.ang; m.g.scale.setScalar(a.escala); grupo.add(m.g); animales.push(a); return a;
  };
  let sem = 1;
  const rnd = () => hs(sem++);
  const elipse = (c, rx, ry) => () => { const t = rnd() * 6.28, r = Math.sqrt(rnd()); const [mx, mz] = K(...c); return [mx + Math.cos(t) * rx * 1000 * r, mz + Math.sin(t) * ry * 1000 * r]; };
  const rectKm = (x0, y0, x1, y1) => () => K(mix(x0, x1, rnd()), mix(y0, y1, rnd()));
  const sembrar = (esp, grupos, tam, region, escala = 1) => {
    for (let gi = 0; gi < grupos; gi++) {
      let c = null; for (let k = 0; k < 40 && !c; k++) { const [x, z] = region(); if (enMapa(x, z) && libre(x, z)) c = [x, z]; }
      if (!c) continue;
      for (let i = 0; i < tam; i++) { let x = c[0] + (rnd() - 0.5) * 12, z = c[1] + (rnd() - 0.5) * 12; if (!libre(x, z)) { x = c[0]; z = c[1]; } poner(esp, x, z, { escala: escala * mix(0.92, 1.08, rnd()) }); }
    }
  };

  // pajonal y campo abierto
  const PAJONAL = rectKm(0.0, 1.55, 1.5, 2.4), ABIERTO = rectKm(0.0, 0.0, 3.2, 2.4);
  sembrar('nandu', 3, 3, PAJONAL); sembrar('nandu', 1, 3, ABIERTO);
  sembrar('venado', 3, 2, PAJONAL); sembrar('venado', 1, 3, ABIERTO);
  sembrar('peludo', 6, 1, PAJONAL); sembrar('zorro', 4, 1, PAJONAL); sembrar('zorro', 2, 1, elipse(MONTE.c, MONTE.rx, MONTE.ry));
  sembrar('perdiz', 5, 3, PAJONAL); sembrar('perdiz', 2, 3, ABIERTO);
  sembrar('yarara', 6, 1, PAJONAL);
  sembrar('puma', 2, 1, elipse(MONTE.c, MONTE.rx * 1.05, MONTE.ry * 1.05));
  // arroyo y laguna
  const ORILLA = () => { const t = rnd() * 6.28, r = 1.15 + rnd() * 0.5; const [mx, mz] = K(...LAGUNA.c); return [mx + Math.cos(t) * LAGUNA.rx * 1000 * r, mz + Math.sin(t) * LAGUNA.ry * 1000 * r]; };
  sembrar('carpincho', 3, 3, ORILLA); sembrar('coipo', 3, 2, ORILLA);
  // vizcacheras: montículo con madrigueras y vizcachas alrededor
  const vizcacheras = [];
  for (const [kx, ky] of [[0.9, 2.1], [0.4, 1.7], [1.25, 1.75]]) {
    const [x, z] = K(kx, ky); vizcacheras.push([x, z]);
    const m = new THREE.Mesh(ensamblar([pieza(ICO, '#8d7a5e', [0, 0.25, 0], [3.2, 0.5, 2.6]), pieza(CIL(0.35, 0.45, 0.3, 6), '#2a2118', [1.2, 0.45, 0.8]), pieza(CIL(0.3, 0.4, 0.3, 6), '#2a2118', [-1.0, 0.4, 0.5]), pieza(CIL(0.3, 0.4, 0.3, 6), '#2a2118', [0.1, 0.5, -0.9]), pieza(CONO(0.05, 1.0, 3), '#6e5a3e', [-1.8, 0.4, -0.4], [1, 1, 1], [0, 0, 0.8]), pieza(CONO(0.05, 0.9, 3), '#6e5a3e', [1.9, 0.4, -0.3], [1, 1, 1], [0, 0, -0.9])]), matAni);
    m.position.set(x, altura(x, z), z); m.castShadow = true; m.userData.noXray = true; grupo.add(m);
    for (let i = 0; i < 4; i++) poner('vizcacha', x + (rnd() - 0.5) * 8, z + (rnd() - 0.5) * 8, { escala: 1 });
  }
  // yarará cerca de ranchos
  for (const [kx, ky] of [[0.62, 0.86], [1.0, 1.04], [2.2, 1.3]]) { const [x, z] = K(kx, ky); poner('yarara', x + 9, z + 7); }

  // vida cerca del punto de partida (rancho de peones): que desde el primer minuto se vea algo moverse
  const [sx0, sz0] = K(0.59, 0.9);
  const anillo = (r0, r1) => () => { const t = rnd() * 6.28, r = mix(r0, r1, rnd()); return [sx0 + Math.cos(t) * r, sz0 + Math.sin(t) * r]; };
  sembrar('perdiz', 3, 3, anillo(25, 60)); sembrar('vaca', 2, 3, anillo(45, 90)); sembrar('nandu', 1, 2, anillo(80, 120));
  sembrar('venado', 1, 2, anillo(100, 140)); sembrar('vizcacha', 1, 2, anillo(55, 85)); sembrar('zorro', 1, 1, anillo(90, 130));

  // ganado y caballos en los potreros, corrales y la posta
  const [p0x, p0z] = K(0.17, 0.08), [p1x, p1z] = K(0.86, 0.42);
  const enPotrero = () => [mix(p0x + 12, p1x - 12, rnd()), mix(p0z + 12, p1z - 12, rnd())];
  sembrar('vaca', 5, 3, enPotrero);
  const fabr = fabricaCaballos(), { caballo } = fabr;
  const colores = ['#7a4a2a', '#2a2018', '#a8754a', '#5a3a24', '#c9a779', '#8a5a34'];
  const caballos = [];
  const corralZ = (cx, cz, w, l, lado) => ({ x0: cx - w / 2 + 1.8, x1: cx + w / 2 - 1.8, z0: cz - l / 2 + 1.8, z1: cz + l / 2 - 1.8, puerta: [cx, cz + lado * l / 2], adentro: [cx, cz + lado * (l / 2 - 3.5)], afuera: [cx, cz + lado * (l / 2 + 7)] });
  const pon = (x, z, rot, i, calc, o = {}) => {
    const c = caballo(colores[i % colores.length], calc ?? i % 2 === 0); c.g.position.set(x, altura(x, z), z); c.g.rotation.y = rot; c.g.scale.setScalar(0.95); grupo.add(c.g);
    const q = { x, z, r: 0.85, activo: true }; personas.push(q);
    const h = { c, g: c.g, q, hx: x, hz: z, ang: rot, tipo: o.tipo || 'libre', zona: o.zona || null, estado: 'pastar', t: rnd() * 6, fase: rnd() * 20, pasto: 0, marcha: 0, ruta: null, fuera: false, volviendo: false, salidaT: 0, radio: 14 };
    caballos.push(h); return h;
  };
  const [ex, ez] = K(0.75, 0.75), [qx, qz] = K(2.15, 1.25);
  for (let i = 0; i < 6; i++) { const [x, z] = enPotrero(); if (libre(x, z)) pon(x, z, rnd() * 6.28, i); }
  const zDoma = corralZ(ex - 26, ez + 22, 18, 18, -1), zGrande = corralZ(ex + 12, ez + 34, 32, 22, -1), zRemuda = corralZ(qx - 6, qz - 40, 26, 16, 1);
  [[ex - 26, ez + 22], [ex - 22, ez + 18]].forEach(([x, z], i) => pon(x, z, 1 + i, i, undefined, { tipo: 'corral', zona: zDoma }));
  [[ex + 8, ez + 34], [ex + 16, ez + 32], [ex + 12, ez + 38]].forEach(([x, z], i) => pon(x, z, 2 + i, i + 2, undefined, { tipo: 'corral', zona: zGrande }));
  [[qx - 10, qz - 40], [qx - 3, qz - 38], [qx - 8, qz - 44], [qx - 1, qz - 44]].forEach(([x, z], i) => pon(x, z, 0.5 + i, i + 1, undefined, { tipo: 'corral', zona: zRemuda }));
  [-2.7, 0, 2.7].forEach((dx, i) => pon(qx - 4 + dx, qz + 8, Math.PI, i + 3, true, { tipo: 'atado' }));      // atados al palenque de la posta

  // carretas de bueyes que recorren el camino real
  const ruta = suavizar(CAMINO, 40).map(([kx, ky]) => K(kx, ky));
  const acum = [0]; for (let i = 1; i < ruta.length; i++) acum.push(acum[i - 1] + Math.hypot(ruta[i][0] - ruta[i - 1][0], ruta[i][1] - ruta[i - 1][1]));
  const largoRuta = acum[acum.length - 1];
  const sobreRuta = (s) => { let i = 1; while (i < acum.length - 1 && acum[i] < s) i++; const t = (s - acum[i - 1]) / (acum[i] - acum[i - 1] || 1); return { x: mix(ruta[i - 1][0], ruta[i][0], t), z: mix(ruta[i - 1][1], ruta[i][1], t), ang: Math.atan2(ruta[i][0] - ruta[i - 1][0], ruta[i][1] - ruta[i - 1][1]) }; };
  const matMad = new THREE.MeshStandardMaterial({ color: '#8a5a34', flatShading: true, roughness: 1 }), matLona = new THREE.MeshStandardMaterial({ color: '#d9c58a', flatShading: true, roughness: 1, side: THREE.DoubleSide });
  const construirCarreta = () => {
    const g = new THREE.Group(), ruedas = [];
    const caja = (w, h, l, m, x, y, z) => { const b = new THREE.Mesh(new THREE.BoxGeometry(w, h, l), m); b.position.set(x, y, z); b.castShadow = true; g.add(b); return b; };
    caja(2.0, 0.18, 4.8, matMad, 0, 1.55, 0);
    for (const sx of [-1, 1]) caja(0.1, 0.5, 4.8, matMad, sx * 1.0, 1.85, 0);
    const arco = new THREE.Mesh(new THREE.CylinderGeometry(1.1, 1.1, 4.4, 8, 1, true, 0, Math.PI).rotateX(Math.PI / 2).rotateZ(Math.PI / 2), matLona); arco.position.set(0, 1.64, 0); arco.castShadow = true; g.add(arco);
    for (const sx of [-1, 1]) {
      const r = new THREE.Group(); r.position.set(sx * 1.2, 1.25, 0.2);
      const aro = new THREE.Mesh(new THREE.CylinderGeometry(1.25, 1.25, 0.16, 12), matMad); aro.rotation.z = Math.PI / 2; aro.castShadow = true; r.add(aro);
      for (let k = 0; k < 4; k++) { const rayo = new THREE.Mesh(new THREE.BoxGeometry(0.08, 2.4, 0.1), matMad); rayo.rotation.x = k * Math.PI / 4; r.add(rayo); }
      g.add(r); ruedas.push(r);
    }
    caja(0.14, 0.14, 4.6, matMad, 0, 1.0, 3.8);                                 // lanza
    caja(2.0, 0.12, 0.12, matMad, 0, 1.55, 6.0);                                // yugo
    const bueyes = [];
    for (const sx of [-1, 1]) { const b = cuadrupedo({ L: 2.0, A: 0.88, pat: 0.7, grosor: 0.42, cuello: 0.4, cab: 0.55, cola: 0.7, color: sx < 0 ? '#a65b32' : '#7a4a2a', claro: '#d8c6a4', orejas: true, cuernos: true }); b.g.position.set(sx * 0.6, 0, 7.3); g.add(b.g); bueyes.push(b); }
    return { g, ruedas, bueyes };
  };
  [[0.15, 1], [0.55, -1], [0.85, 1]].forEach(([f, dir], i) => {
    const c = construirCarreta(); grupo.add(c.g);
    carretas.push({ ...c, s: largoRuta * f, dir, fase: 0, personas: [{ x: 0, z: 0, r: 2.3 }, { x: 0, z: 0, r: 2.3 }, { x: 0, z: 0, r: 1.6 }] });
    carretas[i].personas.forEach((p) => personas.push(p));
  });

  // carancho: vuelan en círculos sobre el pajonal y el monte
  [[0.7, 2.0, 70], [2.3, 0.5, 55], [1.7, 1.6, 90], [0.4, 0.9, 60]].forEach(([kx, ky, r], i) => { const a = carancho(); grupo.add(a.g); vuelos.push({ ...a, c: K(kx, ky), r, f: i * 1.7, h: 28 + i * 6 }); });

  const bailar = (a, mover, dt) => {
    const w = mover > 0 ? Math.sin(a.fase += dt * (mover * 2.4 + 3)) * (mover > 3 ? 0.9 : 0.5) : 0;
    if (a.patas.length === 4) { a.patas[0].rotation.x = w; a.patas[1].rotation.x = w; a.patas[2].rotation.x = -w; a.patas[3].rotation.x = -w; }
    else if (a.patas.length === 2) { a.patas[0].rotation.x = w; a.patas[1].rotation.x = -w; }
  };

  // ---------- minijuego: arreo de vacas a un corral con tranquera ----------
  // Las vacas se alejan de vos: ponete del lado contrario al corral y empujalas hasta la puerta; después cerrá la tranquera (E).
  const CW = 24, CL = 20, GAP = 5;                                  // corral: ancho (x), largo (z), hueco de la tranquera en el lado sur
  const rodeo = (() => {
    const okRing = (x, z) => libre(x, z) && [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11].every((k) => libre(x + Math.cos(k * 0.5236) * 17, z + Math.sin(k * 0.5236) * 17));
    for (let r = 0; r < 90; r += 8) for (let k = 0; k < 12; k++) { const x = ex - 20 + Math.cos(k * 0.52) * r, z = ez + 80 + Math.sin(k * 0.52) * r; if (okRing(x, z)) return [x, z]; }
    return [ex - 20, ez + 80];
  })();
  const dentroCorral = (x, z) => Math.abs(x - rodeo[0]) < CW / 2 - 0.6 && Math.abs(z - rodeo[1]) < CL / 2 - 0.6;
  const tranq = { x: rodeo[0], z: rodeo[1] + CL / 2, abierta: true, ang: Math.PI / 2, alternar: null, panel: null, pers: [] };
  {
    const g = new THREE.Group(), matP = patasMat('#6a4a2a'), cx = rodeo[0], cz = rodeo[1], hw = CW / 2, hl = CL / 2;
    // piso de barro que sigue el terreno
    const SX = 16, SZ = 14, pos = [], idx = [];
    for (let j = 0; j <= SZ; j++) for (let i = 0; i <= SX; i++) { const x = cx - hw + CW * i / SX, z = cz - hl + CL * j / SZ; pos.push(x, altura(x, z) + 0.07, z); }
    for (let j = 0; j < SZ; j++) for (let i = 0; i < SX; i++) { const a = j * (SX + 1) + i; idx.push(a, a + SX + 1, a + 1, a + 1, a + SX + 1, a + SX + 2); }
    const gb = new THREE.BufferGeometry(); gb.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); gb.setIndex(idx); gb.computeVertexNormals();
    const barro = new THREE.Mesh(gb, new THREE.MeshStandardMaterial({ color: '#4a3626', flatShading: true, roughness: 1 })); barro.receiveShadow = true; g.add(barro);
    for (let i = 0; i < 7; i++) {                                    // charcos y manchas más oscuras
      const x = cx + (Math.random() - 0.5) * (CW - 5), z = cz + (Math.random() - 0.5) * (CL - 5);
      const ch = new THREE.Mesh(new THREE.CircleGeometry(0.8 + Math.random() * 1.4, 8).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: i % 2 ? '#2d211a' : '#5b4636', flatShading: true, roughness: i % 2 ? 0.35 : 1 }));
      ch.position.set(x, altura(x, z) + 0.1, z); ch.receiveShadow = true; g.add(ch);
    }
    // cerco: postes + dos rieles por tramo; el lado sur deja el hueco de la tranquera
    const poste = (x, z) => { const m = new THREE.Mesh(new THREE.CylinderGeometry(0.13, 0.16, 1.6, 5).translate(0, 0.8, 0), matP); m.position.set(x, altura(x, z), z); m.castShadow = true; g.add(m); };
    const riel = (x0, z0, x1, z1) => {
      const L = Math.hypot(x1 - x0, z1 - z0), a = Math.atan2(x1 - x0, z1 - z0);
      for (const y of [0.55, 1.15]) { const m = new THREE.Mesh(new THREE.BoxGeometry(0.1, 0.12, L), matP); m.position.set((x0 + x1) / 2, altura((x0 + x1) / 2, (z0 + z1) / 2) + y, (z0 + z1) / 2); m.rotation.y = a; m.castShadow = true; g.add(m); }
      const mx = (x0 + x1) / 2, mz = (z0 + z1) / 2, cs = Math.cos(a), sn = Math.sin(a), w = 0.5, hL = L / 2;
      registrar([[mx - cs * w / 2 - sn * hL, mz + sn * w / 2 - cs * hL], [mx + cs * w / 2 - sn * hL, mz - sn * w / 2 - cs * hL], [mx + cs * w / 2 + sn * hL, mz - sn * w / 2 + cs * hL], [mx - cs * w / 2 + sn * hL, mz + sn * w / 2 + cs * hL]]);
    };
    const lado = (x0, z0, x1, z1) => { const L = Math.hypot(x1 - x0, z1 - z0), n = Math.max(1, Math.round(L / 3)); for (let k = 0; k <= n; k++) poste(mix(x0, x1, k / n), mix(z0, z1, k / n)); riel(x0, z0, x1, z1); };
    lado(cx - hw, cz - hl, cx + hw, cz - hl); lado(cx + hw, cz - hl, cx + hw, cz + hl); lado(cx - hw, cz - hl, cx - hw, cz + hl);
    lado(cx - hw, cz + hl, cx - GAP / 2, cz + hl); lado(cx + GAP / 2, cz + hl, cx + hw, cz + hl);
    // tranquera: un panel de 5 m que gira en el poste este del hueco
    const panel = new THREE.Group(); panel.position.set(cx + GAP / 2, altura(cx, cz + hl), cz + hl);
    for (const y of [0.35, 0.75, 1.15]) { const m = new THREE.Mesh(new THREE.BoxGeometry(GAP, 0.11, 0.09).translate(-GAP / 2, y, 0), patasMat('#8a5a34')); m.castShadow = true; panel.add(m); }
    for (const sx of [-0.1, -GAP + 0.1]) { const m = new THREE.Mesh(new THREE.BoxGeometry(0.1, 1.3, 0.1).translate(sx, 0.65, 0), patasMat('#8a5a34')); m.castShadow = true; panel.add(m); }
    const diag = new THREE.Mesh(new THREE.BoxGeometry(Math.hypot(GAP, 0.9), 0.08, 0.07), patasMat('#8a5a34')); diag.position.set(-GAP / 2, 0.75, 0); diag.rotation.z = Math.atan2(0.8, GAP); panel.add(diag);
    panel.rotation.y = tranq.ang; g.add(panel); tranq.panel = panel;
    // cartel alto para ubicar el corral desde lejos
    const mast = new THREE.Mesh(new THREE.CylinderGeometry(0.08, 0.1, 7, 5).translate(0, 3.5, 0), matP); mast.position.set(cx - hw, altura(cx - hw, cz - hl), cz - hl); g.add(mast);
    const bandera = new THREE.Mesh(new THREE.BoxGeometry(1.6, 1.0, 0.05).translate(0.8, 0, 0), new THREE.MeshStandardMaterial({ color: '#c72d11', flatShading: true, roughness: 1 })); bandera.position.set(cx - hw, altura(cx - hw, cz - hl) + 6.3, cz - hl); g.add(bandera);
    g.traverse((o) => { o.userData.noXray = true; }); g.userData.sucioForzado = true; grupo.add(g);
    for (const dx of [-2, 0, 2]) { const q = { x: cx + dx, z: cz + hl, r: 1.2, activo: false }; personas.push(q); tranq.pers.push(q); }
  }
  const arreo = { activo: false, t: 0, vacas: [], total: 5, dentro: 0, nivel: () => 1, hud: null, onFin: null, rodeo, tranquera: tranq, iniciar: null };
  const contarDentro = () => arreo.vacas.filter((a) => dentroCorral(a.g.position.x, a.g.position.z)).length;
  const poner_tranq = (abierta) => { tranq.abierta = abierta; tranq.pers.forEach((q) => { q.activo = !abierta; }); };
  const finalizar = (ok, dentro) => {
    arreo.activo = false;
    const niv = arreo.nivel(), pago = ok ? 8 + niv + Math.floor(Math.max(0, arreo.t) / 15) : dentro * 3, xp = ok ? 4 : dentro * 0.8;
    arreo.vacas.forEach((a) => { a.arreo = false; a.corral = dentroCorral(a.g.position.x, a.g.position.z); a.salio = false; a.ruta = null; a.hx = a.g.position.x; a.hz = a.g.position.z; a.estado = 'pastar'; a.t = 2; });
    if (arreo.onFin) arreo.onFin({ ok, dentro, pago, xp, resto: Math.ceil(Math.max(0, arreo.t)) });
  };
  tranq.alternar = () => {
    if (tranq.abierta) {
      if (arreo.activo) {
        const n = contarDentro();
        if (n < arreo.total) { decir(`Todavía faltan ${arreo.total - n} vacas adentro del corral.`); return; }
        poner_tranq(false); decir('Tranquera cerrada.'); finalizar(true, n); return;
      }
      poner_tranq(false); decir('Tranquera cerrada.');
    } else { poner_tranq(true); decir('Tranquera abierta.'); }
  };
  arreo.iniciar = () => {
    if (arreo.activo) return;
    poner_tranq(true);
    const th0 = 0.3 + Math.random() * 2.4;
    for (let i = 0; i < arreo.total; i++) {
      let x = rodeo[0] + 55, z = rodeo[1];
      for (let k = 0; k < 60; k++) { const th = th0 + (Math.random() - 0.5) * 1.3, r = 27 + Math.random() * 10, cx = rodeo[0] + Math.cos(th) * r, cz = rodeo[1] + Math.sin(th) * r; if (libre(cx, cz) && enMapa(cx, cz)) { x = cx; z = cz; break; } }
      let a = arreo.vacas[i];
      if (!a) { a = poner('vaca', x, z); arreo.vacas.push(a); } else { a.g.position.set(x, altura(x, z), z); }
      a.arreo = true; a.corral = false; a.salio = false; a.ruta = null; a.hx = x; a.hz = z; a.deriva = (Math.random() - 0.5) * 0.9; a.g.visible = true;
    }
    arreo.activo = true; arreo.t = 150; arreo.dentro = 0; arreo.hudT = 0;
  };
  const arrear = (dt, p) => {
    // la tranquera gira suave hacia su posición
    const obj = tranq.abierta ? Math.PI / 2 : 0; tranq.ang += (obj - tranq.ang) * Math.min(1, dt * 4); tranq.panel.rotation.y = tranq.ang;
    if (!arreo.activo) return;
    arreo.t -= dt; const radio = Math.max(7, 13 - (arreo.nivel() - 1) * 0.8);
    for (const a of arreo.vacas) {
      const g = a.g, x = g.position.x, z = g.position.z, dx = x - p.x, dz = z - p.z, d = Math.hypot(dx, dz);
      let mover = 0;
      if (d < radio) {
        const ang = Math.atan2(dx, dz) + a.deriva * 0.5; mover = 2.2 + (1 - d / radio) * 2.6;
        const sx = Math.sin(ang) * mover * dt, sz = Math.cos(ang) * mover * dt, ok = (nx, nz) => libre(nx, nz) && !choca(nx, nz, 0.7, a.q);
        if (ok(x + sx, z + sz)) { g.position.x += sx; g.position.z += sz; a.ang = ang; }
        else if (ok(x + sx, z)) { g.position.x += sx; a.ang = Math.atan2(sx, 0.001); }
        else if (ok(x, z + sz)) { g.position.z += sz; a.ang = Math.atan2(0.001, sz); }
        else { mover = 0; a.deriva = -a.deriva; }
      }
      let da = a.ang - g.rotation.y; da = Math.atan2(Math.sin(da), Math.cos(da)); g.rotation.y += da * Math.min(1, dt * 6);
      g.position.y = altura(g.position.x, g.position.z); bailar(a, mover, dt);
      if (a.q) { a.q.x = g.position.x; a.q.z = g.position.z; a.q.activo = true; }
    }
    arreo.dentro = contarDentro();
    arreo.hudT -= dt;
    if (arreo.hudT <= 0 && arreo.hud) { arreo.hudT = 0.5; arreo.hud(`Empujá las ${arreo.total} vacas al corral (bandera roja, al sur de la estancia) y cerrá la tranquera con E. Adentro: ${arreo.dentro}/${arreo.total} · Tiempo: ${Math.max(0, Math.ceil(arreo.t))} s`); }
    if (arreo.t <= 0) finalizar(false, arreo.dentro);
  };

  // ---------- director de eventos: siempre pasa algo cerca de donde vas ----------
  const evs = [];
  let prox = 3, vx = 0, vz = 1, px0 = null, pz0 = null, quieto = 0, tTotal = 0;
  const toast = document.createElement('div');
  toast.style.cssText = 'position:fixed;left:50%;bottom:118px;transform:translateX(-50%);padding:8px 16px;background:#f1e6cf;border:1.5px solid #8b6b3f;border-radius:4px;font:italic 15px Georgia,serif;color:#3a2a18;opacity:0;transition:opacity .5s;pointer-events:none;z-index:20;max-width:70vw;text-align:center';
  document.body.appendChild(toast);
  let toastT = 0;
  const decir = (txt) => { toast.textContent = txt; toast.style.opacity = '1'; toastT = 4.5; };
  const alaSimple = () => { const a = new THREE.Group(); const m = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.02, 0.2).translate(0.25, 0, 0), patasMat('#8a6a3a')); a.add(m); return a; };
  const borrarQ = (q) => { const i = personas.indexOf(q); if (i >= 0) personas.splice(i, 1); };
  const quitar = (e) => { if (e.q) borrarQ(e.q); if (e.items) e.items.forEach((it) => it.q && borrarQ(it.q)); grupo.remove(e.g); e.g.traverse((o) => { if (o.geometry) o.geometry.dispose(); }); };
  const aleat = (a) => a[Math.floor(Math.random() * a.length)];
  const FRASES = ['¡Ave María Purísima!', '¡Buenas, paisano! ¡Que Dios lo guarde!', '¡Ey, amigo! ¡Cuidado con los pumas del monte!', '¡Voy con apuro, que hay novedades en la frontera!', '¡Buen día! ¿Anda por trabajo?', '¡Pa\' la posta, che, que se viene el temporal!'];
  const nuevoGrupoCruce = (p, esp, cant, sep) => {
    const dist = 55 + Math.random() * 25, lado = Math.random() < 0.5 ? -1 : 1, nx = -vz * lado, nz = vx * lado, vel = ESP[esp].velH * (esp === 'nandu' ? 1 : 0.9);
    const cx = p.x + vx * dist, cz = p.z + vz * dist, g = new THREE.Group(), items = [];
    for (let i = 0; i < cant; i++) { const m = ESP[esp].m(); g.add(m.g); items.push({ m, off: i * sep + Math.random() * 1.5, lat: (Math.random() - 0.5) * 5 }); }
    grupo.add(g);
    evs.push({ tipo: 'cruce', g, items, sx: cx - nx * 50, sz: cz - nz * 50, dx: nx, dz: nz, vel, t: 0, largo: 100, esp });
  };
  const nuevaTropilla = (p, cant) => {
    const dist = 60 + Math.random() * 25, lado = Math.random() < 0.5 ? -1 : 1, nx = -vz * lado, nz = vx * lado, vel = 11;
    const cx = p.x + vx * dist, cz = p.z + vz * dist, g = new THREE.Group(), items = [];
    for (let i = 0; i < cant; i++) { const m = fabr.caballo(aleat(['#7a4a2a', '#2a2018', '#a8754a', '#c9a779', '#5a3a24']), Math.random() < 0.5); m.g.scale.setScalar(0.95); g.add(m.g); const q = { x: 0, z: 0, r: 0.85, activo: false }; personas.push(q); items.push({ m, off: i * 4.5 + Math.random() * 2, lat: (Math.random() - 0.5) * 6, caballo: true, q }); }
    grupo.add(g);
    evs.push({ tipo: 'cruce', g, items, sx: cx - nx * 60, sz: cz - nz * 60, dx: nx, dz: nz, vel, t: 0, largo: 125, esp: 'caballo' });
  };
  const nuevoJinete = (p) => {                                       // por el camino real, pasa a galope
    let bi = 0, bd = 1e9; ruta.forEach(([x, z], i) => { const d = Math.hypot(x - p.x, z - p.z); if (d < bd) { bd = d; bi = i; } });
    if (bd > 130) return false;
    const sg = Math.random() < 0.5 ? 1 : -1, s0 = Math.max(12, Math.min(largoRuta - 12, acum[bi] - sg * 95));
    const c = fabr.caballo(aleat(['#7a4a2a', '#2a2018', '#8a5a34', '#c9a779']), Math.random() < 0.5); c.g.scale.setScalar(0.95);
    const r = crearPersonaje({ estilo: 'poncho', poncho: aleat(['#7a3a2a', '#3a4a6a', '#4a5a3a', '#8a6a3a']), sombrero: 'ala', colorSombrero: '#3a2a20', bigote: 'manubrio', pelo: '#2a1c14', piel: '#c99a74' });
    r.userData.montar(); r.position.set(0, 0.74, 0); r.userData.sinXray = true; c.g.add(r);
    const g = new THREE.Group(); g.add(c.g); grupo.add(g);
    const q = { x: 0, z: 0, r: 0.9, activo: false }; personas.push(q);
    evs.push({ tipo: 'ruta', g, c, s: s0, sg, vel: 9.5, t: 0, dijo: false, lat: 0, q });
    return true;
  };
  const nuevaBandada = (p) => {
    const dist = 28 + Math.random() * 12, cx = p.x + vx * dist, cz = p.z + vz * dist, g = new THREE.Group(), items = [];
    if (!libre(cx, cz)) return false;
    for (let i = 0; i < 7; i++) {
      const m = ESP.perdiz.m(); m.g.scale.setScalar(1.3); const a = new THREE.Group(), al = [alaSimple(), alaSimple()]; al[1].scale.x = -1; al.forEach((w) => { w.position.y = 0.22; a.add(w); }); m.g.add(a); m.patas.forEach((l) => { l.rotation.x = -1.2; });
      const ang = Math.atan2(vx, vz) + (Math.random() - 0.5) * 2.6;
      items.push({ m, alas: al, ox: (Math.random() - 0.5) * 4, oz: (Math.random() - 0.5) * 4, dx: Math.sin(ang), dz: Math.cos(ang), v: 6 + Math.random() * 4, h: 3 + Math.random() * 4, f: Math.random() * 6, ret: Math.random() * 0.5 });
      g.add(m.g);
    }
    grupo.add(g); evs.push({ tipo: 'bandada', g, items, cx, cz, t: 0 });
    return true;
  };
  const lanzar = (p) => {
    const op = Math.random();
    if (op < 0.2) nuevoGrupoCruce(p, 'nandu', 2 + Math.floor(Math.random() * 2), 2.4);
    else if (op < 0.38) nuevoGrupoCruce(p, 'venado', 2 + Math.floor(Math.random() * 3), 3.2);
    else if (op < 0.46) nuevoGrupoCruce(p, 'zorro', 1, 2);
    else if (op < 0.6) { if (!nuevaBandada(p)) nuevoGrupoCruce(p, 'perdiz', 4, 1.4); }
    else if (op < 0.8) { if (!nuevoJinete(p)) nuevaTropilla(p, 3); }
    else nuevaTropilla(p, 4 + Math.floor(Math.random() * 2));
  };
  const director = (dt, p) => {
    tTotal += dt;
    if (px0 !== null) { const dx = p.x - px0, dz = p.z - pz0, v = Math.hypot(dx, dz) / Math.max(dt, 1e-3); if (v > 0.6) { vx = mix(vx, dx / (v * dt), 0.15); vz = mix(vz, dz / (v * dt), 0.15); const l = Math.hypot(vx, vz) || 1; vx /= l; vz /= l; quieto = 0; } else quieto += dt; }
    px0 = p.x; pz0 = p.z;
    prox -= dt;
    if (prox <= 0) { prox = 7 + Math.random() * 7; if (evs.length < 3) lanzar(p); }
    if (toastT > 0) { toastT -= dt; if (toastT <= 0) toast.style.opacity = '0'; }
    for (let k = evs.length - 1; k >= 0; k--) {
      const e = evs[k]; e.t += dt;
      let fin = false;
      if (e.tipo === 'cruce') {
        const rec = e.vel * e.t; e.g.visible = true;
        for (const it of e.items) {
          const q = rec - it.off, x = e.sx + e.dx * q + (-e.dz) * it.lat, z = e.sz + e.dz * q + e.dx * it.lat;
          it.m.g.position.set(x, altura(x, z) + (it.caballo ? Math.abs(Math.sin(e.t * 9 + it.off)) * 0.12 : 0), z); it.m.g.rotation.y = Math.atan2(e.dx, e.dz);
          if (it.q) { it.q.x = x; it.q.z = z; it.q.activo = Math.hypot(x - p.x, z - p.z) > it.q.r + 0.7; }
          if (it.caballo) { it.m.andar(e.t * 10 + it.off, 0.9); it.m.animar(e.t * 2.2 + it.off, 0, 1); } else bailar({ patas: it.m.patas, fase: e.t * 8 + it.off }, 6, 0);
        }
        if (rec > e.largo + e.items.length * 6) fin = true;
      } else if (e.tipo === 'ruta') {
        e.s += e.sg * e.vel * dt;
        if (e.s < 8 || e.s > largoRuta - 8) fin = true;
        else {
          const pos = sobreRuta(e.s), ang = pos.ang + (e.sg < 0 ? Math.PI : 0);
          const rx0 = Math.cos(ang), rz0 = -Math.sin(ang), qx_ = p.x - pos.x, qz_ = p.z - pos.z, along = qx_ * Math.sin(ang) + qz_ * Math.cos(ang), side = qx_ * rx0 + qz_ * rz0;
          const objL = along > -1 && along < 16 && Math.abs(side - e.lat) < 2.6 ? (side - e.lat > 0 ? -3.2 : 3.2) : 0;           // esquiva al jugador
          e.lat += (objL - e.lat) * Math.min(1, dt * 2.5);
          const hx = pos.x + rx0 * e.lat, hz = pos.z + rz0 * e.lat;
          e.g.position.set(hx, altura(hx, hz) + Math.abs(Math.sin(e.t * 8)) * 0.1, hz); e.g.rotation.y = ang; e.c.andar(e.t * 10, 0.9); e.c.animar(e.t * 2.6, 0, 1);
          e.q.x = hx; e.q.z = hz; e.q.activo = Math.hypot(hx - p.x, hz - p.z) > e.q.r + 0.7;
          const d = Math.hypot(pos.x - p.x, pos.z - p.z);
          if (!e.dijo && d < 22) { e.dijo = true; decir(aleat(FRASES)); }
          if (e.t > 6 && d > 170) fin = true;
        }
      } else if (e.tipo === 'bandada') {
        const dur = 4.2;
        for (const it of e.items) {
          const q = Math.max(0, e.t - it.ret), u = Math.min(1, q / dur), x = e.cx + it.ox + it.dx * it.v * q, z = e.cz + it.oz + it.dz * it.v * q;
          it.m.g.position.set(x, altura(x, z) + 0.2 + Math.sin(Math.PI * Math.min(1, u * 1.1)) * it.h, z); it.m.g.rotation.y = Math.atan2(it.dx, it.dz);
          it.alas[0].rotation.z = Math.sin(e.t * 28 + it.f) * 0.8; it.alas[1].rotation.z = -it.alas[0].rotation.z;
        }
        if (e.t > dur + 0.8) fin = true;
      }
      if (fin) { quitar(e); evs.splice(k, 1); }
    }
  };

  // ---------- caballos con vida propia ----------
  const pasoA = (e, tx, tz, vel, dt) => {
    const dx = tx - e.g.position.x, dz = tz - e.g.position.z;
    if (Math.hypot(dx, dz) < 0.9) return 'llego';
    const ang = Math.atan2(dx, dz), nx = e.g.position.x + Math.sin(ang) * vel * dt, nz = e.g.position.z + Math.cos(ang) * vel * dt;
    if (!enMapa(nx, nz) || !libre(nx, nz) || choca(nx, nz, e.q.r * 0.9, e.q)) return 'bloq';
    e.g.position.x = nx; e.g.position.z = nz; e.ang = ang; return 'ok';
  };
  const decidir = (h) => {
    const z = h.zona;
    if (rnd() < 0.4) { h.t = 4 + rnd() * 8; return; }                                       // sigue pastando
    if (h.tipo === 'corral' && z && !h.fuera) {
      if (rnd() < 0.1 && !caballos.some((o) => o.zona === z && o.fuera)) {                  // alguno sale por la puerta
        h.ruta = [z.adentro, z.puerta, z.afuera]; h.fuera = true; h.salidaT = 50 + rnd() * 60; h.hx = z.afuera[0]; h.hz = z.afuera[1]; h.estado = 'caminar'; h.t = 40; return;
      }
      h.tx = mix(z.x0, z.x1, rnd()); h.tz = mix(z.z0, z.z1, rnd());
    } else { const th = rnd() * 6.28, r = 3 + rnd() * h.radio; h.tx = h.hx + Math.cos(th) * r; h.tz = h.hz + Math.sin(th) * r; }
    h.estado = 'caminar'; h.t = 20;
  };
  const moverCaballos = (dt, p, t) => {
    for (const h of caballos) {
      const g = h.g, d = Math.hypot(g.position.x - p.x, g.position.z - p.z);
      if (d > 130) { g.visible = false; h.q.activo = false; continue; } g.visible = true; h.q.activo = true;
      h.t -= dt; let anda = 0, objPasto = 0;
      if (h.tipo === 'atado') objPasto = Math.sin(t * 0.12 + h.fase) > 0.55 ? 0.45 : 0;
      else if (h.estado === 'pastar') {
        objPasto = Math.sin(t * 0.3 + h.fase) > 0.85 ? 0.1 : 1;
        if (h.fuera && !h.ruta && (h.salidaT -= dt) <= 0) { h.ruta = [h.zona.afuera, h.zona.puerta, h.zona.adentro]; h.volviendo = true; h.estado = 'caminar'; h.t = 60; }
        else if (h.t <= 0) decidir(h);
      } else {
        const dest = h.ruta ? h.ruta[0] : [h.tx, h.tz], r = pasoA(h, dest[0], dest[1], 1.4, dt);
        if (r === 'ok') anda = 1;
        else if (r === 'llego') {
          if (h.ruta) { h.ruta.shift(); if (!h.ruta.length) { h.ruta = null; if (h.volviendo) { h.volviendo = false; h.fuera = false; h.hx = (h.zona.x0 + h.zona.x1) / 2; h.hz = (h.zona.z0 + h.zona.z1) / 2; } h.estado = 'pastar'; h.t = 4 + rnd() * 6; } }
          else { h.estado = 'pastar'; h.t = 4 + rnd() * 8; }
        } else { h.estado = 'pastar'; h.t = 2 + rnd() * 2; if (h.ruta && !h.volviendo && !h.fuera) h.ruta = null; }        // algo le cierra el paso: espera
        if (h.t <= 0 && !h.ruta) { h.estado = 'pastar'; h.t = 3; }
      }
      h.pasto += (objPasto - h.pasto) * Math.min(1, dt * 1.6);
      let da = h.ang - g.rotation.y; da = Math.atan2(Math.sin(da), Math.cos(da)); g.rotation.y += da * Math.min(1, dt * 4);
      g.position.y = altura(g.position.x, g.position.z) + (anda ? Math.abs(Math.sin(h.marcha * 2)) * 0.03 : 0);
      if (anda) h.marcha += dt * 5.5;
      h.c.andar(h.marcha, anda ? 0.45 : 0); h.c.animar(h.fase + t, h.pasto, anda);
      h.q.x = g.position.x; h.q.z = g.position.z;
    }
  };

  return {
    animales, carretas, caballos, director, arreo, decir, lanzar, evs, nuevoJinete, nuevaBandada, nuevaTropilla, nuevoGrupoCruce,
    actualizar(dt, p, frame) {
      director(dt, p); arrear(dt, p);
      const t = frame * 0.02;
      for (const a of animales) {
        if (a.arreo) continue;
        const g = a.g, dx = p.x - g.position.x, dz = p.z - g.position.z, d = Math.hypot(dx, dz);
        if (d > 140) { g.visible = false; if (a.q) a.q.activo = false; continue; } g.visible = true; if (a.q) a.q.activo = true;
        if (a.sp.vel === 0 && !a.sp.huye) continue;                    // yarará: inmóvil
        a.t -= dt; let mover = 0, ang = a.ang;
        if (a.sp.huye && d < a.sp.huida) { a.estado = 'huir'; ang = Math.atan2(-dx, -dz) + a.deriva; mover = a.sp.velH; a.t = 1.2; }
        else if (a.estado === 'huir' && a.t > 0) mover = a.sp.velH * 0.5;
        else {
          if (a.estado === 'huir') a.estado = 'pastar';
          if (a.estado === 'pastar') {
            if (a.t <= 0) {
              if (a.ruta) { a.estado = 'caminar'; a.t = 40; }
              else if (a.corral && tranq.abierta && Math.random() < 0.04 && arreo.vacas.filter((c) => !c.corral).length < 2) { const gx = tranq.x, gz = tranq.z; a.ruta = [[gx, gz - 3], [gx, gz + 1.5], [gx + (Math.random() - 0.5) * 8, gz + 10]]; a.estado = 'caminar'; a.t = 40; a.salio = true; }
              else if (a.corral) { a.tx = rodeo[0] + (Math.random() - 0.5) * (CW - 4); a.tz = rodeo[1] + (Math.random() - 0.5) * (CL - 4); a.estado = 'caminar'; a.t = 14; }
              else { const th = hs(frame + a.hx) * 6.28, r = 4 + hs(frame * 1.7 + a.hz) * a.sp.radio; a.tx = a.hx + Math.cos(th) * r; a.tz = a.hz + Math.sin(th) * r; a.estado = 'caminar'; a.t = 14; }
            }
          } else if (a.estado === 'caminar') {
            const dest = a.ruta ? a.ruta[0] : [a.tx, a.tz], ddx = dest[0] - g.position.x, ddz = dest[1] - g.position.z, dd = Math.hypot(ddx, ddz);
            if (a.ruta && dd < 1.2) { a.ruta.shift(); if (!a.ruta.length) { a.ruta = null; if (a.salio) { a.corral = false; a.salio = false; a.hx = g.position.x; a.hz = g.position.z; } a.estado = 'pastar'; a.t = 3 + hs(frame * 0.3 + a.hx) * 7; } }
            else if (dd < 1.2 || a.t <= 0) { a.ruta = null; a.salio = false; a.estado = 'pastar'; a.t = 3 + hs(frame * 0.3 + a.hx) * 7; } else { ang = Math.atan2(ddx, ddz); mover = a.sp.vel; }
          }
        }
        if (mover > 0) {
          const nx = g.position.x + Math.sin(ang) * mover * dt, nz = g.position.z + Math.cos(ang) * mover * dt;
          if (enMapa(nx, nz) && libre(nx, nz) && !choca(nx, nz, a.q ? a.q.r * 0.8 : 0.25, a.q)) { g.position.x = nx; g.position.z = nz; a.ang = ang; } else { a.estado = 'pastar'; a.t = 1.5; mover = 0; }
          let da = a.ang - g.rotation.y; da = Math.atan2(Math.sin(da), Math.cos(da)); g.rotation.y += da * Math.min(1, dt * 6);
        }
        g.position.y = altura(g.position.x, g.position.z) + (mover > 0 ? Math.abs(Math.sin(a.fase * 1.0)) * 0.04 : 0);
        if (a.q) { a.q.x = g.position.x; a.q.z = g.position.z; }
        const pastando = a.estado === 'pastar' && a.sp.vel > 0;                  // inclina el cuerpo hacia adelante como si pastara
        g.rotation.x += ((pastando ? 0.1 + 0.04 * Math.sin(t * 1.7 + a.fase) : 0) - g.rotation.x) * Math.min(1, dt * 2);
        bailar(a, mover, dt);
      }
      moverCaballos(dt, p, t, frame);
      // carretas: avanzan por el camino y dan la vuelta en los extremos
      for (const c of carretas) {
        if (Math.hypot(c.g.position.x - p.x, c.g.position.z - p.z) > 400) { c.s += c.dir * 1.4 * dt; } else c.s += c.dir * 1.4 * dt;
        if (c.s > largoRuta - 20) c.dir = -1; if (c.s < 20) c.dir = 1;
        const pos = sobreRuta(c.s), ang = pos.ang + (c.dir < 0 ? Math.PI : 0);
        c.g.position.set(pos.x, altura(pos.x, pos.z), pos.z); c.g.rotation.y = ang;
        c.fase += dt * 1.4 * 1.4; c.ruedas.forEach((r) => { r.rotation.x = c.fase * c.dir; });
        c.bueyes.forEach((b) => bailar({ patas: b.patas, fase: c.fase * 2 }, 1.2, 0));
        const s1 = Math.sin(ang), c1 = Math.cos(ang);
        [[0, -1.2], [0, 1.6], [0, 6.8]].forEach(([lx, lz], i) => { const pp = c.personas[i]; pp.x = pos.x + s1 * lz; pp.z = pos.z + c1 * lz; });
      }
      // carancho en vuelo
      for (const v of vuelos) {
        if (Math.hypot(v.c[0] - p.x, v.c[1] - p.z) > 400) { v.g.visible = false; continue; } v.g.visible = true;
        const a = t * 0.35 + v.f; v.g.position.set(v.c[0] + Math.cos(a) * v.r, v.h + Math.sin(a * 2) * 2, v.c[1] + Math.sin(a) * v.r);
        v.g.rotation.y = -a; v.g.rotation.z = -0.25; v.alas.forEach((al, i) => { al.rotation.z = Math.sin(t * 2.2 + v.f) * 0.35 * (i ? 1 : -1); });
      }
    },
  };
}
