import sys, os, bpy
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
from mathutils import Vector
conservar = {"GEO-body_male_realistic", "GEO-body_male_realistic.eye.L", "GEO-body_male_realistic.eye.R"}
for o in list(bpy.data.objects):
    if o.name not in conservar: bpy.data.objects.remove(o, do_unlink=True)
cuerpo = bpy.data.objects["GEO-body_male_realistic"]
mr = [m for m in cuerpo.modifiers if m.type == 'MULTIRES'][0]
print("niveles:", mr.total_levels, "base verts:", len(cuerpo.data.vertices))
def centro(o):
    pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
    return sum(pts, Vector()) / 8
# la cabeza: buscar los ojos
ojo = bpy.data.objects["GEO-body_male_realistic.eye.L"].matrix_world.translation
print("ojo", ojo)
escena()
bpy.context.scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.16, 0.16, 0.17, 1)
c = Vector((ojo.x - 0.03, ojo.y, ojo.z - 0.045))
luces(centro=tuple(c), escala=0.7)
for nivel in (0, 3):
    mr.levels = nivel; mr.render_levels = nivel
    render(f"D:/Unity/blender/carabase_n{nivel}.png", camara((c.x, c.y - 0.85, c.z), tuple(c), 85), 700, 700)
print("OK")
