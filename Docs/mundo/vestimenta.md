# Vestimenta de época (1867)

Estado: **en el juego** (primera versión). La ropa se arma en `Assets/Scripts/Personajes/ConstructorPersonaje.cs` a partir del **Aspecto** de cada ficha: estilo, colores, tocado, peinado y accesorio.

## Qué hay

| Estilo | Para quién | Qué lleva |
|---|---|---|
| **Poncho** (gaucho) | Peones, puesteros, paisanos | Camisa, **chiripá** (color *Ropa*) sobre el **calzoncillo cribado** con flecos, **botas de potro** de cuero crudo con **espuela nazarena**, faja, **tirador** ancho con monedas, **facón** atravesado atrás y pañuelo al cuello. Poncho puesto (rombo con guarda) o, con *Poncho al hombro*, doblado sobre el hombro, y así se ven el tirador y el facón |
| **Vestido** | Mujeres del campo y del pueblo | Pollera larga, delantal, corpiño, **rebozo** con las puntas cruzadas al frente y la **enagua** que asoma abajo. Con el tocado *Pañuelo*, la cabeza cubierta y el nudo atrás |
| **Miriñaque** (nuevo) | Señoras de la estancia o del pueblo, de visita o en misa | Pollera acampanada sobre el miriñaque con tres volados, corpiño ajustado, **mangas pagoda**, chal y cuello de encaje, camafeo. Con el tocado *Mantilla*: mantilla negra sobre peineta |
| **Levita** | Estancieros, comerciantes, el juez | Levita, chaleco, corbata, **leontina del reloj**; galera o sombrero de ala |
| **Sotana** | El cura | Sotana y roquete; con el tocado *Teja*, el **sombrero de teja** |
| **Soldado** (nuevo) | La tropa del fortín | Chaqueta corta azul con botones y **cuello colorado**, **correaje blanco cruzado**, cinturón con **cartuchera**, pantalón claro o azul, kepí; fusil opcional |
| **Quillango** | La toldería | Ver `bordes-pago.md` |

Tocados nuevos: *Pañuelo*, *Mantilla*, *Teja* (y *Vincha*, de la toldería).

## Variantes y contextura

Cada ficha tiene dos valores más en el Aspecto:

- **Contextura** (0,85 a 1,2): de flaco a robusto.
- **Variante** (0 a 3): cambia la ropa dentro del mismo estilo.

| Estilo | 0 | 1 | 2 | 3 |
|---|---|---|---|---|
| Poncho | liso | a rayas | guarda ancha | poncho pampa (cruz tejida) |
| Vestido | con delantal | sin delantal | franja en la pollera | mantón largo |
| Miriñaque | volados | franjas oscuras | volados de otro color | chaquetilla |
| Levita | levita larga | saco corto | chaleco claro | — |
| Soldado | uniforme | poncho encima | chiripá y botas de potro | en camisa |
| Quillango | franjas pintadas | poncho pampa encima | sin pintura | — |

El sombrero de ala también cambia con la variante: de copa baja y ala ancha a copa alta y ala chica.

La **gente sin nombre** sale de `Variedad.cs`: gaucho, paisana, señora, caballero, soldado, cura, vendedor e indígena. Cada uno elige al azar piel, pelo, barba, alto, contextura, colores de época, sombrero y variante, y con la misma semilla sale siempre la misma persona. Lo usan los fogones, la toldería y los transeúntes de Buenos Aires (después de los tipos fijos de la lista original).

## Las fichas

