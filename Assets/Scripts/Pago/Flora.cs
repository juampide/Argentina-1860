using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// Especies autóctonas en low poly (ombú, tala, espinillo, coronillo, sauce, ceibo, cortadera), igual que en campo.js.
    public static class Flora
    {
        static Geo ICO => Geo.Ico(1, 1);
        static Geo ICO0 => Geo.Ico(1, 0);
        static Geo CONO(float r, float h, int n = 5) => Geo.Cono(r, h, n);
        static Geo CIL(float rs, float ri, float h, int n = 6) => Geo.Cilindro(rs, ri, h, n);
        static string Verde(float h, float s, float l) => Conv.Clave(Conv.HSL(h, s, l));
        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);

        public static readonly string[] ESPECIES = { "ombu", "tala", "espinillo", "coronillo", "sauce", "ceibo", "cortadera" };

        public static Nodo Especie(string nombre)
        {
            var n = new Nodo(nombre);
            switch (nombre)
            {
                case "ombu":
                    n.Pieza(CIL(0.9f, 1.5f, 2.6f, 7), "#7b6a55", v(0, 1.3f, 0));
                    for (int i = 0; i < 4; i++)
                        n.Pieza(CONO(0.6f, 1.5f, 4), "#7b6a55", v(Mathf.Cos(i * 1.57f) * 1.3f, 0.55f, Mathf.Sin(i * 1.57f) * 1.3f), v(Mathf.Sin(i * 1.57f) * 0.5f, 0, -Mathf.Cos(i * 1.57f) * 0.5f));
                    n.Pieza(ICO, Verde(0.27f, 0.45f, 0.3f), v(0, 5.2f, 0), default, v(5.6f, 2.4f, 5.6f));
                    n.Pieza(ICO, Verde(0.28f, 0.5f, 0.36f), v(2.8f, 4.5f, 1.2f), default, v(3.6f, 1.8f, 3.6f));
                    n.Pieza(ICO, Verde(0.26f, 0.45f, 0.33f), v(-2.6f, 4.7f, -1.5f), default, v(3.4f, 1.7f, 3.4f));
                    break;
                case "tala":
                    n.Pieza(CIL(0.18f, 0.28f, 1.8f, 5), "#6b5a46", v(0, 0.9f, 0));
                    n.Pieza(ICO, Verde(0.22f, 0.38f, 0.32f), v(0, 2.4f, 0), default, v(1.9f, 0.9f, 1.9f));
                    n.Pieza(ICO, Verde(0.23f, 0.4f, 0.37f), v(0.6f, 3.1f, 0.3f), default, v(1.3f, 0.8f, 1.3f));
                    n.Pieza(ICO, Verde(0.21f, 0.36f, 0.3f), v(-0.7f, 2.8f, -0.4f), default, v(1.2f, 0.7f, 1.2f));
                    break;
                case "espinillo":
                    n.Pieza(CIL(0.12f, 0.2f, 2.2f, 5), "#5f4d3b", v(0, 1.1f, 0));
                    n.Pieza(ICO, Verde(0.2f, 0.42f, 0.34f), v(0, 3.0f, 0), default, v(2.3f, 0.7f, 2.3f));
                    for (int i = 0; i < 5; i++)
                        n.Pieza(ICO0, "#e6c635", v(Mathf.Cos(i * 1.3f) * 1.3f, 3.5f, Mathf.Sin(i * 1.3f) * 1.3f), default, v(0.22f, 0.22f, 0.22f));
                    break;
                case "coronillo":
                    n.Pieza(CIL(0.2f, 0.35f, 2.2f, 5), "#5c4a3a", v(0, 1.1f, 0));
                    n.Pieza(ICO, Verde(0.3f, 0.5f, 0.22f), v(0, 3.6f, 0), default, v(2.4f, 2.0f, 2.4f));
                    n.Pieza(ICO, Verde(0.31f, 0.5f, 0.27f), v(0.9f, 4.4f, 0.4f), default, v(1.5f, 1.3f, 1.5f));
                    break;
                case "sauce":
                    n.Pieza(CIL(0.25f, 0.4f, 3.2f, 5), "#5d4c3c", v(0, 1.6f, 0));
                    n.Pieza(ICO, Verde(0.19f, 0.42f, 0.5f), v(0, 4.3f, 0), default, v(2.6f, 1.3f, 2.6f));
                    for (int i = 0; i < 6; i++)
                        n.Pieza(CONO(0.35f, 3, 4), Verde(0.2f, 0.45f, 0.46f), v(Mathf.Cos(i * 1.05f) * 2.4f, 2.9f, Mathf.Sin(i * 1.05f) * 2.4f), v(Mathf.PI, 0, 0));
                    break;
                case "ceibo":
                    n.Pieza(CIL(0.2f, 0.32f, 2.6f, 5), "#5a463a", v(0, 1.3f, 0));
                    n.Pieza(ICO, Verde(0.27f, 0.45f, 0.3f), v(0, 3.4f, 0), default, v(1.9f, 1.5f, 1.9f));
                    for (int i = 0; i < 6; i++)
                        n.Pieza(ICO0, "#cc2a2a", v(Mathf.Cos(i * 1.0f) * 1.3f, 3.8f + (i % 2) * 0.4f, Mathf.Sin(i * 1.0f) * 1.3f), default, v(0.28f, 0.28f, 0.28f));
                    break;
                case "cortadera":
                    for (int i = 0; i < 7; i++)
                        n.Pieza(CONO(0.16f, 1.7f + (i % 3) * 0.3f, 3), Verde(0.2f, 0.3f, 0.52f), v(Mathf.Cos(i * 0.9f) * 0.45f, 0.85f, Mathf.Sin(i * 0.9f) * 0.45f), v(Mathf.Sin(i * 0.9f) * 0.18f, 0, -Mathf.Cos(i * 0.9f) * 0.18f));
                    for (int i = 0; i < 3; i++)
                        n.Pieza(ICO0, "#e9e1c8", v(Mathf.Cos(i * 2.1f) * 0.5f, 2.1f + i * 0.15f, Mathf.Sin(i * 2.1f) * 0.5f), default, v(0.18f, 0.55f, 0.18f));
                    break;
                default:
                    throw new ArgumentException("Especie desconocida: " + nombre);
            }
            return n;
        }

        public struct Planta
        {
            public string especie;
            public float x, z, s, r;   // posición en el prototipo, escala y giro (radianes)
        }

        /// Reparte la flora con las mismas semillas que el prototipo, así cada árbol queda donde estaba.
        public static List<Planta> Repartir()
        {
            var todo = new List<Planta>();
            int sem = 1;
            Func<float, float, float, Vector2> Elipse(Vector2 c, float rx, float ry) => (a, b, c2) =>
            {
                float t = a * 6.28f, r = Mathf.Sqrt(b) * (0.9f + 0.1f * c2);
                var m = MapaPago.K(c);
                return new Vector2(m.x + Mathf.Cos(t) * rx * 1000 * r, m.y + Mathf.Sin(t) * ry * 1000 * r);
            };
            Func<float, float, float, Vector2> RectKm(float x0, float y0, float x1, float y1) => (a, b, _) =>
                MapaPago.K(MapaPago.Mix(x0, x1, a), MapaPago.Mix(y0, y1, b));

            void Dispersar(string esp, int n, Func<float, float, float, Vector2> region, float e0 = 0.8f, float e1 = 1.3f)
            {
                int cuenta = 0, intentos = 0;
                while (cuenta < n && intentos++ < n * 30)
                {
                    var p = region(MapaPago.Hs(sem++), MapaPago.Hs(sem++), MapaPago.Hs(sem++));
                    if (!MapaPago.PuedeCrecer(p.x, p.y) || !MapaPago.LibreDeSitios(p.x, p.y, 1)) continue;
                    float s = MapaPago.Mix(e0, e1, MapaPago.Hs(sem++));
                    float r = MapaPago.Hs(sem++) * 6.28f;
                    todo.Add(new Planta { especie = esp, x = p.x, z = p.y, s = s, r = r });
                    cuenta++;
                }
            }

            var mc = MapaPago.MONTE_C;
            Dispersar("tala", 170, Elipse(mc, MapaPago.MONTE_RX, MapaPago.MONTE_RY), 0.8f, 1.5f);
            Dispersar("coronillo", 45, Elipse(mc, MapaPago.MONTE_RX, MapaPago.MONTE_RY), 0.8f, 1.3f);
            Dispersar("espinillo", 60, Elipse(mc, MapaPago.MONTE_RX * 1.3f, MapaPago.MONTE_RY * 1.3f), 0.8f, 1.3f);
            Dispersar("espinillo", 40, RectKm(0.0f, 1.55f, 1.5f, 2.4f), 0.8f, 1.2f);

            // sauces y ceibos sobre la ribera del arroyo
            var ribera = new List<Planta>();
            var curva = MapaPago.Suavizar(MapaPago.ARROYO, 30);
            for (int i = 0; i < curva.Count; i++)
            {
                if (i % 2 == 1) continue;
                var p = MapaPago.K(curva[i]);
                float lado = MapaPago.Hs(i + 5) > 0.5f ? 1 : -1, d = 9 + MapaPago.Hs(i + 9) * 14;
                var t = new Planta { x = p.x + lado * d, z = p.y + (MapaPago.Hs(i + 11) - 0.5f) * 12, s = 0.8f + MapaPago.Hs(i + 13) * 0.6f, r = MapaPago.Hs(i + 15) * 6.28f };
                if (MapaPago.PuedeCrecer(t.x, t.z) && MapaPago.LibreDeSitios(t.x, t.z, 1)) ribera.Add(t);
            }
            for (int i = 0; i < ribera.Count; i++)
            {
                var t = ribera[i];
                t.especie = i % 3 != 2 ? "sauce" : "ceibo";
                todo.Add(t);
            }

            var lc = MapaPago.LAGUNA_C;
            Dispersar("cortadera", 260, Elipse(lc, MapaPago.LAGUNA_RX * 1.7f, MapaPago.LAGUNA_RY * 1.9f));
            Dispersar("cortadera", 380, RectKm(0.0f, 1.55f, 1.5f, 2.4f), 0.8f, 1.5f);
            Dispersar("cortadera", 120, RectKm(0.0f, 0.0f, 3.2f, 2.4f), 0.7f, 1.2f);

            // ombúes solitarios y de referencia (se ven desde lejos)
            var ombu = new (float kx, float ky, float dx, float dz)[]
            {
                (0.75f, 0.75f, -17, -15), (2.15f, 1.25f, -30, 24), (1.0f, 1.0f, 12, -10), (1.9f, 1.32f, 0, 0),
                (0.59f, 0.9f, 14, -10), (1.4f, 0.4f, 0, 0), (2.7f, 0.9f, 0, 0), (0.9f, 2.2f, 0, 0),
            };
            for (int i = 0; i < ombu.Length; i++)
            {
                var p = MapaPago.K(ombu[i].kx, ombu[i].ky);
                todo.Add(new Planta { especie = "ombu", x = p.x + ombu[i].dx, z = p.y + ombu[i].dz, s = 1 + MapaPago.Hs(i + 3) * 0.3f, r = MapaPago.Hs(i) * 6.28f });
            }
            return todo;
        }
    }
}
