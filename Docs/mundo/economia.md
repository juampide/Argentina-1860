# Economía, oficios y recetas

Estado: **en el juego** la base y la pesca (desarrollo). Los precios, los peces, las recetas, quién enseña qué y los próximos oficios son decisiones de diseño, y casi todo se cambia desde fichas sin tocar código.

## Lo que ya anda

- **Una sola partida** para los dos mundos: la plata, el morral, las habilidades y las recetas que sabe el Gallego pasan del pago a Buenos Aires y vuelven. Todavía no se guarda en disco: se pierde al cerrar el juego.
- **Morral** (tecla **I**): lo que lleva, cuánto pesa, botones para comer y tirar, las habilidades y las recetas que se hacen en cualquier lado. A pie carga 15 kg; con el caballo, 40 (las alforjas).
- **Hambre y cansancio**, suaves:
  - El hambre se llena en unos 30 minutos de juego. El cansancio sube al correr y al trabajar.
  - Pasado el 70 % dan desventajas: corre menos, hasta quedar caminando, y rinde menos al trabajar. Nunca se muere.
  - Las barras aparecen arriba al centro cuando ya se notan.
  - Se come desde el morral. Se descansa sentado junto a un fogón encendido.
- **Pulpería de Don Ramón**: vende y compra lo que dicen las fichas de objeto. Vende yerba, tabaco, caña, tela, sal, galleta, línea, anzuelos y achuras. Compra lo mismo (salvo las cosas de pesca), los pescados y el pescado salado. Lo que se pescó o se juntó gratis se vende con ganancia entera.
- **Pesca** (tecla **F** mirando a la laguna o al arroyo):
  - Hace falta línea de crin, anzuelo y carnada. La carnada pueden ser lombrices, mojarras o achuras.
  - Se tira la línea y se espera a que se hunda el corcho; entonces hay que apretar F enseguida.
  - Después hay que mantener la tensión en la franja verde: apretando se recoge y sube la tensión, y soltando baja.
  - Si la tensión llega al rojo, se corta la línea y se pierde el anzuelo. Si queda floja mucho rato, el pez se suelta. Si no se tira a tiempo cuando pica, el pez se come la carnada.
  - Con la habilidad de pesca se tiene más tiempo para reaccionar, la franja es más ancha y se saca más rápido.
- **Peces**:
  - **Mojarra**: en todos lados; es carnada, no se vende.
  - **Pejerrey**: en la laguna, al alba y al atardecer.
  - **Tararira**: de día, y pelea mucho.
  - **Bagre**: de noche.
- **Juntar lombrices** (F en la orilla, cuando no se puede pescar): la carnada gratis.
- **Fogón** (F junto a un fogón encendido): cocinar lo que se sabe y sentarse a descansar. Los fogones se prenden a la mañana y a la tarde, según su horario; apagados no sirven, porque todavía no hay leña para prenderlos.
- **Recetas que se aprenden**:
  - **Pescado asado**, en el fogón: el Gallego la sabe desde el principio.
  - **Pescado salado**, en cualquier lado, con 2 pescados y 1 de sal: se la enseña **Jacinta** cuando se habla con ella.

  Las recetas que se saben aparecen en el morral o en el fogón.

## Cómo se editan (diseño)

| Qué | Dónde | Qué se cambia |
|---|---|---|
| Objetos (mercadería, herramientas, carnada, peces, comida) | `Assets/Resources/Objetos/*.asset` (*Create → Argentina 1860 → Ficha de objeto*) | Nombre, descripción, peso, precio, si Don Ramón lo vende o lo compra, si las noticias mueven su precio, cuánto alimenta y qué dice el Gallego al comerlo. En los peces: si viven en la laguna o en el arroyo, cuánto abundan, cuánto tiran, a qué hora pican más y desde qué nivel de pesca |
| Recetas | `Assets/Resources/Recetas/*.asset` (*Create → Argentina 1860 → Receta*) | Ingredientes (un objeto o "cualquiera de una categoría", por ejemplo `pescado`), resultado, dónde se hace (cualquier lado, fogón, banco del rancho, herrería), segundos, si se sabe desde el principio, quién la enseña (su ficha de personaje) y qué dice al enseñarla |