| Personaje | Ropa |
|---|---|
| Don Roque Quiñones | Gaucho, poncho al hombro con guarda; la guitarra a la vista |
| Anselmo Lucero | Gaucho robusto, poncho al hombro a rayas; tirador y facón a la vista |
| Lorenzo | Poncho pampa |
| Hilario Acosta | Gaucho flaco, poncho puesto con guarda |
| Santos Sosa | Pasó de chaleco a gaucho, poncho al hombro |
| Juan Maidana | Pasó de chaleco a gaucho robusto, poncho al hombro a rayas |
| Jacinta Molina | Pañuelo y mantón largo |
| Doña Petrona Silva | Pañuelo, pollera con franja |
| Juana Benítez | Pañuelo y delantal |
| Doña Matilde Garmendia | Miriñaque sin volados |
| Carmen Larralde | Miriñaque con volados |
| Don Vicente Larralde | Levita con chaleco claro, robusto |
| Don Rufino Aramburu | Levita larga, robusto |
| Teniente Obregón | Saco corto, flaco |
| El centinela (ciudad) | Soldado de línea |

Ya se usan en la gente sin nombre: los paisanos de los fogones (gauchos con el poncho al hombro o puesto, paisanas con pañuelo y rebozo) y la ronda del fortín (estilo Soldado). Los personajes con ficha **no se cambiaron**: eso lo decide diseño en cada ficha (ver "Para decidir").

## De dónde sale

*Lo que no está confirmado con una fuente está marcado como a verificar.*

- **Gaucho**: chiripá sostenido con faja y tirador (cinto ancho de cuero, sin bolsillos), calzoncillo blanco cribado y con flecos, botas de potro (cuero crudo de la pata del potro). Fuentes: [Billiken](https://billiken.lat/para-tus-tareas/sabes-como-se-compone-la-vestimenta-del-gaucho/), [Encyclopedia.com: chiripá](https://encyclopedia.com/humanities/encyclopedias-almanacs-transcripts-and-maps/chiripa), [Google Arts & Culture: Sons of the Pampa](https://artsandculture.google.com/story/VgVxbPn5wwiFIA). La obra de referencia es Abel Gari, *Del chiripá al pantalón*. *A verificar*: hacia 1867 la bombacha ya existía, pero en la campaña bonaerense el chiripá seguía siendo lo más común. Billiken fecha el reemplazo en 1843, y eso parece temprano.
- **Mujeres trabajadoras**: blusa, rebozo para el frío, pollera fruncida sobre una enagua. Fuente: [El Litoral](https://www.ellitoral.com/opinion/elegancia-esclavitud-moda-femenina-mediados-siglo-constitucion-nacional-aniversario-sancion_0_pTtnLF2M1E.html), de divulgación; contrastar con [Moreyra, *Arenal*](https://revistaseug.ugr.es/index.php/arenal/article/download/5289/7208/24079).
- **Clase alta**: en los años 1860 el miriñaque estaba en uso en Buenos Aires y era tema de polémica; el censo de 1869 registra miriñaqueros. Fuente: [Garabana, *Paper Dresses* (prensa porteña de 1860 y 1870)](https://revistas.unc.edu.ar/index.php/anuariohistoria/article/download/31725/32551/105175). El **peinetón** grande es de 1830 y ya no se usaba ([GCBA: Los peinetones](https://buenosaires.gob.ar/gcaba_historico/la-cuarta-parte-del-mundo/los-peinetones)); por eso la peineta del juego es chica.
- **Soldados**: no encontré una fuente sobre el uniforme de la frontera entre 1865 y 1870. *A verificar todo*: chaqueta azul, cuello colorado, correaje blanco y pantalón claro salen de fotos de la Guerra del Paraguay. En la frontera la tropa andaba muy gastada y muchos soldados usaban chiripá y poncho con el kepí como única prenda de uniforme. Para el servicio en la Guardia Nacional de campaña ver [este estudio (1865-1870)](https://dialnet.unirioja.es/descarga/articulo/7350161.pdf), que no trata la ropa. El fusil Remington llega después de 1870.

## Para decidir (diseño)

1. Las fichas ya están pasadas (ver arriba); cualquier cambio se hace en el Inspector de cada ficha.
2. ¿Los soldados de la ronda del fortín deberían ir más gauchos (chiripá y poncho con kepí) que uniformados?
3. La peineta (de carey) quedó solo para las señoras con miriñaque. ¿Alguna paisana la usaría para ir a misa?
