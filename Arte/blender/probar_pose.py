"""Prueba de deformación: importa el FBX, levanta un brazo, dobla el codo e inclina la cabeza con los huesos, y renderiza. Uso: blender -b -P probar_pose.py"""
import sys, os, math, bpy
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
from mathutils import Vector, Euler

FBX = "D:/Claude Code/Rol Buenos Aires/Assets/Modelos/Personajes/Obregon.fbx"
limpiar()
bpy.ops.import_scene.fbx(filepath=FBX)
escena()
bpy.context.scene.render.film_transparent = False
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
print("HUESOS:", [b.name for b in arm.pose.bones][:6], "...")

def girar(nombre, x=0, y=0, z=0):
    b = arm.pose.bones[nombre]
    b.rotation_mode = 'XYZ'
    b.rotation_euler = Euler((math.radians(x), math.radians(y), math.radians(z)), 'XYZ')

def foto(nombre, pos, mira, focal=50, ancho=800, alto=1000):
    render(f"D:/Unity/blender/pose_{nombre}.png", camara(pos, mira, focal), ancho, alto, 10)

c = Vector((0, 0, 1.0))
luces(centro=tuple(c), escala=1.8)
# 1) reposo
foto("reposo", (0, -3.2, 0.95), tuple(c))
# 2) brazo derecho arriba (gesto) y codo doblado; cabeza inclinada
girar("UpperArm_R", x=-75, z=0)
girar("LowerArm_R", x=-60)
girar("Head", x=15, z=10)
bpy.context.view_layer.update()
foto("brazo_arriba", (0, -3.2, 0.95), tuple(c))
foto("busto_arriba", (-1.3, -1.9, 1.35), (0, 0, 1.25), 60, 900, 900)
print("OK")
