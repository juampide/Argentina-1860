using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Movimiento del jugador relativo a la cámara isométrica: WASD o flechas para caminar, Shift para correr.
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
        public static System.Func<float, float, bool> AguaEn = MapaPago.EnAgua;

        CharacterController cc;
        ModeloPersonaje modelo;
        float andar, vel, vy;

        /// Mientras hay un diálogo abierto el jugador no se mueve.
        public bool Bloqueado { get; set; }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            modelo = GetComponent<ModeloPersonaje>();
        }

        public void MirarA(Vector3 punto)
        {
            var d = punto - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
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

            var mov = Vector3.zero;
            if (ix != 0 || iz != 0)
            {
                // misma cuenta que el prototipo, pasada a Unity (z al revés)
                float yaw = camara != null ? camara.YawVis : Mathf.PI * 0.75f;
                Vector2 adelante = new Vector2(-Mathf.Cos(yaw), -Mathf.Sin(yaw));
                Vector2 derecha = new Vector2(-adelante.y, adelante.x);
                Vector2 d3 = adelante * iz + derecha * ix;
                var dir = new Vector3(d3.x, 0, -d3.y).normalized;
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
    }
}
