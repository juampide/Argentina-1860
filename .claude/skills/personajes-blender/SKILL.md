---
name: personajes-blender
description: >-
  Cómo modelamos los personajes del juego 1867 en Blender (por scripts de Python) y los llevamos a Unity: base humana libre,
  dirección de arte (estilizado pintado, estilo Disco Elysium, pelo como masas esculpidas), convenciones, rutas y trampas ya encontradas.
  Usar al modelar, vestir, texturizar o exportar personajes, o al tocar Arte/blender/.
---

# Personajes en Blender (piloto: Teniente Obregón)

## Dirección de arte (decidida con Juan)
- Norte: **Disco Elysium** (pintado, sucio, desaturado). Dramático, nada de juguete ni de maqueta.
- Rostros **estilizados con planos marcados** (frente, pómulos, mandíbula bien definidos), pocos polígonos y pintura a mano. No fotorrealismo.
- **El pelo es una masa esculpida de mechones grandes y sólidos**, nunca hebras ni "plumas" de elipsoides. Lo mismo la barba y el bigote: formas sólidas, simples y con volumen. (Un intento con ~1900 elipsoides pequeños quedó horrible.)
- Juan es el director de arte: aprueba con renders antes de llevar nada a Unity. Mostrar siempre frente y tres cuartos.
- Referencias que Juan dio: cabezas estilizadas esculpidas con pelo en masas; cabeza low poly pintada a mano con planos duros y ceja gruesa; esculturas de rostros con arrugas y poros finos.

## Base y herramientas
- **Blender 5.3** en `C:\Program Files\Blender Foundation\Blender 5.3\blender.exe` (winget `BlenderFoundation.Blender`). Se usa sin pantalla: `blender.exe -b archivo.blend -P script.py`.
- **Base humana libre**: *Human Base Meshes* de Blender Studio (CC0, confirmar el texto oficial antes de publicar). Archivo: `D:\Unity\Descargas\human-base-meshes\bundle.blend` (49.423.697 bytes). Objetos útiles: `GEO-body_male_realistic` (10.582 vértices base, multires 3 niveles), `GEO-head_animation_realistic`, `Hand  - Realistic` (dos espacios), ojos, mandíbula, cráneo. Origen: https://projects.blender.org/studio/asset-bundles
- Scripts del proyecto en `Arte/blender/`: `lib.py` (materiales, loft, luces, cámara, render), `obregon_cara.py` (envejece la cara de la base), `pelo2.py`, `cabeza.py`/`pelo.py` (cabeza hecha por código, descartada).
- Renders de revisión en `D:\Unity\blender\` (fuera del repo). Capturas del juego en `D:\Unity\capturas\`.

## Convenciones y trampas
- Personaje mira a **-Y**, Z arriba, metros. Los objetos de la base están repartidos por el archivo (usar el centro de la caja, no el origen).
- Motor de render para revisión: `BLENDER_EEVEE` (funciona sin GPU visible, unos 8 s por imagen). Poner `film_transparent = False`: el archivo base trae fondo blanco/transparente.
- Blender 5.x: el Principled usa `Subsurface Weight`/`Subsurface Radius`/`Subsurface Scale`; los mezcladores son `ShaderNodeMix` (índices 6 y 7 son los colores).
- El cuerpo base tiene Multires de 3 niveles: para revisar usar el nivel 2 o 3; el nivel 0 es la malla de juego.
- Editar la malla de base (nivel 0) arrastra los detalles de escultura de los niveles altos.
- Los ojos de la base son esferas sin iris: hay que agregar iris y pupila.
- Deformar la cara con gaussianas sobre la malla base funciona para envejecer (mejillas, cuencas, ceja, mandíbula); los párpados de la base están muy abiertos y dan cara de asombro: hay que bajar el superior y subir el inferior.
- Exportar a Unity: FBX; los personajes irían a `Assets/Modelos/Personajes/`. Esqueleto y gestos: Mixamo o Rigify (a decidir). Federico se ocupa del lado Unity.

## Estado
Piloto Obregón en curso: cara convertida (flaco, cansado), faltan pelo estilizado en masas, bigote sólido, cuerpo vestido con uniforme gastado, textura pintada y esqueleto. Plan en `Docs/mundo/personajes-blender.md`.
