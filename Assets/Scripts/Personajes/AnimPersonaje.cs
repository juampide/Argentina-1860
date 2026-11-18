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
