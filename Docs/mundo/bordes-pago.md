# Bordes del pago y la frontera

Estado: **en el juego** (primera versión). El código está en `Assets/Scripts/Pago/BordesPago.cs`.

## La idea

El mapa (3,2 × 2,4 km) no tiene que terminar en un corte. El jugador queda encerrado por el paisaje, y cada borde tiene un motivo de época para no seguir:

| Lado | Qué hay | Por qué no se pasa (aviso en el juego) |
|---|---|---|
| **Norte** | El bañado donde nace el arroyo: agua baja, charcos, barro, juncal y cortadera, algún sauce en la orilla | "El bañado no da paso: barro hasta la cincha y juncos más altos que un hombre." |
| **Este** | Monte cerrado de talas, coronillos y espinillos, con matas y cortadera en el borde | "El monte es una maraña de talas y espinillos. Por ahí no pasa ni un perro." |
| **Este, camino real** | El camino sigue por una picada abierta en el monte | "El camino real sigue hacia Buenos Aires. Sin pasaporte del juez de paz no se puede salir del partido." |
| **Sur y oeste** | El cañadón de la frontera: barranca de tosca de casi 3 m con agua al fondo, sauces y ceibos en el borde | "Del otro lado del cañadón empieza tierra adentro. Nadie cruza sin baqueano ni permiso del fortín." |
| **Oeste, vado** | El camino real baja al cañadón por un vado; al lado, un mangrullo de guardia | "El vado lo vigila el fortín. Sin el permiso del comandante nadie cruza la frontera." |

Más allá del terreno sigue el mismo paisaje (objeto **Pampa de afuera**): más bañado al norte, copas del monte al este y pampa abierta al sur y al oeste. El jugador se frena unos 5 m antes de la orilla, del monte o de la barranca. Lo mismo vale para el caballo y para los animales del pago.

## El fortín

Rehecho según la foto de referencia de un fortín de frontera:

- **Estacada de palo a pique**: troncos desparejos con la punta afilada, uno al lado del otro, sobre un terraplén de tierra, con travesaños atados por dentro.
- **Foso** alrededor, con su bordo de afuera. Se cruza por un puente de tablones frente al **portón**, que mira al pago (este).
- **Mangrullo**: torre de cuatro troncos con crucetas, plataforma con baranda y escalera de palos, en la esquina que mira a la frontera.
- **Ranchos de adobe con techo de paja** (cuadra y comandancia), el mástil con la bandera y el fogón con la ronda de soldados.

## Tierra adentro: la toldería

Del otro lado del cañadón, al sur (a unos 50 m del agua), hay una toldería:

- **Toldos** bajos, de cueros de potro cosidos sobre un armazón de palos, con la entrada hacia el este, como se armaban.
- **Cueros estaqueados** secándose, **lanzas de tacuara** apoyadas y clavadas, un fuego y la **caballada** pastando entre los toldos y el cañadón.
- **La gente**: hombres con quillango (manto de cueros de guanaco con el pelo hacia adentro y el cuero pintado), vincha y pelo largo; mujeres con el manto hasta el tobillo y dos trenzas; chicos. Algunos están sentados al fuego, otros trabajan los cueros, y hay vigías con lanza mirando hacia el pago.

No se puede llegar ni hablar con ellos todavía. Para verlos hay que acercarse a la orilla del cañadón y alejar la cámara con la rueda del mouse.

Las ropas salen de las fotos de referencia (pampas y tehuelches de fines del siglo XIX). *A verificar con una fuente antes de darlo por bueno*: hacia 1867 muchos ya usaban también ponchos tejidos, chiripá, botas de potro y prendas criollas, sobre todo los que comerciaban con la frontera. En el juego eso se ve en el chiripá, las botas y algún poncho colgado en los toldos.

**Cuidado con el tono**: la toldería es gente viviendo su vida, no una amenaza decorativa. El malón del guion viene de "las mismas tierras que le vendieron" al Gallego, y el cierre del acto 4 (la frontera que avanza) tiene la ambigüedad que señala `Docs/guion/protagonista.md`. Conviene que lo que se vea acá no la simplifique.

## Cómo se edita

- **Dónde están las orillas** (bañado, frente del monte, eje del cañadón), el margen y los avisos: `BordesPago.cs`. Es código: pedir a desarrollo, o escribirlo con el agente.
- **Aplicarlo al terreno**: `RelievePago.Bordes(terreno)` hunde el cañadón y el bañado, pinta la franja del borde y planta el monte, el juncal y los sauces. Solo toca la franja del borde, y correrlo de nuevo no vuelve a hundir lo que ya está hundido. Por ahora se corre desde el MCP. Si se rehace todo el relieve, los bordes salen solos, porque `MapaPago.Altura` y `MapaPago.PesosSuelo` ya los incluyen. Después hay que volver a correr `Bordes` para replantar.
- **Fortín**: **Construcciones → Fortín** (`Estacada`: puntos, portón, alto, foso; `Mangrullo`: alto y ancho).
- **Guardia del vado**: **Construcciones → Guardia del vado** (un `Mangrullo`).
- **Toldería**: **Construcciones → Toldería (tierra adentro)** (`Tolderia`: toldos, gente, caballos, semilla). El fuego es el `Fogon` hijo, sin ronda.
- **Lo de afuera**: **Construcciones → Pampa de afuera** (`PampaAfuera`: ancho y tamaño de cuadro).

## Preguntas abiertas

1. ¿Quiénes son los de la toldería? ¿Gente de un cacique con nombre, parientes de Lorenzo, indios amigos que comercian con la posta?
2. ¿Se puede cruzar alguna vez? Por ejemplo con Lorenzo como baqueano, o con el permiso de Obregón, en el acto 2.
3. ¿El fortín tiene centinela en el mangrullo? ¿Y en el vado?
4. ¿Hace falta un aviso distinto según el personaje? Por ejemplo, que Lorenzo sí sepa cruzar el cañadón.
5. El camino real del este: ¿hacia dónde va exactamente (Chascomús, Dolores, Buenos Aires)? Hoy el aviso dice Buenos Aires.
