"""Exporta al personaje armado en la escena a un FBX para Unity, con esqueleto: junta las piezas por (material, hueso), aplica los modificadores
(suavizado, grosor, escultura), reparte pesos y centra todo en el origen. Piel y ropa se deforman con varios huesos; el resto (pelo, kepí,
botones, botines...) va atado rígido a un hueso. Convención FBX: Y arriba, frente hacia +Z de Unity."""
import bpy, bmesh, os
from mathutils import Vector, Matrix
import sys
import rig, hornear, expresiones

RUTA_TEX = "D:/Claude Code/Rol Buenos Aires/Assets/Resources/Personajes"
# grupo -> (material de Blender a hornear, tamaño de la imagen, radio del filtro de pintura)
HORNEAR = {"piel": ("piel_obregon", 2048, 2, 1.55), "pelo": ("pelo_obregon", 1024, 1, 1.0), "bigote": ("pelo_bigote", 512, 1, 1.0)}


def _oculta(c):
    """Partes del cuerpo que la ropa tapa del todo: torso, piernas y brazos (quedan cabeza, cuello y manos)."""
    return c.z < 1.385 and not (0.62 < c.z < 0.93 and abs(c.x) > 0.21)

GRUPOS = [
    ("pelo", "pelo"), ("bigote", "bigote"),
    ("iris", "ojo_iris"), ("globo", "ojo_blanco"), ("eye", "ojo_blanco"),
    ("kepi_cinta", "oro"), ("kepi_escudo", "oro"), ("kepi_visera", "cuero_negro"), ("kepi", "saco"),
    ("boton", "oro"), ("hebilla", "oro"), ("hombrera", "oro"), ("insignia", "oro"),
    ("cartuchera", "oliva"), ("correaje", "cuero"), ("cinturon", "cuero"),
    ("puno", "pano_negro"), ("cuello", "pano_negro"),
    ("manga", "saco"), ("saco", "saco"),
    ("pierna", "pantalon"), ("cadera", "pantalon"),
    ("cana", "cuero_negro"), ("empeine", "cuero_negro"), ("puntera", "cuero_negro"), ("suela", "cuero_negro"), ("tacon", "cuero_negro"), ("zapato", "cuero_negro"),
    ("GEO-body", "piel"),
]
COLORES = {
    "piel": (0.76, 0.56, 0.44), "pelo": (0.17, 0.11, 0.075), "bigote": (0.11, 0.075, 0.05),
    "ojo_iris": (0.27, 0.15, 0.08), "ojo_blanco": (0.90, 0.87, 0.80),
    "manos": (0.86, 0.70, 0.60), "saco": (0.30, 0.45, 0.70), "pantalon": (0.36, 0.50, 0.74), "pano_negro": (0.08, 0.085, 0.10),
    "cuero": (0.48, 0.28, 0.14), "cuero_negro": (0.08, 0.07, 0.065), "oliva": (0.42, 0.42, 0.24), "oro": (0.80, 0.62, 0.20),
}
# partes que van atadas rígido a un hueso (el resto de la piel y la ropa se reparte entre varios huesos). {l} = lado R/L según la posición X
RIGIDOS = [
    ("pelo", "Head"), ("bigote", "Head"), ("iris", "Head"), ("globo", "Head"), ("eye", "Head"), ("kepi", "Head"),
    ("boton_puno", "LowerArm_{l}"), ("puno", "LowerArm_{l}"),
    ("boton_cartuchera", "Chest"), ("cartuchera", "Chest"), ("boton", "Chest"), ("insignia", "Neck"), ("cuello", "Neck"),
    ("correaje", "Chest"), ("hombrera", "Chest"), ("hebilla", "Spine"), ("cinturon", "Spine"),
    ("cana", "Foot_{l}"), ("empeine", "Foot_{l}"), ("puntera", "Foot_{l}"), ("suela", "Foot_{l}"), ("tacon", "Foot_{l}"), ("zapato", "Foot_{l}"),
]


# qué huesos pueden mover cada pieza deformable: así el pecho del saco no se va con el brazo cuando este sube
DEDOS = {f"{d}_{k}_{l}" for d in ("Thumb", "Index", "Middle", "Ring", "Pinky") for k in (1, 2, 3) for l in ("R", "L")}
BRAZOS = {"UpperArm_R", "UpperArm_L", "LowerArm_R", "LowerArm_L", "Hand_R", "Hand_L"}
PIERNAS = {"UpperLeg_R", "UpperLeg_L", "LowerLeg_R", "LowerLeg_L", "Foot_R", "Foot_L", "Toes_R", "Toes_L"}
PERMITIDOS = {
    "manga": BRAZOS | {"Chest", "Shoulder_R", "Shoulder_L"},
    "torso": {"Hips", "Spine", "Chest", "Neck", "Shoulder_R", "Shoulder_L"},
    "piernas": PIERNAS | {"Hips", "Spine"},
    "piel": {"Chest", "Neck", "Head"},
    "manos": BRAZOS | DEDOS,
}


def _sub(nombre, hueso):
    """Subgrupo de una pieza deformable (determina qué huesos la pueden mover)."""
    if hueso:
        return None
    if "manga" in nombre:
        return "manga"
    if "saco" in nombre:
        return "torso"
    if "pierna" in nombre or "cadera" in nombre:
        return "piernas"
    if "GEO-body" in nombre:
        return "piel"
    return None


def _grupo_de(nombre):
    for clave, g in GRUPOS:
        if clave in nombre:
            return g
    return None


def _hueso_rigido(nombre, cx):
    for clave, hueso in RIGIDOS:
        if clave in nombre:
            return hueso.replace("{l}", "R" if cx >= 0 else "L")
    return None


