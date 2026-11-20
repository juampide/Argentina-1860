using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Caballo que el jugador puede montar: criollo con recado, aliento para el galope, y vuelve trotando cuando lo
    /// llaman con un silbido. Lo crea <see cref="Caballeriza"/>; no hace falta ponerlo a mano en las escenas.
    /// El transform mira hacia donde mira el caballo (+z). Montado, cuelga del jugador y lo mueve ControlJugador.
    /// </summary>
    public class CaballoMontable : MonoBehaviour
    {
        public string nombre = "Moro";
        public string pelaje = "#5c6168";
        public bool calcetines = true;
        [Tooltip("Velocidad al trote (m/s)")] public float trote = 7f;
        [Tooltip("Velocidad al galope (m/s)")] public float galope = 13f;
        [Tooltip("Segundos de galope con el aliento lleno")] public float segundosGalope = 14f;
        [Tooltip("Segundos para recuperar todo el aliento al trote; quieto, la mitad")] public float segundosRecupera = 12f;

        public enum Estado { Suelto, Viniendo, Montado }
        public Estado Actual { get; private set; } = Estado.Suelto;
        public bool Montado => Actual == Estado.Montado;
        /// 0 a 1. Galopar lo gasta; trotar o estar quieto lo recupera.
        public float Aliento { get; private set; } = 1;
        /// Sin aliento queda cansado y no galopa hasta recuperar un tercio.
        public bool Cansado { get; private set; }

        const float ESCALA = 0.95f, CERCA = 2.6f;
        Bicho b;
        Transform modelo;
        CharacterController cc;
        Transform llamador;
        float t, fase, pasto, vy, vel, chequeo;
        Vector3 posChequeo;
        int atascos;

        public static CaballoMontable Crear(Vector3 pos, float yawGrados, string nombre, string pelaje, bool calcetines)
        {
            var go = new GameObject("Caballo · " + nombre);
            go.SetActive(false);   // para que Awake vea el pelaje antes de armar el modelo
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yawGrados, 0));
            var c = go.AddComponent<CaballoMontable>();
            c.nombre = nombre; c.pelaje = pelaje; c.calcetines = calcetines;
            go.SetActive(true);
            return c;
        }

        void Awake()
        {
            modelo = new GameObject("modelo").transform;
            modelo.SetParent(transform, false);
            modelo.localRotation = Quaternion.Euler(0, 180, 0);   // el modelo del caballo mira hacia -z
            b = ModelosFauna.Caballo(modelo, pelaje, calcetines);
            b.raiz.localScale = Vector3.one * ESCALA;
            Recado(b.raiz);
            cc = gameObject.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0); cc.radius = 0.5f; cc.height = 1.9f;
            cc.stepOffset = 0.35f; cc.slopeLimit = 50; cc.skinWidth = 0.05f;
            t = Random.value * 10;
        }

        /// Recado criollo: pelero de lana, cojinillo de cuero, cabezadas y estribos (coordenadas del modelo del caballo).
        static void Recado(Transform caballo)
        {
            var n = new Nodo("recado");
            n.Pieza(Geo.Caja(0.66f, 0.06f, 0.78f), "#d9ccb0", new Vector3(0, 1.49f, 0.05f));    // pelero
            n.Pieza(Geo.Caja(0.52f, 0.09f, 0.56f), "#4a3220", new Vector3(0, 1.56f, 0.05f));    // cojinillo
            n.Pieza(Geo.Caja(0.4f, 0.1f, 0.07f), "#3a2618", new Vector3(0, 1.63f, 0.33f));      // cabezada delantera
            n.Pieza(Geo.Caja(0.4f, 0.08f, 0.07f), "#3a2618", new Vector3(0, 1.61f, -0.22f));    // cabezada trasera
            foreach (var x in new[] { -0.34f, 0.34f })
            {
                n.Pieza(Geo.Caja(0.03f, 0.42f, 0.05f), "#3a2618", new Vector3(x, 1.3f, 0.08f));  // acción del estribo
                n.Pieza(Geo.Caja(0.1f, 0.03f, 0.13f), "#8a7f6a", new Vector3(x, 1.08f, 0.08f));  // estribo
            }
            n.Instanciar(caballo, null);
        }

        // ---------- montar ----------
        public void AlMontar(Transform jinete)
        {
            Actual = Estado.Montado;
            llamador = null;
            cc.enabled = false;
            transform.SetParent(jinete, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            PonerCapa(jinete.gameObject.layer);   // la silueta del jugador incluye al caballo
        }

        public void AlDesmontar()
        {
            transform.SetParent(null, true);
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            PonerCapa(0);
            modelo.localPosition = Vector3.zero;
            modelo.localRotation = Quaternion.Euler(0, 180, 0);
            cc.enabled = true;
            vel = 0;
            Actual = Estado.Suelto;
        }

        void PonerCapa(int capa)
        {
            foreach (var tr in GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = capa;
        }

        /// Gasta o recupera aliento. Lo llama ControlJugador mientras el caballo va montado.
        public void Esfuerzo(float dt, bool galopando, float velocidad)
        {
            if (galopando) Aliento -= dt / segundosGalope;
            else Aliento += dt / segundosRecupera * (velocidad < 0.5f ? 2f : 1f);
            Aliento = Mathf.Clamp01(Aliento);
            if (Aliento <= 0) Cansado = true;
            else if (Cansado && Aliento > 0.33f) Cansado = false;
        }

        /// Patas, cabeza y vaivén según la velocidad (montado o viniendo).
        public void AnimarAndando(float faseAndar, float velocidad, float dt)
        {
            if (b == null) return;
            t += dt;
            if (velocidad < 0.3f)
            {
                b.Andar(0, 0);
                b.AnimarCaballo(t, 0);
                modelo.localPosition = Vector3.zero;
                modelo.localRotation = Quaternion.Euler(0, 180, 0);
                return;
            }
            float g = Mathf.InverseLerp(trote, galope, velocidad);   // 0 al trote, 1 al galope
            b.Andar(faseAndar, Mathf.Lerp(Mathf.Min(1, velocidad / trote) * 0.55f, 0.95f, g));
            b.AnimarCaballo(t * (2 + g), 0, true);
            float salto = Mathf.Abs(Mathf.Sin(faseAndar)) * Mathf.Lerp(0.05f, 0.1f, g);
            modelo.localPosition = new Vector3(0, salto, 0);
            modelo.localRotation = Quaternion.Euler(Mathf.Sin(faseAndar) * 4f * g, 180, 0);   // galope: hamaca adelante y atrás
        }

        // ---------- suelto: pasta, o viene cuando lo llaman ----------
        /// Silbido: el caballo vuelve trotando hacia el jugador.
        public void Llamar(Transform quien)
        {
            if (Montado) return;
            llamador = quien;
            Actual = Estado.Viniendo;
            atascos = 0; chequeo = 0; posChequeo = transform.position;
        }

        /// Lleva el caballo a un punto sin animación (si estaba lejos o se trabó).
        public void Teletransportar(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
            posChequeo = pos;
        }

        void Update()
        {
            if (Montado || b == null) return;   // b se pierde si Unity recompila en Play
            float dt = Time.deltaTime;
            vy = cc.isGrounded ? -2f : vy - 20f * dt;
            var mov = Vector3.zero;

            if (Actual == Estado.Viniendo && llamador != null)
            {
                var d = llamador.position - transform.position;
                d.y = 0;
                float dist = d.magnitude;
                if (dist < CERCA)
                {
                    Actual = Estado.Suelto;
                    llamador = null;
                }
                else
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 360 * dt);
                    float objetivo = dist > 14 ? galope * 0.8f : Mathf.Max(2.5f, trote * Mathf.Min(1, dist / 8));
                    vel = Mathf.MoveTowards(vel, objetivo, 8 * dt);
                    mov = transform.forward * vel * dt;
                    // si no avanza, se lo trae al lado: mejor que verlo empujando una pared
                    chequeo += dt;
                    if (chequeo > 1.2f)
                    {
                        if (Vector3.Distance(transform.position, posChequeo) < 1f) atascos++;
                        chequeo = 0; posChequeo = transform.position;
                        if (atascos >= 2 && Caballeriza.LugarLibre(llamador.position, 3.5f, out var p)) { Teletransportar(p); atascos = 0; }
                    }
                }
            }
            else vel = Mathf.MoveTowards(vel, 0, 10 * dt);

            cc.Move(mov + Vector3.up * vy * dt);
            if (vel > 0.3f)
            {
                fase += dt * Mathf.Min(vel * 2.2f, 15f);
                AnimarAndando(fase, vel, dt);
            }
            else
            {
                // pasta un rato, levanta la cabeza, vuelve a pastar
                t += dt;
                float objPasto = Mathf.Sin(t * 0.12f + 1.3f) > 0.35f ? 0.45f : 0;
                pasto += (objPasto - pasto) * Mathf.Min(1, dt * 1.5f);
                b.Andar(0, 0);
                b.AnimarCaballo(t, pasto);
                modelo.localPosition = Vector3.zero;
                modelo.localRotation = Quaternion.Euler(0, 180, 0);
            }
        }
    }
}
