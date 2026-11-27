import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import lib, cabeza, pelo
from lib import *
limpiar(); escena()
piel = material("piel", (0.74, 0.50, 0.38), 0.55, 0.15)
suciedad(piel, 0.35, 14, (0.45, 0.30, 0.22))
cab = cabeza.crear_cabeza(1.68, piel)
mp = material("pelo", (0.07, 0.045, 0.03), 0.6)
suciedad(mp, 0.5, 30, (0.30, 0.27, 0.24))
pelo.pelo_corto(cab, mp)
pelo.bigote_fino(cab, mp, 1.68)
luces(centro=(0, 0, 1.68), escala=0.7)
for nombre, pos in (("frente", (0, -0.62, 1.69)), ("perfil", (-0.62, 0, 1.69)), ("tres", (-0.44, -0.44, 1.70))):
    render(f"D:/Unity/blender/cab_{nombre}.png", camara(pos, (0, 0, 1.68), 85), 700, 700)
print("OK")
