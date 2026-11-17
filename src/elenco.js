// Los quince personajes con nombre del pago: lugar, aspecto y presentación.
// Los textos son provisorios: cómo funcionan por dentro (memoria, vínculos, IA) se diseña después.

// K = conversor de km del mapa a metros del mundo (campo.K)
export function elenco(K) {
  const [ex, ez] = K(0.75, 0.75);     // estancia La Soledad
  const [rx, rz] = K(0.59, 0.9);      // rancho de peones
  const [px, pz] = K(2.15, 1.25);     // posta y pulpería
  const [fx, fz] = K(0.12, 1.15);     // fortín
  const [bx, bz] = K(0.55, 1.85);     // puesto del baqueano
  const [cx, cz] = K(1.17, 0.5);      // rancho de Casiana, junto al arroyo
  return [
    { id: 'celestino', nombre: 'Don Celestino Aguirre', xz: [ex - 3, ez + 6.8],
      aspecto: { estilo: 'levita', ropa: '#34343a', chaleco: '#9a4a30', pantalon: '#6e6b66', sombrero: 'ala', colorSombrero: '#1f1b18', barba: true, pelo: '#a8a8a4', peinado: 'corto', baston: true } },
    { id: 'mercedes', nombre: 'Doña Mercedes', xz: [ex - 19, ez - 9],
      aspecto: { estilo: 'vestido', ropa: '#5b6f5a', ropa2: '#efe6d0', pelo: '#6a4a2a' } },
    { id: 'carmen', nombre: 'Carmen', xz: [ex + 11, ez + 10],
      aspecto: { estilo: 'vestido', ropa: '#c9748a', ropa2: '#f4eedc', pelo: '#2a1a12', peinado: 'largo', escala: 0.9 } },
    { id: 'fermin', nombre: 'Fermín Lucero', xz: [ex + 31, ez + 30],
      aspecto: { estilo: 'poncho', poncho: '#4a4a50', sombrero: 'ala', colorSombrero: '#2a2420', bigote: 'manubrio', pelo: '#1f1a17', piel: '#c99a74' } },
    { id: 'anacleto', nombre: 'Don Anacleto', xz: [ex - 26, ez + 9.5],
      aspecto: { estilo: 'poncho', poncho: '#6a4a30', sombrero: 'ala', colorSombrero: '#8a7a5a', barba: true, pelo: '#dcdcd8', piel: '#c99a74', prop: 'guitarra', escala: 0.97 } },
    { id: 'nicasio', nombre: 'Nicasio', xz: [rx + 3, rz + 9],
      aspecto: { estilo: 'pregonero', ropa: '#6a7a4a', ropa2: '#efe6d0', pantalon: '#7a7060', sombrero: 'ala', colorSombrero: '#d8c07a', peinado: 'puas', pelo: '#3a2a1c', piel: '#d9a27a' } },
    { id: 'isidro', nombre: 'Don Isidro Barral', xz: [px - 0.5, pz - 14.6],
      aspecto: { estilo: 'pregonero', ropa: '#3a4a6a', ropa2: '#f1ead8', pantalon: '#5a5348', sombrero: 'gorra', colorSombrero: '#2a2a2a', bigote: 'manubrio', pelo: '#4a3220', peinado: 'corto' } },
    { id: 'juana', nombre: 'Doña Juana', xz: [px + 12, pz + 5],
      aspecto: { estilo: 'vestido', ropa: '#a8553a', ropa2: '#f4eedc', pelo: '#3a2415', prop: 'canasta' } },
    { id: 'cipriano', nombre: 'Cipriano Baigorria', xz: [px - 10, pz + 4],
      aspecto: { estilo: 'levita', ropa: '#5a4a3a', chaleco: '#d9b45a', pantalon: '#8a8478', sombrero: 'ala', colorSombrero: '#3a2e26', bigote: 'fino', pelo: '#3a2a1c', peinado: 'corto' } },
    { id: 'wenceslao', nombre: 'Don Wenceslao Ibáñez', xz: [bx - 6, bz + 4],
      aspecto: { estilo: 'poncho', poncho: '#4a5a4a', sombrero: 'ala', colorSombrero: '#5a4a3a', barba: true, pelo: '#6e6e6a', piel: '#b98a66' } },
    { id: 'casiana', nombre: 'Casiana', xz: [cx + 5, cz + 6],
      aspecto: { estilo: 'vestido', ropa: '#4a3a5a', ropa2: '#d8c9a8', pelo: '#9a9a96', peinado: 'rodete', prop: 'canasta', escala: 0.95 } },
    { id: 'remigio', nombre: 'Don Remigio Páez', xz: [px - 8, pz - 4.8],
      aspecto: { estilo: 'levita', ropa: '#2a2d3a', chaleco: '#8a8a86', pantalon: '#5a5a60', sombrero: 'copa', bigote: 'manubrio', pelo: '#3a3a3a', peinado: 'corto', baston: true } },
    { id: 'obregon', nombre: 'Teniente Obregón', xz: [fx + 2, fz - 3],
      aspecto: { estilo: 'levita', ropa: '#2c3e6b', chaleco: '#c9a227', pantalon: '#b3342b', sombrero: 'kepi', bigote: 'fino', pelo: '#2b2018', peinado: 'corto' } },
    { id: 'lorenzo', nombre: 'Lorenzo', xz: [px + 26, pz + 2],
      aspecto: { estilo: 'poncho', poncho: '#7a5a3a', sombrero: 'ala', colorSombrero: '#8a6a4a', pelo: '#2a1c14', peinado: 'largo', piel: '#c2906a' } },
    { id: 'pascual', nombre: 'Pascual', xz: [px - 40, pz - 10], sigue: 0,       // camina junto a la primera carreta
      aspecto: { estilo: 'pregonero', ropa: '#8a6a3a', ropa2: '#efe6d0', pantalon: '#7a6a50', sombrero: 'ala', colorSombrero: '#6a5a3a', bigote: 'manubrio', pelo: '#3a2a1c', peinado: 'corto' } },
  ];
}

