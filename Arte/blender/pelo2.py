"""Pelo, patillas y bigote sobre una cabeza ya modelada (malla evaluada). Coordenadas locales del cuerpo base: frente = -Y, Z arriba."""
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from lib import *
from pelo import _elipsoide

def malla_cabeza(cuerpo, zmin):
    """Copia (bmesh) de la parte de la malla evaluada (con escultura) por encima de zmin, en coordenadas locales."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = cuerpo.evaluated_get(dg)
    me = ev.to_mesh()
    bm = bmesh.new(); bm.from_mesh(me)
    ev.to_mesh_clear()
    sobra = [f for f in bm.faces if f.calc_center_median().z < zmin]
    bmesh.ops.delete(bm, geom=sobra, context='FACES')
    sobra = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=sobra, context='VERTS')
    return bm

def pelo_oficial(cuerpo, mat, centro, rx=0.080, ry=0.105, rz=0.120, seed=11, mechones=1900):
    """Pelo corto peinado hacia atrás con patillas; mechones finos y largos sobre un casquete."""
    rnd = random.Random(seed)
    bm = malla_cabeza(cuerpo, centro.z - 0.15)
    borrar = []
    for f in bm.faces:
        c = f.calc_center_median() - centro
        nx, ny, nz = c.x / rx, c.y / ry, c.z / rz
        ax = abs(nx)
        base = 0.46 - 0.40 * sstep(-1.0, -0.35, ny)       # línea del pelo: alta al frente, baja a los lados
        base -= 0.80 * sstep(0.05, 0.75, ny)               # nuca
        linea = base
        if ax > 0.80 and -0.55 < ny < -0.05:
            linea = min(linea, -0.42)                        # patillas delante de la oreja
        if ax > 0.86 and ny > -0.05:
            linea = max(linea, 0.18)                         # la oreja queda libre
        if nz < linea: borrar.append(f)
    bmesh.ops.delete(bm, geom=borrar, context='FACES')
    sobra = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=sobra, context='VERTS')
    bm.normal_update()
    for v in bm.verts:
        v.co += v.normal * (0.0030 + 0.0020 * rnd.random())
    centros = [f.calc_center_median() for f in bm.faces]
    normales = [f.normal.copy() for f in bm.faces]
    corona = centro + Vector((0.0, -0.045, rz * 0.9))        # peinado hacia atrás desde adelante
    for _ in range(mechones):
        i = rnd.randrange(len(centros))
        c = centros[i]; n = normales[i]
        flujo = c - corona
        flujo = flujo - n * flujo.dot(n)
        if flujo.length < 1e-5: continue
        flujo += Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-1, 1))) * 0.18 * flujo.length
        _elipsoide(bm, c + n * 0.0028, flujo, n, rnd.uniform(0.018, 0.034), rnd.uniform(0.0026, 0.0042), rnd.uniform(0.0022, 0.0036), seg=8, anillos=5)
    me2 = bpy.data.meshes.new("pelo"); bm.to_mesh(me2); bm.free()
    ob = bpy.data.objects.new("pelo", me2); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    for p in me2.polygons: p.use_smooth = True
    ob.matrix_world = cuerpo.matrix_world
    return ob

def bigote(cuerpo, mat, ojo_z, largo=0.036, seed=4):
    """Bigote de oficial: nace bajo la nariz y cae hacia las comisuras, con mechones finos."""
    rnd = random.Random(seed)
    bm0 = malla_cabeza(cuerpo, ojo_z - 0.14)
    tree = BVHTree.FromBMesh(bm0)
    bm = bmesh.new()
    z0 = ojo_z - 0.0535
    for s in (-1, 1):
        for k in range(14):
            t = k / 13
            x = s * (0.003 + largo * t)
            z = z0 - 0.007 * (t ** 1.5) + rnd.uniform(-0.0006, 0.0006)
            hit = tree.ray_cast(Vector((x, -0.5, z)), Vector((0, 1, 0)))
            if hit[0] is None: continue
            p = hit[0] + hit[1] * 0.0020
            _elipsoide(bm, p, Vector((s * 0.9, 0.05, -0.30 - 0.5 * t)), hit[1], 0.0088 - 0.0040 * t, 0.0030 - 0.0010 * t, 0.0029 - 0.0010 * t, seg=8, anillos=5)
    bm0.free()
    me = bpy.data.meshes.new("bigote"); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("bigote", me); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    for p in me.polygons: p.use_smooth = True
    ob.matrix_world = cuerpo.matrix_world
    return ob
