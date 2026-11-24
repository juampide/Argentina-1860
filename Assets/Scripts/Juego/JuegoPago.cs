using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Juego en el pago (main.js): personajes, diálogos, misión, monedas y habilidades, arreo con Fermín, pulpería de Isidro,
    /// avisos y mapa. La interfaz es provisoria (IMGUI) hasta diseñar la definitiva.
    /// E: hablar, avanzar, tranquera · M: mapa grande · Esc: cerrar · Q / R: girar la vista · rueda: zoom.
    /// </summary>
    public class JuegoPago : MonoBehaviour
    {
        public ControlJugador jugador;
        public CamaraIsometrica camara;
        public Fauna fauna;

        [Header("Misión")]
        public string misionTitulo = "Etapa 1 · Sin nada";
        [TextArea] public string misionTexto = "Conocé a la gente del pago. Anselmo, el capataz, reparte las tareas en los corrales de la estancia.";

        // la plata, el morral y las habilidades viven en Partida (pasan de un mundo al otro)
        public int monedas { get => Partida.Monedas; set => Partida.Monedas = value; }
        bool arreoVisto, pulperiaVista;
        Fauna.ResultadoArreo arreoUlt;

        public int NivelArreo => Partida.Nivel(Partida.ARREO);
        public int NivelComercio => Partida.Nivel(Partida.COMERCIO);

        const float DISTANCIA_HABLAR = 4.5f, DISTANCIA_ETIQUETA = 28f;

        NPC[] npcs = new NPC[0];
        NPC cercano, hablando;
        string[] lineas;
        Action alTerminar;
        int linea;
        bool tienda, mapaGrande, cercaTranquera;
        string tiendaMsg = "";
        Vector2 tiendaScroll;
        float tiempo, avisoT;
        string aviso;
        Mercado mercado;
        MapaDibujo mapa;
        readonly List<MapaDibujo.Lugar> lugares = new List<MapaDibujo.Lugar>();
        GUIStyle sCaja, sTitulo, sTexto, sEtiqueta, sAviso, sAyuda, sBoton, sCelda;
        GUIStyle sSub, sSubNombre, sSubAyuda, sPildora, sPildoraCaja, sTecla, sHud, sHudTitulo, sHudTexto;
        Texture2D papel, degradado, oscuro, circulo;

        void Start()
        {
            npcs = FindObjectsByType<NPC>();
            mercado = new Mercado(Decir);
            if (fauna == null) fauna = FindAnyObjectByType<Fauna>();
            if (fauna != null)
            {
                fauna.juego = this;
                fauna.NivelArreo = () => NivelArreo;
                fauna.HudArreo = t => { misionTitulo = "Tarea · Arreo de vacas"; misionTexto = t; };
                fauna.FinArreo = FinArreo;
            }
            ConstruirMapa();
        }

        // ---------- avisos ----------
        float avisoBordeT;

        /// Cuando el jugador quiere pasar el bañado, el monte, el cañadón o salir por el camino real, se le dice por qué no.
        void AvisoDelBorde(float dt, Transform j)
        {
            if (avisoBordeT > 0) { avisoBordeT -= dt; return; }
            var q = j.position + j.forward * 3;
            var z = BordesPago.Zona(q.x, -q.z);
            if (z == ZonaBorde.Adentro || BordesPago.Zona(j.position.x, -j.position.z) != ZonaBorde.Adentro) return;
            Decir(BordesPago.Aviso(z));
            avisoBordeT = 10;
        }

        public void Decir(string t)
        {
            aviso = t;
            avisoT = 4.5f;
        }

        // ---------- arreo ----------
        void FinArreo(Fauna.ResultadoArreo r)
        {
            int antes = NivelArreo;
            monedas += r.pago; Partida.Ganar(Partida.ARREO, r.xp, avisar: false); arreoUlt = r;
            if (r.ok) { misionTitulo = "Tarea cumplida"; misionTexto = "Hablá con Anselmo si querés otra tanda de vacas."; }
            else { misionTitulo = "Se acabó el tiempo"; misionTexto = $"Metiste {r.dentro} de {Fauna.ArreoTotal}. Hablá con Anselmo para intentarlo de nuevo."; }
            Decir((r.ok ? $"¡Todas adentro! Ganaste ${r.pago}." : $"Tiempo. Cobrás ${r.pago} por {r.dentro} vacas.") + (NivelArreo > antes ? $" Arreo sube a nivel {NivelArreo}." : ""));
        }

        // ---------- diálogos con lógica propia ----------
        (string[] l, Action fin) Dialogo(NPC n)
        {
            string id = n.Ficha != null ? n.Ficha.id : "";
            if (id == "fermin" && fauna != null)
            {
                if (fauna.ArreoActivo) return (new[] { "Las vacas no se arrean solas. Rodeá desde atrás: se van de donde vos venís.", "Llevalas al corral, el de la bandera roja, al sur de la estancia, y cerrá la tranquera." }, null);
                string[] l = !arreoVisto
                    ? new[]
                    {
                        "Escuchame bien: acá se llega temprano y se trabaja hasta que yo diga. Si hacés bien lo tuyo, nos vamos a llevar bien.",
                        "Para empezar, hay cinco vacas sueltas en el potrero. Llevalas al corral, el de la bandera roja, al sur de la estancia, y cerrá la tranquera. Cuanto más rápido, mejor te pago.",
                        "Un consejo: la vaca se va de donde la ves venir. Ponete del lado contrario al corral y empujala despacio.",
                    }
                    : arreoUlt != null && arreoUlt.ok ? new[] { $"Bien, muchacho. Ahí van tus ${arreoUlt.pago}. Hay otra tanda de vacas sueltas, ¿te animás?" }
                    : new[] { "Así no se trabaja, che. Pero se aprende. Otra tanda de vacas anda suelta: probá de nuevo." };
                return (l, () =>
                {
                    arreoVisto = true;
                    fauna.IniciarArreo();
                    misionTitulo = "Tarea · Arreo de vacas";
                    misionTexto = "Arreá las 5 vacas al corral y cerrá la tranquera.";
                });
            }
            if (id == "isidro")
            {
                var l = pulperiaVista
                    ? new[] { "Pase, pase. Mire cómo anda la yerba hoy." }
                    : new[] { "Buenas, buenas. Tengo yerba, tabaco, caña, tela y sal, y para el que pesca, anzuelos, línea y achuras. El pescado también se lo compro. Los precios cambian según llegue o no la carreta.", "Yo le vendo un poco más caro de lo que le compro, así vivo. Pero el que sabe tratar, se va ganando mejor precio. Mire, mire." };
                return (l, () => { pulperiaVista = true; AbrirTienda(); });
            }
            return ConRecetas(id, n.Lineas);
        }

        /// Si el personaje enseña una receta que el Gallego no sabe, después de sus líneas se la enseña (una por charla).
        (string[] l, Action fin) ConRecetas(string id, string[] lineas)
        {
            var r = Catalogo.QueEnsena(id).FirstOrDefault(x => !Partida.Sabe(x));
            if (r == null) return (lineas, null);
            var ensena = r.alEnsenar != null && r.alEnsenar.Length > 0 ? r.alEnsenar : new[] { $"Le voy a enseñar a hacer {r.nombre.ToLowerInvariant()}." };
            return (lineas.Concat(ensena).ToArray(), () => Partida.Aprender(r));
        }

        /// Abre un diálogo con el personaje (también sirve para disparar diálogos desde misiones o eventos).
        public void Hablar(NPC n)
        {
            if (n != null && hablando == null) Abrir(n);
        }

        public bool EnDialogo => hablando != null;

        void Abrir(NPC n)
        {
            var d = Dialogo(n);
            hablando = n;
            lineas = d.l;
            alTerminar = d.fin;
            linea = 0;
            jugador.Bloqueado = true;
            if (camara != null)
            {
                camara.VistaForzada = 7.5f;
                camara.FocoForzado = (jugador.transform.position + n.transform.position) / 2;
            }
            CamaraDialogo.Iniciar(this, n, lineas);   // conversaciones importantes: corte directo a la cámara de cine
        }

        void Avanzar()
        {
            linea++;
            if (linea < lineas.Length) { CamaraDialogo.Linea(linea); return; }
            CamaraDialogo.Terminar();
            var fin = alTerminar;
            hablando = null;
            alTerminar = null;
            if (!tienda) Liberar();
            fin?.Invoke();
        }

        void Liberar()
        {
            jugador.Bloqueado = false;
            if (camara != null) { camara.VistaForzada = null; camara.FocoForzado = null; }
        }

        // ---------- pulpería ----------
        void AbrirTienda()
        {
            tienda = true;
            tiendaMsg = "Esc o E para salir";
            jugador.Bloqueado = true;
        }

        void CerrarTienda()
        {
            tienda = false;
            Liberar();
        }

        void Operar(bool compra, Mercado.Bien b)
        {
            int niv = NivelComercio;
            int n = Partida.Cantidad(b.id);
            if (compra)
            {
                int c = mercado.Compra(b, niv);
                if (!b.vende || monedas < c) return;
                if (b.ficha != null && !Partida.Entra(b.ficha)) { tiendaMsg = $"No te entra en el morral ({Partida.Peso:0.#} de {Partida.CargaMaxima:0} kg)."; return; }
                Partida.RegistrarCompra(b.id, c, n);
                monedas -= c;
                Partida.Dar(b.id, 1, forzar: true);
                tiendaMsg = $"Compraste {b.nombre} por ${c}.";
            }
            else
            {
                if (!b.compra || n < 1) return;
                int v = mercado.Venta(b, niv);
                float gan = v - Partida.CostoPromedio(b.id);   // lo que se pescó o se juntó no costó nada
                bool sube = Partida.Ganar(Partida.COMERCIO, 0.5f + Mathf.Max(0, gan) * 0.4f, avisar: false);
                monedas += v; Partida.Sacar(b.id);
                tiendaMsg = $"Vendiste {b.nombre} por ${v}" + (gan > 0 ? $" (ganancia ${Conv.RoundJS(gan)})." : gan < 0 ? $" (perdiste ${Conv.RoundJS(-gan)})." : ".");
                if (sube) tiendaMsg += $" Comercio sube a nivel {NivelComercio}.";
            }
        }

        // ---------- mapa ----------
        void ConstruirMapa()
        {
            var edificios = new List<(float, float, float, float, float)>();
            foreach (var c in FindObjectsByType<Casa>())
            {
                var p = c.transform.position;
                edificios.Add((p.x, -p.z, c.ancho, c.largo, -c.transform.eulerAngles.y * Mathf.Deg2Rad));
            }
            foreach (var c in FindObjectsByType<EdificioConInterior>())
            {
                var p = c.transform.position;
                edificios.Add((p.x, -p.z, c.Ancho, c.Largo, -c.transform.eulerAngles.y * Mathf.Deg2Rad));
            }
            foreach (var c in FindObjectsByType<Casona>())
            {
                // en el mapa, la casona va como un rectángulo con todo su contorno (frente y alas)
                var p = c.transform.position;
                edificios.Add((p.x, -p.z, c.frente, c.alas ? c.fondo : c.ala, -c.transform.eulerAngles.y * Mathf.Deg2Rad));
            }
            var cercos = new List<List<Vector2>>();
            foreach (var c in FindObjectsByType<Cerco>())
            {
                if (!c.cerrado) continue;
                cercos.Add(c.puntos.Select(q => { var w = c.transform.TransformPoint(new Vector3(q.x, 0, q.y)); return new Vector2(w.x, -w.z); }).ToList());
            }
            // chacras del loteo: cada sembrado con su color y el borde de los lotes cerrados con zanja
            var parcelas = new List<(List<Vector2>, Color32)>();
            foreach (var sm in FindObjectsByType<Sembrado>())
            {
                var esq = new List<Vector2>();
                foreach (var (ex, ez) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
                {
                    var w = sm.transform.TransformPoint(ex * sm.ancho / 2, 0, ez * sm.largo / 2);
                    esq.Add(new Vector2(w.x, -w.z));
                }
                parcelas.Add((esq, ColorSembrado(sm)));
            }
            var lindes = new List<List<Vector2>>();
            var loteo = LoteoPago.Instancia;
            if (loteo != null)
                foreach (var l in loteo.lotes)
                {
                    if (l.km == null || l.km.Length < 3) continue;
                    var poli = l.km.Select(k => MapaPago.K(k)).ToList();
                    if (l.uso == UsoLote.Quinta) parcelas.Add((poli, new Color32(0x9c, 0xb0, 0x70, 255)));
                    if (l.linde == Linde.Zanja || l.linde == Linde.CercoVivo) lindes.Add(poli);
                }
            mapa = new MapaDibujo();
            mapa.Construir(edificios, cercos, parcelas, lindes);

            void L(string n, float kx, float ky) { var k = MapaPago.K(kx, ky); lugares.Add(new MapaDibujo.Lugar { n = n, x = k.x, z = k.y }); }
            L("Estancia La Soledad", 0.75f, 0.75f); L("Ranchos de peones", 0.59f, 0.9f); L("Posta y pulpería", 2.15f, 1.25f); L("Rancho abandonado", 1.0f, 1.0f);
            L("Fortín", 0.12f, 1.15f); L("Puesto del baqueano", 0.55f, 1.85f); L("Rancho de Jacinta", 1.17f, 0.5f); L("Laguna", 1.7f, 2.05f); L("Monte", 2.3f, 0.45f);
        }

        static Color32 ColorSembrado(Sembrado s)
        {
            switch (s.etapa)
            {
                case Etapa.Arado: case Etapa.Sembrado: return new Color32(0x9a, 0x7a, 0x58, 255);
                case Etapa.Brotando: return new Color32(0xb0, 0xb8, 0x80, 255);
                case Etapa.Creciendo: return s.cultivo == Cultivo.Maiz ? new Color32(0x7a, 0x9a, 0x58, 255) : new Color32(0x9a, 0xb6, 0x6a, 255);
                case Etapa.Maduro: return s.cultivo == Cultivo.Maiz ? new Color32(0xb8, 0x9a, 0x62, 255) : new Color32(0xd8, 0xb4, 0x5a, 255);
                default: return new Color32(0xcc, 0xb4, 0x80, 255);
            }
        }

        List<MapaDibujo.Lugar> Lugares()
        {
            var l = new List<MapaDibujo.Lugar>(lugares);
            if (fauna != null && fauna.Rodeo != Vector2.zero) l.Add(new MapaDibujo.Lugar { n = "Corral", x = fauna.Rodeo.x, z = fauna.Rodeo.y });
            return l;
        }

        MapaDibujo.Lugar? Destino()
        {
            if (fauna != null && fauna.ArreoActivo) return new MapaDibujo.Lugar { n = "Corral", x = fauna.Rodeo.x, z = fauna.Rodeo.y };
            var f = npcs.FirstOrDefault(n => n != null && n.Ficha != null && n.Ficha.id == "fermin");
            if (f != null) return new MapaDibujo.Lugar { n = "Anselmo", x = f.transform.position.x, z = -f.transform.position.z };
            return null;
        }

        // ---------- viaje rápido para probar (teclas 1 a 8) ----------
        [Header("Pruebas")]
        [Tooltip("Las teclas 1 a 8 llevan al jugador a cada lugar del pago")]
        public bool viajeRapido = true;

        static readonly (int k, string n, float kx, float ky, float dx, float dz)[] DESTINOS =
        {
            (1, "Ranchos de peones", 0.59f, 0.9f, 3, 12),
            (2, "Estancia (Anselmo)", 0.75f, 0.75f, 26, 36),
            (3, "Corral del arreo", 0.75f, 0.75f, -20, 62),
            (4, "Pulpería (Ramón)", 2.15f, 1.25f, 0, -2),
            (5, "Pajonal", 0.95f, 1.62f, 0, 0),
            (6, "Laguna", 1.7f, 1.85f, 0, 0),
            (7, "Monte", 2.3f, 0.7f, 0, 0),
            (8, "Fortín", 0.12f, 1.15f, 0, 14),
        };

        void ViajeRapido()
        {
            foreach (var d in DESTINOS)
            {
                if (!Teclas.Numero(d.k)) continue;
                var k = MapaPago.K(d.kx, d.ky);
                float x = k.x + d.dx, z = k.y + d.dz;
                if (d.k == 3 && fauna != null && fauna.Rodeo != Vector2.zero) { x = fauna.Rodeo.x; z = fauna.Rodeo.y + 18; }
                var p = new Vector3(x, 0, -z);
                p.y = Terreno.Altura(p) + 0.3f;
                var cc = jugador.GetComponent<CharacterController>();
                cc.enabled = false;
                jugador.transform.position = p;
                cc.enabled = true;
                Decir("Viaje rápido: " + d.n);
            }
        }

        // ---------- bucle ----------
        void LateUpdate()
        {
            mapa?.PrepararMini();   // el minimapa girado se arma fuera del OnGUI
        }

        void Update()
        {
            float dt = Time.deltaTime;
            tiempo += dt;
            if (avisoT > 0) avisoT -= dt;
            mercado?.Actualizar(dt);
            if (jugador == null) return;
            var p = jugador.transform.position;
            foreach (var n in npcs) if (n != null) n.Actualizar(dt, p);
            AvisoDelBorde(dt, jugador.transform);

            var kb = Keyboard.current;
            bool Tecla(Key k) => kb != null && kb[k].wasPressedThisFrame;
            bool e = Tecla(Key.E);
            if (Oficios.Ocupado) return;   // pescando, cocinando o con el morral abierto

            if (tienda) { if (e || Tecla(Key.Escape)) CerrarTienda(); return; }
            if (hablando != null)
            {
                jugador.MirarA(hablando.transform.position);
                if (e || Tecla(Key.Space) || Tecla(Key.Enter)) Avanzar();
                return;
            }
            if (viajeRapido) ViajeRapido();
            if (Tecla(Key.M)) mapaGrande = !mapaGrande;
            if (Tecla(Key.Escape)) mapaGrande = false;

            cercano = null;
            float mejor = DISTANCIA_HABLAR;
            foreach (var n in npcs)
            {
                if (n == null) continue;
                float d = Vector2.Distance(new Vector2(n.transform.position.x, n.transform.position.z), new Vector2(p.x, p.z));
                if (d < mejor) { mejor = d; cercano = n; }
            }
            cercaTranquera = cercano == null && fauna != null && fauna.Rodeo != Vector2.zero && Vector3.Distance(fauna.Tranq.PosUnity, p) < 5.5f;
            if (e && cercano != null) Abrir(cercano);
            else if (e && cercaTranquera) fauna.AlternarTranquera();
        }

        // ---------- HUD provisorio ----------
        void Estilos()
        {
            // Unity descarga solo las texturas que cree que nadie usa (cuando sube la memoria) y no sabe que los
            // estilos del GUI apuntan a esta: sin DontUnloadUnusedAsset los cuadros se quedaban sin fondo.
            if (sCaja != null && papel != null && degradado != null && oscuro != null && circulo != null) return;
            // interfaz liviana, al estilo de los juegos de aventura: texto sobre degradado, avisos junto al personaje y paneles chicos a los bordes
            degradado = new Texture2D(1, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int i = 0; i < 64; i++) degradado.SetPixel(0, i, new Color(0.05f, 0.04f, 0.03f, Mathf.Pow(1 - i / 63f, 1.6f) * 0.74f));   // i = 0 es abajo
            degradado.Apply();
            oscuro = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            oscuro.SetPixel(0, 0, new Color(0.06f, 0.05f, 0.04f, 0.6f));
            oscuro.Apply();
            circulo = Ui.Disco;
            var crema = new Color(0.97f, 0.93f, 0.84f);
            sSub = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            sSub.normal.textColor = crema;
            sSubNombre = new GUIStyle(sSub) { fontStyle = FontStyle.Bold };
            sSubNombre.normal.textColor = new Color(0.93f, 0.72f, 0.45f);
            sSubAyuda = new GUIStyle(sSub) { alignment = TextAnchor.LowerRight };
            sSubAyuda.normal.textColor = new Color(crema.r, crema.g, crema.b, 0.65f);
            sPildora = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            sPildora.normal.textColor = crema;
            sPildoraCaja = new GUIStyle(GUI.skin.box);
            sPildoraCaja.normal.background = Ui.Capsula; sPildoraCaja.border = Ui.BordeCapsula;
            sTecla = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sTecla.normal.textColor = new Color(0.16f, 0.13f, 0.1f);
            sHud = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 8, 10) };
            sHud.normal.background = Ui.Oscuro; sHud.border = Ui.Borde;
            sHudTitulo = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true };
            sHudTitulo.normal.textColor = new Color(0.93f, 0.72f, 0.45f);
            sHudTexto = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            sHudTexto.normal.textColor = crema;
            papel = Ui.Papel;
            var tinta = new Color(0.165f, 0.133f, 0.106f);
            sCaja = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 10, 12) };
            sCaja.normal.background = papel; sCaja.border = Ui.Borde;
            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true };
            sTitulo.normal.textColor = new Color(0.48f, 0.11f, 0.08f);
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            sTexto.normal.textColor = tinta;
            sEtiqueta = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 3, 4) };
            sEtiqueta.normal.background = Ui.Pildora; sEtiqueta.border = Ui.BordePildora;
            sEtiqueta.normal.textColor = crema;
            sAviso = new GUIStyle(sEtiqueta) { fontSize = 15, fontStyle = FontStyle.Italic, wordWrap = true };
            sAyuda = new GUIStyle(sTexto) { fontSize = 13 };
            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 13 };
            sCelda = new GUIStyle(sTexto) { fontSize = 14, wordWrap = false };
        }

        float Rumbo()
        {
            var f = jugador.transform.forward;
            return Mathf.Atan2(f.x, -f.z);
        }

        void OnGUI()
        {
            GUI.matrix = Ui.Matriz;
            if (jugador == null) return;
            Estilos();
            var cam = camara != null ? camara.GetComponent<Camera>() : Camera.main;
            var p = jugador.transform.position;

            // nombres sobre los personajes cercanos
            if (cam != null && hablando == null && !tienda && !CamaraDialogo.Activa)
                foreach (var n in npcs)
                {
                    if (n == null) continue;
                    float d = Vector3.Distance(n.transform.position, p);
                    if (d > DISTANCIA_ETIQUETA) continue;
                    var sp = cam.WorldToScreenPoint(n.transform.position + Vector3.up * n.AltoEtiqueta);
                    if (sp.z < 0) continue;
                    var tam = sEtiqueta.CalcSize(new GUIContent(n.Nombre));
                    GUI.Label(new Rect(sp.x / Ui.Escala - tam.x / 2, Ui.Alto - sp.y / Ui.Escala - tam.y, tam.x, tam.y), n.Nombre, sEtiqueta);
                }

            // misión, monedas y lo que llevás
            if (hablando == null)   // en una conversación no hay nada más en pantalla que el texto: que no tape a los personajes
            {
                GUILayout.BeginArea(new Rect(14, 14, 330, 200));
                GUILayout.BeginVertical(sHud);
                GUILayout.Label(misionTitulo, sHudTitulo);
                GUILayout.Label(misionTexto, sHudTexto);
                GUILayout.Label("$ " + monedas + "   ·   Morral " + Partida.Peso.ToString("0.#") + " kg (I)", sHudTexto);
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }

            // minimapa
            const float S = 200;
            if (mapa != null && !mapaGrande && hablando == null && !tienda)
            {
                var area = new Rect(Ui.Ancho - S - 16, Ui.Alto - S - 16, S, S);
                var cc0 = GUI.color;
                GUI.color = new Color(0.06f, 0.05f, 0.04f, 0.7f);
                GUI.DrawTexture(new Rect(area.x - 6, area.y - 6, S + 12, S + 12), circulo);   // fondo redondo
                GUI.color = cc0;
                mapa.DibujarMini(area, p.x, -p.z, Rumbo(), camara != null ? camara.YawVis : Mathf.PI * 0.75f, Lugares(), Destino());
                var cc1 = GUI.color; GUI.color = new Color(0.93f, 0.72f, 0.45f, 0.9f);
                GUI.DrawTexture(new Rect(area.x - 2, area.y - 2, S + 4, S + 4), Ui.Aro);   // aro dorado
                GUI.color = cc1;
            }

            // ayuda de controles (se va sola)
            if (tiempo < 25 && hablando == null)
            {
                GUILayout.BeginArea(new Rect(Ui.Ancho - 296, 16, 280, 290));
                GUILayout.BeginVertical(sHud);
                GUILayout.Label("WASD / flechas: caminar\nShift: correr\nQ / R: girar la vista\nRueda: zoom\nE: hablar, tranquera\nF: pescar, juntar, fogón\nI: morral\nM: mapa grande\nEsc: cerrar\n9: ir a Buenos Aires (pruebas)" +(viajeRapido ? "\n1–8: viaje rápido (pruebas)" : ""), sHudTexto);
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }

            // aviso (noticias, jinetes, resultados)
            if (avisoT > 0 && !string.IsNullOrEmpty(aviso))
            {
                var tam = sAviso.CalcSize(new GUIContent(aviso));
                float w = Mathf.Clamp(tam.x + 24, 280, Ui.Ancho * 0.7f);
                float h = sAviso.CalcHeight(new GUIContent(aviso), w) + 14;   // la cursiva no entra justa
                var c0 = GUI.color;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(avisoT / 0.5f));
                GUI.Label(new Rect((Ui.Ancho - w) / 2, Ui.Alto - 140 - h, w, h), aviso, sAviso);
                GUI.color = c0;
            }

            if (mapaGrande && mapa != null)
            {
                float w = Mathf.Min(Ui.Ancho - 80, (Ui.Alto - 80) * 4f / 3f), h = w * 3f / 4f;
                var area = new Rect((Ui.Ancho - w) / 2, (Ui.Alto - h) / 2, w, h);
                GUI.Box(new Rect(area.x - 4, area.y - 4, w + 8, h + 8), GUIContent.none, sCaja);
                var personas = npcs.Where(n => n != null).Select(n => new Vector2(n.transform.position.x, -n.transform.position.z)).ToList();
                mapa.DibujarGrande(area, p.x, -p.z, Rumbo(), camara != null ? camara.YawVis : 0, Lugares(), Destino(), personas);
            }

            if (tienda) DibujarTienda();
            else if (hablando != null) DibujarSubtitulo();
            else if ((cercano != null || cercaTranquera) && cam != null) DibujarAviso(cam);
        }

        // ---------- interfaz liviana ----------
        static void ConSombra(Rect r, string t, GUIStyle s)
        {
            var c = s.normal.textColor;
            s.normal.textColor = new Color(0, 0, 0, 0.85f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), t, s);
            GUI.Label(new Rect(r.x - 1f, r.y + 1.5f, r.width, r.height), t, s);
            s.normal.textColor = c;
            GUI.Label(r, t, s);
        }

        /// La conversación se lee como un subtítulo: texto sobre un degradado al pie, sin caja, para que los personajes queden a la vista.
        void DibujarSubtitulo()
        {
            int fs = Mathf.Clamp(Mathf.RoundToInt(Ui.Alto * 0.027f), 16, 30);
            sSub.fontSize = fs; sSubNombre.fontSize = Mathf.RoundToInt(fs * 0.8f); sSubAyuda.fontSize = Mathf.RoundToInt(fs * 0.62f);
            var texto = lineas[Mathf.Min(linea, lineas.Length - 1)];
            bool gallego = PlanosDialogo.HablaElGallego(texto);
            string quien = gallego ? "El Gallego" : hablando.Nombre;
            string dicho = gallego ? texto.Substring(texto.IndexOf(':') + 1).Trim() : texto;
            float w = Mathf.Min(Ui.Ancho * 0.64f, 900), x = (Ui.Ancho - w) / 2;
            float hN = sSubNombre.CalcHeight(new GUIContent(quien), w), hT = sSub.CalcHeight(new GUIContent(dicho), w), hA = sSubAyuda.CalcHeight(new GUIContent("E ▸"), 80);
            float total = hN + hT + 10, y = Ui.Alto - total - Ui.Alto * 0.045f;
            float hG = total + Ui.Alto * 0.13f;
            GUI.DrawTexture(new Rect(0, Ui.Alto - hG, Ui.Ancho, hG), degradado, ScaleMode.StretchToFill);
            ConSombra(new Rect(x, y, w, hN), quien, sSubNombre);
            ConSombra(new Rect(x, y + hN, w, hT), dicho, sSub);
            ConSombra(new Rect(x + w - 80, y + total - hA, 80, hA), "E ▸", sSubAyuda);
        }

        /// "E  Hablar con…" junto al personaje (o la tranquera), con una teclita redonda, en vez de un cartel fijo al pie.
        void DibujarAviso(Camera cam)
        {
            string verbo = cercano != null ? "Hablar con " + cercano.Nombre : (fauna.Tranq.abierta ? "Cerrar la tranquera" : "Abrir la tranquera");
            var mundo = cercano != null ? cercano.transform.position + Vector3.up * (cercano.AltoEtiqueta * 0.5f) : fauna.Tranq.PosUnity + Vector3.up * 1.1f;
            var sp = cam.WorldToScreenPoint(mundo);
            if (sp.z < 0) return;
            var tam = sPildora.CalcSize(new GUIContent(verbo));
            float h = 32, w = tam.x + 46;
            float x = Mathf.Clamp(sp.x / Ui.Escala + 30, 8, Ui.Ancho - w - 8), y = Mathf.Clamp(Ui.Alto - sp.y / Ui.Escala - h / 2, 8, Ui.Alto - h - 8);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, sPildoraCaja);
            var cc = GUI.color; GUI.color = new Color(0.95f, 0.9f, 0.8f); GUI.DrawTexture(new Rect(x + 4, y + 4, 24, 24), circulo); GUI.color = cc;
            GUI.Label(new Rect(x + 4, y + 4, 24, 24), "E", sTecla);
            GUI.Label(new Rect(x + 36, y, tam.x + 8, h), verbo, sPildora);
        }

        void DibujarTienda()
        {
            float w = Mathf.Min(700, Ui.Ancho - 32);
            float h = Mathf.Min(460, Ui.Alto - 60);
            GUILayout.BeginArea(new Rect((Ui.Ancho - w) / 2, (Ui.Alto - h) / 2, w, h));
            GUILayout.BeginVertical(sCaja, GUILayout.ExpandHeight(true));
            GUILayout.Label("Pulpería de Don Ramón", sTitulo);
            GUILayout.Label($"Tenés ${monedas} · Morral {Partida.Peso:0.#} de {Partida.CargaMaxima:0} kg · Comercio nivel {NivelComercio} (a más nivel, mejor precio). Los precios cambian con las noticias del camino.", sAyuda);
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mercadería", sCelda, GUILayout.Width(w * 0.3f));
            GUILayout.Label("Mercado", sCelda, GUILayout.Width(w * 0.15f));
            GUILayout.Label("Ramón vende a", sCelda, GUILayout.Width(w * 0.25f));
            GUILayout.Label("Ramón compra a", sCelda);
            GUILayout.EndHorizontal();
            int niv = NivelComercio;
            tiendaScroll = GUILayout.BeginScrollView(tiendaScroll);
            foreach (var b in mercado.bienes)
            {
                int c = mercado.Compra(b, niv), v = mercado.Venta(b, niv);
                int n = Partida.Cantidad(b.id);
                float rel = b.precio / b.base_;
                string tend = rel > 1.12f ? "▲ cara" : rel < 0.9f ? "▼ barata" : "·";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{b.nombre}  (tenés {n})", sCelda, GUILayout.Width(w * 0.3f));
                GUILayout.Label(tend, sCelda, GUILayout.Width(w * 0.15f));
                GUILayout.BeginHorizontal(GUILayout.Width(w * 0.25f));
                if (b.vende)
                {
                    GUILayout.Label("$" + c, sCelda, GUILayout.Width(40));
                    GUI.enabled = monedas >= c;
                    if (GUILayout.Button("Comprar", sBoton, GUILayout.Width(80))) Operar(true, b);
                    GUI.enabled = true;
                }
                else GUILayout.Label("no vende", sCelda);
                GUILayout.EndHorizontal();
                if (b.compra)
                {
                    GUILayout.Label("$" + v, sCelda, GUILayout.Width(40));
                    GUI.enabled = n >= 1;
                    if (GUILayout.Button("Vender", sBoton, GUILayout.Width(80))) Operar(false, b);
                    GUI.enabled = true;
                }
                else GUILayout.Label("no compra", sCelda);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Label(tiendaMsg, sTexto);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