def _material(nombre, rgb):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (rgb[0] ** 2.2, rgb[1] ** 2.2, rgb[2] ** 2.2, 1)
    p.inputs["Roughness"].default_value = 0.8
    return m


def exportar(cuerpo, ruta, nivel_cuerpo=1, nombre="Obregon"):
    for m in cuerpo.modifiers:
        if m.type == 'MULTIRES':
            m.levels = nivel_cuerpo; m.render_levels = nivel_cuerpo
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    origen = cuerpo.matrix_world.translation.copy()

    # esqueleto (en coordenadas locales del cuerpo, que son las de la exportación)
    arm, geom = rig.crear(cuerpo, nombre + "_Esqueleto")

    por_clave = {}   # (grupo, hueso o None) -> lista de mallas
    for ob in list(bpy.data.objects):
        if ob.type != 'MESH' or ob.hide_render or ob.name.startswith(nombre):
            continue
        g = _grupo_de(ob.name)
        if g is None:
            continue
        ev = ob.evaluated_get(dg)
        me = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=False, depsgraph=dg)
        me.transform(ob.matrix_world)
        me.transform(Matrix.Translation(-origen))
        cx = sum(v.co.x for v in me.vertices) / max(1, len(me.vertices))
        hueso = _hueso_rigido(ob.name, cx)
        por_clave.setdefault((g, hueso, _sub(ob.name, hueso)), []).append(me)

    nuevos = []
    materiales = {}
    def material_de(g):
        if g not in materiales:
            materiales[g] = _material(f"{nombre}_{g}", COLORES.get(g, (0.5, 0.5, 0.5)))
        return materiales[g]

    for (g, hueso, sub), mallas in por_clave.items():
        bm = bmesh.new()
        for me in mallas:
            bm.from_mesh(me)
        if g == "piel":
            bmesh.ops.delete(bm, geom=[fc for fc in bm.faces if _oculta(fc.calc_center_median())], context='FACES')
            bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
            # las manos van como pieza aparte, con color liso; la textura horneada es solo de la cabeza y el cuello
            bm_manos = bm.copy()
            bmesh.ops.delete(bm, geom=[fc for fc in bm.faces if fc.calc_center_median().z < 1.385], context='FACES')
            bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
            bmesh.ops.delete(bm_manos, geom=[fc for fc in bm_manos.faces if fc.calc_center_median().z >= 1.385], context='FACES')
            bmesh.ops.delete(bm_manos, geom=[v for v in bm_manos.verts if not v.link_faces], context='VERTS')
            if bm_manos.faces:
                rm = bpy.data.meshes.new(f"{nombre}_manos")
                bm_manos.to_mesh(rm)
                for p in rm.polygons: p.use_smooth = True
                rm.materials.append(material_de("manos"))
                om = bpy.data.objects.new(f"{nombre}_manos", rm)
                bpy.context.collection.objects.link(om)
                rig.pesar_por_distancia(om, geom, permitidos=PERMITIDOS["manos"], sigma=0.012)
                rig.enlazar_armadura(om, arm)
                nuevos.append(om)
            bm_manos.free()
        if g == "pelo":
            # relaja los picos que deja el grosor en las esquinas del borde del pelo (se veían como cuernitos en las sienes)
            for _ in range(4):
                bmesh.ops.smooth_vert(bm, verts=bm.verts, factor=0.6, use_axis_x=True, use_axis_y=True, use_axis_z=True)
        etiqueta = f"{nombre}_{g}" + (f"__{hueso}" if hueso else "") + (f"_{sub}" if sub else "")
        res = bpy.data.meshes.new(etiqueta)
        bm.to_mesh(res); bm.free()
        for p in res.polygons: p.use_smooth = True
        res.materials.append(material_de(g))
        o = bpy.data.objects.new(etiqueta, res)
        bpy.context.collection.objects.link(o)
        if "--hornear" in sys.argv and g in HORNEAR:
            mat_origen, tam, pint, sat = HORNEAR[g]
            mo = bpy.data.materials.get(mat_origen)
            if mo is not None:
                os.makedirs(RUTA_TEX, exist_ok=True)
                hornear.hornear(o, mo, f"{RUTA_TEX}/{nombre}_{g}.png", tam, pintura=pint, saturacion=sat, uv_propio=hornear.uv_cabeza_y_manos if g == "piel" else None)
        if g == "piel" and not hueso:
            oi, od = bpy.data.objects.get("GEO-body_male_realistic.eye.L"), bpy.data.objects.get("GEO-body_male_realistic.eye.R")
            if oi and od:
                ojo = (oi.matrix_world.translation + od.matrix_world.translation) / 2 - origen
                expresiones.agregar(o, ojo)
        if hueso:
            rig.atar_rigido(o, hueso)
        else:
            rig.pesar_por_distancia(o, geom, permitidos=PERMITIDOS.get(sub))
        rig.enlazar_armadura(o, arm)
        nuevos.append(o)

    bpy.ops.object.select_all(action='DESELECT')
    for o in nuevos + [arm]:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=ruta, use_selection=True, object_types={'ARMATURE', 'MESH'}, global_scale=1.0, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, axis_forward='-Z', axis_up='Y',
                             use_mesh_modifiers=False, mesh_smooth_type='FACE', add_leaf_bones=False, armature_nodetype='NULL',
                             bake_anim=False, path_mode='COPY', embed_textures=False)
    tris = sum(len(o.data.polygons) for o in nuevos)
    print(f"EXPORTADO {ruta}: {len(nuevos)} piezas, {len(geom)} huesos, {tris} polígonos")
    return nuevos
