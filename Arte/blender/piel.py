"""Material de piel por nodos para la cara de la base humana: color por zonas (rubor, ojeras, sombra de barba, labios),
poros y arrugas en relieve, brillo graso en nariz y frente, y dispersión bajo la piel. Todo en coordenadas de objeto del cuerpo base."""
import bpy, math
from mathutils import Vector


def _gauss(nt, coord, c, s, simetrico=True):
    """Máscara 0..1 gaussiana alrededor de c (x,y,z) con anchos s. simetrico: usa |x| (espejo)."""
    n = nt.nodes
    l = nt.links
    sub = n.new("ShaderNodeVectorMath")
    sub.operation = 'SUBTRACT'
    if simetrico:
        absn = n.new("ShaderNodeVectorMath")
        absn.operation = 'ABSOLUTE'
        l.new(coord, absn.inputs[0])
        sub.inputs[1].default_value = (abs(c[0]), c[1], c[2])
        l.new(absn.outputs[0], sub.inputs[0])
    else:
        sub.inputs[1].default_value = (c[0], c[1], c[2])
        l.new(coord, sub.inputs[0])
    sep = n.new("ShaderNodeSeparateXYZ")
    l.new(sub.outputs[0], sep.inputs[0])
    terminos = []
    for eje, sg in zip(("X", "Y", "Z"), s):
        d = n.new("ShaderNodeMath")
        d.operation = 'DIVIDE'
        d.inputs[1].default_value = sg
        l.new(sep.outputs[eje], d.inputs[0])
        q = n.new("ShaderNodeMath")
        q.operation = 'POWER'
        q.inputs[1].default_value = 2.0
        l.new(d.outputs[0], q.inputs[0])
        terminos.append(q)
    a1 = n.new("ShaderNodeMath")
    a1.operation = 'ADD'
    l.new(terminos[0].outputs[0], a1.inputs[0])
    l.new(terminos[1].outputs[0], a1.inputs[1])
    a2 = n.new("ShaderNodeMath")
    a2.operation = 'ADD'
    l.new(a1.outputs[0], a2.inputs[0])
    l.new(terminos[2].outputs[0], a2.inputs[1])
    ng = n.new("ShaderNodeMath")
    ng.operation = 'MULTIPLY'
    ng.inputs[1].default_value = -1.0
    l.new(a2.outputs[0], ng.inputs[0])
    ex = n.new("ShaderNodeMath")
    ex.operation = 'EXPONENT'
    l.new(ng.outputs[0], ex.inputs[0])
    return ex.outputs[0]


def _mezclar(nt, color_prev, color, factor_socket, fuerza):
    n = nt.nodes
    l = nt.links
    mx = n.new("ShaderNodeMix")
    mx.data_type = 'RGBA'
    mul = n.new("ShaderNodeMath")
    mul.operation = 'MULTIPLY'
    mul.inputs[1].default_value = fuerza
    l.new(factor_socket, mul.inputs[0])
    l.new(mul.outputs[0], mx.inputs[0])
    l.new(color_prev, mx.inputs[6])
    mx.inputs[7].default_value = (color[0], color[1], color[2], 1)
    return mx.outputs[2]


def _banda(nt, obj, escala, direccion, distorsion, mascara, peso, detalle=2):
    """Franjas de relieve (arrugas) en una dirección, visibles solo donde la máscara vale 1."""
    n = nt.nodes
    l = nt.links
    onda = n.new("ShaderNodeTexWave")
    onda.wave_type = 'BANDS'
    onda.bands_direction = direccion
    onda.inputs["Scale"].default_value = escala
    onda.inputs["Distortion"].default_value = distorsion
    onda.inputs["Detail"].default_value = detalle
    l.new(obj, onda.inputs["Vector"])
    a = n.new("ShaderNodeMath")
    a.operation = 'MULTIPLY'
    l.new(onda.outputs["Fac"], a.inputs[0])
    l.new(mascara, a.inputs[1])
    b = n.new("ShaderNodeMath")
    b.operation = 'MULTIPLY'
    b.inputs[1].default_value = peso
    l.new(a.outputs[0], b.inputs[0])
    return b.outputs[0]


