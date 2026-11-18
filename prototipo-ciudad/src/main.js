import * as THREE from 'three';
import { obstaculos, rejilla, CELDA, clave, libre, ocupado, setAgua, personas, choca, alturaEn } from './colision.js';
import { construirCiudad, construirArboles, construirCarros, cargarTexturas } from './ciudad.js';
import { crearPersonaje, siluetear, cargarTelas } from './personaje.js';
import { xray, aplicarXrayEscena } from './xray.js';
import { construirCampo } from './campo.js';
import { elenco, DIALOGOS_CAMPO } from './elenco.js';
import { crearMercado } from './mercado.js';
import { crearMapa } from './mapa.js';
import { instalarGrade, ensuciarEscena } from './suciedad.js';
instalarGrade();

// Dos mundos: la ciudad (por defecto) y el pago del campo (?mundo=campo)
const MUNDO = new URLSearchParams(location.search).get('mundo') === 'campo' ? 'campo' : 'ciudad';
let mundoCampo = null;
await cargarTelas();

// ---------- Datos y escala ----------
const ESC = 0.675;                 // metros por píxel del plano
let PLAZA = [10335, 9485];         // Plaza de la Victoria (se pisa con ciudad.json)
const aMundo = (px, py) => [(px - PLAZA[0]) * ESC, (py - PLAZA[1]) * ESC];

async function cargarPak(url) {
  const r = await fetch(url);
  const buf = await new Response(r.body.pipeThrough(new DecompressionStream('gzip'))).arrayBuffer();
  const n = new DataView(buf).getUint32(0, true);
  const cab = JSON.parse(new TextDecoder().decode(new Uint8Array(buf, 4, n)));
  let off = 4 + n; const out = {};
  for (const p of cab.partes) {
    const raw = new Uint8Array(buf, off, p.bytes), s = new Uint8Array(p.bytes), c = p.bytes / p.registro;
    for (let e = 0; e < p.registro; e++) { const t = e * c; for (let k = 0; k < c; k++) s[k * p.registro + e] = raw[t + k]; }
    out[p.nombre] = s.buffer; off += p.bytes;
  }
  return { manzanas: new Float32Array(out.manzanas), arboles: new Float32Array(out.arboles) };
}

// ---------- Escena, cielo y luz ----------
const renderer = new THREE.WebGLRenderer({ antialias: true, stencil: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.setSize(innerWidth, innerHeight);
renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 0.85;
renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
document.body.prepend(renderer.domElement);
const scene = new THREE.Scene();
const HORIZONTE = new THREE.Color('#f6e6c8');
scene.fog = new THREE.Fog(HORIZONTE, 170, 1800);
scene.background = HORIZONTE;
// Cámara isométrica: ortogonal, a 45° de inclinación y a 45° de giro; la vista gira de a 90° con Q / R
let yaw = Math.PI * 0.75, yawVis = yaw, vista = 13, vistaAct = vista;   // vista = mitad de la altura visible, en metros
const pitch = Math.PI / 4;
const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 1, 700);
function ajustarCamara() {
  const a = innerWidth / innerHeight;
  camera.left = -vistaAct * a; camera.right = vistaAct * a; camera.top = vistaAct; camera.bottom = -vistaAct; camera.updateProjectionMatrix();
}
ajustarCamara();

// cielo en degradé (cálido en el horizonte, celeste arriba)
const domo = new THREE.Mesh(new THREE.SphereGeometry(5500, 24, 12), new THREE.ShaderMaterial({
  side: THREE.BackSide, depthWrite: false, fog: false,
  uniforms: { arriba: { value: new THREE.Color('#8aa9c6') }, abajo: { value: HORIZONTE } },
  vertexShader: 'varying vec3 vP; void main(){ vP = position; gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }',
  fragmentShader: [
    'uniform vec3 arriba; uniform vec3 abajo; varying vec3 vP;',
    'void main() {',
    '  float t = smoothstep(0.0, 0.6, normalize(vP).y);',
    '  gl_FragColor = vec4(mix(abajo, arriba, t), 1.0);',
    '  #include <colorspace_fragment>',
    '}'].join('\n'),
}));
const sky = domo; scene.add(domo);
const sunDir = new THREE.Vector3().setFromSphericalCoords(1, THREE.MathUtils.degToRad(90 - 34), THREE.MathUtils.degToRad(235));

scene.add(new THREE.HemisphereLight('#cfe0f5', '#b99d78', 1.05));
const sol = new THREE.DirectionalLight('#ffe2b4', 3.2);
sol.castShadow = true; sol.shadow.mapSize.set(2048, 2048);
Object.assign(sol.shadow.camera, { left: -70, right: 70, top: 70, bottom: -70, near: 1, far: 500 });
sol.shadow.bias = -0.0004; sol.shadow.normalBias = 0.04;
scene.add(sol, sol.target);

addEventListener('resize', () => {
  renderer.setSize(innerWidth, innerHeight);
  ajustarCamara();
});

// ---------- Suelo: tierra con dos escalas y agua según la máscara del plano ----------
function crearSuelo(T, mascara, ancho, alto) {
  // la textura de tierra que te gustó, con dos escalas para que no se repita, y el mar según la máscara del plano
  const mat = new THREE.MeshStandardMaterial({ map: T.tieC, normalMap: T.tieN, normalScale: new THREE.Vector2(0.8, 0.8), roughness: 1 });
  T.tieC.repeat.set(ancho * ESC / 5, alto * ESC / 5); T.tieN.repeat.copy(T.tieC.repeat);
  mat.onBeforeCompile = (sh) => {
    sh.uniforms.mascara = { value: mascara };
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec2 vMk;').replace('#include <uv_vertex>', '#include <uv_vertex>\nvMk = uv;');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\nvarying vec2 vMk; uniform sampler2D mascara;')
      .replace('#include <map_fragment>', `
        vec4 c1 = texture2D(map, vMapUv);
        vec4 c2 = texture2D(map, vMapUv * 0.09 + vec2(0.31, 0.77));
        vec3 base = c1.rgb * (0.55 + c2.rgb * 1.0) * 1.5;
        vec4 mk = texture2D(mascara, vMk);
        float agua = smoothstep(0.45, 0.8, mk.r) * (1.0 - smoothstep(0.25, 0.55, mk.g));
        vec3 col = mix(base, vec3(0.13, 0.36, 0.46), agua);
        diffuseColor *= vec4(col, 1.0);`);
  };
  const suelo = new THREE.Mesh(new THREE.PlaneGeometry(ancho * ESC, alto * ESC), mat);
  suelo.rotation.x = -Math.PI / 2; suelo.receiveShadow = true; suelo.userData.noXray = true;
  const [cx, cz] = aMundo(ancho / 2, alto / 2); suelo.position.set(cx, 0, cz); scene.add(suelo);
  return suelo;
}

async function prepararAgua(ancho, alto) {
  const img = await new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = './plano/suelo.webp'; });
  const cv = document.createElement('canvas'); cv.width = img.width; cv.height = img.height;
  const c = cv.getContext('2d', { willReadFrequently: true }); c.drawImage(img, 0, 0);
  const datos = c.getImageData(0, 0, cv.width, cv.height).data;
  setAgua((x, z) => {
    const px = x / ESC + PLAZA[0], py = z / ESC + PLAZA[1];
    const ix = Math.floor(px / ancho * cv.width), iy = Math.floor(py / alto * cv.height);
    if (ix < 0 || iy < 0 || ix >= cv.width || iy >= cv.height) return true;      // fuera del plano
    const k = (iy * cv.width + ix) * 4; return datos[k] > 170 && datos[k + 1] < 110;
  });
  const tex = new THREE.CanvasTexture(cv); tex.colorSpace = THREE.NoColorSpace;
  return tex;
}

