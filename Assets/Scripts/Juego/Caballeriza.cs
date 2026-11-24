using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Argentina1860
{
    /// <summary>
    /// El caballo del jugador en cualquier mundo: si tiene uno, lo pone cerca al cargar la escena (montado, si venía
    /// montado). E monta o baja (si no hay nadie cerca con quien hablar), H silba para que venga.
    /// Se instala solo, como CambioDeMundo; no hace falta ponerlo en las escenas.
    ///
    /// Cómo se consigue el caballo lo decide el guion (ver Docs/guion/caballo.md). Mientras tanto, con
    /// <see cref="TRUCOS"/> activado, O da uno para probar.
    /// </summary>
    public class Caballeriza : MonoBehaviour
    {
        /// O da un caballo (para probar hasta que esté la misión). Apagar antes de publicar el juego.
        public const bool TRUCOS = true;

        // estado del jugador que pasa de un mundo al otro (después irá a la partida guardada)
        public static bool TieneCaballo;
        public static string Nombre = "Moro", Pelaje = "#5c6168";   // moro: pelo negro y blanco mezclados, gris azulado
        public static bool Calcetines = true;
        static bool ibaMontado;

        /// El que se instaló al arrancar (para que las misiones den el caballo con <see cref="DarCaballo"/>).
        public static Caballeriza Instancia { get; private set; }

        const float DISTANCIA_MONTAR = 2.8f, DISTANCIA_HABLAR = 4.5f;
        ControlJugador jug;
        CaballoMontable caballo;
        NPC[] npcs = new NPC[0];
        string aviso; float avisoT, tiempo;
        bool listo;
        GUIStyle sCaja, sEtiqueta, sTexto;
        Texture2D papel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Instalar()
        {
            var go = new GameObject("Caballeriza");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            Instancia = go.AddComponent<Caballeriza>();
        }

        void OnEnable() { SceneManager.sceneLoaded += AlCargar; StartCoroutine(Preparar()); }
        void OnDisable() { SceneManager.sceneLoaded -= AlCargar; }
        void AlCargar(Scene s, LoadSceneMode m) { StopAllCoroutines(); StartCoroutine(Preparar()); }

        IEnumerator Preparar()
        {
            listo = false; jug = null; caballo = null; tiempo = 0;
            while ((jug = FindAnyObjectByType<ControlJugador>()) == null) yield return null;
            // la ciudad arma las manzanas y después mueve al jugador a un lugar libre: esperar a que termine
            var ciudad = Ciudad.Instancia;
            if (ciudad != null) while (ciudad != null && !ciudad.Generado) yield return null;
            for (int i = 0; i < 5; i++) yield return null;
            npcs = FindObjectsByType<NPC>();
            if (TieneCaballo) Aparecer(ibaMontado);
            listo = true;
        }

        /// Sin caballo, como al empezar una partida nueva (lo usan también los tests).
        public static void Reiniciar()
        {
            TieneCaballo = false; ibaMontado = false;
            Nombre = "Moro"; Pelaje = "#5c6168"; Calcetines = true;
        }

        /// Da el caballo al jugador y lo pone a su lado (lo usan las misiones; la O en pruebas).
        public void DarCaballo(string nombre = null, string pelaje = null)
        {
            if (nombre != null) Nombre = nombre;
            if (pelaje != null) Pelaje = pelaje;
            TieneCaballo = true;
            if (caballo == null && jug != null) Aparecer(false);
        }

        void Aparecer(bool montado)
        {
            if (jug == null) return;
            var p = jug.transform.position;
            if (!montado && LugarLibre(p, 3.5f, out var libre)) p = libre;
            caballo = CaballoMontable.Crear(p, jug.transform.eulerAngles.y + (montado ? 0 : 90), Nombre, Pelaje, Calcetines);
            if (montado) jug.Montar(caballo);
        }

        void Update()
        {
            if (avisoT > 0) avisoT -= Time.deltaTime;
            tiempo += Time.deltaTime;
            // hasta que termina Preparar el jugador nuevo todavía va a pie: no pisar si venía montado
            if (jug == null || !listo) return;
            ibaMontado = jug.Montura != null;
            var kb = Keyboard.current;
            if (kb == null) return;

            if (TRUCOS && kb.oKey.wasPressedThisFrame && !TieneCaballo)
            {
                DarCaballo();
                Avisar($"Te dieron el {Nombre} (pruebas). E para montarlo, H para silbarle.");
            }
            if (jug.Bloqueado) return;   // diálogo o tienda abiertos

            if (kb.hKey.wasPressedThisFrame && TieneCaballo) Silbar();
            if (kb.eKey.wasPressedThisFrame && !HayConQuienHablar())
            {
                if (jug.Montura != null) Bajarse();
                else if (PuedeMontar()) jug.Montar(caballo);
            }
        }

        bool PuedeMontar() => caballo != null && !caballo.Montado && Distancia(caballo.transform.position) < DISTANCIA_MONTAR;

        float Distancia(Vector3 p)
        {
            var j = jug.transform.position;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(j.x, j.z));
        }

        /// La E es para hablar si hay alguien cerca (lo maneja JuegoPago o JuegoCiudad).
        bool HayConQuienHablar()
        {
            foreach (var n in npcs) if (n != null && Distancia(n.transform.position) < DISTANCIA_HABLAR) return true;
            return false;
        }

        void Bajarse()
        {
            var t = jug.transform;
            foreach (var lado in new[] { -t.right * 1.4f, t.right * 1.4f, -t.forward * 2.2f, t.forward * 2.2f })
                if (Libre(t.position + lado, 0.35f, t.position.y, out var p)) { jug.Desmontar(p + Vector3.up * 0.05f); return; }
            Avisar("No hay lugar para bajarse acá.");
        }

        void Silbar()
        {
            if (caballo == null) { Aparecer(false); caballo?.Llamar(jug.transform); return; }
            if (caballo.Montado) return;
            // si quedó lejos (o en otra punta del mapa) aparece a una distancia prudente y viene al trote
            if (Distancia(caballo.transform.position) > 45 && LugarLibre(jug.transform.position, 30, out var p)) caballo.Teletransportar(p);
            caballo.Llamar(jug.transform);
            Avisar("Silbás. " + Nombre + " viene.");
        }

        void Avisar(string s) { aviso = s; avisoT = 3.5f; }

        // ---------- lugares libres (sirve en el pago y en la ciudad) ----------
        /// Busca un lugar libre en un anillo de radio <paramref name="r"/> alrededor de <paramref name="centro"/>.
        public static bool LugarLibre(Vector3 centro, float r, out Vector3 p)
        {
            float a0 = Random.value * Mathf.PI * 2;
            for (int i = 0; i < 16; i++)
            {
                float a = a0 + i * Mathf.PI / 8;
                if (Libre(centro + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r, 0.6f, centro.y, out p)) return true;
            }
            p = centro;
            return false;
        }

        /// Hay piso cerca de la altura de referencia, no es agua y no choca con nada (paredes, árboles, gente).
        public static bool Libre(Vector3 q, float radio, float yRef, out Vector3 suelo)
        {
            suelo = q;
            if (ControlJugador.AguaEn(q.x, -q.z)) return false;
            if (!Physics.Raycast(new Vector3(q.x, yRef + 25, q.z), Vector3.down, out var h, 50, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (Mathf.Abs(h.point.y - yRef) > 1.5f) return false;   // techo, pared o barranca
            suelo = h.point;
            return !Physics.CheckCapsule(h.point + Vector3.up * (radio + 0.4f), h.point + Vector3.up * 1.8f, radio, ~0, QueryTriggerInteraction.Ignore);
        }

        // ---------- HUD provisorio ----------
        void Estilos()
        {
            if (sCaja != null && papel != null) return;
            papel = Ui.Papel;
            var tinta = new Color(0.165f, 0.133f, 0.106f);
            sCaja = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 8, 10) };
            sCaja.normal.background = papel; sCaja.border = Ui.Borde;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            sTexto.normal.textColor = tinta;
            sEtiqueta = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 3, 4), wordWrap = true };
            sEtiqueta.normal.background = papel; sEtiqueta.border = Ui.Borde;
            sEtiqueta.normal.textColor = tinta;
        }

        void OnGUI()
        {
            GUI.matrix = Ui.Matriz;
            if (jug == null) return;
            Estilos();

            if (jug.Montura != null)
            {
                // aliento del caballo y teclas, abajo a la izquierda
                var c = jug.Montura;
                var area = new Rect(16, Ui.Alto - 96, 250, 80);
                GUI.Box(area, GUIContent.none, sCaja);
                GUI.Label(new Rect(area.x + 12, area.y + 6, 226, 20), c.nombre + (c.Cansado ? " · cansado" : ""), sTexto);
                var barra = new Rect(area.x + 12, area.y + 30, 226, 10);
                GUI.DrawTexture(barra, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.3f, 0.25f, 0.2f, 0.35f), 0, 3);
                var lleno = new Rect(barra.x, barra.y, barra.width * c.Aliento, barra.height);
                var col = c.Cansado ? new Color(0.63f, 0.2f, 0.13f) : new Color(0.42f, 0.33f, 0.18f);
                GUI.DrawTexture(lleno, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, col, 0, 3);
                GUI.Label(new Rect(area.x + 12, area.y + 46, 226, 20), "Shift: galopar · E: bajarse", sTexto);
            }
            else if (!jug.Bloqueado && PuedeMontar() && !HayConQuienHablar())
            {
                var txt = "E  ·  Montar al " + caballo.nombre;
                var tam = sEtiqueta.CalcSize(new GUIContent(txt));
                GUI.Label(new Rect((Ui.Ancho - tam.x) / 2, Ui.Alto - 90, tam.x, tam.y), txt, sEtiqueta);
            }
            else if (TieneCaballo && tiempo < 25)
            {
                GUI.Label(new Rect(16, Ui.Alto - 44, 260, 28), "H: silbarle al " + Nombre, sEtiqueta);
            }
            else if (TRUCOS && !TieneCaballo && tiempo < 25)
            {
                GUI.Label(new Rect(16, Ui.Alto - 44, 260, 28), "O: darte un caballo (pruebas)", sEtiqueta);
            }

            if (avisoT > 0 && !string.IsNullOrEmpty(aviso))
            {
                var tam = sEtiqueta.CalcSize(new GUIContent(aviso));
                float w = Mathf.Min(tam.x, Ui.Ancho * 0.7f);
                GUI.Label(new Rect((Ui.Ancho - w) / 2, Ui.Alto - 200, w, tam.y), aviso, sEtiqueta);
            }
        }
    }
}
