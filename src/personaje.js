import * as THREE from 'three';

// Personajes low poly estilizados: formas angulares, sin cara (solo la nariz),
// la identidad está en el sombrero, el pelo, el bigote y la ropa. Pensados para verse desde arriba.
// Telas reales (CC0, ambientCG) sobre formas facetadas: lana, lino y sarga, en gris para teñirlas.
const TELAS = {};
export async function cargarTelas() {
  const l = new THREE.TextureLoader();
  const cargar = (u, srgb) => l.loadAsync(u).then((t) => { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 8; if (srgb) t.colorSpace = THREE.SRGBColorSpace; return t; });
  for (const n of ['lana', 'lino', 'sarga']) {
    const [map, normalMap] = await Promise.all([cargar(`./tex/${n}_c.jpg`, true), cargar(`./tex/${n}_n.jpg`)]);
    TELAS[n] = { map, normalMap };
  }
}
const BRILLO = { lana: 1.9, lino: 1.05, sarga: 2.1 };
function tela(nombre, color, rep = [2, 2], o = {}) {
  const t = TELAS[nombre];
  if (!t) return M(color, o);
  const map = t.map.clone(), nm = t.normalMap.clone();
  for (const x of [map, nm]) { x.repeat.set(rep[0], rep[1]); x.needsUpdate = true; }
  return new THREE.MeshStandardMaterial({ color: new THREE.Color(color).multiplyScalar(BRILLO[nombre]), map, normalMap: nm, normalScale: new THREE.Vector2(1, 1),
    flatShading: true, roughness: 0.95, side: THREE.DoubleSide, ...o });
}

const M = (color, o = {}) => new THREE.MeshStandardMaterial({ color, flatShading: true, roughness: 0.92, metalness: 0, side: THREE.DoubleSide, ...o });
const sombra = (m) => { m.castShadow = true; return m; };
const en = (m, x, y, z) => { m.position.set(x, y, z); return m; };
// tronco de pirámide/cilindro con pocas caras
const tronco = (rs, ri, h, mat, seg = 6, abierto = false) => sombra(new THREE.Mesh(new THREE.CylinderGeometry(rs, ri, h, seg, 1, abierto), mat));
const caja = (x, y, z, mat) => sombra(new THREE.Mesh(new THREE.BoxGeometry(x, y, z), mat));
const ico = (r, mat, sx = 1, sy = 1, sz = 1) => { const m = sombra(new THREE.Mesh(new THREE.IcosahedronGeometry(r, 0), mat)); m.scale.set(sx, sy, sz); return m; };
const octa = (r, mat, sx = 1, sy = 1, sz = 1) => { const m = sombra(new THREE.Mesh(new THREE.OctahedronGeometry(r, 0), mat)); m.scale.set(sx, sy, sz); return m; };
const cono = (r, h, mat, seg = 4) => sombra(new THREE.Mesh(new THREE.ConeGeometry(r, h, seg), mat));
const girar = (m, x = 0, y = 0, z = 0) => { m.rotation.set(x, y, z); return m; };
const aclara = (c, f) => new THREE.Color(c).multiplyScalar(f);

/**
 * o: { estilo: 'levita'|'vestido'|'sotana'|'poncho'|'pregonero', piel, pelo, ropa, ropa2, chaleco, pantalon,
 *      sombrero: 'copa'|'ala'|'bonete'|'gorra', colorSombrero, peinado: 'puas'|'largo'|'rodete'|'canoso'|'corto',
 *      barba, bigote: 'fino'|'manubrio', baston }
 */