// ---------- Personajes y misiones ----------
const jugador = crearPersonaje(MUNDO === 'campo'
  ? { estilo: 'poncho', poncho: '#8a5a3a', sombrero: 'ala', colorSombrero: '#4a3a2a', bigote: 'fino', pelo: '#3a2a1c', peinado: 'corto', piel: '#d9a27a' }      // peón rural
  : { estilo: 'levita', ropa: '#2f4a7a', chaleco: '#e0b84e', pantalon: '#9a948a', sombrero: 'copa', bigote: 'fino', pelo: '#4a3220', peinado: 'corto', baston: true });
scene.add(jugador); siluetear(jugador);

function etiqueta(texto) {
  const cv = document.createElement('canvas'); cv.width = 512; cv.height = 96;
  const c = cv.getContext('2d');
  c.font = '500 40px Georgia, serif'; c.textAlign = 'center'; c.textBaseline = 'middle';
  const w = c.measureText(texto).width + 40;
  c.fillStyle = '#f1e6cd'; c.fillRect((512 - w) / 2, 14, w, 68);
  c.strokeStyle = '#2a221b'; c.lineWidth = 3; c.strokeRect((512 - w) / 2, 14, w, 68);
  c.fillStyle = '#2a221b'; c.fillText(texto, 256, 49);
  const tx = new THREE.CanvasTexture(cv); tx.colorSpace = THREE.SRGBColorSpace;
  const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: tx, depthTest: true, transparent: true, toneMapped: false, fog: false }));
  sp.material.sizeAttenuation = false; sp.scale.set(0.2, 0.0375, 1); sp.renderOrder = 10; return sp;
}

const estado = { q: 0, monedas: 0, objetos: new Set(), hab: { arreo: 0 }, arreoVisto: false, arreoUlt: null };
estado.hab.comercio = 0; estado.inv = {}; estado.costo = {};
const nivelArreo = () => 1 + Math.floor(estado.hab.arreo / 6);
const nivelComercio = () => 1 + Math.floor(estado.hab.comercio / 6);
const MISION0 = ['Etapa 1 · Sin nada', 'Conocé a la gente del pago. Fermín, el capataz, reparte las tareas en los corrales de la estancia.'];
let misionCampo = [...MISION0];
const NPCS = [];

function crearNPC(def) {
  let [x, z] = def.xz ?? aMundo(def.px, def.py);
  if (!libre(x, z)) {
    let hallado = false;
    for (let r = 2; r < 80 && !hallado; r += 2)
      for (let a = 0; a < 16 && !hallado; a++) {
        const xx = x + Math.cos(a * Math.PI / 8) * r, zz = z + Math.sin(a * Math.PI / 8) * r;
        if (libre(xx, zz)) { x = xx; z = zz; hallado = true; }
      }
  }
  const g = crearPersonaje(def.aspecto);
  g.position.set(x, alturaEn(x, z), z); scene.add(g); siluetear(g);
  const et = etiqueta(def.nombre); et.position.set(0, 2.3 / (def.aspecto.escala || 1), 0); g.add(et);
  const pers = { x, z, r: 0.5, id: def.id }; personas.push(pers);          // no se los puede atravesar
  const npc = { ...def, g, x, z, et, pers };
  NPCS.push(npc); return npc;
}

const anselmo = { id: 'anselmo', nombre: 'Don Anselmo', px: 10335, py: 9530,
  aspecto: { estilo: 'pregonero', ropa: '#8a5a3a', ropa2: '#f1ead8', pantalon: '#7a7468', sombrero: 'gorra', colorSombrero: '#6f7a68', bigote: 'manubrio', pelo: '#5a3d26', peinado: 'corto' } };
const remedios = { id: 'remedios', nombre: 'Doña Remedios', px: 10024, py: 9380,
  aspecto: { estilo: 'vestido', ropa: '#7a3f8f', ropa2: '#f1e3c4', pelo: '#2a1a12' } };
const sacristan = { id: 'sacristan', nombre: 'El sacristán', px: 9575, py: 9640,
  aspecto: { estilo: 'sotana', sombrero: 'bonete', pelo: '#bdbdb8', piel: '#f0c19a' } };
const paseante = { id: 'paseante', nombre: 'Un paisano', px: 10450, py: 9420,
  aspecto: { estilo: 'poncho', poncho: '#b53a2a', sombrero: 'ala', colorSombrero: '#3b2f27', barba: true, pelo: '#2a1c14', piel: '#d9a27a' } };

