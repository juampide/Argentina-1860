"""Convierte la cara de la base humana (Human Base Meshes, CC0) en la del Teniente Obregón: flaco, cansado, de unos 40 años."""
import sys, os, math, bpy
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
from mathutils import Vector

def preparar():
    """Deja solo el cuerpo masculino realista y sus ojos; devuelve (cuerpo, multires, ojoL, ojoR)."""
    conservar = {"GEO-body_male_realistic", "GEO-body_male_realistic.eye.L", "GEO-body_male_realistic.eye.R"}
    for o in list(bpy.data.objects):
        if o.name not in conservar: bpy.data.objects.remove(o, do_unlink=True)
    cuerpo = bpy.data.objects["GEO-body_male_realistic"]
    mr = [m for m in cuerpo.modifiers if m.type == 'MULTIRES'][0]
    return cuerpo, mr, bpy.data.objects["GEO-body_male_realistic.eye.L"], bpy.data.objects["GEO-body_male_realistic.eye.R"]

LID_UP, LID_DN = 0.0055, 0.0025

def g(dx, dz, cx, cz, sx, sz):
    return math.exp(-(((dx - cx) / sx) ** 2) - (((dz - cz) / sz) ** 2))

def envejecer(cuerpo, ojoL, ojoR, flaco=1.0, cansancio=1.0):
    """Mueve la malla de base de la cara. y negativo = adelante. Unidades en metros."""
    loc = cuerpo.matrix_world.inverted() @ ((ojoL.matrix_world.translation + ojoR.matrix_world.translation) / 2)
    me = cuerpo.data
    tocados = 0
    for v in me.vertices:
        p = v.co
        dx, dy, dz = p.x - loc.x, p.y - loc.y, p.z - loc.z
        if abs(dx) > 0.11 or dz < -0.20 or dz > 0.13 or dy > 0.07: continue
        s = abs(dx)
        front = max(0.0, min(1.0, (-(dy) + 0.02) / 0.05))      # 1 en la superficie de la cara, 0 en la nuca
        adentro = 0.0       # desplazamiento hacia adentro (+y)
        lateral = 0.0       # desplazamiento hacia el centro (-x*signo)
        # mejillas hundidas
        adentro += 0.0105 * flaco * g(s, dz, 0.052, -0.052, 0.020, 0.024)
        # pómulos que sobresalen
        adentro -= 0.0050 * flaco * g(s, dz, 0.062, -0.018, 0.018, 0.016)
        # ceja marcada
        adentro -= 0.0050 * g(s, dz, 0.030, 0.030, 0.028, 0.009)
        adentro -= 0.0030 * g(s, dz, 0.0, 0.026, 0.020, 0.010)
        # cuencas más hondas y ojeras
        adentro += 0.0014 * cansancio * g(s, dz, 0.032, 0.008, 0.020, 0.014)
        adentro += 0.0010 * cansancio * g(s, dz, 0.030, -0.014, 0.022, 0.008)
        # surco nasolabial y comisuras
        adentro += 0.0035 * cansancio * g(s, dz, 0.032, -0.058, 0.007, 0.022)
        adentro += 0.0030 * cansancio * g(s, dz, 0.028, -0.080, 0.012, 0.010)
        # mandíbula y mentón más angostos
        estrecho = 0.075 * flaco * max(0.0, min(1.0, (-dz - 0.045) / 0.07))
        lateral = estrecho * s * 0.55
        # sien hundida
        adentro += 0.0030 * flaco * g(s, dz, 0.068, 0.030, 0.014, 0.020)
        d = Vector((0, 0, 0))
        d.z -= LID_UP * g(s, dz, 0.032, 0.010, 0.022, 0.0075) * min(1.0, front * 1.6)   # párpado superior caído
        d.z += LID_DN * g(s, dz, 0.032, -0.009, 0.022, 0.0065) * min(1.0, front * 1.6)  # párpado inferior subido
        d.y = adentro * front
        d.x = -math.copysign(lateral, dx) * front
        if d.length > 1e-7:
            v.co = p + d; tocados += 1
    return tocados

def iris(oL, oR):
    ir = material('iris', (0.16, 0.085, 0.045), 0.25)
    pu = material('pupila', (0.01, 0.008, 0.007), 0.2)
    for o in (oL, oR):
        c = o.matrix_world.translation
        esfera('iris', (c.x, c.y - 0.0105, c.z), (0.0066, 0.0016, 0.0066), ir, seg=24, anillos=8)
        esfera('pupila', (c.x, c.y - 0.0118, c.z), (0.0026, 0.0008, 0.0026), pu, seg=16, anillos=6)

def render_cara(cuerpo, mr, oL, oR, prefijo, vistas=('frente', 'tres', 'perfil')):
    ojo = (oL.matrix_world.translation + oR.matrix_world.translation) / 2
    sc = escena(); sc.render.film_transparent = False
    c = Vector((ojo.x, ojo.y, ojo.z - 0.045))
    luces(centro=tuple(c), escala=0.7)
    mr.levels = mr.total_levels; mr.render_levels = mr.total_levels
    pos = {'frente': (c.x, c.y - 0.85, c.z), 'tres': (c.x - 0.6, c.y - 0.6, c.z + 0.03), 'perfil': (c.x - 0.85, c.y, c.z)}
    for n in vistas:
        render(f'D:/Unity/blender/{prefijo}_{n}.png', camara(pos[n], tuple(c), 85), 700, 700)

if __name__ == '__main__':
    import pelo3
    cuerpo, mr, oL, oR = preparar()
    print('vertices movidos:', envejecer(cuerpo, oL, oR, flaco=2.2, cansancio=2.4))
    import piel as piel_mod
    ojo0 = (oL.matrix_world.translation + oR.matrix_world.translation) / 2
    loc0 = cuerpo.matrix_world.inverted() @ ojo0
    piel = piel_mod.piel_obregon(loc0)
    cuerpo.data.materials.clear(); cuerpo.data.materials.append(piel)
    for o in (oL, oR): o.location.y += 0.0035
    bpy.context.view_layer.update()
    iris(oL, oR)
    mr.levels = 2; mr.render_levels = 2
    ojo = (oL.matrix_world.translation + oR.matrix_world.translation) / 2
    loc = cuerpo.matrix_world.inverted() @ ojo
    mp = piel_mod.pelo_textura()
    centro_cab = Vector((loc.x, loc.y + 0.085, loc.z + 0.045))
    pelo3.pelo_masas(cuerpo, mp, centro_cab, grosor=0.0105, copete=0.024)
    import bigote as bigote_mod
    bigote_mod.bigote_masa(cuerpo, bigote_mod.material_bigote(), loc.z)
    render_cara(cuerpo, mr, oL, oR, 'obregon')
    c = Vector((ojo.x, ojo.y, ojo.z))
    render('D:/Unity/blender/obregon_ojos.png', camara((c.x, c.y - 0.34, c.z + 0.01), tuple(c), 85), 900, 450)
    print('OK')
