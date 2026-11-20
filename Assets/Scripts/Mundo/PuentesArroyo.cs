using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Los puentes del arroyo: donde un camino del loteo cruza el agua, un puente precario de campaña.
    /// Tablones desparejos (alguno falta o está torcido) sobre tres largueros de tronco, pilotes clavados
    /// en el barro de a pares con su cabezal, y una baranda de palos atados, con algún poste caído.
    /// Los cruces se buscan solos a partir de los caminos de LoteoPago y del arroyo de MapaPago: si cambia el
    /// trazado, se rearma. El jugador y el caballo pasan por arriba (tiene colisión) y la baranda no los deja caer.
    /// </summary>
    public class PuentesArroyo : ProceduralBase
    {
        [Tooltip("Ancho del puente del camino real (los callejones y sendas lo tienen más angosto)")]
        [Min(2)] public float anchoReal = 4.4f;
        [Tooltip("Cuánto sigue el puente sobre la tierra a cada lado del agua")]
        [Min(0)] public float apoyo = 8f;
        [Range(0, 0.3f)] public float faltan = 0.06f;
        public int semilla = 7;

        public struct Cruce { public Vector2 a, b; public float ancho; }

        static float H(double i) => MapaPago.Hs(i * 1.93 + 4441);
        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);

        /// Busca dónde cada camino entra y sale del agua del arroyo (metros del prototipo).
        public static List<Cruce> Cruces(float anchoReal, float apoyo)
        {
            var l = new List<Cruce>();
            var loteo = LoteoPago.Instancia;
            if (loteo == null) return l;
            foreach (var cm in loteo.caminos)
            {
                if (cm.km == null || cm.km.Length < 2) continue;
                var pts = MapaPago.Suavizar(cm.km);
                float ancho = cm.tipo == TipoCamino.Real ? anchoReal : Mathf.Max(2.2f, LoteoPago.MedioAncho(cm.tipo) * 1.6f);
                bool dentro = false; Vector2 entra = default;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    var p = MapaPago.K(pts[i]); var q = MapaPago.K(pts[i + 1]);
                    float d = Vector2.Distance(p, q);
                    int n = Mathf.Max(1, Mathf.CeilToInt(d));
                    for (int k = 0; k < n; k++)
                    {
                        var u = Vector2.Lerp(p, q, (float)k / n);
                        bool agua = MapaPago.EnArroyo(u.x, u.y);
                        if (agua && !dentro) { dentro = true; entra = u; }
                        if (!agua && dentro)
                        {
                            dentro = false;
                            var dir = (u - entra).normalized;
                            if (dir.sqrMagnitude > 0.5f) l.Add(new Cruce { a = entra - dir * apoyo, b = u + dir * apoyo, ancho = ancho });
                        }
                    }
                }
            }
            return l;
        }

        /// Hunde el cauce debajo de cada puente y lo pinta como agua (el camino lo había dejado a nivel, como un vado), para que el puente
        /// cruce sobre el agua y no quede apoyado en el suelo. Toca solo el terreno alrededor de los cruces.
        /// Se corre una vez (desde el MCP o un menú); correrlo de nuevo no vuelve a hundir.
        public static void HundirCauces(Terrain t, float anchoReal, float apoyo, float hondo = 1.3f)
        {
            var td = t.terrainData; var pos = t.transform.position; var size = td.size;
            int res = td.heightmapResolution;
            var h = td.GetHeights(0, 0, res, res);
            foreach (var c in Cruces(anchoReal, apoyo))
            {
                var dir = (c.b - c.a).normalized; var lado = new Vector2(-dir.y, dir.x);
                var centro = (c.a + c.b) / 2; float L = Vector2.Distance(c.a, c.b);
                float orilla = Mathf.Max(Terreno.Altura(new Vector3(c.a.x, 0, -c.a.y)), Terreno.Altura(new Vector3(c.b.x, 0, -c.b.y)));
                for (int i = 0; i < res; i++)
                    for (int j = 0; j < res; j++)
                    {
                        float x = pos.x + (float)j / (res - 1) * size.x, z = -(pos.z + (float)i / (res - 1) * size.z);
                        var r = new Vector2(x, z) - centro;
                        float a = Mathf.Abs(Vector2.Dot(r, dir)), l = Mathf.Abs(Vector2.Dot(r, lado));
                        if (a > L / 2 + 6 || l > c.ancho / 2 + 22) continue;
                        float d = MapaPago.DistArroyo(x, z);
                        float prof = hondo * (1 - MapaPago.SStep(3.5f, 7.5f, d)) * (1 - MapaPago.SStep(c.ancho / 2 + 4, c.ancho / 2 + 22, l));   // baja suave hacia los costados
                        if (prof < 0.01f) continue;
                        float objetivo = (orilla - prof - pos.y) / size.y;
                        h[i, j] = Mathf.Min(h[i, j], objetivo);
                    }
            }
            td.SetHeights(0, 0, h);

            // el agua sigue corriendo por debajo: el cauce se pinta como agua aunque pase el camino por arriba
            int ar = td.alphamapResolution;
            var al = td.GetAlphamaps(0, 0, ar, ar);
            foreach (var c in Cruces(anchoReal, apoyo))
            {
                var dir = (c.b - c.a).normalized; var lado = new Vector2(-dir.y, dir.x);
                var centro = (c.a + c.b) / 2; float L = Vector2.Distance(c.a, c.b);
                for (int i = 0; i < ar; i++)
                    for (int j = 0; j < ar; j++)
                    {
                        float x = pos.x + (j + 0.5f) / ar * size.x, z = -(pos.z + (i + 0.5f) / ar * size.z);
                        var r = new Vector2(x, z) - centro;
                        if (Mathf.Abs(Vector2.Dot(r, dir)) > L / 2 || Mathf.Abs(Vector2.Dot(r, lado)) > c.ancho / 2 + 12) continue;
                        float d = MapaPago.DistArroyo(x, z);
                        float agua = 1 - MapaPago.SStep(3.5f, 5.5f, d);
                        float orilla = (1 - MapaPago.SStep(5.5f, 8f, d)) * (1 - agua);   // barro en la orilla
                        if (agua + orilla < 0.01f) continue;
                        for (int k = 0; k < 4; k++) al[i, j, k] *= 1 - agua - orilla * 0.7f;
                        al[i, j, 3] += agua;
                        al[i, j, 2] += orilla * 0.7f;
                    }
            }
            td.SetAlphamaps(0, 0, al);
        }

        protected override void Construir(Transform raiz)
        {
            // las coordenadas MapaPago (prototipo) están en metros del mundo; este objeto debe quedar en el origen
            int k = semilla * 1000;
            foreach (var c in Cruces(anchoReal, apoyo))
            {
                var n = new Nodo("puente");
                Puente(n, raiz, c, ref k);
                n.Instanciar(raiz, Registrar);
            }
        }

        float Suelo(Vector2 p) => Terreno.Altura(new Vector3(p.x, 0, -p.y)) - transform.position.y;

        void Puente(Nodo n, Transform raiz, Cruce c, ref int k)
        {
            var dir = (c.b - c.a).normalized;
            var lado = new Vector2(-dir.y, dir.x);
            float L = Vector2.Distance(c.a, c.b), w = c.ancho;
            float yA = Suelo(c.a), yB = Suelo(c.b);
            float Y(float t) => Mathf.Lerp(yA, yB, t) + 0.22f;                  // la cubierta, apenas sobre las cabeceras
            Vector3 P3(Vector2 p, float y) => v(p.x, y, p.y);                     // espacio del prototipo
            float rumbo = Mathf.Atan2(dir.x, dir.y);                              // giro del prototipo (eje z a lo largo)

            // largueros: tres troncos a lo largo
            foreach (var s in new[] { -0.36f, 0f, 0.36f })
            {
                var a = c.a + lado * s * w; var b = c.b + lado * s * w;
                Palo(n, P3(a, Y(0) - 0.2f), P3(b, Y(1) - 0.2f), 0.13f, "#5a4632");
            }

            // tablones atravesados, desparejos
            for (float s = 0.2f; s < L - 0.1f; s += 0.36f)
            {
                k++;
                if (H(k) < faltan && s > 1 && s < L - 1) continue;               // falta un tablón
                float t = s / L;
                var m = c.a + dir * s + lado * (H(k + 0.3) - 0.5f) * 0.25f;
                float largo = w * (0.92f + 0.16f * H(k + 0.5));
                string mat = H(k + 0.7) < 0.55f ? "madera" : H(k + 0.9) < 0.5f ? "#9a8a72" : "#7d6c55";
                n.Pieza(Geo.Caja(largo, 0.06f, 0.3f), mat, P3(m, Y(t) + (H(k + 1.1) - 0.5f) * 0.04f), v((H(k + 1.3) - 0.5f) * 0.05f, rumbo + (H(k + 1.5) - 0.5f) * 0.12f, (H(k + 1.7) - 0.5f) * 0.04f));
            }

            // pilotes de a pares con su cabezal, cada unos 3 m sobre el agua
            int np = Mathf.Max(1, Mathf.RoundToInt((L - 2 * apoyo) / 3f));
            for (int i = 0; i <= np; i++)
            {
                float s = apoyo + (L - 2 * apoyo) * i / Mathf.Max(1, np);
                float t = s / L;
                var m = c.a + dir * s;
                float fondo = Suelo(m) - 0.8f;
                foreach (var sl in new[] { -0.45f, 0.45f })
                {
                    var p = m + lado * sl * w;
                    Palo(n, P3(p, fondo), P3(p + dir * (H(++k) - 0.5f) * 0.2f, Y(t) - 0.3f), 0.14f, "#4a3a2a");
                }
                Palo(n, P3(m - lado * 0.55f * w, Y(t) - 0.33f), P3(m + lado * 0.55f * w, Y(t) - 0.33f), 0.11f, "#4a3a2a");   // cabezal
            }

            // baranda: postes cada 1,6 m, pasamanos atado; algún poste caído y algún tramo sin pasamanos
            foreach (var sl in new[] { -0.5f, 0.5f })
            {
                Vector3? prev = null;
                for (float s = 0.3f; s <= L - 0.2f; s += 1.6f)
                {
                    k++;
                    float t = s / L;
                    var p = c.a + dir * s + lado * sl * w;
                    bool caido = H(k) < 0.08f;
                    var pie = P3(p, Y(t) - 0.25f);
                    var cima = caido ? P3(p + lado * sl * 0.9f, Y(t) + 0.15f) : P3(p + lado * sl * (H(k + 0.2) - 0.3f) * 0.12f, Y(t) + 0.95f);
                    Palo(n, pie, cima, 0.06f, "#6a5640");
                    if (!caido)
                    {
                        if (prev.HasValue && H(k + 0.4) > 0.1f) Palo(n, prev.Value, cima, 0.045f, "#7d6c55");
                        prev = cima;
                    }
                    else prev = null;
                }
            }

            // colisiones: la cubierta (dos tramos por si las cabeceras no están a la misma altura) y las barandas
            var centro = (c.a + c.b) / 2;
            float inclina = Mathf.Atan2(yB - yA, L) * Mathf.Rad2Deg;
            var rot = Quaternion.LookRotation(new Vector3(dir.x, 0, -dir.y)) * Quaternion.Euler(-inclina, 0, 0);
            float yc = (Y(0) + Y(1)) / 2;
            Colision(raiz, new Vector3(centro.x, yc - 0.1f, -centro.y), new Vector3(w, 0.2f, L), rot);
            foreach (var sl in new[] { -0.5f, 0.5f })
            {
                var p = centro + lado * (sl * w + Mathf.Sign(sl) * 0.1f);
                Colision(raiz, new Vector3(p.x, yc + 0.6f, -p.y), new Vector3(0.25f, 1.4f, L), rot);
            }
        }

        static void Palo(Nodo n, Vector3 a, Vector3 b, float r, string mat)
        {
            var d = b - a;
            float L = d.magnitude;
            if (L < 0.01f) return;
            n.PiezaM(Geo.Cilindro(r * 0.85f, r, L, 5), mat, Matrix4x4.TRS((a + b) / 2, Quaternion.FromToRotation(Vector3.up, d / L), Vector3.one));
        }
    }
}
