using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Casco de estancia de gente pudiente, como se construía en la campaña de Buenos Aires hacia 1860:
    /// casa de patio en U, muros encalados con zócalo, galería al frente sobre pilares, ventanas con reja
    /// y postigos, puerta de dos hojas y, si se pide, mirador (la torre desde donde se vigilaba el campo).
    /// El techo puede ser de teja a dos aguas o de azotea con pretil. Se cambian los valores y se rearma sola.
    /// El frente (galería y puerta) mira hacia -Z local, como en Casa.
    /// </summary>
    public class Casona : ProceduralBase
    {
        public enum Techo { Teja, Azotea }

        [Header("Medidas (metros)")]
        [Tooltip("Frente de la casa")]
        [Min(10)] public float frente = 28;
        [Tooltip("Fondo total, de la galería a la punta de las alas")]
        [Min(8)] public float fondo = 20;
        [Tooltip("Ancho de cada ala (una fila de cuartos)")]
        [Min(4)] public float ala = 7;
        [Min(3)] public float alto = 4.2f;

        [Header("Forma")]
        public Techo techo = Techo.Teja;
        [Tooltip("Alas a los costados que cierran el patio por detrás (casa en U)")]
        public bool alas = true;
        public bool galeria = true;
        public bool mirador = true;
        public bool rejas = true;

        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
        const string ZOCALO = "#7d7266", POSTIGO = "#2f4a35", PUERTA = "#4a3020", LADRILLO = "#9a5a3e", PILAR = "blanco";

        protected override void Construir(Transform raiz)
        {
            var n = new Nodo("casona");
            float W = frente, F = alas ? fondo : ala, p = ala, h = alto;
            // en el espacio del prototipo +z es el frente; el ala del frente queda adelante
            float zFrente = F / 2 - p / 2;

            Cuerpo(n, v(0, 0, zFrente), W, p, h, true);
            if (alas)
            {
                float largoAla = F - p;
                foreach (var sx in new[] { -1f, 1f })
                    Cuerpo(n, v(sx * (W / 2 - p / 2), 0, zFrente - p / 2 - largoAla / 2), p, largoAla, h, false);
            }

            // fachada: puerta de dos hojas al medio y ventanas repartidas
            float zf = zFrente + p / 2 + 0.03f;
            n.Pieza(Geo.Caja(1.8f, 2.8f, 0.12f, 1.5f), PUERTA, v(0, 1.4f, zf));
            n.Pieza(Geo.Caja(0.06f, 2.8f, 0.14f, 1.5f), "oscuro", v(0, 1.4f, zf + 0.01f));
            n.Pieza(Geo.Caja(2.2f, 0.25f, 0.18f, 1.5f), PILAR, v(0, 2.95f, zf + 0.02f));   // dintel
            int nVent = Mathf.Max(2, Mathf.RoundToInt(W / 4.5f));
            for (int i = 0; i < nVent; i++)
            {
                float x = -W / 2 + (i + 0.5f) * W / nVent;
                if (Mathf.Abs(x) < 2) continue;
                Ventana(n, v(x, 2.0f, zf), 1.1f, 1.8f);
            }
            // ventanas de los costados de las alas, mirando afuera
            if (alas)
                foreach (var sx in new[] { -1f, 1f })
                    for (int i = 0; i < 2; i++)
                    {
                        float z = zFrente - p / 2 - (i + 0.5f) * (F - p) / 2;
                        Ventana(n, v(sx * (W / 2 + 0.03f), 2.0f, z), 1.1f, 1.8f, true);
                    }

            if (galeria)
            {
                float prof = 3.2f, zg = zFrente + p / 2 + prof;
                n.Pieza(Geo.Caja(W + 0.6f, 0.25f, prof + 0.2f, 2), LADRILLO, v(0, 0.12f, zFrente + p / 2 + prof / 2));   // piso de ladrillo
                int nP = Mathf.Max(2, Mathf.RoundToInt(W / 3.2f));
                for (int k = 0; k <= nP; k++)
                {
                    float x = -W / 2 + k * W / nP;
                    n.Pieza(Geo.Caja(0.4f, h - 0.2f, 0.4f, 1.5f), PILAR, v(x, 0.25f + (h - 0.2f) / 2 - 0.12f, zg - 0.2f));
                }
                // techo de la galería: con teja en pendiente; con azotea, plano con pretil
                if (techo == Techo.Teja)
                    n.Pieza(Geo.Caja(W + 1.2f, 0.16f, prof + 0.9f, 2), "teja", v(0, h + 0.05f, zFrente + p / 2 + prof / 2 + 0.2f), v(0.16f, 0, 0));
                else
                {
                    n.Pieza(Geo.Caja(W + 0.6f, 0.3f, prof + 0.4f, 2), "blanco", v(0, h, zFrente + p / 2 + prof / 2));
                    n.Pieza(Geo.Caja(W + 0.6f, 0.6f, 0.2f, 2), "blanco", v(0, h + 0.45f, zg));
                }
            }

            if (mirador)
            {
                // torre sobre la esquina del frente, del lado del oeste
                float lado = 3.6f, alt = 4.6f, x = -W / 2 + lado / 2 + 0.6f, z = zFrente;
                float base_ = h + (techo == Techo.Teja ? 0.2f : 0.4f);
                n.Pieza(Geo.Caja(lado, alt, lado, 2), "blanco", v(x, base_ + alt / 2 - 0.4f, z));
                n.Pieza(Geo.Caja(lado + 0.3f, 0.3f, lado + 0.3f, 2), ZOCALO, v(x, base_ + alt - 0.25f, z));   // cornisa
                foreach (var (dx, dz, rot) in new[] { (0f, lado / 2 + 0.03f, false), (0f, -lado / 2 - 0.03f, false), (lado / 2 + 0.03f, 0f, true), (-lado / 2 - 0.03f, 0f, true) })
                    n.Pieza(Geo.Caja(rot ? 0.1f : 1.4f, 1.4f, rot ? 1.4f : 0.1f, 1.5f), "oscuro", v(x + dx, base_ + alt - 1.6f, z + dz));
                // remate: pretil con almenas bajas
                for (int k = 0; k < 4; k++)
                {
                    float a = k * Mathf.PI / 2;
                    n.Pieza(Geo.Caja(lado + 0.3f, 0.5f, 0.25f, 2), "blanco", v(x + Mathf.Sin(a) * (lado / 2 + 0.05f), base_ + alt + 0.1f, z + Mathf.Cos(a) * (lado / 2 + 0.05f)), v(0, a, 0));
                }
                Colision(raiz, Conv.P(x, base_ + alt / 2, z), new Vector3(lado, alt, lado));
            }

            // chimeneas
            foreach (var sx in new[] { -0.3f, 0.32f })
                n.Pieza(Geo.Caja(0.7f, 1.6f, 0.7f, 2), "blanco", v(sx * W, h + 1.2f, zFrente - p * 0.15f));

            n.Instanciar(raiz, Registrar);
            // colisiones por cuerpo (en Unity la z va al revés)
            Colision(raiz, Conv.P(0, h / 2, zFrente), new Vector3(W, h, p));
            if (alas)
                foreach (var sx in new[] { -1f, 1f })
                    Colision(raiz, Conv.P(sx * (W / 2 - p / 2), h / 2, zFrente - p / 2 - (F - p) / 2), new Vector3(p, h, F - p));
        }

        /// Un cuerpo de la casa: muro encalado con zócalo y su techo.
        void Cuerpo(Nodo n, Vector3 c, float w, float l, float h, bool frenteX)
        {
            n.Pieza(Geo.Caja(w, h, l, 3), "blanco", c + v(0, h / 2, 0));
            n.Pieza(Geo.Caja(w + 0.08f, 0.7f, l + 0.08f, 2), ZOCALO, c + v(0, 0.35f, 0));
            n.Pieza(Geo.Caja(w + 0.25f, 0.22f, l + 0.25f, 2), "blanco", c + v(0, h - 0.05f, 0));   // cornisa
            if (techo == Techo.Teja)
            {
                var t = n.Hijo("techo", c + v(0, h + 0.05f, 0));
                bool ancho = w > l;
                if (ancho) t.rot = v(0, Mathf.PI / 2, 0);
                t.Pieza(Geo.Techo(Mathf.Min(w, l), Mathf.Max(w, l), 2.0f, 0.5f), "teja", Vector3.zero);
            }
            else
            {
                n.Pieza(Geo.Caja(w, 0.25f, l, 2), "blanco", c + v(0, h + 0.1f, 0));
                // pretil
                n.Pieza(Geo.Caja(w + 0.1f, 0.7f, 0.25f, 2), "blanco", c + v(0, h + 0.45f, l / 2));
                n.Pieza(Geo.Caja(w + 0.1f, 0.7f, 0.25f, 2), "blanco", c + v(0, h + 0.45f, -l / 2));
                n.Pieza(Geo.Caja(0.25f, 0.7f, l, 2), "blanco", c + v(w / 2, h + 0.45f, 0));
                n.Pieza(Geo.Caja(0.25f, 0.7f, l, 2), "blanco", c + v(-w / 2, h + 0.45f, 0));
            }
        }

        /// Ventana alta con reja de hierro y postigos verdes abiertos.
        void Ventana(Nodo n, Vector3 c, float an, float al, bool deCostado = false)
        {
            Vector3 X = deCostado ? v(0, 0, 1) : v(1, 0, 0);
            Vector3 hacia = deCostado ? v(Mathf.Sign(c.x), 0, 0) : v(0, 0, 1);
            Vector3 Caja(float a, float b, float g) => deCostado ? v(g, b, a) : v(a, b, g);
            var s = Caja(an, al, 0.1f);
            n.Pieza(Geo.Caja(s.x, s.y, s.z, 1.5f), "oscuro", c);
            var m = Caja(an + 0.3f, 0.12f, 0.16f);
            n.Pieza(Geo.Caja(m.x, m.y, m.z, 1.5f), "blanco", c + v(0, -al / 2 - 0.08f, 0) + hacia * 0.03f);   // alféizar
            foreach (var lado in new[] { -1f, 1f })
            {
                var ps = Caja(an * 0.5f, al, 0.05f);
                n.Pieza(Geo.Caja(ps.x, ps.y, ps.z, 1.5f), POSTIGO, c + X * lado * (an * 0.75f) + hacia * 0.06f);
            }
            if (!rejas) return;
            for (int k = 1; k < 5; k++)
            {
                var b = Caja(0.04f, al, 0.04f);
                n.Pieza(Geo.Caja(b.x, b.y, b.z), "hierro", c + X * (-an / 2 + k * an / 5) + hacia * 0.1f);
            }
            var tr = Caja(an, 0.04f, 0.04f);
            n.Pieza(Geo.Caja(tr.x, tr.y, tr.z), "hierro", c + v(0, al * 0.2f, 0) + hacia * 0.1f);
        }
    }
}
