# Diseño: guion y diálogos

Carpeta de diseño narrativo. Es solo texto: no depende de three.js ni de Unity, así que sirve para los dos y no choca con la migración.

| Archivo | Qué tiene |
|---|---|
| `protagonista.md` | El Gallego: quién es, qué quiere, la estafa y el patrón de los cuatro actos. |
| `personajes.md` | Ficha de cada uno de los 15 personajes del pago: quién es, qué quiere, qué esconde, cómo habla y qué le pasa en el malón. |
| `charla-ia.md` | Charla con IA por opciones (tres tonos y despedirse), relación, seducción y peleas, misiones secundarias y personajes secundarios. |
| `voz.md` | Cómo habla cada grupo en 1867, basado en el estudio de Perna: qué rasgos usar, cuáles no y el vocabulario de época. |
| `arco-pago.md` | La historia del pago por etapas, desde que llega el Gallego estafado hasta que huye a Buenos Aires como forajido. |
| `contexto-1867.md` | *(pendiente)* Datos de época para mantener el tono y no meter anacronismos. |

## Convenciones

- **Estado**: todo es **borrador**. Cada ficha y cada etapa dice si está *propuesta*, *acordada* o *en el juego*.
- **Identificadores**: se usan los mismos `id` que el código (`celestino`, `fermin`, `isidro`…), así después se pueden pasar a Unity sin renombrar nada.
- **Voz**: ver `voz.md`. En resumen: castellano rioplatense con voseo, como máximo dos marcas de época por línea, y que un jugador de hoy lo entienda a la primera. Sin modismos de hoy ("re", "ok", "chabón"). Los personajes de más edad o de más rango pueden tratar de *usted*. El Gallego habla castellano de España (tú, vosotros) y al principio no entiende las palabras del campo; con el tiempo las va aprendiendo.
- **Diálogos**: cada línea es una "burbuja" que el jugador pasa con E. Conviene que tenga 25 palabras o menos.
- **Preguntas abiertas**: al final de cada archivo, para resolver entre los dos.

## Pendiente con la migración

- Acordar el formato en que Unity va a leer los diálogos (JSON, ScriptableObjects, Ink, Yarn…). Mientras tanto se escriben acá en Markdown y después se convierten.
