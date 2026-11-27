"""Uniforme de oficial de infantería con ropa SUELTA y con caída (v2). A diferencia de uniforme.py, la ropa no copia la malla del cuerpo:
son volúmenes propios (tubos con pliegues) para que tengan vuelo, no marquen músculos y caigan como tela.
Guías visuales de Juan: lámina de uniforme argentino y soldado de la guerra civil norteamericana (saco corto, pantalón recto que cae sobre el zapato).
Coordenadas locales del cuerpo base (Z arriba, frente -Y)."""
import bpy, bmesh, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from lib import *
import ropa


def _arbol(ob):
    dg = bpy.context.evaluated_depsgraph_get()
    return BVHTree.FromObject(ob, dg)


def _ropa_tubo(nombre, anillos, mat, lados=22, grosor=0.006, subdiv=1, cerrar_arriba=False, cerrar_abajo=False):
    ob = loft(nombre, anillos, mat, lados=lados, subdiv=subdiv, cerrar_arriba=cerrar_arriba, cerrar_abajo=cerrar_abajo)
    s = ob.modifiers.new("g", 'SOLIDIFY')
    s.thickness = grosor
    s.offset = 0.0
    return ob


def _cinta(nombre, puntos_xz, arbol, ancho, grosor, mat, despeje=0.006):
    bm = bmesh.new()
    izq, der, pts = [], [], []
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
    previos = set(bpy.data.objects)
    azul = ropa.tela("lana_azul", (0.075, 0.125, 0.235), rugosidad=0.92, mugre=0.55, escala_trama=380)
    azul_p = ropa.tela("pantalon_azul", (0.100, 0.155, 0.280), rugosidad=0.92, mugre=0.6, desgaste_abajo=0.55, z_base=0.05, z_alto=0.75)
    negro = ropa.tela("pano_negro", (0.020, 0.021, 0.028), rugosidad=0.9, mugre=0.35, fuerza_trama=0.5)
    rojo = ropa.tela("vivo_rojo", (0.30, 0.040, 0.035), rugosidad=0.9, mugre=0.35)
    marron = ropa.cuero("cuero_marron", color=(0.20, 0.100, 0.045), rugosidad=0.5)
    zapato_m = ropa.cuero("zapatos", color=(0.020, 0.017, 0.015), rugosidad=0.5)
    oliva = ropa.tela("oliva", (0.150, 0.150, 0.075), rugosidad=0.9, mugre=0.5, escala_trama=300)
    oro = ropa.metal_dorado()

    vs = [v.co.copy() for v in cuerpo.data.vertices]

    def medir(z, lado, xmin, xmax, banda=0.022, margen=0.016):
        """Centro y radios (x, y) que ocupa el cuerpo a la altura z, en la franja xmin..xmax del lado dado."""
        sel = [p for p in vs if abs(p.z - z) < banda and xmin <= lado * p.x <= xmax] if lado else [p for p in vs if abs(p.z - z) < banda and xmin <= abs(p.x) <= xmax]
        if not sel:
            return None
        x0 = min(p.x for p in sel); x1 = max(p.x for p in sel); y0 = min(p.y for p in sel); y1 = max(p.y for p in sel)
        return ((x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2 + margen, (y1 - y0) / 2 + margen)

    # ---------- pantalón: dos tubos rectos y sueltos que caen sobre el zapato, con quiebres en rodilla y tobillo ----------
    def anillos_pierna(s):
        zs = [0.99, 0.90, 0.78, 0.66, 0.54, 0.46, 0.36, 0.26, 0.17, 0.105]
        out = []
        tobillo = medir(0.30, s, 0.0, 0.215, margen=0.015)
        for z in zs:
            m = medir(z, s, 0.0, 0.215, margen=0.018 if z > 0.45 else 0.015)
            if z < 0.30 and tobillo is not None:
                m = (tobillo[0], tobillo[1], tobillo[2] * 1.02, tobillo[3] * 1.02)      # bajo el tobillo la pierna sigue recta, centrada sobre el tobillo
            if m is None: continue
            cx, cy, rx, ry = m
            cierre = 0.07 * math.exp(-((z - 0.46) / 0.07) ** 2) + 0.14 * math.exp(-((z - 0.13) / 0.05) ** 2) + 0.05 * math.exp(-((z - 0.80) / 0.08) ** 2)   # quiebres de tela
            suelto = 1.0 + (0.06 if z < 0.30 else 0.0)                                                          # la botamanga cae algo más ancha
            out.append(((cx, cy, z), rx * suelto, ry * suelto, 0.0, (0.035 + cierre, 6, z * 26.0 + s * 1.3)))
        return out
    for s in (-1, 1):
        _ropa_tubo("pierna", anillos_pierna(s), azul_p, lados=44, grosor=0.006)

    cad = []
    for z in (0.74, 0.80, 0.86, 0.92):
        m = medir(z, 0, 0.0, 0.215 if z < 0.86 else 0.170, margen=0.020)
        if m: cad.append(((m[0], m[1], z), m[2], m[3], 0.0, (0.03, 5, z * 20.0)))
    if cad:
        _ropa_tubo("cadera", cad, azul_p, lados=36, grosor=0.006)

    # ---------- zapatos: botines de cuero negro con caña, empeine, suela y taco ----------
    suela_m = ropa.cuero("suela", color=(0.012, 0.010, 0.009), rugosidad=0.7)
    for s_ in (-1, 1):
        sel = [p for p in vs if p.z < 0.11 and s_ * p.x > 0.0]
        if not sel: continue
        x0 = min(p.x for p in sel); x1 = max(p.x for p in sel); y0 = min(p.y for p in sel); y1 = max(p.y for p in sel)
        cx = (x0 + x1) / 2; ancho = (x1 - x0) / 2 + 0.010
        ta = medir(0.30, s_, 0.0, 0.215, margen=0.0)                       # el tobillo: la caña va justo debajo de la pierna del pantalón
        cxa, yt = (ta[0], ta[1]) if ta else (cx, y1 - 0.052)
        _ropa_tubo("cana", [((cxa, yt, 0.020), 0.052, 0.058), ((cxa, yt, 0.075), 0.049, 0.054), ((cxa, yt, 0.140), 0.051, 0.056)], zapato_m, lados=22, grosor=0.006)
        esfera("empeine", (cx, (y0 + yt) / 2 - 0.004, 0.036), (ancho, (yt - y0) / 2 + 0.030, 0.037), zapato_m, seg=26, anillos=14, subdiv=1)
        esfera("puntera", (cx, y0 + 0.022, 0.027), (ancho * 0.94, 0.036, 0.028), zapato_m, seg=20, anillos=10, subdiv=1)
        esfera("suela", (cx, (y0 + y1) / 2 - 0.004, 0.009), (ancho + 0.004, (y1 - y0) / 2 + 0.022, 0.012), suela_m, seg=26, anillos=10, subdiv=1)
        esfera("tacon", (cx, y1 - 0.032, 0.011), (ancho * 0.82, 0.036, 0.016), suela_m, seg=16, anillos=8, subdiv=1)

    # ---------- saco: volumen suelto, algo acampanado, con pliegues; se mide el torso y se le da holgura ----------
    ang = lambda z, f: (0.045, 6, z * 17.0 + f)
    torso = []
    perfil_torso = ((0.855, 0.168, 0.120, 0.000, 0.100, 9), (0.930, 0.150, 0.108, 0.000, 0.070, 8), (1.000, 0.130, 0.098, 0.000, 0.090, 10),
                    (1.080, 0.132, 0.098, -0.002, 0.040, 6), (1.170, 0.146, 0.102, -0.003, 0.030, 5), (1.260, 0.156, 0.104, -0.003, 0.025, 5),
                    (1.330, 0.162, 0.100, -0.002, 0.022, 5))
    for z, rx, ry, cy, extra, nl in perfil_torso:
        m = medir(z, 0, 0.0, 0.185 if z < 1.28 else 0.215, margen=0.010)       # el cuerpo real: la ropa solo lo sigue si no cabe
        if m is not None:
            rx = max(rx, m[2]); ry = max(ry, m[3])
        cy_c = m[1] if m is not None else cy
        torso.append(((0.0, cy_c, z), rx, ry, 0.0, (extra, nl, z * 17.0 + z * 3.0)))
    torso += [((0, 0.001, 1.360), 0.176, 0.102, 0.0), ((0, 0.002, 1.398), 0.138, 0.088, 0.0), ((0, 0.003, 1.432), 0.098, 0.075, 0.0), ((0, 0.003, 1.458), 0.067, 0.065, 0.0)]
    saco = _ropa_tubo("saco", torso, azul, lados=72, grosor=0.007, subdiv=1)
    arbol = _arbol(saco)

    # mangas sueltas: tubos del hombro a la muñeca, con la holgura medida sobre el brazo
    def anillos_manga(s):
        out = []
        for t in (0.0, 0.10, 0.22, 0.36, 0.50, 0.64, 0.78, 0.90, 1.0):
            z = 1.372 - 0.470 * t
            x_c = 0.150 + 0.197 * t
            m = medir(z, s, max(0.10, x_c - 0.075), x_c + 0.075, margen=0.013) if t > 0.05 else None
            if m is None:
                out.append(((s * x_c, -0.004, z), 0.070, 0.066, 0.0, (0.04, 5, t * 9.0 + s)))
            else:
                cx, cy, rx, ry = m
                amp = 0.035 + 0.11 * math.exp(-((t - 0.52) / 0.10) ** 2) + 0.075 * math.exp(-((t - 0.96) / 0.08) ** 2)
                out.append(((cx, cy, z), rx * 1.0, ry, 0.0, (amp, 5, t * 11.0 + s * 1.7)))
        out = list(reversed(out))
        out.append(((s * 0.146, -0.003, 1.404), 0.062, 0.058))      # el hombro se redondea: la manga cierra por arriba
        out.append(((s * 0.128, -0.003, 1.426), 0.034, 0.032))
        return out
    for s in (-1, 1):
        _ropa_tubo("manga", anillos_manga(s), azul, lados=36, grosor=0.0055, cerrar_arriba=True)
        zc = 0.918
        m = medir(zc, s, 0.28, 0.40, margen=0.022)
        cx, cy, rx, ry = m if m else (s * 0.338, -0.004, 0.058, 0.054)
        _ropa_tubo("puno", [((cx, cy, zc - 0.010), rx, ry), ((cx, cy, zc + 0.052), rx + 0.004, ry + 0.003)], negro, lados=22, grosor=0.0055)
        for k in range(3):
            esfera("boton_puno", (cx + s * (rx + 0.002) * 0.55, cy - ry * 0.86, zc + 0.002 + k * 0.018), (0.0050, 0.0030, 0.0050), oro, seg=12, anillos=6)

    # cuello oscuro con vivo rojo y insignia
    _ropa_tubo("cuello", [((0, 0.002, 1.450), 0.064, 0.062), ((0, 0.002, 1.478), 0.060, 0.058), ((0, 0.003, 1.500), 0.058, 0.056)], negro, lados=24, grosor=0.005)
    _ropa_tubo("cuello_vivo", [((0, 0.002, 1.498), 0.0605, 0.0585), ((0, 0.003, 1.504), 0.0600, 0.0580)], rojo, lados=24, grosor=0.004)
    for s in (-1, 1):
        esfera("insignia", (s * 0.040, -0.052, 1.468), (0.0095, 0.0035, 0.0095), oro, seg=14, anillos=8)

    # botones y hombreras
    for k in range(7):
        z = 1.385 - k * 0.0625
        hit = arbol.ray_cast(Vector((0.0, -0.7, z)), Vector((0, 1, 0)))
        if hit[0] is not None:
            esfera("boton", hit[0] + hit[1] * 0.004, (0.0085, 0.0045, 0.0085), oro, seg=14, anillos=8)
    for s in (-1, 1):
        hit = arbol.ray_cast(Vector((s * 0.150, 0.0, 1.8)), Vector((0, 0, -1)))
        if hit[0] is not None:
            p = hit[0] + hit[1] * 0.006
            bpy.ops.mesh.primitive_cube_add(size=1)
            h = bpy.context.active_object
            h.name = "hombrera"; h.scale = (0.052, 0.078, 0.008); h.location = p; h.rotation_euler = (0, s * 0.42, 0)
            h.data.materials.append(oro)
            bev = h.modifiers.new("b", 'BEVEL'); bev.width = 0.003; bev.segments = 2

    # ---------- cinturón, correaje y cartucheras ----------
    zs = 1.0
    ring = min(torso, key=lambda r: abs(r[0][2] - zs))                       # el cinturón se apoya sobre el saco a esa altura
    mb = (ring[0][0], ring[0][1], ring[1] + 0.014, ring[2] + 0.014)
    cinto = _ropa_tubo("cinturon", [((mb[0], mb[1], zs - 0.023), mb[2], mb[3]), ((mb[0], mb[1], zs + 0.023), mb[2], mb[3])], marron, lados=36, grosor=0.007)
    bpy.ops.mesh.primitive_cube_add(size=1); b = bpy.context.active_object
    b.name = "hebilla"; b.scale = (0.038, 0.007, 0.032); b.location = (mb[0], mb[1] - mb[3] - 0.003, zs); b.data.materials.append(oro)
    bv = b.modifiers.new("b", 'BEVEL'); bv.width = 0.004; bv.segments = 2
    for s in (-1, 1):
        pts = [(s * (0.115 - 0.060 * (k / 8)), 1.410 - 0.405 * (k / 8)) for k in range(9)]
        _cinta("correaje", pts, arbol, 0.030, 0.0035, marron, despeje=0.010)
    for x in (-0.078, 0.0, 0.078):
        hit = arbol.ray_cast(Vector((x, -0.7, 1.062)), Vector((0, 1, 0)))
        if hit[0] is None: continue
        bpy.ops.mesh.primitive_cube_add(size=1)
        c = bpy.context.active_object
        c.name = "cartuchera"; c.scale = (0.060, 0.032, 0.066)
        c.location = hit[0] + hit[1] * 0.020
        c.rotation_mode = 'QUATERNION'; c.rotation_quaternion = (-hit[1]).to_track_quat('Y', 'Z')
        c.data.materials.append(oliva)
        bev = c.modifiers.new("b", 'BEVEL'); bev.width = 0.006; bev.segments = 3
        esfera("boton_cartuchera", hit[0] + hit[1] * 0.0385 + Vector((0, 0, 0.012)), (0.0065, 0.0035, 0.0065), oro, seg=12, anillos=6)

    # ---------- kepí ----------
    z0 = loc.z + 0.046      # más bajo: la frente quedaba enorme
    cyh = centro_cab.y
    kepi = _ropa_tubo("kepi", [((0, cyh, z0), 0.101, 0.110), ((0, cyh, z0 + 0.020), 0.101, 0.110), ((0, cyh, z0 + 0.075), 0.099, 0.108), ((0, cyh, z0 + 0.102), 0.103, 0.112)],
                      azul, lados=32, grosor=0.004, cerrar_arriba=True)
    cinta = loft("kepi_cinta", [((0, cyh, z0 + 0.002), 0.1080, 0.1170), ((0, cyh, z0 + 0.024), 0.1080, 0.1170)], oro, lados=32, subdiv=0, cerrar_arriba=False, cerrar_abajo=False)
    visera = esfera("kepi_visera", (0, cyh - 0.113, z0 + 0.004), (0.082, 0.052, 0.004), zapato_m, seg=24, anillos=8, rot=(0.20, 0, 0))
    escudo = esfera("kepi_escudo", (0, cyh - 0.1105, z0 + 0.052), (0.014, 0.004, 0.014), oro, seg=14, anillos=8)
    # el kepí se usa ladeado hacia adelante: la visera cae sobre la frente
    from mathutils import Matrix
    pivote = Vector((0, cyh, z0 + 0.03))
    giro = Matrix.Translation(pivote) @ Matrix.Rotation(0.15, 4, 'X') @ Matrix.Translation(-pivote)
    for pieza in (kepi, cinta, visera, escudo):
        pieza.matrix_basis = giro @ pieza.matrix_basis      # matrix_world de un objeto recién creado todavía no está actualizada

    return list(set(bpy.data.objects) - previos)