Reglas para no romper nada:
- El **id** de una ficha no se cambia una vez usado: lo usan las recetas, el código y la partida guardada que viene.
- Una ficha nueva tiene que estar en esas carpetas para que el juego la encuentre.
- Un personaje enseña **una receta por charla**, después de sus líneas de siempre.
- El menú *Argentina 1860 → Economía → Crear las fichas que falten* vuelve a crear solo las fichas que se hayan borrado; las existentes no las toca.
- Hay tests que revisan las fichas: ids repetidos, recetas que piden algo que no existe, recetas que nadie enseña, peces que no viven en ningún lado. Si uno falla después de editar, avisar a desarrollo.

## Decisiones para el guion (pendientes)

### Plata y precios
- ¿Cuánto tiene que ganar el Gallego con un día de pesca, comparado con el arreo (8 a 15 pesos) y con lo que cuesta lo de la pulpería? Hoy, una buena tarde de pesca da unos 20 o 30 pesos.
- ¿Se usa "pesos" a secas, o se distingue el peso moneda corriente (papel, muy devaluado) del peso fuerte (metal)? En 1867 convivían; puede dar juego con los precios de Buenos Aires.
- ¿Para qué junta plata el Gallego? El sueño es volver a buscar a su hija (`protagonista.md`). ¿Una meta concreta, como el pasaje, mandar giros o comprar un caballo o un terreno?

### Recetas y quién las enseña
- ¿Quién más enseña y qué? Ideas de época:
  - **charqui** (carne salada y secada), Santos o Doña Petrona;
  - **trenzar tientos** para hacer lazo, maneas o boleadoras, Hilario el baqueano;
  - **remedios de yuyos**, Jacinta, que encaja con que sea curandera;
  - **mate cocido** o **tortas fritas** en el fogón, Doña Petrona;
  - **enastar un hacha**, Don Roque.
- ¿Algunas recetas se ganan con favores o misiones, en vez de enseñarse en la primera charla?
- ¿El Gallego, labrador de la meseta, sabe de entrada algo que los paisanos no saben, como hacer pan, trillar o arar mejor?

### Próximos oficios (orden propuesto)
1. **Leña y combustible**:
   - hacha en el monte del noreste, que no tiene dueño;
   - bosta seca y cardo en la pampa, que era lo que se quemaba;
   - prender un fogón propio.

   Decidir si cortar en tierra con dueño es robo y quién se enoja.
2. **Changas a jornal**:
   - siega del trigo con Anselmo (el Gallego es labrador: lo único que hace mejor que los paisanos);
   - esquila, porque 1867 es plena "fiebre del lanar";
   - zanjeo, típico de inmigrantes.

   Decidir quién da cada trabajo, cuánto paga y si se cobra por día o por tarea.
3. **Caza y cueros**: perdices, vizcachas, plumas de ñandú, cueros de la carneada.
4. **Herrería en Buenos Aires**: anzuelos, hachas, herraduras.

### Comida y necesidades
- ¿Dónde come el Gallego si no cocina? ¿Le dan de comer en la estancia como peón? ¿La fonda de la posta?
- ¿Dormir? Hoy se descansa sentado junto al fogón. ¿Hace falta dormir de noche (el día dura 24 minutos)?
- El pescado asado "hay que comerlo en el día": ¿queremos que la comida se eche a perder? Le daría sentido al pescado salado.

## Pendiente con desarrollo
- Guardar la partida en disco (plata, morral, recetas, caballo).
- Prender fogones propios con leña.
- Enseñar recetas también en Buenos Aires (hoy solo en el pago).
- Los próximos oficios, cuando se decidan arriba.
