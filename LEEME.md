# Buenos Aires, 1867 — juego de rol (prototipo)

Prototipo de rol de exploración y charla sobre el plano del Departamento Topográfico de 1867.

## Cómo correrlo
En esta carpeta: `npm install` (una vez) y `npm run dev`. Se abre en http://127.0.0.1:5174

## Controles
WASD o flechas: caminar · Shift: correr · arrastrar con el mouse: girar la cámara · rueda: zoom · E: hablar.

## Qué tiene
- La ciudad en 3D generada a partir de las manzanas del plano (`public/datos/ciudad.pak`): 3.600 manzanas
  divididas en lotes con casas de distinta altura, 35.000 árboles y 16 iglesias con torre.
- Colisión con casas, minimapa, cámara que no atraviesa paredes.
- Primera misión con 4 personajes ("La carta urgente"): se resuelve hablando en orden.

## Dónde tocar
Todo está en `src/main.js`: personajes y diálogos en `TEXTOS`, misiones en `OBJETIVOS`, personajes en `crearNPC`.

## Estilo realista (versión 2)
- Casas con techo de teja a dos aguas o azotea, ventanas con postigos, rejas y puertas; patios interiores.
- Monumentos de 1867 ubicados según el plano: Cabildo con recova y torre, Recova Vieja, Catedral con pórtico y cúpula,
  Pirámide de Mayo, iglesias con torre.
- Cielo, sombras en tiempo real, suelo de tierra, río (la máscara del plano marca el agua y bloquea el paso).
- Personajes de época hechos con formas simples: caballero con levita y sombrero de copa, dama con crinolina y chal,
  sacristán, paisano con poncho, vendedor de diarios (`src/personaje.js`).

## Texturas (licencia CC0, ambientCG — https://ambientcg.com)
RoofingTiles014A (teja), Plaster002 (revoque), Ground054 (tierra), Ground037 (pasto), Ground048 (tierra de patio). Están en `public/tex/`.

## Créditos y datos
- Plano de 1867: Departamento Topográfico de la Provincia de Buenos Aires.
- Datos de manzanas, árboles y plano digitalizado: tomados de https://baires.mormon.garden/ con permiso de su autor.
- `suelo.webp` es una máscara (río / tierra / calles) y todavía no se usa; `relieve.bin`, `calles.*` y `hoy.json` quedan para después.