const soldado = { id: 'soldado', nombre: 'El centinela', px: 10380, py: 9400,
  aspecto: { estilo: 'levita', ropa: '#2c3e6b', chaleco: '#c9a227', pantalon: '#b3342b', sombrero: 'kepi', bigote: 'manubrio', pelo: '#3a2a1c', peinado: 'corto', prop: 'fusil' } };
const aguatero = { id: 'aguatero', nombre: 'El aguatero', px: 10424, py: 9529,
  aspecto: { estilo: 'poncho', poncho: '#3f7a8a', sombrero: 'ala', colorSombrero: '#b89a6a', barba: true, pelo: '#5a4a3a', prop: 'barril', piel: '#d9a27a' } };
const vendedora = { id: 'vendedora', nombre: 'La pastelera', px: 10276, py: 9567,
  aspecto: { estilo: 'vestido', ropa: '#d18a3a', ropa2: '#f6efdc', pelo: '#3a2415', prop: 'canasta' } };
const lustra = { id: 'lustra', nombre: 'El lustrabotas', px: 10298, py: 9441,
  aspecto: { estilo: 'pregonero', ropa: '#7a8a5a', ropa2: '#efe6d0', pantalon: '#8a7a60', sombrero: 'gorra', colorSombrero: '#5a4a3a', pelo: '#4a3220', peinado: 'puas', escala: 0.72, prop: 'cajon' } };

const TEXTOS = {
  anselmo: () => {
    if (estado.q === 0) return { l: [
      'Buen día, forastero. Soy Anselmo, vendo diarios y noticias en la Plaza de la Victoria. Acá se entera uno de todo antes que nadie.',
      'Mire, justo necesitaba a alguien de confianza. Esta carta es para Doña Remedios, la pulpera. Es urgente y a mí no me da el tiempo.',
      'Se la encuentra hacia el oeste, cerca de una de las iglesias. Pregunte, que todos la conocen.'],
      fin: () => { estado.q = 1; estado.objetos.add('Carta lacrada'); } };
    if (estado.q === 1) return { l: ['¿Todavía acá? La carta es para Doña Remedios, hacia el oeste. Siga la luz rojiza, que yo la marco para usted.'] };
    if (estado.q === 2 || estado.q === 3 && !estado.objetos.has('Medalla de plata')) return { l: ['Si anda por ahí, Doña Remedios me contó que hay un paquete que llevar al sacristán. Hágale el favor.'] };
    if (estado.q === 3) return { l: [
      'Con que la medalla... ¡Esa es la que se había perdido! Ahora sí que me debo una.',
      'Tome estas monedas, se las ganó. Y vuelva cuando quiera: en esta ciudad siempre hay algún mandado.'],
      fin: () => { estado.q = 4; estado.objetos.delete('Medalla de plata'); estado.monedas += 50; } };
    return { l: ['La ciudad crece rápido, ¿vio? Dicen que van a traer más trenes y más gente. Qué tiempos.'] };
  },
  remedios: () => {
    if (estado.q === 0) return { l: ['Estoy ocupada, hijo. Si viene de parte de alguien, que venga con carta.'] };
    if (estado.q === 1) return { l: [
      '¿Una carta de Anselmo? Dámela... Ajá. Ya veo. Siempre con apuros ese hombre.',
      'Mirá, llevale este paquete de yerba al sacristán de la iglesia, más al oeste. Él sabe de qué se trata. Y no lo abras.'],
      fin: () => { estado.q = 2; estado.objetos.delete('Carta lacrada'); estado.objetos.add('Paquete de yerba'); } };
    return { l: ['En la pulpería hay de todo: yerba, caña, tabaco... y chismes, que son gratis.'] };
  },
  sacristan: () => {
    if (estado.q < 2) return { l: ['Silencio, que es casa de Dios. Si busca algo, vuelva más tarde.'] };
    if (estado.q === 2) return { l: [
      'Así que Remedios le mandó yerba. Con la yerba se hacen buenas migas, dicen.',
      'A cambio, llévele esto a Anselmo: es la medalla de plata que se le cayó de la capilla hace unos días.'],
      fin: () => { estado.q = 3; estado.objetos.delete('Paquete de yerba'); estado.objetos.add('Medalla de plata'); } };
    return { l: ['Vaya con Dios. Y cuide la medalla.'] };
  },
  paseante: () => ({ l: [
    'Dicen que el río llegaba hasta acá no hace mucho. Fíjese en la barranca: la ciudad se levantó sobre lo que el agua dejó.',
    'Y no vaya muy lejos hacia las afueras, que las calles se acaban pronto.'] }),
};

Object.assign(TEXTOS, {
  soldado: () => ({ l: [
    'Alto ahí, ciudadano... ah, es usted. Circule nomás, circule.',
    'El Cabildo ya no es lo que era, pero la guardia es la guardia. Cuando cae el sol, esta plaza se pone brava.'] }),
  aguatero: () => ({ l: [
    '¡Agua fresca del río, a un real el balde!',
    'Cada año tengo que bajar más lejos con la carreta: el río se aleja y la ciudad se va comiendo la costa.'] }),
  vendedora: () => ({ l: [
    '¡Pastelitos calientes, criollos y de membrillo!',
    'Si busca a Doña Remedios, vive hacia el oeste, donde la calle se pone más tranquila. Llévele pasteles, que le encantan.'] }),
  lustra: () => ({ l: [
    '¿Le lustro las botas, caballero? Con medio real quedan como espejos.',
    'Desde acá se ve todo: quién entra al Cabildo, quién sale de misa... y quién no paga.'] }),
});

const OBJETIVOS = [
  ['Un encargo en la plaza', 'Hablá con Don Anselmo, el diario de la plaza.'],
  ['La carta urgente', 'Llevale la carta a Doña Remedios, hacia el oeste.'],
  ['Un paquete de yerba', 'Llevale el paquete al sacristán, más al oeste.'],
  ['La medalla de plata', 'Devolvele la medalla a Don Anselmo, en la plaza.'],
  ['Misión cumplida', 'Recorré la ciudad, hay más por descubrir. (Próximamente)'],
];
const objetivoNPC = () => (MUNDO === 'campo' ? null : [anselmo, remedios, sacristan, anselmo, null][estado.q]);

