using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Argentina1860
{
    /// <summary>
    /// Juego en Buenos Aires (la parte de la ciudad de main.js): misión "La carta urgente" (Anselmo → Remedios → el sacristán → Anselmo),
    /// personajes de la plaza, transeúntes, carros, haz de luz sobre el objetivo, minimapa y diálogos. Interfaz provisoria (IMGUI).
    /// </summary>
    public class JuegoCiudad : MonoBehaviour
    {
        public ControlJugador jugador;
        public CamaraIsometrica camara;
        public Ciudad ciudad;
        public int monedas;
        [Tooltip("F1 a F8 llevan al jugador junto a cada personaje (pruebas)")]
        public bool viajeRapido = true;

        int q;   // etapa de la misión
        readonly HashSet<string> objetos = new HashSet<string>();
        NPC[] npcs = new NPC[0];
        NPC cercano, hablando;
        string[] lineas;
        Action alTerminar;
        int linea;
        float tiempo, avisoT;
        string aviso;
        bool listo;
        CampoCiudad campo;
        Transform haz;
        Material matHaz;
        Texture2D mapa;
        const float MAPA_R = 1600, MAPA_ESC = 0.5f;   // el minimapa cubre ±1,6 km alrededor de la plaza
        GUIStyle sCaja, sTitulo, sTexto, sEtiqueta, sAyuda;

        static readonly (string t, string x)[] OBJETIVOS =
        {
            ("Un encargo en la plaza", "Hablá con Don Anselmo, el diario de la plaza."),
            ("La carta urgente", "Llevale la carta a Doña Remedios, hacia el oeste."),
            ("Un paquete de yerba", "Llevale el paquete al sacristán, más al oeste."),
            ("La medalla de plata", "Devolvele la medalla a Don Anselmo, en la plaza."),
            ("Misión cumplida", "Recorré la ciudad, hay más por descubrir. (Próximamente)"),
        };

        IEnumerator Start()
        {
            if (ciudad == null) ciudad = FindAnyObjectByType<Ciudad>();
            while (ciudad == null || !ciudad.Generado || ciudad.Campo == null) yield return null;
            campo = ciudad.Campo;
            ControlJugador.AguaEn = campo.EnAgua;
            npcs = FindObjectsByType<NPC>();
            foreach (var n in npcs) Ubicar(n.transform);
            Ubicar(jugador.transform);
            var cc = jugador.GetComponent<CharacterController>();
            CrearTranseuntes();
            CrearCarros();
            CrearHaz();
            CrearMapa();
            if (camara != null) camara.Ubicar();
            listo = true;
        }

        void OnDestroy() { ControlJugador.AguaEn = MapaPago.Bloqueado; }

        /// Lleva un objeto al lugar libre más cercano y lo apoya en el piso (veredas incluidas).
        void Ubicar(Transform t)
        {
            float x = t.position.x, z = -t.position.z;
            if (!campo.Libre(x, z))
            {
                bool ok = false;
                for (float r = 2; r < 80 && !ok; r += 2)
                    for (int a = 0; a < 16 && !ok; a++)
                    {
                        float xx = x + Mathf.Cos(a * Mathf.PI / 8) * r, zz = z + Mathf.Sin(a * Mathf.PI / 8) * r;
                        if (campo.Libre(xx, zz)) { x = xx; z = zz; ok = true; }
                    }
            }
            var cc = t.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            t.position = new Vector3(x, campo.Altura(x, z) + 0.05f, -z);
            if (cc != null) cc.enabled = true;
        }

        public void Decir(string t) { aviso = t; avisoT = 4.5f; }

        // ---------- diálogos y misión ----------
        (string[] l, Action fin) Dialogo(NPC n)
        {
            string id = n.Ficha != null ? n.Ficha.id : "";
            switch (id)
            {
                case "anselmo":
                    if (q == 0) return (n.Lineas, () => { q = 1; objetos.Add("Carta lacrada"); });
                    if (q == 1) return (new[] { "¿Todavía acá? La carta es para Doña Remedios, hacia el oeste. Siga la luz rojiza, que yo la marco para usted." }, null);
                    if (q == 2 || (q == 3 && !objetos.Contains("Medalla de plata"))) return (new[] { "Si anda por ahí, Doña Remedios me contó que hay un paquete que llevar al sacristán. Hágale el favor." }, null);
                    if (q == 3) return (new[] { "Con que la medalla... ¡Esa es la que se había perdido! Ahora sí que me debo una.", "Tome estas monedas, se las ganó. Y vuelva cuando quiera: en esta ciudad siempre hay algún mandado." },
                        () => { q = 4; objetos.Remove("Medalla de plata"); monedas += 50; Decir("Ganaste $50."); });
                    return (new[] { "La ciudad crece rápido, ¿vio? Dicen que van a traer más trenes y más gente. Qué tiempos." }, null);
                case "remedios":
                    if (q == 0) return (new[] { "Estoy ocupada, hijo. Si viene de parte de alguien, que venga con carta." }, null);
                    if (q == 1) return (new[] { "¿Una carta de Anselmo? Dámela... Ajá. Ya veo. Siempre con apuros ese hombre.", "Mirá, llevale este paquete de yerba al sacristán de la iglesia, más al oeste. Él sabe de qué se trata. Y no lo abras." },
                        () => { q = 2; objetos.Remove("Carta lacrada"); objetos.Add("Paquete de yerba"); });
                    return (new[] { "En la pulpería hay de todo: yerba, caña, tabaco... y chismes, que son gratis." }, null);
                case "sacristan":
                    if (q < 2) return (new[] { "Silencio, que es casa de Dios. Si busca algo, vuelva más tarde." }, null);
                    if (q == 2) return (new[] { "Así que Remedios le mandó yerba. Con la yerba se hacen buenas migas, dicen.", "A cambio, llévele esto a Anselmo: es la medalla de plata que se le cayó de la capilla hace unos días." },
                        () => { q = 3; objetos.Remove("Paquete de yerba"); objetos.Add("Medalla de plata"); });
                    return (new[] { "Vaya con Dios. Y cuide la medalla." }, null);
            }
            return (n.Lineas, null);
        }

        NPC Objetivo()
        {
            string id = q switch { 0 => "anselmo", 1 => "remedios", 2 => "sacristan", 3 => "anselmo", _ => null };
            return id == null ? null : npcs.FirstOrDefault(n => n.Ficha != null && n.Ficha.id == id);
        }

        void Abrir(NPC n)
        {
            var d = Dialogo(n);
            hablando = n; lineas = d.l; alTerminar = d.fin; linea = 0;
            jugador.Bloqueado = true;
            if (camara != null) { camara.VistaForzada = 7.5f; camara.FocoForzado = (jugador.transform.position + n.transform.position) / 2; }
        }

        void Avanzar()
        {
            if (++linea < lineas.Length) return;
            var fin = alTerminar;
            hablando = null; alTerminar = null;
            jugador.Bloqueado = false;
            if (camara != null) { camara.VistaForzada = null; camara.FocoForzado = null; }
            fin?.Invoke();
        }

        // ---------- transeúntes: gente de fondo que camina por la plaza y las calles cercanas ----------
        class Transeunte { public Transform g; public AnimPersonaje anim; public float x, z, hx, hz, tx, tz, espera, fase; }
        readonly List<Transeunte> trans = new List<Transeunte>();
        static float Hz(double i) { double x = Math.Sin(i * 127.1 + 311.7) * 43758.5453; return (float)(x - Math.Floor(x)); }

        void CrearTranseuntes()
        {
            Aspecto A(Estilo e, string ropa = null, string ropa2 = null, string chaleco = null, string pantalon = null, string poncho = null, Sombrero sb = Sombrero.Ninguno,
                string colorSombrero = null, bool barba = false, Bigote bigote = Bigote.Ninguno, string pelo = null, Peinado peinado = Peinado.SegunEstilo, string piel = null,
                Accesorio prop = Accesorio.Ninguno, float escala = 1)
            {
                var a = new Aspecto { estilo = e, sombrero = sb, barba = barba, bigote = bigote, peinado = peinado, accesorio = prop, escala = escala };
                if (ropa != null) a.ropa = Conv.Hex(ropa); if (ropa2 != null) a.ropa2 = Conv.Hex(ropa2); if (chaleco != null) a.chaleco = Conv.Hex(chaleco);
                if (pantalon != null) a.pantalon = Conv.Hex(pantalon); if (poncho != null) a.poncho = Conv.Hex(poncho); if (colorSombrero != null) a.colorSombrero = Conv.Hex(colorSombrero);
                if (pelo != null) a.pelo = Conv.Hex(pelo); if (piel != null) a.piel = Conv.Hex(piel);
                return a;
            }
            var tipos = new[]
            {
                A(Estilo.Levita, "#7a3a3a", chaleco: "#c4cfa4", pantalon: "#8a8478", sb: Sombrero.Copa, bigote: Bigote.Fino, peinado: Peinado.Corto, pelo: "#2b2018"),
                A(Estilo.Levita, "#3d5c4a", chaleco: "#e8d9a0", pantalon: "#a09a90", sb: Sombrero.Copa, barba: true, pelo: "#7a7a76", peinado: Peinado.Corto),
                A(Estilo.Vestido, "#2f6f8f", "#f1e3c4", pelo: "#4a2c1a"),
                A(Estilo.Vestido, "#b04a3a", "#e8d9b0", pelo: "#1f1410"),
                A(Estilo.Vestido, "#4a7a4f", "#f4eedc", pelo: "#8a5a2a"),
                A(Estilo.Poncho, poncho: "#2f6f8f", sb: Sombrero.Ala, colorSombrero: "#6a4a30", barba: true, pelo: "#2a1c14", piel: "#d9a27a"),
                A(Estilo.Poncho, poncho: "#7a3f8f", sb: Sombrero.Ala, colorSombrero: "#2a2420", bigote: Bigote.Manubrio, pelo: "#2a1c14", piel: "#cf9a72"),
                A(Estilo.Pregonero, "#4a5a8a", "#f1ead8", pantalon: "#8a8478", sb: Sombrero.Gorra, colorSombrero: "#8a5a3a", peinado: Peinado.Corto, pelo: "#2b2018"),
                A(Estilo.Sotana, sb: Sombrero.Bonete, pelo: "#c9c9c4"),
                A(Estilo.Levita, "#7a5aa0", chaleco: "#e0b84e", pantalon: "#6e6d73", sb: Sombrero.Copa, bigote: Bigote.Manubrio, pelo: "#3a2a1c", peinado: Peinado.Corto),
                A(Estilo.Pregonero, "#8a6a3a", "#efe6d0", pantalon: "#7a7060", sb: Sombrero.Gorra, colorSombrero: "#4a5a6a", pelo: "#3a2a1c", peinado: Peinado.Puas, escala: 0.72f),
                A(Estilo.Levita, "#2c3e6b", chaleco: "#c9a227", pantalon: "#b3342b", sb: Sombrero.Kepi, bigote: Bigote.Fino, pelo: "#2b2018", peinado: Peinado.Corto, prop: Accesorio.Fusil),
                A(Estilo.Poncho, poncho: "#8a3a2a", sb: Sombrero.Ala, colorSombrero: "#4a3a2a", prop: Accesorio.Barril, pelo: "#2a1c14", piel: "#cf9a72"),
                A(Estilo.Vestido, "#6a4a8a", "#f1e3c4", pelo: "#2a1a12", prop: Accesorio.Canasta),
            };
            var raiz = new GameObject("Transeúntes").transform;
            for (int i = 0; i < 24; i++)
            {
                // los primeros son los tipos de arriba; los demás, gente de todas las clases que no se repite
                var a = i < tipos.Length ? JsonUtility.FromJson<Aspecto>(JsonUtility.ToJson(tipos[i])) : Variedad.Transeunte(new System.Random(i * 7919 + 13));
                float x = (Hz(i + 1) - 0.5f) * 120, z = (Hz(i + 9) - 0.5f) * 100;
                for (int k = 0; k < 40 && !campo.Libre(x, z); k++) { x = (Hz(i + k * 3 + 2) - 0.5f) * 140; z = (Hz(i + k * 5 + 4) - 0.5f) * 120; }
                var go = new GameObject("transeúnte " + i);
                go.transform.SetParent(raiz, false);
                go.transform.position = new Vector3(x, campo.Altura(x, z), -z);
                var frente = go.transform;
                var anim = ConstructorPersonaje.Construir(frente, a, null);
                trans.Add(new Transeunte { g = go.transform, anim = anim, x = x, z = z, hx = x, hz = z, tx = x, tz = z, espera = Hz(i) * 4, fase = Hz(i + 5) * 6 });
            }
        }

        bool Choca(float x, float z, float r, Transeunte yo)
        {
            foreach (var t in trans) if (t != yo && Vector2.Distance(new Vector2(t.x, t.z), new Vector2(x, z)) < r + 0.45f) return true;
            foreach (var n in npcs) { var p = n.transform.position; if (Vector2.Distance(new Vector2(p.x, -p.z), new Vector2(x, z)) < r + 0.5f) return true; }
            var j = jugador.transform.position;
            return Vector2.Distance(new Vector2(j.x, -j.z), new Vector2(x, z)) < r + 0.42f;
        }

        void MoverTranseuntes(float dt, Vector2 p)
        {
            foreach (var t in trans)
            {
                float d = Vector2.Distance(new Vector2(t.x, t.z), p);
                bool cerca = d < 140;
                if (t.g.gameObject.activeSelf != cerca) t.g.gameObject.SetActive(cerca);
                if (!cerca) continue;
                if (t.espera > 0) { t.espera -= dt; t.anim.Animar(t.fase += dt * 0.6f, 0); continue; }
                float dx = t.tx - t.x, dz = t.tz - t.z, l = Mathf.Sqrt(dx * dx + dz * dz);
                if (l < 0.6f)   // elegir otro destino cerca de su lugar, solo en terreno libre
                {
                    for (int k = 0; k < 6; k++)
                    {
                        float a = Random.value * 6.28f, r = 8 + Random.value * 28, nx = t.hx + Mathf.Cos(a) * r, nz = t.hz + Mathf.Sin(a) * r;
                        bool ok = campo.Libre(nx, nz);
                        for (int s = 1; s < 8 && ok; s++) ok = campo.Libre(t.x + (nx - t.x) * s / 8, t.z + (nz - t.z) * s / 8);
                        if (ok) { t.tx = nx; t.tz = nz; break; }
                    }
                    t.espera = 1 + Random.value * 4;
                    continue;
                }
                float v = 1.3f * dt, mx = dx / l * v, mz = dz / l * v;
                if (campo.Libre(t.x + mx, t.z + mz) && !Choca(t.x + mx, t.z + mz, 0.4f, t)) { t.x += mx; t.z += mz; }
                else { t.tx = t.x; t.tz = t.z; t.espera = 0.6f; }
                t.g.position = new Vector3(t.x, campo.Altura(t.x, t.z), -t.z);
                var dir = new Vector3(dx, 0, -dz);
                t.g.rotation = Quaternion.Slerp(t.g.rotation, Quaternion.LookRotation(dir), Mathf.Min(1, dt * 6));
                t.anim.Animar(t.fase += dt * 7.5f, 0.8f);
            }
        }

        // ---------- carros con caballo ----------
        class Carro { public Bicho caballo; public float fase; }
        readonly List<Carro> carros = new List<Carro>();

        void CrearCarros()
        {
            var lista = new (float x, float z, float rot, string color, string carga, bool calc)[]
            {
                (52, 48, 0.4f, "#7a4a2a", "heno", true), (-54, -58, 2.6f, "#2a2018", "barriles", false), (-125, 30, 1.2f, "#a8754a", "cajas", true),
                (92, -22, 4.2f, "#5a3a24", "heno", false), (10, 90, 3.3f, "#c9a779", "barriles", true),
            };
            var raiz = new GameObject("Carros").transform;
            foreach (var c in lista)
            {
                float s = Mathf.Sin(c.rot), co = Mathf.Cos(c.rot);
                bool LibreTodo(float px, float pz) => campo.Libre(px, pz) && campo.Libre(px + s * 4, pz + co * 4) && campo.Libre(px - s * 3, pz - co * 3);
                float x = c.x, z = c.z;
                if (!LibreTodo(x, z))
                {
                    bool ok = false;
                    for (float r = 3; r < 70 && !ok; r += 3)
                        for (int a = 0; a < 12 && !ok; a++)
                        {
                            float xx = c.x + Mathf.Cos(a * Mathf.PI / 6) * r, zz = c.z + Mathf.Sin(a * Mathf.PI / 6) * r;
                            if (LibreTodo(xx, zz)) { x = xx; z = zz; ok = true; }
                        }
                }
                var g = new GameObject("carro").transform;
                g.SetParent(raiz, false);
                g.position = new Vector3(x, 0, -z);
                g.rotation = Conv.Yaw(c.rot);
                var cb = ModelosFauna.Caballo(g, c.color, c.calc);
                ModelosCiudad.Carro(g, c.carga);
                // el conjunto es un obstáculo: rectángulo orientado (x ±1,05; z de -3,7 a 1,7)
                Vector2 M(float lx, float lz) => new Vector2(x + lx * co + lz * s, z - lx * s + lz * co);
                campo.Registrar(new[] { M(-1.05f, -3.7f), M(1.05f, -3.7f), M(1.05f, 1.7f), M(-1.05f, 1.7f) });
                var bc = g.gameObject.AddComponent<BoxCollider>();
                bc.center = Conv.P(0, 1, -1); bc.size = new Vector3(2.1f, 2, 5.4f);
                carros.Add(new Carro { caballo = cb, fase = Random.value * 20 });
            }
        }

        // ---------- haz de luz sobre el objetivo ----------
        void CrearHaz()
        {
            var n = new Nodo("haz");
            n.Pieza(Geo.Cilindro(0.45f, 0.45f, 80, 10, true), "haz", Vector3.zero);
            haz = n.Instanciar(transform, null);
            var mr = haz.GetComponentInChildren<MeshRenderer>();
            if (mr != null) { mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; matHaz = mr.material; }
        }

        // ---------- minimapa: manzanas e iglesias alrededor de la plaza ----------
        void CrearMapa()
        {
            int S = Mathf.RoundToInt(MAPA_R * 2 * MAPA_ESC);
            var px = new Color32[S * S];
            Color32 papel = Conv.Hex("#d8c7a4"), manz = Conv.Hex("#9b8863"), agua = Conv.Hex("#9cc0c4");
            for (int j = 0; j < S; j++)
                for (int i = 0; i < S; i++)
                {
                    float x = i / MAPA_ESC - MAPA_R, z = j / MAPA_ESC - MAPA_R;
                    px[(S - 1 - j) * S + i] = campo.EnAgua(x, z) ? agua : campo.Ocupado(x, z) ? manz : papel;
                }
            mapa = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Mapa de la ciudad" };
            mapa.SetPixels32(px);
            mapa.Apply(true);
        }

        void ViajeRapido(Func<Key, bool> tecla)
        {
            var orden = new[] { "anselmo", "remedios", "sacristan", "paseante", "soldado", "aguatero", "vendedora", "lustra" };
            var teclas = new[] { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8 };
            for (int i = 0; i < orden.Length; i++)
            {
                if (!tecla(teclas[i])) continue;
                var n = npcs.FirstOrDefault(m => m.Ficha != null && m.Ficha.id == orden[i]);
                if (n == null) continue;
                var p = n.transform.position + new Vector3(2.5f, 0, 0);
                var cc = jugador.GetComponent<CharacterController>();
                cc.enabled = false; jugador.transform.position = p + Vector3.up * 0.3f; cc.enabled = true;
                Decir("Viaje rápido: " + n.Nombre);
            }
        }

        // ---------- bucle ----------
        void Update()
        {
            if (!listo || jugador == null) return;
            float dt = Time.deltaTime;
            tiempo += dt;
            if (avisoT > 0) avisoT -= dt;
            var pu = jugador.transform.position;
            var p = new Vector2(pu.x, -pu.z);
            foreach (var n in npcs) if (n != null) n.Actualizar(dt, pu);
            MoverTranseuntes(dt, p);
            foreach (var c in carros) c.caballo.AnimarCaballo(c.fase += dt * 1.2f);

            var obj = Objetivo();
            if (haz != null)
            {
                bool ver = obj != null && Vector3.Distance(obj.transform.position, pu) > 16;
                haz.gameObject.SetActive(ver);
                if (ver)
                {
                    haz.position = new Vector3(obj.transform.position.x, 40, obj.transform.position.z);
                    if (matHaz != null) { var c = matHaz.color; c.a = 0.35f + Mathf.Sin(Time.time * 2.5f) * 0.1f; matHaz.color = c; }
                }
            }

            var kb = Keyboard.current;
            bool Tecla(Key k) => kb != null && kb[k].wasPressedThisFrame;
            bool e = Tecla(Key.E);
            if (hablando != null)
            {
                jugador.MirarA(hablando.transform.position);
                if (e || Tecla(Key.Space) || Tecla(Key.Enter)) Avanzar();
                return;
            }
            if (viajeRapido) ViajeRapido(Tecla);
            cercano = null;
            float mejor = 4.5f;
            foreach (var n in npcs)
            {
                float d = Vector2.Distance(new Vector2(n.transform.position.x, n.transform.position.z), new Vector2(pu.x, pu.z));
                if (d < mejor) { mejor = d; cercano = n; }
            }
            if (e && cercano != null) Abrir(cercano);
        }

        // ---------- HUD provisorio ----------
        void Estilos()
        {
            if (sCaja != null) return;
            var papel = new Texture2D(1, 1);
            papel.SetPixel(0, 0, new Color(0.945f, 0.902f, 0.804f, 0.94f));
            papel.Apply();
            var tinta = new Color(0.165f, 0.133f, 0.106f);
            sCaja = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 10, 12) };
            sCaja.normal.background = papel;
            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true };
            sTitulo.normal.textColor = new Color(0.48f, 0.11f, 0.08f);
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            sTexto.normal.textColor = tinta;
            sEtiqueta = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 3, 4), wordWrap = true };
            sEtiqueta.normal.background = papel;
            sEtiqueta.normal.textColor = tinta;
            sAyuda = new GUIStyle(sTexto) { fontSize = 13 };
        }

        void OnGUI()
        {
            if (!listo || jugador == null) return;
            Estilos();
            var cam = camara != null ? camara.GetComponent<Camera>() : Camera.main;
            var pu = jugador.transform.position;

            if (cam != null && hablando == null)   // nombres sobre los personajes cercanos
                foreach (var n in npcs)
                {
                    if (Vector3.Distance(n.transform.position, pu) > 28) continue;
                    var sp = cam.WorldToScreenPoint(n.transform.position + Vector3.up * n.AltoEtiqueta);
                    if (sp.z < 0) continue;
                    var tam = sEtiqueta.CalcSize(new GUIContent(n.Nombre));
                    GUI.Label(new Rect(sp.x - tam.x / 2, Screen.height - sp.y - tam.y, tam.x, tam.y), n.Nombre, sEtiqueta);
                }

            var (titulo, texto) = OBJETIVOS[Mathf.Min(q, OBJETIVOS.Length - 1)];
            GUILayout.BeginArea(new Rect(16, 16, 400, 240));
            GUILayout.BeginVertical(sCaja);
            GUILayout.Label(titulo, sTitulo);
            GUILayout.Label(texto, sTexto);
            GUILayout.Label("$ " + monedas + (objetos.Count > 0 ? "  ·  " + string.Join(" · ", objetos) : ""), sAyuda);
            GUILayout.EndVertical();
            GUILayout.EndArea();

            // minimapa: norte arriba, objetivo en rojo
            if (mapa != null)
            {
                const float S = 240, esc = 0.9f;
                var area = new Rect(Screen.width - S - 16, Screen.height - S - 16, S, S);
                GUI.Box(new Rect(area.x - 3, area.y - 3, S + 6, S + 6), GUIContent.none, sCaja);
                float x = pu.x, z = -pu.z, span = S / esc;
                float u = (x + MAPA_R) * MAPA_ESC, v = (z + MAPA_R) * MAPA_ESC, W = mapa.width;
                GUI.DrawTextureWithTexCoords(area, mapa, new Rect((u - span * MAPA_ESC / 2) / W, 1 - (v + span * MAPA_ESC / 2) / W, span * MAPA_ESC / W, span * MAPA_ESC / W));
                var obj = Objetivo();
                if (obj != null)
                {
                    float dx = (obj.transform.position.x - pu.x) * esc, dz = (-obj.transform.position.z - z) * esc, d = Mathf.Sqrt(dx * dx + dz * dz), max = S / 2 - 10;
                    if (d > max) { dx *= max / d; dz *= max / d; }
                    GUI.DrawTexture(new Rect(area.center.x + dx - 6, area.center.y + dz - 6, 12, 12), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.63f, 0.2f, 0.13f), 0, 6);
                }
                GUI.DrawTexture(new Rect(area.center.x - 5, area.center.y - 5, 10, 10), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.165f, 0.133f, 0.106f), 0, 5);
            }

            if (tiempo < 20)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 296, 16, 280, 200));
                GUILayout.BeginVertical(sCaja);
                GUILayout.Label("WASD / flechas: caminar\nShift: correr\nQ / R: girar la vista\nRueda: zoom\nE: hablar\nF9: volver al pago" + (viajeRapido ? "\nF1–F8: viaje rápido (pruebas)" : ""), sAyuda);
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }

            if (avisoT > 0 && !string.IsNullOrEmpty(aviso))
            {
                var tam = sEtiqueta.CalcSize(new GUIContent(aviso));
                GUI.Label(new Rect((Screen.width - tam.x) / 2, Screen.height - 150, tam.x, tam.y), aviso, sEtiqueta);
            }

            if (hablando != null)
            {
                float w = Mathf.Min(760, Screen.width - 32);
                GUILayout.BeginArea(new Rect((Screen.width - w) / 2, Screen.height - 190, w, 174));
                GUILayout.BeginVertical(sCaja, GUILayout.ExpandHeight(true));
                GUILayout.Label(hablando.Nombre, sTitulo);
                GUILayout.Label(lineas[Mathf.Min(linea, lineas.Length - 1)], sTexto);
                GUILayout.FlexibleSpace();
                GUILayout.Label("E para seguir", sAyuda);
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
            else if (cercano != null)
            {
                var txt = "E  ·  Hablar con " + cercano.Nombre;
                var tam = sEtiqueta.CalcSize(new GUIContent(txt));
                GUI.Label(new Rect((Screen.width - tam.x) / 2, Screen.height - 90, tam.x, tam.y), txt, sEtiqueta);
            }
        }
    }
}
