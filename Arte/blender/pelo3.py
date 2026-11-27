"""Pelo estilizado como masa esculpida (casquete con surcos y mechones grandes) y bigote sólido. Sobre la malla evaluada de la cabeza.
Coordenadas locales del cuerpo base: frente = -Y, Z arriba."""
import bpy, bmesh, math, random
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from lib import *
from pelo2 import malla_cabeza

def _ruido(x, y, z, sem):
    return (math.sin(x * 12.9898 + y * 78.233 + z * 37.719 + sem) * 43758.5453) % 1.0

def linea_pelo(nx, ny, nz, sem=0.0):
    ax = abs(nx)
    base = 0.10 - 0.04 * sstep(-1.0, -0.35, ny)
    base -= 0.85 * sstep(-0.05, 0.45, ny)
    f = sstep(0.66, 0.86, ax) * sstep(-0.64, -0.50, ny) * (1.0 - sstep(-0.34, -0.04, ny))
    base = base * (1 - f) + (-0.40) * f                 # patilla en cuña delante de la oreja
    g = sstep(0.62, 0.84, ax) * sstep(-0.34, -0.14, ny)      # los costados bajan hasta justo encima de la oreja (la oreja queda mas abajo)
    base = min(base, base * (1 - g) + (-0.30) * g)
    # borde irregular: el nacimiento del pelo no es una línea recta
    base += 0.025 * math.sin(nx * 9 + sem) + 0.015 * math.sin(nx * 21 + 2.0 * sem)
    return base

