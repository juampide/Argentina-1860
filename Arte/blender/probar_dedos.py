"""Prueba de dedos con el eje de flexión real: cada falange gira alrededor de (dirección del dedo x hacia donde mira la palma).
Es el mismo cálculo que hace el animador de Unity. Uso: blender -b -P probar_dedos.py"""
import sys, os, math, bpy
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
from mathutils import Vector, Quaternion
limpiar()
bpy.ops.import_scene.fbx(filepath="D:/Claude Code/Rol Buenos Aires/Assets/Modelos/Personajes/Obregon.fbx")
escena(); bpy.context.scene.render.film_transparent = False
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
M = arm.matrix_world.to_3x3()

def flexionar(nombre, grados):
    pb = arm.pose.bones[nombre]
    b = pb.bone
    d = (M @ (b.tail_local - b.head_local)).normalized()
    x = (M @ b.head_local).x
    palma = Vector((-1, 0, 0)) if x > 0 else Vector((1, 0, 0))
    eje_mundo = d.cross(palma).normalized()
    eje_arm = (M.inverted() @ eje_mundo).normalized()
    delta = Quaternion(eje_arm, math.radians(grados))
    rest = b.matrix_local.to_quaternion()
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = rest.inverted() @ delta @ rest

c = Vector((0.31, -0.09, 0.80)); luces(centro=tuple(c), escala=0.45)
def foto(n, pos): render(f"D:/Unity/blender/dedos_{n}.png", camara(pos, tuple(c), 70), 700, 700, 10)
# 1) puño
for dedo, mx in (("Index", (80, 95, 70)), ("Middle", (80, 95, 70)), ("Ring", (80, 95, 70)), ("Pinky", (80, 95, 70)), ("Thumb", (25, 35, 30))):
    for k in (1, 2, 3):
        flexionar(f"{dedo}_{k}_R", mx[k - 1])
bpy.context.view_layer.update()
foto("puno", (0.95, -0.55, 0.85))
foto("puno_lado", (1.05, 0.05, 0.85))
# 2) solo el índice extendido (señalar)
for dedo in ("Middle", "Ring", "Pinky"):
    pass
for k in (1, 2, 3):
    flexionar(f"Index_{k}_R", 0)
bpy.context.view_layer.update()
foto("senalar", (0.95, -0.55, 0.85))
print("OK")
