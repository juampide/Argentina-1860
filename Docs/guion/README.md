# Diseño: guion y diálogos

Guion y diseño narrativo, en texto. Lo escribe el lado de diseño (ver `CLAUDE.md`). Las fichas jugables de cada personaje están en `Assets/Datos/Personajes`: lo que se acuerda acá se pasa a esas fichas.

| Archivo | Qué tiene |
|---|---|
| `protagonista.md` | El Gallego: quién es, qué quiere, la estafa y el patrón de los cuatro actos. |
| `personajes.md` | Ficha de cada uno de los 15 personajes del pago: quién es, qué quiere, qué esconde, cómo habla y qué le pasa en el malón. |
| `charla-ia.md` | Charla con IA por opciones (tres tonos y despedirse), relación, seducción y peleas, misiones secundarias y personajes secundarios. |
| `voz.md` | Cómo habla cada grupo en 1867, basado en el estudio de Perna: qué rasgos usar, cuáles no y el vocabulario de época. |
| `arco-pago.md` | La historia del pago por etapas, desde que llega el Gallego estafado hasta que huye a Buenos Aires como forajido. |
| `caballo.md` | El caballo del Gallego: qué ya funciona en el juego y las **decisiones pendientes** (cómo lo consigue, si aprende a montar, qué pasa en el malón, robo de caballos). |
| `contexto-1867.md` | *(pendiente)* Datos de época para mantener el tono y no meter anacronismos. |

## Convenciones

- **Estado**: todo es **borrador**. Cada ficha y cada etapa dice si está *propuesta*, *acordada* o *en el juego*.
- **Identificadores**: se usan los mismos `id` que las fichas de Unity (`celestino`, `fermin`, `isidro`…). Los nombres que se ven en pantalla cambian; los `id` no.
- **Voz**: ver `voz.md`. En resumen: castellano rioplatense con voseo, como máximo dos marcas de época por línea, y que un jugador de hoy lo entienda a la primera. Sin modismos de hoy ("re", "ok", "chabón"). Los personajes de más edad o de más rango pueden tratar de *usted*. El Gallego habla castellano de España (tú, vosotros) y al principio no entiende las palabras del campo; con el tiempo las va aprendiendo.
- **Diálogos**: cada línea es una "burbuja" que el jugador pasa con E. Conviene que tenga 25 palabras o menos.
- **Preguntas abiertas**: al final de cada archivo, para resolver entre los dos.

## Pendiente con desarrollo

- ~~Pasar a las fichas los nombres nuevos y los campos *personalidad*, *sabe* y *noSabe*~~: hecho. También se cambió el aspecto de Petrona Silva y los nombres viejos en los textos de `JuegoPago`.
- Los nombres viejos siguen en `Assets/Scripts/Editor/DatosPago.cs` (datos del generador). No afecta al juego porque el generador no pisa las fichas existentes, pero conviene actualizarlo.
- El diálogo por opciones (`charla-ia.md`) necesita código nuevo: queda para desarrollo.
- **Caballo** (`caballo.md`): desarrollo ya hizo montar, trotar, galopar y silbar. Falta que diseño decida cómo se consigue para armar la misión.
