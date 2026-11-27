"""Teniente Obregón completo: cuerpo flaco, cabeza (cara, pelo, bigote) y uniforme de frontera gastado.
Uso: blender -b human-base-meshes/bundle.blend -P obregon_cuerpo.py"""
import sys, os, math, bpy
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
from mathutils import Vector
import obregon_cara as cara
import piel as piel_mod
import pelo3, bigote as bigote_mod
import ropa


def construir():
    cuerpo, mr, oL, oR = cara.preparar()
    ropa.adelgazar(cuerpo)
    cara.envejecer(cuerpo, oL, oR, flaco=2.2, cansancio=2.4)
    ojo0 = (oL.matrix_world.translation + oR.matrix_world.translation) / 2
    loc0 = cuerpo.matrix_world.inverted() @ ojo0
    piel = piel_mod.piel_obregon(loc0)
    cuerpo.data.materials.clear(); cuerpo.data.materials.append(piel)
    for o in (oL, oR): o.location.y += 0.0035
    bpy.context.view_layer.update()
    cara.iris(oL, oR)
    mr.levels = 2; mr.render_levels = 2
    loc = cuerpo.matrix_world.inverted() @ ((oL.matrix_world.translation + oR.matrix_world.translation) / 2)
    mp = piel_mod.pelo_textura()
    centro_cab = Vector((loc.x, loc.y + 0.085, loc.z + 0.045))
    pelo3.pelo_masas(cuerpo, mp, centro_cab, grosor=0.0105, copete=0.024)
    bigote_mod.bigote_masa(cuerpo, bigote_mod.material_bigote(), loc.z)

    # ---- uniforme (ver uniforme.py) ----
    mr.levels = 1
    bpy.context.view_layer.update()
    previos = set(bpy.data.objects)
    import uniforme2 as uniforme
    uniforme.vestir(cuerpo, loc, centro_cab)

    # lo armado en coordenadas del cuerpo se lleva a donde está el cuerpo en el mundo
    for o in set(bpy.data.objects) - previos:
        o.location += cuerpo.location
    bpy.context.view_layer.update()

    if "--exportar" in sys.argv:
        import exportar_unity
        exportar_unity.exportar(cuerpo, "D:/Claude Code/Rol Buenos Aires/Assets/Modelos/Personajes/Obregon.fbx")

    # ---- render ----
    mr.levels = 2
    sc = escena(); sc.render.film_transparent = False
    cb = cuerpo.matrix_world @ Vector((loc.x, loc.y + 0.02, 0.86))
    luces(centro=tuple(cb), escala=2.2)
    alto = Vector((cb.x, cb.y, 1.28))
    vistas = (("cuerpo_frente", (cb.x, cb.y - 3.6, 0.9), cb, 55, 700, 1000), ("cuerpo_tres", (cb.x - 2.6, cb.y - 2.6, 0.95), cb, 55, 700, 1000),
              ("cuerpo_perfil", (cb.x - 3.7, cb.y, 0.9), cb, 55, 700, 1000), ("busto_tres", (cb.x - 1.2, cb.y - 1.5, 1.42), alto, 60, 900, 900))
    for nombre, pos, mira, focal, ancho, alto_px in vistas:
        render(f"D:/Unity/blender/obregon_{nombre}.png", camara(pos, tuple(mira), focal), ancho, alto_px)
    print("OK")


if __name__ == "__main__":
    construir()
