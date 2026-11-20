using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Movimiento del jugador relativo a la cámara isométrica: WASD o flechas para caminar, Shift para correr.
    /// A caballo: WASD al trote, Shift al galope (gasta aliento); el caballo acelera, frena y gira de a poco.
    /// No entra al agua del arroyo ni de la laguna, como en el prototipo.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(ModeloPersonaje))]
    public class ControlJugador : MonoBehaviour
    {
        public CamaraIsometrica camara;
        public float velocidad = 4.6f;
        public float velocidadCorrer = 9f;
        [Tooltip("Impide entrar al agua del pago")]
        public bool bloquearAgua = true;

        /// Qué es agua en el mundo actual (x, z del prototipo). Por defecto el pago; la ciudad pone el río del plano.
        public static System.Func<float, float, bool> AguaEn = MapaPago.Bloqueado;

        /// Altura del jinete sobre el piso (el recado queda a la altura de la cadera).
        const float ALTURA_JINETE = 0.8f;

        CharacterController cc;
        ModeloPersonaje modelo;
        float andar, vel, vy;
        // a caballo
        float velMont, faseMont, tMont;
        float ccRadio, ccAlto; Vector3 ccCentro;

        /// Para pruebas automáticas desde el editor (con Unity sin foco el teclado no llega): si tiene valor, reemplaza
        /// a las teclas. x: derecha (+1) o izquierda (-1), y: adelante (+1) o atrás (-1). Dejar en null al terminar.
        public static Vector2? EntradaPrueba;
        public static bool CorrerPrueba;

        /// Mientras hay un diálogo abierto el jugador no se mueve.
        public bool Bloqueado { get; set; }
        /// El caballo que va montando, o null si va a pie.
        public CaballoMontable Montura { get; private set; }
        /// Velocidad actual a caballo (m/s).
        public float VelocidadMontado => velMont;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            modelo = GetComponent<ModeloPersonaje>();
            ccRadio = cc.radius; ccAlto = cc.height; ccCentro = cc.center;
        }

        public void MirarA(Vector3 punto)
        {
            var d = punto - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }

        // ---------- montar y bajarse ----------
        public void Montar(CaballoMontable c)
        {
            if (Montura != null || c == null) return;
            // el jugador se para donde está el caballo, mirando hacia donde mira
            cc.enabled = false;
            transform.SetPositionAndRotation(c.transform.position, Quaternion.Euler(0, c.transform.eulerAngles.y, 0));
            cc.radius = 0.55f; cc.height = 2.7f; cc.center = new Vector3(0, 1.35f, 0);
            cc.enabled = true;
            Montura = c;
            c.AlMontar(transform);
            Modelo().localPosition = new Vector3(0, ALTURA_JINETE, 0);
            velMont = 0;
        }

        /// Se baja y queda parado en <paramref name="dondeBajar"/> (un lugar libre al costado del caballo).
        public void Desmontar(Vector3 dondeBajar)
        {
            if (Montura == null) return;
            var c = Montura;
            Montura = null;
            c.AlDesmontar();
            cc.enabled = false;
            cc.radius = ccRadio; cc.height = ccAlto; cc.center = ccCentro;
            transform.position = dondeBajar;
            cc.enabled = true;
            var m = Modelo(); m.localPosition = Vector3.zero;
            MirarA(c.transform.position);
            vel = 0;
        }

        Transform Modelo()
        {
            var g = transform.Find(ProceduralBase.NOMBRE_GENERADO);
            return g != null ? g : transform;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var kb = Keyboard.current;
            float ix = 0, iz = 0;
            bool corre = false;
            if (!Bloqueado && kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) iz += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) iz -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) ix += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) ix -= 1;
                corre = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }
            if (!Bloqueado && EntradaPrueba is Vector2 ep) { ix = ep.x; iz = ep.y; corre = CorrerPrueba; }

            bool hay = ix != 0 || iz != 0;
            var dir = Vector3.zero;
            if (hay)
            {
                // misma cuenta que el prototipo, pasada a Unity (z al revés)
                float yaw = camara != null ? camara.YawVis : Mathf.PI * 0.75f;
                Vector2 adelante = new Vector2(-Mathf.Cos(yaw), -Mathf.Sin(yaw));
                Vector2 derecha = new Vector2(-adelante.y, adelante.x);
                Vector2 d3 = adelante * iz + derecha * ix;
                dir = new Vector3(d3.x, 0, -d3.y).normalized;
            }

            if (Montura != null) { AlTrote(dt, hay, dir, corre); return; }

            var mov = Vector3.zero;
            if (hay)
            {
                float v = corre ? velocidadCorrer : velocidad;
                mov = dir * v * dt;
                if (bloquearAgua)
                {
                    var p = transform.position;
                    if (AguaEn(p.x + mov.x, -p.z)) mov.x = 0;
                    if (AguaEn(p.x + mov.x, -(p.z + mov.z))) mov.z = 0;
                }
                var giro = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, giro, Mathf.Min(1, dt * 12));
                andar += dt * (corre ? 12 : 8.5f);
                vel = corre ? 1.35f : 1;
            }
            else vel *= Mathf.Pow(0.8f, dt * 60);

            vy = cc.isGrounded ? -2f : vy - 20f * dt;
            cc.Move(mov + Vector3.up * vy * dt);
            modelo.Anim?.Animar(andar, vel);
        }

        /// A caballo: el caballo va siempre hacia adelante y gira hacia donde apuntan las teclas, más despacio cuanto
        /// más rápido va. Acelera y frena de a poco, y el galope gasta aliento.
        void AlTrote(float dt, bool hay, Vector3 dir, bool galopa)
        {
            var c = Montura;
            galopa = galopa && hay && !c.Cansado;
            float objetivo = !hay ? 0 : galopa ? c.galope : c.trote;
            velMont = Mathf.MoveTowards(velMont, objetivo, (objetivo > velMont ? 6f : 11f) * dt);
            if (hay)
            {
                float giroMax = Mathf.Lerp(320f, 130f, velMont / c.galope);   // grados por segundo
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), giroMax * dt);
            }
            var mov = transform.forward * velMont * dt;
            if (bloquearAgua)
            {
                // se mira un poco más adelante: la cabeza del caballo no entra al agua
                var p = transform.position + transform.forward * 1.1f;
                if (AguaEn(p.x + mov.x, -(p.z + mov.z))) { mov = Vector3.zero; velMont *= 0.3f; }
            }
            c.Esfuerzo(dt, galopa && velMont > c.trote + 0.5f, velMont);

            vy = cc.isGrounded ? -2f : vy - 20f * dt;
            cc.Move(mov + Vector3.up * vy * dt);
            // si chocó contra algo, la velocidad baja a la que de verdad logró
            var real = cc.velocity; real.y = 0;
            if (velMont > 1 && real.magnitude < velMont * 0.5f) velMont = Mathf.Max(real.magnitude, velMont * 0.9f);

            faseMont += dt * Mathf.Min(velMont * 2.2f, 15f);
            tMont += dt;
            c.AnimarAndando(faseMont, velMont, dt);
            modelo.Anim?.Jinete(faseMont, velMont / c.trote, Mathf.InverseLerp(c.trote, c.galope, velMont), tMont);
        }
    }
}
