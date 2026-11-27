"""Comprueba el FBX exportado: lo importa en una escena limpia, le pone las texturas horneadas y saca renders de la cara y del cuerpo.
Uso: blender -b -P ver_fbx.py"""
import sys, os, bpy, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
from mathutils import Vector

FBX = "D:/Claude Code/Rol Buenos Aires/Assets/Modelos/Personajes/Obregon.fbx"
TEX = "D:/Claude Code/Rol Buenos Aires/Assets/Resources/Personajes"
limpiar()
bpy.ops.import_scene.fbx(filepath=FBX)
escena()
bpy.context.scene.render.film_transparent = False
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    for i, m in enumerate(o.data.materials):
        if m is None:
            continue
        grupo = m.name.replace("Obregon_", "").split(".")[0]
        ruta = f"{TEX}/Obregon_{grupo}.png"
        if grupo in ("piel", "pelo", "bigote") and os.path.exists(ruta):
            m.use_nodes = True
            nt = m.node_tree
            p = nt.nodes["Principled BSDF"]
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = bpy.data.images.load(ruta)
            nt.links.new(tex.outputs["Color"], p.inputs["Base Color"])
            p.inputs["Roughness"].default_value = 0.55
cabeza = None
zs = [v.co.z for o in bpy.data.objects if o.type == 'MESH' for v in o.data.vertices]
print("ALTO del modelo:", max(zs) - min(zs))
c = Vector((0, -0.02, 1.62))
luces(centro=tuple(c), escala=0.7)
for nombre, pos in (("fbx_frente", (0, -0.95, 1.63)), ("fbx_tres", (-0.62, -0.62, 1.64)), ("fbx_perfil", (-0.95, 0, 1.63))):
    render(f"D:/Unity/blender/obregon_{nombre}.png", camara(pos, tuple(c), 85), 800, 800)
print("OK")