export const DIALOGOS_CAMPO = {
  celestino: () => ({ l: ['Vos sos el muchacho nuevo. En La Soledad el que trabaja bien no pasa hambre, y el que no, se va por donde vino.', 'Hablá con Fermín, que él reparte las tareas. Y cuidá mis caballos como si fueran tuyos.'] }),
  mercedes: () => ({ l: ['Pasá, pasá, que tenés cara de no haber probado bocado. Siempre sobra algo de guiso para los peones.', 'Mi marido es duro, pero no es mal hombre. Lo preocupan más las deudas que los peones.'] }),
  carmen: () => ({ l: ['¿Es verdad que en Buenos Aires hay calles empedradas y teatros? Me lo contó un carretero y no le creí.', 'Algún día me voy de acá. Pero no se lo cuentes a mi padre.'] }),
  fermin: () => ({ l: ['Escuchame bien: acá se llega temprano y se trabaja hasta que yo diga. Si hacés bien lo tuyo, nos vamos a llevar bien.', 'Hoy hay faena en el corral grande. Ya veremos qué sabés hacer.'] }),
  anacleto: () => ({ l: ['Mirá, muchacho, un caballo no se doma a los gritos. Se lo escucha primero, y recién después se le habla.', 'Cuando te sientas listo, venite al corral. Y a la noche, si hay guitarra, hay versos.'] }),
  nicasio: () => ({ l: ['¡Che, qué tal! Yo soy Nicasio, hace dos años que estoy acá. Sentate junto al fogón, que hay mate.', 'Si me acompañás en la faena te enseño a sacar un cuero entero sin cortarlo. Pero no le cuentes a Fermín que te lo enseñé yo.'] }),
  isidro: () => ({ l: ['Buenas, buenas. Tengo yerba, tabaco, telas, aguardiente y lo que se le ocurra al cliente. Y si no tiene plata, ya veremos.', 'Todo se anota en la libreta. Yo anoto y no olvido.'] }),
  juana: () => ({ l: ['Hay puchero y empanadas. Los viajeros pagan, los vecinos se anotan en la libreta, y los peones comen lo que sobra.', 'Acá una se entera de todo. Pero también sabe cuándo callarse.'] }),
  cipriano: () => ({ l: ['¡Caballos de remuda, los mejores del camino! Cambio, alquilo o vendo. Pero rápido, que el coche no espera.', 'Los viajeros traen noticias del este. Dicen que se viene tiempo de leva.'] }),
  wenceslao: () => ({ l: ['El campo habla, muchacho, solo hay que saber leerlo. Mirá esa huella: venado, de esta mañana.', 'Y fijate en el sur. Hace días que los pájaros se levantan sin motivo. No me gusta nada.'] }),
  casiana: () => ({ l: ['Tenés cara de cansado. Si te duele algo, tengo yuyos para todo, menos para los males del alma.', 'No me gustan los uniformes ni los papeles. Los de la autoridad siempre llegan cuando ya es tarde.'] }),
  remigio: () => ({ l: ['Soy el juez de paz. Si buscás trabajo, andá con tu papeleta de conchabo en regla; no quiero problemas.', 'Ando de paso por la pulpería. Los tiempos están difíciles y el gobierno pide hombres.'] }),
  obregon: () => ({ l: ['Teniente Obregón, a su servicio. Este fortín tiene pocos hombres y menos provisiones, pero la frontera no se cuida sola.', 'Si ve rastros o gente extraña, avíseme. Y no ande solo de noche.'] }),
  lorenzo: () => ({ l: ['Yo hablo con los de acá y con los de allá. Se pueden hacer buenos tratos si uno sabe escuchar.', 'No todo lo que se dice de la frontera es verdad. Ni de un lado ni del otro.'] }),
  pascual: () => ({ l: ['Llevo cuero y lana a Buenos Aires. Es un camino largo, pero se paga bien.', 'Si algún día necesitás ir a la ciudad, hablame. Siempre hay lugar en la carreta para uno más, si es gente de bien.'] }),
};
