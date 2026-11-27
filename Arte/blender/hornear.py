"""Horneado de materiales de Blender a imágenes para Unity, con un filtro de "pintura" (Kuwahara) opcional.
Se desenvuelve la malla en una hoja UV única y se hornea el color del material original (Cycles, solo color directo)."""
import bpy, math
import numpy as np


def kuwahara(rgb, radio=2):
    """Filtro de pintura: en cada píxel gana el cuadrante más parejo de la ventana (a mano alzada, sin detalle fino). rgb: (alto, ancho, 3) float."""
    h, w, _ = rgb.shape
    r = radio
    pad = np.pad(rgb, ((r, r), (r, r), (0, 0)), mode='edge')
    lum = pad @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    def integral(x):
        s = np.zeros((x.shape[0] + 1, x.shape[1] + 1) + x.shape[2:], dtype=np.float64)
        s[1:, 1:] = x.cumsum(0).cumsum(1)
        return s
    S_rgb = integral(pad.astype(np.float64)); S_l = integral(lum.astype(np.float64)); S_l2 = integral((lum ** 2).astype(np.float64))

    def ventana(S, dy0, dy1, dx0, dx1):
        # suma sobre dy en [dy0, dy1], dx en [dx0, dx1] alrededor de cada píxel original
        y0 = r + dy0; y1 = r + dy1 + 1; x0 = r + dx0; x1 = r + dx1 + 1
        return (S[y1:y1 + h, x1:x1 + w] - S[y0:y0 + h, x1:x1 + w] - S[y1:y1 + h, x0:x0 + w] + S[y0:y0 + h, x0:x0 + w])

    n = float((r + 1) ** 2)
    medias, varianzas = [], []
    for (dy0, dy1, dx0, dx1) in ((-r, 0, -r, 0), (-r, 0, 0, r), (0, r, -r, 0), (0, r, 0, r)):
        mc = ventana(S_rgb, dy0, dy1, dx0, dx1) / n
        ml = ventana(S_l, dy0, dy1, dx0, dx1) / n
        ml2 = ventana(S_l2, dy0, dy1, dx0, dx1) / n
        medias.append(mc); varianzas.append(np.abs(ml2 - ml * ml))
    var = np.stack(varianzas, 0)
    pesos = 1.0 / (1.0 + 4000.0 * var)
    pesos = pesos ** 2
    med = np.stack(medias, 0)
    res = (med * pesos[..., None]).sum(0) / pesos.sum(0)[..., None]
    return res.astype(np.float32)


def uv_cabeza_y_manos(ob, z_cabeza=1.385):
    """Una sola hoja UV para la piel que queda en el juego: la cabeza (y el cuello) desenvuelta como un cilindro continuo (la cara queda al centro,
    la costura cae en la nuca) en la mitad de arriba, y las dos manos con proyección plana en la de abajo."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    for capa in list(bm.loops.layers.uv):
        bm.loops.layers.uv.remove(capa)
    uv = bm.loops.layers.uv.new("UVMap")
    cab = [f for f in bm.faces if f.calc_center_median().z >= z_cabeza]
    zs = [v.co.z for f in cab for v in f.verts]
    zmin, zmax = (min(zs), max(zs)) if zs else (1.4, 1.7)
    ys = [v.co.y for f in cab for v in f.verts]
    cy = (min(ys) + max(ys)) / 2 if ys else 0.0
    for f in bm.faces:
        if f in cab:
            us = []
            for l in f.loops:
                p = l.vert.co
                us.append(math.atan2(p.x, -(p.y - cy)) / (2 * math.pi) + 0.5)
            ref = us[0]
            us = [u + (1.0 if ref - u > 0.5 else (-1.0 if u - ref > 0.5 else 0.0)) for u in us]   # continuidad en la costura
            for l, u in zip(f.loops, us):
                v = (l.vert.co.z - zmin) / max(1e-6, zmax - zmin)
                l[uv].uv = (u, 0.5 + 0.5 * v)
        else:
            for l in f.loops:
                p = l.vert.co
                u = ((abs(p.x) - 0.30) / 0.14) * 0.48 + (0.5 if p.x > 0 else 0.01)
                v = ((p.z - 0.66) / 0.30) * 0.46 + 0.02
                l[uv].uv = (u, v)
    bm.to_mesh(ob.data)
    bm.free()


def hornear(ob, material_origen, ruta_png, tam=2048, muestras=16, margen=8, pintura=0, desenvolver=True, uv_propio=None, saturacion=1.0):
    """Desenvuelve ob (si hace falta), hornea el color de material_origen en una imagen tam x tam y la guarda en ruta_png."""
    sc = bpy.context.scene
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    if uv_propio is not None:
        uv_propio(ob)
    elif desenvolver:
        for uvl in list(ob.data.uv_layers):
            ob.data.uv_layers.remove(uvl)
        ob.data.uv_layers.new(name="UVMap")
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.004, scale_to_bounds=True)
        bpy.ops.object.mode_set(mode='OBJECT')
    img = bpy.data.images.new(ob.name + "_hornear", tam, tam, alpha=False)
    nodos = material_origen.node_tree.nodes
    tex = nodos.new('ShaderNodeTexImage')
    tex.image = img
    nodos.active = tex
    tex.select = True
    anterior = list(ob.data.materials)
    ob.data.materials.clear()
    ob.data.materials.append(material_origen)
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = muestras
    try:
        sc.cycles.use_denoising = False
    except Exception:
        pass
    bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, margin=margen, use_clear=True)
    if pintura or saturacion != 1.0:
        px = np.array(img.pixels[:], dtype=np.float32).reshape(tam, tam, 4)
        if pintura:
            px[..., :3] = kuwahara(px[..., :3], pintura)
        if saturacion != 1.0:
            rgb = px[..., :3]
            lum = (rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32))[..., None]
            rgb = lum + (rgb - lum) * saturacion
            rgb = rgb * np.array([1.04, 0.99, 0.94], dtype=np.float32)      # un poco más cálido
            px[..., :3] = np.clip(rgb, 0.0, 1.0)
        img.pixels = px.flatten().tolist()
    img.filepath_raw = ruta_png
    img.file_format = 'PNG'
    img.save()
    nodos.remove(tex)
    ob.data.materials.clear()
    for m in anterior:
        ob.data.materials.append(m)
    print(f"HORNEADO {ruta_png} ({tam}x{tam}, pintura={pintura})")
    return img