// ---------- Transeúntes: gente de fondo que camina por la plaza y las calles cercanas ----------
const TRANS = [];
const hz = (i) => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };
function crearTranseuntes() {
  const tipos = [
    { estilo: 'levita', ropa: '#7a3a3a', chaleco: '#c4cfa4', pantalon: '#8a8478', sombrero: 'copa', bigote: 'fino', peinado: 'corto', pelo: '#2b2018' },
    { estilo: 'levita', ropa: '#3d5c4a', chaleco: '#e8d9a0', pantalon: '#a09a90', sombrero: 'copa', barba: true, pelo: '#7a7a76', peinado: 'corto' },
    { estilo: 'vestido', ropa: '#2f6f8f', ropa2: '#f1e3c4', pelo: '#4a2c1a' },
    { estilo: 'vestido', ropa: '#b04a3a', ropa2: '#e8d9b0', pelo: '#1f1410' },
    { estilo: 'vestido', ropa: '#4a7a4f', ropa2: '#f4eedc', pelo: '#8a5a2a' },
    { estilo: 'poncho', poncho: '#2f6f8f', sombrero: 'ala', colorSombrero: '#6a4a30', barba: true, pelo: '#2a1c14', piel: '#d9a27a' },
    { estilo: 'poncho', poncho: '#7a3f8f', sombrero: 'ala', colorSombrero: '#2a2420', bigote: 'manubrio', pelo: '#2a1c14', piel: '#cf9a72' },
    { estilo: 'pregonero', ropa: '#4a5a8a', ropa2: '#f1ead8', pantalon: '#8a8478', sombrero: 'gorra', colorSombrero: '#8a5a3a', peinado: 'corto', pelo: '#2b2018' },
    { estilo: 'sotana', sombrero: 'bonete', pelo: '#c9c9c4' },
    { estilo: 'levita', ropa: '#7a5aa0', chaleco: '#e0b84e', pantalon: '#6e6d73', sombrero: 'copa', bigote: 'manubrio', pelo: '#3a2a1c', peinado: 'corto' },
    { estilo: 'pregonero', ropa: '#8a6a3a', ropa2: '#efe6d0', pantalon: '#7a7060', sombrero: 'gorra', colorSombrero: '#4a5a6a', pelo: '#3a2a1c', peinado: 'puas', escala: 0.72 },
    { estilo: 'levita', ropa: '#2c3e6b', chaleco: '#c9a227', pantalon: '#b3342b', sombrero: 'kepi', bigote: 'fino', pelo: '#2b2018', peinado: 'corto', prop: 'fusil' },
    { estilo: 'poncho', poncho: '#8a3a2a', sombrero: 'ala', colorSombrero: '#4a3a2a', prop: 'barril', pelo: '#2a1c14', piel: '#cf9a72' },
    { estilo: 'vestido', ropa: '#6a4a8a', ropa2: '#f1e3c4', pelo: '#2a1a12', prop: 'canasta' },
  ];
  const colorH = (h, s, l) => '#' + new THREE.Color().setHSL(h, s, l).getHexString();
  const pelos = ['#2b2018', '#6a4a2a', '#8a6a40', '#1f1410', '#a9a9a4', '#4a3220'];
  const N = 24;
  for (let i = 0; i < N; i++) {
    const asp = { ...tipos[i % tipos.length] };
    if (i >= tipos.length) {                          // variantes de color para que no se repitan
      if (asp.ropa && asp.estilo !== 'levita') asp.ropa = colorH(hz(i * 3 + 1), 0.42, 0.4);
      if (asp.ropa && asp.estilo === 'levita' && asp.sombrero === 'copa') asp.ropa = colorH(hz(i * 3 + 1), 0.3, 0.3);
      if (asp.poncho) asp.poncho = colorH(hz(i * 5 + 2), 0.5, 0.4);
      asp.pelo = pelos[Math.floor(hz(i + 7) * pelos.length)];
    }
    let x = (hz(i + 1) - 0.5) * 120, z = (hz(i + 9) - 0.5) * 100;
    for (let k = 0; k < 40 && !libre(x, z); k++) { x = (hz(i + k * 3 + 2) - 0.5) * 140; z = (hz(i + k * 5 + 4) - 0.5) * 120; }
    const g = crearPersonaje(asp); g.position.set(x, alturaEn(x, z), z); scene.add(g);
    const pers = { x, z, r: 0.45 }; personas.push(pers);
    TRANS.push({ g, x, z, hx: x, hz: z, tx: x, tz: z, espera: hz(i) * 4, fase: hz(i + 5) * 6, pers });
  }
}
function moverTranseuntes(dt, p) {
  for (const t of TRANS) {
    const d = Math.hypot(t.x - p.x, t.z - p.z);
    t.g.visible = d < 140; t.pers.activo = d < 140; if (d > 140) continue;
    if (t.espera > 0) { t.espera -= dt; t.g.userData.animar(t.fase += dt * 0.6, 0); continue; }
    let dx = t.tx - t.x, dz = t.tz - t.z, l = Math.hypot(dx, dz);
    if (l < 0.6) {                                  // elegir otro destino cerca de su lugar, solo en terreno libre
      for (let k = 0; k < 6; k++) {
        const a = Math.random() * 6.28, r = 8 + Math.random() * 28, nx = t.hx + Math.cos(a) * r, nz = t.hz + Math.sin(a) * r;
        let ok = libre(nx, nz); for (let s = 1; s < 8 && ok; s++) ok = libre(t.x + (nx - t.x) * s / 8, t.z + (nz - t.z) * s / 8);
        if (ok) { t.tx = nx; t.tz = nz; break; }
      }
      t.espera = 1 + Math.random() * 4; continue;
    }
    const v = 1.3 * dt, mx = dx / l * v, mz = dz / l * v;
    if (libre(t.x + mx, t.z + mz) && !choca(t.x + mx, t.z + mz, 0.4, t.pers)) { t.x += mx; t.z += mz; } else { t.tx = t.x; t.tz = t.z; t.espera = 0.6; }
    t.pers.x = t.x; t.pers.z = t.z;
    t.g.position.set(t.x, alturaEn(t.x, t.z), t.z);
    let da = Math.atan2(dx, dz) - t.g.rotation.y; da = Math.atan2(Math.sin(da), Math.cos(da)); t.g.rotation.y += da * Math.min(1, dt * 6);
    t.g.userData.animar(t.fase += dt * 7.5, 0.8);
  }
}

