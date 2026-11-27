"""Herramientas comunes para modelar personajes en Blender por código (Blender 5.x).
Convención: el personaje mira hacia -Y (frente de Blender), Z arriba, medidas en metros."""
import bpy, bmesh, math, random
from mathutils import Vector

def limpiar():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def sstep(a, b, x):
    if a == b: return 0.0
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def bump(x, z, cx, cz, sx, sz, h):
    return h * math.exp(-(((x - cx) / sx) ** 2) - (((z - cz) / sz) ** 2))

def material(nombre, color, rugosidad=0.7, subsuelo=0.0, metal=0.0):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Roughness"].default_value = rugosidad
    p.inputs["Metallic"].default_value = metal
    if subsuelo and "Subsurface Weight" in p.inputs:
        p.inputs["Subsurface Weight"].default_value = subsuelo
        p.inputs["Subsurface Radius"].default_value = (0.9, 0.4, 0.25)
        p.inputs["Subsurface Scale"].default_value = 0.01
    return m

def suciedad(m, fuerza=0.6, escala=6.0, tono=(0.25, 0.19, 0.13)):
    """Mezcla mugre procedural sobre el color base del material (se vería pintado/sucio)."""
    nt = m.node_tree; n = nt.nodes; l = nt.links
    p = n["Principled BSDF"]
    base = p.inputs["Base Color"].default_value
    coord = n.new("ShaderNodeTexCoord")
    ruido = n.new("ShaderNodeTexNoise"); ruido.inputs["Scale"].default_value = escala; ruido.inputs["Detail"].default_value = 8
    rampa = n.new("ShaderNodeValToRGB")
    rampa.color_ramp.elements[0].position = 0.42; rampa.color_ramp.elements[1].position = 0.75
    mezcla = n.new("ShaderNodeMix"); mezcla.data_type = 'RGBA'
    mezcla.inputs[6].default_value = base
    mezcla.inputs[7].default_value = (*tono, 1)
    mult = n.new("ShaderNodeMath"); mult.operation = 'MULTIPLY'; mult.inputs[1].default_value = fuerza
    l.new(coord.outputs["Object"], ruido.inputs["Vector"])
    l.new(ruido.outputs["Fac"], rampa.inputs["Fac"])
    l.new(rampa.outputs["Color"], mult.inputs[0])
    l.new(mult.outputs[0], mezcla.inputs[0])
    l.new(mezcla.outputs[2], p.inputs["Base Color"])

def a_objeto(nombre, bm, mat=None, subdiv=0, suave=True, padre=None):
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(nombre, me)
    bpy.context.collection.objects.link(ob)
    if mat: ob.data.materials.append(mat)
    if suave:
        for p in me.polygons: p.use_smooth = True
    if subdiv:
        mod = ob.modifiers.new("sub", 'SUBSURF'); mod.levels = subdiv; mod.render_levels = subdiv
    if padre: ob.parent = padre
    return ob

def esfera(nombre, centro, radios, mat, seg=24, anillos=14, subdiv=0, rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=anillos, radius=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * radios[0], v.co.y * radios[1], v.co.z * radios[2]))
    ob = a_objeto(nombre, bm, mat, subdiv)
    ob.location = centro; ob.rotation_euler = rot
    return ob

