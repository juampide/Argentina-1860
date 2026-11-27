using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Geometría low poly con la misma forma, medidas y orientación que las de three.js del prototipo
    /// (CylinderGeometry, ConeGeometry, BoxGeometry, IcosahedronGeometry, etc.).
    /// Trabaja en el espacio del prototipo; MallaTres la convierte a Unity al final.
    /// Es siempre una lista de triángulos sueltos para que el sombreado quede facetado.
    /// </summary>
    public class Geo
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();

        /// Si es true, las normales se suavizan entre caras vecinas con poca diferencia de ángulo (el resto queda con aristas duras).
        public bool suave;
        public Geo Suave() { suave = true; return this; }

        void T(Vector3 a, Vector3 b, Vector3 c) { T(a, b, c, Vector2.zero, Vector2.zero, Vector2.zero); }

        /// Agrega un triángulo suelto (orden antihorario visto desde afuera, como en three.js).
        public void Tri(Vector3 a, Vector3 b, Vector3 c) => T(a, b, c);

        /// Triángulo con UV propias.
        public void TriUV(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc) => T(a, b, c, ua, ub, uc);

        /// Cuadrilátero a-b-c-d (antihorario visto desde afuera) con UV propias.
        public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            T(a, b, c, ua, ub, uc);
            T(a, c, d, ua, uc, ud);
        }

        void T(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            v.Add(a); v.Add(b); v.Add(c);
            uv.Add(ua); uv.Add(ub); uv.Add(uc);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float w)
        {
            T(a, b, c, new Vector2(0, 0), new Vector2(u, 0), new Vector2(u, w));
            T(a, c, d, new Vector2(0, 0), new Vector2(u, w), new Vector2(0, w));
        }

        /// CylinderGeometry(radioArriba, radioAbajo, alto, segmentos, abierto)
        public static Geo Cilindro(float rs, float ri, float h, int seg = 6, bool abierto = false)
        {
            var g = new Geo();
            float hh = h / 2;
            var arriba = new Vector3[seg + 1];
            var abajo = new Vector3[seg + 1];
            for (int x = 0; x <= seg; x++)
            {
                float th = (float)x / seg * Mathf.PI * 2, s = Mathf.Sin(th), c = Mathf.Cos(th);
                arriba[x] = new Vector3(rs * s, hh, rs * c);
                abajo[x] = new Vector3(ri * s, -hh, ri * c);
            }
            for (int x = 0; x < seg; x++)
            {
                float u0 = (float)x / seg, u1 = (float)(x + 1) / seg;
                if (rs > 0) g.T(arriba[x], abajo[x], arriba[x + 1], new Vector2(u0, 1), new Vector2(u0, 0), new Vector2(u1, 1));
                if (ri > 0) g.T(abajo[x], abajo[x + 1], arriba[x + 1], new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1));
            }
            if (!abierto)
            {
                var ca = new Vector3(0, hh, 0);
                var cb = new Vector3(0, -hh, 0);
                for (int x = 0; x < seg; x++)
                {
                    if (rs > 0) g.T(arriba[x], arriba[x + 1], ca);
                    if (ri > 0) g.T(abajo[x + 1], abajo[x], cb);
                }
            }
            return g;
        }

        /// ConeGeometry(radio, alto, segmentos)
        public static Geo Cono(float r, float h, int seg = 4) => Cilindro(0, r, h, seg);

        /// BoxGeometry(ancho, alto, largo). Con esc > 0 las UV quedan a escala del mundo (1 repetición cada esc metros), como cajaUV.
        public static Geo Caja(float w, float h, float d, float esc = 0)
        {
            var g = new Geo();
            float hw = w / 2, hh = h / 2, hd = d / 2;
            float k = esc > 0 ? 1f / esc : 0f;
            g.Cara(new Vector3(hw, 0, 0), new Vector3(0, 0, -hd), new Vector3(0, hh, 0), d * k, h * k);
            g.Cara(new Vector3(-hw, 0, 0), new Vector3(0, 0, hd), new Vector3(0, hh, 0), d * k, h * k);
            g.Cara(new Vector3(0, hh, 0), new Vector3(hw, 0, 0), new Vector3(0, 0, -hd), w * k, d * k);
            g.Cara(new Vector3(0, -hh, 0), new Vector3(hw, 0, 0), new Vector3(0, 0, hd), w * k, d * k);
            g.Cara(new Vector3(0, 0, hd), new Vector3(hw, 0, 0), new Vector3(0, hh, 0), w * k, h * k);
            g.Cara(new Vector3(0, 0, -hd), new Vector3(-hw, 0, 0), new Vector3(0, hh, 0), w * k, h * k);
            return g;
        }

        // cara de caja: centro, semieje u, semieje v (u × v apunta hacia afuera)
        void Cara(Vector3 c, Vector3 u, Vector3 w, float du, float dv)
        {
            Quad(c - u - w, c + u - w, c + u + w, c - u + w, du, dv);
        }

        static readonly float FI = (1 + Mathf.Sqrt(5)) / 2;
        static readonly float[] ICO_V = { -1, FI, 0, 1, FI, 0, -1, -FI, 0, 1, -FI, 0, 0, -1, FI, 0, 1, FI, 0, -1, -FI, 0, 1, -FI, FI, 0, -1, FI, 0, 1, -FI, 0, -1, -FI, 0, 1 };
        static readonly int[] ICO_I = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
        static readonly float[] OCTA_V = { 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1 };
        static readonly int[] OCTA_I = { 0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2, 1, 2, 5, 1, 5, 3, 1, 3, 4, 1, 4, 2 };

        /// IcosahedronGeometry(radio, detalle) con detalle 0 o 1
        public static Geo Ico(float r, int detalle = 0) => Poliedro(ICO_V, ICO_I, r, detalle);

        /// OctahedronGeometry(radio)
        public static Geo Octa(float r) => Poliedro(OCTA_V, OCTA_I, r, 0);

        static Geo Poliedro(float[] vs, int[] idx, float r, int detalle)
        {
            var g = new Geo();
            Vector3 V(int i) => new Vector3(vs[i * 3], vs[i * 3 + 1], vs[i * 3 + 2]);
            for (int i = 0; i < idx.Length; i += 3)
            {
                Vector3 a = V(idx[i]), b = V(idx[i + 1]), c = V(idx[i + 2]);
                if (detalle <= 0) g.T(a.normalized * r, b.normalized * r, c.normalized * r);
                else
                {
                    Vector3 ab = (a + b) / 2, bc = (b + c) / 2, ca = (c + a) / 2;
                    foreach (var t in new[] { (a, ab, ca), (ab, b, bc), (ca, bc, c), (ab, bc, ca) })
                        g.T(t.Item1.normalized * r, t.Item2.normalized * r, t.Item3.normalized * r);
                }
            }
            return g;
        }

        /// TorusGeometry(radio, tubo, segmentosRadiales, segmentosTubulares, arco) en el plano XY
        public static Geo Toro(float R, float tubo, int radSeg, int tubSeg, float arco = Mathf.PI * 2)
        {
            var g = new Geo();
            var p = new Vector3[(radSeg + 1) * (tubSeg + 1)];
            for (int j = 0; j <= radSeg; j++)
                for (int i = 0; i <= tubSeg; i++)
                {
                    float u = (float)i / tubSeg * arco, w = (float)j / radSeg * Mathf.PI * 2;
                    p[j * (tubSeg + 1) + i] = new Vector3((R + tubo * Mathf.Cos(w)) * Mathf.Cos(u), (R + tubo * Mathf.Cos(w)) * Mathf.Sin(u), tubo * Mathf.Sin(w));
                }
            for (int j = 1; j <= radSeg; j++)
                for (int i = 1; i <= tubSeg; i++)
                {
                    int a = (tubSeg + 1) * j + i - 1, b = (tubSeg + 1) * (j - 1) + i - 1, c = (tubSeg + 1) * (j - 1) + i, d = (tubSeg + 1) * j + i;
                    g.T(p[a], p[b], p[d]);
                    g.T(p[b], p[c], p[d]);
                }
            return g;
        }

        /// Cilindro abierto con un arco parcial (CylinderGeometry con thetaLength), para lonas y techos curvos.
        public static Geo CilindroArco(float r, float h, int seg, float arco)
        {
            var g = new Geo();
            float hh = h / 2;
            for (int x = 0; x < seg; x++)
            {
                float t0 = (float)x / seg * arco, t1 = (float)(x + 1) / seg * arco;
                Vector3 a = new Vector3(r * Mathf.Sin(t0), hh, r * Mathf.Cos(t0)), b = new Vector3(r * Mathf.Sin(t0), -hh, r * Mathf.Cos(t0));
                Vector3 c = new Vector3(r * Mathf.Sin(t1), -hh, r * Mathf.Cos(t1)), d = new Vector3(r * Mathf.Sin(t1), hh, r * Mathf.Cos(t1));
                g.T(a, b, d);
                g.T(b, c, d);
            }
            return g;
        }

        public struct Seccion
        {
            public float x, y, z, rx, ry;
            public Seccion(float z, float rx, float ry, float y, float x = 0) { this.x = x; this.y = y; this.z = z; this.rx = rx; this.ry = ry; }
        }

        /// Cuerpo "loft": anillos de pocos lados a lo largo del eje z, unidos en facetas (loft() de animales.js).
        public static Geo Loft(Seccion[] sec, int lados)
        {
            var g = new Geo();
            Vector3 P(int k, int i)
            {
                float t = (float)(i % lados) / lados * Mathf.PI * 2;
                var q = sec[k];
                return new Vector3(q.x + Mathf.Cos(t) * q.rx, q.y + Mathf.Sin(t) * q.ry, q.z);
            }
            for (int k = 0; k < sec.Length - 1; k++)
                for (int i = 0; i < lados; i++)
                {
                    Vector3 A = P(k, i), B = P(k, i + 1), C = P(k + 1, i + 1), D = P(k + 1, i);
                    g.T(A, B, C);
                    g.T(A, C, D);
                }
            var c0 = new Vector3(sec[0].x, sec[0].y, sec[0].z);
            var c1 = new Vector3(sec[sec.Length - 1].x, sec[sec.Length - 1].y, sec[sec.Length - 1].z);
            int u = sec.Length - 1;
            for (int i = 0; i < lados; i++)
            {
                g.T(c0, P(0, i), P(0, i + 1));
                g.T(c1, P(u, i + 1), P(u, i));
            }
            return g;
        }

        /// Plano XY de ancho w y alto h (PlaneGeometry), mirando hacia +z.
        public static Geo Plano(float w, float h)
        {
            var g = new Geo();
            g.Quad(new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, -h / 2, 0), new Vector3(w / 2, h / 2, 0), new Vector3(-w / 2, h / 2, 0), 1, 1);
            return g;
        }

        /// Copia trasladada (como geometry.translate de three.js).
        public Geo Trasladar(float x, float y, float z)
        {
            var g = new Geo();
            var d = new Vector3(x, y, z);
            for (int i = 0; i < v.Count; i++) { g.v.Add(v[i] + d); g.uv.Add(uv[i]); }
            return g;
        }

        /// Techo a dos aguas con UV a escala de textura (geoTecho del prototipo)
        public static Geo Techo(float a, float l, float rise, float vol, float esc = 1.6f)
        {
            var g = new Geo();
            float hw = a / 2 + vol, hl = l / 2 + vol * 0.7f, sl = Mathf.Sqrt(hw * hw + rise * rise);
            g.Quad(new Vector3(-hw, 0, -hl), new Vector3(-hw, 0, hl), new Vector3(0, rise, hl), new Vector3(0, rise, -hl), 2 * hl / esc, sl / esc);
            g.Quad(new Vector3(hw, 0, hl), new Vector3(hw, 0, -hl), new Vector3(0, rise, -hl), new Vector3(0, rise, hl), 2 * hl / esc, sl / esc);
            float rg = rise * (a / 2) / hw;
            foreach (var z in new[] { l / 2, -l / 2 })
                g.T(new Vector3(-a / 2, 0, z), new Vector3(a / 2, 0, z), new Vector3(0, rg, z),
                    new Vector2(0, 0), new Vector2(a / esc, 0), new Vector2(a / esc / 2, rg / esc));
            return g;
        }
    }
}
