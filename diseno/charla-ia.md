# Charla con IA y personajes secundarios

Estado: **propuesta**. Viene de la hoja de ruta del README ("NPC con libertad narrativa mediante un modelo de lenguaje").

## La idea

Con cualquier personaje se puede charlar y estirar la conversación todo lo que uno quiera. Un modelo de lenguaje (IA) responde en personaje.

**Acordado: el jugador no escribe, elige.** Cada vez que el personaje habla, el juego ofrece **4 opciones**:

1. Un tono (por ejemplo, amable).
2. Otro tono (por ejemplo, pícaro o seductor).
3. Otro tono (por ejemplo, desafiante).
4. **Despedirse**.

- **Los tonos dependen del personaje**. Con Rufino pueden ser sumiso, astuto o encarado; con Doña Matilde, agradecido, coqueto o curioso; con Santos, compinche, burlón o serio.
- **El juego decide** qué tonos ofrece y qué efecto tiene cada uno en la relación. **La IA solo escribe** la frase del personaje y el texto de las opciones del Gallego, con su voz castellana.
- **Con menores**, como Carmen, el juego nunca ofrece una opción seductora.

**Pendiente**: la IA corre dentro del juego (un modelo gratuito local) o en un servidor. Antes de decidirlo, hay que probar si un modelo chico logra la voz de época.

La charla puede cambiar la relación con el personaje:

- Ganar su confianza, su amistad o su cariño.
- Intentar **seducirlo**.
- **Ofenderlo**. Si se ofende de verdad, te desafía a pelear.
- Que te ofrezca una **misión secundaria**.

## Dos tipos de personaje

| | Principales (los 15 del acto) | Secundarios |
|---|---|---|
| Rol | Mueven la historia del acto 1 | Dan vida al pago y misiones secundarias |
| Charla libre | Sí, pero con **candados** | Sí, libre |
| ¿Pueden cambiar la historia? | Solo por los caminos previstos en el guion | **No**: lo que pase con ellos no toca la trama principal |
| Si mueren o se enojan | La historia tiene un plan B, o no pueden morir antes de su escena | Consecuencias locales: se va del pago, no te vende, te guarda rencor |

**Candados**: cada personaje principal sabe solo lo que corresponde a la etapa en curso. Por ejemplo, Ramón no menciona los pagos de la Agencia antes de la etapa 3, por más que el jugador insista. El personaje puede esquivar el tema, mentir o cambiar de conversación, pero nunca revelar antes de tiempo.

## Qué recibe la IA en cada charla

1. **La ficha del personaje**: quién es, qué quiere, qué esconde y cómo habla (de `personajes.md` o de la ficha de los secundarios).
2. **Lo que sabe en esta etapa** y lo que no puede decir todavía.
3. **Su memoria del Gallego**: un resumen de las charlas anteriores y de lo que hizo el Gallego (si le pagó, si le mintió, si lo ayudó).
4. **Cómo está la relación**: los indicadores de abajo.
5. **El contexto**: dónde están, la hora y qué pasó hace poco en el pago.
6. **Las reglas de época y de tono**: ver "Límites".

## Qué devuelve la IA

Además del texto del diálogo, devuelve **acciones** que el juego ejecuta:

- Cambios en la relación: `confianza +1`, `enojo +2`.
- `desafio_pelea`: el personaje te invita a pelear (con puños o con facón, según quién sea).
- `ofrece_mision`: el personaje te propone una misión secundaria, a partir de un molde (ver abajo).
- `regala` / `vende` / `fia`.
- `termina_charla`: se va, te echa o se queda callado.
- `recuerda`: un dato nuevo que el personaje va a recordar.

Así el juego tiene el control: la IA **propone**, el juego **decide** si la acción es válida.

## Relación con cada personaje

Cada personaje tiene cinco indicadores, de 0 a 10:

| Indicador | Qué lo sube | Qué lo baja | Qué destraba |
|---|---|---|---|
| Confianza | Cumplir, decir la verdad | Mentir, robar | Secretos y misiones |
| Simpatía | Charlar, ayudar, el humor | Despreciarlo | Precios, regalos, ayuda en la huida |
| Atracción | Coqueteo bien recibido | Insistir cuando dijo que no | Romance |
| Respeto | Ganar peleas, hacer bien el trabajo | Huir, quedar en ridículo | Que te tomen en serio |
| Enojo | Insultos, ofensas, celos | El tiempo, disculparse, un regalo | Con mucho enojo, te desafía a pelear |

