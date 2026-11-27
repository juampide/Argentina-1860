"""Pasa una animación de Mixamo (esqueleto estándar mixamorig:) al esqueleto de Obregón y exporta un FBX con la animación.
Uso: blender -b -P retarget_mixamo.py -- <animacion_mixamo.fbx> <nombre_clip> <salida.fbx>
Método: por cada hueso se toma la DIRECCIÓN del hueso equivalente de Mixamo y se gira el nuestro para apuntar igual (swing), sin depender de que
los reposos (brazos caídos vs T) coincidan. La cadera copia el movimiento escalado a la altura de Obregón.
Nuestros lados están espejados respecto de Mixamo: _R de Obregón queda del lado +X, igual que LeftXxx de Mixamo."""
import sys, bpy
from mathutils import Vector, Matrix

OBREGON = "D:/Claude Code/Rol Buenos Aires/Assets/Modelos/Personajes/Obregon.fbx"
argv = sys.argv[sys.argv.index("--") + 1:]
ANIM_FBX, CLIP, SALIDA = argv[0], argv[1], argv[2]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=OBREGON)
tgt = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
antes = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=ANIM_FBX)
nuevos = set(bpy.data.objects) - antes
src = [o for o in nuevos if o.type == 'ARMATURE'][0]
nombres_nuevos = [o.name for o in nuevos]
for o in list(nuevos):
    if o.type == 'MESH': bpy.data.objects.remove(o, do_unlink=True)

def mapa():
    m = {"Hips": "Hips", "Spine": "Spine1", "Chest": "Spine2", "Neck": "Neck", "Head": "Head"}
    for lado, mix in (("R", "Left"), ("L", "Right")):
        m[f"Shoulder_{lado}"] = f"{mix}Shoulder"; m[f"UpperArm_{lado}"] = f"{mix}Arm"
        m[f"LowerArm_{lado}"] = f"{mix}ForeArm"; m[f"Hand_{lado}"] = f"{mix}Hand"
        # los dedos no se pasan: los maneja el juego (AnimPersonaje.Mano); girar cada falange solo por dirección los dobla mal
        m[f"UpperLeg_{lado}"] = f"{mix}UpLeg"; m[f"LowerLeg_{lado}"] = f"{mix}Leg"
        m[f"Foot_{lado}"] = f"{mix}Foot"; m[f"Toes_{lado}"] = f"{mix}ToeBase"
    return {k: "mixamorig:" + v for k, v in m.items()}
MAPA = mapa()

# orden: padres antes que hijos
def profundidad(b):
    n = 0
    while b.parent: b = b.parent; n += 1
    return n
orden = sorted([b for b in tgt.data.bones if b.name in MAPA], key=profundidad)
faltan = [b.name for b in tgt.data.bones if b.name not in MAPA]
print("SIN MAPEAR:", faltan)

sc = bpy.context.scene
f0, f1 = int(bpy.data.actions[-1].frame_range[0]), int(bpy.data.actions[-1].frame_range[1])
# la animación de Mixamo es la que se acaba de importar
accion_src = [a for a in bpy.data.actions if a.users][-1]
sc.frame_set(f0)
src_h0 = (src.matrix_world @ src.pose.bones["mixamorig:Hips"].head)
escala = tgt.pose.bones["Hips"].head.z / max(1e-6, src_h0.z)
hips_rest = tgt.pose.bones["Hips"].head.copy()
print("ESCALA", escala, "frames", f0, f1)

# huesos propios: acción nueva
for pb in tgt.pose.bones: pb.rotation_mode = 'QUATERNION'
tgt.animation_data_create()
nueva = bpy.data.actions.new(CLIP)
tgt.animation_data.action = nueva
tgt.animation_data.action.use_fake_user = True

def dir_mundo_src(nombre):
    pb = src.pose.bones[nombre]
    return (src.matrix_world.to_3x3() @ (pb.tail - pb.head)).normalized()

rest_mat = {b.name: tgt.matrix_world @ b.matrix_local for b in tgt.data.bones}
rest_dir = {n: (m.to_3x3() @ Vector((0, 1, 0))).normalized() for n, m in rest_mat.items()}

grabado = {}
for f in range(f0, f1 + 1):
    sc.frame_set(f)
    posadas = {}
    for b in orden:
        s = MAPA[b.name]
        d = dir_mundo_src(s)
        R = rest_dir[b.name].rotation_difference(d).to_matrix()
        rot = R @ rest_mat[b.name].to_3x3()
        if b.parent is None or b.parent.name not in posadas:
            if b.name == "Hips":
                pos_s = src.matrix_world @ src.pose.bones[s].head
                pos = hips_rest + Vector(((pos_s.x - src_h0.x) * escala, (pos_s.y - src_h0.y) * escala, (pos_s.z - src_h0.z) * escala))
            else:
                pos = rest_mat[b.name].translation
        else:
            padre = posadas[b.parent.name]
            local = b.parent.matrix_local.inverted() @ b.matrix_local
            pos = (padre @ Matrix.Translation(local.translation)).translation if False else (padre @ local).translation
        M = Matrix.Translation(pos) @ rot.to_4x4()
        posadas[b.name] = M
        tgt.pose.bones[b.name].matrix = tgt.matrix_world.inverted() @ M
    grabado[f] = {pb.name: (pb.rotation_quaternion.copy(), pb.location.copy()) for pb in tgt.pose.bones if pb.name in MAPA}
    bpy.context.view_layer.update()

# bucle sin salto: los últimos cuadros se funden hacia la pose del primero (si no, al repetir el clip el personaje da un tirón)
K = min(24, (f1 - f0) // 3)
for f in range(f1 - K, f1 + 1):
    w = (f - (f1 - K)) / K; w = w * w * (3 - 2 * w)
    for n, (q, loc) in grabado[f].items():
        q0, l0 = grabado[f0][n]
        grabado[f][n] = (q.slerp(q0, w), loc.lerp(l0, w))
for f in range(f0, f1 + 1):
    for n, (q, loc) in grabado[f].items():
        pb = tgt.pose.bones[n]; pb.rotation_quaternion = q; pb.location = loc
        pb.keyframe_insert("rotation_quaternion", frame=f)
        if n == "Hips": pb.keyframe_insert("location", frame=f)

# FBX: la armadura de Obregón con sus mallas y la animación horneada
sc.frame_start, sc.frame_end = f0, f1
sc.render.fps = 30
# afuera todo lo de Mixamo
for n in nombres_nuevos:
    if n in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[n], do_unlink=True)
bpy.ops.object.select_all(action='SELECT')
SOLO_HUESOS = len(argv) > 3 and argv[3] == "huesos"
if SOLO_HUESOS:
    for o in list(bpy.data.objects):
        if o.type == 'MESH': bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=SALIDA, use_selection=True, object_types={'ARMATURE'} if SOLO_HUESOS else {'ARMATURE', 'MESH'}, global_scale=1.0, apply_unit_scale=True,
                         apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, axis_forward='-Z', axis_up='Y',
                         use_mesh_modifiers=False, mesh_smooth_type='FACE', add_leaf_bones=False, armature_nodetype='NULL',
                         bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
                         bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=False)
print("EXPORTADO", SALIDA)
