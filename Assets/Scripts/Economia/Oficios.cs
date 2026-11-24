using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Argentina1860
{
    /// <summary>
    /// Lo que el Gallego hace con las manos, en cualquier mundo: el hambre y el cansancio, el morral (I), pescar en la
    /// laguna y el arroyo, juntar lombrices en la orilla, y en un fogón encendido cocinar lo que sabe y descansar (F).
    /// Se instala solo, como <see cref="Caballeriza"/>; no hace falta ponerlo en las escenas.
    /// El estado (plata, morral, recetas, necesidades) está en <see cref="Partida"/>.
    /// </summary>
    public class Oficios : MonoBehaviour
    {
        public static Oficios Instancia { get; private set; }

        /// Hay un panel abierto o el Gallego está pescando, cocinando o descansando: los diálogos y el caballo esperan.
        public static bool Ocupado => Instancia != null && Instancia.haciendo != Haciendo.Nada;

        /// Para tests automáticos: si tiene valor, decide si la tecla de pescar está apretada (reemplaza al teclado).
        public static System.Func<LancePesca, bool> ApretarPrueba;

        enum Haciendo { Nada, Morral, Fogon, Pescando, Juntando, Cocinando, Descansando }

        /// Cuánta plata da el truco de la P.
        const int TRUCO_PLATA = 50;
        const float DISTANCIA_FOGON = 3.6f, AGUA_MIN = 1.5f, AGUA_MAX = 6.5f, CORCHO_MAX = 10f;
        const string CARNADA = "carnada", LINEA = "linea", ANZUELO = "anzuelo", LOMBRICES = "lombrices";

        Haciendo haciendo;
        ControlJugador jug;
        CharacterController cc;
        Fogon[] fogones = new Fogon[0];
        JuegoPago juegoPago;
        JuegoCiudad juegoCiudad;
        bool esPago, listo;
        readonly System.Random rnd = new System.Random();

        // lo que se ofrece con F en este momento
        enum Accion { Ninguna, Pescar, Juntar, Fogon }
        Accion accion;
        string textoAccion;
        Fogon fogonCerca;
        Vector3 corchoDestino;
        int aguaTipo;   // 1 laguna, 2 arroyo

        // pesca
        LancePesca lance;
        FichaObjeto carnadaUsada;
        bool carnadaGastada;
        float lanceT;
        Transform cana, sedal, corcho;
        Vector3 corchoPos;
        float nivelAgua;
        string panelMsg = "";

        // cocinar y juntar
        FichaReceta receta;
        float trabajoT, trabajoDur;
        Vector2 scroll;

        // pantalla
        string aviso; float avisoT;
        GUIStyle sCaja, sTitulo, sTexto, sAyuda, sEtiqueta, sBoton, sCelda;
        Texture2D papel, blanco;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Instalar()
        {
            var go = new GameObject("Oficios");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            Instancia = go.AddComponent<Oficios>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += AlCargar;
            Partida.Aviso += Decir;
            StartCoroutine(Preparar());
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= AlCargar;
            Partida.Aviso -= Decir;
        }

        void AlCargar(Scene s, LoadSceneMode m) { StopAllCoroutines(); Terminar(); StartCoroutine(Preparar()); }

        IEnumerator Preparar()
        {
            listo = false; jug = null;
            while ((jug = FindAnyObjectByType<ControlJugador>()) == null) yield return null;
            cc = jug.GetComponent<CharacterController>();
            esPago = SceneManager.GetActiveScene().name == CambioDeMundo.PAGO;
            fogones = FindObjectsByType<Fogon>();
            juegoPago = FindAnyObjectByType<JuegoPago>();
            juegoCiudad = FindAnyObjectByType<JuegoCiudad>();
            listo = true;
        }

        /// Avisa por el cartel del juego del mundo actual (o por el propio si no hay).
        public void Decir(string t)
        {
            if (juegoPago != null) juegoPago.Decir(t);
            else if (juegoCiudad != null) juegoCiudad.Decir(t);
            else { aviso = t; avisoT = 4.5f; }
        }

        // ---------- bucle ----------
        void Update()
        {
            if (avisoT > 0) avisoT -= Time.deltaTime;
            if (!listo || jug == null) return;
            float dt = Time.deltaTime;
            Partida.Pasar(dt, ActividadActual());

            var kb = Keyboard.current;
            bool T(Key k) => kb != null && kb[k].wasPressedThisFrame;
            bool f = T(Key.F), esc = T(Key.Escape), i = T(Key.I);

            // truco de pruebas (se apaga con Caballeriza.TRUCOS, igual que la O del caballo): P da plata
            if (Caballeriza.TRUCOS && T(Key.P) && !jug.Bloqueado) { Partida.Monedas += TRUCO_PLATA; Decir($"Truco: ${TRUCO_PLATA} (tenés ${Partida.Monedas})."); }

            switch (haciendo)
            {
                case Haciendo.Nada:
                    BuscarAccion();
                    if (jug.Bloqueado) break;   // diálogo o pulpería
                    if (i) Abrir(Haciendo.Morral);   // también a caballo (el caballo se frena mientras está abierto)
                    else if (f && jug.Montura == null) Hacer();   // pescar, juntar y el fogón, a pie
                    break;
                case Haciendo.Morral:
                    if (i || esc || f) Cerrar();
                    break;
                case Haciendo.Fogon:
                    if (esc || f) Cerrar();
                    break;
                case Haciendo.Pescando:
                    Pescar(dt, kb, esc);
                    break;
                case Haciendo.Juntando:
                case Haciendo.Cocinando:
                    Trabajar(dt, esc);
                    break;
                case Haciendo.Descansando:
                    jug.Personaje.Anim?.Sentado(Time.time, 0, 0, 0.62f);
                    if (esc || f || Moviendo(kb) || Partida.Cansancio <= 0) { Decir(Partida.Cansancio <= 0 ? "Descansaste. Ya estás como nuevo." : "Te levantás."); Cerrar(); }
                    break;
            }
        }

        Actividad ActividadActual()
        {
            switch (haciendo)
            {
                case Haciendo.Pescando: case Haciendo.Juntando: case Haciendo.Cocinando: return Actividad.Trabajando;
                case Haciendo.Descansando: return Actividad.Descansando;
            }
            if (jug.Montura != null) return Actividad.Montado;
            var v = cc != null ? cc.velocity : Vector3.zero; v.y = 0;
            return jug.Corriendo && v.magnitude > 5.5f ? Actividad.Corriendo : v.magnitude > 0.5f ? Actividad.Caminando : Actividad.Quieto;
        }

        static bool Moviendo(Keyboard kb) => kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed
            || kb.upArrowKey.isPressed || kb.downArrowKey.isPressed || kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed);

        void Abrir(Haciendo h)
        {
            haciendo = h;
            jug.Bloqueado = true;
            panelMsg = "";
            scroll = Vector2.zero;
        }

        void Cerrar()
        {
            if (haciendo == Haciendo.Nada) return;
            haciendo = Haciendo.Nada;
            if (jug != null) { jug.Bloqueado = false; jug.AnimaOtro = false; }
            BorrarAparejo();
            lance = null;
        }

        /// Corta lo que esté haciendo (al cambiar de escena o desde un test).
        public void Terminar() => Cerrar();

        // ---------- qué se puede hacer acá ----------
        void BuscarAccion()
        {
            accion = Accion.Ninguna; textoAccion = null; fogonCerca = null;
            if (jug.Montura != null) return;
            var p = jug.transform.position;

            float mejor = DISTANCIA_FOGON;
            foreach (var fg in fogones)
            {
                if (fg == null) continue;
                float d = Vector3.Distance(fg.transform.position, p);
                if (d < mejor) { mejor = d; fogonCerca = fg; }
            }
            if (fogonCerca != null)
            {
                if (Encendido(fogonCerca)) { accion = Accion.Fogon; textoAccion = "F  ·  Fogón: cocinar o descansar"; }
                else
                {
                    // todavía no hay leña para prenderlo: se usa cuando la gente lo tiene encendido
                    textoAccion = fogonCerca.siempre ? "El fogón está apagado" : $"El fogón está apagado: lo prenden de {Hora(fogonCerca.manana.x)} a {Hora(fogonCerca.manana.y)} y de {Hora(fogonCerca.tarde.x)} a {Hora(fogonCerca.tarde.y)}";
                    fogonCerca = null;
                }
                return;
            }

            if (!esPago || !AguaAdelante(p, jug.transform.forward, out corchoDestino, out aguaTipo)) return;
            if (Partida.Cantidad(LINEA) > 0 && Partida.Cantidad(ANZUELO) > 0 && Partida.CantidadCategoria(CARNADA) > 0)
            {
                accion = Accion.Pescar;
                textoAccion = $"F  ·  Pescar en {(aguaTipo == 1 ? "la laguna" : "el arroyo")}";
            }
            else
            {
                accion = Accion.Juntar;
                textoAccion = "F  ·  Juntar lombrices en la orilla" + (Partida.Cantidad(LINEA) == 0 ? "  (para pescar: línea, anzuelo y carnada)" : Partida.Cantidad(ANZUELO) == 0 ? "  (te falta un anzuelo para pescar)" : "");
            }
        }

        static bool Encendido(Fogon f) => f.Fuego > 0.15f || f.Brasa > 0.3f;

        static string Hora(float h) { int hh = Mathf.FloorToInt(h), mm = Mathf.RoundToInt((h - hh) * 60); return mm == 0 ? $"{hh}" : $"{hh}:{mm:00}"; }

        /// Agua para pescar delante del jugador, en el pago. Devuelve dónde caería el corcho y si es la laguna (1) o el arroyo (2).
        public static bool AguaAdelante(Vector3 pos, Vector3 adelante, out Vector3 corcho, out int tipo)
        {
            corcho = default; tipo = 0;
            adelante.y = 0;
            if (adelante.sqrMagnitude < 1e-4f) return false;
            adelante.Normalize();
            float primera = -1;
            for (float d = AGUA_MIN; d <= CORCHO_MAX; d += 0.5f)
            {
                var q = pos + adelante * d;
                int t = TipoAgua(q.x, -q.z);
                if (t == 0) { if (primera >= 0) break; if (d > AGUA_MAX) return false; continue; }
                if (primera < 0) { primera = d; tipo = t; }
                corcho = q;
                if (d >= primera + 3) break;   // unos metros adentro alcanza
            }
            if (primera < 0) return false;
            corcho.y = NivelAgua(corcho);
            return true;
        }

        /// 1 laguna, 2 arroyo, 0 tierra (coordenadas del prototipo).
        public static int TipoAgua(float x, float z) => MapaPago.DistLaguna(x, z) < 0 ? 1 : MapaPago.EnArroyo(x, z) ? 2 : 0;

        /// Altura de la superficie del agua (la misma cuenta que AguaViva: lo más alto del fondo alrededor, un poco arriba).
        static float NivelAgua(Vector3 p)
        {
            float fondo = float.MinValue;
            for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                    fondo = Mathf.Max(fondo, Terreno.Altura(p + new Vector3(dx * 1.5f, 0, dz * 1.5f)));
            return fondo + 0.08f;
        }

        void Hacer()
        {
            switch (accion)
            {
                case Accion.Fogon: Abrir(Haciendo.Fogon); break;
                case Accion.Pescar: EmpezarPesca(); break;
                case Accion.Juntar:
                    Abrir(Haciendo.Juntando);
                    trabajoT = 0; trabajoDur = 3.5f;
                    jug.AnimaOtro = true;
                    break;
            }
        }

        // ---------- pescar ----------
        /// Tira la línea hacia el agua que tiene adelante (lo usa la F; público para los tests).
        public bool EmpezarPesca()
        {
            if (jug == null || !AguaAdelante(jug.transform.position, jug.transform.forward, out corchoDestino, out aguaTipo)) return false;
            carnadaUsada = Partida.Morral().Where(x => x.o.categoria == CARNADA).OrderBy(x => x.o.precio).Select(x => x.o).FirstOrDefault();
            if (carnadaUsada == null || Partida.Cantidad(LINEA) == 0 || Partida.Cantidad(ANZUELO) == 0) return false;

            int nivel = Partida.Nivel(Partida.PESCA);
            var (pez, espera) = ElegirPez(aguaTipo == 1, nivel, Ambiente.Hora, rnd);
            if (pez == null) { Decir("Acá no pica nada."); return false; }
            lance = new LancePesca(pez, pez.fuerza, espera, nivel, Partida.FactorTrabajo, rnd);
            carnadaGastada = false;
            apretabaAntes = false;
            lanceT = 0;
            Abrir(Haciendo.Pescando);
            jug.MirarA(corchoDestino);
            ArmarAparejo();
            return true;
        }

        /// Qué pez va a picar y en cuántos segundos, según el agua, la hora y el nivel de pesca.
        public static (FichaObjeto pez, float espera) ElegirPez(bool laguna, int nivel, float hora, System.Random rnd)
        {
            bool alba = (hora >= 5 && hora < 8.5f) || (hora >= 17.5f && hora < 20.5f), noche = hora >= 20.5f || hora < 5;
            float Peso(FichaObjeto o)
            {
                if (o.tipo != TipoObjeto.Pescado || (laguna ? !o.enLaguna : !o.enArroyo) || nivel < o.nivelMinimo) return 0;
                float h = o.hora switch
                {
                    HoraPesca.AlbaYAtardecer => alba ? 2.2f : 0.6f,
                    HoraPesca.Noche => noche ? 2.5f : alba ? 1f : 0.3f,
                    HoraPesca.Dia => noche ? 0.2f : 1.3f,
                    _ => 1,
                };
                return o.abundancia * h;
            }
            var peces = Catalogo.Objetos.Select(o => (o, w: Peso(o))).Where(x => x.w > 0).ToList();
            float total = peces.Sum(x => x.w);
            if (total <= 0) return (null, 0);
            float r = (float)rnd.NextDouble() * total;
            var elegido = peces[peces.Count - 1].o;
            foreach (var (o, w) in peces) { if (r < w) { elegido = o; break; } r -= w; }
            // al alba y al atardecer pica más rápido; con nivel, también
            float espera = (4 + (float)rnd.NextDouble() * 9) * (alba ? 0.7f : 1) / (1 + 0.05f * (nivel - 1));
            return (elegido, espera);
        }

        void Pescar(float dt, Keyboard kb, bool esc)
        {
            lanceT += dt;
            const float VUELO = 0.7f;
            bool aprieta, recien;
            if (ApretarPrueba != null) { aprieta = ApretarPrueba(lance); recien = aprieta && !apretabaAntes; }
            else { aprieta = kb != null && kb.fKey.isPressed; recien = kb != null && kb.fKey.wasPressedThisFrame; }
            apretabaAntes = aprieta;

            if (esc || (lance.Estado == EstadoLance.Esperando && Moviendo(kb)))
            {
                if (lance.Estado == EstadoLance.Peleando) { GastarCarnada(); Decir("Soltaste la línea y el pez se fue."); }
                Cerrar();
                return;
            }
            if (lanceT > VUELO) lance.Paso(dt, aprieta, recien && lanceT > VUELO + 0.1f);
            if (lance.Estado >= EstadoLance.Peleando && lance.Estado != EstadoLance.Temprano) GastarCarnada();
            MoverAparejo(Mathf.Clamp01(lanceT / VUELO));
            if (lance.Terminado) FinPesca();
        }
        bool apretabaAntes;

        void GastarCarnada()
        {
            if (carnadaGastada || carnadaUsada == null) return;
            Partida.Sacar(carnadaUsada);
            carnadaGastada = true;
        }

        void FinPesca()
        {
            var pez = lance.Pez;
            switch (lance.Estado)
            {
                case EstadoLance.Sacado:
                    Partida.Ganar(Partida.PESCA, 1 + pez.fuerza * 2.5f);
                    if (Partida.Dar(pez)) Decir($"¡Sacaste {Articulo(pez)} de {pez.peso.ToString("0.#")} kg!");
                    else Decir($"Sacaste {Articulo(pez)}, pero no te entra en el morral: la devolvés al agua.");
                    break;
                case EstadoLance.Escapo:
                    Partida.Ganar(Partida.PESCA, 0.3f);
                    Decir("La línea quedó floja y el pez se soltó.");
                    break;
                case EstadoLance.Cortado:
                    Partida.Ganar(Partida.PESCA, 0.2f);
                    Partida.Sacar(ANZUELO);
                    Decir("¡Se cortó la línea! Perdiste el anzuelo." + (Partida.Cantidad(ANZUELO) == 0 ? " Don Ramón vende anzuelos en la pulpería." : ""));
                    break;
                case EstadoLance.SinCarnada:
                    Decir("Picó y no tiraste a tiempo: se comió la carnada.");
                    break;
                case EstadoLance.Temprano:
                    Decir("Tiraste antes de que picara. Esperá a que se hunda el corcho.");
                    break;
            }
            Cerrar();
        }

        static string Articulo(FichaObjeto o)
        {
            string n = o.nombre.ToLowerInvariant();
            return (n.EndsWith("a") ? "una " : "un ") + n;
        }

        // ---------- caña, sedal y corcho ----------
        void ArmarAparejo()
        {
            BorrarAparejo();
            var raiz = jug.transform;

            var nc = new Nodo("caña de tacuara");
            nc.Pieza(Geo.Cilindro(0.012f, 0.022f, 2.6f), "#b8a26a", new Vector3(0, 1.3f, 0));
            nc.Pieza(Geo.Cilindro(0.026f, 0.026f, 0.05f), "#6a5a3a", new Vector3(0, 0.5f, 0));   // nudo de la tacuara
            nc.Pieza(Geo.Cilindro(0.02f, 0.02f, 0.04f), "#6a5a3a", new Vector3(0, 1.4f, 0));
            cana = nc.Instanciar(raiz, null);
            cana.localPosition = new Vector3(0.26f, 1.05f, 0.2f);
            cana.localRotation = Quaternion.Euler(52, 0, 0);   // hacia adelante y arriba

            var ns = new Nodo("sedal");
            ns.Pieza(Geo.Caja(0.012f, 1, 0.012f), "#e8e2d0", Vector3.zero);
            sedal = ns.Instanciar(null, null);

            var nk = new Nodo("corcho");
            nk.Pieza(Geo.Ico(0.07f), "#b8402a", Vector3.zero);
            nk.Pieza(Geo.Cilindro(0.012f, 0.012f, 0.12f), "#e8e2d0", new Vector3(0, 0.08f, 0));
            corcho = nk.Instanciar(null, null);
            nivelAgua = corchoDestino.y;
            foreach (var r in cana.GetComponentsInChildren<Renderer>().Concat(sedal.GetComponentsInChildren<Renderer>()).Concat(corcho.GetComponentsInChildren<Renderer>()))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MoverAparejo(0);
        }

        void BorrarAparejo()
        {
            if (cana != null) Destroy(cana.gameObject);
            if (sedal != null) Destroy(sedal.gameObject);
            if (corcho != null) Destroy(corcho.gameObject);
            cana = sedal = corcho = null;
        }

        void MoverAparejo(float vuelo)
        {
            if (cana == null || lance == null) return;
            var punta = cana.TransformPoint(0, 2.6f, 0);
            float t = Time.time;
            // el corcho vuela en arco hasta el agua; al pelear se acerca a la orilla
            var orilla = new Vector3(jug.transform.position.x, nivelAgua, jug.transform.position.z) + jug.transform.forward * 1.2f;
            var agua = Vector3.Lerp(corchoDestino, orilla, lance.Avance * 0.85f);
            agua.y = nivelAgua;
            if (vuelo < 1)
            {
                corchoPos = Vector3.Lerp(punta, agua, vuelo) + Vector3.up * Mathf.Sin(vuelo * Mathf.PI) * 1.6f;
                cana.localRotation = Quaternion.Euler(Mathf.Lerp(20, 52, vuelo), 0, 0);
            }
            else
            {
                float hunde = 0, sacude = 0;
                switch (lance.Estado)
                {
                    case EstadoLance.Esperando: hunde = Mathf.Sin(t * 2.1f) * 0.012f; break;
                    case EstadoLance.Pica: hunde = -0.07f - Mathf.Abs(Mathf.Sin(t * 22)) * 0.06f; break;
                    case EstadoLance.Peleando: hunde = -0.09f; sacude = lance.Tiron ? 0.14f : 0.05f; break;
                }
                corchoPos = agua + new Vector3(Mathf.Sin(t * 17) * sacude, hunde, Mathf.Cos(t * 13) * sacude);
                float curva = lance.Estado == EstadoLance.Peleando ? 52 - lance.Tension * 22 : 52;
                cana.localRotation = Quaternion.Euler(curva + (lance.Tiron ? Mathf.Sin(t * 30) * 2 : 0), 0, 0);
            }
            corcho.position = corchoPos;
            var d = corchoPos - punta;
            sedal.position = punta + d / 2;
            sedal.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            sedal.localScale = new Vector3(1, d.magnitude, 1);
        }

        // ---------- juntar lombrices, cocinar ----------
        void Trabajar(float dt, bool esc)
        {
            if (esc) { Decir(haciendo == Haciendo.Cocinando ? "Dejaste de cocinar." : "Dejaste de juntar."); Cerrar(); return; }
            trabajoT += dt * Partida.FactorTrabajo;
            if (haciendo == Haciendo.Cocinando && fogonCerca != null) jug.MirarA(fogonCerca.transform.position);
            jug.Personaje.Anim?.Cocinando(Time.time, haciendo == Haciendo.Cocinando ? 0.45f : 1.1f);
            if (trabajoT < trabajoDur) return;

            if (haciendo == Haciendo.Juntando)
            {
                int n = rnd.Next(0, 4);
                if (n == 0) Decir("Removiste el barro de la orilla y no salió nada.");
                else if (Partida.Dar(LOMBRICES, n)) Decir($"Juntaste {n} lombri{(n == 1 ? "z" : "ces")}.");
                Cerrar();
            }
            else
            {
                var r = receta;
                bool ok = Recetario.Hacer(r, LugarReceta.Fogon);
                haciendo = Haciendo.Fogon;   // vuelve al panel del fogón
                jug.AnimaOtro = false;
                panelMsg = ok ? $"Hiciste {r.resultado.nombre}{(r.cantidad > 1 ? " ×" + r.cantidad : "")}." : "No se pudo.";
            }
        }

        void EmpezarReceta(FichaReceta r, LugarReceta aca)
        {
            if (!Recetario.PuedeHacer(r, aca, out var porQue)) { panelMsg = $"No podés: {porQue}."; return; }
            if (aca != LugarReceta.Fogon || r.segundos <= 0)
            {
                Recetario.Hacer(r, aca);
                panelMsg = $"Hiciste {r.resultado.nombre}.";
                return;
            }
            receta = r;
            trabajoT = 0; trabajoDur = r.segundos;
            haciendo = Haciendo.Cocinando;
            jug.AnimaOtro = true;
        }

        // ---------- pantalla ----------
        void Estilos()
        {
            if (sCaja != null && papel != null) return;
            papel = Ui.Papel;
            blanco = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            blanco.SetPixel(0, 0, Color.white);
            blanco.Apply();
            var tinta = new Color(0.165f, 0.133f, 0.106f);
            sCaja = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 10, 12) };
            sCaja.normal.background = papel; sCaja.border = Ui.Borde;
            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true };
            sTitulo.normal.textColor = new Color(0.48f, 0.11f, 0.08f);
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            sTexto.normal.textColor = tinta;
            sAyuda = new GUIStyle(sTexto) { fontSize = 12 };
            sCelda = new GUIStyle(sTexto) { wordWrap = false };
            sEtiqueta = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 3, 4) };
            sEtiqueta.normal.background = papel; sEtiqueta.border = Ui.Borde;
            sEtiqueta.normal.textColor = tinta;
            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 13 };
        }

        void Barra(Rect r, float v, Color c)
        {
            var c0 = GUI.color;
            GUI.color = new Color(0.2f, 0.16f, 0.12f, 0.55f);
            GUI.DrawTexture(r, blanco);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(v), r.height - 2), blanco);
            GUI.color = c0;
        }

        void OnGUI()
        {
            GUI.matrix = Ui.Matriz;
            if (!listo || jug == null) return;
            Estilos();

            Necesidades();

            switch (haciendo)
            {
                case Haciendo.Nada:
                    if (textoAccion != null && !jug.Bloqueado) Etiqueta(textoAccion, Ui.Alto - 124);
                    break;
                case Haciendo.Morral: PanelMorral(); break;
                case Haciendo.Fogon: PanelFogon(); break;
                case Haciendo.Pescando: PanelPesca(); break;
                case Haciendo.Juntando:
                case Haciendo.Cocinando:
                    Etiqueta(haciendo == Haciendo.Juntando ? "Juntando lombrices…  (Esc: dejar)" : $"Cocinando {receta?.nombre}…  (Esc: dejar)", Ui.Alto - 150);
                    Barra(new Rect(Ui.Ancho / 2 - 120, Ui.Alto - 120, 240, 12), trabajoT / trabajoDur, new Color(0.55f, 0.36f, 0.16f));
                    break;
                case Haciendo.Descansando:
                    Etiqueta("Descansando junto al fogón…  (F o moverse: levantarse)", Ui.Alto - 150);
                    Barra(new Rect(Ui.Ancho / 2 - 120, Ui.Alto - 120, 240, 12), 1 - Partida.Cansancio, new Color(0.35f, 0.5f, 0.3f));
                    break;
            }

            if (avisoT > 0 && !string.IsNullOrEmpty(aviso)) Etiqueta(aviso, Ui.Alto - 200);
        }

        void Etiqueta(string t, float y)
        {
            var tam = sEtiqueta.CalcSize(new GUIContent(t));
            GUI.Label(new Rect((Ui.Ancho - tam.x) / 2, y, tam.x, tam.y), t, sEtiqueta);
        }

        /// Hambre y cansancio arriba al centro, solo cuando ya se notan (o con el morral abierto).
        void Necesidades()
        {
            bool ver = haciendo == Haciendo.Morral || Partida.Hambre > 0.45f || Partida.Cansancio > 0.45f;
            if (!ver) return;
            float w = 300, x = (Ui.Ancho - w) / 2;
            GUI.Box(new Rect(x, 10, w, 30), GUIContent.none, sCaja);
            GUI.Label(new Rect(x + 10, 14, 60, 20), "Hambre", sAyuda);
            Barra(new Rect(x + 66, 19, 70, 10), Partida.Hambre, Partida.Hambre > Partida.UMBRAL ? new Color(0.6f, 0.2f, 0.12f) : new Color(0.62f, 0.48f, 0.25f));
            GUI.Label(new Rect(x + 150, 14, 70, 20), "Cansancio", sAyuda);
            Barra(new Rect(x + 220, 19, 70, 10), Partida.Cansancio, Partida.Cansancio > Partida.UMBRAL ? new Color(0.6f, 0.2f, 0.12f) : new Color(0.4f, 0.45f, 0.5f));
        }

        void PanelMorral()
        {
            float w = Mathf.Min(560, Ui.Ancho - 32), h = Mathf.Min(460, Ui.Alto - 80);
            GUILayout.BeginArea(new Rect((Ui.Ancho - w) / 2, (Ui.Alto - h) / 2, w, h));
            GUILayout.BeginVertical(sCaja, GUILayout.ExpandHeight(true));
            GUILayout.Label("Morral", sTitulo);
            GUILayout.Label($"$ {Partida.Monedas}  ·  Carga {Partida.Peso:0.#} de {Partida.CargaMaxima:0} kg{(Caballeriza.TieneCaballo ? " (con las alforjas del caballo)" : "")}", sTexto);
            var habs = new[] { Partida.ARREO, Partida.COMERCIO, Partida.PESCA }.Concat(Partida.Habilidades).Distinct();
            GUILayout.Label(string.Join("  ·  ", habs.Select(x => $"{Partida.NombreHabilidad(x)} nv {Partida.Nivel(x)}")), sAyuda);
            GUILayout.Space(6);

            scroll = GUILayout.BeginScrollView(scroll);
            var morral = Partida.Morral();
            if (morral.Count == 0) GUILayout.Label("Vacío. Lo que compres, pesques o juntes va acá.", sTexto);
            foreach (var (o, n) in morral)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent($"{o.nombre} ×{n}", o.descripcion), sCelda, GUILayout.Width(w * 0.42f));
                GUILayout.Label($"{o.peso * n:0.##} kg", sCelda, GUILayout.Width(70));
                if (o.SeCome) { if (GUILayout.Button("Comer", sBoton, GUILayout.Width(70))) Partida.Comer(o); }
                else GUILayout.Space(74);
                if (GUILayout.Button("Tirar", sBoton, GUILayout.Width(60))) { Partida.Sacar(o); Decir($"Tiraste {o.nombre}."); }
                GUILayout.EndHorizontal();
            }

            var aMano = Catalogo.RecetasEn(LugarReceta.Cualquiera).Where(Partida.Sabe).ToList();
            if (aMano.Count > 0)
            {
                GUILayout.Space(8);
                GUILayout.Label("Lo que sabés hacer", sTitulo);
                foreach (var r in aMano) FilaReceta(r, LugarReceta.Cualquiera, w);
            }
            GUILayout.EndScrollView();

            GUILayout.Label(!string.IsNullOrEmpty(panelMsg) ? panelMsg : !string.IsNullOrEmpty(GUI.tooltip) ? GUI.tooltip : "I o Esc: cerrar", sAyuda);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        void PanelFogon()
        {
            float w = Mathf.Min(560, Ui.Ancho - 32), h = Mathf.Min(400, Ui.Alto - 80);
            GUILayout.BeginArea(new Rect((Ui.Ancho - w) / 2, (Ui.Alto - h) / 2, w, h));
            GUILayout.BeginVertical(sCaja, GUILayout.ExpandHeight(true));
            GUILayout.Label("Junto al fogón", sTitulo);
            scroll = GUILayout.BeginScrollView(scroll);
            var todas = Catalogo.RecetasEn(LugarReceta.Fogon).ToList();
            var sabe = todas.Where(Partida.Sabe).ToList();
            if (sabe.Count == 0) GUILayout.Label("Todavía no sabés cocinar nada.", sTexto);
            foreach (var r in sabe) FilaReceta(r, LugarReceta.Fogon, w);
            int noSabe = todas.Count - sabe.Count;
            if (noSabe > 0) GUILayout.Label($"Hay {noSabe} receta{(noSabe > 1 ? "s" : "")} de fogón que todavía no sabés. Alguien del pago te las puede enseñar.", sAyuda);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUI.enabled = Partida.Cansancio > 0.02f;
            if (GUILayout.Button("Sentarse a descansar", sBoton, GUILayout.Width(180)))
            {
                haciendo = Haciendo.Descansando;
                jug.AnimaOtro = true;
                if (fogonCerca != null) jug.MirarA(fogonCerca.transform.position);
            }
            GUI.enabled = true;
            GUILayout.Label(panelMsg.Length > 0 ? panelMsg : "F o Esc: cerrar", sAyuda);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        void FilaReceta(FichaReceta r, LugarReceta aca, float w)
        {
            bool puede = Recetario.PuedeHacer(r, aca, out var porQue);
            GUILayout.BeginHorizontal();
            string pide = string.Join(" + ", r.ingredientes.Select(i => (i.cantidad > 1 ? i.cantidad + " " : "") + i.Nombre));
            GUILayout.Label(new GUIContent($"{r.nombre}   ({pide})", r.descripcion), sTexto, GUILayout.Width(w * 0.62f));
            GUI.enabled = puede;
            if (GUILayout.Button(puede ? "Hacer" : porQue, sBoton, GUILayout.MinWidth(80))) EmpezarReceta(r, aca);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        void PanelPesca()
        {
            if (lance == null) return;
            string txt = lance.Estado switch
            {
                EstadoLance.Esperando => "Esperando que pique…  (F cuando se hunda el corcho · Esc: recoger)",
                EstadoLance.Pica => "¡Pica! ¡Tirá con F!",
                EstadoLance.Peleando => "Mantené F para recoger, soltá si la línea se tensa demasiado",
                _ => "",
            };
            if (txt.Length > 0) Etiqueta(txt, Ui.Alto - 160);
            if (lance.Estado != EstadoLance.Peleando) return;

            // tensión: franja buena en verde; si llega a la derecha se corta
            float w = 300, x = (Ui.Ancho - w) / 2, y = Ui.Alto - 124;
            var c0 = GUI.color;
            GUI.color = new Color(0.2f, 0.16f, 0.12f, 0.6f);
            GUI.DrawTexture(new Rect(x, y, w, 16), blanco);
            GUI.color = new Color(0.4f, 0.6f, 0.3f, 0.85f);
            GUI.DrawTexture(new Rect(x + w * lance.ZonaMin, y, w * (lance.ZonaMax - lance.ZonaMin), 16), blanco);
            GUI.color = new Color(0.7f, 0.2f, 0.12f, 0.85f);
            GUI.DrawTexture(new Rect(x + w * 0.94f, y, w * 0.06f, 16), blanco);
            GUI.color = lance.Tiron ? new Color(1, 0.85f, 0.4f) : Color.white;
            GUI.DrawTexture(new Rect(x + w * Mathf.Clamp01(lance.Tension) - 2, y - 3, 4, 22), blanco);
            GUI.color = c0;
            GUI.Label(new Rect(x - 70, y - 2, 66, 20), "Tensión", sAyuda);
            Barra(new Rect(x, y + 22, w, 8), lance.Avance, new Color(0.55f, 0.36f, 0.16f));
        }
    }
}
