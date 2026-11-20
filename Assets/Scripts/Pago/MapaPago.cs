using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// El pago: 3,2 × 2,4 km, portado de campo.js con los mismos trazados, semillas y fórmulas.
    /// Todas las funciones usan coordenadas del prototipo: x hacia el este y z hacia el sur, en metros,
    /// con el centro del mapa en (0, 0). En Unity la z cambia de signo (ver Conv).
    /// </summary>
    public static class MapaPago
    {
        public const float ANCHO = 3200, ALTO = 2400;
        /// Rango de alturas del Terrain: de YMIN a YMIN + RANGO metros.
        public const float YMIN = -5f, RANGO = 15f;

        /// Coordenadas del dibujo en km (este → derecha, sur → abajo) a metros del prototipo.
        public static Vector2 K(float kx, float ky) => new Vector2((kx - 1.6f) * 1000f, (ky - 1.2f) * 1000f);
        public static Vector2 K(Vector2 k) => K(k.x, k.y);

        /// Punto del prototipo (x, z) a posición de Unity apoyada en el suelo.
        public static Vector3 AUnity(float x, float z) => new Vector3(x, Altura(x, z), -z);
        public static Vector3 AUnity(Vector2 p) => AUnity(p.x, p.y);

        // ---------- ruido (idéntico al del prototipo) ----------
        static double H21(double x, double y)
        {
            double s = Math.Sin(x * 127.1 + y * 311.7) * 43758.5453;
            return s - Math.Floor(s);
        }

        public static float Ruido(double x, double y)
        {
            double ix = Math.Floor(x), iy = Math.Floor(y), fx = x - ix, fy = y - iy;
            double ux = fx * fx * (3 - 2 * fx), uy = fy * fy * (3 - 2 * fy);
            double a = H21(ix, iy), b = H21(ix + 1, iy), c = H21(ix, iy + 1), d = H21(ix + 1, iy + 1);
            return (float)(a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy);
        }

        /// hs(i) del prototipo: número pseudoaleatorio fijo en [0, 1) para la semilla i.
        public static float Hs(double i) => (float)H21(i * 12.9898, i * 4.1414 + 7);
        public static float Mix(float a, float b, float t) => a + (b - a) * t;

        public static float SStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3 - 2 * t);
        }

        // ---------- trazados (en km) ----------
        public static readonly Vector2[] ARROYO = V(1.4f, -0.05f, 1.38f, 0.2f, 1.25f, 0.55f, 1.18f, 0.8f, 1.16f, 1.05f, 1.15f, 1.3f, 1.25f, 1.6f, 1.4f, 1.85f, 1.52f, 2.0f);
        static readonly Vector2[] CAMINO_PROTO = V(-0.05f, 1.5f, 0.4f, 1.46f, 0.8f, 1.38f, 1.15f, 1.3f, 1.6f, 1.27f, 2.15f, 1.25f, 2.7f, 1.15f, 3.25f, 1.0f);
        static readonly Vector2[][] SENDAS_PROTO =
        {
            V(0.75f, 0.8f, 0.9f, 0.92f, 1.0f, 1.0f, 1.08f, 1.15f, 1.15f, 1.3f),   // estancia → paso
            V(0.59f, 0.9f, 0.68f, 0.85f, 0.75f, 0.8f),                             // rancho de peones → estancia
            V(2.15f, 1.25f, 2.0f, 1.6f, 1.85f, 1.9f, 1.78f, 2.0f),                 // posta → laguna
            V(2.15f, 1.25f, 2.25f, 0.9f, 2.3f, 0.6f),                              // posta → monte
            V(0.75f, 0.8f, 0.45f, 0.95f, 0.2f, 1.1f, 0.12f, 1.15f),                // estancia → fortín
            V(0.75f, 0.8f, 0.62f, 1.3f, 0.57f, 1.6f, 0.55f, 1.85f),                // estancia → puesto del baqueano
            V(0.75f, 0.8f, 0.95f, 0.62f, 1.17f, 0.5f),                             // estancia → rancho de Casiana
        };
        /// Camino real: sale del loteo (Resources/LoteoPago.asset); si no hay loteo, el del prototipo.
        public static Vector2[] CAMINO
        {
            get
            {
                var r = LoteoPago.Instancia?.CaminoReal();
                return r != null && r.km != null && r.km.Length >= 2 ? r.km : CAMINO_PROTO;
            }
        }

        /// Los demás caminos (callejones, sendas, huellas) del loteo; si no hay loteo, las sendas del prototipo.
        public static Vector2[][] SENDAS
        {
            get
            {
                var l = LoteoPago.Instancia;
                if (l == null) return SENDAS_PROTO;
                var r = new List<Vector2[]>();
                foreach (var c in l.caminos) if (c.tipo != TipoCamino.Real && c.km != null && c.km.Length >= 2) r.Add(c.km);
                return r.ToArray();
            }
        }

        public static readonly Vector2 LAGUNA_C = new Vector2(1.7f, 2.05f);
        public const float LAGUNA_RX = 0.21f, LAGUNA_RY = 0.14f;
        public static readonly Vector2 MONTE_C = new Vector2(2.3f, 0.45f);
        public const float MONTE_RX = 0.33f, MONTE_RY = 0.2f;

        static Vector2[] V(params float[] f)
        {
            var r = new Vector2[f.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = new Vector2(f[i * 2], f[i * 2 + 1]);
            return r;
        }

        /// Curva Catmull-Rom igual a THREE.CatmullRomCurve3(..., 'catmullrom', 0.5).getPoints()
        public static List<Vector2> Suavizar(Vector2[] pts, int paso = 12)
        {
            int div = Math.Max(8, pts.Length * paso);
            var r = new List<Vector2>(div + 1);
            int l = pts.Length;
            for (int d = 0; d <= div; d++)
            {
                float t = (float)d / div;
                float p = (l - 1) * t;
                int ip = (int)Math.Floor(p);
                float w = p - ip;
                if (w == 0 && ip == l - 1) { ip = l - 2; w = 1; }
                Vector2 p0 = ip > 0 ? pts[ip - 1] : pts[0] - pts[1] + pts[0];
                Vector2 p1 = pts[ip], p2 = pts[ip + 1];
                Vector2 p3 = ip + 2 < l ? pts[ip + 2] : pts[l - 1] - pts[l - 2] + pts[l - 1];
                r.Add(new Vector2(CR(p0.x, p1.x, p2.x, p3.x, w), CR(p0.y, p1.y, p2.y, p3.y, w)));
            }
            return r;
        }

        static float CR(float x0, float x1, float x2, float x3, float t)
        {
            float t0 = 0.5f * (x2 - x0), t1 = 0.5f * (x3 - x1);
            float c2 = -3 * x1 + 3 * x2 - 2 * t0 - t1, c3 = 2 * x1 - 2 * x2 + t0 + t1;
            return x1 + t0 * t + c2 * t * t + c3 * t * t * t;
        }

        // ---------- sitios aplanados para las construcciones ----------
        public struct Sitio
        {
            public Vector2 p;
            public float r, h;
        }

        public static readonly Sitio[] SITIOS;

        static MapaPago()
        {
            var s = new (float kx, float ky, float r)[] { (0.75f, 0.75f, 75), (0.59f, 0.9f, 40), (2.15f, 1.25f, 80), (1.0f, 1.0f, 25), (0.12f, 1.15f, 45), (0.55f, 1.85f, 28), (1.17f, 0.5f, 30) };
            SITIOS = new Sitio[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                var p = K(s[i].kx, s[i].ky);
                SITIOS[i] = new Sitio { p = p, r = s[i].r, h = Base(p.x, p.y) };
            }
        }

        static float Base(float x, float z)
        {
            float h = 2.2f * Ruido(x * 0.0032 + 10, z * 0.0032) + 0.9f * Ruido(x * 0.011, z * 0.011 + 5) + 0.25f * Ruido(x * 0.05, z * 0.05) - 1.2f;
            var pp = K(0.55f, 1.85f);
            float d2 = (x - pp.x) * (x - pp.x) + (z - pp.y) * (z - pp.y);
            h += 3.2f * Mathf.Exp(-d2 / (2 * 70 * 70));
            return h;
        }

        // ---------- campos de distancia al arroyo y a los caminos ----------
        // Reemplazan a la máscara dibujada en un canvas del prototipo: dan el mismo resultado sin depender del navegador.
        class CampoDist
        {
            const float CELDA = 2f;
            const int W = 1601, H = 1201;
            readonly float[] d = new float[W * H];
            readonly float tope;

            public CampoDist(float tope)
            {
                this.tope = tope;
                for (int i = 0; i < d.Length; i++) d[i] = tope;
            }

            public void Polilinea(List<Vector2> km, float resta)
            {
                for (int i = 0; i + 1 < km.Count; i++) Segmento(K(km[i]), K(km[i + 1]), resta);
            }

            void Segmento(Vector2 a, Vector2 b, float resta)
            {
                float r = tope + resta;
                int x0 = Mathf.Clamp((int)((Mathf.Min(a.x, b.x) - r + 1600) / CELDA), 0, W - 1);
                int x1 = Mathf.Clamp((int)((Mathf.Max(a.x, b.x) + r + 1600) / CELDA) + 1, 0, W - 1);
                int z0 = Mathf.Clamp((int)((Mathf.Min(a.y, b.y) - r + 1200) / CELDA), 0, H - 1);
                int z1 = Mathf.Clamp((int)((Mathf.Max(a.y, b.y) + r + 1200) / CELDA) + 1, 0, H - 1);
                Vector2 ab = b - a;
                float l2 = Mathf.Max(1e-6f, ab.sqrMagnitude);
                for (int iz = z0; iz <= z1; iz++)
                    for (int ix = x0; ix <= x1; ix++)
                    {
                        var p = new Vector2(-1600 + ix * CELDA, -1200 + iz * CELDA);
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
                        float dist = Vector2.Distance(p, a + ab * t) - resta;
                        int k = iz * W + ix;
                        if (dist < d[k]) d[k] = dist;
                    }
            }

            public float Muestra(float x, float z)
            {
                float fx = (x + 1600) / CELDA, fz = (z + 1200) / CELDA;
                if (fx < 0 || fz < 0 || fx >= W - 1 || fz >= H - 1) return tope;
                int ix = (int)fx, iz = (int)fz;
                float tx = fx - ix, tz = fz - iz;
                int k = iz * W + ix;
                float a = Mathf.Lerp(d[k], d[k + 1], tx), b = Mathf.Lerp(d[k + W], d[k + W + 1], tx);
                return Mathf.Lerp(a, b, tz);
            }
        }

        static CampoDist arroyo, caminos;
        static List<Vector2> patios = new List<Vector2>();

        /// Patios de tierra pisada donde terminan los caminos (puerta de una casa, tranquera, orilla), en metros.
        public static IReadOnlyList<Vector2> Patios { get { Asegurar(); return patios; } }

        /// Vuelve a calcular los caminos (cuando cambia el loteo).
        public static void Invalidar()
        {
            lock (candado) { caminos = null; arroyo = null; }
        }

        /// Uso del lote en un punto (en metros del prototipo). Sin loteo o fuera de todo lote: campo abierto.
        public static UsoLote Uso(float x, float z)
        {
            var l = LoteoPago.Instancia?.LoteEn(x, z);
            return l != null ? l.uso : UsoLote.CampoAbierto;
        }
        static readonly object candado = new object();

        static void Asegurar()
        {
            if (caminos != null) return;
            lock (candado)
            {
                if (caminos != null) return;
                var a = new CampoDist(60);
                a.Polilinea(Suavizar(ARROYO), 0);
                var c = new CampoDist(30);
                var pt = new List<Vector2>();
                var l = LoteoPago.Instancia;
                if (l != null)
                    foreach (var cm in l.caminos)
                    {
                        if (cm.km == null || cm.km.Length < 2) continue;
                        c.Polilinea(Suavizar(cm.km), LoteoPago.MedioAncho(cm.tipo));
                        if (cm.patioAlFinal) pt.Add(K(cm.km[cm.km.Length - 1]));
                    }
                else
                {
                    c.Polilinea(Suavizar(CAMINO_PROTO), 3.5f);                  // camino real: 7 m de ancho
                    foreach (var s in SENDAS_PROTO) c.Polilinea(Suavizar(s), 1.8f);   // sendas: 3,6 m
                }
                patios = pt;
                arroyo = a;
                caminos = c;
            }
        }

        static bool FueraDelMapa(float x, float z) => x < -ANCHO / 2 || x > ANCHO / 2 || z < -ALTO / 2 || z > ALTO / 2;

        /// Distancia con signo al borde de la laguna (negativa adentro).
        static float DistLaguna(float x, float z)
        {
            var c = K(LAGUNA_C);
            float rx = LAGUNA_RX * 1000, ry = LAGUNA_RY * 1000;
            float ex = (x - c.x) / rx, ez = (z - c.y) / ry, r = Mathf.Sqrt(ex * ex + ez * ez);
            if (r < 1e-4f) return -ry;
            float gx = ex / (r * rx), gz = ez / (r * ry);
            return (r - 1) / Mathf.Sqrt(gx * gx + gz * gz);
        }

        static float Erf(float x)
        {
            float s = Mathf.Sign(x);
            x = Mathf.Abs(x);
            float t = 1f / (1f + 0.3275911f * x);
            float y = 1 - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-x * x);
            return s * y;
        }

        // El prototipo hunde el terreno con la máscara de agua desenfocada (blur de 7 px ≈ 11 m).
        const float SIGMA = 10.9f;

        static float CorteAgua(float x, float z)
        {
            float k = 1f / (SIGMA * 1.41421356f);
            float da = arroyo.Muestra(x, z);
            float ca = 0.5f * (Erf((da + 4.5f) * k) - Erf((da - 4.5f) * k));
            float cl = 0.5f * (1 - Erf(DistLaguna(x, z) * k));
            return Mathf.Min(1, ca + cl);
        }

        // ---------- consultas ----------

        /// Altura del suelo: lomas suaves, aplanado en los sitios y hundido en el arroyo y la laguna.
        public static float Altura(float x, float z)
        {
            Asegurar();
            float h = Base(x, z);
            foreach (var s in SITIOS)
            {
                float w = SStep(s.r * 1.7f, s.r, Vector2.Distance(new Vector2(x, z), s.p));
                if (w > 0) h = Mix(h, s.h, w);
            }
            if (!FueraDelMapa(x, z))
            {
                float c = CorteAgua(x, z);
                if (c > 0) h -= 1.5f * Mathf.Min(1, c * 1.4f);
            }
            return h;
        }

        public static bool EnAgua(float x, float z)
        {
            if (FueraDelMapa(x, z)) return true;
            Asegurar();
            if (caminos.Muestra(x, z) < 0) return false;   // el camino cruza el arroyo por el paso
            return arroyo.Muestra(x, z) < 4.5f || DistLaguna(x, z) < 0;
        }

        public static bool EnCamino(float x, float z)
        {
            Asegurar();
            return caminos.Muestra(x, z) < 0;
        }

        public static bool PuedeCrecer(float x, float z) => !EnAgua(x, z) && !EnCamino(x, z);

        /// Distancia al borde del camino real o de una senda (negativa sobre el camino), hasta 30 m.
        public static float DistCamino(float x, float z)
        {
            Asegurar();
            return caminos.Muestra(x, z);
        }

        /// Cuánto se nota la gente en un lugar, de 0 (campo abierto) a 1 (al lado de las casas).
        /// Cerca de los sitios poblados y a la orilla de los caminos el pasto está comido por los animales
        /// y pisoteado; lejos crece alto. El borde no es parejo: lo corta un ruido.
        public static float Pastoreo(float x, float z, float alcance = 1)
        {
            Asegurar();
            float f = 0;
            foreach (var s in SITIOS)
            {
                float d = Vector2.Distance(new Vector2(x, z), s.p);
                f = Mathf.Max(f, 1 - SStep(s.r * 1.2f, s.r * (1.2f + 3f * alcance), d));
            }
            float dc = caminos.Muestra(x, z);
            f = Mathf.Max(f, (1 - SStep(1, Mathf.Min(1 + 22 * alcance, 29), dc)) * 0.75f);   // el campo de distancia llega hasta 30 m
            switch (Uso(x, z))
            {
                case UsoLote.Casco: case UsoLote.Posta: case UsoLote.Potrero: f = Mathf.Max(f, 0.9f); break;   // comido y pisoteado
                case UsoLote.Quinta: f = Mathf.Max(f, 0.6f); break;
                case UsoLote.Chacra: f = Mathf.Max(f, 0.45f); break;   // las orillas de la chacra las come el caballo del chacarero
            }
            float n = Ruido(x * 0.03 + 3, z * 0.03 - 7);
            return Mathf.Clamp01(f * Mix(0.6f, 1.35f, n));
        }

        public static bool LibreDeSitios(float x, float z, float margen = 1)
        {
            foreach (var s in SITIOS)
                if (Vector2.Distance(new Vector2(x, z), s.p) < s.r * margen) return false;
            return true;
        }

        /// Pesos de las capas del suelo (pasto verde, pasto seco, tierra, agua), como el shader del prototipo.
        public static Vector4 PesosSuelo(float x, float z)
        {
            Asegurar();
            float dc = caminos.Muestra(x, z);
            float da = arroyo.Muestra(x, z) - 4.5f;
            float dAgua = Mathf.Min(da, DistLaguna(x, z));
            float gRaw = 1 - SStep(-1, 1, dc);
            foreach (var pt in patios)   // patios de tierra donde terminan los caminos
            {
                float dp = Vector2.Distance(new Vector2(x, z), pt);
                if (dp < 14) gRaw = Mathf.Max(gRaw, 1 - SStep(6, 14, dp + 3 * (Ruido(x * 0.3, z * 0.3) - 0.5f)));
            }
            float g = Mathf.Clamp01(gRaw * 1.3f);
            float rSoft = 1 - SStep(-1.5f, 1.5f, dAgua);
            float agua = Mathf.Clamp01(rSoft * (1 - gRaw) * 1.6f);
            float orilla = (1 - SStep(0, 3, dAgua)) * (1 - rSoft);
            float n1 = Ruido(x * 0.012, z * 0.012);
            float paja = SStep(0.52f, 0.82f, n1) * 0.7f;
            float seco = SStep(0.78f, 0.95f, n1) * 0.6f;

            var w = new Vector4(1, 0, 0, 0);
            Hacia(ref w, 1, paja);
            Hacia(ref w, 2, seco);
            Hacia(ref w, 2, g);
            Hacia(ref w, 2, orilla * 0.8f);
            Hacia(ref w, 3, agua);
            return w;
        }

        static void Hacia(ref Vector4 w, int k, float f)
        {
            w *= 1 - f;
            w[k] += f;
        }
    }
}
