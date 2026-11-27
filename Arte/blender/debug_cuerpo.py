import sys, os, bpy
sys.path.insert(0, os.path.dirname(__file__))
import obregon_cuerpo as oc
oc.construir()
for o in bpy.data.objects:
    if o.type == 'MESH':
        dg = bpy.context.evaluated_depsgraph_get(); ev = o.evaluated_get(dg)
        print("OBJ", o.name, "render_oculto=", o.hide_render, "viewport_oculto=", o.hide_viewport, "verts_eval=", len(ev.data.vertices), "loc=", tuple(round(x,2) for x in o.location))
