"""Esqueleto humanoide para el personaje y reparto de pesos por distancia al hueso más cercano.
Se construye en coordenadas locales del cuerpo base (las mismas en las que se exporta); R = lado +X, L = lado -X.
Nombres de huesos pensados para Unity: Hips, Spine, Chest, Neck, Head, Shoulder_*, UpperArm_*, LowerArm_*, Hand_*, UpperLeg_*, LowerLeg_*, Foot_*, Toes_*."""
import bpy, math
from mathutils import Vector

# (nombre, padre, cabeza, cola) se llenan en crear()


def _centro(vs, z, lado, xmin, xmax, banda=0.02):
    sel = [p for p in vs if abs(p.z - z) < banda and xmin <= lado * p.x <= xmax]
    if not sel:
        return None
    return Vector(((min(p.x for p in sel) + max(p.x for p in sel)) / 2, (min(p.y for p in sel) + max(p.y for p in sel)) / 2, z))


def crear(cuerpo, nombre="Esqueleto"):
    """Arma la armadura a partir de las medidas reales del cuerpo (ya adelgazado). Devuelve (objeto armadura, {hueso: (cabeza, cola)})."""
    vs = [v.co.copy() for v in cuerpo.data.vertices]
    huesos = {}

    def h(nom, padre, cabeza, cola):
        huesos[nom] = (padre, Vector(cabeza), Vector(cola))

    cad = _centro(vs, 0.96, 0, 0.0, 0.17) or Vector((0, 0, 0.96))
    h("Hips", None, (cad.x, cad.y, 0.93), (cad.x, cad.y, 1.04))
    h("Spine", "Hips", (cad.x, cad.y, 1.04), (cad.x, cad.y, 1.20))
    h("Chest", "Spine", (cad.x, cad.y, 1.20), (cad.x, cad.y, 1.38))
    h("Neck", "Chest", (cad.x, cad.y + 0.005, 1.38), (cad.x, cad.y + 0.01, 1.50))
    h("Head", "Neck", (cad.x, cad.y + 0.01, 1.50), (cad.x, cad.y + 0.01, 1.70))
    for lado, suf in ((1, "R"), (-1, "L")):
        pierna_c = _centro(vs, 0.93, lado, 0.0, 0.215) or Vector((lado * 0.08, 0, 0.93))
        rodilla = _centro(vs, 0.50, lado, 0.0, 0.215) or Vector((lado * 0.08, -0.01, 0.50))
        tobillo = _centro(vs, 0.12, lado, 0.0, 0.215) or Vector((lado * 0.08, 0.02, 0.12))
        pie = [p for p in vs if p.z < 0.11 and lado * p.x > 0.0]
        y_punta = min((p.y for p in pie), default=tobillo.y - 0.17)
        h(f"UpperLeg_{suf}", "Hips", (pierna_c.x, pierna_c.y, 0.93), (rodilla.x, rodilla.y, 0.50))
        h(f"LowerLeg_{suf}", f"UpperLeg_{suf}", (rodilla.x, rodilla.y, 0.50), (tobillo.x, tobillo.y, 0.095))
        h(f"Foot_{suf}", f"LowerLeg_{suf}", (tobillo.x, tobillo.y, 0.095), (tobillo.x, y_punta + 0.05, 0.035))
        h(f"Toes_{suf}", f"Foot_{suf}", (tobillo.x, y_punta + 0.05, 0.035), (tobillo.x, y_punta + 0.005, 0.025))
        # brazo: del hombro a la muñeca por el eje del brazo, con el codo medido
        sh = Vector((lado * 0.150, cad.y, 1.372))
        codo = _centro(vs, 1.135, lado, 0.15, 0.40, banda=0.03) or Vector((lado * 0.25, -0.01, 1.135))
        mun = _centro(vs, 0.905, lado, 0.25, 0.40, banda=0.03) or Vector((lado * 0.34, -0.01, 0.905))
        mano_pts = [p for p in vs if lado * p.x > 0.30 and p.z < 0.90]
        z_dedos = min((p.z for p in mano_pts), default=0.72)
        h(f"Shoulder_{suf}", "Chest", (cad.x + lado * 0.02, cad.y, 1.395), (sh.x, sh.y, sh.z + 0.01))
        h(f"UpperArm_{suf}", f"Shoulder_{suf}", (sh.x, sh.y, sh.z + 0.01), (codo.x, codo.y, 1.135))
        h(f"LowerArm_{suf}", f"UpperArm_{suf}", (codo.x, codo.y, 1.135), (mun.x, mun.y, 0.905))
        h(f"Hand_{suf}", f"LowerArm_{suf}", (mun.x, mun.y, 0.905), (mun.x + lado * 0.03, mun.y, z_dedos + 0.01))
        def xmano(z): return lado * (0.285 + (0.905 - z) * 0.27)      # los dedos se abren hacia afuera a medida que bajan
        # (nombre, y del nudillo, z del nudillo, largos de falange)
        for nombre, y, zn, largos in (("Index", -0.120, 0.815, (0.028, 0.022, 0.020)), ("Middle", -0.098, 0.812, (0.031, 0.025, 0.021)),
                                      ("Ring", -0.075, 0.814, (0.029, 0.023, 0.020)), ("Pinky", -0.053, 0.820, (0.023, 0.018, 0.016))):
            z0 = zn
            padre = f"Hand_{suf}"
            for k, largo in enumerate(largos):
                z1 = z0 - largo
                h(f"{nombre}_{k + 1}_{suf}", padre, (xmano(z0), y, z0), (xmano(z1), y, z1))
                padre = f"{nombre}_{k + 1}_{suf}"
                z0 = z1
        # pulgar: nace en la base de la palma, por delante, y baja hacia afuera
        pts = [((0.288, -0.138, 0.870), (0.300, -0.146, 0.838)), ((0.300, -0.146, 0.838), (0.314, -0.151, 0.810)), ((0.314, -0.151, 0.810), (0.326, -0.152, 0.784))]
        padre = f"Hand_{suf}"
        for k, (a, b) in enumerate(pts):
            h(f"Thumb_{k + 1}_{suf}", padre, (lado * a[0], a[1], a[2]), (lado * b[0], b[1], b[2]))
            padre = f"Thumb_{k + 1}_{suf}"

    arm_data = bpy.data.armatures.new(nombre)
    arm = bpy.data.objects.new(nombre, arm_data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    orden = list(huesos.keys())
    for nom in orden:
        padre, cab, cola = huesos[nom]
        b = eb.new(nom)
        b.head = cab
        b.tail = cola if (cola - cab).length > 1e-4 else cab + Vector((0, 0, 0.02))
    for nom in orden:
        padre = huesos[nom][0]
        if padre:
            eb[nom].parent = eb[padre]
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.select_set(False)
    return arm, {n: (huesos[n][1], huesos[n][2]) for n in huesos}


def _dist_segmento(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(1e-9, ab.dot(ab))))
    return (p - (a + ab * t)).length


