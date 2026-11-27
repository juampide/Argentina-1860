import sys, os, bpy
sys.path.insert(0, os.path.dirname(__file__))
import obregon_cara as cara, ropa
cuerpo, mr, oL, oR = cara.preparar()
ropa.adelgazar(cuerpo)
vs = [v.co for v in cuerpo.data.vertices if v.co.x > 0.18 and v.co.z < 1.0]
print("MED brazo derecho: x desde 0.18; secciones por x:")
x = 0.18
while x < 0.50:
    sec = [p for p in vs if x <= p.x < x + 0.02]
    if sec:
        zs = [p.z for p in sec]; ys = [p.y for p in sec]
        print(f"MED x[{x:.2f},{x+0.02:.2f}] z=[{min(zs):.2f},{max(zs):.2f}] alto={max(zs)-min(zs):.3f} prof={max(ys)-min(ys):.3f} n={len(sec)}")
    x += 0.02
# piernas: ancho en x
ps = [v.co for v in cuerpo.data.vertices if v.co.z < 0.5 and v.co.x > 0.0]
print("MED pierna/pie: x max", max(p.x for p in ps), "z min", min(p.z for p in ps))
