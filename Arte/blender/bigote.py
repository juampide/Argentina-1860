"""Bigote de oficial como una masa con silueta definida, proyectada sobre la cara (no un tubo).
Gruesa al centro, se afina y cae hacia las puntas; con vetas de pelo en el relieve (material pelo_bigote)."""
import bpy, bmesh, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from lib import *
from pelo2 import malla_cabeza


def _perfil(u, ancho, alto_centro, alto_punta, caida):
    """Devuelve (x, z_arriba, z_abajo) del contorno en el parámetro u en [-1, 1]."""
    a = abs(u)
    x = u * ancho
    z_arriba = -caida * (a ** 1.7) + 0.0015 * (1 - a)             # el borde de arriba acompaña la base de la nariz y baja
    alto = alto_punta + (alto_centro - alto_punta) * (1 - a ** 1.1)
    return x, z_arriba, z_arriba - alto


def bigote_masa(cuerpo, mat, ojo_z, ancho=0.037, alto_centro=0.0135, alto_punta=0.0040, caida=0.0115, grosor=0.0095,
                nu=26, nv=7, muesca=0.0022):
    bm0 = malla_cabeza(cuerpo, ojo_z - 0.14)
    tree = BVHTree.FromBMesh(bm0)
    z0 = ojo_z - 0.0510
    bm = bmesh.new()
    filas = []
    for j in range(nv):
        v = j / (nv - 1)                       # 0 arriba, 1 abajo
        fila = []
        for i in range(nu):
            u = -1 + 2 * i / (nu - 1)
            x, za, zb = _perfil(u, ancho, alto_centro, alto_punta, caida)
            z = z0 + za + (zb - za) * v
            hit = tree.ray_cast(Vector((x, -0.5, z)), Vector((0, 1, 0)))
            if hit[0] is None:
                fila.append(None); continue
            a = abs(u)
            # grosor: máximo en el centro y en la parte de arriba (donde nace), afinándose en puntas y borde inferior
            borde = math.sin(math.pi * min(1.0, max(0.0, 0.18 + 0.82 * (1 - v)))) ** 0.7
            t = 0.0012 + grosor * (1 - a ** 1.6) * (0.35 + 0.65 * borde) + 0.0008
            # muesca del centro (filtrum): el bigote se parte apenas bajo la nariz
            if a < 0.07:
                t *= 0.55
            p = hit[0] + hit[1] * t
            fila.append(bm.verts.new(p))
        filas.append(fila)
    for j in range(nv - 1):
        for i in range(nu - 1):
            vs = (filas[j][i], filas[j][i + 1], filas[j + 1][i + 1], filas[j + 1][i])
            if all(vs):
                bm.faces.new(vs)
    bm0.free()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("bigote"); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("bigote", me); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    for p in me.polygons: p.use_smooth = True
    sol = ob.modifiers.new("grosor", 'SOLIDIFY'); sol.thickness = 0.0045; sol.offset = -1.0; sol.use_rim = True
    sub = ob.modifiers.new("sub", 'SUBSURF'); sub.levels = 2; sub.render_levels = 2
    ob.matrix_world = cuerpo.matrix_world
    return ob


def material_bigote(color=(0.040, 0.027, 0.019)):
    """Pelo del bigote: vetas finas horizontales que caen hacia las puntas, con brillo suave."""
    m = bpy.data.materials.new("pelo_bigote")
    m.use_nodes = True
    nt = m.node_tree; n = nt.nodes; l = nt.links
    p = n["Principled BSDF"]
    coord = n.new("ShaderNodeTexCoord")
    onda = n.new("ShaderNodeTexWave"); onda.wave_type = 'BANDS'; onda.bands_direction = 'Z'
    onda.inputs["Scale"].default_value = 900; onda.inputs["Distortion"].default_value = 4.0; onda.inputs["Detail"].default_value = 3
    l.new(coord.outputs["Object"], onda.inputs["Vector"])
    ruido = n.new("ShaderNodeTexNoise"); ruido.inputs["Scale"].default_value = 700; ruido.inputs["Detail"].default_value = 4
    mp = n.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (0.05, 1.0, 1.0)
    l.new(coord.outputs["Object"], mp.inputs["Vector"]); l.new(mp.outputs["Vector"], ruido.inputs["Vector"])
    suma = n.new("ShaderNodeMath"); suma.operation = 'ADD'
    l.new(onda.outputs["Fac"], suma.inputs[0]); l.new(ruido.outputs["Fac"], suma.inputs[1])
    rb = n.new("ShaderNodeRGB"); rb.outputs[0].default_value = (color[0], color[1], color[2], 1)
    claro = n.new("ShaderNodeMix"); claro.data_type = 'RGBA'; claro.inputs[7].default_value = (0.16, 0.11, 0.075, 1)
    mul = n.new("ShaderNodeMath"); mul.operation = 'MULTIPLY'; mul.inputs[1].default_value = 0.22
    l.new(suma.outputs[0], mul.inputs[0]); l.new(mul.outputs[0], claro.inputs[0])
    l.new(rb.outputs[0], claro.inputs[6]); l.new(claro.outputs[2], p.inputs["Base Color"])
    bump = n.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 0.9; bump.inputs["Distance"].default_value = 0.0015
    l.new(suma.outputs[0], bump.inputs["Height"]); l.new(bump.outputs["Normal"], p.inputs["Normal"])
    p.inputs["Roughness"].default_value = 0.38
    return m
