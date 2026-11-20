using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// Animación procedural del personaje: piernas, rodillas, brazos y codos que se balancean al caminar,
    /// y respiración y cabeceo en reposo (userData.animar del prototipo).
    public class AnimPersonaje
    {
        public Transform cuerpo, torso, cab;
        public readonly List<Transform> piernas = new List<Transform>(), rodillas = new List<Transform>();
        public readonly List<Transform> brazos = new List<Transform>(), codos = new List<Transform>();
        public bool vestido;

        /// Sentado a caballo: muslos hacia adelante y abiertos, rodillas dobladas, manos en las riendas.
        public void Montar()
        {
            for (int i = 0; i < piernas.Count; i++) piernas[i].localRotation = Conv.Rot(-1.3f, 0, i == 0 ? -0.5f : 0.5f);
            foreach (var r in rodillas) r.localRotation = Conv.Rot(1.25f, 0, 0);
            for (int i = 0; i < brazos.Count; i++) brazos[i].localRotation = Conv.Rot(-0.7f, 0, (i == 0 ? -1 : 1) * 0.1f);
            foreach (var c in codos) c.localRotation = Conv.Rot(-0.9f, 0, 0);
        }

        /// Jinete en movimiento: la pose de Montar con el vaivén del paso. fase: la del caballo; g: 0 quieto o al trote,
        /// 1 al galope (se inclina hacia adelante y acompaña la hamaca); t: tiempo, para la cabeza en reposo.
        public void Jinete(float fase, float v, float g, float t)
        {
            if (cuerpo == null) return;
            Montar();
            float trote = Mathf.Min(1, v) * (1 - g);
            cuerpo.localPosition = Conv.P(0, Mathf.Abs(Mathf.Sin(fase)) * (0.05f * trote + 0.025f * g), 0);
            cuerpo.localRotation = Conv.Rot(0.12f * g + Mathf.Sin(fase) * 0.05f * g, 0, 0);   // positivo: hacia adelante (como Cocinando)
            torso.localScale = Vector3.one;
            float reposo = 1 - Mathf.Min(1, v);
            cab.localRotation = Conv.Rot(0, Mathf.Sin(t * 0.37f) * 0.25f * reposo, 0);
        }

        /// Sentado en un tronco, charlando: muslos adelante, rodillas dobladas, manos sobre las rodillas.
        /// gesto (0 a 1) levanta una mano como quien cuenta algo; mira gira la cabeza hacia los otros (radianes).
        /// baja: cuánto baja la cadera (con vestido, menos, porque no hay piernas).
        public void Sentado(float t, float gesto, float mira, float baja = 0.44f)
        {
            if (cuerpo == null) return;
            for (int i = 0; i < piernas.Count; i++)
            {
                piernas[i].localRotation = Conv.Rot(-1.45f, 0, (i == 0 ? -1 : 1) * 0.14f);
                rodillas[i].localRotation = Conv.Rot(1.4f, 0, 0);
            }
            float abre = vestido ? 0.2f : 0.1f;
            for (int i = 0; i < brazos.Count; i++)
            {
                float dir = i == 0 ? -1 : 1, g = i == 1 ? gesto : 0;
                brazos[i].localRotation = Conv.Rot(-0.5f - g * 0.75f + Mathf.Sin(t * 4.1f) * 0.12f * g, 0, dir * abre);
                codos[i].localRotation = Conv.Rot(-0.75f - g * 0.6f, 0, 0);
            }
            cuerpo.localPosition = Conv.P(0, -baja, 0);
            cuerpo.localRotation = Conv.Rot(0.12f, 0, 0);
            torso.localScale = new Vector3(1, 1 + Mathf.Sin(t * 0.9f) * 0.006f, 1);
            cab.localRotation = Conv.Rot(0.05f + Mathf.Sin(t * 0.31f) * 0.05f, mira, 0);
        }

        /// Parado e inclinado sobre la olla o el asado: un brazo revuelve (o acomoda la carne) y el otro descansa.
        /// ritmo: vueltas del cucharón por segundo (más lento para el asado).
        public void Cocinando(float t, float ritmo = 0.45f)
        {
            if (cuerpo == null) return;
            foreach (var p in piernas) p.localRotation = Conv.Rot(-0.12f, 0, 0);
            foreach (var r in rodillas) r.localRotation = Conv.Rot(0.22f, 0, 0);
            float w = t * ritmo * Mathf.PI * 2;
            float abre = vestido ? 0.24f : 0.08f;
            if (brazos.Count > 1)
            {
                brazos[1].localRotation = Conv.Rot(-1.0f + Mathf.Sin(w) * 0.16f, 0, 0.12f + Mathf.Cos(w) * 0.18f);
                codos[1].localRotation = Conv.Rot(-0.55f + Mathf.Cos(w) * 0.1f, 0, 0);
                brazos[0].localRotation = Conv.Rot(-0.25f, 0, -abre);
                codos[0].localRotation = Conv.Rot(-0.9f, 0, 0);   // la mano en la cintura
            }
            cuerpo.localPosition = Conv.P(0, -0.04f, 0);
            cuerpo.localRotation = Conv.Rot(0.24f, 0, 0);
            torso.localScale = Vector3.one;
            cab.localRotation = Conv.Rot(0.28f, Mathf.Sin(t * 0.27f) * 0.2f, 0);
        }

        /// t: fase del paso; v: 0 quieto, 1 caminando, 1.35 corriendo.
        public void Animar(float t, float v)
        {
            if (cuerpo == null) return;
            float a = Mathf.Sin(t) * 0.6f * v;
            for (int i = 0; i < piernas.Count; i++)
            {
                float fase = i == 0 ? t : t + Mathf.PI;
                piernas[i].localRotation = Conv.Rot(Mathf.Sin(fase) * 0.6f * v, 0, 0);
                rodillas[i].localRotation = Conv.Rot(Mathf.Max(0, Mathf.Sin(fase - 1.3f)) * 0.9f * v, 0, 0);
            }
            float abre = vestido ? 0.24f : 0.05f;
            for (int i = 0; i < brazos.Count; i++)
            {
                float dir = i == 0 ? -1 : 1;
                brazos[i].localRotation = Conv.Rot(dir * a * 0.75f, 0, dir * abre);
                codos[i].localRotation = Conv.Rot(-0.18f - Mathf.Max(0, dir * -a) * 0.9f * (v != 0 ? 1 : 0), 0, 0);
            }
            float reposo = 1 - Mathf.Min(1, v);
            cuerpo.localPosition = Conv.P(0, Mathf.Abs(Mathf.Sin(t)) * 0.04f * v, 0);
            cuerpo.localRotation = Conv.Rot(0, Mathf.Sin(t) * 0.08f * v, 0);
            torso.localScale = new Vector3(1, 1 + Mathf.Sin(t * 0.9f) * 0.006f * reposo, 1);
            cab.localRotation = Conv.Rot(0, Mathf.Sin(t * 0.37f) * 0.25f * reposo, Mathf.Sin(t * 0.23f) * 0.05f * reposo);
        }
    }
}
