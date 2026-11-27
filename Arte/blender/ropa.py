"""Ropa a partir de la propia malla del cuerpo: se recortan las caras de una zona, se inflan a lo largo de las normales,
se les da grosor y se les agregan pliegues. Coordenadas locales del cuerpo base (Z arriba, frente = -Y)."""
import bpy, bmesh, math
from mathutils import Vector
from lib import *
from mathutils.bvhtree import BVHTree


def adelgazar(cuerpo, factor_x=0.78, factor_y=0.86, z_max=1.46, pecho_y=0.74):
    """Hace el cuerpo más flaco (de los hombros para abajo). El pecho además se aplana de frente a espalda: el de la base es muy profundo."""
    for v in cuerpo.data.vertices:
        if v.co.z < z_max:
            suave = 1.0 if v.co.z < z_max - 0.10 else (z_max - v.co.z) / 0.10
            v.co.x *= 1.0 - (1.0 - factor_x) * suave
            v.co.y *= 1.0 - (1.0 - factor_y) * suave
            if 0.90 < v.co.z < 1.45 and abs(v.co.x) < 0.20:
                b = min(1.0, (v.co.z - 0.90) / 0.10) * min(1.0, (1.45 - v.co.z) / 0.08)
                v.co.y *= 1.0 - (1.0 - pecho_y) * b


