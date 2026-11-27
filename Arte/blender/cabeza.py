import bpy, bmesh, math, random
from mathutils import Vector
from lib import *

def crear_cabeza(cz, piel, edad=0.6, flaco=0.6, seed=3):
    """Cabeza esculpida con desplazamientos sobre una esfera (cuencas, ceja, nariz, pómulos, labios, mentón).
    cz: altura del centro de la cabeza. edad 0..1 (arrugas, ojeras), flaco 0..1 (mejillas hundidas, pómulos)."""
    rnd = random.Random(seed)
    RX, RY, RZ = 0.078, 0.098, 0.118
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=110, v_segments=72, radius=1.0)
    def pot(a, e): return math.copysign(abs(a) ** e, a)
    for v in bm.verts:
        ux, uy, uz = v.co.x, v.co.y, v.co.z
        ux, uy = pot(ux, 0.80), pot(uy, 0.84)
        uz = pot(uz, 0.72) if uz < 0 else uz
        x, y, z = ux * RX, uy * RY, uz * RZ
        nx, nz = x / RX, z / RZ
        frente = sstep(0.05, 0.55, -y / RY)            # 1 en la cara, 0 en la nuca
        # mandíbula que se afina hacia el mentón
        ancho = 1.0 - (0.10 + 0.10 * flaco) * sstep(-0.1, -1.0, nz)
        x *= ancho
        # frente algo más plana y cráneo más ancho arriba/atrás
        y *= 1.0 + 0.06 * sstep(0.0, 0.8, nz) * (1 - frente)
        fy = 0.0   # desplazamiento hacia adelante (-y)
        fx = 0.0
        def B(cx, cz2, sx, sz, h, simetrico=True):
            nonlocal fy
            h = h * 1.6
            if simetrico:
                fy += bump(abs(nx), nz, cx, cz2, sx, sz, h)
            else:
                fy += bump(nx, nz, cx, cz2, sx, sz, h)
        # frente y ceja
        B(0.0, 0.62, 0.8, 0.22, 0.004)
        B(0.44, 0.30, 0.38, 0.10, 0.011)           # arco superciliar
        B(0.0, 0.30, 0.20, 0.10, 0.007)            # entrecejo
        # cuencas de los ojos
        B(0.42, 0.13, 0.22, 0.12, -0.013)
        # párpado superior y bolsa de abajo
        B(0.42, 0.20, 0.20, 0.05, 0.003)
        B(0.42, 0.02, 0.22, 0.06, 0.004 * (0.5 + edad))
        # nariz: puente, punta y alas
        B(0.0, 0.05, 0.11, 0.33, 0.019)
        B(0.0, -0.22, 0.15, 0.11, 0.021)
        B(0.17, -0.26, 0.09, 0.07, 0.008)
        B(0.0, -0.31, 0.10, 0.05, -0.006)          # base de la nariz
        # pómulos y mejillas
        B(0.62, -0.06, 0.20, 0.15, 0.007 + 0.006 * flaco)
        B(0.52, -0.42, 0.20, 0.17, -0.008 - 0.012 * flaco)   # mejilla hundida
        # surco nasolabial
        B(0.30, -0.36, 0.06, 0.18, -0.004 - 0.003 * edad)
        # boca
        B(0.0, -0.50, 0.30, 0.025, -0.006)         # comisura de los labios
        B(0.0, -0.44, 0.22, 0.05, 0.006)           # labio superior
        B(0.0, -0.57, 0.22, 0.05, 0.007)           # labio inferior
        B(0.0, -0.40, 0.04, 0.03, 0.003)           # surco del labio
        # mentón
        B(0.0, -0.82, 0.30, 0.13, 0.011)
        B(0.0, -0.66, 0.30, 0.05, -0.004)          # hueco bajo el labio
        # pómulo hacia afuera
        fx = bump(abs(nx), nz, 0.72, -0.08, 0.2, 0.14, 0.004)
        y -= fy * frente
        x += (fx * (1 if x >= 0 else -1)) * frente
        v.co = Vector((x, y, z))
    bm.normal_update()
    cab = a_objeto("cabeza", bm, piel, subdiv=0)
    cab.location = (0, 0, cz)
    # cuello y trapecios
    cuello = loft("cuello", [((0, 0.006, cz - 0.17), 0.043, 0.046), ((0, 0.004, cz - 0.10), 0.040, 0.043), ((0, 0.0, cz - 0.03), 0.040, 0.045)], piel, lados=16, subdiv=1, cerrar_arriba=False, cerrar_abajo=False)
    # orejas
    for s in (-1, 1):
        o = esfera("oreja", (s * 0.076, 0.012, cz - 0.01), (0.007, 0.022, 0.033), piel, seg=16, anillos=10, subdiv=1, rot=(0, 0, s * 0.25))
    # ojos: globo, iris y párpados
    blanco = material("ojo_blanco", (0.86, 0.82, 0.74), 0.3)
    iris = material("iris", (0.10, 0.065, 0.04), 0.25)
    parpado = material("parpado", tuple(min(1, c * 0.82) for c in piel.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value[:3]), 0.6, 0.05)
    for s in (-1, 1):
        ex, ey, ez = s * 0.034, -0.071, cz + 0.014
        esfera("globo", (ex, ey, ez), (0.0135, 0.0135, 0.0135), blanco, seg=20, anillos=12)
        esfera("iris", (ex, ey - 0.0090, ez), (0.0083, 0.005, 0.0083), iris, seg=16, anillos=8)
        # párpados: dos cáscaras apenas más grandes que el globo (arriba y abajo) que dejan una ranura en forma de almendra
        for k in (0, 1):
            bmp = bmesh.new()
            R = 0.0150
            bmesh.ops.create_uvsphere(bmp, u_segments=24, v_segments=14, radius=R)
            quitar = []
            for f in bmp.faces:
                c = f.calc_center_median()
                frente = c.y < 0.25 * R
                if k == 0: ok = frente and c.z > 0.0042
                else: ok = frente and c.z < -0.0078
                if not ok: quitar.append(f)
            bmesh.ops.delete(bmp, geom=quitar, context='FACES')
            ob = a_objeto('parpado', bmp, parpado, subdiv=1)
            ob.location = (ex, ey, ez)
    return cab
