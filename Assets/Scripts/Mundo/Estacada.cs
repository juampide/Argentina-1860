using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Estacada de fortín de frontera: palo a pique de troncos desparejos con la punta afilada, clavados uno al
    /// lado del otro sobre un terraplén de tierra. Afuera, el foso: una zanja ancha con su bordo, que se cruza
    /// por un puente de tablones frente al portón. Los puntos están en metros, relativos al objeto (x, z de Unity),
    /// como en Cerco, y deben formar un polígono convexo en sentido horario visto desde arriba.
    /// El objeto no debe rotarse: para mover la estacada se mueve el objeto o los puntos.
    /// </summary>
    public class Estacada : ProceduralBase
    {
        public List<Vector2> puntos = new List<Vector2> { new Vector2(-14, -12), new Vector2(-14, 12), new Vector2(14, 12), new Vector2(14, -12) };
        [Tooltip("Lado con el portón (el lado i va del punto i al i+1); -1 = sin portón")]
        public int porton = 2;
        [Min(1)] public float alto = 2.8f;
        [Tooltip("Foso con bordo alrededor, y puente frente al portón")]
        public bool foso = true;
        [Min(1)] public float anchoFoso = 3.5f;

        const float GRUESO = 0.24f;   // separación entre palos
        const float ANCHO_PORTON = 3.2f;

        float y0;
        float Alt(Vector2 u) => Terreno.Altura(transform.TransformPoint(new Vector3(u.x, 0, u.y))) - y0;
        static float Hs(double i) => MapaPago.Hs(i * 2.31 + 77);

        protected override void Construir(Transform raiz)
        {
            if (puntos == null || puntos.Count < 3) return;
            y0 = transform.position.y;
            var n = new Nodo("estacada");
            int k = 0;
            for (int i = 0; i < puntos.Count; i++)
            {
                Vector2 a = puntos[i], b = puntos[(i + 1) % puntos.Count];
                float L = Vector2.Distance(a, b);
                if (L < 0.01f) continue;
                var dir = (b - a) / L;
                float medio = L / 2;
                int np = Mathf.CeilToInt(L / GRUESO);
                for (int j = 0; j < np; j++)
                {
                    float s = (j + 0.5f) * L / np;
                    if (i == porton && Mathf.Abs(s - medio) < ANCHO_PORTON / 2 + 0.2f) continue;
                    k++;
                    var p = a + dir * s;
                    float h = alto * (0.82f + 0.3f * Hs(k));
                    float r = 0.09f + 0.05f * Hs(k + 0.3);
                    string mat = Hs(k + 0.6) < 0.55f ? "madera" : Hs(k + 0.8) < 0.5f ? "#9a8a72" : "#7d6c55";
                    var inclina = new Vector3((Hs(k + 1.1) - 0.5f) * 0.07f, 0, (Hs(k + 1.3) - 0.5f) * 0.07f);
                    float y = Alt(p) + (foso ? 0.75f : 0) - 0.2f;
                    var pos3 = new Vector3(p.x, y, -p.y);
                    n.PiezaM(Geo.Cilindro(r, r * 1.08f, h, 5), mat, Matrix4x4.TRS(pos3 + new Vector3(0, h / 2, 0), Conv.Q3(inclina), Vector3.one));
                    n.PiezaM(Geo.Cono(r, 0.32f + 0.15f * Hs(k + 2), 5), mat, Matrix4x4.TRS(pos3 + new Vector3(0, h + 0.15f, 0), Conv.Q3(inclina), Vector3.one));
                }
                // travesaño atado por dentro, a media altura
                var c = (a + b) / 2;
                float ang = Mathf.Atan2(b.x - a.x, -(b.y - a.y));
                var adentro = new Vector2(dir.y, -dir.x) * 0.22f;   // horario: la derecha del lado mira hacia adentro
                foreach (var f in new[] { 0.45f, 0.8f })
                {
                    var cc = c + adentro;
                    n.PiezaM(Geo.Caja(0.1f, 0.12f, L), "#6a5640", Matrix4x4.TRS(new Vector3(cc.x, Alt(cc) + alto * f + (foso ? 0.55f : 0), -cc.y), Conv.Q3(new Vector3(0, ang, 0)), Vector3.one));
                }
                if (i == porton) Porton(n, a + dir * medio, dir, Alt(a + dir * medio) + (foso ? 0.6f : 0));
                // colisión por tramo (dos tramos si hay portón)
                var tramos = i == porton ? new[] { (0f, medio - ANCHO_PORTON / 2), (medio + ANCHO_PORTON / 2, L) } : new[] { (0f, L) };
                foreach (var (ta, tb) in tramos)
                {
                    var m = a + dir * ((ta + tb) / 2);
                    Colision(raiz, new Vector3(m.x, Alt(m) + alto / 2, m.y), new Vector3(0.45f, alto + 1, tb - ta), Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y)));
                }
            }
            n.Instanciar(raiz, Registrar);
            if (foso) Foso(raiz);
        }

        void Porton(Nodo n, Vector2 c, Vector2 dir, float y)
        {
            float h = alto + 0.9f;
            foreach (var s in new[] { -1f, 1f })
            {
                var p = c + dir * s * (ANCHO_PORTON / 2 + 0.1f);
                n.Pieza(Geo.Cilindro(0.17f, 0.2f, h, 6), "#6a5640", new Vector3(p.x, y + h / 2 - 0.2f, -p.y));
            }
            float ang = Mathf.Atan2(dir.x, -dir.y);
            n.PiezaM(Geo.Caja(ANCHO_PORTON + 0.9f, 0.22f, 0.22f), "#6a5640", Matrix4x4.TRS(new Vector3(c.x, y + h - 0.35f, -c.y), Conv.Q3(new Vector3(0, ang - Mathf.PI / 2, 0)), Vector3.one));
            // una hoja de la tranquera, abierta hacia adentro
            var adentro = new Vector2(dir.y, -dir.x);
            var bisagra = c - dir * (ANCHO_PORTON / 2);
            var hoja = bisagra + adentro * 0.8f;
            float angH = Mathf.Atan2(adentro.x, -adentro.y);
            for (int i = 0; i < 4; i++)
                n.PiezaM(Geo.Caja(0.08f, 0.1f, 1.6f), "#7d6c55", Matrix4x4.TRS(new Vector3(hoja.x, y + 0.3f + i * 0.45f, -hoja.y), Conv.Q3(new Vector3(0, angH, 0)), Vector3.one));
        }

        /// Terraplén donde están clavados los palos, zanja ancha y bordo de afuera, todo como franjas de tierra.
        void Foso(Transform raiz)
        {
            var m = new ObraMalla { metrosTextura = 3 };
            var tierra = "tierra_rastreada";
            int nPts = puntos.Count;
            // cortes del perfil: distancia hacia afuera desde la línea de palos y altura sobre el suelo
            // (el terreno tapa lo que quede por debajo del suelo: el foso se lee por los bordos que lo encierran)
            var perfil = new (float d, float h)[]
            {
                (-1.6f, 0.0f), (-0.8f, 0.75f), (0.5f, 0.75f), (1.1f, 0.06f),
                (1.2f + anchoFoso * 0.5f, 0.04f),
                (1.2f + anchoFoso, 0.06f), (1.9f + anchoFoso, 0.55f), (2.8f + anchoFoso, 0.0f),
            };
            string[] mats = { tierra, tierra, tierra, "barro", "barro", tierra, tierra };   // un material por tramo del perfil
            for (int i = 0; i < nPts; i++)
            {
                Vector2 a = puntos[i], b = puntos[(i + 1) % nPts];
                float L = Vector2.Distance(a, b);
                if (L < 0.01f) continue;
                var dir = (b - a) / L;
                var afuera = new Vector2(-dir.y, dir.x);
                // en las esquinas, cada franja se estira por la bisectriz para cerrar sin huecos
                Vector2 Desp(Vector2 punto, int idx, float d)
                {
                    var prev = puntos[(idx - 1 + nPts) % nPts]; var next = puntos[(idx + 1) % nPts];
                    var d1 = (punto - prev).normalized; var d2 = (next - punto).normalized;
                    var n1 = new Vector2(-d1.y, d1.x); var n2 = new Vector2(-d2.y, d2.x);
                    var bis = (n1 + n2).normalized;
                    float cos = Mathf.Max(0.3f, Vector2.Dot(bis, n1));
                    return punto + bis * (d / cos);
                }
                int pasos = Mathf.Max(1, Mathf.CeilToInt(L / 2f));
                for (int s = 0; s < pasos; s++)
                {
                    float t0 = (float)s / pasos, t1 = (float)(s + 1) / pasos;
                    for (int c = 0; c + 1 < perfil.Length; c++)
                    {
                        Vector2 Q(float t, float d)
                        {
                            var pa = Desp(a, i, d); var pb = Desp(b, (i + 1) % nPts, d);
                            return Vector2.Lerp(pa, pb, t);
                        }
                        Vector3 V(Vector2 q, float h) => new Vector3(q.x, Alt(q) + h + 0.03f, q.y);
                        var q00 = Q(t0, perfil[c].d); var q10 = Q(t1, perfil[c].d);
                        var q01 = Q(t0, perfil[c + 1].d); var q11 = Q(t1, perfil[c + 1].d);
                        // frente al portón, el foso se cruza por un puente: ahí no se hunde
                        bool puente = i == porton && Mathf.Abs((t0 + t1) / 2 * L - L / 2) < 1.6f && c >= 2 && c <= 5;
                        float h0 = puente ? 0.62f : perfil[c].h, h1 = puente ? 0.62f : perfil[c + 1].h;
                        string mat = puente ? "madera" : mats[c];
                        m.Quad(mat, V(q00, h0), V(q01, h1), V(q11, h1), V(q10, h0));
                    }
                }
            }
            m.Instanciar("terraplén y foso", raiz, Registrar, false);
        }
    }
}