def malla_zona(cuerpo, criterio):
    """bmesh con las caras de la malla evaluada (con escultura) cuyo centro cumple criterio(centro)."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = cuerpo.evaluated_get(dg)
    me = ev.to_mesh()
    bm = bmesh.new()
    bm.from_mesh(me)
    ev.to_mesh_clear()
    sobra = [f for f in bm.faces if not criterio(f.calc_center_median())]
    bmesh.ops.delete(bm, geom=sobra, context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.normal_update()
    return bm


def _suavizar_borde(bm, iteraciones=10, fijar=None):
    borde = [v for v in bm.verts if v.is_boundary]
    for _ in range(iteraciones):
        nuevos = {}
        for v in borde:
            vec = [e.other_vert(v).co for e in v.link_edges if e.is_boundary]
            if len(vec) == 2:
                nuevos[v.index] = v.co * 0.4 + (vec[0] + vec[1]) * 0.3
        for v in borde:
            if v.index in nuevos:
                v.co = nuevos[v.index]
                if fijar: fijar(v)


def prenda(nombre, bm, mat, inflar, pliegues=0.004, grosor=0.004, ajustar_borde=None, escala_pliegue=22.0, sem=1.0, subdiv=1, suavizado=0, perfil=None):
    """Infla las caras a lo largo de la normal, agrega pliegues suaves (ruido de baja frecuencia) y da grosor."""
    bm.normal_update()
    if suavizado:
        # la tela cuelga: se promedia la forma (sin tocar el borde) para que no copie cada músculo
        interior = [v for v in bm.verts if not v.is_boundary]
        for _ in range(suavizado):
            bmesh.ops.smooth_vert(bm, verts=interior, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
        bm.normal_update()
    orig = {v.index: (v.co.copy(), v.normal.copy()) for v in bm.verts}
    for v in bm.verts:
        p, n = orig[v.index]
        f1 = math.sin(p.x * escala_pliegue + p.z * 9.0 + sem) * math.sin(p.z * escala_pliegue * 0.8 + p.y * 7.0 + 2 * sem)
        f2 = math.sin(p.z * escala_pliegue * 1.7 + p.x * 4.0 + 3 * sem)
        extra = perfil(p) if perfil else 0.0
        v.co = p + n * (inflar + extra + pliegues * (0.7 * f1 + 0.3 * f2))
    if ajustar_borde:
        for v in bm.verts:
            if v.is_boundary: ajustar_borde(v)
        _suavizar_borde(bm, 8, ajustar_borde)
    bm.normal_update()
    arbol = BVHTree.FromBMesh(bm)
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(nombre, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    for p in me.polygons: p.use_smooth = True
    sol = ob.modifiers.new("grosor", 'SOLIDIFY'); sol.thickness = grosor; sol.offset = -1.0; sol.use_rim = True
    if subdiv:
        sub = ob.modifiers.new("sub", 'SUBSURF'); sub.levels = subdiv; sub.render_levels = subdiv
    return ob, arbol


# ---------- materiales de tela ----------
def tela(nombre, color, rugosidad=0.85, escala_trama=420.0, fuerza_trama=0.35, mugre=0.35, desgaste_abajo=0.0, z_base=0.0, z_alto=1.0):
    """Lana/algodón: trama tejida en relieve, variación de tono, mugre en manchas y (opcional) barro hacia abajo."""
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    nt = m.node_tree; n = nt.nodes; l = nt.links
    p = n["Principled BSDF"]
    coord = n.new("ShaderNodeTexCoord")
    obj = coord.outputs["Object"]
    rb = n.new("ShaderNodeRGB"); rb.outputs[0].default_value = (color[0], color[1], color[2], 1)
    # variación de tono
    r1 = n.new("ShaderNodeTexNoise"); r1.inputs["Scale"].default_value = 14; r1.inputs["Detail"].default_value = 4
    osc = n.new("ShaderNodeMix"); osc.data_type = 'RGBA'; osc.inputs[7].default_value = (color[0] * 0.55, color[1] * 0.55, color[2] * 0.55, 1)
    f1 = n.new("ShaderNodeMath"); f1.operation = 'MULTIPLY'; f1.inputs[1].default_value = 0.55
    l.new(r1.outputs["Fac"], f1.inputs[0]); l.new(f1.outputs[0], osc.inputs[0])
    l.new(rb.outputs[0], osc.inputs[6])
    col = osc.outputs[2]
    # mugre en manchas
    r2 = n.new("ShaderNodeTexNoise"); r2.inputs["Scale"].default_value = 7; r2.inputs["Detail"].default_value = 8
    rp = n.new("ShaderNodeValToRGB"); rp.color_ramp.elements[0].position = 0.45; rp.color_ramp.elements[1].position = 0.75
    l.new(r2.outputs["Fac"], rp.inputs["Fac"])
    sc = n.new("ShaderNodeSeparateColor"); l.new(rp.outputs["Color"], sc.inputs[0])
    mz = n.new("ShaderNodeMix"); mz.data_type = 'RGBA'; mz.inputs[7].default_value = (0.20, 0.15, 0.10, 1)
    mm = n.new("ShaderNodeMath"); mm.operation = 'MULTIPLY'; mm.inputs[1].default_value = mugre
    l.new(sc.outputs[0], mm.inputs[0]); l.new(mm.outputs[0], mz.inputs[0]); l.new(col, mz.inputs[6])
    col = mz.outputs[2]
    if desgaste_abajo > 0:
        sep = n.new("ShaderNodeSeparateXYZ"); l.new(obj, sep.inputs[0])
        # barro: más oscuro y marrón hacia abajo (en coordenadas locales del cuerpo)
        mr = n.new("ShaderNodeMapRange"); mr.inputs["From Min"].default_value = z_alto; mr.inputs["From Max"].default_value = z_base
        l.new(sep.outputs["Z"], mr.inputs["Value"])
        br = n.new("ShaderNodeTexNoise"); br.inputs["Scale"].default_value = 18; br.inputs["Detail"].default_value = 6
        mul = n.new("ShaderNodeMath"); mul.operation = 'MULTIPLY'; l.new(mr.outputs[0], mul.inputs[0]); l.new(br.outputs["Fac"], mul.inputs[1])
        mul2 = n.new("ShaderNodeMath"); mul2.operation = 'MULTIPLY'; mul2.inputs[1].default_value = desgaste_abajo * 2.2
        l.new(mul.outputs[0], mul2.inputs[0])
        cl = n.new("ShaderNodeMath"); cl.operation = 'MINIMUM'; cl.inputs[1].default_value = 1.0; l.new(mul2.outputs[0], cl.inputs[0])
        bz = n.new("ShaderNodeMix"); bz.data_type = 'RGBA'; bz.inputs[7].default_value = (0.16, 0.115, 0.075, 1)
        l.new(cl.outputs[0], bz.inputs[0]); l.new(col, bz.inputs[6])
        col = bz.outputs[2]
    l.new(col, p.inputs["Base Color"])
    # relieve: trama tejida + arrugas finas
    sepo = n.new("ShaderNodeSeparateXYZ"); l.new(obj, sepo.inputs[0])
    w1 = n.new("ShaderNodeTexWave"); w1.wave_type = 'BANDS'; w1.bands_direction = 'X'; w1.inputs["Scale"].default_value = escala_trama; w1.inputs["Distortion"].default_value = 0.6
    w2 = n.new("ShaderNodeTexWave"); w2.wave_type = 'BANDS'; w2.bands_direction = 'Z'; w2.inputs["Scale"].default_value = escala_trama; w2.inputs["Distortion"].default_value = 0.6
    l.new(obj, w1.inputs["Vector"]); l.new(obj, w2.inputs["Vector"])
    tr = n.new("ShaderNodeMath"); tr.operation = 'MULTIPLY'; l.new(w1.outputs["Fac"], tr.inputs[0]); l.new(w2.outputs["Fac"], tr.inputs[1])
    tr2 = n.new("ShaderNodeMath"); tr2.operation = 'MULTIPLY'; tr2.inputs[1].default_value = fuerza_trama
    l.new(tr.outputs[0], tr2.inputs[0])
    fn = n.new("ShaderNodeTexNoise"); fn.inputs["Scale"].default_value = 90; fn.inputs["Detail"].default_value = 5
    fn2 = n.new("ShaderNodeMath"); fn2.operation = 'MULTIPLY'; fn2.inputs[1].default_value = 0.25
    l.new(fn.outputs["Fac"], fn2.inputs[0])
    sm = n.new("ShaderNodeMath"); sm.operation = 'ADD'; l.new(tr2.outputs[0], sm.inputs[0]); l.new(fn2.outputs[0], sm.inputs[1])
    bump = n.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 0.5; bump.inputs["Distance"].default_value = 0.0015
    l.new(sm.outputs[0], bump.inputs["Height"]); l.new(bump.outputs["Normal"], p.inputs["Normal"])
    p.inputs["Roughness"].default_value = rugosidad
    return m


def cuero(nombre, color=(0.03, 0.022, 0.018), rugosidad=0.5):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    nt = m.node_tree; n = nt.nodes; l = nt.links
    p = n["Principled BSDF"]
    p.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1)
    p.inputs["Roughness"].default_value = rugosidad
    coord = n.new("ShaderNodeTexCoord")
    ru = n.new("ShaderNodeTexNoise"); ru.inputs["Scale"].default_value = 160; ru.inputs["Detail"].default_value = 6
    l.new(coord.outputs["Object"], ru.inputs["Vector"])
    bump = n.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 0.6; bump.inputs["Distance"].default_value = 0.0015
    l.new(ru.outputs["Fac"], bump.inputs["Height"]); l.new(bump.outputs["Normal"], p.inputs["Normal"])
    # desgaste: más claro y brillante en manchas
    rp = n.new("ShaderNodeValToRGB"); rp.color_ramp.elements[0].position = 0.55; rp.color_ramp.elements[1].position = 0.8
    l.new(ru.outputs["Fac"], rp.inputs["Fac"])
    sc = n.new("ShaderNodeSeparateColor"); l.new(rp.outputs["Color"], sc.inputs[0])
    mx = n.new("ShaderNodeMix"); mx.data_type = 'RGBA'
    mx.inputs[6].default_value = (color[0], color[1], color[2], 1); mx.inputs[7].default_value = (0.16, 0.11, 0.08, 1)
    mu = n.new("ShaderNodeMath"); mu.operation = 'MULTIPLY'; mu.inputs[1].default_value = 0.6
    l.new(sc.outputs[0], mu.inputs[0]); l.new(mu.outputs[0], mx.inputs[0]); l.new(mx.outputs[2], p.inputs["Base Color"])
    return m


def metal_dorado(nombre="dorado"):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (0.62, 0.45, 0.14, 1)
    p.inputs["Metallic"].default_value = 1.0
    p.inputs["Roughness"].default_value = 0.35
    return m
