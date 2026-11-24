# Cinemáticas y diálogos: cómo contamos

Estado: **acordado** (decisiones de Juan) y **propuesta** (lo que desarrollo tiene que armar). Hoy nada de esto está en el juego: los diálogos son un cuadro de texto con la cámara isométrica acercada, y la llegada (`LlegadaP1`) está armada con código a mano.

La idea: cuando la conversación importa, el juego **corta** a una cámara de película, a la altura de la gente, y vuelve a la isométrica al terminar. Sirve igual para las charlas escritas que para las que va a improvisar un modelo de lenguaje, porque la cámara se dirige sola.

## Decisiones acordadas

| Tema | Decisión |
|---|---|
| Transiciones | **Corte directo, siempre.** Nunca fundido, salvo que Juan lo pida para una escena en particular. |
| Qué conversaciones usan la cámara cinematográfica | **Solo las importantes**: las de personajes con nombre que tienen peso en la historia. Todo lo demás (los transeúntes, las charlas menores, la pulpería) sigue con la cámara isométrica. |
| Voces | **Solo texto**, por ahora. |
| Cuadros por segundo | **30** alcanzan. No hace falta sostener 60. |
| Desenfoque de campo (bokeh) | **No va en la versión base.** Si se quiere, se arma en una rama aparte y se mide a 30 cuadros. |
| Ritmo de trabajo | De a poco, sin armar de más. |
| Referencia | *Disco Elysium* como norte de hacia dónde pueden evolucionar los gráficos y las texturas. No es para esta etapa. |

## Qué se arma en la versión base

### 1. Conversación importante
- El jugador habla con un personaje marcado como importante → **corte directo** a una cámara en perspectiva, a la altura de los ojos.
- **Siempre los mismos tres planos** (decisión de Juan):
  1. **Plano general (de conjunto):** los dos personajes en cuadro, de perfil o tres cuartos, con algo de entorno.
  2. **Plano medio de cada personaje, con referencia del otro:** el que habla, de la cintura para arriba, con el hombro o la nuca del otro en primer plano.
  3. **Plano corto de cada personaje:** de los hombros para arriba, para las líneas con peso.
- Cómo se alternan: la conversación abre con el plano general. Cuando habla alguien, plano medio de ese personaje. Si habla dos líneas seguidas o la línea es fuerte, plano corto. Cada tanto vuelve el plano general para ubicar de nuevo. La regla exacta es de desarrollo y se ajusta jugando.
- La cámara se coloca sola a partir de dónde están los dos personajes. Si no hay lugar (una pared, un árbol, un cerco), queda la cámara isométrica acercada, como hoy.
- Al terminar la charla, **corte directo** de vuelta a la isométrica.
- El texto sigue siendo un cuadro abajo, con el nombre de quien habla. Se pasa con E, líneas de 25 palabras o menos (ver `README.md`).
- Sin desenfoque en esta versión.

### 2. Cómo se marca qué es importante
Una casilla en la ficha del personaje (`FichaPersonaje`): **conversación cinematográfica**. Además, una misión puede forzar la cámara cinematográfica en un momento puntual aunque el personaje no la tenga marcada. Quedan para decidir entre los dos cuáles son (propuesta abajo).

### 3. Escenas clave (pocas)
Escenas con cortes de cámara armados a mano, hechas con las herramientas que ya están en el proyecto (**Cinemachine** y **Timeline**). Todas con **corte directo**. Candidatas del acto 1:

1. La llegada en carreta y la despedida de Juan Maidana (P1).
2. Obregón le revela que la tierra queda del otro lado de la frontera (P1).
3. Rufino se queda con el título y el pasaporte (P1).
4. La libreta de Ramón: el hallazgo (P5).
5. El malón (P6).
6. La huida en la carreta (P7).

### 4. Cómo se escribe una misión
Cada misión se arma con **momentos**, de cuatro tipos: **conversación**, **escena**, **tarea** y **decisión**. Es lo que ya hace `misiones.md`; lo que falta es que el juego lo pueda leer. Las pistas y los objetivos, cuando se pueda, dentro del mundo (una carta, la libreta del Gallego) y no en carteles de pantalla.

## Propuesta de personajes con cámara cinematográfica

Para discutir, a partir de `personajes.md` y `misiones.md`:

| Personaje | Por qué |
|---|---|
| Obregón | La revelación de la frontera (P1). |
| Rufino | Se queda con los papeles (P1), y reaparece en P5 y P7. |
| Anselmo | Capataz: confianza y muerte en el malón. |
| Juana, Hilario | La revelación del italiano (P3) y las señales (P4). |
| Ramón | La libreta (P5). |
| Santos | El amigo del Gallego; la decisión del malón. |
| Jacinta | El refugio de la huida (P7). |

El resto (Isidro como pulpero, los de los oficios, los transeúntes) sigue en isométrica.

## Cosas técnicas que desarrollo tiene que tener presentes

- **Ortogonal a perspectiva.** La cámara de juego es ortogonal y la cinematográfica es en perspectiva: no se mezclan suavemente, y está bien, porque el pedido es de corte directo.
- **Recorte de árboles y cercos.** Hoy depende de la posición del jugador y de la cámara isométrica (`CamaraIsometrica`: radio de recorte). En la cámara cinematográfica hay que apagarlo o rehacerlo, para que un árbol no tape el plano.
- **Cuadros por segundo.** `Juego/Rendimiento.cs` tiene el tope en 60 (`CUADROS_POR_SEGUNDO`). Pasarlo a 30.
- **Diálogo.** Hoy se dibuja con IMGUI en `JuegoPago.OnGUI`. Si se rehace, mantener el cuadro simple de texto.
- **Las charlas con modelo de lenguaje** (`charla-ia.md`) usan este mismo sistema de cámara: hay que poder dirigirla sin una escena escrita de antemano.
- **Rama aparte, para más adelante:** desenfoque de campo con bokeh (el *Volume* de URP ya lo trae) y su costo a 30 cuadros.

## Fundidos que ya están en el guion

La escena de la mañana siguiente (P1, "La mañana siguiente") está escrita con **fundido a negro**. Por la regla de arriba, pasa a **corte directo** con el rótulo, salvo que Juan pida lo contrario para esa escena. A confirmar.

## Preguntas abiertas

1. ¿Cuáles de los personajes de la tabla de arriba quedan? ¿Se agrega alguien?
2. ¿Los primeros planos de las escenas clave se arman en el editor, o desarrollo prepara primero una herramienta para definir los planos en datos?
3. ¿La escena de la mañana siguiente va con corte directo y rótulo, o se pide el fundido en ese caso?
4. ¿Qué hacemos cuando la cámara cinematográfica no tiene lugar (pared, árbol)? La propuesta es quedarse en la isométrica acercada.
