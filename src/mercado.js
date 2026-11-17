// Mercado de la pulpería: los precios se mueven solos (ruido + noticias que los empujan y se disipan).
// El comercio sube con el uso: a más nivel, menos diferencia entre lo que paga y lo que cobra Isidro.
const BIENES = [
  { id: 'yerba', nombre: 'Yerba', base: 6 },
  { id: 'tabaco', nombre: 'Tabaco', base: 8 },
  { id: 'cana', nombre: 'Caña', base: 11 },
  { id: 'tela', nombre: 'Tela', base: 15 },
  { id: 'sal', nombre: 'Sal', base: 3 },
];

const NOTICIAS = [
  { f: 0.65, t: (n) => `Llegó una carreta cargada: ${n} está barata.` },
  { f: 1.55, t: (n) => `Se corta el camino: escasea ${n}.` },
  { f: 0.75, t: (n) => `Hay mucha oferta de ${n} en la pulpería.` },
  { f: 1.4, t: (n) => `Pasó una tropa y se llevó todo el stock de ${n}.` },
];

export function crearMercado(aviso) {
  const bienes = BIENES.map((b) => ({ ...b, ruido: 1, mult: 1, precio: b.base, antes: b.base, hist: [b.base] }));
  const m = { bienes, t: 0, prox: 50 + Math.random() * 40, porId: (id) => bienes.find((b) => b.id === id) };
  const recalcular = () => { for (const b of bienes) b.precio = Math.max(1, Math.round(b.base * b.mult * b.ruido)); };

  m.compra = (b, nivel) => Math.max(b.precio + 1, Math.round(b.precio * Math.max(1.05, 1.18 - 0.012 * nivel)));
  m.venta = (b, nivel) => Math.max(1, Math.round(b.precio * Math.min(0.9, 0.78 + 0.012 * nivel)));

  m.actualizar = (dt) => {
    m.t += dt;
    for (const b of bienes) {
      b.ruido += (1 - b.ruido) * 0.05 * dt + (Math.random() - 0.5) * 0.05 * Math.sqrt(dt);
      b.ruido = Math.max(0.8, Math.min(1.25, b.ruido));
      b.mult += (1 - b.mult) * 0.012 * dt;                       // las noticias se disipan en un minuto o dos
    }
    m.prox -= dt;
    if (m.prox <= 0) {
      m.prox = 70 + Math.random() * 60;
      const b = bienes[Math.floor(Math.random() * bienes.length)], n = NOTICIAS[Math.floor(Math.random() * NOTICIAS.length)];
      b.mult = Math.max(0.55, Math.min(1.8, b.mult * n.f));
      if (aviso) aviso(n.t(b.nombre));
    }
    const viejos = bienes.map((b) => b.precio);
    recalcular();
    m.hist = (m.hist || 0) + dt;
    if (m.hist >= 10) { m.hist = 0; bienes.forEach((b) => { b.antes = b.hist[Math.max(0, b.hist.length - 3)]; b.hist.push(b.precio); if (b.hist.length > 24) b.hist.shift(); }); }
    return viejos;
  };
  recalcular();
  return m;
}
