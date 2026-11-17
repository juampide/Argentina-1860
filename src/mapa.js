// Mapa del pago: un dibujo del mundo entero (caminos, arroyo, laguna, monte, casas y corrales) más dos vistas:
// el minimapa, que gira junto con la cámara (arriba = arriba en pantalla) y marca los lugares con nombre y distancia,
// y el mapa grande (tecla M) con todo el pago a la vista.
import { K, ARROYO, CAMINO, SENDAS, LAGUNA, MONTE, suavizar } from './campo.js';

const ANCHO = 3200, ALTO = 2400, ESC = 0.5;               // el dibujo base va a 0,5 px por metro
const PAPEL = '#e2d3ae', TINTA = '#2a221b', ROJO = '#a03220';

export function crearMapa({ campo, lugares, destino, cvMini, cvGrande }) {
  const base = document.createElement('canvas'); base.width = ANCHO * ESC; base.height = ALTO * ESC;
  const b = base.getContext('2d');
  const pk = (kx, ky) => [kx * 1000 * ESC, ky * 1000 * ESC];
  const wx = (x, z) => [(x + ANCHO / 2) * ESC, (z + ALTO / 2) * ESC];
  const trazo = (pts, color, ancho) => { b.strokeStyle = color; b.lineWidth = ancho * ESC; b.lineCap = b.lineJoin = 'round'; b.beginPath(); suavizar(pts).forEach(([kx, ky], i) => { const [x, y] = pk(kx, ky); i ? b.lineTo(x, y) : b.moveTo(x, y); }); b.stroke(); };
  b.fillStyle = PAPEL; b.fillRect(0, 0, base.width, base.height);
  // cuadrícula cada 500 m, para tener escala
  b.strokeStyle = '#2a221b18'; b.lineWidth = 1;
  for (let x = 0; x <= ANCHO; x += 500) { b.beginPath(); b.moveTo(x * ESC, 0); b.lineTo(x * ESC, base.height); b.stroke(); }
  for (let y = 0; y <= ALTO; y += 500) { b.beginPath(); b.moveTo(0, y * ESC); b.lineTo(base.width, y * ESC); b.stroke(); }
  { const [x, y] = pk(...MONTE.c); b.fillStyle = '#a9b57e'; b.beginPath(); b.ellipse(x, y, MONTE.rx * 1000 * ESC, MONTE.ry * 1000 * ESC, 0, 0, 7); b.fill();
    b.fillStyle = '#8a9a62'; for (let i = 0; i < 90; i++) { const a = i * 2.4, r = Math.sqrt((i + 1) / 90); b.beginPath(); b.arc(x + Math.cos(a) * MONTE.rx * 1000 * ESC * r * 0.9, y + Math.sin(a) * MONTE.ry * 1000 * ESC * r * 0.9, 5, 0, 7); b.fill(); } }
  { const [x, y] = pk(...LAGUNA.c); b.fillStyle = '#9cc0c4'; b.strokeStyle = '#6d98a0'; b.lineWidth = 3; b.beginPath(); b.ellipse(x, y, LAGUNA.rx * 1000 * ESC, LAGUNA.ry * 1000 * ESC, 0, 0, 7); b.fill(); b.stroke(); }
  trazo(ARROYO, '#9cc0c4', 9);
  trazo(CAMINO, '#c3a674', 8); trazo(CAMINO, '#d3ba8a', 5);
  SENDAS.forEach((s) => trazo(s, '#cdb383', 3.6));
  // casas y corrales
  b.fillStyle = '#7b5a3c'; b.strokeStyle = '#3a2a1c'; b.lineWidth = 1;
  for (const [x, z, w, l, rot] of campo.edificios) {
    const [cx, cy] = wx(x, z); b.save(); b.translate(cx, cy); b.rotate(-rot); const ww = Math.max(w * ESC, 5), ll = Math.max(l * ESC, 4); b.fillRect(-ww / 2, -ll / 2, ww, ll); b.strokeRect(-ww / 2, -ll / 2, ww, ll); b.restore();
  }
  b.strokeStyle = '#6a4a2a'; b.lineWidth = 2; b.setLineDash([5, 3]);
  for (const pts of campo.cercos) { b.beginPath(); pts.forEach(([x, z], i) => { const [X, Y] = wx(x, z); i ? b.lineTo(X, Y) : b.moveTo(X, Y); }); b.closePath(); b.stroke(); }
  b.setLineDash([]);

  const lug = () => lugares.map((l) => (typeof l === 'function' ? l() : l));
  const halo = (c, txt, x, y, tam, col = TINTA, bold = false) => {
    c.font = `${bold ? 'bold ' : ''}${tam}px Georgia, serif`; c.lineWidth = 4; c.strokeStyle = '#e2d3aeee'; c.lineJoin = 'round'; c.strokeText(txt, x, y); c.fillStyle = col; c.fillText(txt, x, y);
  };
  const flecha = (c, x, y, ang, r, col) => { c.save(); c.translate(x, y); c.rotate(ang); c.fillStyle = col; c.strokeStyle = '#fff6e2'; c.lineWidth = 2; c.beginPath(); c.moveTo(0, -r * 1.3); c.lineTo(r * 0.85, r * 0.9); c.lineTo(0, r * 0.4); c.lineTo(-r * 0.85, r * 0.9); c.closePath(); c.fill(); c.stroke(); c.restore(); };

  // ---------- minimapa ----------
  const S = cvMini.width, m = cvMini.getContext('2d'), esc = 0.6, k = esc / ESC;
  const dibujarMini = (px, pz, rumbo, yaw) => {
    const a = Math.PI / 2 - yaw, ca = Math.cos(a), sa = Math.sin(a);
    m.fillStyle = PAPEL; m.fillRect(0, 0, S, S);
    m.save(); m.translate(S / 2, S / 2); m.rotate(a); m.scale(k, k); m.translate(-(px + ANCHO / 2) * ESC, -(pz + ALTO / 2) * ESC); m.drawImage(base, 0, 0); m.restore();
    const pant = (x, z) => { const dx = x - px, dz = z - pz; return [(dx * ca - dz * sa) * esc, (dx * sa + dz * ca) * esc]; };
    const M = S / 2 - 12;
    m.textAlign = 'center'; m.textBaseline = 'middle';
    const dest = destino(), ocupado = [];
    const marca = (l, esDest) => {
      const [sx, sy] = pant(l.x, l.z), d = Math.hypot(l.x - px, l.z - pz), fuera = Math.abs(sx) > M || Math.abs(sy) > M;
      let X = sx, Y = sy;
      if (fuera) { const f = M / Math.max(Math.abs(sx), Math.abs(sy)); X = sx * f; Y = sy * f; }
      X += S / 2; Y += S / 2;
      const col = esDest ? ROJO : '#3a2a1c', txt = l.n + (fuera || esDest ? ` · ${d >= 1000 ? (d / 1000).toFixed(1) + ' km' : Math.round(d / 10) * 10 + ' m'}` : '');
      if (fuera) flecha(m, X, Y, Math.atan2(sx, -sy), 6, col); else { m.fillStyle = col; m.beginPath(); m.arc(X, Y, esDest ? 6 : 4, 0, 7); m.fill(); m.strokeStyle = '#fff6e2'; m.lineWidth = 1.5; m.stroke(); }
      m.font = `${esDest ? 'bold ' : ''}${esDest ? 12 : 11}px Georgia, serif`; const tw = m.measureText(txt).width / 2 + 4;
      const tx = Math.max(tw, Math.min(S - tw, X)), ty = Math.max(9, Math.min(S - 9, fuera ? (Y < S / 2 ? Y + 14 : Y - 14) : Y - 12));
      const caja = [tx - tw, ty - 8, tx + tw, ty + 8];
      if (!esDest && ocupado.some((o) => caja[0] < o[2] && caja[2] > o[0] && caja[1] < o[3] && caja[3] > o[1])) return;
      ocupado.push(caja); halo(m, txt, tx, ty, esDest ? 12 : 11, col, esDest);
    };
    if (dest) marca(dest, true);
    for (const l of lug().sort((p, q) => Math.hypot(p.x - px, p.z - pz) - Math.hypot(q.x - px, q.z - pz))) if (!dest || Math.hypot(l.x - dest.x, l.z - dest.z) > 12) marca(l, false);
    flecha(m, S / 2, S / 2, Math.PI - rumbo + a, 8, '#2a221b');                         // vos, mirando hacia donde caminás
    const [nx, ny] = [(0 * ca - -1 * sa), (0 * sa + -1 * ca)]; halo(m, 'N', S / 2 + nx * (S / 2 - 22) * 0 + nx * 118, S / 2 + ny * 118, 13, ROJO, true);
  };

  // ---------- mapa grande ----------
  const G = cvGrande, g = G.getContext('2d'), eg = G.width / ANCHO;
  const dibujarGrande = (px, pz, rumbo, yaw, nombres) => {
    g.drawImage(base, 0, 0, G.width, G.height);
    g.textAlign = 'center'; g.textBaseline = 'middle';
    const dest = destino(), P = (x, z) => [(x + ANCHO / 2) * eg, (z + ALTO / 2) * eg];
    for (const l of lug()) { const [X, Y] = P(l.x, l.z); g.fillStyle = '#3a2a1c'; g.beginPath(); g.arc(X, Y, 4, 0, 7); g.fill(); halo(g, l.n, X, Y - 13, 13); }
    for (const n of nombres) { const [X, Y] = P(n.x, n.z); g.fillStyle = '#5a6a8a'; g.beginPath(); g.arc(X, Y, 2.2, 0, 7); g.fill(); }
    if (dest) { const [X, Y] = P(dest.x, dest.z); g.strokeStyle = ROJO; g.lineWidth = 3; g.beginPath(); g.arc(X, Y, 11, 0, 7); g.stroke(); g.fillStyle = ROJO; g.beginPath(); g.arc(X, Y, 4.5, 0, 7); g.fill(); halo(g, 'Objetivo: ' + dest.n, X, Y + 22, 14, ROJO, true); }
    const [X, Y] = P(px, pz);
    // hacia dónde mira la cámara (arriba en pantalla)
    const fx = -Math.cos(yaw), fz = -Math.sin(yaw); g.strokeStyle = '#2a221b66'; g.lineWidth = 2; g.setLineDash([6, 5]); g.beginPath(); g.moveTo(X, Y); g.lineTo(X + fx * 70, Y + fz * 70); g.stroke(); g.setLineDash([]);
    halo(g, 'arriba en pantalla', X + fx * 95, Y + fz * 95, 11, '#2a221bbb');
    flecha(g, X, Y, Math.PI - rumbo, 9, '#2a221b'); halo(g, 'Vos', X, Y + 24, 14, '#2a221b', true);
    halo(g, 'N', G.width - 24, 22, 18, ROJO, true); g.strokeStyle = ROJO; g.lineWidth = 2; g.beginPath(); g.moveTo(G.width - 24, 52); g.lineTo(G.width - 24, 36); g.stroke();
    g.fillStyle = '#2a221b'; g.fillRect(18, G.height - 22, 500 * eg, 3); halo(g, '500 m', 18 + 250 * eg, G.height - 34, 12);
    g.strokeStyle = TINTA; g.lineWidth = 2; g.strokeRect(1, 1, G.width - 2, G.height - 2);
  };
  return { dibujarMini, dibujarGrande };
}
