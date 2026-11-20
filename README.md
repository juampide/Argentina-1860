# Argentina 1860

Rol de exploración en 3D, *low poly* sucio y de época, ambientado en la Argentina de 1867. Proyecto en **Unity 6 (6000.6.9f1) con URP**, migrado del prototipo web en three.js (que sigue entero en la rama `main`).

Están migrados los dos mundos: el **pago** (campo, escena `Assets/Escenas/Pago.unity`) y **Buenos Aires** (escena `Assets/Escenas/BuenosAires.unity`).

**Abrir:** en Unity Hub, *Add → Add project from disk* y elegir la carpeta raíz del repo. Después abrir una de las dos escenas, darle Play y hacer clic en la ventana Game. **9** pasa de un mundo al otro (provisorio, hasta que esté el paso de la historia). Las teclas 1 a 8 llevan a cada lugar del pago o junto a cada personaje de la ciudad (viaje rápido para pruebas; se apaga en el objeto **Juego**).

| Tecla | Acción |
|---|---|
| WASD / flechas | caminar |
| Shift | correr |
| Q / R | girar la vista de a 90° |
| Rueda del mouse | zoom |
| E | hablar, avanzar el diálogo, abrir o cerrar la tranquera, salir de la tienda, montar o bajarse del caballo |
| Shift a caballo | galopar (gasta aliento) |
| H | silbarle al caballo para que venga |
| O | darse el caballo (pruebas, hasta que esté la misión) |
| P | darse $50 (pruebas; se apaga con `Caballeriza.TRUCOS`, igual que la O) |
| F | pescar (mirando a la laguna o al arroyo), juntar lombrices en la orilla, cocinar o descansar en un fogón encendido |
| I | morral: lo que llevás, comer, tirar, habilidades y recetas |
| M | mapa grande |
| Esc | cerrar el mapa, la tienda o el morral; dejar de pescar o de cocinar |
| 1 a 8 | viaje rápido a cada lugar del pago o junto a cada personaje de la ciudad (pruebas) |
| 9 | pasar del pago a Buenos Aires y viceversa (provisorio) |
| 0 | cuadros por segundo y milisegundos por cuadro (el juego está limitado a 60 cuadros por segundo en `Juego/Rendimiento.cs`) |

## Qué hay

- **Terreno** de 3,2 × 2,4 km con lomas, arroyo, laguna, camino real y sendas. **Flora** autóctona (1.215 plantas) y **pasto alto** que se mece y se aplasta cerca del jugador.
- **Construcciones**: estancia La Soledad, ranchos, posta, fortín de frontera (estacada de palo a pique, foso, mangrullo, ranchos de adobe y mástil), puesto del baqueano y rancho de Casiana. Cercos, fogones con horario y ronda de gente, palenques, pozos y carteles.
- **Bordes del pago** (`Docs/mundo/bordes-pago.md`): el jugador queda encerrado sin que el mapa parezca cortado. Al norte, el bañado donde nace el arroyo; al este, un monte cerrado que el camino real cruza por una picada; al sur y al oeste, el cañadón de la frontera, con un mangrullo de guardia en el vado. Del otro lado del cañadón, tierra adentro, se ve una toldería. Al querer pasar, un aviso explica por qué no se puede.
- **Interiores**: rancho de peones amueblado y pulpería completa (mostrador, balanza, vitrina, estantes, mesas). Al entrar se ocultan el techo y las paredes que dan a la cámara.
- **Quince personajes** con nombre, ficha y diálogo. Fermín da el **arreo de vacas** (minijuego con tranquera y tiempo) e Isidro abre la **pulpería** (precios que fluctúan con noticias, habilidad de comercio).
- **Fauna con vida propia**: vacas, caballos (potreros, corrales que se abren, atados al palenque), ñandúes, venados, zorros, pumas, carpinchos, coipos, vizcachas con vizcacheras, peludos, perdices, yararás y caranchos. **Eventos** cerca del jugador: jinetes al galope por el camino real, perdices que se vuelan y carretas de bueyes (Pascual camina junto a la primera).
- **Minimapa** que gira con la cámara, con lugares y distancias, y **mapa grande** con la tecla M.
- **Aspecto**: cámara isométrica, silueta del personaje cuando algo lo tapa, recorte tramado de árboles y cercos, suciedad procedural (barro, hollín, chorreaduras, grietas del adobe) y corrección de color polvorienta.
- **Caballo propio** en los dos mundos: montar y bajarse, trote y galope con aliento, silbido para llamarlo. Cómo se consigue lo decide el guion (`Docs/guion/caballo.md`); por ahora la O lo da para probar.
- **Economía y oficios** (`Docs/mundo/economia.md`): una sola partida para los dos mundos (plata, morral con peso, habilidades, recetas). Hambre y cansancio suaves, que solo dan desventajas. **Pesca** en la laguna y el arroyo (pejerrey, tararira, bagre, mojarra), con corcho y tensión de la línea. Lombrices en la orilla. **Recetas que se aprenden**: pescado asado en el fogón; pescado salado, que enseña Jacinta. La pulpería compra y vende según las fichas de objeto.
- **Buenos Aires**: la ciudad armada desde el plano del Departamento Topográfico de 1867. Son 3.612 manzanas: en el centro, lotes con casas de paredes pastel, techos de teja o planos con cornisa, zócalo, puertas, ventanas con reja y balcones; más lejos, volúmenes simples. Veredas elevadas con cordón, calles empedradas, 35.660 árboles y farolas. Plaza de la Victoria con la Pirámide de Mayo, el Cabildo y la Recova, 16 iglesias (con la Catedral) y el río. Misión "La carta urgente" (Anselmo → Remedios → el sacristán → Anselmo) con haz de luz sobre el objetivo, ocho personajes, transeúntes, carros con caballo y minimapa.