def pelo_masas(cuerpo, mat, centro, rx=0.080, ry=0.105, rz=0.120, seed=3, grosor=0.0125, copete=0.028):
    """Casquete de pelo con mechones anchos peinados hacia atrás, copete al frente y patillas. Los surcos van de frente a nuca."""
    rnd = random.Random(seed)
    sem = rnd.random() * 10
    bm = malla_cabeza(cuerpo, centro.z - 0.15)
    bm.normal_update()
    ref = bm.copy(); arbol = BVHTree.FromBMesh(ref)
    # límite del pelo: cada vértice tiene su 'linea' y se queda si está por encima
    linea = lambda nx, ny, nz: linea_pelo(nx, ny, nz, sem)
    info = {}
    for v in bm.verts:
        c = v.co - centro
        nx, ny, nz = c.x / rx, c.y / ry, c.z / rz
        fuera_craneo = (nx * nx + ny * ny + nz * nz) > 1.22 and abs(nx) > 0.78     # la oreja
        info[v.index] = (nx, ny, nz, (nz - linea(nx, ny, nz)) if not fuera_craneo else -1.0)
    borrar = [f for f in bm.faces if sum(1 for v in f.verts if info[v.index][3] > 0) < len(f.verts)]
    bmesh.ops.delete(bm, geom=borrar, context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    # se queda solo la pieza más grande del pelo: los trocitos sueltos del borde se veían como cuernitos
    bm.faces.ensure_lookup_table()
    visto, islas = set(), []
    for f0 in bm.faces:
        if f0.index in visto: continue
        pila, isla = [f0], []
        visto.add(f0.index)
        while pila:
            f = pila.pop(); isla.append(f)
            for e in f.edges:
                for g in e.link_faces:
                    if g.index not in visto:
                        visto.add(g.index); pila.append(g)
        islas.append(isla)
    if len(islas) > 1:
        islas.sort(key=len, reverse=True)
        sobra = [f for isla in islas[1:] for f in isla]
        bmesh.ops.delete(bm, geom=sobra, context='FACES')
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.verts.ensure_lookup_table(); bm.normal_update()
    # borde limpio: suavizar el contorno a lo largo del propio borde y volver a pegarlo a la piel
    borde_v = [v for v in bm.verts if v.is_boundary]
    for _ in range(14):
        nuevos = {}
        for v in borde_v:
            vec = [e.other_vert(v).co for e in v.link_edges if e.is_boundary]
            if len(vec) == 2:
                nuevos[v.index] = (v.co * 0.4 + (vec[0] + vec[1]) * 0.3)
        for v in borde_v:
            if v.index in nuevos:
                hit = arbol.find_nearest(nuevos[v.index])
                if hit[0] is not None: v.co = hit[0]
    for v in bm.verts:
        c = v.co - centro
        nx, ny, nz = c.x / rx, c.y / ry, c.z / rz
        borde = sstep(0.0, 0.22, nz - linea(nx, ny, nz))          # 0 en el nacimiento, 1 adentro
        s = (ny + 1.0) / 2.0                                         # 0 frente → 1 nuca
        # mechones: bandas anchas que van de frente a nuca, algo onduladas
        onda = 0.5 + 0.5 * math.sin(nx * 15.0 + 2.2 * math.sin(s * 4.0 + nx * 3.0) + sem)
        onda2 = 0.5 + 0.5 * math.sin(nx * 6.5 + 1.3 * math.sin(s * 3.0 + sem))
        masa = grosor * (0.55 + 0.45 * onda2) + 0.0045 * onda
        # copete: volumen que sube al frente y cae hacia atrás
        copete_h = copete * math.exp(-(((ny + 0.70) / 0.30) ** 2) - ((nx / 0.75) ** 2)) * sstep(0.2, 0.9, nz)
        grueso = (masa * borde + 0.0015) 
        v.co = v.co + v.normal * grueso + Vector((0, 0.35 * copete_h, copete_h)) * borde
    # suavizar un poco la superficie resultante y dar volumen
    bmesh.ops.smooth_vert(bm, verts=bm.verts, factor=0.35, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    ref.free()
    me = bpy.data.meshes.new("pelo"); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("pelo", me); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    for p in me.polygons: p.use_smooth = True
    sol = ob.modifiers.new("grosor", 'SOLIDIFY'); sol.thickness = 0.007; sol.offset = -1.0; sol.use_rim = True
    mod = ob.modifiers.new("sub", 'SUBSURF'); mod.levels = 1; mod.render_levels = 1
    ob.matrix_world = cuerpo.matrix_world
    return ob

def bigote_solido(cuerpo, mat, ojo_z, ancho=0.031, grosor=0.0064):
    """Bigote de oficial: dos mechones sólidos y cónicos que nacen bajo la nariz y caen hacia las comisuras."""
    bm0 = malla_cabeza(cuerpo, ojo_z - 0.14)
    tree = BVHTree.FromBMesh(bm0)
    objs = []
    z0 = ojo_z - 0.0515
    for lado in (-1, 1):
        pts, rad = [], []
        for k in range(9):
            t = k / 8
            x = lado * (0.002 + ancho * t)
            z = z0 - 0.0125 * (t ** 1.3)
            hit = tree.ray_cast(Vector((x, -0.5, z)), Vector((0, 1, 0)))
            if hit[0] is None: continue
            pts.append(hit[0] + hit[1] * (0.0016 + grosor * 0.55))
            rad.append(grosor * (1.0 - 0.72 * (t ** 1.2)))
        if len(pts) >= 3:
            objs.append(tubo("bigote", pts, rad, mat, lados=10, aplastar=0.78, subdiv=1))
    bm0.free()
    for o in objs: o.matrix_world = cuerpo.matrix_world
    return objs


def _norm(v):
    return v.normalized() if v.length > 1e-9 else Vector((0, -1, 0))

def mechones(cuerpo, mat, centro, rx=0.080, ry=0.105, rz=0.120, seed=21, cantidad=1.0):
    """Mechones grandes y cónicos (pinceladas) que nacen en la línea del pelo y se peinan hacia atrás, pegados al cráneo.
    Devuelve la lista de objetos creados."""
    rnd = random.Random(seed)
    sem = 0.0
    bm0 = malla_cabeza(cuerpo, centro.z - 0.15)
    tree = BVHTree.FromBMesh(bm0)
    radios = Vector((rx, ry, rz))
    def sobre(d, lift):
        o = centro + Vector((d.x * radios.x, d.y * radios.y, d.z * radios.z)) * 1.6
        hit = tree.ray_cast(o, -d)
        if hit[0] is None: return None
        return hit[0] + hit[1] * lift
    def mechon(nombre, d0, d_fin, r0, largo_t=1.0, elevar=0.0, curva=0.0, aplastar=0.55, pasos=9):
        pts, rad = [], []
        for k in range(pasos):
            t = k / (pasos - 1) * largo_t
            d = _norm(d0 * (1 - t) + d_fin * t)
            d = _norm(d + Vector((curva * math.sin(t * 3.14159) * (1 if d0.x >= 0 else -1), 0, 0)))
            r = r0 * (1.0 - 0.88 * (t ** 1.15))
            p = sobre(d, r * 0.55 + elevar * math.sin(t * 3.14159) )
            if p is None: continue
            pts.append(p); rad.append(max(r, 0.0012))
        if len(pts) >= 4:
            return tubo(nombre, pts, rad, mat, lados=8, aplastar=aplastar, subdiv=1)
        return None
    objs = []
    # frente: arco de mechones sobre la frente, peinados hacia atrás con el copete elevado
    n = int(21 * cantidad)
    for i in range(n):
        u = -1 + 2 * i / max(1, n - 1)
        nx = u * 0.80 + rnd.uniform(-0.04, 0.04)
        ny = -0.96 + 0.10 * abs(u) ** 1.5
        nz = linea_pelo(nx, ny, 0.0, sem) - 0.02 + rnd.uniform(-0.015, 0.015)
        d0 = _norm(Vector((nx / 1.0, ny, nz)))
        d_fin = _norm(Vector((nx * 0.55, 0.55, 0.62)))
        m = mechon("mechon_frente", d0, d_fin, rnd.uniform(0.0105, 0.0150) * (1.15 - 0.35 * abs(u)),
                   elevar=0.012 * (1 - abs(u)) + 0.004, curva=0.10 * u, largo_t=rnd.uniform(0.85, 1.0))
        if m: objs.append(m)
    # coronilla y nuca: mechones largos que van hacia atrás y abajo
    for i in range(int(12 * cantidad)):
        a = rnd.uniform(-0.9, 0.9)
        d0 = _norm(Vector((a * 0.75, -0.15 + rnd.uniform(-0.1, 0.1), 0.96)))
        d_fin = _norm(Vector((a * 0.8, 0.95, 0.10)))
        m = mechon("mechon_atras", d0, d_fin, rnd.uniform(0.012, 0.016), elevar=0.005, curva=0.05 * a, largo_t=1.0)
        if m: objs.append(m)
    bm0.free()
    for o in objs:
        o.matrix_world = cuerpo.matrix_world
    return objs


def bigote_facetado(cuerpo, mat, ojo_z, ancho=0.029, grosor=0.0105):
    """Bigote como masa de planos marcados (sección en rombo, sin suavizar): se lee como forma esculpida y no como tubo blando."""
    bm0 = malla_cabeza(cuerpo, ojo_z - 0.14)
    tree = BVHTree.FromBMesh(bm0)
    objs = []
    z0 = ojo_z - 0.0510
    for lado in (-1, 1):
        pts, rad = [], []
        for k in range(5):
            t = k / 4
            x = lado * (0.0015 + ancho * t)
            z = z0 - 0.0105 * (t ** 1.5)
            hit = tree.ray_cast(Vector((x, -0.5, z)), Vector((0, 1, 0)))
            if hit[0] is None: continue
            pts.append(hit[0] + hit[1] * (0.0008 + grosor * 0.30))
            rad.append(grosor * (1.0 - 0.72 * (t ** 1.2)))
        if len(pts) >= 3:
            o = tubo("bigote", pts, rad, mat, lados=4, aplastar=0.85, subdiv=0)
            for q in o.data.polygons: q.use_smooth = False
            objs.append(o)
    bm0.free()
    for o in objs: o.matrix_world = cuerpo.matrix_world
    return objs