def pesar_por_distancia(ob, geom_huesos, permitidos=None, sigma=0.045, maximo=3, cabeza_z=(1.40, 1.46)):
    """Pesos de piel: cada vértice recibe los 3 huesos más cercanos con caída gaussiana. permitidos: limita a esos huesos."""
    nombres = [n for n in geom_huesos if (permitidos is None or n in permitidos)]
    grupos = {n: ob.vertex_groups.new(name=n) for n in nombres}
    for v in ob.data.vertices:
        ds = []
        for n in nombres:
            a, b = geom_huesos[n]
            ds.append((_dist_segmento(v.co, a, b), n))
        ds.sort()
        cercanos = ds[:maximo]
        d0 = cercanos[0][0]
        ws = [math.exp(-(((d - d0) / sigma) ** 2)) for d, _ in cercanos]
        total = sum(ws)
        pesos = {n: w / total for (d, n), w in zip(cercanos, ws) if w / total > 0.02}
        if cabeza_z and "Head" in grupos and v.co.z > cabeza_z[0]:
            t = min(1.0, (v.co.z - cabeza_z[0]) / max(1e-6, cabeza_z[1] - cabeza_z[0]))   # 0 en el cuello, 1 en la cabeza
            pesos = {n: w * (1 - t) for n, w in pesos.items()}
            pesos["Head"] = pesos.get("Head", 0.0) + t
        for n, w in pesos.items():
            if w > 0.01:
                grupos[n].add([v.index], w, 'REPLACE')


def atar_rigido(ob, hueso):
    g = ob.vertex_groups.new(name=hueso)
    g.add(list(range(len(ob.data.vertices))), 1.0, 'REPLACE')


def enlazar_armadura(ob, arm):
    mod = ob.modifiers.new("Armature", 'ARMATURE')
    mod.object = arm
    ob.parent = arm