// ---------- HUD ----------
const $ = (id) => document.getElementById(id);
function actualizarHUD() {
  if (MUNDO === 'campo') { $('m-tit').textContent = misionCampo[0]; $('m-txt').textContent = misionCampo[1]; }
  else { const [t, x] = OBJETIVOS[estado.q]; $('m-tit').textContent = t; $('m-txt').textContent = x; }
  $('monedas').textContent = estado.monedas;
  const inv = Object.entries(estado.inv).filter(([, n]) => n > 0).map(([id, n]) => `${mercado ? mercado.porId(id).nombre : id} ×${n}`);
  $('objetos').textContent = [...estado.objetos, ...inv, ...(MUNDO === 'campo' ? [`Arreo nv ${nivelArreo()}`, `Comercio nv ${nivelComercio()}`] : [])].join(' · ');
}

let dialogo = null;
let camPrevia = null;
const foco = { x: 0, z: 8 };
function abrirDialogo(npc) {
  const d = TEXTOS[npc.id](); dialogo = { npc, lineas: d.l, i: 0, fin: d.fin };
  camPrevia = { vista };
  const pj = jugador.position;
  vista = 7.5;
  jugador.rotation.y = Math.atan2(npc.x - pj.x, npc.z - pj.z);
  document.body.classList.add('hablando');
  $('dialogo').style.display = 'block'; $('aviso').style.display = 'none';
  $('d-nombre').textContent = npc.nombre; $('d-texto').textContent = d.l[0];
}
function avanzarDialogo() {
  if (!dialogo) return;
  dialogo.i++;
  if (dialogo.i >= dialogo.lineas.length) {
    const f = dialogo.fin; dialogo = null;
    $('dialogo').style.display = 'none'; document.body.classList.remove('hablando');
    if (camPrevia) { vista = camPrevia.vista; camPrevia = null; }
    if (f) f(); actualizarHUD();
  } else $('d-texto').textContent = dialogo.lineas[dialogo.i];
}

// ---------- Pulpería: compra y venta con precios que se mueven ----------
let mercado = null;
function dibujarTienda() {
  const niv = nivelComercio();
  $('t-info').textContent = `Tenés $${estado.monedas} · Comercio nivel ${niv} (a más nivel, mejor precio). Los precios cambian con las noticias del camino.`;
  const filas = mercado.bienes.map((b) => {
    const c = mercado.compra(b, niv), v = mercado.venta(b, niv), n = estado.inv[b.id] || 0, rel = b.precio / b.base;
    const tend = rel > 1.12 ? '<span class="sube">▲ cara</span>' : rel < 0.9 ? '<span class="baja">▼ barata</span>' : '·';
    return `<tr><td>${b.nombre} <small style="display:inline">(tenés ${n})</small></td><td>${tend}</td><td>$${c} <button data-c="${b.id}" ${estado.monedas < c ? 'disabled' : ''}>Comprar</button></td><td>$${v} <button data-v="${b.id}" ${n < 1 ? 'disabled' : ''}>Vender</button></td></tr>`;
  }).join('');
  $('t-tabla').innerHTML = '<tr><th>Mercadería</th><th>Mercado</th><th>Isidro vende a</th><th>Isidro compra a</th></tr>' + filas;
}
function operar(tipo, id) {
  const b = mercado.porId(id), niv = nivelComercio(); let msg = '';
  if (tipo === 'c') {
    const c = mercado.compra(b, niv); if (estado.monedas < c) return;
    const n = estado.inv[id] || 0; estado.costo[id] = ((estado.costo[id] || 0) * n + c) / (n + 1);
    estado.monedas -= c; estado.inv[id] = n + 1; msg = `Compraste ${b.nombre} por $${c}.`;
  } else {
    const n = estado.inv[id] || 0; if (n < 1) return;
    const v = mercado.venta(b, niv), gan = v - (estado.costo[id] || v), antes = nivelComercio();
    estado.monedas += v; estado.inv[id] = n - 1; estado.hab.comercio += 0.5 + Math.max(0, gan) * 0.4;
    msg = `Vendiste ${b.nombre} por $${v}` + (gan > 0 ? ` (ganancia $${Math.round(gan)}).` : gan < 0 ? ` (perdiste $${Math.round(-gan)}).` : '.');
    if (nivelComercio() > antes) msg += ` Comercio sube a nivel ${nivelComercio()}.`;
  }
  $('t-msg').textContent = msg; dibujarTienda(); actualizarHUD();
}
$('t-tabla').addEventListener('click', (e) => { const t = e.target; if (t.dataset && t.dataset.c) operar('c', t.dataset.c); else if (t.dataset && t.dataset.v) operar('v', t.dataset.v); });
function abrirTienda(npc) {
  dialogo = { npc, lineas: [], i: 0, tienda: true }; document.body.classList.add('hablando');
  $('t-msg').textContent = 'Esc o E para salir'; dibujarTienda(); $('tienda').style.display = 'block';
  tiendaT = 0;
}
function cerrarTienda() {
  $('tienda').style.display = 'none'; document.body.classList.remove('hablando');
  dialogo = null; if (camPrevia) { vista = camPrevia.vista; camPrevia = null; } actualizarHUD();
}
let tiendaT = 0;

