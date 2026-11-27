import bpy
print("=== COLECCIONES ===")
for c in bpy.data.collections:
    print(" ", c.name, len(c.objects))
print("=== OBJETOS ===")
for o in bpy.data.objects:
    n = len(o.data.vertices) if o.type == 'MESH' else '-'
    mods = ",".join(f"{m.type}" + (f"(niv {m.total_levels})" if m.type == 'MULTIRES' else "") for m in o.modifiers)
    print(f"  {o.name:45s} {o.type:10s} verts={n} mods=[{mods}] dims={tuple(round(x,3) for x in o.dimensions)}")
print("=== MATERIALES ===")
for m in bpy.data.materials: print(" ", m.name)
