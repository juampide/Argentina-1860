using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argentina1860
{
    /// <summary>
    /// Buenos Aires en 1867 armada a partir del plano (ciudad.js del prototipo): manzanas con casas de paredes pastel y techos
    /// de teja o planos con cornisa, veredas elevadas con cordón, calles empedradas, Plaza de la Victoria con la Pirámide de Mayo,
    /// el Cabildo y la Recova, las iglesias con sus torres y la Catedral, farolas en las esquinas y el río.
    /// Se genera al abrir la escena y al dar Play. Todo en coordenadas del prototipo (x este, z sur, plaza en el origen).
    /// </summary>
    public class Ciudad : ProceduralBase
    {
        [Header("Datos del plano")]
        public TextAsset json;
        public TextAsset pak;
        [Tooltip("Máscara del plano: rojo = agua")]
        public Texture2D mascaraAgua;

        [Header("Detalle")]
        [Tooltip("Hasta qué distancia de la plaza las manzanas tienen casas (metros)")]
        public float radioLotes = 1500;
        [Tooltip("Hasta qué distancia las fachadas tienen puertas y ventanas")]
        public float radioDecor = 520;
        [Tooltip("Hasta qué distancia hay casas de dos plantas")]
        public float radioCentro = 700;
        [Tooltip("Armar la ciudad también en el editor (tarda unos segundos al abrir la escena)")]
        public bool generarEnEditor = true;

        public static Ciudad Instancia { get; private set; }
        public DatosCiudad Datos { get; private set; }
        public CampoCiudad Campo { get; private set; }
        public readonly List<Vector2[]> bloques = new List<Vector2[]>();

        static float Hash(double i) { double x = Math.Sin(i * 127.1 + 311.7) * 43758.5453; return (float)(x - Math.Floor(x)); }
        static Vector3 W3(Vector2 p, float y) => new Vector3(p.x, y, p.y);
        static string Hex(Color c) => Conv.Clave(c);
        static Color Mul(Color c, float f) => new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f));

        // Paleta del pueblo: paredes pastel encaladas y techos de teja en tonos cálidos
        static readonly string[] PAREDES = { "#f4ead2", "#f1dcae", "#ebbfa0", "#c4d8d8", "#f2d28f", "#dcc7a4", "#f6dfce", "#ccd9bb", "#e9c9c2", "#d6e0ea" };
        static readonly string[] TEJAS = { "#ffffff", "#f6dccf", "#e8c4b0", "#f9e9de", "#ffe7d6" };
        const float PROF = 24, FRENTE = 9.5f, FONDO = 7;

        // ---------- fragmentos (una malla por material y por zona de 350 m) ----------
        class Trozo { public readonly Dictionary<string, Geo> g = new Dictionary<string, Geo>(); public readonly Geo col = new Geo(); }
        readonly Dictionary<long, Trozo> trozos = new Dictionary<long, Trozo>();

        Trozo T(float x, float z)
        {
            long k = Mathf.FloorToInt(x / 350) * 1000L + Mathf.FloorToInt(z / 350);
            if (!trozos.TryGetValue(k, out var t)) trozos[k] = t = new Trozo();
            return t;
        }

        static Geo G(Trozo t, string clave)
        {
            if (!t.g.TryGetValue(clave, out var g)) t.g[clave] = g = new Geo();
            return g;
        }

        protected override void OnEnable()
        {
            Instancia = this;
            base.OnEnable();
        }

        protected override void Construir(Transform raiz)
        {
            trozos.Clear();
            bloques.Clear();
            if (json == null || pak == null) return;
            Datos = DatosCiudad.Cargar(json, pak);
            Campo = new CampoCiudad();
            if (mascaraAgua != null && mascaraAgua.isReadable) Campo.agua = new MascaraAgua(mascaraAgua, Datos);
            if (!Application.isPlaying && !generarEnEditor) return;

            Manzanas();
            var mon = new Nodo("monumentos");
            Piramide(mon);
            float zE = -68; for (float z = -58; z > -140; z -= 0.5f) if (Campo.Ocupado(0, z)) { zE = z; break; }
            float zS = 120; for (float z = 58; z < 200; z += 0.5f) if (Campo.Ocupado(0, z)) { zS = z; break; }
            Cabildo(mon, zE);
            Recova(mon, zS);
            Iglesias(mon);
            Farolas(mon);
            Suelo(raiz);

            // mallas por fragmento, con colisión de manzanas y veredas
            foreach (var o in Campo.Obstaculos()) Prisma(T(o[0].x, o[0].y).col, o, 6);
            int n = 0;
            foreach (var kv in trozos)
            {
                var nodo = new Nodo("trozo " + n++);
                foreach (var g in kv.Value.g) nodo.Pieza(g.Value, g.Key, Vector3.zero);
                var t = nodo.Instanciar(raiz, Registrar);
                var colGeo = new Nodo("col");
                colGeo.Pieza(kv.Value.col, "#000000", Vector3.zero);
                var mt = new MallaTres();
                foreach (var p in colGeo.piezas) mt.Agregar(p.g, p.mat, p.m);
                var malla = Registrar(mt.Construir("colisión", out _));
                if (malla.vertexCount > 0) t.gameObject.AddComponent<MeshCollider>().sharedMesh = malla;
            }
            mon.Instanciar(raiz, Registrar);
        }

        // ---------- paredes, techos y alas de edificio ----------
        static void Pared(Geo g, Vector2 p, Vector2 q, float y0, float y1, float off = 0, float esc = 3.2f)
        {
            float L = Vector2.Distance(p, q) / esc;
            g.QuadUV(W3(p, y0), W3(q, y0), W3(q, y1), W3(p, y1), new Vector2(off, y0 / esc), new Vector2(off + L, y0 / esc), new Vector2(off + L, y1 / esc), new Vector2(off, y1 / esc));
        }

        static void PlanoTecho(Geo g, Vector3 A, Vector3 B, Vector3 C, Vector3 D)
        {
            float lab = new Vector2(B.x - A.x, B.z - A.z).magnitude / 1.5f, s = (D - A).magnitude / 1.5f;
            g.QuadUV(A, B, C, D, Vector2.zero, new Vector2(lab, 0), new Vector2(lab, s), new Vector2(0, s));
        }

        static void TriPared(Geo g, Vector3 a, Vector3 b, Vector3 c)
        {
            float d = new Vector2(b.x - a.x, b.z - a.z).magnitude;
            g.TriUV(a, b, c, new Vector2(0, a.y / 3.2f), new Vector2(d / 3.2f, b.y / 3.2f), new Vector2(d / 6.4f, c.y / 3.2f));
        }

        class OpAla { public float izqH, derH; public bool decal, sinFrente, ladrillo; public int k, pisos = 1; }

        /// Un ala de edificio: a-b es el frente (hacia afuera), d-c el fondo. tipo: dos aguas o plano con cornisa.
        void Ala(Trozo B, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float h, bool dosAguas, string colP, string colT, OpAla o = null)
        {
            o ??= new OpAla();
            float dep = Vector2.Distance(a, d);
            Vector2 sal = (a - d) / dep;
            float off = Hash(o.k + 31) * 6;
            string pared = "revoque*" + colP, techo = "teja*" + colT;
            string paredFrente = o.ladrillo ? "ladrillo*" + Hex(Color.white * (0.8f + Hash(o.k + 3) * 0.35f)) : pared;
            float esc = o.ladrillo ? 1.3f : 3.2f;
            if (!o.sinFrente)
            {
                Pared(G(B, paredFrente), a, b, 0, h, off, esc);
                const float e = 0.03f;   // zócalo de sillares al pie de la fachada
                Pared(G(B, "sillar"), a + sal * e, b + sal * e, 0, 0.75f, off, 1.5f);
            }
            if (h > o.izqH) Pared(G(B, paredFrente), d, a, o.izqH, h, off + 1, esc);
            if (h > o.derH) Pared(G(B, paredFrente), b, c, o.derH, h, off + 2, esc);
            if (!dosAguas)
            {
                var e2 = sal * 0.15f;
                G(B, techo).QuadUV(W3(a, h), W3(b, h), W3(c, h), W3(d, h), a / 1.5f, b / 1.5f, c / 1.5f, d / 1.5f);
                Vector2 a2 = a + e2, b2 = b + e2;
                Pared(G(B, pared), a2, b2, h, h + 0.65f, off);
                if (!o.sinFrente)   // cornisa que sobresale: da sombra y relieve a la fachada
                {
                    const float ce = 0.16f;
                    var cp = Conv.Hex(colP);
                    string lc = "revoque*" + Hex(Mul(cp, 1.06f)), lo = "revoque*" + Hex(Mul(cp, 0.74f));
                    Vector2 p1 = a + sal * ce, q1 = b + sal * ce;
                    Pared(G(B, lc), p1, q1, h - 0.42f, h - 0.04f, off);
                    G(B, lc).QuadUV(W3(a, h - 0.04f), W3(b, h - 0.04f), W3(q1, h - 0.04f), W3(p1, h - 0.04f), Vector2.zero, new Vector2(1, 0), new Vector2(1, 0.1f), new Vector2(0, 0.1f));
                    G(B, lo).QuadUV(W3(a, h - 0.42f), W3(b, h - 0.42f), W3(q1, h - 0.42f), W3(p1, h - 0.42f), Vector2.zero, new Vector2(1, 0), new Vector2(1, 0.1f), new Vector2(0, 0.1f));
                }
                Pared(G(B, pared), d, a2, h, h + 0.55f, off);
                Pared(G(B, pared), b2, c, h, h + 0.55f, off);
            }
            else
            {
                float rise = dep * 0.25f;   // pendiente ~27°
                Vector2 ra = Vector2.Lerp(a, d, 0.5f), rb = Vector2.Lerp(b, c, 0.5f);
                const float voz = 0.45f;
                Vector3 ap = W3(a + sal * voz, h - 0.12f), bp = W3(b + sal * voz, h - 0.12f), dp = W3(d - sal * voz, h - 0.12f), cpp = W3(c - sal * voz, h - 0.12f);
                Vector3 Ra = W3(ra, h + rise), Rb = W3(rb, h + rise);
                PlanoTecho(G(B, techo), ap, bp, Rb, Ra);
                PlanoTecho(G(B, techo), dp, cpp, Rb, Ra);
                if (h > o.izqH || o.izqH == 0) TriPared(G(B, pared), W3(a, h), W3(d, h), Ra);
                if (h > o.derH || o.derH == 0) TriPared(G(B, pared), W3(b, h), W3(c, h), Rb);
            }
            if (o.decal)   // puertas, ventanas con reja y balcones del piso alto (atlas de vanos)
            {
                float ancho = Vector2.Distance(a, b);
                int bahias = Mathf.Max(1, Mathf.FloorToInt(ancho / 3.3f));
                int puerta = Mathf.FloorToInt(Hash(o.k + 5) * bahias);
                var dir = (b - a) / ancho;
                var vg = G(B, "vanos");
                for (int i = 0; i < bahias; i++)
                {
                    var p = Vector2.Lerp(a, b, (i + 0.5f) / bahias) + sal * 0.05f;
                    void Poner(float y0, float y1, float hw, int celda)
                    {
                        float u0 = celda * 0.25f, u1 = (celda + 1) * 0.25f;
                        Vector2 l = p - dir * hw, r = p + dir * hw;
                        vg.QuadUV(W3(l, y0), W3(r, y0), W3(r, y1), W3(l, y1), new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1));
                    }
                    if (i == puerta && ancho > 4) Poner(0, 3.3f, 0.85f, 1); else Poner(0.55f, 3.2f, 0.7f, 0);
                    if (o.pisos > 1 || h > 6.8f) Poner(4.3f, 7.0f, 0.7f, 2);
                }
            }
        }

        // ---------- manzanas ----------
        static readonly (float x0, float x1, float z0, float z1)[] RESERVAS = { (-62, 62, -140, -62) };   // Cabildo, al norte de la plaza
        static bool EnReserva(Vector2 p) { foreach (var r in RESERVAS) if (p.x > r.x0 && p.x < r.x1 && p.y > r.z0 && p.y < r.z1) return true; return false; }

        void Manzanas()
        {
            int N = Datos.CantManzanas;
            var pts = new Vector2[4];
            for (int i = 0; i < N; i++)
            {
                int f = i * 10;
                for (int k = 0; k < 4; k++) pts[k] = Datos.AMundo(Datos.manzanas[f + k * 2], Datos.manzanas[f + k * 2 + 1]);
                float minx = Mathf.Min(pts[0].x, pts[1].x, pts[2].x, pts[3].x), maxx = Mathf.Max(pts[0].x, pts[1].x, pts[2].x, pts[3].x);
                float minz = Mathf.Min(pts[0].y, pts[1].y, pts[2].y, pts[3].y), maxz = Mathf.Max(pts[0].y, pts[1].y, pts[2].y, pts[3].y);
                if (maxx - minx > 400 || maxz - minz > 400) continue;
                var copia = (Vector2[])pts.Clone();
                Campo.Registrar(copia);
                bloques.Add(copia);
                var c = (copia[0] + copia[2]) / 2;
                float d = c.magnitude, lu = Vector2.Distance(copia[0], copia[1]), lv = Vector2.Distance(copia[0], copia[3]);
                if (d < radioLotes && lu > 2 * PROF + 14 && lv > 2 * PROF + 14) Lotes(copia, i, lu, lv, d < radioDecor, d < radioCentro);
                else CajaSimple(copia, i);
            }
        }

        void Lotes(Vector2[] pts, int i, float lu, float lv, bool decor, bool centro)
        {
            Vector2 P(float u, float v) => (1 - v) * ((1 - u) * pts[0] + u * pts[1]) + v * ((1 - u) * pts[3] + u * pts[2]);
            float du = PROF / lu, dv = PROF / lv;
            var B = T((pts[0].x + pts[2].x) / 2, (pts[0].y + pts[2].y) / 2);
            float Rnd(int k) => Hash(i * 37 + k);
            float Altura(int k) => centro && Rnd(k + 500) > 0.66f ? 7.4f + Rnd(k + 900) * 1.2f : 4.2f + Rnd(k) * 1.0f;

            void Franja(Func<float, float, Vector2> Q, float a0, float a1, float fOut, float fIn, int bas)
            {
                float largo = Vector2.Distance(Q(a0, fOut), Q(a1, fOut));
                int n = Mathf.Max(1, Conv.RoundJS(largo / (9 + Rnd(bas) * 5)));
                var hs = new float[n];
                for (int t = 0; t < n; t++) hs[t] = Altura(bas + t * 7);
                float fF = fOut + (fIn - fOut) * (FRENTE / PROF), fB = fIn - (fIn - fOut) * (FONDO / PROF);
                for (int t = 0; t < n; t++)
                {
                    int k = bas + t * 7;
                    float s0 = a0 + (a1 - a0) * t / n, s1 = a0 + (a1 - a0) * (t + 1) / n;
                    if (EnReserva(Q((s0 + s1) / 2, (fOut + fIn) / 2))) continue;   // lo ocupa un monumento
                    float h = hs[t];
                    string colP = PAREDES[Mathf.FloorToInt(Rnd(k + 77) * PAREDES.Length)], colT = TEJAS[Mathf.FloorToInt(Rnd(k + 33) * TEJAS.Length)];
                    var op = new OpAla { izqH = t > 0 ? hs[t - 1] : 0, derH = t + 1 < n ? hs[t + 1] : 0, decal = decor, k = k, pisos = h > 6.8f ? 2 : 1, ladrillo = Rnd(k + 19) > 0.78f };
                    Ala(B, Q(s0, fOut), Q(s1, fOut), Q(s1, fF), Q(s0, fF), h, Rnd(k + 11) > 0.35f, colP, colT, op);
                    float hf = 3.6f + Rnd(k + 3) * 0.8f;
                    Ala(B, Q(s0, fB), Q(s1, fB), Q(s1, fIn), Q(s0, fIn), hf, Rnd(k + 8) > 0.5f, colP, colT, new OpAla { k = k + 1 });
                }
            }
            Franja((s, f) => P(s, f), 0, 1, 0, dv, 1);
            Franja((s, f) => P(s, f), 0, 1, 1, 1 - dv, 100);
            Franja((s, f) => P(f, s), dv, 1 - dv, 0, du, 200);
            Franja((s, f) => P(f, s), dv, 1 - dv, 1, 1 - du, 300);

            // patio y huerta del centro de la manzana
            Vector2 q0 = P(du, dv), q1 = P(1 - du, dv), q2 = P(1 - du, 1 - dv), q3 = P(du, 1 - dv);
            G(B, "patio").QuadUV(W3(q0, 0.04f), W3(q1, 0.04f), W3(q2, 0.04f), W3(q3, 0.04f), q0 / 6, q1 / 6, q2 / 6, q3 / 6);
            // vereda elevada con cordón, y calzada empedrada hasta la mitad de la calle
            float mv = 1.6f / lu, mh = 1.6f / lv;
            const float HV = 0.16f;
            foreach (var (u0, v0, u1, v1) in new[] { (-mv, -mh, 1 + mv, 0f), (-mv, 1f, 1 + mv, 1 + mh), (-mv, 0f, 0f, 1f), (1f, 0f, 1 + mv, 1f) })
            {
                Vector2 a = P(u0, v0), b = P(u1, v0), c = P(u1, v1), d = P(u0, v1);
                G(B, "vereda").QuadUV(W3(a, HV), W3(b, HV), W3(c, HV), W3(d, HV), a / 2.2f, b / 2.2f, c / 2.2f, d / 2.2f);
            }
            var oc = new[] { P(-mv, -mh), P(1 + mv, -mh), P(1 + mv, 1 + mh), P(-mv, 1 + mh) };
            for (int j = 0; j < 4; j++)
            {
                Vector2 p = oc[j], q = oc[(j + 1) % 4];
                float Lr = Vector2.Distance(p, q) / 2.2f;
                G(B, "vereda*#c9c2b2").QuadUV(W3(p, 0), W3(q, 0), W3(q, HV), W3(p, HV), Vector2.zero, new Vector2(Lr, 0), new Vector2(Lr, 0.07f), new Vector2(0, 0.07f));
            }
            float cu = 7.5f / lu, cv = 7.5f / lv;
            var cc = new[] { P(-cu, -cv), P(1 + cu, -cv), P(1 + cu, 1 + cv), P(-cu, 1 + cv) };
            G(B, "calle").QuadUV(W3(cc[0], 0.03f), W3(cc[1], 0.03f), W3(cc[2], 0.03f), W3(cc[3], 0.03f), cc[0] / 2.4f, cc[1] / 2.4f, cc[2] / 2.4f, cc[3] / 2.4f);
            Campo.RegistrarAltura(oc, HV);
            Prisma(B.col, oc, HV);   // vereda maciza: el jugador sube el cordón con el stepOffset
        }

        // manzana lejana: volumen simple
        void CajaSimple(Vector2[] pts, int i)
        {
            var B = T((pts[0].x + pts[2].x) / 2, (pts[0].y + pts[2].y) / 2);
            float h = 4.6f + Hash(i) * 1.2f;
            string colP = PAREDES[Mathf.FloorToInt(Hash(i + 9) * PAREDES.Length)], colT = TEJAS[Mathf.FloorToInt(Hash(i + 3) * TEJAS.Length)];
            for (int j = 0; j < 4; j++) Pared(G(B, "revoque*" + colP), pts[j], pts[(j + 1) % 4], 0, h, Hash(i + j) * 5);
            G(B, "teja*" + Hex(Mul(Conv.Hex(colT), 0.9f))).QuadUV(W3(pts[0], h), W3(pts[1], h), W3(pts[2], h), W3(pts[3], h), pts[0] / 1.5f, pts[1] / 1.5f, pts[2] / 1.5f, pts[3] / 1.5f);
        }

        /// Prisma de colisión (paredes y tapa) sobre un polígono. Cada cara va en los dos sentidos: los polígonos del
        /// plano vienen en sentido horario o antihorario según la manzana, y PhysX deja pasar por la cara de atrás.
        static void Prisma(Geo g, Vector2[] p, float h)
        {
            void Tri2(Vector3 a, Vector3 b, Vector3 c) { g.Tri(a, b, c); g.Tri(a, c, b); }
            for (int j = 0; j < p.Length; j++)
            {
                Vector3 a = W3(p[j], 0), b = W3(p[(j + 1) % p.Length], 0), c = W3(p[(j + 1) % p.Length], h), d = W3(p[j], h);
                Tri2(a, b, c); Tri2(a, c, d);
            }
            for (int j = 1; j + 1 < p.Length; j++) Tri2(W3(p[0], h), W3(p[j], h), W3(p[j + 1], h));
        }

        // ---------- monumentos ----------
        void Piramide(Nodo m)
        {
            Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
            m.Pieza(Geo.Caja(7.2f, 0.35f, 7.2f, 1.5f), "sillar", v(0, 0.175f, 0));
            m.Pieza(Geo.Caja(6.0f, 0.35f, 6.0f, 1.5f), "sillar", v(0, 0.525f, 0));
            m.Pieza(Geo.Caja(4.8f, 0.35f, 4.8f, 1.5f), "sillar", v(0, 0.875f, 0));
            m.Pieza(Geo.Caja(3.6f, 1.6f, 3.6f, 1.5f), "sillar", v(0, 1.85f, 0));
            m.Pieza(Geo.Caja(4.0f, 0.24f, 4.0f, 1.6f), "travertino", v(0, 2.77f, 0));
            foreach (var (sx, sz) in new[] { (1f, 1f), (-1f, 1f), (1f, -1f), (-1f, -1f) })
            {
                m.Pieza(Geo.Caja(0.45f, 0.4f, 0.45f, 1.2f), "travertino", v(sx * 1.65f, 3.09f, sz * 1.65f));
                m.Pieza(Geo.Cono(0.34f, 0.9f, 4), "travertino", v(sx * 1.65f, 3.75f, sz * 1.65f), v(0, Mathf.PI / 4, 0));
            }
            foreach (var (sx, sz, ry) in new[] { (0f, 1.81f, 0f), (0f, -1.81f, Mathf.PI), (1.81f, 0f, Mathf.PI / 2), (-1.81f, 0f, -Mathf.PI / 2) })
                m.Pieza(Geo.Caja(1.5f, 0.9f, 0.05f), "#3a302a", v(sx, 1.9f, sz), v(0, ry, 0));
            m.Pieza(Geo.Cilindro(0.5f, 1.15f, 8.4f, 4), "travertino", v(0, 7.2f, 0), v(0, Mathf.PI / 4, 0));
            m.Pieza(Geo.Cilindro(0.05f, 0.5f, 1.1f, 4), "travertino", v(0, 11.95f, 0), v(0, Mathf.PI / 4, 0));
            m.Pieza(Geo.Cilindro(0.16f, 0.24f, 0.9f, 6), "#7f9099", v(0, 12.95f, 0));   // la Libertad, en plomo
            m.Pieza(Geo.Ico(0.17f, 1), "#7f9099", v(0, 13.55f, 0));
            m.Pieza(Geo.Caja(0.07f, 0.6f, 0.07f), "#7f9099", v(0.2f, 13.55f, 0), v(0, 0, -0.5f));
            const float lado = 10.4f; const int np = 11; const string fe = "#2a2724";   // reja de hierro
            for (int i = 0; i < np * 4; i++)
            {
                float t = (i % np) / (float)np * lado - lado / 2; int k = i / np;
                var p = k == 0 ? new Vector2(t, -lado / 2) : k == 1 ? new Vector2(lado / 2, t) : k == 2 ? new Vector2(-t, lado / 2) : new Vector2(-lado / 2, -t);
                m.Pieza(Geo.Cilindro(0.035f, 0.045f, 1.5f, 5), fe, v(p.x, 0.75f, p.y));
                m.Pieza(Geo.Cono(0.07f, 0.2f, 4), fe, v(p.x, 1.6f, p.y));
            }
            foreach (var (x, z, sx, sz) in new[] { (0f, -lado / 2, lado, 0.05f), (0f, lado / 2, lado, 0.05f), (-lado / 2, 0f, 0.05f, lado), (lado / 2, 0f, 0.05f, lado) })
                foreach (var y in new[] { 0.45f, 1.25f }) m.Pieza(Geo.Caja(sx, 0.05f, sz), fe, v(x, y, z));
            Campo.Rect(-lado / 2 - 0.3f, -lado / 2 - 0.3f, lado / 2 + 0.3f, lado / 2 + 0.3f);
        }

        /// Arcada: plano con arcos recortados (textura con transparencia), de x0 a x1 sobre z, mirando hacia "cara".
        static void Arcada(Nodo m, float x0, float x1, float z, float h, int pisos, float cara)
        {
            int bahias = Conv.RoundJS((x1 - x0) / 4);
            var g = new Geo();
            g.QuadUV(new Vector3(x0, 0, z), new Vector3(x1, 0, z), new Vector3(x1, h, z), new Vector3(x0, h, z), Vector2.zero, new Vector2(bahias, 0), new Vector2(bahias, 1), new Vector2(0, 1));
            m.Pieza(g, pisos == 1 ? "arco1" : "arco2", Vector3.zero);   // el material es de dos caras: sirve para los dos lados
        }

        void CajaTexturada(Trozo t, float x0, float x1, float z0, float z1, float h, string colP)
        {
            var p = new[] { new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1) };
            for (int j = 0; j < 4; j++) Pared(G(t, "revoque*" + colP), p[j], p[(j + 1) % 4], 0, h);
        }

        void TechoDosAguas(Trozo t, float x0, float x1, float z0, float z1, float h, string colT, string colP) =>
            Ala(t, new Vector2(x0, z1), new Vector2(x1, z1), new Vector2(x1, z0), new Vector2(x0, z0), h, true, colP, colT, new OpAla { sinFrente = true, izqH = h, derH = h });

        static Geo MedioDisco(float r, int seg)
        {
            var g = new Geo();
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * i / seg, a1 = Mathf.PI * (i + 1) / seg;
                g.TriUV(Vector3.zero, new Vector3(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0), new Vector3(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0), Vector2.zero, Vector2.zero, Vector2.zero);
            }
            return g;
        }

        void Cabildo(Nodo m, float zE)
        {
            const float x0 = -34, x1 = 34, hCab = 11, prof = 22;
            const string colCab = "#f6eed8", colT = "#f6dccf";
            var t = T(0, zE);
            CajaTexturada(t, x0, x1, zE - prof, zE - 2.6f, hCab, "#d6c8aa");   // volumen al fondo de la galería
            CajaTexturada(t, x0, x0 + 0.3f, zE - 2.6f, zE, hCab, colCab);
            CajaTexturada(t, x1 - 0.3f, x1, zE - 2.6f, zE, hCab, colCab);
            Arcada(m, x0, x1, zE - 0.2f, hCab, 2, 1);
            m.Pieza(Geo.Caja(x1 - x0, 0.05f, 2.6f), "#3a302a", new Vector3(0, 5.6f, zE - 1.4f));
            m.Pieza(Geo.Caja(x1 - x0, 0.05f, 2.6f), "#3a302a", new Vector3(0, hCab - 0.05f, zE - 1.4f));
            TechoDosAguas(t, x0, x1, zE - prof, zE, hCab, colT, colCab);
            CajaTexturada(t, -3.8f, 3.8f, zE - 9, zE - 1.2f, 21, colCab);   // torre
            m.Pieza(Geo.Cono(6.2f, 5, 4), "#8a8f94", new Vector3(0, 23.5f, zE - 5.1f), new Vector3(0, Mathf.PI / 4, 0));
            m.Pieza(Geo.Caja(2.4f, 0.4f, 0.1f), "#efe3c8", new Vector3(0, 17, zE - 1.1f));
            m.Pieza(Geo.Cilindro(0.9f, 0.9f, 0.04f, 24), "#f4efe0", new Vector3(0, 15.5f, zE - 1.14f), new Vector3(Mathf.PI / 2, 0, 0));   // reloj
            Campo.Rect(x0, zE - prof, x1, zE);
        }

        void Recova(Nodo m, float zS)
        {
            float zR = zS - 14;
            const float x0 = -58, x1 = 58, h = 6.8f;
            var t = T(0, zR);
            Arcada(m, x0, x1, zR + 3.2f, h, 1, 1);
            Arcada(m, x0, x1, zR - 3.2f, h, 1, -1);
            m.Pieza(Geo.Caja(x1 - x0, 0.05f, 6.4f), "#3a302a", new Vector3(0, h - 0.1f, zR));
            TechoDosAguas(t, x0 - 0.3f, x1 + 0.3f, zR - 3.6f, zR + 3.6f, h, "#f2d6c6", "#eadfc6");
            CajaTexturada(t, -6, 6, zR - 3.8f, zR + 3.8f, h + 3.2f, "#e6dcc3");   // portal central más alto
            m.Pieza(MedioDisco(2.6f, 24), "#3a302a", new Vector3(0, 3.2f, zR + 3.85f));
            m.Pieza(MedioDisco(2.6f, 24), "#3a302a", new Vector3(0, 3.2f, zR - 3.85f), new Vector3(0, Mathf.PI, 0));
            m.Pieza(Geo.Caja(11.9f, 3.2f, 7.4f), "#3a302a", new Vector3(0, 1.6f, zR));
            Campo.Rect(x0, zR - 3.6f, -3, zR + 3.6f);
            Campo.Rect(3, zR - 3.6f, x1, zR + 3.6f);
        }

        void Iglesias(Nodo m)
        {
            for (int idx = 0; idx < Datos.iglesias.Length; idx++)
            {
                var g = Datos.iglesias[idx];
                var c = Datos.AMundo(g.x, g.y);
                float ux = g.ux, uz = g.uy, vx = -uz, vz = ux;
                float L = g.largo * DatosCiudad.ESC / 2, A = g.ancho * DatosCiudad.ESC / 2;
                Vector2 Pt(float u, float v) => new Vector2(c.x + ux * u + vx * v, c.y + uz * u + vz * v);
                Campo.Registrar(new[] { Pt(L, A), Pt(L, -A), Pt(-L, -A), Pt(-L, A) });
                bool catedral = idx == 11;
                var B = T(c.x, c.y);
                string colP = catedral ? "#f3ead4" : "#efe6cf", colT = "#ecccb8";
                float hN = catedral ? 15 : 10.5f;
                Ala(B, Pt(L, A), Pt(-L, A), Pt(-L, -A), Pt(L, -A), hN, true, colP, colT, new OpAla { k = idx });   // nave
                float fr = g.frente;
                if (catedral)
                {
                    float Wp = A * 2 - 1, prof = 8, uF = fr * L, uP = fr * (L + prof);
                    Ala(B, Pt(uF, Wp / 2), Pt(uP, Wp / 2), Pt(uP, -Wp / 2), Pt(uF, -Wp / 2), 12, true, colP, colT, new OpAla { izqH = 12, derH = 12, k = 99, sinFrente = true });   // pórtico
                    for (int k = 0; k < 12; k++)
                    {
                        var p = Pt(uP, -Wp / 2 + 1.3f + k * (Wp - 2.6f) / 11);
                        m.Pieza(Geo.Cilindro(0.62f, 0.7f, 11.2f, 14), "#efe3c8", new Vector3(p.x, 5.6f, p.y));
                    }
                    var dc = Pt(fr * (L - 14), 0);   // cúpula
                    m.Pieza(Geo.Cilindro(6.6f, 6.8f, 7, 20), "#efe3c8", new Vector3(dc.x, hN + 3.4f, dc.y));
                    m.Pieza(Geo.Ico(1, 1), "#7f9099", new Vector3(dc.x, hN + 6.9f, dc.y), default, new Vector3(6.7f, 6.7f, 6.7f));
                    m.Pieza(Geo.Cilindro(1.1f, 1.3f, 2.4f, 10), "#efe3c8", new Vector3(dc.x, hN + 14, dc.y));
                    Campo.Registrar(new[] { Pt(uF, Wp / 2), Pt(uP, Wp / 2), Pt(uP, -Wp / 2), Pt(uF, -Wp / 2) });
                }
                else
                {
                    float tw = Mathf.Min(4.2f, A * 0.6f), uT = fr * (L - tw);   // torre con techo piramidal al frente
                    var bas = new[] { Pt(uT + tw, tw), Pt(uT + tw, -tw), Pt(uT - tw, -tw), Pt(uT - tw, tw) };
                    for (int j = 0; j < 4; j++) Pared(G(B, "revoque*" + colP), bas[j], bas[(j + 1) % 4], 0, 22, j);
                    var tp = Pt(uT, 0);
                    m.Pieza(Geo.Cono(tw * 1.5f, 6, 4), "#7d858a", new Vector3(tp.x, 25, tp.y), new Vector3(0, Mathf.Atan2(uz, ux) + Mathf.PI / 4, 0));
                    m.Pieza(Geo.Cilindro(0.1f, 0.1f, 2.2f, 6), "#7f9099", new Vector3(tp.x, 29, tp.y));
                }
            }
        }

        // farolas en las esquinas de las manzanas cercanas
        void Farolas(Nodo m)
        {
            var poste = Geo.Cilindro(0.05f, 0.08f, 4.2f, 5).Trasladar(0, 2.1f, 0);
            var farol = Geo.Cilindro(0.2f, 0.13f, 0.42f, 4).Trasladar(0, 4.45f, 0);
            var sombrero = Geo.Cono(0.28f, 0.2f, 4).Trasladar(0, 4.78f, 0);
            for (int bi = 0; bi < bloques.Count; bi++)
            {
                var pts = bloques[bi];
                var c = (pts[0] + pts[2]) / 2;
                if (c.magnitude > 650) continue;
                for (int k = 0; k < 4; k++)
                {
                    if (Hash(bi * 7 + k * 13) < 0.45f) continue;
                    var dir = (pts[k] - c).normalized;
                    var p = pts[k] + dir * 1.6f;
                    var pos = new Vector3(p.x, 0, p.y);
                    m.Pieza(poste, "#2d2a28", pos);
                    m.Pieza(farol, "farol", pos);
                    m.Pieza(sombrero, "#2d2a28", pos);
                }
            }
        }

        // ---------- suelo de tierra y río según la máscara del plano ----------
        void Suelo(Transform raiz)
        {
            var d = Datos;
            Vector2 a = d.AMundo(d.Marco.x, d.Marco.y), b = d.AMundo(d.Marco.z, d.Marco.w);
            var suelo = new Geo();
            suelo.QuadUV(new Vector3(a.x, 0, a.y), new Vector3(b.x, 0, a.y), new Vector3(b.x, 0, b.y), new Vector3(a.x, 0, b.y), a / 5, new Vector2(b.x, a.y) / 5, b / 5, new Vector2(a.x, b.y) / 5);
            var n = new Nodo("suelo");
            n.Pieza(suelo, "suelo_ciudad", Vector3.zero);
            var agua = Campo.agua;
            if (agua != null)   // tiras de agua por fila de la máscara
            {
                var g = new Geo();
                float pxW = d.Ancho / agua.W, pxH = d.Alto / agua.H;
                int iy0 = Mathf.FloorToInt(d.Marco.y / pxH), iy1 = Mathf.CeilToInt(d.Marco.w / pxH), ix0 = Mathf.FloorToInt(d.Marco.x / pxW), ix1 = Mathf.CeilToInt(d.Marco.z / pxW);
                for (int iy = iy0; iy < iy1; iy++)
                {
                    int ini = -1;
                    for (int ix = ix0; ix <= ix1; ix++)
                    {
                        bool w = ix < ix1 && agua.AguaPx(ix, iy);
                        if (w && ini < 0) ini = ix;
                        else if (!w && ini >= 0)
                        {
                            Vector2 p0 = d.AMundo(ini * pxW, iy * pxH), p1 = d.AMundo(ix * pxW, (iy + 1) * pxH);
                            g.QuadUV(new Vector3(p0.x, 0.03f, p0.y), new Vector3(p1.x, 0.03f, p0.y), new Vector3(p1.x, 0.03f, p1.y), new Vector3(p0.x, 0.03f, p1.y), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                            ini = -1;
                        }
                    }
                }
                n.Pieza(g, "agua_ciudad", Vector3.zero);
            }
            var t = n.Instanciar(raiz, Registrar);
            foreach (var r in t.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            var piso = new GameObject("piso");
            piso.transform.SetParent(raiz, false);
            var bc = piso.AddComponent<BoxCollider>();
            var cen = (a + b) / 2;
            bc.center = Conv.P(cen.x, -0.5f, cen.y);
            bc.size = new Vector3(Mathf.Abs(b.x - a.x), 1, Mathf.Abs(b.y - a.y));
        }
    }
}