// ---------- Control ----------
const teclas = new Set();
addEventListener('keydown', (e) => {
  const k = e.key.toLowerCase();
  if (dialogo && dialogo.tienda) { if (k === 'e' || k === 'escape') cerrarTienda(); e.preventDefault(); return; }
  if (dialogo && (k === 'e' || k === ' ' || k === 'enter')) { avanzarDialogo(); e.preventDefault(); return; }
  if (k === 'e' && cercano && !dialogo) { abrirDialogo(cercano); return; }
  if (k === 'e' && cercaTq && !dialogo) { mundoCampo.fauna.arreo.tranquera.alternar(); return; }
  if (k === 'm' && mapaCampo && !dialogo) { mapaGrande = !mapaGrande; $('mapa-g').style.display = mapaGrande ? 'block' : 'none'; return; }
  if (k === 'escape' && mapaGrande) { mapaGrande = false; $('mapa-g').style.display = 'none'; return; }
  if (k === 'q') { yaw -= Math.PI / 2; return; }
  if (k === 'r') { yaw += Math.PI / 2; return; }
  teclas.add(k);
  if (['arrowup', 'arrowdown', 'arrowleft', 'arrowright', ' '].includes(k)) e.preventDefault();
});
addEventListener('keyup', (e) => teclas.delete(e.key.toLowerCase()));
addEventListener('blur', () => teclas.clear());
renderer.domElement.addEventListener('wheel', (e) => { if (!dialogo) vista = Math.max(6, Math.min(45, vista + e.deltaY * 0.012)); e.preventDefault(); }, { passive: false });
setTimeout(() => $('ayuda').classList.add('ido'), 25000);

// ---------- Minimapa ----------
const mc = $('mapa-c').getContext('2d');
let mapaCampo = null, mapaGrande = false;
function dibujarMapa() {
  if (mapaCampo) { mapaCampo.dibujarMini(jugador.position.x, jugador.position.z, jugador.rotation.y, yawVis); if (mapaGrande) mapaCampo.dibujarGrande(jugador.position.x, jugador.position.z, jugador.rotation.y, yawVis, NPCS); return; }
  const W = 300, esc = 0.9;
  mc.fillStyle = '#d8c7a4'; mc.fillRect(0, 0, W, W);
  mc.save(); mc.translate(W / 2, W / 2);
  mc.fillStyle = '#9b8863';
  const px = jugador.position.x, pz = jugador.position.z, rad = (W / 2) / esc;
  for (let cx = Math.floor((px - rad) / CELDA); cx <= Math.floor((px + rad) / CELDA); cx++)
    for (let cz = Math.floor((pz - rad) / CELDA); cz <= Math.floor((pz + rad) / CELDA); cz++) {
      const l = rejilla.get(clave(cx, cz)); if (!l) continue;
      for (const id of l) { const o = obstaculos[id]; if (o.dib === frame) continue; o.dib = frame;
        mc.beginPath(); o.pts.forEach((p, i) => { const X = (p[0] - px) * esc, Y = (p[1] - pz) * esc; i ? mc.lineTo(X, Y) : mc.moveTo(X, Y); });
        mc.closePath(); mc.fill(); }
    }
  const obj = objetivoNPC();
  if (obj) { const npc = NPCS.find(n => n.id === obj.id);
    let dx = (npc.x - px), dz = (npc.z - pz); const d = Math.hypot(dx, dz), max = (W / 2 - 14) / esc;
    if (d > max) { dx *= max / d; dz *= max / d; }
    mc.fillStyle = '#a03220'; mc.beginPath(); mc.arc(dx * esc, dz * esc, 7, 0, 7); mc.fill(); }
  mc.fillStyle = '#2a221b'; mc.beginPath(); mc.arc(0, 0, 5, 0, 7); mc.fill();
  mc.restore();
}

// ---------- Bucle ----------
const reloj = new THREE.Clock();
let cercaTq = false;
let cercano = null, frame = 0, andar = 0, vel = 0, haz = null;
const adelante = new THREE.Vector3(), derecha = new THREE.Vector3();
const jugadorP = { x: 0, z: 8, r: 0.42 }; personas.push(jugadorP);

function mover(dt) {
  if (dialogo) { vel *= 0.8; return; }
  let ix = 0, iz = 0;
  if (teclas.has('w') || teclas.has('arrowup')) iz += 1;
  if (teclas.has('s') || teclas.has('arrowdown')) iz -= 1;
  if (teclas.has('d') || teclas.has('arrowright')) ix += 1;
  if (teclas.has('a') || teclas.has('arrowleft')) ix -= 1;
  if (!ix && !iz) { vel *= 0.8; return; }
  adelante.set(-Math.cos(yawVis), 0, -Math.sin(yawVis)); derecha.set(-adelante.z, 0, adelante.x);          // a la derecha de la pantalla
  const dx = adelante.x * iz + derecha.x * ix, dz = adelante.z * iz + derecha.z * ix;
  const len = Math.hypot(dx, dz) || 1, corre = teclas.has('shift'), v = (corre ? 9 : 4.6) * dt;
  const mx = dx / len * v, mz = dz / len * v;
  const p = jugador.position;
  if (libre(p.x + mx, p.z) && !choca(p.x + mx, p.z, 0.42, jugadorP)) p.x += mx;
  if (libre(p.x, p.z + mz) && !choca(p.x, p.z + mz, 0.42, jugadorP)) p.z += mz;
  jugadorP.x = p.x; jugadorP.z = p.z;
  const rumbo = Math.atan2(dx, dz);
  let d = rumbo - jugador.rotation.y; d = Math.atan2(Math.sin(d), Math.cos(d)); jugador.rotation.y += d * Math.min(1, dt * 12);
  andar += dt * (corre ? 12 : 8.5); vel = corre ? 1.35 : 1;
}

function tick() { actualizar(Math.min(reloj.getDelta(), 0.05)); requestAnimationFrame(tick); }

