using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Argentina1860
{
    /// <summary>
    /// Conducta de los animales del pago (se suma a Fauna.cs):
    /// - Cada especie tiene su carácter: hasta dónde deja acercarse a la gente (se alerta, se aparta, huye),
    ///   a qué hora anda (de día, de noche, al atardecer), si vive en manada, si va a la aguada, si duerme la siesta
    ///   a la sombra, si tiene cueva o se mete al agua, si vuela, si caza y qué caza.
    /// - Manadas con una líder: el resto la sigue, y si una se espanta, se espanta toda la manada.
    /// - Rutina con la hora del día (Ambiente.Hora): pastan a la mañana, van a tomar agua, siesta a la sombra,
    ///   pastan a la tarde, de noche duermen echadas; las nocturnas salen al atardecer y de día se esconden.
    /// - Mansos y ariscos: las vacas lecheras y los perros dejan acercarse; el ganado criollo se aparta;
    ///   venados y ñandúes disparan de lejos.
    /// - Depredadores: el puma y el zorro acechan y corren a su presa (de noche o al atardecer); lo que cazan queda
    ///   tirado y los caranchos bajan a comer. Los perros de las casas ladran al que llega y corren al zorro y al puma.
    /// - Majadas de ovejas con su perro ovejero, que al caer la tarde las junta y las arrima al corral.
    /// - Teros que gritan y vuelan en picada sobre el que se acerca al nido; patos y flamencos en la laguna.
    /// </summary>
    public partial class Fauna
    {
        class Rasgo
        {
            public float alerta, huida, paso, carrera, radio = 30, caza, alto = 0.3f;
            public bool gregario, vuela, nocturno, diurno = true, crepuscular, aguada, siesta, cueva, agua, zigzag, territorial;
            public string[] presas;
        }

        static readonly Dictionary<string, Rasgo> RASGOS = new Dictionary<string, Rasgo>
        {
            ["vaca"] = new Rasgo { alerta = 26, huida = 8, paso = 0.6f, carrera = 3.4f, radio = 45, gregario = true, aguada = true, siesta = true, alto = 0.55f },
            ["oveja"] = new Rasgo { alerta = 20, huida = 7, paso = 0.65f, carrera = 3.6f, radio = 30, gregario = true, aguada = true, siesta = true, alto = 0.3f },
            ["venado"] = new Rasgo { alerta = 80, huida = 45, paso = 1.2f, carrera = 9, radio = 60, gregario = true, crepuscular = true, zigzag = true, alto = 0.45f },
            ["nandu"] = new Rasgo { alerta = 75, huida = 40, paso = 1.4f, carrera = 10, radio = 70, gregario = true, zigzag = true, alto = 0.5f },
            ["perdiz"] = new Rasgo { alerta = 10, huida = 6, paso = 0.9f, carrera = 6, radio = 20, vuela = true, alto = 0.1f },
            ["vizcacha"] = new Rasgo { alerta = 25, huida = 14, paso = 0.7f, carrera = 5, radio = 15, gregario = true, diurno = false, nocturno = true, crepuscular = true, cueva = true, alto = 0.15f },
            ["peludo"] = new Rasgo { alerta = 14, huida = 7, paso = 0.8f, carrera = 4, radio = 25, diurno = false, nocturno = true, crepuscular = true, cueva = true, alto = 0.1f },
            ["carpincho"] = new Rasgo { alerta = 30, huida = 14, paso = 0.8f, carrera = 5, radio = 25, gregario = true, crepuscular = true, agua = true, siesta = true, alto = 0.25f },
            ["coipo"] = new Rasgo { alerta = 20, huida = 10, paso = 0.8f, carrera = 4.5f, radio = 18, diurno = false, nocturno = true, crepuscular = true, agua = true, alto = 0.15f },
            ["zorro"] = new Rasgo { alerta = 45, huida = 24, paso = 1.4f, carrera = 7.5f, radio = 60, diurno = false, nocturno = true, crepuscular = true, presas = new[] { "perdiz", "vizcacha", "tero" }, caza = 22, alto = 0.15f },
            ["puma"] = new Rasgo { alerta = 60, huida = 30, paso = 1.1f, carrera = 11, radio = 80, diurno = false, nocturno = true, crepuscular = true, presas = new[] { "oveja", "venado", "vizcacha", "carpincho", "nandu" }, caza = 28, alto = 0.3f },
            ["tero"] = new Rasgo { alerta = 45, huida = 32, paso = 0.8f, carrera = 3, radio = 12, vuela = true, territorial = true, nocturno = true, alto = 0.1f },
            ["pato"] = new Rasgo { alerta = 40, huida = 30, paso = 0.4f, carrera = 2, radio = 25, gregario = true, vuela = true, agua = true, alto = 0.05f },
            ["flamenco"] = new Rasgo { alerta = 60, huida = 40, paso = 0.5f, carrera = 2, radio = 20, gregario = true, vuela = true, agua = true, alto = 0.4f },
        };

        void AgregarEspecies()
        {
            void E(string n, float vel, float huida, float velH, float radio) { if (!ESP.ContainsKey(n)) ESP[n] = new Esp { vel = vel, huida = huida, velH = velH, radio = radio, huye = true }; }
            E("oveja", 0.65f, 7, 3.6f, 30); E("perro", 1.2f, 0, 6, 10); E("tero", 0.8f, 30, 3, 12); E("pato", 0.4f, 30, 2, 25); E("flamenco", 0.5f, 40, 2, 20);
            if (!RCOL.ContainsKey("oveja")) RCOL["oveja"] = 0.45f;
        }

        class Manada
        {
            public readonly List<Animal> m = new List<Animal>();
            public Animal lider;
            public Vector2 casa, campo;
            public float panico;
            public Vector2 panicoDe;
            public bool majada, encierre;
            public Perro ovejero;
        }

        class Perro
        {
            public Bicho b; public Vector2 casa; public float x, z, yaw, fase, t, ladrarT, calma;
            public string estado = "echado";
            public Manada majada;
            public Animal persigue;
        }

        readonly List<Manada> manadas = new List<Manada>();
        readonly List<Perro> perros = new List<Perro>();
        readonly List<Vector2> aguadas = new List<Vector2>(), sombras = new List<Vector2>(), cuevas = new List<Vector2>();
        readonly List<Animal> osamentas = new List<Animal>();

        // ---------- población nueva ----------
        void PoblarConducta()
        {
            Vector2 K(float a, float b) => MapaPago.K(a, b);

            // agua para tomar: orillas del arroyo y de la laguna
            for (float x = -1580; x < 1580; x += 22)
                for (float z = -1180; z < 1180; z += 22)
                    if (!MapaPago.EnAgua(x, z) && (MapaPago.EnAgua(x + 14, z) || MapaPago.EnAgua(x - 14, z) || MapaPago.EnAgua(x, z + 14) || MapaPago.EnAgua(x, z - 14)) && MapaPago.DistCamino(x, z) > 4)
                        aguadas.Add(new Vector2(x, z));
            // sombra: árboles grandes del terreno
            var ter = Terrain.activeTerrain;
            if (ter != null)
            {
                var td = ter.terrainData;
                foreach (var a in td.treeInstances)
                {
                    if (a.heightScale < 0.9f) continue;
                    string n = td.treePrototypes[a.prototypeIndex].prefab != null ? td.treePrototypes[a.prototypeIndex].prefab.name : "";
                    if (n != "ombu" && n != "tala" && n != "sauce" && n != "ceibo") continue;
                    var w = Vector3.Scale(a.position, td.size) + ter.transform.position;
                    sombras.Add(new Vector2(w.x, -w.z));
                }
            }
            foreach (var (kx, ky) in new[] { (0.9f, 2.1f), (0.4f, 1.7f), (1.25f, 1.75f) }) cuevas.Add(K(kx, ky));

            // las manadas: animales gregarios de la misma especie que andan juntos
            foreach (var a in animales)
            {
                if (a.arreo || a.corral || a.manada != null || !RASGOS.TryGetValue(a.esp, out var r) || !r.gregario) continue;
                var m = new Manada { lider = a, casa = new Vector2(a.hx, a.hz), campo = new Vector2(a.hx, a.hz) };
                foreach (var b in animales)
                    if (b.manada == null && b.esp == a.esp && !b.arreo && !b.corral && Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < 35) Unir(m, b);
                manadas.Add(m);
            }
            foreach (var a in animales)
                if (a.esp == "vizcacha" || a.esp == "peludo") a.casa = Cercano(cuevas, new Vector2(a.x, a.z)) ?? new Vector2(a.x, a.z);
                else a.casa = new Vector2(a.hx, a.hz);

            // vacas lecheras mansas, al lado del corral del casco
            var lech = new Manada { casa = K(0.80f, 0.79f), campo = K(0.80f, 0.79f) };
            for (int i = 0; i < 3; i++) { var v = Poner("vaca", lech.casa.x + i * 3, lech.casa.y + (i % 2) * 3); v.manso = true; v.cuidado = 0.15f; Unir(lech, v); }
            lech.lider = lech.m[0]; manadas.Add(lech);

            // majadas de ovejas con su ovejero: duermen en el corral y de día pastan en el campo
            Majada(K(0.62f, 0.975f), K(0.42f, 1.12f), 28);
            Majada(K(0.564f, 1.854f), K(0.78f, 1.72f), 26);

            // perros de las casas
            foreach (var (kx, ky, n) in new[] { (0.752f, 0.765f, 3), (2.155f, 1.262f, 2), (0.595f, 0.91f, 1), (0.552f, 1.86f, 1), (1.168f, 0.51f, 1) })
                for (int i = 0; i < n; i++) NuevoPerro(K(kx, ky) + new Vector2((i - 1) * 3, 4), null);

            // teros: parejas con su nido en el campo de pastoreo
            for (int i = 0, puestos = 0; i < 200 && puestos < 8; i++)
            {
                var q = K(0.2f + Hs(i * 3.1) * 2.8f, 0.2f + Hs(i * 5.3) * 2.0f);
                var u = MapaPago.Uso(q.x, q.y);
                if ((u != UsoLote.CampoAbierto && u != UsoLote.Potrero) || !Libre(q.x, q.y) || MapaPago.DistCamino(q.x, q.y) < 10 || MapaPago.EnAgua(q.x, q.y)) continue;
                for (int k = 0; k < 2; k++) { var tt = Poner("tero", q.x + k * 2, q.y + k); tt.nido = q; tt.casa = q; }
                puestos++;
            }

            // aves de la laguna
            var lc = MapaPago.K(MapaPago.LAGUNA_C);
            for (int g = 0; g < 4; g++)
            {
                string esp = g < 3 ? "pato" : "flamenco";
                var c = lc + new Vector2((Hs(g * 7.1) - 0.5f) * 260, (Hs(g * 3.3) - 0.5f) * 160);
                if (esp == "flamenco") c = Cercano(aguadas, lc + new Vector2(120, 40)) ?? c;
                var m = new Manada { casa = c, campo = c };
                int n = esp == "pato" ? 7 : 10;
                for (int i = 0; i < n; i++)
                {
                    float x = c.x + (Hs(g * 100 + i) - 0.5f) * 14, z = c.y + (Hs(g * 200 + i) - 0.5f) * 10;
                    Unir(m, Poner(esp, x, z));
                }
                m.lider = m.m[0];
                manadas.Add(m);
            }
        }

        void Unir(Manada m, Animal a)
        {
            a.manada = m;
            a.ofs = new Vector2((Hs(a.x * 1.7 + m.m.Count) - 0.5f) * 1, (Hs(a.z * 2.3 + m.m.Count) - 0.5f) * 1).normalized * (2 + m.m.Count * 0.9f);
            m.m.Add(a);
        }

        void Majada(Vector2 corral, Vector2 campo, int n)
        {
            var m = new Manada { casa = corral, campo = campo, majada = true };
            bool noche = Ambiente.Hora < 7 || Ambiente.Hora > 19.5f;
            var c = noche ? corral : campo;
            for (int i = 0; i < n; i++)
            {
                float ang = i * 2.4f, r = 1.5f + Mathf.Sqrt(i) * 1.6f;
                var o = Poner("oveja", c.x + Mathf.Cos(ang) * r, c.y + Mathf.Sin(ang) * r, Mix(0.9f, 1.08f, Hs(i * 3.7)));
                Unir(m, o);
            }
            m.lider = m.m[0];
            manadas.Add(m);
            m.ovejero = NuevoPerro(c + new Vector2(8, 0), m);
        }

        Perro NuevoPerro(Vector2 casa, Manada majada)
        {
            var b = ModelosFauna.Crear("perro", raiz);
            var pe = new Perro { b = b, casa = casa, x = casa.x, z = casa.y, yaw = Random.value * 6.28f, fase = Random.value * 6, majada = majada, estado = majada != null ? "cuidar" : "echado" };
            Ubicar(b.raiz, pe.x, pe.z, pe.yaw);
            perros.Add(pe);
            return pe;
        }

        static Vector2? Cercano(List<Vector2> l, Vector2 p, float max = 1e9f)
        {
            Vector2? r = null; float mejor = max * max;
            foreach (var q in l) { float d = (q - p).sqrMagnitude; if (d < mejor) { mejor = d; r = q; } }
            return r;
        }

        // ---------- cerebro de cada animal ----------
        static bool Crepusculo(float h) => (h >= 18.3f && h < 21.2f) || (h >= 4.8f && h < 7.3f);

        bool Activo(Rasgo r)
        {
            float h = Ambiente.Hora;
            bool noche = Ambiente.EsDeNoche;
            return (r.diurno && !noche) || (r.nocturno && noche) || (r.crepuscular && Crepusculo(h));
        }

        /// La amenaza más cercana para este animal: la gente, los perros, un depredador que lo caza.
        float Amenaza(Animal a, Rasgo r, Vector2 jugador, float dJug, out Vector2 desde)
        {
            desde = jugador;
            float best = a.manso ? dJug / Mathf.Max(0.05f, a.cuidado) : dJug;
            if (a.esp == "perro") best = 1e9f;
            var pa = new Vector2(a.x, a.z);
            foreach (var pe in perros)
            {
                // a sus propias ovejas el ovejero no las espanta de lejos: solo las empuja
                // de día el ovejero las deja tranquilas; a la tarde, cuando las arrea, se le apartan
                float escala = a.esp == "oveja" ? (pe.majada == a.manada ? (a.manada.encierre ? 1.3f : 3.2f) : 1.5f) : a.esp == "vaca" ? 2f : 1;
                float d = Vector2.Distance(pa, new Vector2(pe.x, pe.z)) * escala;
                if (d < best) { best = d; desde = new Vector2(pe.x, pe.z); }
            }
            if (r.presas == null)
                foreach (var dep in animales)
                {
                    if (dep.estado != "acechar" && dep.estado != "correr") continue;
                    if (dep.presa != a && (dep.estado != "correr" || Vector2.Distance(pa, new Vector2(dep.x, dep.z)) > 25)) continue;
                    float d = Vector2.Distance(pa, new Vector2(dep.x, dep.z)) * (dep.estado == "acechar" ? 1.6f : 0.8f);
                    if (d < best) { best = d; desde = new Vector2(dep.x, dep.z); }
                }
            return best;
        }

        void Pensar(Animal a, Vector2 jug, float dJug, float dt, float t)
        {
            if (!RASGOS.TryGetValue(a.esp, out var r)) { Deambular(a, null, dt, t); return; }
            if (a.muerto) { Muerto(a, dt); return; }
            a.t -= dt;
            var pa = new Vector2(a.x, a.z);

            float dAm = Amenaza(a, r, jug, dJug, out var desde);
            var mn = a.manada;
            if (mn != null && mn.panico > 0 && dAm > r.huida * 0.9f) { dAm = r.huida * 0.9f; desde = mn.panicoDe; }

            // aves: vuelan si se asustan (y los teros, si alguien se arrima al nido)
            if (r.vuela)
            {
                bool nido = r.territorial && Vector2.Distance(jug, a.nido) < 35;
                if (a.vuelo > 0 || dAm < r.huida || nido) { Volar(a, r, desde, jug, nido, dt, t); return; }
            }

            // huir
            if (dAm < r.huida)
            {
                if (mn != null) { mn.panico = 2.5f; mn.panicoDe = desde; }
                a.echado = 0; a.escondido = false; a.estado = "huir";
                Vector2? refugio = null;
                if (r.cueva) refugio = Cercano(cuevas, pa, 120);
                else if (r.agua) refugio = Cercano(aguadas, pa, 150);
                float ang;
                if (refugio.HasValue && Vector2.Distance(refugio.Value, desde) > Vector2.Distance(pa, desde) * 0.6f)
                {
                    var dr = refugio.Value - pa;
                    if (dr.magnitude < 2.5f) { a.escondido = r.cueva; ang = a.yaw; }
                    else ang = Mathf.Atan2(dr.x, dr.y);
                }
                else ang = Mathf.Atan2(pa.x - desde.x, pa.y - desde.y) + a.deriva * 0.6f + (r.zigzag ? Mathf.Sin(t * 2.3f + a.fase) * 0.7f : 0);
                float v = r.carrera * (dAm < r.huida * 0.5f ? 1 : 0.8f);
                Mover(a, r, ang, v, dt);
                Pose(a, r, v, -0.04f, dt);
                return;
            }
            if (a.estado == "huir") { a.estado = "pastar"; a.t = 2; }

            // alerta: levanta la cabeza, mira y, si se acercan más, se aparta al paso
            if (dAm < r.alerta)
            {
                a.echado = Mathf.Max(0, a.echado - dt * 2);
                a.estado = "alerta";
                float mirar = Mathf.Atan2(desde.x - pa.x, desde.y - pa.y);
                if (dAm < (r.huida + r.alerta) * 0.5f)
                {
                    float ang = mirar + Mathf.PI + a.deriva * 0.4f;
                    Mover(a, r, ang, r.paso * 1.5f, dt);
                    Pose(a, r, r.paso * 1.5f, -0.05f, dt);
                }
                else
                {
                    a.yaw += Ang(mirar - a.yaw) * Mathf.Min(1, dt * 3);
                    Pose(a, r, 0, -0.08f, dt);
                }
                return;
            }

            // los que cazan
            if (r.presas != null && (Activo(r) || a.estado == "comer") && Cazar(a, r, dt, t)) return;

            // fuera de su horario: escondidos en la cueva o durmiendo echados
            if (!Activo(r) && !(mn != null && mn.majada && mn.encierre))
            {
                if (r.cueva)
                {
                    var cv = a.casa;
                    if (Vector2.Distance(pa, cv) < 2.5f) { a.escondido = true; return; }
                    var dc = cv - pa;
                    Mover(a, r, Mathf.Atan2(dc.x, dc.y), r.paso * 1.5f, dt);
                    Pose(a, r, r.paso, 0, dt);
                    return;
                }
                Echarse(a, r, dt);
                return;
            }
            a.escondido = false;

            // rutina de la manada (decide la líder; el resto la sigue)
            if (mn != null && mn.lider != null && mn.lider != a && !mn.lider.muerto)
            {
                var lid = mn.lider;
                var obj = new Vector2(lid.x, lid.z) + Rotar(a.ofs, lid.yaw);
                float dd = Vector2.Distance(pa, obj);
                if (dd > 3.5f && !(lid.echado > 0.5f && dd < 8))
                {
                    var dv = obj - pa;
                    float v = Mathf.Min(r.carrera * 0.6f, r.paso * (dd > 12 ? 2.5f : 1.2f));
                    a.echado = Mathf.Max(0, a.echado - dt * 2);
                    Mover(a, r, Mathf.Atan2(dv.x, dv.y), v, dt);
                    Pose(a, r, v, 0, dt);
                }
                else if (lid.echado > 0.5f) Echarse(a, r, dt);
                else if (lid.estado == "beber") { a.estado = "beber"; Pose(a, r, 0, 0.16f, dt); }
                else { a.estado = "pastar"; Pose(a, r, 0, 0.1f + 0.04f * Mathf.Sin(t * 1.7f + a.fase), dt); }
                return;
            }

            // la líder (o el que anda solo) sigue la rutina del día
            float h = Ambiente.Hora;
            if (mn != null && mn.majada)
            {
                if (mn.encierre) { IrA(a, r, mn.casa, 3, dt, t, true); return; }
                if (Vector2.Distance(pa, mn.campo) > 120) { IrA(a, r, mn.campo, 20, dt, t, false); return; }
            }
            if (r.siesta && h >= 12.5f && h < 15.3f)
            {
                var s = Cercano(sombras, pa, 160);
                if (s.HasValue) { IrA(a, r, s.Value + new Vector2(2.5f, 1.5f), 2.5f, dt, t, true); return; }
                Echarse(a, r, dt); return;
            }
            if (r.aguada && ((h >= 10 && h < 11.3f) || (h >= 17 && h < 18.3f)))
            {
                var w = Cercano(aguadas, pa, 500);
                if (w.HasValue)
                {
                    if (Vector2.Distance(pa, w.Value) < 3) { a.estado = "beber"; Pose(a, r, 0, 0.18f, dt); return; }
                    IrA(a, r, w.Value, 3, dt, t, false); return;
                }
            }
            Deambular(a, r, dt, t);
        }

        static Vector2 Rotar(Vector2 v, float ang)
        {
            float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
            return new Vector2(v.x * c + v.y * s, -v.x * s + v.y * c);
        }

        void IrA(Animal a, Rasgo r, Vector2 dest, float llegar, float dt, float t, bool echarseAlLlegar)
        {
            var pa = new Vector2(a.x, a.z);
            var d = dest - pa;
            if (d.magnitude < llegar)
            {
                if (echarseAlLlegar) Echarse(a, r, dt);
                else { a.estado = "pastar"; Pose(a, r, 0, 0.1f + 0.04f * Mathf.Sin(t * 1.7f + a.fase), dt); }
                return;
            }
            a.echado = Mathf.Max(0, a.echado - dt * 2);
            a.estado = "caminar";
            Mover(a, r, Mathf.Atan2(d.x, d.y), r.paso * 1.3f, dt);
            Pose(a, r, r.paso * 1.3f, 0, dt);
        }

        void Echarse(Animal a, Rasgo r, float dt)
        {
            a.estado = "echado";
            a.echado = Mathf.Min(1, a.echado + dt * 0.8f);
            Pose(a, r, 0, 0, dt);
        }

        /// Pastar y caminar sin apuro dentro de su querencia (como antes, pero con la nueva forma de moverse).
        void Deambular(Animal a, Rasgo r, float dt, float t)
        {
            a.echado = Mathf.Max(0, a.echado - dt * 2);
            float radio = r != null ? r.radio : a.sp.radio;
            var centro = a.manada != null ? a.manada.campo : a.casa;
            if (a.estado != "caminar")
            {
                a.estado = "pastar";
                if (a.t <= 0)
                {
                    float th = Hs(frame + a.hx) * 6.28f, rr = 4 + Hs(frame * 1.7 + a.hz) * radio;
                    a.tx = centro.x + Mathf.Cos(th) * rr; a.tz = centro.y + Mathf.Sin(th) * rr;
                    a.estado = "caminar"; a.t = 14;
                }
                if (r != null) Pose(a, r, 0, 0.1f + 0.04f * Mathf.Sin(t * 1.7f + a.fase), dt);
                return;
            }
            float ddx = a.tx - a.x, ddz = a.tz - a.z, dd = Mathf.Sqrt(ddx * ddx + ddz * ddz);
            if (dd < 1.2f || a.t <= 0) { a.estado = "pastar"; a.t = 3 + Hs(frame * 0.3 + a.hx) * 8; return; }
            float v = r != null ? r.paso : a.sp.vel;
            if (r != null) { Mover(a, r, Mathf.Atan2(ddx, ddz), v, dt); Pose(a, r, v, 0, dt); }
        }

        static readonly Collider[] bufObras = new Collider[24];

        /// Como Libre, pero los animales no se traban entre ellos (en una majada andan pegados): solo cuentan
        /// las casas, los cercos, los árboles con colisión y la gente.
        bool LibreDeObras(float x, float z, float r)
        {
            int n = Physics.OverlapSphereNonAlloc(new Vector3(x, Alt(x, z) + 1.0f, -z), r, bufObras, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = bufObras[i];
                if (c is TerrainCollider || c.transform.IsChildOf(raiz)) continue;
                return false;
            }
            return true;
        }

        bool Domestico(string esp) => esp == "vaca" || esp == "oveja";

        /// Avanza esquivando construcciones; el ganado no se mete en los sembrados (están cerrados con zanja)
        /// y los animales de tierra no se meten al agua.
        void Mover(Animal a, Rasgo r, float ang, float vel, float dt)
        {
            float paso = vel * dt;
            if (paso <= 0) return;
            float rr = RCOL.TryGetValue(a.esp, out var rc) ? rc * 0.8f : 0.25f;
            foreach (var giro in new[] { 0f, 0.6f, -0.6f, 1.3f, -1.3f })
            {
                float g = ang + giro;
                float nx = a.x + Mathf.Sin(g) * paso, nz = a.z + Mathf.Cos(g) * paso;
                if (!EnMapa(nx, nz)) continue;
                if (!r.agua && MapaPago.EnAgua(nx, nz)) continue;
                if (Domestico(a.esp) && (Sembrado.Ocupa(nx, nz) || MapaPago.Uso(nx, nz) == UsoLote.Chacra)) continue;
                if (!LibreDeObras(nx, nz, Mathf.Max(rr, 0.3f))) continue;
                a.x = nx; a.z = nz; a.ang = g;
                a.yaw += Ang(a.ang - a.yaw) * Mathf.Min(1, dt * 6);
                return;
            }
            a.deriva = -a.deriva;
            a.t = 0;
        }

        void Pose(Animal a, Rasgo r, float mover, float pitch, float dt)
        {
            a.pitch += ((a.echado > 0.5f ? 0 : pitch) - a.pitch) * Mathf.Min(1, dt * 3);
            float dy = -r.alto * 0.7f * a.echado + (mover > 0 ? Mathf.Abs(Mathf.Sin(a.fase)) * 0.04f : 0);
            Ubicar(a.G, a.x, a.z, a.yaw, a.pitch, dy);
            Bailar(a.m, ref a.fase, a.echado > 0.5f ? 0 : mover, dt);
        }

        // ---------- aves ----------
        void Volar(Animal a, Rasgo r, Vector2 desde, Vector2 jug, bool nido, float dt, float t)
        {
            var pa = new Vector2(a.x, a.z);
            if (a.vuelo <= 0)
            {
                // levanta vuelo hacia el lado contrario
                a.vuelo = 0.01f; a.vueloT = 0;
                var lejos = (pa - desde).normalized;
                if (lejos.sqrMagnitude < 0.01f) lejos = new Vector2(Mathf.Sin(a.fase), Mathf.Cos(a.fase));
                float dist = a.esp == "perdiz" ? 25 + Random.value * 25 : 60 + Random.value * 80;
                var dest = pa + lejos * dist;
                if (r.agua) dest = Cercano(aguadas, dest, 400) ?? dest;
                if (a.esp == "pato" || a.esp == "flamenco") dest = MapaPago.K(MapaPago.LAGUNA_C) + new Vector2((Random.value - 0.5f) * 250, (Random.value - 0.5f) * 140);
                a.tx = dest.x; a.tz = dest.y;
            }
            a.vueloT += dt;
            float altura = a.esp == "perdiz" ? 2.5f : a.esp == "tero" ? 5 : 10;
            Vector2 obj;
            float vel;
            if (nido && r.territorial)
            {
                // el tero da vueltas sobre el intruso y baja en picada
                float ang = t * 1.6f + a.fase;
                obj = jug + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 7;
                altura = 3 + 2.5f * (0.5f + 0.5f * Mathf.Sin(t * 2.2f + a.fase));
                vel = 9;
                a.tx = a.nido.x; a.tz = a.nido.y;   // al terminar, vuelve al nido
                a.vueloT = 0;
            }
            else { obj = new Vector2(a.tx, a.tz); vel = a.esp == "perdiz" ? 9 : 8; }
            var d = obj - pa;
            float dist2 = d.magnitude;
            // sube al arrancar, baja al llegar
            float subir = Mathf.Clamp01(a.vueloT * 1.5f) * Mathf.Clamp01(dist2 / 12f + (nido ? 1 : 0));
            a.vuelo = Mathf.Max(0.01f, subir);
            if (dist2 > 0.5f)
            {
                float paso = Mathf.Min(dist2, vel * dt);
                a.x += d.x / dist2 * paso; a.z += d.y / dist2 * paso;
                a.yaw += Ang(Mathf.Atan2(d.x, d.y) - a.yaw) * Mathf.Min(1, dt * 5);
            }
            if (!nido && dist2 < 1 && a.vueloT > 1) { a.vuelo = 0; a.estado = "pastar"; a.t = 2; }
            a.echado = 0;
            Ubicar(a.G, a.x, a.z, a.yaw, 0, altura * a.vuelo);
            float aleteo = a.vuelo > 0.05f ? Mathf.Sin(t * 14 + a.fase) * 0.7f : 0;
            for (int i = 0; i < a.m.alas.Count; i++) a.m.alas[i].localRotation = Conv.Rot(0, 0, aleteo * (i == 1 ? 1 : -1));
            a.m.Bailar(0);
        }

        // ---------- caza ----------
        bool Cazar(Animal a, Rasgo r, float dt, float t)
        {
            var pa = new Vector2(a.x, a.z);
            if (a.estado == "comer" && a.presa != null && a.presa.muerto)
            {
                a.comida -= dt;
                if (a.comida > 0)
                {
                    a.yaw += Ang(Mathf.Atan2(a.presa.x - a.x, a.presa.z - a.z) - a.yaw) * Mathf.Min(1, dt * 3);
                    Pose(a, r, 0, 0.22f + 0.05f * Mathf.Sin(t * 5), dt);
                    return true;
                }
                a.presa = null; a.estado = "pastar"; a.t = 30;
                return false;
            }
            if (a.t > 0 && a.presa == null && a.estado == "pastar") return false;   // descansa entre cacería y cacería
            if (a.presa == null || a.presa.muerto || a.presa.escondido || Vector2.Distance(pa, new Vector2(a.presa.x, a.presa.z)) > 140)
            {
                a.presa = null;
                float mejor = 220;
                foreach (var b in animales)
                {
                    if (b.muerto || b.escondido || b.arreo || b.corral || b.vuelo > 0 || System.Array.IndexOf(r.presas, b.esp) < 0) continue;
                    float d = Vector2.Distance(pa, new Vector2(b.x, b.z));
                    if (d < mejor) { mejor = d; a.presa = b; }
                }
                if (a.presa == null) return false;
                a.comida = 0;
            }
            var pr = a.presa;
            var dv = new Vector2(pr.x, pr.z) - pa;
            float dist = dv.magnitude;
            float angP = Mathf.Atan2(dv.x, dv.y);
            if (dist < 1.3f)
            {
                // la alcanzó
                pr.muerto = true; pr.muertoT = 0; pr.estado = "muerto";
                if (pr.manada != null) { pr.manada.panico = 4; pr.manada.panicoDe = pa; }
                osamentas.Add(pr);
                a.estado = "comer"; a.comida = a.esp == "puma" ? 45 : 20;
                return true;
            }
            if (dist > r.caza)
            {
                a.estado = "acechar";   // agazapado, despacio
                Mover(a, r, angP, r.paso * 1.1f, dt);
                Pose(a, r, r.paso, 0.08f, dt);
                Ubicar(a.G, a.x, a.z, a.yaw, a.pitch, -0.08f);
                return true;
            }
            a.estado = "correr";
            a.comida += dt;   // tiempo corriendo
            if (a.comida > 14) { a.presa = null; a.estado = "pastar"; a.t = 25; a.comida = 0; return false; }   // se cansó
            Mover(a, r, angP, r.carrera, dt);
            Pose(a, r, r.carrera, -0.02f, dt);
            return true;
        }

        void Muerto(Animal a, float dt)
        {
            a.muertoT += dt;
            // tirado de costado
            a.G.position = new Vector3(a.x, Alt(a.x, a.z) + 0.1f, -a.z);
            a.G.rotation = Conv.Rot(0, a.yaw, 1.45f);
            a.m.Bailar(0);
        }

        // ---------- perros, osamentas, majadas ----------
        void ActualizarConducta(float dt, Vector2 jug, float t)
        {
            float h = Ambiente.Hora;
            foreach (var m in manadas)
            {
                m.panico = Mathf.Max(0, m.panico - dt);
                if (m.majada) m.encierre = h >= 18.4f || h < 6.5f;   // a la caída del sol vuelven al corral
                if (m.lider == null || m.lider.muerto) foreach (var b in m.m) if (!b.muerto) { m.lider = b; break; }
            }

            // osamentas: después de un rato, si nadie mira, el animal "vuelve" a su querencia
            for (int i = osamentas.Count - 1; i >= 0; i--)
            {
                var o = osamentas[i];
                if (o.muertoT > 300 && Vector2.Distance(jug, new Vector2(o.x, o.z)) > 160)
                {
                    o.muerto = false; o.estado = "pastar"; o.t = 3;
                    var c = o.manada != null ? o.manada.campo : o.casa;
                    o.x = c.x + (Random.value - 0.5f) * 20; o.z = c.y + (Random.value - 0.5f) * 20;
                    osamentas.RemoveAt(i);
                }
            }

            foreach (var pe in perros) Perrear(pe, jug, dt, t);
        }

        void Perrear(Perro pe, Vector2 jug, float dt, float t)
        {
            var pp = new Vector2(pe.x, pe.z);
            float dJ = Vector2.Distance(pp, jug);
            bool cerca = dJ < 150;
            Activo(pe.b.raiz, cerca);
            if (!cerca) return;
            pe.calma = Mathf.Max(0, pe.calma - dt);

            // ¿zorro o puma rondando? los corre
            if (pe.persigue == null || pe.persigue.escondido || Vector2.Distance(pp, new Vector2(pe.persigue.x, pe.persigue.z)) > 80)
            {
                pe.persigue = null;
                foreach (var a in animales)
                    if ((a.esp == "zorro" || a.esp == "puma") && !a.escondido && Vector2.Distance(pe.casa, new Vector2(a.x, a.z)) < 70) { pe.persigue = a; break; }
            }
            Vector2 obj; float vel; bool ladra = false;
            if (pe.persigue != null) { obj = new Vector2(pe.persigue.x, pe.persigue.z); vel = 8; ladra = true; pe.estado = "correr"; }
            else if (pe.majada != null) Ovejero(pe, out obj, out vel);
            else
            {
                // perro de la casa: sale a ladrarle al que llega, después se vuelve a echar
                bool llega = Vector2.Distance(jug, pe.casa) < 40;
                if (llega && pe.calma <= 0 && pe.estado != "ladrar") { pe.estado = "ladrar"; pe.ladrarT = 8 + Random.value * 6; }
                if (!llega && pe.estado == "ladrar") pe.estado = "volver";
                if (pe.estado == "ladrar")
                {
                    pe.ladrarT -= dt;
                    if (pe.ladrarT <= 0) { pe.estado = "volver"; pe.calma = 40; }
                    var lejos = (pp - jug).normalized;
                    obj = jug + lejos * 4.5f; vel = 6; ladra = true;
                }
                else
                {
                    if (Vector2.Distance(pp, pe.casa) > 3) { obj = pe.casa; vel = 2.2f; pe.estado = "volver"; }
                    else { obj = pp; vel = 0; pe.estado = "echado"; }
                }
            }
            var d = obj - pp;
            float dist = d.magnitude;
            float mover = 0;
            if (dist > 1 && vel > 0)
            {
                float paso = Mathf.Min(dist, vel * dt);
                float nx = pe.x + d.x / dist * paso, nz = pe.z + d.y / dist * paso;
                if (LibreDeObras(nx, nz, 0.3f) && !MapaPago.EnAgua(nx, nz)) { pe.x = nx; pe.z = nz; mover = vel; }
                pe.yaw += Ang(Mathf.Atan2(d.x, d.y) - pe.yaw) * Mathf.Min(1, dt * 6);
            }
            else if (ladra)
                pe.yaw += Ang(Mathf.Atan2(jug.x - pe.x, jug.y - pe.z) - pe.yaw) * Mathf.Min(1, dt * 6);
            // ladrido: saltitos con las patas delanteras
            float salto = ladra && mover == 0 ? Mathf.Max(0, Mathf.Sin(t * 9 + pe.fase)) * 0.12f : 0;
            bool echado = pe.estado == "echado" || (pe.estado == "cuidar" && mover == 0);
            Ubicar(pe.b.raiz, pe.x, pe.z, pe.yaw, ladra ? -0.15f * salto * 6 : 0, echado ? -0.2f : salto);
            float w = mover > 0 ? Mathf.Sin(pe.fase += dt * (mover * 2.4f + 3)) * (mover > 4 ? 0.9f : 0.5f) : 0;
            pe.b.Bailar(w);
        }

        /// Lejos del jugador la majada no se dibuja ni se anima, pero igual va y viene del corral al campo.
        void MajadaLejos(Animal a, float dt)
        {
            var m = a.manada;
            var dest = (m.encierre ? m.casa : m.campo) + a.ofs * 1.2f;
            var d = dest - new Vector2(a.x, a.z);
            float dist = d.magnitude;
            if (dist < 2) return;
            float paso = Mathf.Min(dist, 1.2f * dt);
            a.x += d.x / dist * paso; a.z += d.y / dist * paso;
            a.yaw = Mathf.Atan2(d.x, d.y);
            a.echado = 0;
        }

        /// El ovejero anda en la orilla de la majada; a la tarde se pone del lado de afuera y la empuja al corral.
        void Ovejero(Perro pe, out Vector2 obj, out float vel)
        {
            var m = pe.majada;
            var cen = Vector2.zero; int n = 0; float rad = 0;
            foreach (var o in m.m) if (!o.muerto) { cen += new Vector2(o.x, o.z); n++; }
            if (n == 0) { obj = m.casa; vel = 2; return; }
            cen /= n;
            foreach (var o in m.m) if (!o.muerto) rad = Mathf.Max(rad, Vector2.Distance(cen, new Vector2(o.x, o.z)));
            if (m.encierre && Vector2.Distance(cen, m.casa) > 8)
            {
                // detrás de la majada, del lado contrario al corral, y la va arrimando
                var haciaCorral = (m.casa - cen).normalized;
                obj = cen - haciaCorral * (rad + 5) + new Vector2(-haciaCorral.y, haciaCorral.x) * Mathf.Sin(Time.time * 0.6f) * rad * 0.6f;
                vel = 4.5f; pe.estado = "arrear";
                // las ovejas se apartan del perro: eso las lleva al corral; la líder además camina hacia allá
                if (m.lider != null && !m.lider.muerto) { m.lider.tx = m.casa.x; m.lider.tz = m.casa.y; }
                return;
            }
            // de día: a un costado de la majada, echado o dando una vuelta
            var lado = new Vector2(Mathf.Cos(Time.time * 0.05f + pe.fase), Mathf.Sin(Time.time * 0.05f + pe.fase));
            obj = cen + lado * (rad + 6);
            vel = Vector2.Distance(new Vector2(pe.x, pe.z), obj) > 6 ? 3 : 0;
            pe.estado = "cuidar";
        }

        /// Un carancho baja a comer de una osamenta. Devuelve true si este carancho está ocupado con eso.
        bool Carroña(Vuelo v, float dt, float t)
        {
            if (v.carcasa == null)
            {
                foreach (var o in osamentas)
                {
                    if (o.muertoT < 10 || Vector2.Distance(v.c, new Vector2(o.x, o.z)) > 500) continue;
                    bool tomada = false;
                    foreach (var w in vuelos) if (w != v && w.carcasa == o) tomada = true;
                    if (tomada) continue;
                    v.carcasa = o; v.bajada = 0; v.comerT = 0; v.pos = v.b.raiz.position;
                    break;
                }
                if (v.carcasa == null) return false;
            }
            var c = v.carcasa;
            var jug = juego.jugador.transform.position;
            bool espantado = Vector2.Distance(new Vector2(jug.x, -jug.z), new Vector2(c.x, c.z)) < 25;
            if (!c.muerto || espantado || v.comerT > 70) { v.carcasa = null; return false; }
            var suelo = new Vector3(c.x + 1.6f, Alt(c.x + 1.6f, c.z) + 0.25f, -c.z);
            v.bajada = Mathf.Min(1, v.bajada + dt * 0.15f);
            v.b.raiz.position = Vector3.Lerp(v.pos, suelo, Mathf.SmoothStep(0, 1, v.bajada));
            if (v.bajada >= 1)
            {
                v.comerT += dt;
                v.b.raiz.rotation = Conv.Rot(0.3f + Mathf.Max(0, Mathf.Sin(t * 6)) * 0.4f, -Mathf.PI / 2, 0);   // picotea mirando a la osamenta
                foreach (var al in v.b.alas) al.localRotation = Quaternion.identity;
            }
            else
            {
                var dir = suelo - v.pos;
                v.b.raiz.rotation = Conv.Rot(0, Mathf.Atan2(dir.x, -dir.z), 0);
                for (int i = 0; i < v.b.alas.Count; i++) v.b.alas[i].localRotation = Conv.Rot(0, 0, Mathf.Sin(t * 4 + v.f) * 0.3f * (i == 1 ? 1 : -1));
            }
            return true;
        }
    }
}