## Qué se puede editar (diseño)

| Qué | Dónde | Cómo |
|---|---|---|
| Relieve, caminos y agua | objeto **Terreno** | Herramientas de Terrain de Unity: esculpir y pintar con las 4 capas (pasto verde, pasto seco, tierra, agua) |
| Árboles y matas | objeto **Terreno** → pestaña de árboles | Pintar o borrar. Especies en `Assets/Mundo/Pago/Flora`. Los árboles tienen colisión en el tronco (las matas no); si se agrega una especie, pedir a desarrollo que la sume a `GeneradorPago.TRONCOS` |
| Casas, cercos, fogones, palenques, pozos, carteles, mástil | **Construcciones** | Mover, rotar o duplicar el objeto y cambiar las medidas en el Inspector. La pieza se rearma sola. Rancho de peones y pulpería son edificios con interior |
| Suciedad, grietas y recorte de cada material | Materiales en `Assets/Arte/Materiales` (shader `Argentina/Mundo`) | Valores *Suciedad*, *Grietas* y *Recorte tramado* |
| Personajes: nombre, aspecto, diálogos | `Assets/Datos/Personajes/*.asset` | Editar la ficha. El modelo cambia al instante. Tiene campos para el futuro NPC con LLM (personalidad, qué sabe y qué no) |
| Texturas y colores de materiales | `Assets/Arte/Materiales`, `Assets/Resources/MaterialesMundo.asset` | Cambiar textura o color del material |
| Luz, niebla y color | **Sol**, **PostProceso** (`Assets/Mundo/Pago/PerfilPago.asset`) | Inspector |
| Hora, clima y viento | Objeto **Ambiente** (componente `Ambiente`) | Hora de arranque, minutos reales por día, clima automático o fijo (despejado, nublado, tormenta, pampero), nubosidad, lluvia y viento |
| Carácter de cada animal | `Assets/Scripts/Pago/Fauna.Conducta.cs`, tabla `RASGOS` | Distancia de alerta y de huida, horario, manada, aguada, siesta, cueva, presas (por ahora en código: pedir a desarrollo pasarlo a una ficha) |
| Loteo: lotes, dueños, usos, lindes y caminos | `Assets/Resources/LoteoPago.asset` | Ver `Docs/mundo/loteo-pago.md`. Los caminos, las zanjas, el pasto y los árboles salen de acá |
| Sembrados | **Construcciones → Sembrados** (componente `Sembrado`) | Mover, rotar o duplicar. Elegir cultivo (trigo o maíz), etapa (arado, sembrado, brotando, creciendo, maduro, cosechado), tamaño y fallas. El pasto no crece adentro. Se puede caminar por arriba, y el trigo y el maíz se abren alrededor del jugador (valor *Se abre alrededor del jugador* del material). Si se pone uno nuevo, sacar a mano los árboles que queden adentro |
| Huellas del camino | **Construcciones → Camino real (huellas)** y **Sendas (huellas)** (componente `Huellas`) | Pares de huellas, trocha, ancho, vaivén, charcos y matas. Siguen el trazado de `MapaPago`. Después de esculpir el terreno: clic derecho en el componente → *Regenerar* |
| Bordes del pago: bañado, monte, cañadón | `Assets/Scripts/Pago/BordesPago.cs` (líneas y avisos) | Ver `Docs/mundo/bordes-pago.md`. Para cambiar dónde están las orillas hay que tocar el código (pedir a desarrollo) y volver a aplicar `RelievePago.Bordes` |
| Agua | **Construcciones → Agua** (componente `AguaViva`, shader `Argentina/Agua`, material `AguaViva`) | Cubre todo lo pintado con la capa Agua del terreno. Colores de lo hondo y la orilla, opacidad, tamaño de las ondas y destellos en el material. Después de repintar el agua: clic derecho → *Regenerar* |
| Viento en árboles y matas | Materiales en `Assets/Mundo/Pago/Flora/Materiales` (valor *Se mece con el viento*) | 0 quieto, 1 normal, 2 mucho. Sigue el viento de `Ambiente` |
| Puentes del arroyo | **Construcciones → Puentes del arroyo** (componente `PuentesArroyo`) | Se arman solos donde un camino del loteo cruza el arroyo: ancho, apoyos sobre la orilla y cuántos tablones faltan. Si cambia el trazado, volver a hundir el cauce y pintarlo de agua con `PuentesArroyo.HundirCauces` (MCP) |
| Fortín | **Construcciones → Fortín**: `Estacada`, `Mangrullo`, `Casa` | Puntos de la estacada, lado del portón, alto y foso; alto del mangrullo |
| Toldería | **Construcciones → Toldería (tierra adentro)** (componente `Tolderia`) | Cantidad de toldos, gente y caballos, y semilla. El fuego es el `Fogon` hijo |
| Objetos: mercadería, herramientas, carnada, peces, comida | `Assets/Resources/Objetos/*.asset` (*Create → Argentina 1860 → Ficha de objeto*) | Nombre, descripción, peso, precio, si la pulpería lo vende o lo compra, cuánto alimenta. En los peces: dónde viven, cuánto abundan, cuánto tiran y a qué hora pican. Ver `Docs/mundo/economia.md` |
| Recetas | `Assets/Resources/Recetas/*.asset` (*Create → Argentina 1860 → Receta*) | Ingredientes, resultado, dónde se hace, si se sabe desde el principio, quién la enseña y qué dice al enseñarla |
| Árboles y cercos que tapan al jugador | Cámara → componente `CamaraIsometrica` | *Radio recorte* (metros) y *Transparencia recorte* (1 se borran del todo; 0,75 quedan a medias, como un fantasma) |
| Pasto corto cerca de la gente y pisadas | **Juego** → componente `Pasto` | Altura y alcance del pasto comido, segundos que tarda en levantarse una mata pisada y cuánto se dobla |

