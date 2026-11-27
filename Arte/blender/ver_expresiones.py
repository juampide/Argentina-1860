"""Hoja de contacto con cada forma de la cara del FBX exportado. Uso: blender -b -P ver_expresiones.py"""
import sys, os, bpy
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np
from lib import *
from mathutils import Vector

FBX = "D:/Claude Code/Rol Buenos Aires/Assets/Modelos/Personajes/Obregon.fbx"
TEX = "D:/Claude Code/Rol Buenos Aires/Assets/Resources/Personajes"
limpiar()
bpy.ops.import_scene.fbx(filepath=FBX)
escena()
bpy.context.scene.render.film_transparent = False
cabeza = None
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    for m in o.data.materials:
        if m is None: continue
        g = m.name.replace("Obregon_", "").split(".")[0]
        ruta = f"{TEX}/Obregon_{g}.png"
        if g in ("piel", "pelo", "bigote") and os.path.exists(ruta):
            m.use_nodes = True
            nt = m.node_tree
            tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(ruta)
            nt.links.new(tex.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    if o.data.shape_keys and len(o.data.shape_keys.key_blocks) > 1:
        cabeza = o
    if "kepi" in o.name or "pelo" in o.name:
        o.hide_render = True            # sin kepí ni pelo para ver mejor la cara
print("CABEZA:", cabeza.name if cabeza else None, [k.name for k in cabeza.data.shape_keys.key_blocks] if cabeza else None)
c = Vector((0, -0.02, 1.60))
luces(centro=tuple(c), escala=0.6)
cam = camara((0, -0.75, 1.61), tuple(c), 85)
sc = bpy.context.scene
nombres = [k.name for k in cabeza.data.shape_keys.key_blocks][1:]
fotos = []
for nombre in ["(neutra)"] + nombres:
    for k in cabeza.data.shape_keys.key_blocks:
        k.value = 1.0 if k.name == nombre else 0.0
    ruta = f"D:/Unity/blender/_exp_{nombre.strip('()')}.png"
    render(ruta, cam, 420, 420, 12)
    fotos.append((nombre, ruta))
# hoja 4 x 2
imgs = []
for nombre, ruta in fotos:
    im = bpy.data.images.load(ruta)
    a = np.array(im.pixels[:], dtype=np.float32).reshape(im.size[1], im.size[0], 4)
    imgs.append(a)
vacia = np.zeros_like(imgs[0])
while len(imgs) % 4: imgs.append(vacia)
filas = [np.concatenate(imgs[i:i + 4], axis=1) for i in range(0, len(imgs), 4)]
hoja = np.concatenate(filas[::-1], axis=0)       # las imágenes de Blender van de abajo hacia arriba
out = bpy.data.images.new("hoja", hoja.shape[1], hoja.shape[0])
out.pixels = hoja.flatten().tolist()
out.filepath_raw = "D:/Unity/blender/obregon_expresiones.png"
out.file_format = 'PNG'
out.save()
print("ORDEN:", [n for n, _ in fotos])
print("OK")
