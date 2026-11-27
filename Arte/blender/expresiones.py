"""Formas de la cara (shape keys / blendshapes) para el modelo de la cabeza: gestos de la ficha (Dura, Cansada, Amable, Triste, Enojado),
Hablar (abre la mandíbula) y Parpadeo. Se calculan con campanas gaussianas alrededor de los puntos de la cara, medidos desde el punto medio entre los ojos.
Convención: x lateral, y profundidad (frente = -Y), z arriba; metros."""
import math
from mathutils import Vector


def _g(dx, dz, cx, cz, sx, sz):
    return math.exp(-(((dx - cx) / sx) ** 2) - (((dz - cz) / sz) ** 2))


FUERZA = 1.7      # los gestos se exageran un poco: en el juego la cara se ve chica


def agregar(ob, ojo):
    """ob: malla de la cabeza (en coordenadas locales). ojo: punto medio entre los ojos (Vector). Crea Basis y las formas."""
    me = ob.data
    base = [v.co.copy() for v in me.vertices]
    ob.shape_key_add(name="Basis", from_mix=False)

    def delta_generico(p, f):
        dx, dy, dz = p.x - ojo.x, p.y - ojo.y, p.z - ojo.z
        if abs(dx) > 0.12 or dz < -0.16 or dz > 0.10 or dy > 0.04:
            return Vector((0, 0, 0))
        frente = max(0.0, min(1.0, (-dy + 0.02) / 0.05))
        lado = 1.0 if dx >= 0 else -1.0
        d = f(abs(dx), dz, lado)       # (desplazamiento x hacia afuera, y, z)
        return Vector((d[0] * lado, d[1], d[2])) * frente

    def crear(nombre, f):
        sk = ob.shape_key_add(name=nombre, from_mix=False)
        for i, p in enumerate(base):
            sk.data[i].co = p + delta_generico(p, f) * FUERZA
        sk.value = 0.0

    # --- partes comunes ---
    def ceja_interna(s, dz):  return _g(s, dz, 0.017, 0.030, 0.018, 0.017)
    def ceja_externa(s, dz):  return _g(s, dz, 0.046, 0.030, 0.020, 0.016)
    def parpado_sup(s, dz):   return _g(s, dz, 0.032, 0.010, 0.022, 0.0075)
    def parpado_inf(s, dz):   return _g(s, dz, 0.032, -0.009, 0.022, 0.0065)
    def comisura(s, dz):      return _g(s, dz, 0.028, -0.066, 0.012, 0.012)
    def pomulo(s, dz):        return _g(s, dz, 0.055, -0.030, 0.022, 0.022)
    def labio_inf(s, dz):     return _g(s, dz, 0.0, -0.075, 0.020, 0.008)
    def boca(s, dz):          return _g(s, dz, 0.0, -0.068, 0.034, 0.016)

    # --- gestos ---
    crear("Dura", lambda s, dz, l: (0, -0.0020 * ceja_interna(s, dz), -0.0065 * ceja_interna(s, dz) + 0.0015 * ceja_externa(s, dz) - 0.003 * comisura(s, dz) - 0.0010 * parpado_sup(s, dz)))
    crear("Enojado", lambda s, dz, l: (0, -0.0035 * ceja_interna(s, dz) - 0.0015 * ceja_externa(s, dz), -0.0085 * ceja_interna(s, dz) - 0.0020 * ceja_externa(s, dz) - 0.0030 * parpado_sup(s, dz) + 0.0015 * parpado_inf(s, dz) - 0.0045 * comisura(s, dz)))
    crear("Cansada", lambda s, dz, l: (0, 0.0, -0.0045 * ceja_externa(s, dz) - 0.0050 * parpado_sup(s, dz) - 0.0020 * parpado_inf(s, dz) - 0.0035 * comisura(s, dz)))
    crear("Amable", lambda s, dz, l: (0.004 * comisura(s, dz), 0.002 * comisura(s, dz), 0.003 * ceja_interna(s, dz) + 0.002 * ceja_externa(s, dz) + 0.0045 * pomulo(s, dz) + 0.009 * comisura(s, dz) + 0.0012 * parpado_inf(s, dz)))
    crear("Triste", lambda s, dz, l: (0, -0.0010 * ceja_interna(s, dz), 0.0045 * ceja_interna(s, dz) - 0.0025 * ceja_externa(s, dz) - 0.0030 * parpado_sup(s, dz) - 0.0060 * comisura(s, dz) + 0.002 * labio_inf(s, dz)))

    # --- boca: formas para las vocales (se mezclan con Hablar, que baja la mandíbula) y gestos de apoyo ---
    crear("Boca_O", lambda s, dz, l: (-0.0065 * comisura(s, dz), -0.0070 * boca(s, dz), -0.0030 * labio_inf(s, dz)))          # O, U: labios redondos y hacia adelante
    crear("Boca_I", lambda s, dz, l: (0.0080 * comisura(s, dz), 0.0010 * boca(s, dz), 0.0020 * comisura(s, dz)))              # E, I: labios anchos
    crear("CejasArriba", lambda s, dz, l: (0, -0.0010 * ceja_externa(s, dz), 0.0042 * ceja_interna(s, dz) + 0.0038 * ceja_externa(s, dz) + 0.0015 * parpado_sup(s, dz)))   # pregunta, sorpresa
    crear("Mueca", lambda s, dz, l: (0.0030 * comisura(s, dz) * (1 if l > 0 else 0), 0.0, (0.0080 * comisura(s, dz) + 0.0030 * pomulo(s, dz)) * (1 if l > 0 else -0.25) + 0.0040 * ceja_externa(s, dz) * (1 if l < 0 else 0)))   # media sonrisa: ironía, duda

    # --- boca al hablar: baja la mandíbula (todo lo que está bajo la línea de los labios) ---
    sk = ob.shape_key_add(name="Hablar", from_mix=False)
    for i, p in enumerate(base):
        dx, dy, dz = p.x - ojo.x, p.y - ojo.y, p.z - ojo.z
        if dz > -0.062 or abs(dx) > 0.075 or dy > 0.03:
            sk.data[i].co = p
            continue
        baja = min(1.0, (-0.062 - dz) / 0.030)            # 0 en la comisura, 1 en el mentón
        ancho = math.exp(-((dx / 0.052) ** 2))
        frente = max(0.0, min(1.0, (-dy + 0.01) / 0.05))
        sk.data[i].co = p + Vector((0, 0.005 * baja * ancho * frente, -0.026 * baja * ancho * frente))
    sk.value = 0.0

    # --- parpadeo: el párpado superior baja hasta tapar el ojo y el inferior sube ---
    crear("Parpadeo", lambda s, dz, l: (0, -0.0008 * parpado_sup(s, dz), -0.0100 * parpado_sup(s, dz) + 0.0040 * parpado_inf(s, dz)))
    print("EXPRESIONES:", [k.name for k in me.shape_keys.key_blocks])