Los objetos hijos `_generado` no se guardan en la escena y no se editan a mano: salen de los parámetros del componente.

## Regenerar desde el prototipo

Menú **Argentina 1860 → Pago**. Cada paso reemplaza lo que ese paso crea en la escena:

1. Materiales y texturas (no pisa materiales existentes)
2. Terreno (**reemplaza el relieve y la pintura del suelo**)
3. Flora (**reemplaza los árboles pintados**)
4. Construcciones
5. Personajes (no pisa las fichas existentes)
6. Jugador, cámara y luz

Una vez que el diseño empiece a editar el terreno o las construcciones a mano, no conviene volver a correr esos pasos.

## Código (`Assets/Scripts`)

- `Nucleo/`: conversión de coordenadas three.js → Unity (`Conv`), geometría low poly igual a la del prototipo (`Geo`, `MallaTres`), base de los objetos armados por código (`ProceduralBase`) y materiales con nombre.
- `Pago/`: `MapaPago` (trazados, relieve, agua y caminos de `campo.js`; `Pastoreo` dice cuánto se nota la gente en cada lugar), `Flora`, `Pasto` (matas por GPU, pasto corto cerca de las casas y textura de pisadas) y `Pisadas` (registro de lo que aplasta el pasto: jugador, personajes, animales grandes y carretas).
- `Mundo/`: `Casa`, `Cerco`, `Fogon`, `Palenque`, `Pozo`, `Sembrado`, `Huellas` (caminos con surcos), `Lindes` (zanjas, bordos, tunas, mojones, tranqueras) y `ObraMalla` (mallas grandes apoyadas en el terreno).
- `Pago/LoteoPago` (datos del loteo) y `Pago/RelievePago` (rehace relieve, suelo y árboles a partir del loteo).
- `Personajes/`: `Aspecto`, `FichaPersonaje`, `ConstructorPersonaje` (de `personaje.js`), animación y `NPC`.
- `Juego/`: cámara isométrica, control del jugador (a pie y a caballo), diálogos y HUD (provisorio, IMGUI). `Caballeriza` (el caballo del jugador entre mundos, montar, silbar) y `CaballoMontable` (el caballo con recado, aliento y llamado) se instalan solos, sin estar en las escenas.
- `Economia/`: `Partida` (plata, morral, habilidades, recetas, hambre y cansancio; pasa de un mundo al otro), `FichaObjeto`, `FichaReceta`, `Catalogo` (lee las fichas de `Resources`), `Recetario`, `LancePesca` (la pesca sin pantalla) y `Oficios` (morral, pesca, lombrices y fogón; se instala solo, como `Caballeriza`).
- `Ciudad/`: `DatosCiudad` (lee el plano: `Assets/Datos/Ciudad`), `Ciudad` (arma manzanas, monumentos, iglesias, farolas, suelo y río), `ArbolesCiudad` (árboles por GPU), `ModelosCiudad` (carro y texturas pintadas por código) y `JuegoCiudad` (misión, personajes, transeúntes, carros, minimapa).
- `Editor/`: los generadores del pago y de la ciudad (menú *Argentina 1860*), los datos del elenco y `FichasEconomia` (*Argentina 1860 → Economía → Crear las fichas que falten*: no pisa las existentes).

