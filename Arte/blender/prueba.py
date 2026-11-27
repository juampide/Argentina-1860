import bpy, sys, math
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.mesh.primitive_uv_sphere_add(radius=1, segments=32, ring_count=16)
obj = bpy.context.active_object
obj.modifiers.new("sub", 'SUBSURF').levels = 2
bpy.ops.object.shade_smooth()
bpy.ops.object.light_add(type='SUN', location=(3, -3, 5)); bpy.context.active_object.data.energy = 3
bpy.ops.object.camera_add(location=(0, -5, 0.5), rotation=(math.radians(90), 0, 0)); cam = bpy.context.active_object
bpy.context.scene.camera = cam
sc = bpy.context.scene
for eng in ('BLENDER_EEVEE', 'BLENDER_EEVEE_NEXT', 'CYCLES'):
    try:
        sc.render.engine = eng; print("motor:", eng); break
    except Exception as e:
        print("no", eng)
sc.render.resolution_x = 400; sc.render.resolution_y = 400
sc.render.filepath = "D:/Unity/blender/prueba.png"
bpy.ops.render.render(write_still=True)
print("listo")
