import * as THREE from 'three';

// Caballo criollo y carro de madera (low poly angular). Se comparten entre la ciudad y el campo.
export function fabricaCaballos() {
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
    const base = patas.map((p) => p.rotation.x);
    const andar = (fase, amp) => {                                                           // paso o galope: las diagonales se mueven juntas
      const w = Math.sin(fase) * amp;
      patas[0].rotation.x = base[0] + w; patas[3].rotation.x = base[3] + w; patas[1].rotation.x = base[1] - w; patas[2].rotation.x = base[2] - w;
    };
    const animar = (t, pasto = 0, anda = 0) => {                                             // reposo: cabeceo, cola, pasta con la cabeza gacha, a veces rasca el suelo
      const alto = 1 - pasto;
      cuello.rotation.x = -0.85 + Math.sin(t * 0.5) * 0.05 * alto + pasto * 1.5 + Math.sin(t * 2.1) * 0.03 * pasto;
      cuello.rotation.y = Math.sin(t * 0.23) * 0.12 * (1 - pasto * 0.7);
      cabeza.rotation.x = 1.3 + Math.sin(t * 0.7 + 1) * 0.06 * alto - pasto * 0.35;
      cola.rotation.z = Math.sin(t * 0.9) * 0.18 + Math.sin(t * 3.1) * 0.1 * pasto; cola.rotation.x = Math.sin(t * 0.6) * 0.06;
      if (!anda) { patas[1].rotation.x = base[1] + Math.max(0, Math.sin(t * 0.4 - 1)) * 0.35 * Math.max(0, Math.sin(t * 0.13)) * alto; patas[2].rotation.x = base[2] + Math.max(0, Math.sin(t * 0.21 + 2) - 0.7) * 0.5; }
    };
    g.userData.sinXray = true; g.userData.vivo = true; return { g, animar, andar, patas };
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


  return { caballo, carro, Mt, poner, caja, tronco, cono, octa, sombra, loft };
}