def _sumar(nt, sockets):
    n = nt.nodes
    l = nt.links
    acc = sockets[0]
    for s_ in sockets[1:]:
        a = n.new("ShaderNodeMath")
        a.operation = 'ADD'
        l.new(acc, a.inputs[0])
        l.new(s_, a.inputs[1])
        acc = a.outputs[0]
    return acc


def piel_obregon(loc, base=(0.58, 0.38, 0.28)):
    """loc: punto medio entre los ojos en coordenadas locales del cuerpo."""
    m = bpy.data.materials.new("piel_obregon")
    m.use_nodes = True
    nt = m.node_tree
    n = nt.nodes
    l = nt.links
    p = n["Principled BSDF"]
    coord = n.new("ShaderNodeTexCoord")
    obj = coord.outputs["Object"]
    cy, cz = loc.y, loc.z
    # color base con variación suave de tono
    rb = n.new("ShaderNodeRGB")
    rb.outputs[0].default_value = (base[0], base[1], base[2], 1)
    ruido = n.new("ShaderNodeTexNoise")
    ruido.inputs["Scale"].default_value = 28
    ruido.inputs["Detail"].default_value = 5
    claro = n.new("ShaderNodeMix")
    claro.data_type = 'RGBA'
    claro.inputs[7].default_value = (0.74, 0.52, 0.40, 1)
    l.new(rb.outputs[0], claro.inputs[6])
    mulc = n.new("ShaderNodeMath")
    mulc.operation = 'MULTIPLY'
    mulc.inputs[1].default_value = 0.35
    l.new(ruido.outputs["Fac"], mulc.inputs[0])
    l.new(mulc.outputs[0], claro.inputs[0])
    col = claro.outputs[2]
    rojo = (0.78, 0.36, 0.30)
    col = _mezclar(nt, col, rojo, _gauss(nt, obj, (0.055, cy - 0.010, cz - 0.030), (0.030, 0.040, 0.030)), 0.75)             # mejillas
    col = _mezclar(nt, col, rojo, _gauss(nt, obj, (0.0, cy - 0.012, cz - 0.040), (0.014, 0.024, 0.022), False), 0.75)         # nariz
    col = _mezclar(nt, col, rojo, _gauss(nt, obj, (0.077, cy + 0.020, cz - 0.030), (0.022, 0.028, 0.032)), 0.70)             # orejas
    col = _mezclar(nt, col, (0.42, 0.27, 0.27), _gauss(nt, obj, (0.033, cy - 0.010, cz - 0.014), (0.024, 0.026, 0.009)), 0.85)  # ojeras
    col = _mezclar(nt, col, (0.45, 0.33, 0.30), _gauss(nt, obj, (0.034, cy - 0.012, cz + 0.014), (0.028, 0.026, 0.008)), 0.45)  # párpados
    col = _mezclar(nt, col, (0.30, 0.27, 0.28), _gauss(nt, obj, (0.0, cy, cz - 0.100), (0.056, 0.060, 0.032), False), 0.60)      # sombra de barba
    col = _mezclar(nt, col, (0.30, 0.27, 0.28), _gauss(nt, obj, (0.0, cy - 0.010, cz - 0.050), (0.040, 0.030, 0.012), False), 0.25)  # bajo la nariz
    col = _mezclar(nt, col, (0.55, 0.26, 0.25), _gauss(nt, obj, (0.0, cy - 0.012, cz - 0.067), (0.018, 0.020, 0.0055), False), 0.85)   # labios
    # pecas y manchas pequeñas
    pecas = n.new("ShaderNodeTexNoise")
    pecas.inputs["Scale"].default_value = 420
    pecas.inputs["Detail"].default_value = 2
    rampa = n.new("ShaderNodeValToRGB")
    rampa.color_ramp.elements[0].position = 0.60
    rampa.color_ramp.elements[1].position = 0.70
    l.new(pecas.outputs["Fac"], rampa.inputs["Fac"])
    sep_r = n.new("ShaderNodeSeparateColor")
    l.new(rampa.outputs["Color"], sep_r.inputs[0])
    col = _mezclar(nt, col, (0.38, 0.22, 0.16), sep_r.outputs[0], 0.30)
    # barba incipiente: puntitos oscuros dentro de la zona de barba
    punt = n.new("ShaderNodeTexNoise")
    punt.inputs["Scale"].default_value = 1900
    punt.inputs["Detail"].default_value = 1
    rp = n.new("ShaderNodeValToRGB")
    rp.color_ramp.elements[0].position = 0.52
    rp.color_ramp.elements[1].position = 0.64
    l.new(punt.outputs["Fac"], rp.inputs["Fac"])
    sp = n.new("ShaderNodeSeparateColor")
    l.new(rp.outputs["Color"], sp.inputs[0])
    mb = _gauss(nt, obj, (0.0, cy - 0.004, cz - 0.098), (0.050, 0.060, 0.034), False)
    barba = n.new("ShaderNodeMath")
    barba.operation = 'MULTIPLY'
    l.new(sp.outputs[0], barba.inputs[0])
    l.new(mb, barba.inputs[1])
    col = _mezclar(nt, col, (0.16, 0.14, 0.15), barba.outputs[0], 0.80)
    # capilares rotos en mejillas y nariz
    cap = n.new("ShaderNodeTexNoise")
    cap.inputs["Scale"].default_value = 150
    cap.inputs["Detail"].default_value = 8
    cap.inputs["Distortion"].default_value = 1.5
    rc = n.new("ShaderNodeValToRGB")
    rc.color_ramp.elements[0].position = 0.56
    rc.color_ramp.elements[1].position = 0.68
    l.new(cap.outputs["Fac"], rc.inputs["Fac"])
    sc = n.new("ShaderNodeSeparateColor")
    l.new(rc.outputs["Color"], sc.inputs[0])
    mc = _gauss(nt, obj, (0.052, cy - 0.010, cz - 0.034), (0.030, 0.04, 0.030))
    capm = n.new("ShaderNodeMath")
    capm.operation = 'MULTIPLY'
    l.new(sc.outputs[0], capm.inputs[0])
    l.new(mc, capm.inputs[1])
    col = _mezclar(nt, col, (0.55, 0.16, 0.16), capm.outputs[0], 0.55)
    l.new(col, p.inputs["Base Color"])
    # relieve: poros, grano y arrugas por zona (frente, patas de gallo, bolsas, entrecejo, labios, cuello)
    voro = n.new("ShaderNodeTexVoronoi")
    voro.feature = 'F1'
    voro.inputs["Scale"].default_value = 1500
    poro = n.new("ShaderNodeMath")
    poro.operation = 'MULTIPLY'
    poro.inputs[1].default_value = 0.35
    l.new(voro.outputs["Distance"], poro.inputs[0])
    fino = n.new("ShaderNodeTexNoise")
    fino.inputs["Scale"].default_value = 260
    fino.inputs["Detail"].default_value = 6
    f2 = n.new("ShaderNodeMath")
    f2.operation = 'MULTIPLY'
    f2.inputs[1].default_value = 0.30
    l.new(fino.outputs["Fac"], f2.inputs[0])
    ondu = n.new("ShaderNodeTexNoise")
    ondu.inputs["Scale"].default_value = 55
    ondu.inputs["Detail"].default_value = 3
    o2 = n.new("ShaderNodeMath")
    o2.operation = 'MULTIPLY'
    o2.inputs[1].default_value = 0.35
    l.new(ondu.outputs["Fac"], o2.inputs[0])
    terminos = [poro.outputs[0], f2.outputs[0], o2.outputs[0]]
    terminos.append(_banda(nt, obj, 70, 'Z', 6.0, _gauss(nt, obj, (0.0, cy - 0.012, cz + 0.058), (0.050, 0.040, 0.030), False), 0.55, 4))     # frente
    terminos.append(_banda(nt, obj, 190, 'DIAGONAL', 1.4, _gauss(nt, obj, (0.058, cy - 0.008, cz - 0.002), (0.013, 0.022, 0.013)), 0.9))  # patas de gallo
    terminos.append(_banda(nt, obj, 260, 'Z', 1.2, _gauss(nt, obj, (0.033, cy - 0.012, cz - 0.019), (0.024, 0.022, 0.006)), 0.85))        # bolsas
    terminos.append(_banda(nt, obj, 250, 'X', 1.2, _gauss(nt, obj, (0.007, cy - 0.014, cz + 0.026), (0.012, 0.02, 0.010)), 0.9))          # entrecejo
    terminos.append(_banda(nt, obj, 55, 'Z', 7.0, _gauss(nt, obj, (0.0, cy + 0.03, cz - 0.15), (0.055, 0.06, 0.035), False), 0.2, 4))        # cuello
    terminos.append(_banda(nt, obj, 600, 'X', 0.8, _gauss(nt, obj, (0.0, cy - 0.012, cz - 0.067), (0.016, 0.02, 0.007), False), 0.5))     # labios
    altura = _sumar(nt, terminos)
    bump = n.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.6
    bump.inputs["Distance"].default_value = 0.0012
    l.new(altura, bump.inputs["Height"])
    l.new(bump.outputs["Normal"], p.inputs["Normal"])
    # brillo graso en nariz y frente, mate en el resto; dispersión bajo la piel
    t_zone = _gauss(nt, obj, (0.0, cy - 0.012, cz - 0.030), (0.030, 0.030, 0.050), False)
    graso = n.new("ShaderNodeMath")
    graso.operation = 'MULTIPLY'
    graso.inputs[1].default_value = -0.22
    l.new(t_zone, graso.inputs[0])
    rug = n.new("ShaderNodeMath")
    rug.operation = 'ADD'
    rug.inputs[1].default_value = 0.52
    l.new(graso.outputs[0], rug.inputs[0])
    l.new(rug.outputs[0], p.inputs["Roughness"])
    if "Subsurface Weight" in p.inputs:
        p.inputs["Subsurface Weight"].default_value = 0.15
        p.inputs["Subsurface Radius"].default_value = (1.0, 0.35, 0.22)
        p.inputs["Subsurface Scale"].default_value = 0.012
    return m


