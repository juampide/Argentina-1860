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

        /// Material de las ventanas: se encienden de noche (si no existe en MaterialesMundo, se usa el oscuro).
        public static string Ventana() => MaterialesMundo.Instancia != null && MaterialesMundo.Instancia.Buscar("ventana") != null ? "ventana" : "oscuro";

        protected override void Construir(Transform raiz)
        {
            float w = ancho, l = largo, h = alto, rise = caida;
            string mPared = pared == Pared.Adobe ? "adobe" : "blanco";
            string mTecho = techo == TipoTecho.Paja ? "paja" : "teja";
            if (ruina) { Ruina(raiz, w, l, h, Mathf.Max(rise, 1.6f), mPared, mTecho); return; }
            var n = new Nodo("casa");

            n.Pieza(Geo.Caja(w, h, l, 3), mPared, v(0, h / 2, 0));
            if (pared == Pared.Adobe) Varas(n, w, h, l);

            if (techo == TipoTecho.Paja && rise < 2) rise = 2.2f;
            float lg = Mathf.Max(w, l), an = Mathf.Min(w, l);
            var tm = n.Hijo("techo", v(0, h, 0));
            if (w > l) tm.rot = v(0, Mathf.PI / 2, 0);
            tm.Pieza(Geo.Techo(an, lg, rise, 0.55f), mTecho, Vector3.zero);
            if (techo == TipoTecho.Paja) tm.Pieza(Geo.Caja(0.16f, 0.16f, lg + 1.9f, 1.5f), "madera", v(0, rise + 0.03f, 0));

            if (puerta) n.Pieza(Geo.Caja(0.95f, 1.9f, 0.14f, 1.5f), "oscuro", v(0, 0.95f, l / 2 + 0.04f));
            if (w > 5)
                foreach (var sx in new[] { -1f, 1f })
                {
                    n.Pieza(Geo.Caja(0.8f, 0.9f, 0.12f, 1.5f), Ventana(), v(sx * w * 0.3f, 1.5f, l / 2 + 0.04f));
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

        /// Rancho abandonado: muros desmoronados (el del fondo casi en el suelo), el techo caído con la cumbrera
        /// quebrada y algunos tirantes a la vista, un pedazo de quincho todavía colgando y adobes tirados alrededor.
        void Ruina(Transform raiz, float w, float l, float h, float rise, string mPared, string mTecho)
        {
            var n = new Nodo("ruina");
            float R(double i) => MapaPago.Hs(i * 3.7 + w * 13 + l * 7);
            const float e = 0.35f;   // espesor del muro de adobe
            // cada muro en tramos de 0,8 m con la parte de arriba despareja
            void Muro(float x0, float z0, float x1, float z1, float alto, int sem, float hueco = -1)
            {
                float largoM = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
                int tramos = Mathf.Max(1, Mathf.RoundToInt(largoM / 0.8f));
                float ang = Mathf.Atan2(z1 - z0, x1 - x0);
                for (int i = 0; i < tramos; i++)
                {
                    float t = (i + 0.5f) / tramos;
                    if (hueco >= 0 && Mathf.Abs(t - hueco) < 0.12f) continue;   // la puerta (o el boquete)
                    float hh = alto * (0.55f + 0.45f * R(sem * 31 + i)) * (1 - 0.35f * Mathf.Abs(t - 0.5f) * R(sem + 99));
                    if (R(sem * 17 + i * 3) < 0.12f) hh *= 0.35f;   // un boquete
                    var c = v(Mathf.Lerp(x0, x1, t), hh / 2, Mathf.Lerp(z0, z1, t));
                    n.Pieza(Geo.Caja(largoM / tramos + 0.02f, hh, e, 2), mPared, c, v(0, -ang, 0));
                }
            }
            float hw = w / 2, hl = l / 2;
            Muro(-hw, hl, hw, hl, h, 1, 0.5f);           // frente, con la puerta
            Muro(-hw, -hl, hw, -hl, h * 0.35f, 2);       // fondo: casi en el suelo
            Muro(-hw, -hl, -hw, hl, h * 0.9f, 3);
            Muro(hw, -hl, hw, hl, h * 0.7f, 4);
            // cumbrera quebrada: una punta sobre el muro de un costado y la otra clavada adentro
            float cima = h + rise * 0.6f;
            Tirante(n, v(-hw + 0.2f, cima, 0), v(hw * 0.35f, 0.6f, -hl * 0.2f), 0.14f);
            for (int i = 0; i < 4; i++)
            {
                float x = -hw + 0.6f + i * (w - 1.2f) / 3;
                if (R(i + 40) < 0.3f) continue;
                float caidaT = R(i + 50) * 1.6f;
                Tirante(n, v(x, h * 0.85f, hl), v(x + (R(i + 60) - 0.5f) * 0.8f, Mathf.Max(0.4f, cima - caidaT - x * 0.15f), 0), 0.08f);
            }
            // el pedazo de quincho que queda, colgando torcido sobre el frente
            var q = n.Hijo("quincho", v(-hw * 0.35f, h * 0.9f, hl * 0.45f));
            q.rot = v(0.55f, 0.15f, 0.12f);
            q.Pieza(Geo.Caja(w * 0.55f, 0.18f, l * 0.55f, 1.5f), mTecho, Vector3.zero);
            // adobes y terrones tirados
            for (int i = 0; i < 14; i++)
            {
                float ang = R(i + 70) * Mathf.PI * 2, r = Mathf.Max(hw, hl) * (0.6f + R(i + 80) * 0.7f);
                float s = 0.3f + R(i + 90) * 0.35f;
                n.Pieza(Geo.Caja(s * 1.4f, s * 0.6f, s, 2), mPared, v(Mathf.Cos(ang) * r, s * 0.25f, Mathf.Sin(ang) * r), v(R(i + 100) * 0.5f, ang, R(i + 110) * 0.5f));
            }
            n.Instanciar(raiz, Registrar);
            Colision(raiz, new Vector3(0, h * 0.4f, 0), new Vector3(w + 0.2f, h * 0.8f, l + 0.2f));
        }

        static void Tirante(Nodo n, Vector3 a, Vector3 b, float grueso)
        {
            var d = b - a;
            float largo = d.magnitude;
            var c = (a + b) / 2;
            // la caja va a lo largo de x; se gira hacia la dirección del tirante (ángulos del prototipo)
            float yaw = -Mathf.Atan2(d.z, d.x), pitch = Mathf.Asin(Mathf.Clamp(d.y / Mathf.Max(largo, 1e-4f), -1, 1));
            n.Pieza(Geo.Caja(largo, grueso, grueso, 1.5f), "madera", c, v(0, yaw, pitch));
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
