// Colisión: rejilla espacial de manzanas/edificios (polígonos) + agua
export const obstaculos = [];
export const CELDA = 64;
export const rejilla = new Map();
export const clave = (cx, cz) => cx * 100003 + cz;

export function registrar(pts) {
  const xs = pts.map(p => p[0]), zs = pts.map(p => p[1]);
  const o = { pts, minx: Math.min(...xs), maxx: Math.max(...xs), minz: Math.min(...zs), maxz: Math.max(...zs) };
  const id = obstaculos.length; obstaculos.push(o);
  for (let cx = Math.floor(o.minx / CELDA); cx <= Math.floor(o.maxx / CELDA); cx++)
    for (let cz = Math.floor(o.minz / CELDA); cz <= Math.floor(o.maxz / CELDA); cz++) {
      const k = clave(cx, cz); if (!rejilla.has(k)) rejilla.set(k, []); rejilla.get(k).push(id);
    }
  return id;
}
export const rect = (x0, z0, x1, z1) => registrar([[x0, z0], [x1, z0], [x1, z1], [x0, z1]]);

function dentro(o, x, z) {
  if (x < o.minx || x > o.maxx || z < o.minz || z > o.maxz) return false;
  let c = false; const p = o.pts;
  for (let i = 0, j = p.length - 1; i < p.length; j = i++) {
    if ((p[i][1] > z) !== (p[j][1] > z) && x < (p[j][0] - p[i][0]) * (z - p[i][1]) / (p[j][1] - p[i][1]) + p[i][0]) c = !c;
  }
  return c;
}
export function ocupado(x, z) {
  const l = rejilla.get(clave(Math.floor(x / CELDA), Math.floor(z / CELDA)));
  if (!l) return false;
  for (const id of l) if (dentro(obstaculos[id], x, z)) return true;
  return false;
}

let aguaFn = () => false;
export const setAgua = (f) => { aguaFn = f; };
export const enAgua = (x, z) => aguaFn(x, z);

const R = 0.7;
export const libre = (x, z) =>
  !(ocupado(x, z) || ocupado(x + R, z) || ocupado(x - R, z) || ocupado(x, z + R) || ocupado(x, z - R)) && !aguaFn(x, z);

// ---------- Personas (NPC y transeúntes): círculos móviles que bloquean el paso ----------
export const personas = [];
export function choca(x, z, radio, ignorar) {
  for (const p of personas) {
    if (p === ignorar || p.activo === false) continue;
    if (Math.hypot(p.x - x, p.z - z) < p.r + radio) return true;
  }
  return false;
}

// ---------- Altura del piso (veredas elevadas) ----------
const alturas = [];
export function registrarAltura(pts, h) {
  const xs = pts.map(p => p[0]), zs = pts.map(p => p[1]);
  alturas.push({ pts, h, minx: Math.min(...xs), maxx: Math.max(...xs), minz: Math.min(...zs), maxz: Math.max(...zs) });
}
const gridAlt = new Map();
let gridListo = false;
function armarGridAlt() {
  alturas.forEach((o, id) => {
    for (let cx = Math.floor(o.minx / CELDA); cx <= Math.floor(o.maxx / CELDA); cx++)
      for (let cz = Math.floor(o.minz / CELDA); cz <= Math.floor(o.maxz / CELDA); cz++) {
        const k = clave(cx, cz); if (!gridAlt.has(k)) gridAlt.set(k, []); gridAlt.get(k).push(id);
      }
  });
  gridListo = true;
}
// El terreno (lomas del campo) se suma a la altura de las veredas; en la ciudad es plano.
let terrenoFn = () => 0;
export const setTerreno = (f) => { terrenoFn = f; };
export function alturaEn(x, z) {
  if (!gridListo) armarGridAlt();
  const base = terrenoFn(x, z);
  const l = gridAlt.get(clave(Math.floor(x / CELDA), Math.floor(z / CELDA)));
  if (!l) return base;
  for (const id of l) if (dentro(alturas[id], x, z)) return base + alturas[id].h;
  return base;
}
