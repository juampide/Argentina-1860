using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Casa de campo armada por código (casa() de campo.js): muro de adobe o encalado, techo de paja o teja a dos aguas,
    /// puerta, ventanas con postigos, galería opcional y estado de ruina. Se cambian los valores y se rearma sola.
    /// El frente (puerta) mira hacia -Z local.
    /// </summary>
    public class Casa : ProceduralBase
    {
        public enum Pared { Adobe, Blanco }
        public enum TipoTecho { Paja, Teja }

        [Header("Medidas (metros)")]
        [Min(1)] public float ancho = 7;
        [Min(1)] public float largo = 5;
        [Min(1)] public float alto = 2.6f;
        [Tooltip("Altura de la cumbrera sobre el muro. Con paja se fuerza a 2,2 m si es menor a 2.")]
        [Min(0.3f)] public float caida = 1.6f;

        [Header("Materiales y forma")]
        public Pared pared = Pared.Adobe;
        public TipoTecho techo = TipoTecho.Paja;
        public bool puerta = true;
        public bool galeria;
        public bool ruina;

        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);

        protected override void Construir(Transform raiz)
        {
            float w = ancho, l = largo, h = alto, rise = caida;
            string mPared = pared == Pared.Adobe ? "adobe" : "blanco";
            string mTecho = techo == TipoTecho.Paja ? "paja" : "teja";
            var n = new Nodo("casa");

            n.Pieza(Geo.Caja(w, h, l, 3), mPared, v(0, h / 2, 0));
            if (pared == Pared.Adobe) Varas(n, w, h, l);

            if (techo == TipoTecho.Paja && rise < 2) rise = 2.2f;
            float lg = Mathf.Max(w, l), an = Mathf.Min(w, l);
            var tm = n.Hijo("techo", v(0, h, 0));
            if (w > l) tm.rot = v(0, Mathf.PI / 2, 0);
            if (ruina)
            {
                tm.esc = v(1, 0.7f, 0.55f);
                tm.pos = v(0, h, lg * 0.18f);
                tm.rot = v(tm.rot.x, tm.rot.y, 0.08f);
            }
            tm.Pieza(Geo.Techo(an, lg, rise, 0.55f), mTecho, Vector3.zero);
            if (techo == TipoTecho.Paja) tm.Pieza(Geo.Caja(0.16f, 0.16f, lg + 1.9f, 1.5f), "madera", v(0, rise + 0.03f, 0));

            if (puerta) n.Pieza(Geo.Caja(0.95f, 1.9f, 0.14f, 1.5f), "oscuro", v(0, 0.95f, l / 2 + 0.04f));
            if (w > 5)
                foreach (var sx in new[] { -1f, 1f })
                {
                    n.Pieza(Geo.Caja(0.8f, 0.9f, 0.12f, 1.5f), "oscuro", v(sx * w * 0.3f, 1.5f, l / 2 + 0.04f));
                    n.Pieza(Geo.Caja(0.2f, 1.0f, 0.16f, 1.5f), "postigo", v(sx * w * 0.3f - 0.5f, 1.5f, l / 2 + 0.06f));
                    n.Pieza(Geo.Caja(0.2f, 1.0f, 0.16f, 1.5f), "postigo", v(sx * w * 0.3f + 0.5f, 1.5f, l / 2 + 0.06f));
                }

            if (galeria)
            {
                float gz = l / 2 + 1.9f;
                int nP = Mathf.Max(1, Conv.RoundJS(w / 2.6f));
                for (int k = 0; k <= nP; k++)
                {
                    float px = -w / 2 + k * (w / nP);
                    n.Pieza(Geo.Cilindro(0.1f, 0.12f, h + 0.2f, 6), "madera", v(px, (h + 0.2f) / 2, gz));
                }
                n.Pieza(Geo.Caja(w + 0.8f, 0.14f, 2.8f, 2), "madera", v(0, h + 0.15f, l / 2 + 1.3f), v(0.1f, 0, 0));
            }

            n.Instanciar(raiz, Registrar);
            Colision(raiz, new Vector3(0, h / 2, 0), new Vector3(w + 0.4f, h, l + 0.4f));
        }

        // varas de madera que atan el adobe y postes en las esquinas (como en las fotos de época)
        static void Varas(Nodo g, float w, float h, float l)
        {
            const string m = "vara";
            foreach (var y in new[] { 0.75f, 1.65f })
            {
                if (y > h - 0.3f) continue;
                foreach (var sz in new[] { -1f, 1f }) g.Pieza(Geo.Caja(w + 0.12f, 0.05f, 0.05f), m, v(0, y, sz * (l / 2 + 0.03f)));
                foreach (var sx in new[] { -1f, 1f }) g.Pieza(Geo.Caja(0.05f, 0.05f, l + 0.12f), m, v(sx * (w / 2 + 0.03f), y, 0));
            }
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    g.Pieza(Geo.Caja(0.13f, h, 0.13f), m, v(sx * (w / 2 + 0.02f), h / 2, sz * (l / 2 + 0.02f)));
        }
    }
}