function actualizar(dt, sinRender) {
  frame++;
  yawVis += (yaw - yawVis) * Math.min(1, dt * 7);
  vistaAct += (vista - vistaAct) * Math.min(1, dt * 8); ajustarCamara();
  mover(dt);
  jugador.userData.animar(andar, vel);
  const p = jugador.position;
  jugadorP.x = p.x; jugadorP.z = p.z;
  p.y += (alturaEn(p.x, p.z) - p.y) * Math.min(1, dt * 14);                 // sube y baja de la vereda
  moverTranseuntes(dt, p);
  if (mercado) { mercado.actualizar(dt); if (dialogo && dialogo.tienda && (tiendaT += dt) > 1.5) { tiendaT = 0; dibujarTienda(); } }
  if (mundoCampo) { mundoCampo.actualizar(dt, p, frame, yawVis); if (mundoCampo.fauna) mundoCampo.fauna.actualizar(dt, p, frame); }
  for (const c of (window.__caballos || [])) if (Math.hypot(c.x - p.x, c.z - p.z) < 90) c.animar(c.fase + frame * 0.02);
  if (!window.__ov) {
    // la cámara está lejos y alta: no choca con nada. En un diálogo se centra entre los dos personajes.
    let fx = p.x, fz = p.z;
    if (dialogo) { fx = (p.x + dialogo.npc.x) / 2; fz = (p.z + dialogo.npc.z) / 2; }
    foco.x += (fx - foco.x) * Math.min(1, dt * 6); foco.z += (fz - foco.z) * Math.min(1, dt * 6);
    const D = 70, cp = Math.cos(pitch);
    camera.position.set(foco.x + Math.cos(yawVis) * cp * D, p.y + 1 + Math.sin(pitch) * D, foco.z + Math.sin(yawVis) * cp * D);
    camera.lookAt(foco.x, p.y + 1, foco.z);
    xray.uJug.value.set(p.x, p.y + 1, p.z);
    xray.uDir.value.set(foco.x - camera.position.x, p.y + 1 - camera.position.y, foco.z - camera.position.z).normalize();   // dirección cámara→jugador
    xray.uDir.value.set(-Math.cos(yawVis) * cp, -Math.sin(pitch), -Math.sin(yawVis) * cp);
  }
  // el sol y su sombra siguen al jugador
  sol.target.position.set(p.x, 0, p.z); sol.position.set(p.x + sunDir.x * 160, sunDir.y * 160, p.z + sunDir.z * 160);
  sky.position.set(camera.position.x, 0, camera.position.z);
  cercano = null; let mejor = 4.5;
  for (const n of NPCS) {
    const d = Math.hypot(n.x - p.x, n.z - p.z);
    const ct = n.sigue !== undefined && mundoCampo && mundoCampo.fauna ? mundoCampo.fauna.carretas[n.sigue] : null;
    if (ct) {                                                         // camina junto a su carreta, al costado de los bueyes
      const a = ct.g.rotation.y, s1 = Math.sin(a), c1 = Math.cos(a);
      n.x = ct.g.position.x + 2.2 * c1 + 5.8 * s1; n.z = ct.g.position.z - 2.2 * s1 + 5.8 * c1;
      n.g.position.set(n.x, mundoCampo.altura(n.x, n.z), n.z); n.g.rotation.y = a; n.pers.x = n.x; n.pers.z = n.z;
      n.g.userData.animar(frame * 0.17, 0.8);
    } else {
      if (d < 12) n.g.rotation.y = Math.atan2(p.x - n.x, p.z - n.z);
      n.g.userData.animar(frame * 0.02, 0);
    }
    n.et.visible = d < 28;
    if (d < mejor) { mejor = d; cercano = n; }
  }
  const av = $('aviso');
  const tq = mundoCampo && mundoCampo.fauna ? mundoCampo.fauna.arreo.tranquera : null;
  cercaTq = !!tq && !cercano && !dialogo && Math.hypot(tq.x - p.x, tq.z - p.z) < 5.5;
  if (cercano && !dialogo) { av.style.display = 'block'; $('aviso-txt').textContent = 'Hablar con ' + cercano.nombre; }
  else if (cercaTq) { av.style.display = 'block'; $('aviso-txt').textContent = tq.abierta ? 'Cerrar la tranquera' : 'Abrir la tranquera'; }
  else av.style.display = 'none';
  const obj = objetivoNPC();
  if (obj && haz) {
    const n = NPCS.find(q => q.id === obj.id);
    haz.visible = Math.hypot(n.x - p.x, n.z - p.z) > 16;
    haz.position.set(n.x, 40, n.z); haz.material.opacity = 0.35 + Math.sin(performance.now() / 400) * 0.1;
  } else if (haz) haz.visible = false;
  if (frame % 4 === 0) dibujarMapa();
  if (!sinRender) renderer.render(scene, camera);
}

