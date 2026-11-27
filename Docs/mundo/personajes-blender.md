# Personajes modelados en Blender (piloto)

Estado: **en prueba**. Decidido con Juan: los personajes armados con piezas simples en código (`ConstructorPersonaje.cs`) llegaron a su techo (aspecto de maqueta). Probamos el camino habitual: modelar en Blender, exportar a Unity como FBX y usarlo como prefab. Si el piloto no da el salto de calidad buscado, se cambia de camino.

## Norte visual
- **Disco Elysium** (isométrico, pintado, sucio, desaturado, luz con carácter). Es también la referencia para la etapa del cólera en Buenos Aires.
- En esa vista el personaje se ve chico: lo que sostiene el look es la **textura pintada**, la **silueta** y la **luz**, no tanto la cantidad de detalle 3D.
- Dirección: dramático y sucio, nada de juguete. Proporciones más altas y flacas, hombros caídos, gesto cansado.

## Reparto
- **Juan**: dirección de arte. Junta referencias por personaje (fotos de época, dibujos), aprueba o corrige.
- **Claude**: modela en Blender con scripts de Python (subdivisión, cuerpo con anatomía, ropa con pliegues, pelo en mechones, textura pintada) y renderiza vistas para aprobar antes de llevar a Unity.
- **Federico**: lado Unity (importación FBX, esqueleto, animaciones, prefabs).

## Piloto: Teniente Obregón (`obregon`)
Según `Docs/guion/personajes.md` y `vestimenta.md`: jefe del fortín, militar, correcto y cansado; **saco corto, flaco**, estilo soldado/oficial de frontera, muy gastado.

Criterio de aceptación (antes de llevarlo a Unity):
1. Render de frente, perfil y tres cuartos que Juan apruebe.
2. Cara con expresión legible (neutra y cansada, al menos).
3. Manos con dedos, ropa con pliegues y costuras, pelo y bigote con volumen.
4. Se ve bien bajo el filtro pintado (F4) a la distancia del juego.

## Base elegida
La cara y el cuerpo armados por código no alcanzaron (se descartaron). Se usa el paquete **Human Base Meshes** de Blender Studio (se publica como CC0; "a confirmar con el texto oficial antes de publicar"): cuerpo masculino y femenino realistas (10.582 vértices, con niveles de escultura), cabeza para animación con ojos e iris, manos, pies, mandíbula y cráneo. Descargado en `D:\Unity\Descargas\human-base-meshes\bundle.blend` (49.423.697 bytes; la huella coincide con la del repositorio oficial: https://projects.blender.org/studio/asset-bundles). Scripts de trabajo en `Arte/blender/`.

## Pipeline
Blender (GPL; lo que se exporta es nuestro) → FBX → `Assets/Modelos/Personajes/` → prefab. El esqueleto y las animaciones de gestos pueden salir de Mixamo (a verificar sus términos de uso antes de publicar).

## Pendiente
- Referencias visuales por personaje (Juan).
- Decidir el esqueleto (Rigify propio o Mixamo).
- Medir costo de pasar todos los personajes si el piloto funciona.

## Animaciones de Mixamo (estado al 27/11/2026)
- Se bajan de Mixamo sobre **Remy** (esqueleto estándar `mixamorig:`), no sobre Obregón: el auto-rigger de Mixamo cruzaba izquierda y derecha (nuestros lados están espejados: `_R` de Obregón es el `Left` de Mixamo) y daba animaciones al revés.
- `Arte/blender/retarget_mixamo.py` pasa la animación al esqueleto de Obregón por dirección de hueso (sin dedos; los dedos los maneja `AnimPersonaje.Mano`) y funde los últimos cuadros hacia el primero para el bucle. Uso: `blender -b -P retarget_mixamo.py -- <anim.fbx> <Nombre> <salida.fbx>`.
- Los FBX de salida van a `Assets/Modelos/Animaciones/Obregon_<Nombre>.fbx`; `ExtraerAnimaciones` los convierte en `Assets/Resources/Animaciones/<Nombre>.anim` y `AnimPersonaje` los mezcla (Reposo, Caminar, Correr, Hablar).
- Esos FBX y clips de Mixamo **no se suben al repositorio** (es público y los términos de Adobe permiten usarlos en un juego, pero no redistribuirlos sueltos). Cada uno los regenera con el script a partir de las descargas de Mixamo (`D:\Unity\Descargas\mixamo`). Sin clips, el juego cae a la animación procedural.

## PENDIENTE: arreglar la costura del clip "Hablar"
Al terminar de hablar, Obregón "se recuesta en el aire como si se fuera a caer", se sacude y vuelve a empezar. Pasa igual aunque se fundieron los últimos 24 cuadros hacia el primero y se cambió la transición a Reposo. Sospechas a revisar:
1. El clip "Talking" de Mixamo termina inclinado hacia atrás; el fundido hacia el primer cuadro no alcanza o cruza por una pose rara (slerp de cadera y columna).
2. La posición de la cadera (curva de Hips) no cierra el ciclo o difiere de la del reposo, y al pasar a Reposo se nota el salto.
3. Mezcla de pesos en `AplicarClips` (suma distinta de 1 cuando entra o sale un clip).
Ideas: recortar el clip a un tramo estable en bucle (sin el final inclinado), quitar la rotación de la cadera en el eje de inclinación, o usar un clip de gestos más corto del Gestures Pack Basic. Probar en Blender mirando frame a frame antes de volver a Unity.
