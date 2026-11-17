# Argentina 1867 — juego de rol (prototipo)

Rol de exploración en 3D, de estilo *low poly* sucio y de época, ambientado en la Argentina de 1867. Tiene dos mundos:

- **El pago** (campo, tutorial): una estancia, ranchos, una posta con pulpería, un fortín y la pampa. Es donde arranca la historia, y está pensado para terminar con un malón que arrasa todo y manda al protagonista a Buenos Aires como forajido.
- **Buenos Aires** (ciudad): generada a partir del plano del Departamento Topográfico de 1867 (3.600 manzanas, lotes con casas, árboles, iglesias con torre). Hoy tiene una primera misión de prueba ("La carta urgente").

Todo es un prototipo en desarrollo: lo que está listo y lo que falta se detalla más abajo.

## Cómo correrlo

Hace falta Node.js.

```bash
npm install      # una sola vez
npm run dev      # abre http://127.0.0.1:5174
```

- Ciudad: `http://127.0.0.1:5174/`
- Pago (campo): `http://127.0.0.1:5174/?mundo=campo`

`npm run build` genera la versión para publicar. Necesita internet solo para instalar las dependencias; una vez instalado, funciona sin conexión.

## Controles

| Tecla | Acción |
|---|---|
| WASD / flechas | caminar |
| Shift | correr |
| Q / R | girar la vista de a 90° |
| Rueda del mouse | zoom |
| E | hablar, abrir o cerrar la tranquera, salir de la tienda |
| M | mapa grande (solo en el pago) |
| Esc | cerrar el mapa o la tienda |

## Qué hay hecho

### El pago (campo)
- **Mundo**: 3,2 × 2,4 km con terreno de lomas suaves, arroyo, laguna, monte, pajonal, caminos y sendas, y sitios aplanados para las construcciones.
- **Quince personajes con nombre** repartidos en sus lugares (Celestino, Mercedes, Carmen, Fermín, Anacleto, Nicasio, Isidro, Juana, Cipriano, Wenceslao, Casiana, Remigio, Obregón, Lorenzo, Pascual). Los textos son provisorios.
- **Animales con vida propia**: vacas holando, caballos, ñandúes, venados, zorros, carpinchos, pumas, vizcachas, perdices, caranchos, etc. Pastan, caminan y huyen si te acercás. Los grandes tienen colliders y no se atraviesan. Los caballos atados, de corral y sueltos tienen comportamientos distintos; de los corrales sale alguno de a uno.
- **Eventos constantes mientras caminás**: jinetes que pasan al galope y te esquivan, tropillas, bandadas, grupos de ñandúes y venados cruzando, carretas de bueyes por el camino.
- **Arreo de vacas** (primer minijuego, tarea de Fermín): rodeás cinco vacas hasta un corral con piso de barro y tranquera, y la cerrás con E. Paga según el tiempo y mejora la habilidad *Arreo*.
- **Pulpería de Don Isidro**: tienda con yerba, tabaco, caña, tela y sal. Los precios fluctúan solos y con noticias del camino (carretas, cortes de ruta). La habilidad *Comercio* achica la diferencia entre compra y venta.
- **Interiores**: la pulpería (mostrador con reja y paneles, balanza, vitrina, estantes hasta el techo, sillas, reloj) y el rancho de peones (fogón con olla, catre, mesa, tinaja). Al entrar se ocultan el techo y las paredes que miran a la cámara.
- **Arquitectura de época** tomada de fotos: ranchos de adobe con varas y techo de paja empinado; pulpería con frontón, cartel pintado, ladrillo a la vista y alero de vigas.
- **Pasto de pampa**: matas altas que se mecen con el viento (hasta ~2 m en el pajonal) y se aplastan cerca del jugador.
- **Mapa**: minimapa que gira con la cámara, con nombres y distancias de los lugares y el objetivo en rojo; mapa grande con M.

### Aspecto
- Cámara isométrica ortogonal con rotación de a 90°.
- Edificios opacos; cuando algo te tapa, tu personaje se ve como silueta clara. Árboles y cercos se recortan con un hueco tramado.
- Suciedad procedural (barro, hollín, chorreaduras, grietas de adobe, desgaste) y corrección de color apagada y polvorienta.

### Ciudad (Buenos Aires)
- 3.600 manzanas, 35.000 árboles, 16 iglesias, colisión con casas, minimapa y la misión de prueba con 4 personajes.

## Estructura del código

Todo está en `src/` (three.js 0.170 + Vite).

| Archivo | Qué hace |
|---|---|
| `main.js` | bucle principal, cámara, controles, HUD, diálogos, tienda, misiones y arranque de ambos mundos |
| `campo.js` | el pago: terreno, caminos, construcciones, interiores, cercos, pasto |
| `ciudad.js` | la ciudad a partir de los datos del plano |
| `fauna.js` | animales, caballos, eventos, corral y minijuego de arreo |
| `animales.js` | modelo y animación del caballo y del carro |
| `personaje.js` | personajes: aspecto, animación y silueta |
| `elenco.js` | los quince personajes del pago y sus diálogos |
| `mercado.js` | precios que fluctúan de la pulpería |
| `mapa.js` | minimapa y mapa grande |
| `colision.js` | colisiones con edificios, personas y animales |
| `xray.js` | transparencia tramada de árboles y cercos |
| `suciedad.js` | mugre y corrección de color |

`public/` tiene los datos de la ciudad (`datos/`), las texturas (`tex/`) y las imágenes del plano (`plano/`). `decode.mjs` es una herramienta auxiliar para inspeccionar `ciudad.pak`.

## Qué falta (hoja de ruta)

- **Economía del campo**: doma de caballos (Anacleto), faena (Nicasio) y trueque; cada actividad con un minijuego de 30 a 60 s. Las habilidades suben con el uso.
- **Combate** a pie y a caballo con facón y boleadoras (sin magia).
- **Interiores** de la estancia grande y de los demás ranchos.
- **El golpe**: el malón que arrasa el pago y manda al protagonista a la ciudad como forajido.
- **Amenazas del campo** (pumas, malón) y progresión por actos.
- **NPC con libertad narrativa** mediante un modelo de lenguaje (diseño pendiente).
- **Ciudad**: ampliar misiones y adaptar el estilo sucio y los interiores; olas en el río.
- Los números de dificultad (tiempos del arreo, velocidad de las vacas, frecuencia de eventos) son estimaciones sin ajustar.

## Datos y créditos

- El plano y los datos de la ciudad provienen del trabajo sobre el plano del Departamento Topográfico de 1867 publicado en baires.mormon.garden. **Antes de hacer público este repositorio, conviene confirmar la licencia de esos datos.**
- Referencias visuales de ranchos y pulperías: fotografías de época.
- Hecho con [three.js](https://threejs.org) y [Vite](https://vitejs.dev).