El código del juego está en el ensamblado `Argentina1860` (`Assets/Scripts`) y las herramientas del editor en `Argentina1860.Editor` (`Assets/Scripts/Editor`). Un script nuevo dentro de esas carpetas entra solo en el ensamblado que corresponde.

## Tests (`Assets/Tests`)

Menú **Argentina 1860 → Tests**, o la ventana *Window → General → Test Runner*. El resumen queda en `Logs/tests.txt`.

- **Tests de lógica** (`EditMode`, unos 3 segundos): conversión de coordenadas y colores, caras de las mallas, agua del pago, obstáculos y veredas de la ciudad, datos del plano de 1867, precios de la pulpería, aliento del caballo, morral, recetas, hambre y cansancio, el lance de pesca y que las fichas de objeto y receta estén completas.
- **Tests del juego** (`PlayMode`, unos 45 segundos): cargan el pago y la ciudad y revisan que el jugador esté en el piso, que estén los personajes, que las casas bloqueen y las veredas se suban, que el caballo galope, se baje, venga al silbarle y cambie de mundo montado, que se pueda pescar en la laguna y que la plata y el morral pasen a la ciudad. Cualquier error en la consola hace fallar el test. Hay que guardar la escena antes de correrlos.

Conviene correr los dos antes de subir cambios de código.

**Coordenadas.** El prototipo usa x hacia el este y z hacia el sur. En Unity la z queda al revés (el norte del mapa es +Z). `MapaPago` trabaja en coordenadas del prototipo. Para pasar a Unity se usa `MapaPago.AUnity` o `Conv.P`.

## Git: LFS y Smart Merge

Las texturas, el terreno y las mallas generadas van por **Git LFS** (`brew install git-lfs`, después `git lfs install`). Para que Git fusione escenas y prefabs con la herramienta de Unity, configurar una vez:

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver '"/Applications/Unity/Hub/Editor/6000.6.9f1/Unity.app/Contents/Helpers/UnityYAMLMerge" merge -p %O %B %A %A'
```

Ajustar la ruta a la versión de Unity instalada (en Windows: `C:\Program Files\Unity\Hub\Editor\<versión>\Editor\Data\Tools\UnityYAMLMerge.exe`).

## Falta portar

- La interfaz es provisoria (IMGUI): falta diseñarla.
- La ciudad todavía no tiene el clima, la noche ni el pasto corto que tiene el pago.
- El paso del pago a la ciudad dentro de la historia (hoy es la tecla 9).
- Hoja de ruta del diseño (del prototipo): doma de caballos y faena con minijuegos, combate a pie y a caballo, interiores de la estancia y los demás ranchos, el malón que arrasa el pago, NPC con modelo de lenguaje.

## Datos y créditos

- El plano y los datos de la ciudad provienen del trabajo sobre el plano del Departamento Topográfico de 1867 publicado en baires.mormon.garden. **Antes de hacer público este repositorio, conviene confirmar la licencia de esos datos.**
- Telas y texturas: CC0 (ambientCG). Referencias visuales de ranchos y pulperías: fotografías de época.