// Silueta clara que se ve donde algo tapa al personaje (edificios, árboles). El stencil evita pintarla sobre el propio cuerpo.
const matSilueta = new THREE.MeshBasicMaterial({ color: '#ffe3ae', transparent: true, opacity: 0.55, depthFunc: THREE.GreaterDepth, depthWrite: false, stencilWrite: true, stencilFunc: THREE.NotEqualStencilFunc, stencilRef: 1 });
export function siluetear(g) {
  const mallas = []; g.traverse((m) => { if (m.isMesh && !m.userData.esSilueta) mallas.push(m); });
  for (const m of mallas) {
    (Array.isArray(m.material) ? m.material : [m.material]).forEach((mt) => { mt.stencilWrite = true; mt.stencilRef = 1; mt.stencilFunc = THREE.AlwaysStencilFunc; mt.stencilZPass = THREE.ReplaceStencilOp; });
    m.renderOrder = 10;                                       // se dibuja después del escenario: así el stencil solo queda donde el cuerpo realmente se ve
    const s = new THREE.Mesh(m.geometry, matSilueta); s.userData.esSilueta = true; s.renderOrder = 20; s.position.copy(m.position); s.quaternion.copy(m.quaternion); s.scale.copy(m.scale); s.castShadow = false;
    m.parent.add(s);
  }
}

