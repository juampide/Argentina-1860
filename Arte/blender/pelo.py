import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from lib import *

RX, RY, RZ = 0.078, 0.098, 0.118

def _elipsoide(bm, centro, eje_largo, normal, largo, ancho, alto, seg=8, anillos=6):
    t = eje_largo.normalized()
    n = normal.normalized()
    s = t.cross(n).normalized()
    n = s.cross(t).normalized()
    rot = Matrix((s, t, n)).transposed().to_4x4()
    m = Matrix.Translation(centro) @ rot @ Matrix.Diagonal((ancho, largo, alto, 1))
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=anillos, radius=1.0, matrix=m)

def pelo_corto(cab, mat, seed=5, mechones=420, patillas=True):
    """Casquete de pelo pegado al cuero cabelludo + mechones sueltos que le dan textura; con patillas delante de la oreja."""
    rnd = random.Random(seed)
    me = cab.data
    bm = bmesh.new(); bm.from_mesh(me)
    bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    borrar = []
    for f in bm.faces:
        c = f.calc_center_median()
        nx, ny, nz = c.x / RX, c.y / RY, c.z / RZ
        ax = abs(nx)
        base = 0.55 - 0.50 * sstep(-1.0, -0.4, ny)
        base -= 0.80 * sstep(0.1, 0.8, ny)
        linea = base
        if patillas and ax > 0.78 and -0.50 < ny < -0.02:
            linea = min(linea, -0.34)
        if nz < linea: borrar.append(f)
    bmesh.ops.delete(bm, geom=borrar, context='FACES')
    for v in bm.verts:
        v.co += v.normal * (0.0045 + 0.0025 * rnd.random())
    centros = [f.calc_center_median() for f in bm.faces]
    normales = [f.normal.copy() for f in bm.faces]
    corona = Vector((0.0, 0.015, RZ * 0.95))
    for _ in range(mechones):
        i = rnd.randrange(len(centros))
        c = centros[i]; n = normales[i]
        flujo = (c - corona)
        flujo = (flujo - n * flujo.dot(n))
        if flujo.length < 1e-5: continue
        flujo += Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-1, 1))) * 0.25 * flujo.length
        # el pelo del frente peina hacia atrás y de costado
        _elipsoide(bm, c + n * 0.004, flujo, n, rnd.uniform(0.016, 0.034), rnd.uniform(0.0055, 0.009), rnd.uniform(0.0045, 0.0075))
    ob = a_objeto("pelo", bm, mat, subdiv=0, suave=True)
    ob.location = cab.location
    return ob

def bigote_fino(cab, mat, cz):
    """Dos mechones de bigote que nacen del centro del labio superior y caen hacia las comisuras."""
    bm = bmesh.new()
    zl = -0.44 * RZ - 0.006
    for s in (-1, 1):
        for k in range(5):
            t = k / 4
            x = s * (0.004 + 0.034 * t)
            z = zl - 0.008 * t * t - 0.002
            # punto de la superficie de la cara en (x, z)
            hit = cab.ray_cast(Vector((x, -0.4, z)), Vector((0, 1, 0)))
            if not hit[0]: continue
            p = hit[1] + hit[2] * 0.0035
            _elipsoide(bm, p, Vector((s * 0.9, 0, -0.35)), hit[2], 0.0115 - 0.005 * t, 0.0042 - 0.0016 * t, 0.0042 - 0.0016 * t, seg=10, anillos=6)
    ob = a_objeto("bigote", bm, mat, subdiv=0)
    ob.location = cab.location
    return ob
