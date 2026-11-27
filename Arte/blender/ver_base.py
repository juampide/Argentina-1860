import sys, os, bpy
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
from mathutils import Vector
mantener = {"GEO-body_male_realistic", "GEO-body_male_realistic.eye.L", "GEO-body_male_realistic.eye.R",
            "GEO-head_animation_realistic", "GEO-head_animation_realistic.iris.L", "GEO-head_animation_realistic.iris.R",
            "GEO-head_animation_realistic.sclera.L", "GEO-head_animation_realistic.sclera.R"}
for o in list(bpy.data.objects):
    if o.name not in mantener:
        bpy.data.objects.remove(o, do_unlink=True)
for o in bpy.data.objects:
    for m in o.modifiers:
        if m.type == 'MULTIRES':
            m.levels = m.total_levels; m.render_levels = m.total_levels; m.sculpt_levels = m.total_levels
    print(o.name, tuple(round(x, 3) for x in o.location), tuple(round(x, 3) for x in o.dimensions))
cuerpo = bpy.data.objects['GEO-body_male_realistic']
cabeza = bpy.data.objects['GEO-head_animation_realistic']
def centro(o):
    from mathutils import Vector
    pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
    return sum(pts, Vector()) / 8
escena()
cb = centro(cuerpo); ch = centro(cabeza)
print('centro cuerpo', cb, 'centro cabeza', ch)
luces(centro=tuple(cb), escala=1.0)
render('D:/Unity/blender/base_cuerpo.png', camara((cb.x, cb.y - 4.2, cb.z), tuple(cb), 55), 700, 900)
for o in bpy.data.objects:
    if 'body_male_realistic' in o.name: o.hide_render = True
for l in [o for o in bpy.data.objects if o.type == 'LIGHT']: bpy.data.objects.remove(l, do_unlink=True)
luces(centro=tuple(ch), escala=0.7)
render('D:/Unity/blender/base_cabeza.png', camara((ch.x, ch.y - 0.9, ch.z), tuple(ch), 85), 700, 700)
render('D:/Unity/blender/base_cabeza_tres.png', camara((ch.x - 0.65, ch.y - 0.65, ch.z + 0.05), tuple(ch), 85), 700, 700)
print('OK')