def pelo_textura(color=(0.06, 0.04, 0.028)):
    """Pelo con vetas que siguen la dirección frente-nuca (eje Y), brillo suave y variación de tono."""
    m = bpy.data.materials.new("pelo_obregon")
    m.use_nodes = True
    nt = m.node_tree
    n = nt.nodes
    l = nt.links
    p = n["Principled BSDF"]
    coord = n.new("ShaderNodeTexCoord")
    mp = n.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1.0, 0.04, 1.0)
    l.new(coord.outputs["Object"], mp.inputs["Vector"])
    veta = n.new("ShaderNodeTexNoise")
    veta.inputs["Scale"].default_value = 520
    veta.inputs["Detail"].default_value = 3
    l.new(mp.outputs["Vector"], veta.inputs["Vector"])
    rb = n.new("ShaderNodeRGB")
    rb.outputs[0].default_value = (color[0], color[1], color[2], 1)
    claro = n.new("ShaderNodeMix")
    claro.data_type = 'RGBA'
    claro.inputs[7].default_value = (0.20, 0.14, 0.09, 1)
    mul = n.new("ShaderNodeMath")
    mul.operation = 'MULTIPLY'
    mul.inputs[1].default_value = 0.85
    l.new(veta.outputs["Fac"], mul.inputs[0])
    l.new(mul.outputs[0], claro.inputs[0])
    l.new(rb.outputs[0], claro.inputs[6])
    l.new(claro.outputs[2], p.inputs["Base Color"])
    bump = n.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.55
    bump.inputs["Distance"].default_value = 0.002
    l.new(veta.outputs["Fac"], bump.inputs["Height"])
    l.new(bump.outputs["Normal"], p.inputs["Normal"])
    p.inputs["Roughness"].default_value = 0.42
    return m
