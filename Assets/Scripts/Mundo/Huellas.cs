using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Camino de tierra con relieve: una cinta que cubre el camino real (o las sendas) apoyada en el terreno,
    /// con los surcos hundidos que dejaron las ruedas de las carretas, el barro que la rueda corrió hacia los
    /// costados, el lomo del medio con matas, charcos en los bajos y las orillas que se meten bajo el pasto.
    /// Cada carreta agarró por un lado un poco distinto, así que hay varios pares de surcos que se separan,
    /// se juntan y a ratos se borran.
    ///
    /// El terreno tiene que estar un poco más hondo debajo del camino (así lo deja el relieve del pago) para
    /// que la cinta no quede flotando. Sigue el trazado de MapaPago y se apoya en el terreno: si se esculpe
    /// el terreno, hay que rearmarlo (clic derecho en el componente → Regenerar).
    ///
    /// Materiales (MaterialesMundo): "camino", "huella", "barro" y "charco". Si falta alguno se usa un color liso.
    /// </summary>
    public class Huellas : ProceduralBase
    {
        public enum Trazado { CaminoReal, Sendas }   // Sendas: todos los caminos del loteo que no son el camino real

        public Trazado trazado = Trazado.CaminoReal;
        [Tooltip("Pares de surcos de carreta que se entrecruzan")]
        [Range(1, 5)] public int pares = 3;
        [Tooltip("Distancia entre las dos ruedas de la carreta, en metros")]
        public float trocha = 1.9f;
        [Tooltip("Ancho de cada surco, en metros")]
        public float anchoHuella = 0.45f;
        [Tooltip("Hondura del surco más usado, en metros")]
        [Range(0, 0.3f)] public float hondura = 0.14f;
        [Tooltip("Cuánto se corre cada par de surcos a lo largo del camino, en metros")]
        public float vaiven = 1.2f;
        [Tooltip("Cantidad de charcos en los bajos (0 = camino seco)")]
        [Range(0, 1)] public float charcos = 0.5f;
        [Tooltip("Matas de pasto en el lomo entre los surcos y en las orillas")]
        [Range(0, 1)] public float matas = 0.6f;

        const float PASO = 1f, LATERAL = 0.3f, TRAMO = 100, ALTO = 0.2f;
        const string MATA = "#7b8a46", MATA_SECA = "#9a9255";

        static string Mat(string nombre, string color)
        {
            var mm = MaterialesMundo.Instancia;
            return mm != null && mm.Buscar(nombre) != null ? nombre : color;
        }

        string CAMINO, HUELLA, BARRO, CHARCO;

        float Alto(Vector3 w) => Terreno.Altura(w);

        Vector3 Punto(Vector2 p, Vector2 nrm, float lado, float dy)
        {
            var w = new Vector3(p.x + nrm.x * lado, 0, -(p.y + nrm.y * lado));
            w.y = Alto(w) + dy;
            return transform.InverseTransformPoint(w);
        }

        // ---------- perfil del camino ----------
        struct Surco { public float centro, medio, hondo, borde; public bool barro; }

        /// Surcos que hay a la altura s del recorrido.
        // parámetros del camino que se está armando (dependen del tipo)
        float tTrocha, tHondo = 1, tAlto = ALTO;

        List<Surco> Surcos(float s, int linea, int nPares, float medio, bool senda)
        {
            var r = new List<Surco>(nPares * 2);
            float tr = tTrocha;
            float humedad = MapaPago.Ruido(s * 0.006 + linea * 3.3, 4.1);
            for (int p = 0; p < nPares; p++)
            {
                bool principal = p == 0;
                // los pares viejos aparecen y desaparecen a lo largo del camino
                float vis = principal ? 1 : MapaPago.SStep(0.4f, 0.55f, MapaPago.Ruido(s * 0.01 + p * 41.3 + linea * 7.1, p * 2.7));
                if (vis <= 0.01f) continue;
                float c = Desvio(p, nPares, s, linea, medio, tr);
                bool barro = principal && humedad > (senda ? 0.72f : 0.6f) && MapaPago.Ruido(s * 0.15 + linea, p + 0.5) > 0.35f;
                float hondo = tHondo * hondura * (principal ? 1 : 0.55f) * vis * (barro ? 1.3f : 1);
                foreach (var lado in new[] { -tr / 2, tr / 2 })
                {
                    float w = anchoHuella * (0.75f + 0.55f * MapaPago.Ruido(s * 0.07 + p * 13.1 + lado * 3.7, lado * 1.9)) * (principal ? 1 : 0.8f);
                    r.Add(new Surco { centro = c + lado, medio = w / 2, hondo = hondo, borde = (barro ? 0.06f : 0.03f) * vis, barro = barro });
                }
            }
            return r;
        }

        /// Altura de la cinta sobre el terreno en un punto del corte. Devuelve también qué hay ahí.
        float Perfil(float s, float lado, List<Surco> surcos, float medio, float margen, int linea, out string mat)
        {
            float h = tAlto + 0.03f * (MapaPago.Ruido(s * 0.9 + linea * 11, lado * 0.9 + 3) - 0.5f) * 2;
            mat = CAMINO;
            float hundido = 0, maxRel = 0;
            bool barro = false;
            foreach (var u in surcos)
            {
                float d = Mathf.Abs(lado - u.centro);
                float t = 1 - MapaPago.SStep(u.medio * 0.35f, u.medio * 1.25f, d);   // fondo redondeado
                if (t > 0) { float hh = u.hondo * t; if (hh > hundido) { hundido = hh; barro = u.barro; maxRel = t; } }
                float rb = d - u.medio * 1.45f;                                        // borde levantado
                h += u.borde * Mathf.Exp(-rb * rb / (2 * 0.12f * 0.12f));
            }
            h -= hundido;
            if (maxRel > 0.45f && hundido > 0.06f) mat = barro ? BARRO : HUELLA;   // los surcos viejos y bajos no cambian de color
            // la orilla se mete bajo el pasto
            float fuera = Mathf.Abs(lado) - medio;
            if (fuera > 0) h = MapaPago.Mix(h, -0.1f, MapaPago.SStep(0, margen, fuera));
            return h;
        }

        protected override void Construir(Transform raiz)
        {
            CAMINO = Mat("camino", "#8a7458"); HUELLA = Mat("huella", "#6e5a44"); BARRO = Mat("barro", "#463629"); CHARCO = Mat("charco", "#4f5a63");

            // caminos a armar: del loteo si hay; si no, los trazados del prototipo
            var lista = new List<(Vector2[] km, TipoCamino tipo, bool patio)>();
            var loteo = LoteoPago.Instancia;
            if (loteo != null)
            {
                foreach (var c in loteo.caminos)
                    if (c.km != null && c.km.Length >= 2 && (c.tipo == TipoCamino.Real) == (trazado == Trazado.CaminoReal))
                        lista.Add((c.km, c.tipo, c.patioAlFinal));
            }
            else if (trazado == Trazado.CaminoReal) lista.Add((MapaPago.CAMINO, TipoCamino.Real, false));
            else foreach (var k in MapaPago.SENDAS) lista.Add((k, TipoCamino.Senda, true));

            int nTramo = 0;
            for (int li = 0; li < lista.Count; li++)
            {
                var (km, tipo, _) = lista[li];
                bool real = tipo == TipoCamino.Real;
                bool senda = tipo == TipoCamino.Senda || tipo == TipoCamino.Huella;
                int nPares = real ? pares : tipo == TipoCamino.Callejon ? 2 : 1;
                float medio = LoteoPago.MedioAncho(tipo);
                float margen = real ? 1.6f : tipo == TipoCamino.Callejon ? 1.3f : 0.9f;
                tTrocha = tipo == TipoCamino.Senda ? 0.9f : trocha;    // en las sendas, las huellas de los caballos
                tHondo = real ? 1 : tipo == TipoCamino.Callejon ? 0.85f : tipo == TipoCamino.Senda ? 0.5f : 0.35f;
                tAlto = real ? ALTO : ALTO - 0.03f;                       // en los cruces manda el camino real
                int cols = Mathf.CeilToInt((medio + margen) * 2 / LATERAL);
                float l0 = -(medio + margen), paso = (medio + margen) * 2 / cols;

                var pts = Remuestrear(MapaPago.Suavizar(km));
                if (pts.Count < 2) continue;
                float largo = (pts.Count - 1) * PASO;
                var obra = new ObraMalla();
                float hecho = 0;
                Vector3[] filaA = null; string[] matA = null;   // fila anterior del corte
                for (int i = 0; i < pts.Count; i++)
                {
                    float s = i * PASO;
                    var p = pts[i];
                    var t = Tangente(pts, i);
                    var n = new Vector2(-t.y, t.x);
                    // los caminos que no son el real nacen y mueren en el suelo: arrancan angostos sobre el camino
                    // del que salen y terminan abriéndose en el patio
                    float punta = real ? 1 : MapaPago.SStep(0, 18, Mathf.Min(s, largo - s));
                    var surcos = Surcos(s, li, nPares, medio, senda);
                    // los surcos se borran antes de que la cinta se hunda, así la punta termina pareja y no en dedos
                    float borra = MapaPago.SStep(0.55f, 1, punta);
                    for (int k = 0; k < surcos.Count; k++) { var u = surcos[k]; u.hondo *= borra; u.borde *= borra; surcos[k] = u; }
                    float medioAqui = medio * MapaPago.Mix(0.8f, 1, punta);
                    var fila = new Vector3[cols + 1];
                    var mats = new string[cols + 1];
                    for (int c = 0; c <= cols; c++)
                    {
                        float lado = l0 + c * paso;
                        float h = Perfil(s, lado, surcos, medioAqui, margen, li, out mats[c]);
                        if (!real) h = MapaPago.Mix(-0.16f, h, MapaPago.SStep(0, 0.55f, punta));   // se hunde de a poco en el patio o en el otro camino
                        fila[c] = Punto(p, n, lado, h);
                    }
                    bool agua = MapaPago.EnAgua(p.x, p.y) && MapaPago.DistCamino(p.x, p.y) >= 0;
                    if (filaA != null && !agua)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            string m = Prioridad(matA[c], matA[c + 1], mats[c], mats[c + 1]);
                            obra.Quad(m, filaA[c], fila[c], fila[c + 1], filaA[c + 1]);
                        }
                        if (matas > 0 && punta > 0.5f)
                        {
                            // matas en el lomo del medio (entre las ruedas del par más usado) y en las orillas
                            if (!senda && surcos.Count >= 2 && Hash(s * 1.3f + li * 101) < matas * 0.35f)
                            {
                                float c0 = (surcos[0].centro + surcos[1].centro) / 2;
                                Mata(obra, Punto(p, n, c0 + (Hash(s * 2.9f) - 0.5f) * 0.5f, tAlto + 0.02f), s);
                            }
                            if (Hash(s * 0.7f + li * 37) < matas * 0.4f)
                                Mata(obra, Punto(p, n, (Hash(s * 2.1f) < 0.5f ? -1 : 1) * (medio + 0.2f + Hash(s * 3.3f) * margen * 0.6f), 0.02f), s * 1.7f);
                        }
                        if (charcos > 0 && i % 5 == 0 && punta >= 1 && surcos.Count > 0 && EsBajo(pts, i) && Hash(s * 0.37f + li) < charcos)
                            Charco(obra, p, n, t, surcos, s);
                    }
                    filaA = fila; matA = mats;

                    hecho += PASO;
                    if ((hecho >= TRAMO || i == pts.Count - 1) && obra.Triangulos > 0)
                    {
                        obra.Instanciar($"tramo {nTramo++}", raiz, Registrar);
                        obra = new ObraMalla();
                        hecho = 0;
                    }
                }
            }
        }

        /// Material de un cuadro: barro si cualquier esquina cae en barro, huella si la mitad cae en el surco.
        string Prioridad(params string[] m)
        {
            foreach (var x in m) if (x == BARRO) return BARRO;
            int h = 0;
            foreach (var x in m) if (x == HUELLA) h++;
            return h >= 2 ? HUELLA : CAMINO;
        }

        /// Lado al que se corre cada par de surcos: el más usado va por el medio, los otros a los costados,
        /// y todos van y vienen con un ruido suave.
        float Desvio(int par, int n, float s, int linea, float medio, float tr)
        {
            float lim = Mathf.Max(0, medio - tr / 2 - anchoHuella);
            float baseLado = par == 0 || n < 2 ? 0
                : n == 2 ? lim * 0.6f
                : Mathf.Lerp(-lim * 0.7f, lim * 0.7f, (par - 1f) / (n - 2));
            float onda = (MapaPago.Ruido(s * 0.012 + par * 31.7 + linea * 5.3, par * 3.1 + linea) - 0.5f) * 2 * vaiven;
            return Mathf.Clamp(baseLado + onda, -lim, lim);
        }

        static void Mata(ObraMalla o, Vector3 b, float sem)
        {
            float alto = 0.18f + 0.22f * Hash(sem * 1.9f);
            string c = Hash(sem * 4.1f) < 0.4f ? MATA_SECA : MATA;
            for (int k = 0; k < 4; k++)
            {
                float ang = Hash(sem + k * 7.3f) * 6.28f;
                var lado = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 0.03f;
                var punta = b + new Vector3(Mathf.Sin(ang) * 0.08f, alto, -Mathf.Cos(ang) * 0.08f);
                o.Tri(c, b - lado, punta, b + lado);
            }
        }

        /// Agua quieta en el fondo de los surcos: a veces en uno solo y a veces en todo el ancho.
        void Charco(ObraMalla o, Vector2 p, Vector2 n, Vector2 t, List<Surco> surcos, float sem)
        {
            bool todo = surcos.Count >= 2 && Hash(sem * 2.7f) < 0.35f;
            float a = surcos[0].centro, b = surcos[Mathf.Min(1, surcos.Count - 1)].centro;
            float cen = todo ? (a + b) / 2 : (Hash(sem * 5.1f) < 0.5f ? a : b);
            float rLat = todo ? Mathf.Abs(b - a) / 2 + anchoHuella * 0.8f : anchoHuella * 0.55f;
            float rLong = 1.5f + Hash(sem * 3.9f) * 3f;
            var c = p + n * cen;
            float nivel = Alto(new Vector3(c.x, 0, -c.y)) + tAlto - surcos[0].hondo * 0.45f;   // el agua queda horizontal
            const int lados = 12;
            var centro = transform.InverseTransformPoint(new Vector3(c.x, nivel, -c.y));
            for (int k = 0; k < lados; k++)
            {
                float a0 = k * Mathf.PI * 2 / lados, a1 = (k + 1) * Mathf.PI * 2 / lados;
                float w0 = 0.8f + 0.4f * Hash(sem + k), w1 = 0.8f + 0.4f * Hash(sem + (k + 1) % lados);
                var q0 = c + t * Mathf.Cos(a0) * rLong * w0 + n * Mathf.Sin(a0) * rLat * w0;
                var q1 = c + t * Mathf.Cos(a1) * rLong * w1 + n * Mathf.Sin(a1) * rLat * w1;
                var v0 = transform.InverseTransformPoint(new Vector3(q0.x, nivel, -q0.y));
                var v1 = transform.InverseTransformPoint(new Vector3(q1.x, nivel, -q1.y));
                o.Tri(CHARCO, centro, v0, v1);
            }
        }

        /// ¿El punto i está en un bajo del camino? (más hondo que lo que hay 12 m antes y después)
        bool EsBajo(List<Vector2> pts, int i)
        {
            int k = Mathf.RoundToInt(12 / PASO);
            if (i - k < 0 || i + k >= pts.Count) return false;
            float h = Alto(new Vector3(pts[i].x, 0, -pts[i].y));
            float h0 = Alto(new Vector3(pts[i - k].x, 0, -pts[i - k].y)), h1 = Alto(new Vector3(pts[i + k].x, 0, -pts[i + k].y));
            return h < h0 - 0.05f && h < h1 - 0.05f;
        }

        static Vector2 Tangente(List<Vector2> pts, int i)
        {
            var d = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)];
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.right;
        }

        /// Pasa el trazado (en km) a metros del prototipo con un punto cada PASO metros.
        static List<Vector2> Remuestrear(List<Vector2> km)
        {
            var r = new List<Vector2>();
            if (km.Count < 2) return r;
            var prev = MapaPago.K(km[0]);
            r.Add(prev);
            float sobra = 0;
            for (int i = 1; i < km.Count; i++)
            {
                var p = MapaPago.K(km[i]);
                float d = Vector2.Distance(prev, p);
                float s = PASO - sobra;
                while (s <= d)
                {
                    r.Add(Vector2.Lerp(prev, p, s / d));
                    s += PASO;
                }
                sobra = d - (s - PASO);
                prev = p;
            }
            return r;
        }

        static float Hash(float x) => MapaPago.Hs(x * 1.618 + 0.5);
    }
}
