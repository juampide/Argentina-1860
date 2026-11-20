# El caballo del Gallego

Estado: **en el juego** la parte de manejo (desarrollo). Todo lo que es historia está **por decidir** (diseño): ver "Decisiones para el guion".

## Qué ya funciona

- **Montar y bajarse** con E, parado al lado del caballo. Si hay alguien cerca para charlar, la E sirve para hablar (también se puede hablar desde el caballo).
- **Trote** con WASD y **galope** con Shift. El caballo acelera y frena de a poco y gira más abierto cuanto más rápido va.
- **Aliento**: el galope lo gasta (unos 14 segundos de galope con el aliento lleno). Si se queda sin aliento, el caballo queda **cansado** y no galopa hasta recuperar un tercio. Al trote o quieto se recupera. Se ve abajo a la izquierda.
- **Silbido** con H: el caballo viene trotando. Si quedó muy lejos, aparece a unos 30 metros y llega solo.
- **Anda en los dos mundos**: si el Gallego cambia de mundo montado, llega montado.
- El caballo es un criollo de pelaje **moro** (gris azulado, con calcetines blancos), se llama **Moro** y lleva recado: pelero, cojinillo y estribos.
- **Para probar**: la O da el caballo. Es un truco para pruebas y se va a sacar cuando esté la misión.

Todavía no hay forma de conseguirlo dentro de la historia: eso es lo que hay que decidir.

## Decisiones para el guion

### 1. Cómo lo consigue

Ideas que salen del guion actual (elegir una, combinar o proponer otra):

- **En el pago, con Petrona Silva**, la maestra de posta: "puede vender o alquilar un caballo" (`personajes.md`). Podría ser una compra con la plata del arreo o de la pulpería, o un alquiler que hay que devolver.
- **Ganárselo trabajando**: Fermín o el patrón le pagan un trabajo con un caballo viejo. Encaja con que al Gallego todos le tomen el pelo: le dan un **mancarrón**.
- **Santos** junta plata para comprarse un caballo propio (`personajes.md`). Puede haber una misión compartida, o una rivalidad por el mismo caballo.
- **En Buenos Aires**: comprarlo en un corralón o en la feria de las afueras, ganarlo en una apuesta, o recibirlo como pago de una misión.
- **Robarlo**: posible pero con consecuencias (ver punto 4).

### 2. Aprender a montar

El Gallego "nunca anduvo a caballo en campo abierto" (`protagonista.md`). Opciones:

- **Sin aprendizaje**: puede montar desde que tiene el caballo.
- **Con aprendizaje**: al principio solo trota y se cae si galopa mucho; mejora con la práctica o con alguien que le enseñe (¿Fermín, el baqueano?). Sería una **habilidad de jinete**, como la de comercio y la de arreo.
- **Momento de humor**: la primera vez que monta, la gente se ríe. Puede ser una escena.

### 3. El malón y el viaje a la ciudad

- ¿El caballo **sobrevive al malón**? Petrona "pierde la caballada" en el malón.
- El Gallego llega a Buenos Aires en la carreta de Juan Maidana. ¿Se lleva el caballo atado a la carreta, lo pierde o lo vende para pagar el viaje?
- Si lo pierde, conseguir otro en la ciudad puede ser la primera misión del acto 2.

### 4. Caballos ajenos y robo

- ¿Se pueden montar los caballos de otros (los de la posta, los atados al palenque, los de los carros de la ciudad)?
- Si se puede, es **robo**: ¿quién lo ve, quién lo persigue y qué pasa si lo agarran? Esto va de la mano con el sistema de dueños y testigos que viene después (ver la idea en `charla-ia.md`: la IA propone, el juego decide).
- En 1867, robar caballos (abigeato) era un delito grave en el campo. ¿Es una salida para el Gallego forajido, o una línea que no cruza?

### 5. El caballo como personaje

- **Nombre y pelaje**: ¿lo elige el jugador, viene con nombre o se lo pone alguien del pago? Hoy es "Moro" de manera provisoria.
- ¿Tiene **carácter**: arisco, manso, mañero? ¿Hay que **cuidarlo** (darle agua y pasto, herrarlo) o no complicamos eso?
- ¿Puede **morir** o perderse? ¿Se lo pueden robar al Gallego?
- ¿Los personajes **lo comentan** en las charlas ("lindo pingo", "ese mancarrón no llega a la posta")? Sirve para la charla con IA.

### 6. En la ciudad

- ¿Dónde se deja el caballo al entrar a un edificio: palenque en la puerta, caballeriza paga?
- ¿Hay calles donde no se puede galopar o la policía multa? Puede dar situaciones de época.
- ¿Más adelante hay otros vehículos: volanta, sulky, carro?

## Pendiente con desarrollo

- Cuando se decida cómo se consigue: crear la misión y sacar el truco de la O (`Caballeriza.TRUCOS`). Desde el código, una misión da el caballo con `Caballeriza.Instancia.DarCaballo("Nombre", "#color")`.
- Si hay aprendizaje de jinete: agregar la habilidad y que limite el galope.
- Sistema de dueños, testigos y persecución (robo de caballos y carros).
- Guardar la partida: hoy el caballo se pierde al cerrar el juego.

## Preguntas abiertas

1. ¿Dónde y cómo consigue el Gallego su primer caballo?
2. ¿Tiene que aprender a montar?
3. ¿Qué pasa con el caballo en el malón y en el viaje a Buenos Aires?
4. ¿Se puede robar caballos? ¿Con qué consecuencias?
5. ¿Nombre, pelaje y carácter del caballo?
