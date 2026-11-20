using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Lo que se ve más allá del terreno, para que el pago no termine en un corte: una franja alrededor del mapa
    /// que sigue lo que hay en cada borde. Al norte sigue el bañado (agua baja y matas de junco), al este el monte
    /// (copas tupidas, con la picada del camino real), y al sur y al oeste la pampa abierta de tierra adentro.
    /// No se puede caminar: los bordes del pago (BordesPago) frenan al jugador antes. Se rearma sola.
    /// </summary>
    public class PampaAfuera : ProceduralBase
    {
        [Tooltip("Cuántos metros sigue el paisaje más allá del terreno")]
        [Min(50)] public float ancho = 170;
        [Tooltip("Lado de cada cuadro del suelo, en metros")]
        [Min(5)] public float celda = 16;

        const float X0 = BordesPago.XMIN, X1 = BordesPago.XMAX, Z0 = BordesPago.ZMIN, Z1 = BordesPago.ZMAX;

        static float N(double x, double z) => MapaPago.Ruido(x, z);

        enum Lado { Norte, Este, Sur, Oeste }

        // caminos que salen del mapa: punto de salida y dirección (metros del prototipo)
        readonly List<(Vector2 p, Vector2 dir, string mat)> salidas = new List<(Vector2, Vector2, string)>();

        protected override void Construir(Transform raiz)
        {
            BuscarSalidas();
            var suelo = new ObraMalla { metrosTextura = 5 };
            var copas = new Dictionary<(int, int), ObraMalla>();
            ObraMalla Trozo(float x, float z)
            {
                var k = (Mathf.FloorToInt(x / 300), Mathf.FloorToInt(z / 300));
                if (!copas.TryGetValue(k, out var m)) copas[k] = m = new ObraMalla { metrosTextura = 6 };
                return m;
            }

            // suelo: una grilla alrededor del rectángulo del terreno
            float a = ancho, c = celda;
            for (float x = X0 - a; x < X1 + a - 0.01f; x += c)
                for (float z = Z0 - a; z < Z1 + a - 0.01f; z += c)
                {
                    float x2 = Mathf.Min(x + c, X1 + a), z2 = Mathf.Min(z + c, Z1 + a);
                    if (x >= X0 && x2 <= X1 && z >= Z0 && z2 <= Z1) continue;   // adentro está el terreno
                    // los cuadros que pisan el borde del terreno se recortan para no meterse debajo
                    var cuadros = Recortar(x, z, x2, z2);
                    foreach (var (qa, qb, qc, qd) in cuadros)
                    {
                        float mx = (qa + qc) / 2, mz = (qb + qd) / 2;
                        string mat = Material(mx, mz);
                        var p0 = P(qa, qb); var p1 = P(qc, qb); var p2 = P(qc, qd); var p3 = P(qa, qd);
                        suelo.Quad(mat, p0, p1, p2, p3);
                    }
                }

            // copas del monte (este) y matas del juncal (norte): un manto grumoso sobre el suelo
            const float cm = 6.5f;
            for (float x = X0 - a; x < X1 + a; x += cm)
                for (float z = Z0 - a; z < Z1 + a; z += cm)
                {
                    float mx = x + cm / 2, mz = z + cm / 2;
                    if (mx >= X0 && mx <= X1 && mz >= Z0 && mz <= Z1) continue;
                    var lado = LadoDe(mx, mz);
                    if (EnSalida(mx, mz, 7)) continue;
                    if (lado == Lado.Este)
                    {
                        float alto = 5.5f + 2.5f * N(mx * 0.05, mz * 0.05);
                        Copa(Trozo(mx, mz), mx, mz, cm * 0.62f, alto, Verde(mx, mz));
                    }
                    else if (lado == Lado.Norte && N(mx * 0.03 + 3, mz * 0.03 - 4) < 0.5f)
                    {
                        float alto = 1.1f + 0.7f * N(mx * 0.2, mz * 0.2);
                        Copa(Trozo(mx, mz), mx, mz, cm * 0.5f, alto, N(mx * 0.1, mz * 0.1) < 0.5f ? "#6f7a3c" : "#857f45");
                    }
                }

            var gs = suelo.Instanciar("suelo de afuera", raiz, Registrar, false);
            if (gs != null) foreach (var r in gs.GetComponentsInChildren<MeshRenderer>()) r.receiveShadows = true;
            foreach (var kv in copas) kv.Value.Instanciar("monte y juncal de afuera " + kv.Key, raiz, Registrar, true);
        }

        // ---------- forma ----------

        /// Altura del suelo de afuera: en el borde, igual que el terreno; más lejos, el relieve del pago sigue solo.
        Vector3 P(float x, float z)
        {
            float cx = Mathf.Clamp(x, X0, X1), cz = Mathf.Clamp(z, Z0, Z1);
            float borde = Terreno.Altura(new Vector3(cx, 0, -cz));
            float lejos = MapaPago.Altura(x, z);
            float d = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(z - cz));
            float h = Mathf.Lerp(borde, lejos, MapaPago.SStep(0, 60, d));
            if (d < 0.5f) h -= 0.08f;   // apenas debajo del terreno en la costura
            return new Vector3(x, h, -z);
        }

        /// Parte un cuadro que pisa el rectángulo del terreno en los pedazos que quedan afuera.
        static List<(float, float, float, float)> Recortar(float x, float z, float x2, float z2)
        {
            var l = new List<(float, float, float, float)>();
            bool pisa = x < X1 && x2 > X0 && z < Z1 && z2 > Z0;
            if (!pisa) { l.Add((x, z, x2, z2)); return l; }
            if (x < X0) l.Add((x, z, X0, z2));
            if (x2 > X1) l.Add((X1, z, x2, z2));
            float xa = Mathf.Max(x, X0), xb = Mathf.Min(x2, X1);
            if (z < Z0) l.Add((xa, z, xb, Z0));
            if (z2 > Z1) l.Add((xa, Z1, xb, z2));
            return l;
        }

        static Lado LadoDe(float x, float z)
        {
            // en las esquinas manda el lado más cercano
            float dn = Z0 - z, ds = z - Z1, de = x - X1, dw = X0 - x;
            float m = Mathf.Max(Mathf.Max(dn, ds), Mathf.Max(de, dw));
            if (m == de) return Lado.Este;
            if (m == dn) return Lado.Norte;
            if (m == ds) return Lado.Sur;
            return Lado.Oeste;
        }

        string Material(float x, float z)
        {
            foreach (var s in salidas) if (Dist(s, x, z) < 5) return s.mat;
            switch (LadoDe(x, z))
            {
                case Lado.Norte: return N(x * 0.03 + 3, z * 0.03 - 4) < 0.5f ? "barro" : "charco";
                case Lado.Este: return "barro";
                default: return "pampa";
            }
        }

        static string Verde(float x, float z)
        {
            float n = N(x * 0.08 + 11, z * 0.08);
            return n < 0.33f ? "#3b5130" : n < 0.66f ? "#465c33" : "#53653a";
        }

        /// Una copa: pirámide baja y facetada, como las de los árboles del monte vistas desde arriba.
        void Copa(ObraMalla m, float x, float z, float r, float alto, string mat)
        {
            float y = P(x, z).y;
            float jx = (N(x * 0.7, z * 0.7) - 0.5f) * r * 0.6f, jz = (N(x * 0.7 + 5, z * 0.7) - 0.5f) * r * 0.6f;
            var cima = new Vector3(x + jx, y + alto, -(z + jz));
            var b = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI / 3 + N(x, z) * 2;
                float rr = r * (0.8f + 0.4f * N(x * 1.3 + i, z * 1.3));
                b[i] = new Vector3(x + Mathf.Cos(ang) * rr, y + alto * 0.45f, -(z + Mathf.Sin(ang) * rr));
            }
            for (int i = 0; i < 6; i++)
            {
                var p = b[i]; var q = b[(i + 1) % 6];
                m.Tri(mat, cima, p, q);
                var pb = new Vector3(p.x, y, p.z); var qb = new Vector3(q.x, y, q.z);
                m.Quad(mat, p, pb, qb, q);
            }
        }

        // ---------- caminos que salen ----------

        void BuscarSalidas()
        {
            salidas.Clear();
            var l = LoteoPago.Instancia;
            if (l == null) return;
            foreach (var cm in l.caminos)
            {
                if (cm.km == null || cm.km.Length < 2) continue;
                string mat = cm.tipo == TipoCamino.Real ? "camino" : "huella";
                foreach (var (a, b) in new[] { (cm.km[0], cm.km[1]), (cm.km[cm.km.Length - 1], cm.km[cm.km.Length - 2]) })
                {
                    var p = MapaPago.K(a); var q = MapaPago.K(b);
                    bool sale = p.x <= X0 + 30 || p.x >= X1 - 30 || p.y <= Z0 + 30 || p.y >= Z1 - 30;
                    if (!sale) continue;
                    salidas.Add((p, (p - q).normalized, mat));
                }
            }
        }

        static float Dist((Vector2 p, Vector2 dir, string mat) s, float x, float z)
        {
            var v = new Vector2(x, z) - s.p;
            float t = Vector2.Dot(v, s.dir);
            if (t < -5) return float.MaxValue;
            return (v - s.dir * t).magnitude;
        }

        bool EnSalida(float x, float z, float r)
        {
            foreach (var s in salidas) if (Dist(s, x, z) < r) return true;
            return false;
        }
    }
}