export function crearPersonaje(o) {
  const g = new THREE.Group(), cuerpo = new THREE.Group(); g.add(cuerpo); g.scale.setScalar(0.92 * (o.escala || 1));
  const estilo = o.estilo || 'levita';
  const piel = M(o.piel || '#f0c19a'), pielOsc = M(aclara(o.piel || '#f0c19a', 0.82));
  const pelo = M(o.pelo || '#3a2a1c'), negro = M('#1f1a17'), cuero = M('#6b4528'), crema = M('#f4eedc'), oro = M('#d6ac3f', { metalness: 0.3 });
  const ropa = o.ropa || '#2f3f66', ropa2 = o.ropa2 || '#f4eedc';
  const conPiernas = estilo === 'levita' || estilo === 'poncho' || estilo === 'pregonero';
  const piernas = [], rodillas = [], brazos = [], codos = [];
  const CAD = 0.86;                                   // altura de la cadera

  // ---------- piernas flacas y botas grandes ----------
  if (conPiernas) {
    for (const s of [-1, 1]) {
      const cad = new THREE.Group(); cad.position.set(s * 0.085, CAD, 0);
      const gaucho = estilo === 'poncho';
      const mP = gaucho ? tela('lino', '#efe6d0', [2, 2]) : tela('lana', o.pantalon || '#8a8478', [2, 2]);
      cad.add(en(tronco(gaucho ? 0.1 : 0.072, gaucho ? 0.09 : 0.056, 0.44, mP, 5), 0, -0.2, 0));
      const rod = new THREE.Group(); rod.position.y = -0.42; cad.add(rod);
      rod.add(en(tronco(gaucho ? 0.088 : 0.056, gaucho ? 0.07 : 0.046, 0.4, mP, 5), 0, -0.18, 0));
      const mB = gaucho ? M('#a06a3a') : negro;
      rod.add(en(tronco(0.1, 0.066, 0.2, mB, 6), 0, -0.3, 0));                          // caña acampanada
      rod.add(en(caja(0.1, 0.08, 0.24, mB), 0, -0.4, 0.05));                            // pie
      rod.add(en(caja(0.105, 0.03, 0.26, M('#2b2220')), 0, -0.44, 0.05));              // suela
      cuerpo.add(cad); piernas.push(cad); rodillas.push(rod);
    }
  }

  // ---------- tronco: bloques de color ----------
  const torso = new THREE.Group(); cuerpo.add(torso);
  let mangas = M(ropa), punos = true, antebrazoPiel = false, guante = null, anchoHombro = 0.235, sinHombro = false;
  const pecho = (rs, ri, h, y, mat, sz = 0.68) => { const m = tronco(rs, ri, h, mat, 6); m.scale.z = sz; m.position.y = y; return m; };

  if (estilo === 'levita') {
    const abrigo = tela('lana', ropa, [3, 3]); mangas = abrigo;
    torso.add(pecho(0.2, 0.15, 0.5, 1.12, abrigo));
    const faldon = tronco(0.15, 0.27, 0.46, abrigo, 6, true); faldon.scale.z = 0.8; faldon.position.y = 0.67; torso.add(faldon);
    torso.add(en(caja(0.5, 0.07, 0.2, abrigo), 0, 1.36, 0));                                   // hombros
    torso.add(en(caja(0.13, 0.3, 0.04, tela('lana', o.chaleco || '#d9b45a', [1, 1])), 0, 1.1, 0.14));              // chaleco
    for (const s of [-1, 1]) torso.add(girar(en(caja(0.07, 0.34, 0.02, M(aclara(ropa, 1.5))), s * 0.085, 1.16, 0.15), 0, 0, -s * 0.33)); // solapas
    torso.add(girar(en(cono(0.06, 0.12, crema, 3), 0, 1.31, 0.14), Math.PI, 0, 0));            // corbata
  } else if (estilo === 'pregonero') {
    torso.add(pecho(0.19, 0.145, 0.5, 1.12, tela('lino', ropa2, [3, 2])));
    torso.add(pecho(0.2, 0.16, 0.34, 1.06, tela('lana', ropa, [3, 2]), 0.72));                                    // chaleco marrón
    torso.add(en(caja(0.46, 0.06, 0.19, tela('lino', ropa2, [2, 1])), 0, 1.36, 0));
    torso.add(girar(en(cono(0.1, 0.16, M('#c0392b'), 3), 0, 1.3, 0.1), Math.PI, 0, 0));       // pañuelo rojo
    torso.add(girar(en(caja(0.045, 0.62, 0.015, cuero), 0.03, 1.1, 0.145), 0, 0, -0.62));       // correa
    torso.add(en(caja(0.34, 0.28, 0.14, cuero), -0.3, 0.86, 0.02));                             // bolso
    torso.add(girar(en(caja(0.07, 0.22, 0.06, M('#d9d2bd')), -0.3, 0.7, 0.02), 0, 0, 0.15));
    for (let i = 0; i < 3; i++) torso.add(girar(en(caja(0.26, 0.02, 0.2, crema), -0.3, 1.01 + i * 0.022, 0.02), 0, (i - 1) * 0.2, 0.1)); // diarios
    mangas = tela('lino', ropa2, [2, 2]); punos = false; antebrazoPiel = true;
  } else if (estilo === 'vestido') {
    const falda = tronco(0.15, 0.62, 0.86, tela('lana', ropa, [6, 3]), 8, true); falda.position.y = 0.45; torso.add(falda);
    torso.add(en(tronco(0.64, 0.64, 0.05, M(aclara(ropa, 0.72)), 8), 0, 0.03, 0));
    const delantal = girar(en(cono(0.2, 0.62, crema, 3), 0, 0.5, 0.33), Math.PI, 0, 0); delantal.scale.set(1.1, 1, 0.12); torso.add(delantal);
    torso.add(pecho(0.15, 0.12, 0.34, 1.05, tela('lana', aclara(ropa, 1.2), [2, 2]), 0.7));                      // corpiño
    const chal = tronco(0.17, 0.3, 0.3, tela('lino', ropa2, [3, 2]), 6, true); chal.scale.z = 0.85; chal.position.y = 1.2; torso.add(chal);
    const flecos = tronco(0.31, 0.32, 0.04, M('#d4c39a'), 6); flecos.scale.z = 0.85; flecos.position.y = 1.05; torso.add(flecos);
    anchoHombro = 0.2; mangas = tela('lana', ropa, [2, 2]);
  } else if (estilo === 'sotana') {
    const sot = tronco(0.17, 0.34, 1.12, tela('sarga', '#2b2622', [6, 4]), 7, true); sot.scale.z = 0.85; sot.position.y = 0.58; torso.add(sot);
    const roq = tronco(0.2, 0.31, 0.54, tela('lino', ropa2 || '#f4eedc', [6, 2]), 7, true); roq.scale.z = 0.9; roq.position.y = 0.95; torso.add(roq);
    torso.add(en(caja(0.5, 0.06, 0.2, M('#2b2622')), 0, 1.36, 0));
    const encaje = tronco(0.32, 0.34, 0.05, crema, 7); encaje.scale.z = 0.9; encaje.position.y = 0.69; torso.add(encaje);
    torso.add(en(caja(0.02, 0.14, 0.02, oro), 0, 1.12, 0.2)); torso.add(en(caja(0.08, 0.025, 0.02, oro), 0, 1.16, 0.2));
    mangas = tela('sarga', '#2b2622', [2, 2]);
  } else if (estilo === 'poncho') {
    torso.add(pecho(0.17, 0.14, 0.5, 1.12, tela('lino', '#efe6d0', [3, 2])));
    const rojo = o.poncho || '#c0392b';
    const pon = tronco(0.12, 0.5, 0.74, tela('sarga', rojo, [4, 3]), 4, true); pon.rotation.y = Math.PI / 4; pon.position.y = 1.0; torso.add(pon);   // forma de rombo
    const guarda = tronco(0.5, 0.5, 0.08, M('#2a1c14'), 4, true); guarda.rotation.y = Math.PI / 4; guarda.position.y = 0.7; torso.add(guarda);
    const guarda2 = tronco(0.48, 0.48, 0.04, crema, 4, true); guarda2.rotation.y = Math.PI / 4; guarda2.position.y = 0.78; torso.add(guarda2);
    torso.add(girar(en(cono(0.1, 0.16, M('#2f6f8f'), 3), 0, 1.3, 0.12), Math.PI, 0, 0));      // pañuelo
    const faja = tronco(0.19, 0.19, 0.09, M('#2f6f8f'), 6); faja.scale.z = 0.7; faja.position.y = 0.88; torso.add(faja);
    const chir = tela('sarga', '#4f6178', [2, 2]); torso.add(en(caja(0.26, 0.4, 0.02, chir), 0, 0.66, 0.13)); torso.add(en(caja(0.26, 0.4, 0.02, chir), 0, 0.66, -0.13));
    mangas = tela('lino', '#efe6d0', [2, 2]); punos = false; sinHombro = true;
    guante = M(o.piel || '#d9a27a');
  }

  // ---------- brazos ----------
  for (const s of [-1, 1]) {
    const hom = new THREE.Group(); hom.position.set(s * anchoHombro, 1.33, 0);
    if (!sinHombro) hom.add(octa(0.06, mangas));
    hom.add(en(tronco(0.05, 0.042, 0.3, mangas, 5), 0, -0.14, 0));
    const cod = new THREE.Group(); cod.position.y = -0.29; hom.add(cod);
    cod.add(en(tronco(0.042, 0.036, 0.28, antebrazoPiel ? piel : mangas, 5), 0, -0.13, 0));
    if (punos) cod.add(en(tronco(0.058, 0.044, 0.07, crema, 5), 0, -0.26, 0));              // puño acampanado
    cod.add(en(octa(0.055, guante || piel, 0.85, 1.25, 0.8), 0, -0.34, 0.01));              // mano grande
    cuerpo.add(hom); brazos.push(hom); codos.push(cod);
  }
  if (o.baston) {
    const b = new THREE.Group(); b.position.set(0, -0.33, 0.05);
    b.add(en(tronco(0.012, 0.012, 0.86, M('#2f1f14'), 4), 0, -0.34, 0));
    b.add(en(octa(0.035, M('#d9dde0', { metalness: 0.5 })), 0, 0.1, 0));
    codos[1].add(b);
  }

  // ---------- cabeza: faceta alargada, sin cara ----------
  const cab = new THREE.Group(); cab.position.y = 1.4; cuerpo.add(cab);
  cab.add(en(tronco(0.045, 0.05, 0.12, piel, 5), 0, 0.03, 0));
  cab.add(en(ico(0.14, piel, 0.88, 1.28, 0.98), 0, 0.22, 0));
  cab.add(girar(en(cono(0.03, 0.08, pielOsc, 3), 0, 0.2, 0.135), Math.PI / 2, 0, 0));        // nariz: cuña
  const zc = 0.12;

  const peinado = o.peinado || (estilo === 'vestido' ? 'rodete' : estilo === 'poncho' ? 'largo' : estilo === 'sotana' ? 'canoso' : 'puas');
  const puas = (n, r, y, largo, ancho) => {            // mechones en punta alrededor de la cabeza
    for (let i = 0; i < n; i++) {
      const a = (i / n) * Math.PI * 2, c = cono(ancho, largo, pelo, 4);
      c.position.set(Math.sin(a) * r, y + (i % 2) * 0.03, Math.cos(a) * r * 0.9 - 0.02);
      c.rotation.set(Math.cos(a) * 0.9, 0, -Math.sin(a) * 0.9); cab.add(c);
    }
  };
  if (peinado === 'puas') { cab.add(en(ico(0.14, pelo, 0.9, 0.7, 1.0), 0, 0.3, -0.03)); puas(7, 0.1, 0.4, 0.12, 0.045); }
  else if (peinado === 'corto') { cab.add(en(ico(0.145, pelo, 0.92, 0.75, 1.02), 0, 0.3, -0.025)); }
  else if (peinado === 'canoso') {
    for (const s of [-1, 1]) { cab.add(girar(en(cono(0.06, 0.16, pelo, 4), s * 0.14, 0.2, -0.02), 0, 0, s * 1.3)); }
    cab.add(girar(en(cono(0.07, 0.16, pelo, 4), 0, 0.2, -0.13), -1.3, 0, 0));
  } else if (peinado === 'largo') {
    cab.add(en(ico(0.145, pelo, 0.94, 0.8, 1.02), 0, 0.29, -0.03));
    cab.add(en(caja(0.27, 0.32, 0.08, pelo), 0, 0.1, -0.12));
    for (const s of [-1, 1]) cab.add(en(caja(0.04, 0.18, 0.08, pelo), s * 0.13, 0.2, 0.03));
  } else if (peinado === 'rodete') {
    cab.add(en(ico(0.15, pelo, 0.96, 0.85, 1.05), 0, 0.28, -0.03));
    for (const s of [-1, 1]) cab.add(en(caja(0.045, 0.24, 0.12, pelo), s * 0.13, 0.17, -0.02));
    cab.add(en(ico(0.09, pelo), 0, 0.4, -0.14));                                               // rodete
    cab.add(girar(en(caja(0.16, 0.14, 0.02, M('#e6c26a')), 0, 0.46, -0.1), -0.5, 0, 0));       // peineta dorada
    cab.add(en(octa(0.035, M('#c0392b')), 0.06, 0.45, -0.06));                                 // flor
  }
  if (o.barba) { cab.add(en(ico(0.1, pelo, 1.15, 0.9, 0.9), 0, 0.08, 0.075)); cab.add(en(caja(0.3, 0.05, 0.18, pelo), 0, 0.1, 0.01)); }
  if (o.bigote === 'manubrio') for (const s of [-1, 1]) cab.add(girar(en(cono(0.02, 0.16, pelo, 3), s * 0.075, 0.145, zc + 0.02), 0, 0, -s * (Math.PI / 2 - 0.45)));
  else if (o.bigote) cab.add(en(caja(0.13, 0.03, 0.035, pelo), 0, 0.145, zc + 0.02));

  // ---------- sombreros: siluetas inconfundibles ----------
  const t0 = 0.4, sb = o.sombrero;
  if (sb === 'copa') {
    const seda = M('#241f1c');
    cab.add(en(tronco(0.3, 0.3, 0.025, seda, 6), 0, t0, 0));
    cab.add(en(tronco(0.15, 0.18, 0.42, seda, 6), 0, t0 + 0.22, 0));
    cab.add(en(tronco(0.185, 0.185, 0.07, M('#b9bcc2'), 6), 0, t0 + 0.05, 0));
  } else if (sb === 'ala') {
    const f = M(o.colorSombrero || '#3b2f27');
    cab.add(girar(en(tronco(0.42, 0.42, 0.025, f, 8), 0, t0 - 0.02, 0), 0.05, 0, 0.05));
    cab.add(en(tronco(0.14, 0.18, 0.16, f, 6), 0, t0 + 0.07, 0));
    cab.add(en(tronco(0.185, 0.185, 0.045, M('#c0392b'), 6), 0, t0 + 0.02, 0));
  } else if (sb === 'bonete') {
    const n = M('#241f1c');
    cab.add(en(caja(0.3, 0.13, 0.3, n), 0, t0 + 0.02, 0));
    for (const [x, z] of [[1, 1], [-1, 1], [1, -1], [-1, -1]]) cab.add(en(caja(0.04, 0.1, 0.04, n), x * 0.13, t0 + 0.11, z * 0.13));
    cab.add(en(octa(0.045, n), 0, t0 + 0.17, 0));
  } else if (sb === 'kepi') {
    const az = M(o.ropa || '#2c3e6b');
    cab.add(en(tronco(0.2, 0.25, 0.14, az, 8), 0, t0 - 0.02, 0));
    cab.add(en(tronco(0.255, 0.255, 0.035, M('#d6ac3f'), 8), 0, t0 - 0.08, 0));
    cab.add(girar(en(caja(0.22, 0.02, 0.13, M('#1f1a17')), 0, t0 - 0.1, zc + 0.15), 0.18, 0, 0));
    cab.add(en(octa(0.035, M('#d6ac3f')), 0, t0 + 0.07, 0.1));
  } else if (sb === 'gorra') {
    const t = M(o.colorSombrero || '#6f7a68');
    cab.add(en(tronco(0.24, 0.2, 0.1, t, 8), 0, t0 - 0.03, 0));
    cab.add(girar(en(caja(0.24, 0.02, 0.16, M('#3a342d')), 0, t0 - 0.08, zc + 0.12), 0.2, 0, 0));
    cab.add(en(octa(0.03, t), 0, t0 + 0.03, 0));
  }

  // ---------- accesorios ----------
  if (o.prop === 'fusil') {
    const f = new THREE.Group(); f.position.set(0, -0.33, 0.05);
    f.add(en(tronco(0.02, 0.022, 0.62, M('#6b4528'), 5), 0, -0.52, 0));           // culata
    f.add(en(tronco(0.012, 0.014, 0.7, M('#6a6e72'), 5), 0, 0.15, 0));            // caño
    f.add(en(cono(0.016, 0.16, M('#c9ced2'), 3), 0, 0.58, 0));                    // bayoneta
    codos[1].add(f);
  } else if (o.prop === 'canasta') {
    const c = new THREE.Group(); c.position.set(0, -0.36, 0.08);
    c.add(en(tronco(0.15, 0.11, 0.13, M('#b98a4a'), 7), 0, 0, 0));
    c.add(en(tronco(0.145, 0.145, 0.03, M('#f6efdc'), 7), 0, 0.07, 0));
    for (const [x, z] of [[-0.05, 0], [0.05, 0.03], [0, -0.05]]) c.add(en(ico(0.04, M('#e8b86a'), 1, 0.7, 1), x, 0.1, z));
    const asa = sombra(new THREE.Mesh(new THREE.TorusGeometry(0.12, 0.012, 4, 8, Math.PI), M('#8a6a3a'))); asa.position.y = 0.04; c.add(asa);
    codos[0].add(c);
  } else if (o.prop === 'barril') {
    const b = tronco(0.19, 0.17, 0.5, M('#9a6a3c'), 7); b.position.set(0, 1.0, -0.32); torso.add(b);
    for (const y of [-0.15, 0.15]) { const a = new THREE.Mesh(new THREE.TorusGeometry(0.18, 0.012, 4, 8), M('#2a2724')); a.rotation.x = Math.PI / 2; a.position.set(0, 1.0 + y, -0.32); torso.add(a); }
    torso.add(girar(en(caja(0.04, 0.4, 0.01, M('#6b4528')), 0.12, 1.15, -0.1), 0.2, 0, 0.5));
  } else if (o.prop === 'guitarra') {
    const gu = new THREE.Group(); gu.position.set(0.1, 0.92, 0.22); gu.rotation.z = -0.55;
    gu.add(ico(0.2, M('#b07a3d'), 0.9, 1.15, 0.32));
    gu.add(en(ico(0.07, M('#2a1a10')), 0, 0.02, 0.1));
    gu.add(en(caja(0.06, 0.55, 0.04, M('#5a3a22')), 0, 0.42, 0));
    gu.add(en(caja(0.09, 0.12, 0.05, M('#3a2616')), 0, 0.72, 0));
    torso.add(gu);
  } else if (o.prop === 'cajon') {
    const c = caja(0.32, 0.14, 0.22, M('#6b4528')); c.position.set(-0.04, -0.34, 0.12); codos[0].add(c);
    const t = tronco(0.03, 0.03, 0.12, M('#2a2724'), 5); t.position.set(-0.04, -0.24, 0.12); codos[0].add(t);
  }

  cuerpo.traverse((m) => { if (m.isMesh) m.castShadow = true; });

  // ---------- animación ----------
  g.userData.cuerpo = cuerpo;
  g.userData.sinXray = true;                          // los personajes nunca se recortan
  g.userData.animar = (t, v) => {
    const a = Math.sin(t) * 0.6 * v;
    for (let i = 0; i < piernas.length; i++) {
      const fase = i === 0 ? t : t + Math.PI;
      piernas[i].rotation.x = Math.sin(fase) * 0.6 * v;
      rodillas[i].rotation.x = Math.max(0, Math.sin(fase - 1.3)) * 0.9 * v;
    }
    const abre = estilo === 'vestido' ? 0.24 : 0.05;
    for (let i = 0; i < brazos.length; i++) {
      const dir = i === 0 ? -1 : 1;
      brazos[i].rotation.x = dir * a * 0.75;
      brazos[i].rotation.z = (i === 0 ? -1 : 1) * abre;
      codos[i].rotation.x = -0.18 - Math.max(0, dir * -a) * 0.9 * (v ? 1 : 0);
    }
    const reposo = 1 - Math.min(1, v);
    cuerpo.position.y = Math.abs(Math.sin(t)) * 0.04 * v;
    cuerpo.rotation.y = Math.sin(t) * 0.08 * v;
    torso.scale.y = 1 + Math.sin(t * 0.9) * 0.006 * reposo;
    cab.rotation.y = Math.sin(t * 0.37) * 0.25 * reposo;
    cab.rotation.z = Math.sin(t * 0.23) * 0.05 * reposo;
  };
  g.userData.montar = () => {                          // sentado a caballo: muslos hacia adelante y abiertos, rodillas dobladas, manos en las riendas
    piernas.forEach((p, i) => p.rotation.set(-1.3, 0, i === 0 ? -0.5 : 0.5));
    rodillas.forEach((r) => { r.rotation.x = 1.25; });
    brazos.forEach((b, i) => b.rotation.set(-0.7, 0, (i === 0 ? -1 : 1) * 0.1));
    codos.forEach((c) => { c.rotation.x = -0.9; });
  };
  return g;
}
