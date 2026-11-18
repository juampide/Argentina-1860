using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Edificio con interior: cuando el jugador entra, se bajan las paredes que dan a la cámara y se oculta el techo
    /// (como en el prototipo). Las subclases arman la geometría y registran paredes y partes a ocultar.
    /// Coordenadas locales del prototipo: el frente (puerta) mira hacia +z del prototipo (-Z local de Unity).
    /// </summary>
    public abstract class EdificioConInterior : ProceduralBase
    {
        public abstract float Ancho { get; }
        public abstract float Largo { get; }

        protected class Pared { public Transform t; public float nx, nz, k = 1; }
        protected readonly List<Pared> paredes = new List<Pared>();
        protected readonly List<Transform> ocultar = new List<Transform>();

        /// ¿El punto local (del prototipo) está adentro?
        protected abstract bool Dentro(float lx, float lz);

        public bool JugadorAdentro { get; private set; }
        ControlJugador jugador;
        CamaraIsometrica camara;

        protected static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);

        /// Colisión en coordenadas locales del prototipo (x, z del centro; w a lo largo de x, l a lo largo de z).
        protected void Obst(Transform raiz, float x, float z, float w, float l, float h = 2.4f) =>
            Colision(raiz, Conv.P(x, h / 2, z), new Vector3(w, h, l));

        protected void PrepararListas()
        {
            paredes.Clear();
            ocultar.Clear();
        }

        void Update()
        {
            if (!Application.isPlaying || paredes.Count == 0) return;
            if (jugador == null) jugador = FindAnyObjectByType<ControlJugador>();
            if (camara == null) camara = FindAnyObjectByType<CamaraIsometrica>();
            if (jugador == null) return;
            float dt = Time.deltaTime;
            var p = jugador.transform.position;
            var o = transform.position;
            float rot = -transform.eulerAngles.y * Mathf.Deg2Rad, c = Mathf.Cos(rot), s = Mathf.Sin(rot);
            float dx = p.x - o.x, dz = -p.z + o.z;
            float lx = dx * c - dz * s, lz = dx * s + dz * c;
            bool dentro = Dentro(lx, lz);
            JugadorAdentro = dentro;
            float yaw = camara != null ? camara.YawVis : Mathf.PI * 0.75f;
            float cx = Mathf.Cos(yaw), cz = Mathf.Sin(yaw);
            foreach (var w in paredes)
            {
                float wx = w.nx * c + w.nz * s, wz = -w.nx * s + w.nz * c;
                float obj = dentro && (wx * cx + wz * cz) > 0.25f ? 0.1f : 1;
                w.k += (obj - w.k) * Mathf.Min(1, dt * 7);
                if (w.t != null) w.t.localScale = new Vector3(1, w.k, 1);
            }
            foreach (var t in ocultar) if (t != null && t.gameObject.activeSelf == dentro) t.gameObject.SetActive(!dentro);
        }

        /// Techo a dos aguas igual al de Casa (casa() con sinMuro del prototipo).
        protected static Nodo Techo(Nodo g, float w, float l, float h, float rise, string mTecho)
        {
            if (mTecho == "paja" && rise < 2) rise = 2.2f;
            float lg = Mathf.Max(w, l), an = Mathf.Min(w, l);
            var tm = g.Hijo("techo", v(0, h, 0));
            if (w > l) tm.rot = v(0, Mathf.PI / 2, 0);
            tm.Pieza(Geo.Techo(an, lg, rise, 0.55f), mTecho, Vector3.zero);
            if (mTecho == "paja") tm.Pieza(Geo.Caja(0.16f, 0.16f, lg + 1.9f, 1.5f), "madera", v(0, rise + 0.03f, 0));
            return tm;
        }
    }
}
