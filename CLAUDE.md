# Argentina 1860 · instrucciones para agentes

Juego de rol en 3D, *low poly* sucio y de época, ambientado en la Argentina de 1867. Lo hacen dos personas, cada una con su agente de Claude:

- **Diseño e historia**: personajes, diálogos, historia, misiones, escenarios, arte. Edita en el editor de Unity y en los datos.
- **Desarrollo**: sistemas, física, código C#, herramientas, integración del NPC con modelo de lenguaje.

Antes de cambiar algo, fijate a qué lado pertenece (tabla de abajo). Si una tarea de diseño necesita código nuevo (una mecánica, un componente, un campo nuevo en una ficha), proponelo y dejalo anotado para desarrollo en vez de escribir sistemas por tu cuenta. Retocar textos, valores y datos sí es diseño.

Todo en **español rioplatense**: textos del juego, nombres, comentarios y mensajes de commit.

## Preparación (una vez por máquina)

1. **Unity 6000.6.9f1** (exactamente esa versión, la fija `ProjectSettings/ProjectVersion.txt`). En Unity Hub: *Add → Add project from disk* → la carpeta raíz del repo.
2. **Git LFS**: `brew install git-lfs` (o el instalador de Windows) y después `git lfs install`. Texturas, terreno y mallas generadas van por LFS.
3. **Fusión de escenas** (Smart Merge de Unity): ver la sección "Git: LFS y Smart Merge" del `README.md`.
4. **Unity MCP**, para que el agente maneje el editor: con el proyecto abierto, *Edit → Project Settings → AI → Unity MCP*. El bridge tiene que decir *Running*. En la terminal:
   - Mac (Apple Silicon): `claude mcp add unity-mcp -- ~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 --mcp`
   - Windows: `claude mcp add unity-mcp -- "%USERPROFILE%\.unity\relay\relay_win.exe" --mcp`
   
   Reiniciar Claude Code y aceptar la conexión en Unity (*Pending Connections*). Alcanza con el plan Unity Personal. No usar las herramientas `Unity_AssetGeneration_*`: consumen créditos pagos de Unity AI.

## Qué edita cada uno

| Qué | Dónde | Dueño |
|---|---|---|
| Personajes: nombre, lugar, aspecto, diálogos, personalidad, qué sabe y qué no | `Assets/Datos/Personajes/*.asset` (fichas `FichaPersonaje`) | Diseño |
| Ubicación de personajes, casas, cercos, fogones, carteles | Escena `Assets/Escenas/Pago.unity`, objetos bajo **Construcciones** y **Personajes** | Diseño |
| Relieve, pintura del suelo, árboles | Objeto **Terreno** (herramientas de Terrain de Unity) | Diseño |
| Colores, texturas, suciedad y grietas de materiales | `Assets/Arte/Materiales`, `Assets/Resources/MaterialesMundo.asset` | Diseño |
| Luz, niebla y corrección de color | Objetos **Sol** y **PostProceso** (`Assets/Mundo/Pago/PerfilPago.asset`) | Diseño |
| Textos de misión iniciales | Objeto **Juego** → componente `JuegoPago` | Diseño |
| Historia, guion, biblia de personajes, notas de época | `Docs/` (Markdown) | Diseño |
| Código C#, shaders, generador del pago | `Assets/Scripts`, `Assets/Shaders` | Desarrollo |
| Configuración del proyecto, paquetes, renderer | `ProjectSettings/`, `Packages/`, `Assets/Settings/` | Desarrollo |
| Personajes de Buenos Aires | `Assets/Datos/Personajes/Ciudad/*.asset` y escena `Assets/Escenas/BuenosAires.unity` (objeto **Personajes**) | Diseño |
| Detalle de la ciudad (radios de casas, decoración, centro) y materiales de la ciudad | Objeto **Ciudad** en `BuenosAires.unity`, `Assets/Arte/Materiales/Ciudad` | Diseño |
| Datos del plano de 1867 | `Assets/Datos/Ciudad` (no se editan a mano) | Desarrollo |

## Cómo está armado (lo que hay que saber para no romper nada)