// ---------- Arranque ----------
async function arranqueCampo() {
  const T = await cargarTexturas();
  const campo = await construirCampo({ scene, T });
  mundoCampo = campo;
  const [ix, iz] = campo.inicio;
  jugador.position.set(ix, campo.altura(ix, iz), iz); jugadorP.x = ix; jugadorP.z = iz;
  foco.x = ix; foco.z = iz;
  campo.iniciar();
  const { construirFauna } = await import('./fauna.js');
  campo.fauna = await construirFauna({ scene, campo });
  Object.assign(TEXTOS, DIALOGOS_CAMPO);
  // Tarea de Fermín: arreo de vacas (primer minijuego)
  const ar = campo.fauna.arreo;
  ar.nivel = nivelArreo;
  ar.hud = (txt) => { misionCampo = ['Tarea · Arreo de vacas', txt]; actualizarHUD(); };
  ar.onFin = (r) => {
    const antes = nivelArreo(); estado.monedas += r.pago; estado.hab.arreo += r.xp; estado.arreoUlt = r;
    misionCampo = r.ok ? ['Tarea cumplida', 'Hablá con Fermín si querés otra tanda de vacas.'] : ['Se acabó el tiempo', 'Metiste ' + r.dentro + ' de ' + ar.total + '. Hablá con Fermín para intentarlo de nuevo.'];
    campo.fauna.decir((r.ok ? `¡Todas adentro! Ganaste $${r.pago}.` : `Tiempo. Cobrás $${r.pago} por ${r.dentro} vacas.`) + (nivelArreo() > antes ? ` Arreo sube a nivel ${nivelArreo()}.` : ''));
    actualizarHUD();
  };
  mercado = crearMercado((t) => campo.fauna.decir(t));
  const lugar = (n, kx, ky) => { const [x, z] = campo.K(kx, ky); return { n, x, z }; };
  const fer = () => NPCS.find((n) => n.id === 'fermin');
  mapaCampo = crearMapa({
    campo, cvMini: $('mapa-c'), cvGrande: $('mapa-gc'),
    lugares: [lugar('Estancia La Soledad', 0.75, 0.75), lugar('Ranchos de peones', 0.59, 0.9), lugar('Posta y pulpería', 2.15, 1.25), lugar('Rancho abandonado', 1.0, 1.0),
      lugar('Fortín', 0.12, 1.15), lugar('Puesto del baqueano', 0.55, 1.85), lugar('Rancho de Casiana', 1.17, 0.5), lugar('Laguna', 1.7, 2.05), lugar('Monte', 2.3, 0.45),
      () => ({ n: 'Corral', x: ar.rodeo[0], z: ar.rodeo[1] })],
    destino: () => (ar.activo ? { n: 'Corral', x: ar.rodeo[0], z: ar.rodeo[1] } : fer() ? { n: 'Fermín', x: fer().x, z: fer().z } : null),
  });
  TEXTOS.isidro = () => ({
    l: estado.pulperiaVista
      ? ['Pase, pase. Mire cómo anda la yerba hoy.']
      : ['Buenas, buenas. Tengo yerba, tabaco, caña, tela y sal. Los precios cambian según llegue o no la carreta.', 'Yo le vendo un poco más caro de lo que le compro, así vivo. Pero el que sabe tratar, se va ganando mejor precio. Mire, mire.'],
    fin: () => { estado.pulperiaVista = true; abrirTienda(NPCS.find((n) => n.id === 'isidro')); } });
  TEXTOS.fermin = () => {
    if (ar.activo) return { l: ['Las vacas no se arrean solas. Rodeá desde atrás: se van de donde vos venís.', 'Llevalas al corral, el de la bandera roja, al sur de la estancia, y cerrá la tranquera.'] };
    const u = estado.arreoUlt;
    const l = !estado.arreoVisto
      ? ['Escuchame bien: acá se llega temprano y se trabaja hasta que yo diga. Si hacés bien lo tuyo, nos vamos a llevar bien.',
         'Para empezar, hay cinco vacas sueltas en el potrero. Llevalas al corral, el de la bandera roja, al sur de la estancia, y cerrá la tranquera. Cuanto más rápido, mejor te pago.',
         'Un consejo: la vaca se va de donde la ves venir. Ponete del lado contrario al corral y empujala despacio.']
      : u && u.ok ? [`Bien, muchacho. Ahí van tus $${u.pago}. Hay otra tanda de vacas sueltas, ¿te animás?`]
      : ['Así no se trabaja, che. Pero se aprende. Otra tanda de vacas anda suelta: probá de nuevo.'];
    return { l, fin: () => { estado.arreoVisto = true; ar.iniciar(jugador.position); misionCampo = ['Tarea · Arreo de vacas', 'Arreá las 5 vacas al corral y cerrá la tranquera.']; } };
  };
  elenco(campo.K).forEach(crearNPC);                       // los quince personajes con nombre
  $('m-tit').textContent = 'Etapa 1 · Sin nada';
  $('m-txt').textContent = 'Conocé a la gente del pago. Fermín, el capataz, reparte las tareas en los corrales de la estancia.';
  $('monedas').textContent = '0'; $('objetos').textContent = '';
  window.__juego = { estado, NPCS, jugador, scene, camera, renderer, teclas, campo,
    cam: (y, p, v) => { yaw = yawVis = y; vista = vistaAct = v || vista; },
    libre, ocupado,
    simular: (seg) => { for (let t = 0; t < seg; t += 0.05) actualizar(0.05, true); actualizar(0.001); } };
  ensuciarEscena(scene);
  aplicarXrayEscena(scene);
  $('cargando').classList.add('fuera');
  requestAnimationFrame(tick);
}

(async () => {
  if (MUNDO === 'campo') { await arranqueCampo(); return; }
  const [meta, datos] = await Promise.all([
    fetch('./datos/ciudad.json').then(r => r.json()), cargarPak('./datos/ciudad.pak')]);
  PLAZA = meta.plano.plaza;
  const [ancho, alto] = [meta.plano.ancho, meta.plano.alto];
  const [mascara, T] = await Promise.all([prepararAgua(ancho, alto), cargarTexturas()]);
  crearSuelo(T, mascara, ancho, alto);
  await construirCiudad({ scene, manzanas: datos.manzanas, iglesias: meta.iglesias, aMundo, ESC });
  construirArboles(scene, datos.arboles, aMundo);
  const caballos = construirCarros(scene, libre, aMundo);
  window.__caballos = caballos;

  [anselmo, remedios, sacristan, paseante, soldado, aguatero, vendedora, lustra].forEach(crearNPC);
  crearTranseuntes();
  jugador.position.set(0, 0, 8);
  if (!libre(0, 8)) for (let r = 2; r < 60; r += 2) { let ok = false;
    for (let a = 0; a < 16; a++) { const x = Math.cos(a * Math.PI / 8) * r, z = 8 + Math.sin(a * Math.PI / 8) * r; if (libre(x, z)) { jugador.position.set(x, 0, z); ok = true; break; } }
    if (ok) break; }

  haz = new THREE.Mesh(new THREE.CylinderGeometry(0.45, 0.45, 80, 10, 1, true),
    new THREE.MeshBasicMaterial({ color: '#ff7a3a', transparent: true, opacity: 0.3, depthWrite: false, side: THREE.DoubleSide, fog: false, toneMapped: false }));
  scene.add(haz);

  window.__juego = { estado, NPCS, jugador, scene, camera, renderer, teclas,
    cam: (y, p, v) => { yaw = yawVis = y; vista = vistaAct = v || vista; },
    libre, ocupado,
    simular: (seg) => { for (let t = 0; t < seg; t += 0.05) actualizar(0.05, true); actualizar(0.001); } };
  actualizarHUD();
  foco.x = jugador.position.x; foco.z = jugador.position.z;
  ensuciarEscena(scene);
  aplicarXrayEscena(scene);                               // los edificios se recortan cuando tapan al personaje
  $('cargando').classList.add('fuera');
  requestAnimationFrame(tick);
})().catch((e) => { $('cargando').textContent = 'Error: ' + e.message; console.error(e); });
