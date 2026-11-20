# Loteo del pago

Estado: **en el juego** (primera versión). Plano de la idea: `loteo-pago.svg`; el loteo real está en `Assets/Resources/LoteoPago.asset`.

## Cómo se edita

- **Lotes y caminos**: en `Assets/Resources/LoteoPago.asset` (Inspector). Cada lote tiene nombre, dueño (id de ficha), uso, linde, vértices en km y tranqueras. Cada camino tiene tipo (real, callejón, senda, huella), puntos en km y si termina en un patio.
- **Sembrados**: objetos bajo **Construcciones → Sembrados**, uno por chacra (cultivo, etapa, tamaño, fallas).
- **Lindes** (zanjas, bordos, tunas, mojones, tranqueras): objeto **Construcciones → Lindes del loteo**. Se rearma solo desde el loteo.
- **Caminos**: **Construcciones → Camino real (huellas)** y **Sendas (huellas)**. Se rearman solos desde el loteo.
- **Terreno**: después de cambiar lotes o caminos hay que rehacer el relieve (caminos hundidos y zanjas), la pintura del suelo y los árboles con `RelievePago` (por ahora desde el MCP; queda pedido un menú para desarrollo). Eso pisa lo que se haya esculpido o pintado a mano.

Las posiciones de la estancia, la posta y los demás sitios no se movieron (los usan la fauna, el mapa y las teclas 1 a 8): lo que cambió es todo lo que los rodea.

## El problema

Hoy el pago es un campo verde parejo con piezas sueltas encima: parcelas sembradas en cualquier lado, sendas que cruzan corrales y nada que diga de quién es cada pedazo de tierra. Un campo de verdad tiene un orden: dueños, límites, usos y caminos que respetan esos límites. Ese orden sale del loteo, y todo lo demás (pasto, sembrados, cercos, árboles, sendas) se arma a partir de él.

## Cómo se ordenaba la tierra en 1867 (lo que usamos)

*Datos de época a verificar con una fuente antes de darlos por buenos.*

- **El campo es ganadero.** En la provincia de Buenos Aires de esos años lo que manda es la oveja (la "fiebre del lanar") y la vaca. La estancia es casi toda campo abierto, dividido en **puestos**: un rancho con su puestero y su majada.
- **No hay alambrados.** El alambre recién empieza; casi nadie lo tiene. Los límites se marcan con **mojones**, con **zanjas** (cavadas por zanjeadores, muchas veces vascos o irlandeses) y con **cercos** de palo a pique o cercos vivos (tuna, cina-cina).
- **La agricultura es chica y está cerrada.** Como los animales andan sueltos, lo sembrado tiene que estar protegido: zanja con bordo de tierra y, a veces, cerco vivo encima. Se siembra trigo y maíz para el consumo y para vender en el pueblo, no extensiones grandes.
- **Las chacras están cerca del pueblo o de la posta**, en lotes angostos y largos con **frente al camino** y fondo hacia el campo. Más lejos están las estancias. Entre medio quedan lotes sin ocupar, marcados con mojones.
- **Medidas de época**: vara (≈ 0,87 m), cuadra (150 varas, ≈ 130 m), legua (6.000 varas, ≈ 5,2 km). Una suerte de estancia medía unas 3.000 × 9.000 varas: más grande que todo nuestro mapa (3,2 × 2,4 km). Así que el mapa muestra **un pedazo** de la estancia, no la estancia entera.

## La propuesta

| Zona | Dueño | Uso | Límite | Qué se ve |
|---|---|---|---|---|
| **Estancia La Soledad** (noroeste, hasta el camino real y el arroyo) | Vicente Larralde | Casco, potreros y campo de pastoreo | Mojones con los vecinos; potreros con palo a pique | Pasto comido alrededor del casco, potreros pelados, majadas sueltas |
| Casco | Vicente | Casa, galpones, corrales | Corrales de palo a pique | Ombúes y talas de sombra, suelo pisoteado |
| Quinta del casco | Matilde | Huerta y frutales (durazno, membrillo) | Zanja y cerco de tuna | Verde oscuro, árboles chicos en filas |
| Chacra de la estancia | Vicente | Trigo o maíz para la casa | Zanja con bordo | Un solo sembrado cerrado, con su tranquera |
| **Chacras de la posta** (a los dos lados del camino real, al este del arroyo) | Chacareros (personajes secundarios a definir) | Trigo, maíz, huerta, rastrojo | Zanja con bordo; tranquera al camino | Lotes angostos y largos, cada uno en un momento distinto del ciclo; algún rancho de chacarero con su parva |
| Lote de la posta | Petrona Silva y Ramón Peralta | Posta, pulpería, corral de remuda | Corral de palo a pique | El lugar más pisoteado del mapa |
| Lotes sin ocupar (entre chacras) | Nadie todavía | Pasto | Mojones | Pasto alto, el mojón en la esquina |
| **Campo abierto** (sur del camino) | Sin dueño claro / tierra pública | Pastoreo común | Ninguno | Pasto alto, matorral, pajonal, laguna; hacia el sur, la frontera |
| Monte (noreste) | De nadie | Leña | Ninguno | Talas y espinillos tupidos |

### Caminos

- **Camino real**: queda donde está. Es el eje: las chacras miran hacia él.
- **Callejones**: entre lotes de chacras, rectos, para llegar al fondo de los lotes, a la laguna y al monte. Reemplazan a las sendas que hoy cruzan en diagonal.
- **Sendas del campo**: solo en el campo abierto y dentro de la estancia, y entrando a los corrales por la tranquera, nunca atravesando un cerco.

### Lo que esto le da a la historia

- Las chacras de la posta son **lo que el Gallego vino a buscar**: tierra propia para sembrar. Las tiene al lado todos los días, y no son suyas.
- Los chacareros son buenos candidatos para personajes secundarios (`charla-ia.md`): otros inmigrantes, algunos con papeles en regla y otros no.
- Un lote sin ocupar con un mojón puede ser un lugar de la trama (¿el del italiano?).

## Cómo se construiría (pedido para desarrollo)

1. **Loteo como datos**: un objeto *Loteo* en la escena con lotes hijos. Cada lote tiene un polígono que se edita en la escena (como los cercos), un **uso** (casco, quinta, chacra, potrero, campo abierto, sin ocupar), un **límite** (zanja, palo a pique, cerco vivo, mojones, nada), un **dueño** (id de ficha) y sus parcelas.
2. **El loteo arma el resto**:
   - Zanjas con bordo de tierra (malla, porque el terreno no tiene resolución para una zanja de un metro), cercos, mojones y tranqueras.
   - Sembrados solo adentro de chacras y de la quinta, con orientación siguiendo el lote.
   - Pasto según el uso del lote (en lugar de la distancia a las casas que se usa hoy).
   - Matorral solo en campo abierto y monte.
   - Callejones y sendas como parte del loteo, no del trazado viejo del prototipo.
3. **Mientras tanto**: los siete sembrados de prueba se mueven o se borran cuando esté el loteo.

## Preguntas abiertas

1. ¿Se acepta el reparto general del plano (estancia al noroeste, chacras en la posta, campo abierto al sur)?
2. ¿Quiénes son los chacareros? ¿Cuántos personajes nuevos queremos?
3. ¿Vicente tiene algún potrero alambrado como novedad cara (marca de estatus), o nada de alambre?
4. ¿Hay ovejas en el pago? Hoy hay vacas y caballos; en 1867 deberían ser mayoría las ovejas.
5. ¿El lote del italiano existe y se puede encontrar?
