using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Argentina1860
{
    /// <summary>
    /// Fauna y eventos del pago (fauna.js): animales que pastan, caminan y huyen; caballos en potreros, corrales y palenque;
    /// carretas de bueyes por el camino real; caranchos en vuelo; un director que hace pasar algo cerca del jugador
    /// (jinetes al galope por el camino real, perdices que se vuelan) y el minijuego del arreo de vacas.
    /// Toda la lógica usa coordenadas del prototipo (x este, z sur) y se pasa a Unity al ubicar cada modelo.
    /// </summary>
    public partial class Fauna : MonoBehaviour
    {
        public JuegoPago juego;

        // ---------- utilidades ----------
        static readonly Collider[] buf = new Collider[24];
        Transform raiz;
        int frame;
        float tiempo;

        static float Alt(float x, float z) => Terreno.Altura(new Vector3(x, 0, -z));
        static bool EnMapa(float x, float z) => Mathf.Abs(x) < 1560 && Mathf.Abs(z) < 1160 && !BordesPago.Fuera(x, z);
        static float Mix(float a, float b, float t) => a + (b - a) * t;
        static float Hs(double i) => MapaPago.Hs(i);
        static float Ang(float a) => Mathf.Atan2(Mathf.Sin(a), Mathf.Cos(a));

        /// Lugar libre: fuera del agua y sin colisionadores (casas, cercos, personas, animales) en el radio.
        public static bool Libre(float x, float z, float r = 0.7f, Transform ignorar = null)
        {
            if (MapaPago.Bloqueado(x, z)) return false;
            int n = Physics.OverlapSphereNonAlloc(new Vector3(x, Alt(x, z) + 1.0f, -z), r, buf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = buf[i];
                if (c is TerrainCollider) continue;
                if (ignorar != null && c.transform.IsChildOf(ignorar)) continue;
                return false;
            }
            return true;
        }

        static void Ubicar(Transform g, float x, float z, float yaw, float pitch = 0, float dy = 0)
        {
            g.position = new Vector3(x, Alt(x, z) + dy, -z);
            g.rotation = Conv.Rot(pitch, yaw, 0);
        }

        static void Activo(Transform g, bool on)
        {
            if (g.gameObject.activeSelf != on) g.gameObject.SetActive(on);
        }

        static Collider Capsula(Transform g, float r)
        {
            var c = g.gameObject.AddComponent<CapsuleCollider>();
            c.radius = r; c.height = Mathf.Max(2 * r, 1.6f); c.center = new Vector3(0, 0.9f, 0);
            return c;
        }

        static void Caja(Transform padre, Vector3 centro, Vector3 tam, Quaternion rot)
        {
            var go = new GameObject("colision");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = centro;
            go.transform.localRotation = rot;
            go.AddComponent<BoxCollider>().size = tam;
        }

        void Decir(string t) { if (juego != null) juego.Decir(t); }

        // ---------- especies ----------
        class Esp { public float vel, huida, velH, radio; public bool huye; }

        static readonly Dictionary<string, Esp> ESP = new Dictionary<string, Esp>
        {
            ["venado"] = new Esp { vel = 1.4f, huida = 14, velH = 8.5f, radio = 40, huye = true },
            ["zorro"] = new Esp { vel = 1.6f, huida = 14, velH = 7.5f, radio = 55, huye = true },
            ["vizcacha"] = new Esp { vel = 0.7f, huida = 9, velH = 5, radio = 8, huye = true },
            ["carpincho"] = new Esp { vel = 0.9f, huida = 10, velH = 5, radio = 24, huye = true },
            ["coipo"] = new Esp { vel = 0.8f, huida = 9, velH = 5, radio = 16, huye = true },
            ["puma"] = new Esp { vel = 0.9f, huida = 8, velH = 6, radio = 20, huye = false },
            ["vaca"] = new Esp { vel = 0.6f, huida = 5, velH = 3, radio = 30, huye = true },
            ["nandu"] = new Esp { vel = 1.4f, huida = 16, velH = 10, radio = 60, huye = true },
            ["perdiz"] = new Esp { vel = 1.1f, huida = 8, velH = 6, radio = 20, huye = true },
            ["peludo"] = new Esp { vel = 0.8f, huida = 7, velH = 4.5f, radio = 20, huye = true },
            ["yarara"] = new Esp { vel = 0, huida = 0, velH = 0, radio = 0, huye = false },
        };
        static readonly Dictionary<string, float> RCOL = new Dictionary<string, float> { ["vaca"] = 0.95f, ["venado"] = 0.55f, ["nandu"] = 0.5f, ["carpincho"] = 0.5f, ["puma"] = 0.5f };

        class Animal
        {
            public string esp; public Esp sp; public Bicho m;
            public float x, z, hx, hz, t, ang, yaw, pitch, fase, deriva, tx, tz;
            public string estado = "pastar";
            public bool arreo, corral, salio;
            public List<Vector2> ruta;
            public Transform G => m.raiz;
            // conducta (Fauna.Conducta.cs)
            public Manada manada;
            public Vector2 ofs, nido, casa;
            public float alerta, quieto, echado, vuelo, vueloT, comida, cuidado = 1;
            public bool muerto, escondido, manso;
            public float muertoT;
            public Animal presa;
            public Vector2 huidaDe;
        }

        readonly List<Animal> animales = new List<Animal>();

        Animal Poner(string esp, float x, float z, float escala = 1)
        {
            var a = new Animal { esp = esp, sp = ESP[esp], m = ModelosFauna.Crear(esp, raiz), x = x, z = z, hx = x, hz = z, tx = x, tz = z };
            a.t = Hs(x * 3.1 + z) * 4; a.ang = Hs(x + z * 2.3) * 6.28f; a.yaw = a.ang; a.fase = Hs(x) * 6; a.deriva = (Hs(z) - 0.5f) * 1.2f;
            if (RCOL.TryGetValue(esp, out var r)) Capsula(a.G, r);
            a.G.localScale = Vector3.one * escala;
            Ubicar(a.G, x, z, a.ang);
            animales.Add(a);
            return a;
        }

        void Bailar(Bicho m, ref float fase, float mover, float dt)
        {
            float w = mover > 0 ? Mathf.Sin(fase += dt * (mover * 2.4f + 3)) * (mover > 3 ? 0.9f : 0.5f) : 0;
            m.Bailar(w);
        }

        // ---------- caballos ----------
        class Zona { public float x0, x1, z0, z1; public Vector2 puerta, adentro, afuera; }

        class Caballo
        {
            public Bicho c; public float x, z, hx, hz, ang, yaw, t, fase, pasto, marcha, salidaT, radio = 14, tx, tz;
            public string tipo = "libre", estado = "pastar";
            public Zona zona; public List<Vector2> ruta; public bool fuera, volviendo;
        }

        readonly List<Caballo> caballos = new List<Caballo>();
        static readonly string[] COLORES = { "#7a4a2a", "#2a2018", "#a8754a", "#5a3a24", "#c9a779", "#8a5a34" };

        static Zona CorralZ(float cx, float cz, float w, float l, float lado) => new Zona
        {
            x0 = cx - w / 2 + 1.8f, x1 = cx + w / 2 - 1.8f, z0 = cz - l / 2 + 1.8f, z1 = cz + l / 2 - 1.8f,
            puerta = new Vector2(cx, cz + lado * l / 2), adentro = new Vector2(cx, cz + lado * (l / 2 - 3.5f)), afuera = new Vector2(cx, cz + lado * (l / 2 + 7)),
        };

        // ---------- carretas, vuelos, eventos ----------
        class Carreta { public Bicho b; public float s, dir, fase; }
        readonly List<Carreta> carretas = new List<Carreta>();
        List<Vector2> ruta;
        float[] acum;
        float largoRuta;

        class Vuelo { public Bicho b; public Vector2 c; public float r, f, h; public Animal carcasa; public float bajada, comerT; public Vector3 pos; }
        readonly List<Vuelo> vuelos = new List<Vuelo>();

        NPC pascual;
        /// Mientras una misión usa a Juan Maidana (la llegada en su carreta), la carreta del camino no lo mueve.
        public static bool PascualPrestado;

        int sem = 1;
        float Rnd() => Hs(sem++);

        // ---------- arranque ----------
        IEnumerator Start()
        {
            yield return null;   // espera a que las casas y cercos generen sus colisiones
            Physics.SyncTransforms();
            raiz = new GameObject("Animales").transform;
            raiz.SetParent(transform, false);
            AgregarEspecies();
            Poblar();
            ConstruirCorralArreo();
            PoblarConducta();
            foreach (var n in FindObjectsByType<NPC>()) if (n.Ficha != null && n.Ficha.id == "pascual") pascual = n;
            if (pascual != null) pascual.ControladoAfuera = true;
            listo = true;
        }

        bool listo;

        Func<Vector2> Elipse(Vector2 c, float rx, float ry) => () =>
        {
            float t = Rnd() * 6.28f, r = Mathf.Sqrt(Rnd());
            var m = MapaPago.K(c);
            return new Vector2(m.x + Mathf.Cos(t) * rx * 1000 * r, m.y + Mathf.Sin(t) * ry * 1000 * r);
        };

        Func<Vector2> RectKm(float x0, float y0, float x1, float y1) => () =>
        {
            float a = Rnd(), b = Rnd();
            return MapaPago.K(Mix(x0, x1, a), Mix(y0, y1, b));
        };

        void Sembrar(string esp, int grupos, int tam, Func<Vector2> region, float escala = 1)
        {
            for (int gi = 0; gi < grupos; gi++)
            {
                Vector2? c = null;
                for (int k = 0; k < 40 && c == null; k++)
                {
                    var p = region();
                    if (EnMapa(p.x, p.y) && Libre(p.x, p.y) && !Sembrado.Ocupa(p.x, p.y) && MapaPago.Uso(p.x, p.y) != UsoLote.Chacra) c = p;
                }
                if (c == null) continue;
                for (int i = 0; i < tam; i++)
                {
                    float x = c.Value.x + (Rnd() - 0.5f) * 12, z = c.Value.y + (Rnd() - 0.5f) * 12;
                    if (!Libre(x, z)) { x = c.Value.x; z = c.Value.y; }
                    Poner(esp, x, z, escala * Mix(0.92f, 1.08f, Rnd()));
                }
            }
        }

        void Poblar()
        {
            Vector2 K(float a, float b) => MapaPago.K(a, b);
            // pajonal y campo abierto
            var PAJONAL = RectKm(0.0f, 1.55f, 1.5f, 2.4f);
            var ABIERTO = RectKm(0.0f, 0.0f, 3.2f, 2.4f);
            var mc = MapaPago.MONTE_C;
            Sembrar("nandu", 3, 3, PAJONAL); Sembrar("nandu", 1, 3, ABIERTO);
            Sembrar("venado", 3, 2, PAJONAL); Sembrar("venado", 1, 3, ABIERTO);
            Sembrar("peludo", 6, 1, PAJONAL); Sembrar("zorro", 4, 1, PAJONAL); Sembrar("zorro", 2, 1, Elipse(mc, MapaPago.MONTE_RX, MapaPago.MONTE_RY));
            Sembrar("perdiz", 5, 3, PAJONAL); Sembrar("perdiz", 2, 3, ABIERTO);
            Sembrar("yarara", 6, 1, PAJONAL);
            Sembrar("puma", 2, 1, Elipse(mc, MapaPago.MONTE_RX * 1.05f, MapaPago.MONTE_RY * 1.05f));
            // arroyo y laguna
            Func<Vector2> ORILLA = () =>
            {
                float t = Rnd() * 6.28f, r = 1.15f + Rnd() * 0.5f;
                var m = MapaPago.K(MapaPago.LAGUNA_C);
                return new Vector2(m.x + Mathf.Cos(t) * MapaPago.LAGUNA_RX * 1000 * r, m.y + Mathf.Sin(t) * MapaPago.LAGUNA_RY * 1000 * r);
            };
            Sembrar("carpincho", 3, 3, ORILLA); Sembrar("coipo", 3, 2, ORILLA);
            // vizcacheras: montículo con madrigueras y vizcachas alrededor
            foreach (var (kx, ky) in new[] { (0.9f, 2.1f), (0.4f, 1.7f), (1.25f, 1.75f) })
            {
                var p = K(kx, ky);
                var mont = ModelosFauna.Vizcachera(raiz);
                mont.position = new Vector3(p.x, Alt(p.x, p.y), -p.y);
                for (int i = 0; i < 4; i++)
                {
                    float x = p.x + (Rnd() - 0.5f) * 8, z = p.y + (Rnd() - 0.5f) * 8;
                    Poner("vizcacha", x, z);
                }
            }
            // yarará cerca de ranchos
            foreach (var (kx, ky) in new[] { (0.62f, 0.86f), (1.0f, 1.04f), (2.2f, 1.3f) }) { var p = K(kx, ky); Poner("yarara", p.x + 9, p.y + 7); }

            // vida cerca del punto de partida (rancho de peones): que desde el primer minuto se vea algo moverse
            var s0 = K(0.59f, 0.9f);
            Func<Vector2> Anillo(float r0, float r1) => () =>
            {
                float t = Rnd() * 6.28f, r = Mix(r0, r1, Rnd());
                return new Vector2(s0.x + Mathf.Cos(t) * r, s0.y + Mathf.Sin(t) * r);
            };
            Sembrar("perdiz", 3, 3, Anillo(25, 60)); Sembrar("vaca", 2, 3, Anillo(45, 90)); Sembrar("nandu", 1, 2, Anillo(80, 120));
            Sembrar("venado", 1, 2, Anillo(100, 140)); Sembrar("vizcacha", 1, 2, Anillo(55, 85)); Sembrar("zorro", 1, 1, Anillo(90, 130));

            // ganado y caballos en los potreros, corrales y la posta
            Vector2 p0 = K(0.17f, 0.08f), p1 = K(0.86f, 0.42f);
            Func<Vector2> EnPotrero = () => { float a = Rnd(), b = Rnd(); return new Vector2(Mix(p0.x + 12, p1.x - 12, a), Mix(p0.y + 12, p1.y - 12, b)); };
            Sembrar("vaca", 5, 3, EnPotrero);

            Caballo Pon(float x, float z, float rot, int i, bool? calc = null, string tipo = "libre", Zona zona = null)
            {
                var b = ModelosFauna.Caballo(raiz, COLORES[i % COLORES.Length], calc ?? i % 2 == 0);
                b.raiz.localScale = Vector3.one * 0.95f;
                Capsula(b.raiz, 0.85f);
                var h = new Caballo { c = b, x = x, z = z, hx = x, hz = z, ang = rot, yaw = rot, tipo = tipo, zona = zona };
                h.t = Rnd() * 6; h.fase = Rnd() * 20;
                Ubicar(b.raiz, x, z, rot);
                caballos.Add(h);
                return h;
            }
            Vector2 e = K(0.75f, 0.75f), q = K(2.15f, 1.25f);
            for (int i = 0; i < 6; i++) { var p = EnPotrero(); float r = Rnd() * 6.28f; if (Libre(p.x, p.y)) Pon(p.x, p.y, r, i); }
            var zDoma = CorralZ(e.x - 26, e.y + 22, 18, 18, -1);
            var zGrande = CorralZ(e.x + 12, e.y + 34, 32, 22, -1);
            var zRemuda = CorralZ(q.x - 6, q.y - 40, 26, 16, 1);
            var doma = new[] { (e.x - 26, e.y + 22), (e.x - 22, e.y + 18) };
            for (int i = 0; i < doma.Length; i++) Pon(doma[i].Item1, doma[i].Item2, 1 + i, i, null, "corral", zDoma);
            var grande = new[] { (e.x + 8, e.y + 34), (e.x + 16, e.y + 32), (e.x + 12, e.y + 38) };
            for (int i = 0; i < grande.Length; i++) Pon(grande[i].Item1, grande[i].Item2, 2 + i, i + 2, null, "corral", zGrande);
            var remuda = new[] { (q.x - 10, q.y - 40), (q.x - 3, q.y - 38), (q.x - 8, q.y - 44), (q.x - 1, q.y - 44) };
            for (int i = 0; i < remuda.Length; i++) Pon(remuda[i].Item1, remuda[i].Item2, 0.5f + i, i + 1, null, "corral", zRemuda);
            var atados = new[] { -2.7f, 0f, 2.7f };
            for (int i = 0; i < atados.Length; i++) Pon(q.x - 4 + atados[i], q.y + 8, Mathf.PI, i + 3, true, "atado");   // atados al palenque de la posta

            // carretas de bueyes que recorren el camino real
            ruta = MapaPago.Suavizar(MapaPago.CAMINO, 40).ConvertAll(k => MapaPago.K(k));
            acum = new float[ruta.Count];
            for (int i = 1; i < ruta.Count; i++) acum[i] = acum[i - 1] + Vector2.Distance(ruta[i], ruta[i - 1]);
            largoRuta = acum[acum.Length - 1];
            foreach (var (f, dir) in new[] { (0.15f, 1f), (0.55f, -1f), (0.85f, 1f) })
            {
                var b = ModelosFauna.Carreta(raiz);
                Caja(b.raiz, new Vector3(0, 1.5f, -3f), new Vector3(2.6f, 3, 11), Quaternion.identity);
                carretas.Add(new Carreta { b = b, s = largoRuta * f, dir = dir });
            }

            // caranchos: vuelan en círculos sobre el pajonal y el monte
            var vs = new[] { (0.7f, 2.0f, 70f), (2.3f, 0.5f, 55f), (1.7f, 1.6f, 90f), (0.4f, 0.9f, 60f) };
            for (int i = 0; i < vs.Length; i++) vuelos.Add(new Vuelo { b = ModelosFauna.Crear("carancho", raiz), c = K(vs[i].Item1, vs[i].Item2), r = vs[i].Item3, f = i * 1.7f, h = 28 + i * 6 });
        }

        (float x, float z, float ang) SobreRuta(float s)
        {
            int i = 1;
            while (i < acum.Length - 1 && acum[i] < s) i++;
            float d = acum[i] - acum[i - 1], t = d > 0 ? (s - acum[i - 1]) / d : 0;
            return (Mix(ruta[i - 1].x, ruta[i].x, t), Mix(ruta[i - 1].y, ruta[i].y, t), Mathf.Atan2(ruta[i].x - ruta[i - 1].x, ruta[i].y - ruta[i - 1].y));
        }

        // =====================================================================
        // ---------- minijuego: arreo de vacas a un corral con tranquera ----------
        // Las vacas se alejan de vos: ponete del lado contrario al corral y empujalas hasta la puerta; después cerrá la tranquera (E).
        public class ResultadoArreo { public bool ok; public int dentro, pago; public float xp; }

        public class Tranquera
        {
            public float x, z;
            public bool abierta = true;
            public float ang = Mathf.PI / 2;
            public Transform panel;
            public Collider col;
            public Vector3 PosUnity => new Vector3(x, Alt(x, z), -z);
        }

        const float CW = 24, CL = 20, GAP = 5;
        public Vector2 Rodeo { get; private set; }
        public Tranquera Tranq { get; } = new Tranquera();
        public bool ArreoActivo { get; private set; }
        public float ArreoTiempo { get; private set; }
        public int ArreoDentro { get; private set; }
        public const int ArreoTotal = 5;
        public Func<int> NivelArreo = () => 1;
        public Action<string> HudArreo;
        public Action<ResultadoArreo> FinArreo;
        readonly List<Animal> vacasArreo = new List<Animal>();
        float hudT;

        bool DentroCorral(float x, float z) => Mathf.Abs(x - Rodeo.x) < CW / 2 - 0.6f && Mathf.Abs(z - Rodeo.y) < CL / 2 - 0.6f;
        int ContarDentro() { int n = 0; foreach (var a in vacasArreo) if (DentroCorral(a.x, a.z)) n++; return n; }

        void ConstruirCorralArreo()
        {
            var e = MapaPago.K(0.75f, 0.75f);
            // lugar para el corral: libre de construcciones, fuera de caminos, patios y lotes sembrados
            bool Apto(float x, float z)
            {
                if (!Libre(x, z) || MapaPago.DistCamino(x, z) < 5 || Sembrado.Ocupa(x, z)) return false;
                var u = MapaPago.Uso(x, z);
                if (u == UsoLote.Chacra || u == UsoLote.Quinta) return false;
                foreach (var pt in MapaPago.Patios) if (Vector2.Distance(pt, new Vector2(x, z)) < 16) return false;
                return true;
            }
            bool OkRing(float x, float z)
            {
                if (!Apto(x, z)) return false;
                for (int k = 0; k < 12; k++)
                    foreach (var r in new[] { 10f, 17f })
                        if (!Apto(x + Mathf.Cos(k * 0.5236f) * r, z + Mathf.Sin(k * 0.5236f) * r)) return false;
                return true;
            }
            Rodeo = new Vector2(e.x - 20, e.y + 80);
            bool hallado = false;
            for (int r = 0; r < 220 && !hallado; r += 8)
                for (int k = 0; k < 12 && !hallado; k++)
                {
                    float x = e.x - 20 + Mathf.Cos(k * 0.52f) * r, z = e.y + 80 + Mathf.Sin(k * 0.52f) * r;
                    if (OkRing(x, z)) { Rodeo = new Vector2(x, z); hallado = true; }
                }

            float cx = Rodeo.x, cz = Rodeo.y, hw = CW / 2, hl = CL / 2;
            var corral = new GameObject("Corral del arreo").transform;
            corral.SetParent(transform, false);
            var n = new Nodo("corral");

            // piso de barro que sigue el terreno
            const int SX = 16, SZ = 14;
            var barro = new Geo();
            Vector3 P(int i, int j) { float x = cx - hw + CW * i / SX, z = cz - hl + CL * j / SZ; return new Vector3(x, Alt(x, z) + 0.07f, z); }
            for (int j = 0; j < SZ; j++)
                for (int i = 0; i < SX; i++)
                {
                    barro.Tri(P(i, j), P(i, j + 1), P(i + 1, j));
                    barro.Tri(P(i + 1, j), P(i, j + 1), P(i + 1, j + 1));
                }
            n.Pieza(barro, "#4a3626", Vector3.zero);
            for (int i = 0; i < 7; i++)   // charcos y manchas más oscuras
            {
                float x = cx + (Random.value - 0.5f) * (CW - 5), z = cz + (Random.value - 0.5f) * (CL - 5), r = 0.8f + Random.value * 1.4f;
                n.Pieza(Geo.Cilindro(r, r, 0.01f, 8), i % 2 == 1 ? "#2d211a" : "#5b4636", new Vector3(x, Alt(x, z) + 0.1f, z));
            }
            // cerco: postes + dos rieles por tramo; el lado sur deja el hueco de la tranquera
            const string matP = "#6a4a2a";
            void Poste(float x, float z) => n.Pieza(Geo.Cilindro(0.13f, 0.16f, 1.6f, 5).Trasladar(0, 0.8f, 0), matP, new Vector3(x, Alt(x, z), z));
            void Riel(float x0, float z0, float x1, float z1)
            {
                float L = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0)), a = Mathf.Atan2(x1 - x0, z1 - z0);
                float mx = (x0 + x1) / 2, mz = (z0 + z1) / 2, ym = Alt(mx, mz);
                foreach (var y in new[] { 0.55f, 1.15f }) n.Pieza(Geo.Caja(0.1f, 0.12f, L), matP, new Vector3(mx, ym + y, mz), new Vector3(0, a, 0));
                Caja(corral, Conv.P(mx, ym + 0.8f, mz), new Vector3(0.5f, 1.6f, L), Conv.Yaw(a));
            }
            void Lado(float x0, float z0, float x1, float z1)
            {
                float L = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
                int m = Mathf.Max(1, Conv.RoundJS(L / 3));
                for (int k = 0; k <= m; k++) Poste(Mix(x0, x1, (float)k / m), Mix(z0, z1, (float)k / m));
                Riel(x0, z0, x1, z1);
            }
            Lado(cx - hw, cz - hl, cx + hw, cz - hl); Lado(cx + hw, cz - hl, cx + hw, cz + hl); Lado(cx - hw, cz - hl, cx - hw, cz + hl);
            Lado(cx - hw, cz + hl, cx - GAP / 2, cz + hl); Lado(cx + GAP / 2, cz + hl, cx + hw, cz + hl);
            // tranquera: un panel de 5 m que gira en el poste este del hueco
            var panel = n.Hijo("tranquera", new Vector3(cx + GAP / 2, Alt(cx, cz + hl), cz + hl));
            panel.rot = new Vector3(0, Tranq.ang, 0);
            foreach (var y in new[] { 0.35f, 0.75f, 1.15f }) panel.Pieza(Geo.Caja(GAP, 0.11f, 0.09f).Trasladar(-GAP / 2, y, 0), "#8a5a34", Vector3.zero);
            foreach (var sx in new[] { -0.1f, -GAP + 0.1f }) panel.Pieza(Geo.Caja(0.1f, 1.3f, 0.1f).Trasladar(sx, 0.65f, 0), "#8a5a34", Vector3.zero);
            panel.Pieza(Geo.Caja(Mathf.Sqrt(GAP * GAP + 0.81f), 0.08f, 0.07f), "#8a5a34", new Vector3(-GAP / 2, 0.75f, 0), new Vector3(0, 0, Mathf.Atan2(0.8f, GAP)));
            // mástil con bandera roja para ubicar el corral desde lejos
            n.Pieza(Geo.Cilindro(0.08f, 0.1f, 7, 5).Trasladar(0, 3.5f, 0), matP, new Vector3(cx - hw, Alt(cx - hw, cz - hl), cz - hl));
            n.Pieza(Geo.Caja(1.6f, 1.0f, 0.05f).Trasladar(0.8f, 0, 0), "#c72d11", new Vector3(cx - hw, Alt(cx - hw, cz - hl) + 6.3f, cz - hl));

            var mapa = new Dictionary<Nodo, Transform>();
            n.Instanciar(corral, null, mapa);
            Tranq.panel = mapa[panel];
            Tranq.x = cx; Tranq.z = cz + hl;
            var cg = new GameObject("colision");
            cg.transform.SetParent(Tranq.panel, false);
            cg.transform.localPosition = new Vector3(-GAP / 2, 0.75f, 0);
            var bc = cg.AddComponent<BoxCollider>();
            bc.size = new Vector3(GAP, 1.5f, 0.3f);
            Tranq.col = bc;
            bc.enabled = !Tranq.abierta;
        }

        void PonerTranquera(bool abierta)
        {
            Tranq.abierta = abierta;
            if (Tranq.col != null) Tranq.col.enabled = !abierta;
        }

        void Finalizar(bool ok, int dentro)
        {
            ArreoActivo = false;
            int niv = NivelArreo();
            int pago = ok ? 8 + niv + Mathf.FloorToInt(Mathf.Max(0, ArreoTiempo) / 15) : dentro * 3;
            float xp = ok ? 4 : dentro * 0.8f;
            foreach (var a in vacasArreo)
            {
                a.arreo = false; a.corral = DentroCorral(a.x, a.z); a.salio = false; a.ruta = null; a.hx = a.x; a.hz = a.z; a.estado = "pastar"; a.t = 2;
            }
            FinArreo?.Invoke(new ResultadoArreo { ok = ok, dentro = dentro, pago = pago, xp = xp });
        }

        /// Abre o cierra la tranquera (tecla E cerca de ella).
        public void AlternarTranquera()
        {
            if (Tranq.abierta)
            {
                if (ArreoActivo)
                {
                    int n = ContarDentro();
                    if (n < ArreoTotal) { Decir($"Todavía faltan {ArreoTotal - n} vacas adentro del corral."); return; }
                    PonerTranquera(false); Decir("Tranquera cerrada."); Finalizar(true, n); return;
                }
                PonerTranquera(false); Decir("Tranquera cerrada.");
            }
            else { PonerTranquera(true); Decir("Tranquera abierta."); }
        }

        /// Suelta cinco vacas cerca del corral y arranca el reloj (150 s).
        public void IniciarArreo()
        {
            if (ArreoActivo || !listo) return;
            PonerTranquera(true);
            float th0 = 0.3f + Random.value * 2.4f;
            for (int i = 0; i < ArreoTotal; i++)
            {
                float x = Rodeo.x + 55, z = Rodeo.y;
                for (int k = 0; k < 60; k++)
                {
                    float th = th0 + (Random.value - 0.5f) * 1.3f, r = 27 + Random.value * 10, cx = Rodeo.x + Mathf.Cos(th) * r, cz = Rodeo.y + Mathf.Sin(th) * r;
                    if (Libre(cx, cz) && EnMapa(cx, cz)) { x = cx; z = cz; break; }
                }
                Animal a = i < vacasArreo.Count ? vacasArreo[i] : null;
                if (a == null) { a = Poner("vaca", x, z); vacasArreo.Add(a); }
                else { a.x = x; a.z = z; }
                a.arreo = true; a.corral = false; a.salio = false; a.ruta = null; a.hx = x; a.hz = z; a.deriva = (Random.value - 0.5f) * 0.9f;
                Activo(a.G, true);
            }
            ArreoActivo = true; ArreoTiempo = 150; ArreoDentro = 0; hudT = 0;
        }

        void Arrear(float dt, Vector2 p)
        {
            // la tranquera gira suave hacia su posición
            float obj = Tranq.abierta ? Mathf.PI / 2 : 0;
            Tranq.ang += (obj - Tranq.ang) * Mathf.Min(1, dt * 4);
            if (Tranq.panel != null) Tranq.panel.localRotation = Conv.Rot(0, Tranq.ang, 0);
            if (!ArreoActivo) return;
            ArreoTiempo -= dt;
            float radio = Mathf.Max(7, 13 - (NivelArreo() - 1) * 0.8f);
            foreach (var a in vacasArreo)
            {
                float dx = a.x - p.x, dz = a.z - p.y, d = Mathf.Sqrt(dx * dx + dz * dz), mover = 0;
                if (d < radio)
                {
                    float ang = Mathf.Atan2(dx, dz) + a.deriva * 0.5f;
                    mover = 2.2f + (1 - d / radio) * 2.6f;
                    float sx = Mathf.Sin(ang) * mover * dt, sz = Mathf.Cos(ang) * mover * dt;
                    bool Ok(float nx, float nz) => Libre(nx, nz, 0.7f, a.G);
                    if (Ok(a.x + sx, a.z + sz)) { a.x += sx; a.z += sz; a.ang = ang; }
                    else if (Ok(a.x + sx, a.z)) { a.x += sx; a.ang = Mathf.Atan2(sx, 0.001f); }
                    else if (Ok(a.x, a.z + sz)) { a.z += sz; a.ang = Mathf.Atan2(0.001f, sz); }
                    else { mover = 0; a.deriva = -a.deriva; }
                }
                a.yaw += Ang(a.ang - a.yaw) * Mathf.Min(1, dt * 6);
                Ubicar(a.G, a.x, a.z, a.yaw);
                Bailar(a.m, ref a.fase, mover, dt);
            }
            ArreoDentro = ContarDentro();
            hudT -= dt;
            if (hudT <= 0 && HudArreo != null)
            {
                hudT = 0.5f;
                HudArreo($"Empujá las {ArreoTotal} vacas al corral (bandera roja, al sur de la estancia) y cerrá la tranquera con E. Adentro: {ArreoDentro}/{ArreoTotal} · Tiempo: {Mathf.Max(0, Mathf.CeilToInt(ArreoTiempo))} s");
            }
            if (ArreoTiempo <= 0) Finalizar(false, ArreoDentro);
        }

        // =====================================================================
        // ---------- director de eventos: siempre pasa algo cerca de donde vas ----------
        class Item { public Bicho m; public float off, lat, ox, oz, dx, dz, v, h, f, ret, fase; public bool caballo; }

        class Evento
        {
            public string tipo; public Transform g; public List<Item> items = new List<Item>();
            public float sx, sz, dx, dz, vel, t, largo, s, sg, lat, cx, cz;
            public bool dijo; public Bicho c; public AnimPersonaje jinete;
        }

        readonly List<Evento> evs = new List<Evento>();
        float prox = 3, vx = 0, vz = 1, quieto;
        Vector2? previo;

        static readonly string[] FRASES =
        {
            "¡Ave María Purísima!", "¡Buenas, paisano! ¡Que Dios lo guarde!", "¡Ey, amigo! ¡Cuidado con los pumas del monte!",
            "¡Voy con apuro, que hay novedades en la frontera!", "¡Buen día! ¿Anda por trabajo?", "¡Pa' la posta, che, que se viene el temporal!",
        };
        static T Aleat<T>(T[] a) => a[Random.Range(0, a.Length)];

        Transform GrupoEvento(string n)
        {
            var g = new GameObject(n).transform;
            g.SetParent(raiz, false);
            return g;
        }

        bool NuevoJinete(Vector2 p)
        {
            int bi = 0; float bd = 1e9f;
            for (int i = 0; i < ruta.Count; i++) { float d = Vector2.Distance(ruta[i], p); if (d < bd) { bd = d; bi = i; } }
            if (bd > 130) return false;
            float sg = Random.value < 0.5f ? 1 : -1, s0 = Mathf.Clamp(acum[bi] - sg * 95, 12, largoRuta - 12);
            var e = new Evento { tipo = "ruta", g = GrupoEvento("jinete"), s = s0, sg = sg, vel = 9.5f };
            e.c = ModelosFauna.Caballo(e.g, Aleat(new[] { "#7a4a2a", "#2a2018", "#8a5a34", "#c9a779" }), Random.value < 0.5f);
            e.c.raiz.localScale = Vector3.one * 0.95f;
            var montura = new GameObject("jinete").transform;
            montura.SetParent(e.c.raiz, false);
            montura.localPosition = Conv.P(0, 0.74f, 0);
            montura.localRotation = Quaternion.Euler(0, 180, 0);   // el modelo del personaje mira hacia +z; el caballo, hacia -z
            var asp = new Aspecto { estilo = Estilo.Poncho, poncho = Conv.Hex(Aleat(new[] { "#7a3a2a", "#3a4a6a", "#4a5a3a", "#8a6a3a" })), sombrero = Sombrero.Ala, colorSombrero = Conv.Hex("#3a2a20"), bigote = Bigote.Manubrio, pelo = Conv.Hex("#2a1c14"), piel = Conv.Hex("#c99a74") };
            e.jinete = ConstructorPersonaje.Construir(montura, asp, null);
            e.jinete.Montar();
            Caja(e.c.raiz, new Vector3(0, 1.1f, 0), new Vector3(0.9f, 2.2f, 2.4f), Quaternion.identity);   // no se lo atraviesa
            evs.Add(e);
            return true;
        }

        bool NuevaBandada(Vector2 p)
        {
            float dist = 28 + Random.value * 12, cx = p.x + vx * dist, cz = p.y + vz * dist;
            if (!Libre(cx, cz)) return false;
            var e = new Evento { tipo = "bandada", g = GrupoEvento("bandada"), cx = cx, cz = cz };
            for (int i = 0; i < 7; i++)
            {
                var m = ModelosFauna.Crear("perdiz", e.g);
                m.raiz.localScale = Vector3.one * 1.3f;
                var a1 = ModelosFauna.Ala(m.raiz, 1); var a2 = ModelosFauna.Ala(m.raiz, -1);
                a1.localPosition = new Vector3(0, 0.22f, 0); a2.localPosition = new Vector3(0, 0.22f, 0);
                m.alas.Add(a1); m.alas.Add(a2);
                foreach (var l in m.patas) l.localRotation = Conv.Rot(-1.2f, 0, 0);
                float ang = Mathf.Atan2(vx, vz) + (Random.value - 0.5f) * 2.6f;
                e.items.Add(new Item { m = m, ox = (Random.value - 0.5f) * 4, oz = (Random.value - 0.5f) * 4, dx = Mathf.Sin(ang), dz = Mathf.Cos(ang), v = 6 + Random.value * 4, h = 3 + Random.value * 4, f = Random.value * 6, ret = Random.value * 0.5f });
            }
            evs.Add(e);
            return true;
        }

        /// Lo que pasa cerca del jugador además de la fauna del pago: un jinete por el camino real o perdices
        /// que se vuelan de golpe. (Los cruces de ñandúes, venados y tropillas que aparecían de la nada y corrían
        /// en línea recta se sacaron: esos animales ya viven en el pago con conducta propia.)
        void Lanzar(Vector2 p)
        {
            if (Random.value < 0.55f) { if (!NuevoJinete(p)) NuevaBandada(p); }
            else NuevaBandada(p);
        }

        void Director(float dt, Vector2 p)
        {
            if (previo.HasValue)
            {
                float dx = p.x - previo.Value.x, dz = p.y - previo.Value.y, v = Mathf.Sqrt(dx * dx + dz * dz) / Mathf.Max(dt, 1e-3f);
                if (v > 0.6f)
                {
                    vx = Mix(vx, dx / (v * dt), 0.15f); vz = Mix(vz, dz / (v * dt), 0.15f);
                    float l = Mathf.Sqrt(vx * vx + vz * vz); if (l < 1e-4f) l = 1; vx /= l; vz /= l; quieto = 0;
                }
                else quieto += dt;
            }
            previo = p;
            prox -= dt;
            if (prox <= 0) { prox = 20 + Random.value * 25; if (evs.Count < 2) Lanzar(p); }

            for (int k = evs.Count - 1; k >= 0; k--)
            {
                var e = evs[k];
                e.t += dt;
                bool fin = false;
                if (e.tipo == "ruta")
                {
                    e.s += e.sg * e.vel * dt;
                    if (e.s < 8 || e.s > largoRuta - 8) fin = true;
                    else
                    {
                        var pos = SobreRuta(e.s);
                        float ang = pos.ang + (e.sg < 0 ? Mathf.PI : 0);
                        float rx0 = Mathf.Cos(ang), rz0 = -Mathf.Sin(ang), qx = p.x - pos.x, qz = p.y - pos.z;
                        float along = qx * Mathf.Sin(ang) + qz * Mathf.Cos(ang), side = qx * rx0 + qz * rz0;
                        float objL = along > -1 && along < 16 && Mathf.Abs(side - e.lat) < 2.6f ? (side - e.lat > 0 ? -3.2f : 3.2f) : 0;   // esquiva al jugador
                        e.lat += (objL - e.lat) * Mathf.Min(1, dt * 2.5f);
                        float hx = pos.x + rx0 * e.lat, hz = pos.z + rz0 * e.lat;
                        Ubicar(e.c.raiz, hx, hz, ang, 0, Mathf.Abs(Mathf.Sin(e.t * 8)) * 0.1f);
                        e.c.Andar(e.t * 10, 0.9f); e.c.AnimarCaballo(e.t * 2.6f, 0, true);
                        float d = Vector2.Distance(new Vector2(pos.x, pos.z), p);
                        if (!e.dijo && d < 22) { e.dijo = true; Decir(Aleat(FRASES)); }
                        if (e.t > 6 && d > 170) fin = true;
                    }
                }
                else if (e.tipo == "bandada")
                {
                    const float dur = 4.2f;
                    foreach (var it in e.items)
                    {
                        float q = Mathf.Max(0, e.t - it.ret), u = Mathf.Min(1, q / dur), x = e.cx + it.ox + it.dx * it.v * q, z = e.cz + it.oz + it.dz * it.v * q;
                        Ubicar(it.m.raiz, x, z, Mathf.Atan2(it.dx, it.dz), 0, 0.2f + Mathf.Sin(Mathf.PI * Mathf.Min(1, u * 1.1f)) * it.h);
                        float az = Mathf.Sin(e.t * 28 + it.f) * 0.8f;
                        it.m.alas[0].localRotation = Conv.Rot(0, 0, az); it.m.alas[1].localRotation = Conv.Rot(0, 0, -az);
                    }
                    if (e.t > dur + 0.8f) fin = true;
                }
                if (fin) { Destroy(e.g.gameObject); evs.RemoveAt(k); }
            }
        }

        // =====================================================================
        // ---------- caballos con vida propia ----------
        string PasoA(Caballo h, float tx, float tz, float vel, float dt)
        {
            float dx = tx - h.x, dz = tz - h.z;
            if (Mathf.Sqrt(dx * dx + dz * dz) < 0.9f) return "llego";
            float ang = Mathf.Atan2(dx, dz), nx = h.x + Mathf.Sin(ang) * vel * dt, nz = h.z + Mathf.Cos(ang) * vel * dt;
            if (!EnMapa(nx, nz) || !Libre(nx, nz, 0.75f, h.c.raiz)) return "bloq";
            h.x = nx; h.z = nz; h.ang = ang;
            return "ok";
        }

        void Decidir(Caballo h)
        {
            var z = h.zona;
            if (Rnd() < 0.4f) { h.t = 4 + Rnd() * 8; return; }   // sigue pastando
            if (h.tipo == "corral" && z != null && !h.fuera)
            {
                bool algunoFuera = caballos.Exists(o => o.zona == z && o.fuera);
                if (Rnd() < 0.1f && !algunoFuera)   // alguno sale por la puerta
                {
                    h.ruta = new List<Vector2> { z.adentro, z.puerta, z.afuera }; h.fuera = true; h.salidaT = 50 + Rnd() * 60; h.hx = z.afuera.x; h.hz = z.afuera.y; h.estado = "caminar"; h.t = 40;
                    return;
                }
                h.tx = Mix(z.x0, z.x1, Rnd()); h.tz = Mix(z.z0, z.z1, Rnd());
            }
            else { float th = Rnd() * 6.28f, r = 3 + Rnd() * h.radio; h.tx = h.hx + Mathf.Cos(th) * r; h.tz = h.hz + Mathf.Sin(th) * r; }
            h.estado = "caminar"; h.t = 20;
        }

        void MoverCaballos(float dt, Vector2 p, float t)
        {
            foreach (var h in caballos)
            {
                float d = Vector2.Distance(new Vector2(h.x, h.z), p);
                Activo(h.c.raiz, d <= 130);
                if (d > 130) continue;
                h.t -= dt;
                bool anda = false; float objPasto = 0;
                if (h.tipo == "atado") objPasto = Mathf.Sin(t * 0.12f + h.fase) > 0.55f ? 0.45f : 0;
                else if (h.estado == "pastar")
                {
                    objPasto = Mathf.Sin(t * 0.3f + h.fase) > 0.85f ? 0.1f : 1;
                    if (h.fuera && h.ruta == null && (h.salidaT -= dt) <= 0)
                    {
                        h.ruta = new List<Vector2> { h.zona.afuera, h.zona.puerta, h.zona.adentro }; h.volviendo = true; h.estado = "caminar"; h.t = 60;
                    }
                    else if (h.t <= 0) Decidir(h);
                }
                else
                {
                    var dest = h.ruta != null ? h.ruta[0] : new Vector2(h.tx, h.tz);
                    var r = PasoA(h, dest.x, dest.y, 1.4f, dt);
                    if (r == "ok") anda = true;
                    else if (r == "llego")
                    {
                        if (h.ruta != null)
                        {
                            h.ruta.RemoveAt(0);
                            if (h.ruta.Count == 0)
                            {
                                h.ruta = null;
                                if (h.volviendo) { h.volviendo = false; h.fuera = false; h.hx = (h.zona.x0 + h.zona.x1) / 2; h.hz = (h.zona.z0 + h.zona.z1) / 2; }
                                h.estado = "pastar"; h.t = 4 + Rnd() * 6;
                            }
                        }
                        else { h.estado = "pastar"; h.t = 4 + Rnd() * 8; }
                    }
                    else { h.estado = "pastar"; h.t = 2 + Rnd() * 2; if (h.ruta != null && !h.volviendo && !h.fuera) h.ruta = null; }   // algo le cierra el paso: espera
                    if (h.t <= 0 && h.ruta == null) { h.estado = "pastar"; h.t = 3; }
                }
                h.pasto += (objPasto - h.pasto) * Mathf.Min(1, dt * 1.6f);
                h.yaw += Ang(h.ang - h.yaw) * Mathf.Min(1, dt * 4);
                Ubicar(h.c.raiz, h.x, h.z, h.yaw, 0, anda ? Mathf.Abs(Mathf.Sin(h.marcha * 2)) * 0.03f : 0);
                if (anda) h.marcha += dt * 5.5f;
                h.c.Andar(h.marcha, anda ? 0.45f : 0);
                h.c.AnimarCaballo(h.fase + t, h.pasto, anda);
            }
        }

        // =====================================================================
        void Update()
        {
            if (!listo || juego == null || juego.jugador == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            frame++; tiempo += dt;
            Physics.SyncTransforms();
            var pu = juego.jugador.transform.position;
            var p = new Vector2(pu.x, -pu.z);
            Director(dt, p);
            Arrear(dt, p);
            float t = tiempo * 1.2f;

            foreach (var a in animales)
            {
                if (a.arreo) continue;
                float dx = p.x - a.x, dz = p.y - a.z, d = Mathf.Sqrt(dx * dx + dz * dz);
                Activo(a.G, d <= 140 && !a.escondido);
                if (d > 140) { if (a.manada != null && a.manada.majada) MajadaLejos(a, dt); continue; }
                if (a.sp.vel == 0 && !a.sp.huye) continue;   // yarará: inmóvil
                // las vacas del corral del arreo siguen con su lógica; las demás, con la conducta nueva
                if (!a.corral && a.ruta == null) { Pensar(a, p, d, dt, t); continue; }
                a.t -= dt;
                float mover = 0, ang = a.ang;
                if (a.sp.huye && d < a.sp.huida) { a.estado = "huir"; ang = Mathf.Atan2(-dx, -dz) + a.deriva; mover = a.sp.velH; a.t = 1.2f; }
                else if (a.estado == "huir" && a.t > 0) mover = a.sp.velH * 0.5f;
                else
                {
                    if (a.estado == "huir") a.estado = "pastar";
                    if (a.estado == "pastar")
                    {
                        if (a.t <= 0)
                        {
                            int fuera = 0; foreach (var c in vacasArreo) if (!c.corral) fuera++;
                            if (a.ruta != null) { a.estado = "caminar"; a.t = 40; }
                            else if (a.corral && Tranq.abierta && Random.value < 0.04f && fuera < 2)
                            {
                                float gx = Tranq.x, gz = Tranq.z;
                                a.ruta = new List<Vector2> { new Vector2(gx, gz - 3), new Vector2(gx, gz + 1.5f), new Vector2(gx + (Random.value - 0.5f) * 8, gz + 10) };
                                a.estado = "caminar"; a.t = 40; a.salio = true;
                            }
                            else if (a.corral) { a.tx = Rodeo.x + (Random.value - 0.5f) * (CW - 4); a.tz = Rodeo.y + (Random.value - 0.5f) * (CL - 4); a.estado = "caminar"; a.t = 14; }
                            else { float th = Hs(frame + a.hx) * 6.28f, r = 4 + Hs(frame * 1.7 + a.hz) * a.sp.radio; a.tx = a.hx + Mathf.Cos(th) * r; a.tz = a.hz + Mathf.Sin(th) * r; a.estado = "caminar"; a.t = 14; }
                        }
                    }
                    else if (a.estado == "caminar")
                    {
                        var dest = a.ruta != null ? a.ruta[0] : new Vector2(a.tx, a.tz);
                        float ddx = dest.x - a.x, ddz = dest.y - a.z, dd = Mathf.Sqrt(ddx * ddx + ddz * ddz);
                        if (a.ruta != null && dd < 1.2f)
                        {
                            a.ruta.RemoveAt(0);
                            if (a.ruta.Count == 0)
                            {
                                a.ruta = null;
                                if (a.salio) { a.corral = false; a.salio = false; a.hx = a.x; a.hz = a.z; }
                                a.estado = "pastar"; a.t = 3 + Hs(frame * 0.3 + a.hx) * 7;
                            }
                        }
                        else if (dd < 1.2f || a.t <= 0) { a.ruta = null; a.salio = false; a.estado = "pastar"; a.t = 3 + Hs(frame * 0.3 + a.hx) * 7; }
                        else { ang = Mathf.Atan2(ddx, ddz); mover = a.sp.vel; }
                    }
                }
                if (mover > 0)
                {
                    float nx = a.x + Mathf.Sin(ang) * mover * dt, nz = a.z + Mathf.Cos(ang) * mover * dt;
                    float rr = RCOL.TryGetValue(a.esp, out var rc) ? rc * 0.8f : 0.25f;
                    if (EnMapa(nx, nz) && Libre(nx, nz, Mathf.Max(rr, 0.3f), a.G)) { a.x = nx; a.z = nz; a.ang = ang; }
                    else { a.estado = "pastar"; a.t = 1.5f; mover = 0; }
                    a.yaw += Ang(a.ang - a.yaw) * Mathf.Min(1, dt * 6);
                }
                bool pastando = a.estado == "pastar" && a.sp.vel > 0;   // inclina el cuerpo hacia adelante como si pastara
                a.pitch += ((pastando ? 0.1f + 0.04f * Mathf.Sin(t * 1.7f + a.fase) : 0) - a.pitch) * Mathf.Min(1, dt * 2);
                Ubicar(a.G, a.x, a.z, a.yaw, a.pitch, mover > 0 ? Mathf.Abs(Mathf.Sin(a.fase)) * 0.04f : 0);
                Bailar(a.m, ref a.fase, mover, dt);
            }

            MoverCaballos(dt, p, t);

            // carretas: avanzan por el camino y dan la vuelta en los extremos
            for (int i = 0; i < carretas.Count; i++)
            {
                var c = carretas[i];
                c.s += c.dir * 1.4f * dt;
                if (c.s > largoRuta - 20) c.dir = -1;
                if (c.s < 20) c.dir = 1;
                var pos = SobreRuta(c.s);
                float ang = pos.ang + (c.dir < 0 ? Mathf.PI : 0);
                Ubicar(c.b.raiz, pos.x, pos.z, ang);
                c.fase += dt * 1.4f * 1.4f;
                foreach (var r in c.b.ruedas) r.localRotation = Conv.Rot(c.fase * c.dir, 0, 0);
                foreach (var b in c.b.bueyes) b.Bailar(Mathf.Sin(c.fase * 2) * 0.5f);
                if (i == 0 && pascual != null && !PascualPrestado)   // Pascual maneja su carreta sentado adelante, con la picana
                    Carrero.Ubicar(pascual.transform, pascual.Modelo.Anim, c.b.raiz, frame * 0.17f);
            }

            ActualizarConducta(dt, p, t);

            // caranchos en vuelo
            foreach (var v in vuelos)
            {
                bool cerca = Vector2.Distance(v.c, p) <= 400;
                Activo(v.b.raiz, cerca || v.carcasa != null);
                if (!cerca && v.carcasa == null) continue;
                if (Carroña(v, dt, t)) continue;   // bajó a comer una osamenta
                float a = t * 0.35f + v.f;
                v.b.raiz.position = new Vector3(v.c.x + Mathf.Cos(a) * v.r, v.h + Mathf.Sin(a * 2) * 2, -(v.c.y + Mathf.Sin(a) * v.r));
                v.b.raiz.rotation = Conv.Rot(0, -a, -0.25f);
                for (int i = 0; i < v.b.alas.Count; i++) v.b.alas[i].localRotation = Conv.Rot(0, 0, Mathf.Sin(t * 2.2f + v.f) * 0.35f * (i == 1 ? 1 : -1));
            }
        }
    }
}