- **Piezas armadas por código.** Casas (`Casa`), cercos (`Cerco`), fogones, palenques, pozos, carteles, mástil, rancho con interior (`Habitacion`), pulpería (`Pulperia`) y personajes (`ModeloPersonaje`) generan su geometría a partir de los valores del componente. Para cambiarlos se mueve, rota o duplica el objeto y se cambian los valores en el Inspector. **Nunca editar los hijos `_generado`**: no se guardan y se rearman solos.
- **El aspecto de un personaje está en su ficha.** Cambiar estilo, colores, sombrero, peinado o accesorio en la ficha rearma el modelo al instante. Para crear un personaje nuevo: crear la ficha (*Create → Argentina 1860 → Ficha de personaje*) y un objeto en la escena con `ModeloPersonaje` (con la ficha asignada), `NPC` y un `CapsuleCollider` (centro y 0,9, radio 0,4, alto 1,8), en la capa **Personaje**.
- **Diálogos.** Las líneas de la ficha son el diálogo básico. Fermín (arreo) e Isidro (pulpería) tienen lógica propia en `JuegoPago.Dialogo()`: cambiar sus textos ahí es diseño, pero cambiar la lógica es desarrollo. Los campos *personalidad*, *sabe* y *noSabe* son para el futuro NPC con modelo de lenguaje: escribirlos pensando en eso (quién es, cómo habla, qué sabe en 1867 y qué no puede saber).
- **Generador del pago** (menú *Argentina 1860 → Pago*). Rearma la escena a partir de los datos del prototipo. **No correr *Generar todo*, *2 · Terreno*, *3 · Flora*, *4 · Construcciones* ni *5 · Personajes* después de haber editado esas cosas a mano: los reemplaza.** Las fichas y los materiales existentes no se pisan. Ante la duda, no correrlo y preguntar.
- **Buenos Aires** sale de los datos del plano: el componente `Ciudad` arma manzanas, monumentos, iglesias y farolas al abrir la escena y al dar Play (no se guardan en la escena). El menú *Argentina 1860 → Ciudad → Generar Buenos Aires* **vuelve a crear la escena `BuenosAires.unity` desde cero**: no correrlo si ya se editó esa escena a mano. Las fichas existentes no se pisan.
- **Coordenadas.** El prototipo usaba x hacia el este y z hacia el sur. En Unity el norte del mapa es +Z. `MapaPago.K(kx, ky)` pasa del mapa en km a metros del prototipo, y `MapaPago.AUnity` pasa a Unity.
- **No editar a mano el YAML** de escenas, prefabs o materiales (`.unity`, `.prefab`, `.mat`): los GUID y fileID se rompen fácil. Usar el MCP de Unity (herramientas de escena y GameObjects, o `Unity_RunCommand` con C#) o pedirle a la persona que lo haga en el editor.

## Trabajar en paralelo sin pisarse

- Por ahora hay **una sola escena** (`Pago.unity`). Si los dos la tocan en ramas distintas, aparecen conflictos. Antes de editarla, avisar. Si una tarea larga de diseño va a tocar mucho la escena, conviene pedirle a desarrollo que la separe en escenas aditivas (mundo, construcciones, personajes).
- Las fichas, los materiales y los documentos de `Docs/` son archivos separados y casi nunca chocan.
- Una rama por tarea. Traer los cambios del otro (`git pull`) antes de empezar.
- **Nunca hacer commit ni push sin que la persona lo pida explícitamente.** Dejar los cambios listos y avisar.
- Un `.meta` va siempre en el mismo commit que su archivo. Si se pierde, se rompen las referencias.

## Probar

- **F9** pasa del pago a Buenos Aires y viceversa (provisorio). En la ciudad, F1 a F8 llevan junto a cada personaje.
- Abrir `Assets/Escenas/Pago.unity`, Play y clic en la ventana Game. **F1 a F8** llevan a cada lugar (ranchos, estancia con Fermín, corral del arreo, pulpería con Isidro, pajonal, laguna, monte, fortín). M abre el mapa y E sirve para hablar.
- Desde el agente, con el MCP: con Unity sin foco, el modo Play no avanza. Pausar y avanzar cuadros con `EditorApplication.Step()` dentro de `Unity_RunCommand`. **Al terminar, dejar `EditorApplication.isPaused = false` y salir de Play**: la pausa queda guardada y la próxima vez que la persona apriete Play, el juego arranca congelado.
- `Unity_RunCommand` no ve el ensamblado de Editor (para correr un menú: `EditorApplication.ExecuteMenuItem`) y no permite `System.Reflection`.
- Los mensajes "NoSubscription" y "Nature/Soft Occlusion shader" en la consola son esperables y no indican un problema.
- Revisar la consola después de cada cambio: los errores de compilación a veces aparecen con tipo "Log".

## Más información

`README.md`: qué hay en el juego, controles, estructura del código, lo que falta portar y la nota de licencia de los datos del plano (confirmarla antes de hacer público el repo).