def loft(nombre, anillos, mat, lados=16, subdiv=1, cerrar_arriba=True, cerrar_abajo=True, suave=True):
    """anillos: lista de (centro(x,y,z), rx, ry[, giro]) de abajo hacia arriba. Une los anillos en una superficie continua."""
    bm = bmesh.new()
    filas = []
    for a in anillos:
        c, rx, ry = a[0], a[1], a[2]
        giro = a[3] if len(a) > 3 else 0.0
        fila = []
        for i in range(lados):
            t = i / lados * 2 * math.pi
            x = math.cos(t) * rx; y = math.sin(t) * ry
            if len(a) > 4:   # pliegues: lóbulos que abultan y hunden el anillo (amplitud, cantidad, fase)
                amp, nl, fase = a[4]
                k = 1.0 + amp * math.sin(nl * t + fase)
                x *= k; y *= k
            cg, sg = math.cos(giro), math.sin(giro)
            fila.append(bm.verts.new(Vector((c[0] + x * cg - y * sg, c[1] + x * sg + y * cg, c[2]))))
        filas.append(fila)
    for k in range(len(filas) - 1):
        for i in range(lados):
            bm.faces.new((filas[k][i], filas[k][(i + 1) % lados], filas[k + 1][(i + 1) % lados], filas[k + 1][i]))
    if cerrar_abajo: bm.faces.new(list(reversed(filas[0])))
    if cerrar_arriba: bm.faces.new(filas[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return a_objeto(nombre, bm, mat, subdiv, suave)

def escena(fondo=(0.16, 0.16, 0.17)):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    w = bpy.data.worlds.new("w"); w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (*fondo, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.5
    sc.world = w
    try: sc.view_settings.view_transform = 'Standard'
    except Exception: pass
    return sc

def luces(centro=(0, 0, 1.0), escala=1.0):
    def foco(nombre, pos, energia, color, tam):
        d = bpy.data.lights.new(nombre, 'AREA'); d.energy = energia; d.color = color; d.size = tam
        o = bpy.data.objects.new(nombre, d); bpy.context.collection.objects.link(o)
        o.location = Vector(pos) * escala + Vector(centro)
        dirv = Vector(centro) - o.location
        o.rotation_euler = dirv.to_track_quat('-Z', 'Y').to_euler()
    foco("clave", (-1.6, -2.2, 1.6), 110 * escala * escala, (1.0, 0.88, 0.74), 1.6)
    foco("relleno", (2.0, -1.6, 0.6), 30 * escala * escala, (0.7, 0.8, 1.0), 2.0)
    foco("contra", (0.6, 2.2, 1.8), 70 * escala * escala, (1.0, 0.95, 0.9), 1.4)

def camara(pos, mira, focal=85):
    d = bpy.data.cameras.new("cam"); d.lens = focal
    o = bpy.data.objects.new("cam", d); bpy.context.collection.objects.link(o)
    o.location = pos
    o.rotation_euler = (Vector(mira) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()
    return o

def render(ruta, cam, ancho=800, alto=800, muestras=24):
    sc = bpy.context.scene
    sc.camera = cam
    sc.render.resolution_x = ancho; sc.render.resolution_y = alto
    try: sc.eevee.taa_render_samples = muestras
    except Exception: pass
    sc.render.filepath = ruta
    bpy.ops.render.render(write_still=True)


def tubo(nombre, puntos, radios, mat, lados=10, aplastar=1.0, subdiv=1):
    """Tubo sólido a lo largo de una lista de puntos, con radio distinto en cada punto (cónico). aplastar<1 lo achata en el eje de la normal."""
    bm = bmesh.new()
    anillos = []
    n = len(puntos)
    for k in range(n):
        p = Vector(puntos[k])
        t = (Vector(puntos[min(k + 1, n - 1)]) - Vector(puntos[max(k - 1, 0)]))
        if t.length < 1e-9: t = Vector((1, 0, 0))
        t.normalize()
        ref = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
        a = t.cross(ref).normalized(); b = t.cross(a).normalized()
        fila = []
        for i in range(lados):
            ang = i / lados * 2 * math.pi
            fila.append(bm.verts.new(p + a * (math.cos(ang) * radios[k]) + b * (math.sin(ang) * radios[k] * aplastar)))
        anillos.append(fila)
    for k in range(n - 1):
        for i in range(lados):
            bm.faces.new((anillos[k][i], anillos[k][(i + 1) % lados], anillos[k + 1][(i + 1) % lados], anillos[k + 1][i]))
    bm.faces.new(list(reversed(anillos[0]))); bm.faces.new(anillos[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return a_objeto(nombre, bm, mat, subdiv, True)
