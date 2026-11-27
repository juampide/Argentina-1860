"""Uniforme de oficial de infantería de frontera (guía visual: lámina de uniforme argentino; adaptado a 1867 con kepí).
Saco de una hilera de botones con cuello y puños oscuros, correaje de cuero con cartucheras, bombacha metida en botas altas con correas.
Todo en coordenadas locales del cuerpo base; al final se desplaza a donde está el cuerpo."""
import bpy, bmesh, math
from mathutils import Vector, Quaternion, Euler
from mathutils.bvhtree import BVHTree
from lib import *
import ropa


def _centro_muneca(cuerpo, lado):
    vs = [v.co for v in cuerpo.data.vertices if lado * v.co.x > 0.335 and lado * v.co.x < 0.358 and 0.78 < v.co.z < 1.0]
    if not vs:
        return Vector((lado * 0.347, -0.01, 0.90))
    return sum(vs, Vector()) / len(vs)


def _cinta(nombre, puntos_xz, arbol, ancho, grosor, mat, despeje=0.006):
    """Cinta plana que sigue la superficie del saco (de frente): para correajes y tiras."""
    bm = bmesh.new()
    izq, der = [], []
    pts = []
    for (x, z) in puntos_xz:
        hit = arbol.ray_cast(Vector((x, -0.7, z)), Vector((0, 1, 0)))
        if hit[0] is None: continue
        pts.append((hit[0] + hit[1] * despeje, hit[1]))
    for k, (p, n) in enumerate(pts):
        t = (pts[min(k + 1, len(pts) - 1)][0] - pts[max(k - 1, 0)][0]).normalized()
        lat = t.cross(n).normalized()
        izq.append(bm.verts.new(p + lat * ancho / 2))
        der.append(bm.verts.new(p - lat * ancho / 2))
    for k in range(len(pts) - 1):
        bm.faces.new((izq[k], izq[k + 1], der[k + 1], der[k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = a_objeto(nombre, bm, mat, subdiv=0, suave=True)
    s = ob.modifiers.new("g", 'SOLIDIFY'); s.thickness = grosor; s.offset = 1.0
    return ob


def vestir(cuerpo, loc, centro_cab):
    """Crea el uniforme completo. Devuelve la lista de objetos nuevos."""
    previos = set(bpy.data.objects)
    azul = ropa.tela("lana_azul", (0.060, 0.105, 0.205), rugosidad=0.9, mugre=0.55, escala_trama=380)
    negro = ropa.tela("pano_negro", (0.020, 0.021, 0.028), rugosidad=0.9, mugre=0.35, fuerza_trama=0.5)
    bomb = ropa.tela("bombacha", (0.075, 0.125, 0.225), rugosidad=0.9, mugre=0.6, desgaste_abajo=0.5, z_base=0.42, z_alto=0.85)
    marron = ropa.cuero("cuero_marron", color=(0.20, 0.100, 0.045), rugosidad=0.5)
    botas_m = ropa.cuero("botas_marron", color=(0.105, 0.052, 0.024), rugosidad=0.5)
    oliva = ropa.tela("oliva", (0.150, 0.150, 0.075), rugosidad=0.9, mugre=0.5, escala_trama=300)
    oro = ropa.metal_dorado()

    # ---------- saco ----------
    muneca = {1: _centro_muneca(cuerpo, 1), -1: _centro_muneca(cuerpo, -1)}
    hombro = {1: Vector((0.19, 0.0, 1.38)), -1: Vector((-0.19, 0.0, 1.38))}
    eje = {l_: (muneca[l_] - hombro[l_]).normalized() for l_ in (1, -1)}

    def d_muneca(c, lado):
        return (c - muneca[lado]).dot(eje[lado])

    def en_saco(c):
        if abs(c.x) < 0.19 and 0.90 <= c.z <= 1.45: return True
        if abs(c.x) >= 0.19 and c.z <= 1.45 and (abs(c.x) >= 0.24 or c.z >= 0.90) and c.z > 0.70:
            lado = 1 if c.x > 0 else -1
            return d_muneca(c, lado) < 0
        return False

    def fij_saco(v):
        if abs(v.co.x) < 0.20 and 0.86 < v.co.z < 0.95: v.co.z = 0.90
        elif v.co.z > 1.42: v.co.z = 1.455
        if abs(v.co.x) >= 0.20 and v.co.z < 1.3:
            lado = 1 if v.co.x > 0 else -1
            d = d_muneca(v.co, lado)
            if d > -0.03: v.co = v.co - eje[lado] * d      # puño cortado perpendicular al antebrazo

    bm = ropa.malla_zona(cuerpo, en_saco)
    saco, arbol_saco = ropa.prenda("saco", bm, azul, inflar=0.034, pliegues=0.0075, grosor=0.0055, ajustar_borde=fij_saco, sem=2.0, suavizado=24)

    # puños negros (banda de 6 cm sobre la misma manga)
    def en_puno(c):
        if abs(c.x) < 0.24: return False
        lado = 1 if c.x > 0 else -1
        d = d_muneca(c, lado)
        return -0.065 < d < 0 and c.z > 0.70
    bm = ropa.malla_zona(cuerpo, en_puno)
    ropa.prenda("punos", bm, negro, inflar=0.0385, pliegues=0.002, grosor=0.0055, ajustar_borde=fij_saco, sem=3.0, suavizado=6, subdiv=1)
    for lado in (-1, 1):
        for k in range(3):
            pc = muneca[lado] - eje[lado] * (0.015 + 0.017 * k)
            esfera("boton_puno", pc + Vector((lado * 0.034, -0.045, 0)), (0.0055, 0.0030, 0.0055), oro, seg=12, anillos=6)

    # cuello alto oscuro con insignia dorada
    cuello = loft("cuello_uniforme", [((0, 0.002, 1.450), 0.064, 0.062), ((0, 0.002, 1.478), 0.060, 0.058), ((0, 0.003, 1.500), 0.058, 0.056)],
                  negro, lados=22, subdiv=1, cerrar_arriba=False, cerrar_abajo=False)
    sol = cuello.modifiers.new("g", 'SOLIDIFY'); sol.thickness = 0.005; sol.offset = 0.0
    for lado in (-1, 1):
        esfera("insignia", (lado * 0.040, -0.052, 1.468), (0.0095, 0.0035, 0.0095), oro, seg=14, anillos=8, rot=(0, 0, 0))

    # una hilera de botones al frente
    for k in range(7):
        z = 1.385 - k * 0.0625
        hit = arbol_saco.ray_cast(Vector((0.0, -0.7, z)), Vector((0, 1, 0)))
        if hit[0] is not None:
            esfera("boton", hit[0] + hit[1] * 0.004, (0.0085, 0.0045, 0.0085), oro, seg=14, anillos=8)

    # hombreras
    for lado in (-1, 1):
        hit = arbol_saco.ray_cast(Vector((lado * 0.165, 0.0, 1.8)), Vector((0, 0, -1)))
        if hit[0] is not None:
            p = hit[0] + hit[1] * 0.006
            bpy.ops.mesh.primitive_cube_add(size=1)
            h = bpy.context.active_object
            h.name = "hombrera"; h.scale = (0.055, 0.080, 0.009); h.location = p; h.rotation_euler = (0, lado * 0.45, 0)
            h.data.materials.append(oro)
            bev = h.modifiers.new("b", 'BEVEL'); bev.width = 0.004; bev.segments = 2

    # ---------- cinturón, correaje y cartucheras ----------
    zs = 1.0
    xs = [v.co for v in saco.data.vertices if abs(v.co.z - zs) < 0.012 and abs(v.co.x) < 0.24]
    if xs:
        cx = (max(p.x for p in xs) + min(p.x for p in xs)) / 2; cy = (max(p.y for p in xs) + min(p.y for p in xs)) / 2
        rx = (max(p.x for p in xs) - min(p.x for p in xs)) / 2 + 0.010; ry = (max(p.y for p in xs) - min(p.y for p in xs)) / 2 + 0.010
        cinto = loft("cinturon", [((cx, cy, zs - 0.023), rx, ry), ((cx, cy, zs + 0.023), rx, ry)], marron, lados=30, subdiv=1, cerrar_arriba=False, cerrar_abajo=False)
        s2 = cinto.modifiers.new("g", 'SOLIDIFY'); s2.thickness = 0.007; s2.offset = 0.0
        bpy.ops.mesh.primitive_cube_add(size=1); b = bpy.context.active_object
        b.name = "hebilla"; b.scale = (0.038, 0.007, 0.032); b.location = (cx, cy - ry - 0.003, zs); b.data.materials.append(oro)
        bv = b.modifiers.new("b", 'BEVEL'); bv.width = 0.004; bv.segments = 2
    # correaje: dos tiras de los hombros al cinturón
    for lado in (-1, 1):
        pts = [(lado * (0.115 - 0.060 * (k / 8)), 1.410 - 0.405 * (k / 8)) for k in range(9)]
        _cinta("correaje", pts, arbol_saco, 0.030, 0.0035, marron)
    # cartucheras (tres) en el frente del cinturón
    for i, x in enumerate((-0.078, 0.0, 0.078)):
        hit = arbol_saco.ray_cast(Vector((x, -0.7, 1.062)), Vector((0, 1, 0)))
        if hit[0] is None: continue
        bpy.ops.mesh.primitive_cube_add(size=1)
        c = bpy.context.active_object
        c.name = "cartuchera"; c.scale = (0.060, 0.032, 0.066)
        c.location = hit[0] + hit[1] * 0.020
        c.rotation_mode = 'QUATERNION'; c.rotation_quaternion = (-hit[1]).to_track_quat('Y', 'Z')
        c.data.materials.append(oliva)
        bev = c.modifiers.new("b", 'BEVEL'); bev.width = 0.006; bev.segments = 3
        # tapa con botón
        esfera("boton_cartuchera", hit[0] + hit[1] * 0.0385 + Vector((0, 0, 0.012)), (0.0065, 0.0035, 0.0065), oro, seg=12, anillos=6)

    # ---------- bombacha y botas ----------
    def perfil_bomba(p):
        z = p.z
        return 0.040 * math.exp(-((z - 0.72) / 0.17) ** 2) + 0.030 * math.exp(-((z - 0.47) / 0.04) ** 2)   # abultada en el muslo y bolsa sobre la bota

    def fij_pant(v):
        if v.co.z < 0.51: v.co.z = 0.465
        elif v.co.z > 0.97: v.co.z = 1.0

    bm = ropa.malla_zona(cuerpo, lambda c: 0.46 <= c.z <= 1.02 and abs(c.x) < 0.25)
    ropa.prenda("bombacha", bm, bomb, inflar=0.014, pliegues=0.007, grosor=0.004, ajustar_borde=fij_pant, escala_pliegue=26.0, sem=5.0, suavizado=10, perfil=perfil_bomba)

    def fij_bota(v):
        if v.co.z > 0.50: v.co.z = 0.52

    bm = ropa.malla_zona(cuerpo, lambda c: c.z <= 0.52)
    botas, arbol_botas = ropa.prenda("botas", bm, botas_m, inflar=0.015, pliegues=0.003, grosor=0.0045, ajustar_borde=fij_bota, escala_pliegue=30.0, sem=7.0, suavizado=16)
    # correas con hebilla en las botas (en la cara externa de cada pierna)
    for lado in (-1, 1):
        for z in (0.40, 0.31):
            ring = [(lado * 0.0, 0.0)]
            ob = None
            hit = arbol_botas.ray_cast(Vector((lado * 0.45, 0.0, z)), Vector((-lado, 0, 0)))
            if hit[0] is None: continue
            esfera("hebilla_bota", hit[0] + hit[1] * 0.004, (0.0035, 0.010, 0.010), oro, seg=10, anillos=6, rot=(0, 0, 0))

    # ---------- kepí ----------
    azul2 = azul
    z0 = loc.z + 0.058
    cyh = centro_cab.y
    kepi = loft("kepi", [((0, cyh, z0), 0.092, 0.101), ((0, cyh, z0 + 0.020), 0.092, 0.101), ((0, cyh, z0 + 0.075), 0.090, 0.099), ((0, cyh, z0 + 0.100), 0.094, 0.103)],
                azul2, lados=30, subdiv=1, cerrar_arriba=True, cerrar_abajo=False)
    sk = kepi.modifiers.new("g", 'SOLIDIFY'); sk.thickness = 0.004; sk.offset = 0.0
    loft("kepi_cinta", [((0, cyh, z0 + 0.002), 0.0990, 0.1080), ((0, cyh, z0 + 0.024), 0.0990, 0.1080)], oro, lados=30, subdiv=0, cerrar_arriba=False, cerrar_abajo=False)
    esfera("kepi_visera", (0, cyh - 0.105, z0 + 0.004), (0.082, 0.052, 0.004), botas_m, seg=24, anillos=8, rot=(0.20, 0, 0))
    esfera("kepi_escudo", (0, cyh - 0.099, z0 + 0.052), (0.014, 0.004, 0.014), oro, seg=14, anillos=8)

    nuevos = set(bpy.data.objects) - previos
    return list(nuevos)
