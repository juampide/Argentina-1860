using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Límites de los lotes del loteo (LoteoPago), como se marcaban en 1867, sin alambre:
    /// - Zanja: la zanja va por afuera (el terreno se hunde ahí, ver RelievePago) y la tierra sacada forma
    ///   un bordo empastado del lado de adentro.
    /// - Cerco vivo: bordo con una hilera de tunas encima (quintas).
    /// - Mojones: un poste con piedras en cada esquina (lotes sin ocupar, límites entre campos).
    /// - Tranqueras de madera donde el lote se abre al camino, con la zanja cortada.
    /// Todo sale del loteo: para cambiarlo, se edita Resources/LoteoPago.asset y se rearma esta pieza
    /// (clic derecho en el componente → Regenerar).
    /// </summary>
    public class Lindes : ProceduralBase
    {
        [Tooltip("Alto del bordo de tierra, en metros")]
        public float altoBordo = 0.5f;
        [Tooltip("Ancho del bordo, en metros")]
        public float anchoBordo = 2.2f;
        [Tooltip("Distancia entre tunas del cerco vivo, en metros")]
        public float pasoTuna = 1.3f;
        [Tooltip("Las tranqueras quedan abiertas (si no, cierran el paso)")]
        public bool tranquerasAbiertas = true;

        const float PASO = 2f, HUECO = 3.5f;
        const string TUNA = "#5d7a3a", TUNA_CLARA = "#7a9548", PIEDRA = "#8a8378";

        static string Mat(string nombre, string color)
        {
            var mm = MaterialesMundo.Instancia;
            return mm != null && mm.Buscar(nombre) != null ? nombre : color;
        }

        Vector3 Suelo(Vector2 m, float dy)
        {
            var w = new Vector3(m.x, 0, -m.y);
            w.y = Terreno.Altura(w) + dy;
            return transform.InverseTransformPoint(w);
        }

        protected override void Construir(Transform raiz)
        {
            var loteo = LoteoPago.Instancia;
            if (loteo == null) return;
            string BORDO = Mat("bordo", "#8d8a55"), MADERA = Mat("madera", "#6b4a2e");
            int n = 0;
            foreach (var lote in loteo.lotes)
            {
                if (lote.km == null || lote.km.Length < 3) continue;
                var poli = new List<Vector2>();
                foreach (var k in lote.km) poli.Add(MapaPago.K(k));
                var tranq = new List<Vector2>();
                if (lote.tranqueras != null) foreach (var k in lote.tranqueras) tranq.Add(MapaPago.K(k));
                // sentido del polígono: el bordo va hacia adentro
                float area = 0;
                for (int i = 0, j = poli.Count - 1; i < poli.Count; j = i++) area += (poli[j].x - poli[i].x) * (poli[j].y + poli[i].y);
                float adentro = area > 0 ? 1 : -1;

                var obra = new ObraMalla { metrosTextura = 3 };
                switch (lote.linde)
                {
                    case Linde.Zanja:
                    case Linde.CercoVivo:
                        Bordo(obra, poli, tranq, adentro, BORDO, lote.linde == Linde.CercoVivo);
                        break;
                    case Linde.Mojones:
                        foreach (var v in poli) Mojon(obra, v, MADERA);
                        break;
                }
                foreach (var t in tranq) Tranquera(obra, poli, t, MADERA);
                if (obra.Triangulos > 0) obra.Instanciar($"linde {n++} · {lote.nombre}", raiz, Registrar);
            }
        }

        /// Recorre el borde del polígono en pasos cortos y llama a f(punto, dirección, normal hacia adentro, ¿hay hueco?).
        static void Recorrer(List<Vector2> poli, List<Vector2> tranq, float adentro, System.Action<Vector2, Vector2, Vector2, Vector2, Vector2, bool> f)
        {
            for (int i = 0; i < poli.Count; i++)
            {
                var a = poli[i]; var b = poli[(i + 1) % poli.Count];
                float l = Vector2.Distance(a, b);
                if (l < 0.1f) continue;
                var d = (b - a) / l;
                var nIn = new Vector2(-d.y, d.x) * adentro;
                int pasos = Mathf.Max(1, Mathf.CeilToInt(l / PASO));
                for (int k = 0; k < pasos; k++)
                {
                    var p0 = Vector2.Lerp(a, b, (float)k / pasos);
                    var p1 = Vector2.Lerp(a, b, (float)(k + 1) / pasos);
                    var pm = (p0 + p1) / 2;
                    bool hueco = false;
                    foreach (var t in tranq) if (Vector2.Distance(pm, t) < HUECO) hueco = true;
                    f(p0, p1, d, nIn, pm, hueco);
                }
            }
        }

        void Bordo(ObraMalla o, List<Vector2> poli, List<Vector2> tranq, float adentro, string mat, bool tunas)
        {
            float w = anchoBordo / 2;
            int nTuna = 0;
            Recorrer(poli, tranq, adentro, (p0, p1, d, nIn, pm, hueco) =>
            {
                if (hueco) return;
                // corte del bordo: lomo redondeado del lado de adentro de la zanja
                float[] lados = { 0.1f, w * 0.45f, w, w * 1.55f, w * 2f };
                float[] altos = { -0.08f, altoBordo * 0.75f, altoBordo, altoBordo * 0.7f, -0.08f };
                for (int c = 0; c + 1 < lados.Length; c++)
                {
                    float r0 = Ruido(p0) * 0.12f, r1 = Ruido(p1) * 0.12f;
                    var a0 = Suelo(p0 + nIn * lados[c], altos[c] + (c > 0 ? r0 : 0));
                    var a1 = Suelo(p1 + nIn * lados[c], altos[c] + (c > 0 ? r1 : 0));
                    var b0 = Suelo(p0 + nIn * lados[c + 1], altos[c + 1] + (c + 1 < lados.Length - 1 ? r0 : 0));
                    var b1 = Suelo(p1 + nIn * lados[c + 1], altos[c + 1] + (c + 1 < lados.Length - 1 ? r1 : 0));
                    Cara(o, mat, a0, a1, b1, b0);
                }
                if (tunas)
                {
                    float largo = Vector2.Distance(p0, p1);
                    for (float s = 0; s < largo; s += pasoTuna)
                    {
                        var q = Vector2.Lerp(p0, p1, s / largo) + nIn * (w + (MapaPago.Hs(nTuna * 3.1) - 0.5f) * 0.4f);
                        Tuna(o, Suelo(q, altoBordo * 0.9f), nTuna++);
                    }
                }
            });
        }

        static float Ruido(Vector2 p) => (MapaPago.Ruido(p.x * 0.35, p.y * 0.35) - 0.5f) * 2;

        /// Cuadro con la cara hacia arriba, sea cual sea el orden en que llegan los puntos.
        static void Cara(ObraMalla o, string mat, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var nrm = Vector3.Cross(b - a, c - a);
            if (nrm.y >= 0) o.Quad(mat, a, b, c, d);
            else o.Quad(mat, a, d, c, b);
        }

        /// Tuna: unas paletas aplastadas, una sobre otra.
        static void Tuna(ObraMalla o, Vector3 b, int sem)
        {
            int paletas = 4 + (int)(MapaPago.Hs(sem * 1.7) * 4);
            var p = b;
            for (int k = 0; k < paletas; k++)
            {
                float ang = MapaPago.Hs(sem * 5.3 + k) * Mathf.PI * 2, alto = 0.35f + 0.15f * MapaPago.Hs(sem + k * 2.1);
                var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                var lado = new Vector3(-dir.z, 0, dir.x);
                var c = (k == 0 ? b : p + dir * 0.15f) + new Vector3(0, k == 0 ? 0 : alto * 0.4f, 0);
                var arriba = c + new Vector3(0, alto, 0) + dir * 0.12f;
                string col = k % 2 == 0 ? TUNA : TUNA_CLARA;
                var m = c + new Vector3(0, alto / 2, 0);
                float an = 0.22f, gr = 0.05f;
                // rombo grueso: cuatro caras de cada lado
                o.Tri(col, c, m + lado * an + dir * gr, arriba); o.Tri(col, c, arriba, m - lado * an + dir * gr);
                o.Tri(col, c, arriba, m + lado * an - dir * gr); o.Tri(col, c, m - lado * an - dir * gr, arriba);
                if (k > 0 && MapaPago.Hs(sem * 9.1 + k) < 0.5f) p = arriba - new Vector3(0, alto * 0.3f, 0);
            }
        }

        void Mojon(ObraMalla o, Vector2 v, string madera)
        {
            // poste de ñandubay con un montoncito de piedras al pie
            var b = Suelo(v, 0);
            const float r = 0.09f, h = 1.1f;
            for (int k = 0; k < 4; k++)
            {
                float a0 = k * Mathf.PI / 2 + 0.3f, a1 = (k + 1) * Mathf.PI / 2 + 0.3f;
                var d0 = new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
                var d1 = new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
                o.Quad(madera, b + d0, b + d0 + Vector3.up * h, b + d1 + Vector3.up * h, b + d1);
            }
            for (int k = 0; k < 5; k++)
            {
                float ang = k * 1.25f + v.x, dist = 0.25f;
                var c = b + new Vector3(Mathf.Cos(ang) * dist, 0, Mathf.Sin(ang) * dist);
                var top = c + new Vector3(0, 0.18f, 0);
                for (int q = 0; q < 4; q++)
                {
                    float a0 = q * Mathf.PI / 2 + ang, a1 = (q + 1) * Mathf.PI / 2 + ang;
                    o.Tri(PIEDRA, top, c + new Vector3(Mathf.Cos(a0) * 0.15f, 0, Mathf.Sin(a0) * 0.15f), c + new Vector3(Mathf.Cos(a1) * 0.15f, 0, Mathf.Sin(a1) * 0.15f));
                }
            }
        }

        /// Tranquera de madera: dos postes gruesos y una hoja de cinco tablas con su diagonal.
        void Tranquera(ObraMalla o, List<Vector2> poli, Vector2 t, string madera)
        {
            // dirección del borde más cercano
            Vector2 dir = Vector2.right; float mejor = 1e9f;
            for (int i = 0; i < poli.Count; i++)
            {
                var a = poli[i]; var b = poli[(i + 1) % poli.Count];
                var ab = b - a; float l2 = ab.sqrMagnitude; if (l2 < 1e-6f) continue;
                float u = Mathf.Clamp01(Vector2.Dot(t - a, ab) / l2);
                float d = Vector2.Distance(t, a + ab * u);
                if (d < mejor) { mejor = d; dir = ab.normalized; }
            }
            const float ancho = 3.6f;
            var pA = t - dir * ancho / 2; var pB = t + dir * ancho / 2;
            Poste(o, Suelo(pA, 0), 0.14f, 1.7f, madera);
            Poste(o, Suelo(pB, 0), 0.14f, 1.7f, madera);
            // la hoja gira sobre el poste A
            var bisagra = Suelo(pA, 0);
            float giro = tranquerasAbiertas ? 1.25f : 0;
            var d3 = new Vector3(dir.x, 0, -dir.y);
            var abre = Quaternion.AngleAxis(giro * Mathf.Rad2Deg, Vector3.up) * d3;
            float largo = ancho - 0.25f;
            for (int k = 0; k < 5; k++)
            {
                float y = 0.3f + k * 0.28f;
                Tabla(o, bisagra + abre * 0.12f + Vector3.up * y, bisagra + abre * largo + Vector3.up * y, 0.11f, 0.04f, madera);
            }
            Tabla(o, bisagra + abre * 0.15f + Vector3.up * 0.3f, bisagra + abre * largo + Vector3.up * 1.42f, 0.1f, 0.04f, madera);   // diagonal
            Tabla(o, bisagra + abre * largo + Vector3.up * 0.25f, bisagra + abre * largo + Vector3.up * 1.5f, 0.1f, 0.05f, madera);   // larguero
        }

        static void Poste(ObraMalla o, Vector3 b, float r, float h, string mat)
        {
            for (int k = 0; k < 5; k++)
            {
                float a0 = k * Mathf.PI * 2 / 5, a1 = (k + 1) * Mathf.PI * 2 / 5;
                var d0 = new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
                var d1 = new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
                o.Quad(mat, b + d0, b + d0 + Vector3.up * h, b + d1 + Vector3.up * h, b + d1);
            }
        }

        /// Tabla entre dos puntos: una caja angosta (alto x grueso).
        static void Tabla(ObraMalla o, Vector3 a, Vector3 b, float alto, float grueso, string mat)
        {
            var d = (b - a).normalized;
            var lado = Vector3.Cross(Vector3.up, d).normalized * grueso / 2;
            var up = Vector3.Cross(d, lado).normalized * alto / 2;
            Vector3[] c = { -lado - up, lado - up, lado + up, -lado + up };
            for (int k = 0; k < 4; k++)
            {
                var c0 = c[k]; var c1 = c[(k + 1) % 4];
                o.Quad(mat, a + c0, b + c0, b + c1, a + c1);
            }
        }
    }
}
