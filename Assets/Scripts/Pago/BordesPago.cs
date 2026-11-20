using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// Qué hay en el borde del pago, para saber si se puede pasar y qué aviso dar.
    public enum ZonaBorde { Adentro, Banado, Monte, Canadon, CaminoEste, PasoOeste }

    /// <summary>
    /// Los bordes del pago: lo que encierra al jugador sin que el mapa parezca cortado.
    /// - Norte: el bañado donde nace el arroyo, con agua baja, barro y juncal.
    /// - Este: un monte cerrado de talas, espinillos y coronillos; el camino real lo cruza por una picada.
    /// - Sur y oeste: el cañadón de la frontera, una barranca de tosca con agua al fondo. Del otro lado
    ///   empieza tierra adentro (ahí está la toldería). El camino real lo cruza por un vado, cerca del fortín.
    /// Todo en metros del prototipo (x hacia el este, z hacia el sur). El jugador se frena unos metros antes
    /// de la orilla, del monte o de la barranca; ver ControlJugador.AguaEn y JuegoPago (avisos).
    /// </summary>
    public static class BordesPago
    {
        public const float XMIN = -1600, XMAX = 1600, ZMIN = -1200, ZMAX = 1200;
        /// Del eje del cañadón al borde de arriba de la barranca.
        public const float MEDIO_ANCHO = 13;
        /// Ancho del agua al fondo del cañadón (desde el eje).
        public const float AGUA = 4;
        /// Cuántos metros antes de la orilla, del monte o de la barranca se frena el jugador.
        public const float MARGEN = 5;

        static float N(double x, double z) => (MapaPago.Ruido(x, z) - 0.5f) * 2;
        static float SStep(float a, float b, float x) => MapaPago.SStep(a, b, x);

        // ---------- las líneas del borde ----------

        /// Orilla del bañado (norte): la z donde empieza el agua.
        public static float OrillaBanado(float x) => -1098 + 14 * Mathf.Sin(x * 0.0041f + 0.7f) + 10 * N(x * 0.006, 3.1);

        /// Frente del monte (este): la x donde empiezan los árboles.
        public static float FrenteMonte(float z) => 1462 + 12 * Mathf.Sin(z * 0.005f + 2) + 10 * N(5.3, z * 0.007);

        /// Eje del cañadón, tramo sur: z en función de x.
        public static float CanadonSur(float x) => 1072 + 16 * Mathf.Sin(x * 0.0043f + 1.1f) + 9 * N(x * 0.008, 7.7);

        /// Eje del cañadón, tramo oeste: x en función de z.
        public static float CanadonOeste(float z) => -1542 + 8 * Mathf.Sin(z * 0.006f + 0.4f) + 5 * N(9.1, z * 0.009);

        /// Distancia al eje del cañadón (los dos tramos se juntan en la esquina sudoeste).
        public static float DistCanadon(float x, float z)
        {
            float d = float.MaxValue;
            float zs = CanadonSur(x), xo = CanadonOeste(z);
            if (x >= xo - MEDIO_ANCHO) d = Mathf.Min(d, Mathf.Abs(z - zs));
            if (z <= zs + MEDIO_ANCHO) d = Mathf.Min(d, Mathf.Abs(x - xo));
            if (x < xo && z > zs) d = Mathf.Min(d, Vector2.Distance(new Vector2(x, z), new Vector2(xo, zs)));
            return d;
        }

        /// Del otro lado del cañadón: tierra adentro.
        public static bool TierraAdentro(float x, float z) => z > CanadonSur(x) || x < CanadonOeste(z);

        // ---------- consultas ----------

        public static ZonaBorde Zona(float x, float z)
        {
            if (z < OrillaBanado(x) + MARGEN) return ZonaBorde.Banado;
            bool camino = MapaPago.DistCamino(x, z) < 8;
            if (x > FrenteMonte(z) - MARGEN) return camino ? ZonaBorde.CaminoEste : ZonaBorde.Monte;
            if (TierraAdentro(x, z) || DistCanadon(x, z) < MEDIO_ANCHO + MARGEN) return camino ? ZonaBorde.PasoOeste : ZonaBorde.Canadon;
            return ZonaBorde.Adentro;
        }

        /// Más allá de donde puede ir el jugador (o los animales del pago).
        public static bool Fuera(float x, float z) => Zona(x, z) != ZonaBorde.Adentro;

        /// Donde no crece el pasto: el agua y la barranca de tosca del cañadón, y los charcos del bañado.
        public static bool SinPasto(float x, float z)
        {
            if (DistCanadon(x, z) < MEDIO_ANCHO - 2) return true;
            float ob = OrillaBanado(x);
            return z < ob + 4 && MapaPago.Ruido(x * 0.025 + 3, z * 0.025 - 4) > 0.49f;
        }

        /// Qué le dice el juego al jugador cuando quiere pasar.
        public static string Aviso(ZonaBorde z) => z switch
        {
            ZonaBorde.Banado => "El bañado no da paso: barro hasta la cincha y juncos más altos que un hombre.",
            ZonaBorde.Monte => "El monte es una maraña de talas y espinillos. Por ahí no pasa ni un perro.",
            ZonaBorde.CaminoEste => "El camino real sigue hacia Buenos Aires. Sin pasaporte del juez de paz no se puede salir del partido.",
            ZonaBorde.PasoOeste => "El vado lo vigila el fortín. Sin el permiso del comandante nadie cruza la frontera.",
            ZonaBorde.Canadon => "Del otro lado del cañadón empieza tierra adentro. Nadie cruza sin baqueano ni permiso del fortín.",
            _ => null,
        };

        /// Cuánto se hunde el suelo: el cañadón (barranca y fondo con agua; vado bajo donde pasa el camino) y el bañado.
        public static float Hundido(float x, float z)
        {
            float h = 0;
            float d = DistCanadon(x, z);
            if (d < MEDIO_ANCHO + 4)
            {
                float prof = 2.7f + 0.5f * N(x * 0.02, z * 0.02);
                float perfil = 1 - SStep(AGUA + 1.5f, MEDIO_ANCHO + 1.5f, d);   // fondo ancho y barranca empinada
                float dc = MapaPago.DistCamino(x, z);
                float vado = Mathf.Lerp(0.3f, 1, SStep(4, 16, dc));                // el camino baja al vado de a poco
                h = prof * perfil * vado;
            }
            float ob = OrillaBanado(x);
            if (z < ob + 16)
                h = Mathf.Max(h, 0.75f * SStep(ob + 16, ob - 12, z) * (0.6f + 0.4f * MapaPago.Ruido(x * 0.02 + 4, z * 0.02)));
            return h;
        }

        /// Pinta el borde sobre los pesos del suelo (pasto verde, pasto seco, tierra, agua).
        public static Vector4 PesosSuelo(float x, float z, Vector4 w)
        {
            float d = DistCanadon(x, z);
            if (d < MEDIO_ANCHO + 4)
            {
                float barranca = 1 - SStep(MEDIO_ANCHO - 2, MEDIO_ANCHO + 3, d + 1.5f * N(x * 0.07, z * 0.07));
                float agua = 1 - SStep(AGUA - 0.5f, AGUA + 1.5f, d + 1.2f * N(x * 0.05 + 2, z * 0.05));
                if (MapaPago.DistCamino(x, z) < 6) agua *= 0.35f;   // el vado: agua baja sobre la huella
                Hacia(ref w, 2, barranca);
                Hacia(ref w, 3, agua);
            }
            else if (TierraAdentro(x, z))
            {
                // pampa abierta, sin animales del pago: paja y pasto seco
                Hacia(ref w, 1, 0.45f + 0.35f * MapaPago.Ruido(x * 0.01 + 8, z * 0.01));
            }
            float ob = OrillaBanado(x);
            if (z < ob + 14)
            {
                float dentro = SStep(ob + 14, ob - 8, z);
                float charco = SStep(0.42f, 0.56f, MapaPago.Ruido(x * 0.025 + 3, z * 0.025 - 4)) * dentro;
                Hacia(ref w, 0, dentro * 0.5f);   // verde del juncal
                Hacia(ref w, 2, dentro * 0.35f);  // barro
                Hacia(ref w, 3, charco);
            }
            float fm = FrenteMonte(z);
            if (x > fm - 8) Hacia(ref w, 2, 0.3f * SStep(fm - 8, fm + 12, x));   // hojarasca y sombra bajo el monte
            return w;
        }

        static void Hacia(ref Vector4 w, int k, float f)
        {
            w *= 1 - f;
            w[k] += f;
        }

        // ---------- plantas del borde ----------

        public struct Planta { public string especie; public float x, z, escala; }

        static float H(double i) => MapaPago.Hs(i * 1.37 + 5003);

        /// Árboles y matas del borde: el monte cerrado del este, el juncal del bañado y los sauces y ceibos
        /// de la barranca. Tierra adentro casi no hay árboles: es pampa abierta.
        public static List<Planta> Plantas()
        {
            var l = new List<Planta>();
            int k = 0;

            // monte: cerrado en los primeros 90 m, más ralo atrás (ya no se ve)
            for (float z = ZMIN + 2; z < ZMAX - 1; z += 3.8f)
                for (float x = 1420; x < XMAX - 1; x += 3.8f)
                {
                    k++;
                    float jx = x + (H(k) - 0.5f) * 3.2f, jz = z + (H(k + 0.5) - 0.5f) * 3.2f;
                    float f = FrenteMonte(jz);
                    float hondo = jx - f;
                    if (hondo < -3) continue;
                    if (hondo > 90 && H(k + 0.7) < 0.5f) continue;
                    if (MapaPago.DistCamino(jx, jz) < 6) continue;   // la picada del camino real
                    float r = H(k + 0.3);
                    string e;
                    if (hondo < 5) e = r < 0.45f ? "cortadera" : r < 0.85f ? "espinillo" : "coronillo";
                    else e = r < 0.42f ? "tala" : r < 0.72f ? "coronillo" : r < 0.94f ? "espinillo" : "sauce";
                    l.Add(new Planta { especie = e, x = jx, z = jz, escala = (hondo < 5 ? 0.75f : 0.95f) + 0.45f * H(k + 0.9) });
                }

            // bañado: juncal y cortadera, sauces sueltos en la orilla
            for (float x = XMIN + 2; x < XMAX - 1; x += 4.5f)
                for (float z = ZMIN + 2; z < -1050; z += 4.5f)
                {
                    k++;
                    float jx = x + (H(k) - 0.5f) * 3.5f, jz = z + (H(k + 0.5) - 0.5f) * 3.5f;
                    float ob = OrillaBanado(jx);
                    if (jz > ob + 6) continue;
                    float densidad = jz > ob - 6 ? 0.75f : 0.4f + 0.4f * MapaPago.Ruido(jx * 0.03, jz * 0.03);
                    if (H(k + 0.2) > densidad) continue;
                    if (jx > FrenteMonte(jz) - 4) continue;
                    bool sauce = jz > ob - 8 && jz < ob + 4 && H(k + 0.4) < 0.07f;
                    l.Add(new Planta { especie = sauce ? "sauce" : "cortadera", x = jx, z = jz, escala = sauce ? 1.1f + 0.3f * H(k + 0.6) : 0.8f + 0.6f * H(k + 0.6) });
                }

            // cañadón: sauces y ceibos en la barranca, cortadera en el fondo; tierra adentro, alguna mata suelta
            for (float x = XMIN + 2; x < XMAX - 1; x += 5f)
                for (float z = ZMIN + 2; z < ZMAX - 1; z += 5f)
                {
                    if (x > -1430 && z < 990) continue;   // lejos del cañadón
                    k++;
                    float jx = x + (H(k) - 0.5f) * 4, jz = z + (H(k + 0.5) - 0.5f) * 4;
                    if (jz < OrillaBanado(jx) + 8 || jx > FrenteMonte(jz) - 4) continue;
                    if (MapaPago.DistCamino(jx, jz) < 7) continue;
                    float d = DistCanadon(jx, jz);
                    float r = H(k + 0.2);
                    if (d < AGUA + 1.5f) { if (r < 0.18f) l.Add(new Planta { especie = "cortadera", x = jx, z = jz, escala = 0.9f + 0.5f * H(k + 0.3) }); }
                    else if (d > MEDIO_ANCHO - 3 && d < MEDIO_ANCHO + 6) { if (r < 0.07f) l.Add(new Planta { especie = H(k + 0.4) < 0.6f ? "sauce" : "ceibo", x = jx, z = jz, escala = 0.9f + 0.4f * H(k + 0.3) }); }
                    else if (TierraAdentro(jx, jz) && r < 0.025f) l.Add(new Planta { especie = "cortadera", x = jx, z = jz, escala = 0.8f + 0.5f * H(k + 0.3) });
                }
            return l;
        }

        /// Si una posición está en la franja del borde (para reemplazar lo que haya ahí al replantar).
        public static bool EnFranja(float x, float z) =>
            z < OrillaBanado(x) + 10 || x > FrenteMonte(z) - 6 || TierraAdentro(x, z) || DistCanadon(x, z) < MEDIO_ANCHO + 8;
    }
}