## Seducción y romance

- Se puede intentar con **cualquier personaje adulto**.
- El personaje **puede decir que no**, y si el Gallego insiste, sube el enojo.
- **Hay consecuencias**: seducir a alguien casado, como Doña Matilde, pone celoso al marido y puede terminar en duelo.
- **Límite firme**: **Carmen Larralde tiene 15 años y queda fuera de cualquier romance**, igual que cualquier otro menor. La IA no sigue ningún coqueteo con ella: lo corta en personaje. El juego además bloquea las acciones de romance con menores, sin depender de la IA.
- *Propuesta*: las escenas íntimas se resuelven con un fundido a negro, sin contenido explícito.

## Peleas

- Cuando el enojo pasa un umbral, o con ciertas ofensas graves (insultar a la madre, tratar a alguien de cobarde o de ladrón), el personaje desafía al Gallego.
- El jugador puede aceptar, disculparse o huir. Huir baja el respeto: "el Gallego es flojo".
- La pelea usa el combate a pie de la hoja de ruta, con puños o facón. *Pregunta abierta*: ¿se puede matar a un secundario? ¿Tiene consecuencias con la justicia, con Rufino?

## Límites de la IA

- Habla según **`voz.md`**: castellano rioplatense de 1867, como máximo dos marcas de época por línea, y siempre comprensible para un jugador de hoy. No conoce nada posterior a 1867, ni siquiera en las opciones que escribe para el Gallego.
- No rompe el personaje ni dice que es una IA.
- No inventa hechos de la trama principal: no puede "resolver" la estafa ni matar a un principal por su cuenta.
- Se mantiene en el tono de época, y eso incluye prejuicios de la época. Pero el juego no los celebra: el personaje los puede tener sin que la IA los amplifique.
- Si falla la conexión, el personaje usa sus líneas escritas a mano, como hoy.

## Misiones secundarias por molde

Para que las misiones que surgen de la charla no rompan nada, la IA elige un **molde** y lo completa:

| Molde | Ejemplo |
|---|---|
| Llevar | Llevarle un encargo a alguien del pago |
| Buscar | Encontrar un caballo perdido, una prenda o un animal |
| Cobrar | Cobrar una deuda, por las buenas o por las malas |
| Ayudar en una tarea | Arreo, faena o doma para otro |
| Acompañar | Escoltar a alguien hasta la posta o hasta el fortín |
| Averiguar | Enterarse de un chisme o de un secreto |

Recompensas: plata, objetos, habilidad o relación.

## Personajes secundarios (propuesta)

Con nombres de las listas que no se usaron todavía:

| Personaje | Quién es | Misiones secundarias típicas |
|---|---|---|
| **Doña Eduarda Ugarte** | Estanciera viuda, vecina y rival de los Larralde | Comprar o vender cueros, averiguar cosas de los Larralde |
| **Domingo Figueroa** | Tropero de carretas | Llevar cargas, cuidar la tropa de noche |
| **Genaro Arrieta** | Puestero solo, en un puesto lejano | Buscarle caballos perdidos |
| **Bernarda Gómez** | Lavandera del arroyo | Chismes, buscar una prenda que se llevó el agua |
| **Josefa Ríos** | Partera del pago, afroargentina | Acompañarla a un parto lejano, de noche |
| **Ceferino Maidana** | Hermano menor del carretero, guitarrero y cuatrero de poca monta | Recuperar algo que robó, payadas |
| **Benito Acosta** | Hijo de Hilario, quiere ser baqueano | Rastrear un animal juntos |
| **Escolástica Pérez** | Vieja devota | Cumplir una promesa al santo |

## Decisiones técnicas (con tu amigo, después de la migración)

- Qué modelo y qué servicio usar, cuánto cuesta cada charla y cuánto tarda en responder.
- Si se puede jugar sin internet.
- Cómo se integra con Unity.
