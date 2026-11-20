using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// P1 · La llegada (Docs/guion/misiones.md, "P1 en detalle"). Al atardecer, el Gallego llega sentado en la carreta
    /// de Juan Maidana hasta el portón del fortín; la carreta se vuelve, Obregón le revela que su tierra está del otro
    /// lado de la frontera, y Santos lo lleva de noche, con el farol, hasta el rancho de peones. Duerme y amanece:
    /// empieza el juego libre.
    /// Va en el objeto Juego, al lado de JuegoPago. Con "jugar" apagado, el juego arranca como antes.
    /// Backspace saltea la llegada y deja al Gallego en la mañana siguiente.
    /// </summary>
    public class LlegadaP1 : MonoBehaviour
    {
        [Tooltip("Al darle Play arranca con la llegada en carreta. Apagado, el juego empieza directo en el pago.")]
        public bool jugar = true;
        [Tooltip("Metros por segundo de la carreta")]
        public float velocidadCarreta = 2.2f;
        [Tooltip("Metros por segundo de Santos con el farol (0: al paso del Gallego)")]
        public float velocidadSantos = 0;
        [Tooltip("Metros que se camina detrás de Santos antes del fundido a la mañana siguiente (el rancho queda a unos 490 m)")]
        public float metrosCaminata = 120;
        [Tooltip("Ficha de Don Rufino, por si no está en la escena: entonces se arma uno para la mañana y después se borra")]
        public FichaPersonaje fichaRufino;

        // ---------- lugares (x, z de Unity) ----------
        static readonly Vector2[] SENDA_CARRETA = { new Vector2(-1450, -160), new Vector2(-1470, -50), new Vector2(-1475, 0), new Vector2(-1452, 28), new Vector2(-1450, 50) };
        static readonly Vector2[] SENDA_SANTOS = { new Vector2(-1452, 50), new Vector2(-1300, 120), new Vector2(-1150, 220), new Vector2(-1020, 283) };
        static readonly Vector2 SANTOS_PORTON = new Vector2(-1449, 44);
        static readonly Vector2 RANCHO = new Vector2(-1018, 300);
        static readonly Vector2 PUERTA_RANCHO = new Vector2(-1016, 291);
        const float HORA_LLEGADA = 18f, HORA_PORTON = 18.7f, HORA_SALIDA = 19.4f, HORA_RANCHO = 20.7f, HORA_ALBA = 6.25f;

        enum Paso { Carreta, Despedida, Fortin, Santos, Caminata, Manana, Listo }
        Paso paso = Paso.Listo;

        JuegoPago juego;
        Ambiente ambiente;
        ControlJugador jugador;
        CharacterController cc;
        NPC juan, obregon, santos, rufino;
        bool rufinoDeLaLlegada;   // lo creó la llegada: se borra cuando se va
        Vector3 rufinoPos; Quaternion rufinoRot;   // el de la escena vuelve a su lugar
        Vector3 santosPos; Quaternion santosRot;
        Farol farolSantos;
        Bicho carreta;
        Habitacion rancho;
        GameObject carretaGo;
        float fundido;            // 0 a 1: negro del paso a la mañana
        string rotulo;            // "La mañana siguiente", sobre el negro
        float avisoSantos;
        GUIStyle sRotulo;
        Texture2D negro;

        public static bool EnCurso { get; private set; }

        // La llegada se juega una sola vez y solo si el Play arrancó en esta escena: no al volver de Buenos Aires
        // ni cuando los tests cargan el pago.
        static string primeraEscena;
        static bool yaJugada;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AlArrancar() { primeraEscena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path; yaJugada = false; }

        const string TEXTO_MANANA = "Amaneció. Anselmo reparte las tareas en los corrales de la estancia. Y el juez de paz, Don Rufino, va a querer verte por lo de tus papeles.";
        const string TEXTO_MANANA_RUFINO = "Don Rufino se quedó con tu título y tu pasaporte, «para hacer el reclamo». Mientras tanto, a trabajar: Anselmo reparte las tareas en los corrales de La Soledad.";

        void Start()
        {
            if (!jugar || !Application.isPlaying || yaJugada || primeraEscena != gameObject.scene.path) { enabled = false; return; }
            yaJugada = true;
            juego = GetComponent<JuegoPago>();
            ambiente = FindAnyObjectByType<Ambiente>();
            jugador = juego != null ? juego.jugador : FindAnyObjectByType<ControlJugador>();
            if (juego == null || jugador == null || ambiente == null) { Debug.LogWarning("LlegadaP1: faltan JuegoPago, el jugador o el Ambiente."); enabled = false; return; }
            cc = jugador.GetComponent<CharacterController>();
            foreach (var n in FindObjectsByType<NPC>())
            {
                if (n.Ficha == null) continue;
                if (n.Ficha.id == "pascual") juan = n;
                else if (n.Ficha.id == "obregon") obregon = n;
                else if (n.Ficha.id == "nicasio") santos = n;
            }
            if (juan == null || obregon == null || santos == null) { Debug.LogWarning("LlegadaP1: faltan Juan Maidana, Obregón o Santos en la escena."); enabled = false; return; }
            santosPos = santos.transform.position; santosRot = santos.transform.rotation;
            EnCurso = true;
            JuegoPago.DialogoMision = DialogoMision;
            StartCoroutine(Llegada());
        }

        void OnDestroy()
        {
            if (EnCurso) { JuegoPago.DialogoMision = null; Fauna.PascualPrestado = false; EnCurso = false; }
            if (negro != null) Destroy(negro);
        }

        // ---------- la escena ----------
        IEnumerator Llegada()
        {
            ambiente.detenerHora = true;
            ambiente.hora = HORA_LLEGADA;
            juego.Mision("Etapa 1 · La llegada", "En la carreta de Juan Maidana, camino al fortín de la frontera.");

            // 1. la carreta
            paso = Paso.Carreta;
            Fauna.PascualPrestado = true;
            carretaGo = new GameObject("Carreta de Juan Maidana (llegada)");   // solo en Play: se borra sola al salir
            carreta = ModelosFauna.Carreta(carretaGo.transform);
            jugador.enabled = false;
            cc.enabled = false;
            jugador.AnimaOtro = true;
            if (juego.camara != null) juego.camara.VistaForzada = 8.5f;   // más cerca, para ver la carreta

            float largo = Largo(SENDA_CARRETA), s = 0, fase = 0, t = 0;
            var dichos = new (float en, string l)[]
            {
                (0.06f, "Juan Maidana: ¿Ve aquellos palos paraos, Gallego? El fortín. Más al sur ya no hay estancias."),
                (0.38f, "Juan Maidana: Pregunte por el teniente Obregón. Es seco, pero derecho."),
                (0.7f, "Juan Maidana: Yo lo dejo en la puerta y me vuelvo, que tengo que hacer noche en la posta."),
            };
            int dicho = 0;
            while (s < largo)
            {
                float dt = Time.deltaTime;
                // arranca y frena despacio
                float v = velocidadCarreta * Mathf.Clamp(Mathf.Min(t / 3f, (largo - s) / 6f), 0.15f, 1);
                s = Mathf.Min(largo, s + v * dt); t += dt; fase += dt * v * 1.4f;
                ambiente.hora = Mathf.Lerp(HORA_LLEGADA, HORA_PORTON, s / largo);
                var (p, fwd) = Punto(SENDA_CARRETA, s);
                MoverCarreta(p, fwd, fase, 1);
                SentarJugador(p, fwd, t);
                if (dicho < dichos.Length && s / largo >= dichos[dicho].en) juego.Decir(dichos[dicho++].l);
                yield return null;
            }

            // 2. la despedida: el Gallego baja y Juan Maidana se vuelve
            paso = Paso.Despedida;
            var (fin, dirFin) = Punto(SENDA_CARRETA, largo);
            var derecha = new Vector2(dirFin.y, -dirFin.x);
            Bajar(fin - derecha * 3f - dirFin * 2.5f, new Vector3(juan.transform.position.x, 0, juan.transform.position.z));
            yield return new WaitForSeconds(0.6f);
            juego.Hablar(juan);
            while (juego.EnDialogo) { MoverCarreta(fin, dirFin, fase, 0); yield return null; }

            juego.Mision("Etapa 1 · La llegada", "Presentate ante el comandante del fortín, el teniente Obregón.");
            StartCoroutine(Irse(fin, dirFin, fase));

            // 3. Obregón (el jugador va solo hasta él)
            paso = Paso.Fortin;
            while (paso == Paso.Fortin) yield return null;

            // 4. Santos espera en el portón con el farol
            ambiente.hora = 19f;
            PonerSantosEnElPorton();
            juego.Mision("Etapa 1 · La llegada", "Santos, el peón de La Soledad, te espera en el portón del fortín.");
            while (paso == Paso.Santos) yield return null;

            // 5. la caminata: un tramo detrás del farol de Santos
            juego.Mision("Etapa 1 · La llegada", "Seguí a Santos hasta el rancho de peones. Pegate al farol.");
            yield return Caminata();

            // 6. fundido a negro: la mañana siguiente, con Don Rufino en la puerta del rancho
            yield return LaManana();
        }

        /// La carreta da la vuelta en un arco abierto, hacia el lado contrario al fortín (los bueyes no giran en el
        /// lugar), y se va por donde vino; lejos del jugador, desaparece y Juan Maidana vuelve a su camino.
        IEnumerator Irse(Vector2 p, Vector2 dir, float fase)
        {
            const float R = 9;   // radio de la vuelta
            var der = new Vector2(dir.y, -dir.x);
            var centro = p + der * R;
            var puntos = new List<Vector2>();
            for (int i = 0; i <= 16; i++)
            {
                float a = Mathf.PI * i / 16;
                puntos.Add(centro - der * R * Mathf.Cos(a) + dir * R * Mathf.Sin(a));
            }
            // derecho un trecho y después de vuelta por la senda hacia el camino real
            puntos.Add(p + der * 2 * R - dir * 25);
            puntos.Add(SENDA_CARRETA[1]);
            puntos.Add(SENDA_CARRETA[0]);
            var senda = puntos.ToArray();
            float largo = Largo(senda), s = 0, t = 0;
            yield return new WaitForSeconds(1f);
            while (s < largo && carretaGo != null)
            {
                float dt = Time.deltaTime;
                float v = velocidadCarreta * Mathf.Clamp(t / 4f, 0.15f, 1) * 0.8f;
                s += v * dt; t += dt; fase += dt * v * 1.4f;
                var (q, f) = Punto(senda, s);
                MoverCarreta(q, f, fase, 1);
                if (Vector2.Distance(q, Plano(jugador.transform.position)) > 140) break;
                yield return null;
            }
            QuitarCarreta();
        }

        IEnumerator Caminata()
        {
            paso = Paso.Caminata;
            santos.ControladoAfuera = true;
            float largo = Mathf.Min(Largo(SENDA_SANTOS), metrosCaminata), s = 0, fase = 0;
            var dichos = new (float en, string l)[]
            {
                (0.03f, "Santos: ¿Oís los teros? Gritan si pasa alguien. Son los mejores perros que tiene el pago."),
                (0.36f, "Santos: Aquella luz es La Soledad, la estancia. El patrón es Don Vicente, pero en los corrales manda Anselmo, el capataz."),
                (0.7f, "Santos: De noche no se anda sin farol. Si te perdés, buscá un fogón, que siempre hay algún cristiano."),
            };
            int dicho = 0;
            avisoSantos = 0;
            while (s < largo)
            {
                float dt = Time.deltaTime;
                var st = santos.transform;
                float lejos = Vector2.Distance(Plano(jugador.transform.position), Plano(st.position));
                var anim = santos.Modelo.Anim;
                if (avisoSantos > 0) avisoSantos -= dt;
                if (lejos > 9)
                {
                    // lo espera mirándolo, y lo llama
                    var d = jugador.transform.position - st.position; d.y = 0;
                    if (d.sqrMagnitude > 0.01f) st.rotation = Quaternion.Slerp(st.rotation, Quaternion.LookRotation(d), Mathf.Min(1, dt * 4));
                    fase += dt * 1.2f;
                    anim?.Animar(fase, 0);
                    if (lejos > 14 && avisoSantos <= 0) { juego.Decir("Santos: ¡Eh, Gallego! Pegate al farol."); avisoSantos = 9; }
                }
                else
                {
                    s = Mathf.Min(largo, s + (velocidadSantos > 0 ? velocidadSantos : jugador.velocidad) * dt);
                    var (p, f) = Punto(SENDA_SANTOS, s);
                    st.position = Suelo(p);
                    st.rotation = Quaternion.Slerp(st.rotation, Quaternion.LookRotation(new Vector3(f.x, 0, f.y)), Mathf.Min(1, dt * 5));
                    fase += dt * 8.5f;
                    anim?.Animar(fase, 1);
                    if (dicho < dichos.Length && s / largo >= dichos[dicho].en) juego.Decir(dichos[dicho++].l);
                }
                ambiente.hora = Mathf.Lerp(HORA_SALIDA, HORA_RANCHO, s / largo);
                yield return null;
            }
            // que termine de leerse la última línea antes del fundido
            yield return new WaitForSeconds(2.5f);
        }

        /// Fundido a negro y "La mañana siguiente". El Gallego despierta en el catre del rancho de peones, golpean la
        /// puerta, se levanta y sale: afuera lo espera Don Rufino.
        IEnumerator LaManana()
        {
            paso = Paso.Manana;
            jugador.Bloqueado = true;
            while (fundido < 1) { fundido = Mathf.Min(1, fundido + Time.deltaTime / 1.5f); yield return null; }
            rotulo = "La mañana siguiente";
            Despejar(enCama: true);
            juego.Mision("Etapa 1 · La llegada", "Amanece en el rancho de peones de La Soledad.");
            var r = PonerRufino();
            if (juego.camara != null) juego.camara.VistaForzada = 6.5f;
            yield return new WaitForSeconds(2.8f);
            while (fundido > 0)
            {
                fundido = Mathf.Max(0, fundido - Time.deltaTime / 2.5f);
                if (fundido < 0.6f) rotulo = null;
                yield return null;
            }

            // golpean la puerta
            yield return new WaitForSeconds(1.2f);
            juego.Decir("(Golpean la puerta del rancho.)");
            yield return new WaitForSeconds(2.2f);
            juego.Decir("Desde afuera: —¡Ave María Purísima! ¿Hay alguien en el rancho?");
            yield return new WaitForSeconds(1.5f);
            yield return Levantarse();
            if (juego.camara != null) juego.camara.VistaForzada = null;
            jugador.Bloqueado = false;
            if (r == null) { Terminar(TEXTO_MANANA); yield break; }
            juego.Mision("Etapa 1 · La llegada", "Alguien llama a la puerta del rancho. Salí a ver quién es.");

            // cuando sale y se acerca, Rufino lo saluda (o antes, si el jugador le habla)
            while (!juego.EnDialogo && Vector2.Distance(Plano(jugador.transform.position), Plano(r.transform.position)) > 3.6f)
            {
                r.Actualizar(Time.deltaTime, jugador.transform.position);
                yield return null;
            }
            if (!juego.EnDialogo) juego.Hablar(r);
            while (juego.EnDialogo) { r.Actualizar(Time.deltaTime, jugador.transform.position); yield return null; }
            Terminar(TEXTO_MANANA_RUFINO);

            // Rufino se va por el camino, con los papeles en la levita (después vuelve a su lugar de siempre)
            r.ControladoAfuera = true;
            var hacia = new Vector3(1, 0, -0.35f).normalized;
            float fase = 0;
            for (float t = 0; t < 14 && r != null; t += Time.deltaTime)
            {
                var p = r.transform.position + hacia * 1.3f * Time.deltaTime;
                r.transform.SetPositionAndRotation(Suelo(p), Quaternion.Slerp(r.transform.rotation, Quaternion.LookRotation(hacia), Mathf.Min(1, Time.deltaTime * 4)));
                fase += Time.deltaTime * 6.5f;
                r.Modelo.Anim?.Animar(fase, 0.85f);
                yield return null;
            }
            DevolverRufino();
        }

        /// El Gallego, dormido boca arriba en el catre del rancho (la almohada queda hacia +x de la Habitacion).
        void AcostarEnCatre()
        {
            FrenteRancho(0);
            if (rancho == null) { Bajar(Plano(FrenteRancho(0.9f)), FrenteRancho(6)); return; }
            jugador.enabled = false;
            cc.enabled = false;
            jugador.AnimaOtro = true;
            var t = rancho.transform;
            jugador.Personaje.Anim?.Animar(0, 0);
            jugador.transform.SetPositionAndRotation(t.TransformPoint(1.32f, 0.66f, 1.35f), Quaternion.LookRotation(Vector3.up, t.right));
        }

        /// Se sienta, se para al lado del catre y queda mirando la puerta, ya con el control del jugador.
        IEnumerator Levantarse()
        {
            if (rancho == null) yield break;
            var t = rancho.transform;
            Vector3 p0 = jugador.transform.position, p1 = t.TransformPoint(2.0f, 0.02f, 0.4f);
            Quaternion r0 = jugador.transform.rotation, r1 = Quaternion.LookRotation(-t.forward);
            for (float u = 0; u < 1; u += Time.deltaTime / 1.4f)
            {
                float k = Mathf.SmoothStep(0, 1, u);
                jugador.transform.SetPositionAndRotation(Vector3.Lerp(p0, p1, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.25f, Quaternion.Slerp(r0, r1, k));
                yield return null;
            }
            Bajar(Plano(p1), FrenteRancho(2));
        }

        void DevolverRufino()
        {
            if (rufino == null) return;
            if (rufinoDeLaLlegada) { Destroy(rufino.gameObject); rufino = null; return; }
            rufino.transform.SetPositionAndRotation(rufinoPos, rufinoRot);
            rufino.ControladoAfuera = false;
        }

        /// Don Rufino en la puerta del rancho: el de la escena si ya tiene uno, o uno que se arma con su ficha.
        NPC PonerRufino()
        {
            // la ficha de Rufino conserva el id viejo ("remigio")
            foreach (var n in FindObjectsByType<NPC>()) if (n.Ficha != null && (n.Ficha.id == "rufino" || n.Ficha.id == "remigio")) rufino = n;
            if (rufino != null) { rufinoPos = rufino.transform.position; rufinoRot = rufino.transform.rotation; }
            if (rufino == null)
            {
                if (fichaRufino == null) { Debug.LogWarning("LlegadaP1: falta la ficha de Don Rufino; la mañana arranca sin él."); return null; }
                var go = new GameObject("Don Rufino (llegada)");
                int capa = LayerMask.NameToLayer("Personaje");
                if (capa >= 0) go.layer = capa;
                var m = go.AddComponent<ModeloPersonaje>();
                m.ficha = fichaRufino;
                var cap = go.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0, 0.9f, 0); cap.radius = 0.4f; cap.height = 1.8f;
                rufino = go.AddComponent<NPC>();
                rufinoDeLaLlegada = true;
            }
            rufino.ControladoAfuera = false;
            var donde = FrenteRancho(3.1f, 0.35f);
            var mira = jugador.transform.position - donde; mira.y = 0;
            rufino.transform.SetPositionAndRotation(donde, Quaternion.LookRotation(mira));
            return rufino;
        }

        // ---------- diálogos de la llegada ----------
        (string[] l, System.Action fin)? DialogoMision(NPC n)
        {
            if (n == juan && paso == Paso.Despedida)
                return (new[]
                {
                    "Hasta acá llegamos, Gallego. Tome su atao.",
                    "Gallego: ¿Y no me esperas? Mañana he de ir a ver mis tierras.",
                    "¿Esperarlo? Ni loco hago noche en la frontera. Suerte con esas tierras… y que Dios lo ayude.",
                }, null);
            if (n == obregon && paso == Paso.Fortin)
                return (new[]
                {
                    "Teniente Obregón, comandante de este fortín. ¿Y usted quién es, que llega a estas horas y de a pie?",
                    "Gallego: Vengo de España, señor teniente. Compré tierra a la Agencia de Emigración Trasatlántica. Aquí traigo el título: me dijeron que preguntase en el fortín.",
                    "(Obregón despliega el papel y lo mira un rato largo, con la poca luz que queda.)",
                    "El campo que le han marcado queda diez leguas al sur. Del otro lado del cañadón. Del otro lado de la frontera.",
                    "Allí no vive ningún cristiano, señor. Eso es Tierra Adentro: tierra de indios.",
                    "Gallego: Pero si lo he pagado. Lo he pagado todo: el pasaje, la tierra, los papeles…",
                    "No es usted el primero que me trae un papel así. Y yo nada puedo hacer: tengo veinte hombres mal comidos para cuidar diez leguas de frontera.",
                    "Mañana pasa el juez de paz, Don Rufino Aramburu. Los reclamos son cosa suya.",
                    "Esta noche no puedo tenerlo en el fortín. En La Soledad, la estancia de Larralde, siempre precisan un peón, y justo anda por acá el muchacho que trajo la carne. Él lo lleva.",
                }, () => paso = Paso.Santos);
            if (n == obregon && paso > Paso.Fortin && paso < Paso.Listo)
                return (new[] { "Ya se lo dije, señor: los reclamos, con el juez. Vaya con Santos, que se hace de noche." }, null);
            if (n == santos && paso == Paso.Santos)
                return (new[]
                {
                    "Santos Sosa, pa' servirle. Así que vos sos el gallego que trajo Maidana.",
                    "Gallego: No soy gallego, hombre. Soy de Castilla.",
                    "Acá todo el que viene de España es gallego, amigo. Ya te vas a acostumbrar.",
                    "Vamos, que se viene la noche. El rancho de los peones queda pa' allá, cruzando el campo. Pegate al farol, que de noche el campo engaña.",
                }, () => paso = Paso.Caminata);
            if (n == santos && paso == Paso.Caminata)
                return (new[] { "Vamos, Gallego, que se nos hace tarde." }, null);
            if (n == rufino && paso == Paso.Manana)
                return (new[]
                {
                    "(Afuera espera un señor de levita y galera, sonriente, con las botas bien lustradas.)",
                    "Gallego: Sin pecado concebida. Buenos días, señor.",
                    "¡Buenos días, buenos días! Rufino Aramburu, juez de paz del partido, para servir a usted. ¿Es usted el español que llegó anoche al fortín?",
                    "Gallego: El mismo. El teniente me dijo que los reclamos eran cosa de usted.",
                    "Y le dijo bien. Ya estoy al tanto de lo de su tierra. Una lástima, una verdadera lástima. Esas agencias de Buenos Aires… no es usted el primero, créame.",
                    "Pero no se me desanime. Paso a decirle cómo es el trámite: usted me confía el título, y el pasaporte también, que sin papeles no hay reclamo que valga. Yo me ocupo de elevarlo a Buenos Aires.",
                    "Gallego: ¿El pasaporte también? Sin él no soy nadie en esta tierra.",
                    "Es el trámite, amigo. En mis manos está más seguro que en un rancho de peones. Y mientras se resuelve, trabaje: en La Soledad siempre precisan brazos. Eso sí, consiga su papeleta de conchabo, que sin ella la partida no pregunta dos veces.",
                    "(Don Rufino guarda los papeles en la levita, se toca el ala de la galera y se va por el camino.)",
                }, null);
            if (paso == Paso.Fortin && n != obregon && Vector3.Distance(n.transform.position, obregon.transform.position) < 60)
                return (new[] { "El teniente está allá, junto al mástil. Hable con él nomás." }, null);
            return null;
        }

        // ---------- cada cuadro ----------
        void Update()
        {
            var kb = Keyboard.current;
            if (paso != Paso.Listo && kb != null && kb.backspaceKey.wasPressedThisFrame) Saltear();
        }

        /// Deja el mundo como a la mañana siguiente: el Gallego en la puerta del rancho, a las 6:15.
        void Despejar(bool enCama = false)
        {
            QuitarCarreta();
            Fauna.PascualPrestado = false;
            if (farolSantos != null) Destroy(farolSantos.gameObject);
            santos.ControladoAfuera = false;
            santos.transform.SetPositionAndRotation(Suelo(santosPos), santosRot);
            if (enCama) AcostarEnCatre();
            else
            {
                // el Gallego, en la puerta del rancho, mirando hacia afuera
                var umbral = Plano(FrenteRancho(0.9f));
                if (!jugador.enabled) Bajar(umbral, FrenteRancho(6));
                else Teletransportar(umbral);
                jugador.MirarA(FrenteRancho(6));
            }
            ambiente.hora = HORA_ALBA;
        }

        /// Termina la llegada: el reloj vuelve a andar y el pago queda libre.
        void Terminar(string texto)
        {
            ambiente.detenerHora = false;
            juego.Mision("Etapa 1 · El pago", texto);
            JuegoPago.DialogoMision = null;
            EnCurso = false;
            paso = Paso.Listo;
        }

        /// Saltea la llegada (Backspace) y deja al Gallego en la mañana siguiente. Público para las pruebas.
        public void Saltear()
        {
            if (paso == Paso.Listo) return;
            StopAllCoroutines();
            fundido = 0;
            rotulo = null;
            jugador.Bloqueado = false;
            if (juego.camara != null) juego.camara.VistaForzada = null;
            Despejar();
            Terminar(TEXTO_MANANA);
            DevolverRufino();
            juego.Decir("Llegada salteada: amanece en el rancho de peones.");
        }

        // ---------- piezas ----------
        void MoverCarreta(Vector2 p, Vector2 fwd, float fase, float paso01)
        {
            if (carreta == null) return;
            var r = carreta.raiz;
            r.position = Suelo(p);
            r.rotation = Quaternion.LookRotation(-new Vector3(fwd.x, 0, fwd.y));   // el modelo mira hacia -z de Unity
            foreach (var w in carreta.ruedas) w.localRotation = Conv.Rot(fase, 0, 0);
            foreach (var b in carreta.bueyes) b.Bailar(Mathf.Sin(fase * 2) * 0.5f * paso01);
            // Juan Maidana la maneja sentado adelante, con la picana
            Carrero.Ubicar(juan.transform, juan.Modelo.Anim, r, fase, paso01 > 0.1f);
        }

        /// El Gallego sentado en la cola de la carreta, mirando hacia atrás, con las piernas colgando.
        void SentarJugador(Vector2 p, Vector2 fwd, float t)
        {
            var q = p - fwd * 2.0f;
            var pos = Suelo(q) + Vector3.up * 1.18f;
            jugador.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-new Vector3(fwd.x, 0, fwd.y)));
            jugador.Personaje.Anim?.Sentado(t, 0.15f, Mathf.Sin(t * 0.21f) * 0.6f);
        }

        void Bajar(Vector2 donde, Vector3 mirarA)
        {
            jugador.transform.position = Suelo(donde) + Vector3.up * 0.05f;
            cc.enabled = true;
            jugador.enabled = true;
            jugador.AnimaOtro = false;
            jugador.MirarA(mirarA);
            if (juego.camara != null) juego.camara.VistaForzada = null;
        }

        void Teletransportar(Vector2 donde)
        {
            cc.enabled = false;
            jugador.transform.position = Suelo(donde) + Vector3.up * 0.05f;
            cc.enabled = true;
        }

        void PonerSantosEnElPorton()
        {
            santos.ControladoAfuera = false;
            santos.transform.position = Suelo(SANTOS_PORTON);
            santos.transform.rotation = Quaternion.LookRotation(jugador.transform.position - santos.transform.position, Vector3.up);
            var codos = santos.Modelo.Anim != null ? santos.Modelo.Anim.codos : null;
            if (codos != null && codos.Count > 0 && farolSantos == null) farolSantos = Farol.Colgar(codos[0], soloDeNoche: false);
        }

        void QuitarCarreta()
        {
            if (carretaGo != null) Destroy(carretaGo);
            carretaGo = null; carreta = null;
            Fauna.PascualPrestado = false;
        }

        /// Un punto frente a la puerta del rancho de peones, a <paramref name="afuera"/> metros de la pared del frente.
        /// (La puerta de Habitacion está en el medio del frente, hacia -Z local.)
        Vector3 FrenteRancho(float afuera, float costado = 0)
        {
            if (rancho == null)
            {
                float mejor = 30;
                foreach (var h in FindObjectsByType<Habitacion>())
                {
                    float d = Vector2.Distance(Plano(h.transform.position), RANCHO);
                    if (d < mejor) { mejor = d; rancho = h; }
                }
            }
            if (rancho == null) return Suelo(PUERTA_RANCHO);
            return Suelo(rancho.transform.TransformPoint(costado, 0, -(rancho.Largo / 2 + afuera)));
        }

        static Vector2 Plano(Vector3 p) => new Vector2(p.x, p.z);
        static Vector3 Suelo(Vector2 p) { var v = new Vector3(p.x, 0, p.y); v.y = Terreno.Altura(v); return v; }
        static Vector3 Suelo(Vector3 p) => Suelo(Plano(p));

        static float Largo(Vector2[] senda)
        {
            float l = 0;
            for (int i = 1; i < senda.Length; i++) l += Vector2.Distance(senda[i - 1], senda[i]);
            return l;
        }

        /// Punto y dirección a s metros del comienzo de la senda (la dirección se suaviza en las esquinas).
        static (Vector2 p, Vector2 dir) Punto(Vector2[] senda, float s)
        {
            for (int i = 1; i < senda.Length; i++)
            {
                float l = Vector2.Distance(senda[i - 1], senda[i]);
                if (s <= l || i == senda.Length - 1)
                {
                    float u = l > 0 ? Mathf.Clamp01(s / l) : 1;
                    var d = (senda[i] - senda[i - 1]).normalized;
                    // cerca del final del tramo, la dirección va girando hacia la del tramo siguiente
                    if (i + 1 < senda.Length && l - s < 8)
                        d = Vector2.Lerp(d, (senda[i + 1] - senda[i]).normalized, 0.5f * (1 - (l - s) / 8)).normalized;
                    if (i > 1 && s < 8)
                        d = Vector2.Lerp((senda[i - 1] - senda[i - 2]).normalized, d, 0.5f + 0.5f * s / 8).normalized;
                    return (Vector2.Lerp(senda[i - 1], senda[i], u), d);
                }
                s -= l;
            }
            return (senda[senda.Length - 1], (senda[senda.Length - 1] - senda[senda.Length - 2]).normalized);
        }

        // ---------- pantalla ----------
        void OnGUI()
        {
            GUI.depth = -10;   // el negro va por encima del resto de la interfaz
            if (fundido > 0)
            {
                if (negro == null) { negro = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave }; negro.SetPixel(0, 0, Color.black); negro.Apply(); }
                var c0 = GUI.color;
                GUI.color = new Color(1, 1, 1, fundido);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), negro);
                GUI.color = c0;
            }
            if (!string.IsNullOrEmpty(rotulo))
            {
                if (sRotulo == null)
                {
                    sRotulo = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
                    sRotulo.normal.textColor = new Color(0.93f, 0.89f, 0.8f);
                }
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), rotulo, sRotulo);
            }
        }
    }
}
