using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Humo que sube de un fogón o de una chimenea y se lo lleva el viento (Ambiente.Viento).
    /// Las chimeneas humean a la hora de cocinar (madrugada, mediodía y noche); los fogones, siempre.
    /// Con lluvia fuerte el humo es más blanco y bajo. Opcional: chispas que saltan del fuego.
    /// Lo agregan Fogon y Casona al armarse en el juego; no se guarda en la escena.
    /// </summary>
    public class Humo : MonoBehaviour
    {
        public bool chimenea;
        public float intensidad = 1;
        /// Fuerza de la llama (0 a 1): las chispas saltan solo con fuego vivo.
        public float llama = 1;
        ParticleSystem humo, chispas;

        public static Humo Poner(Transform padre, Vector3 posLocal, bool chimenea, float intensidad = 1, bool conChispas = false)
        {
            var go = new GameObject(chimenea ? "humo de chimenea" : "humo del fogón");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posLocal;
            var h = go.AddComponent<Humo>();
            h.chimenea = chimenea; h.intensidad = intensidad;
            h.humo = Particulas.Crear("humo", go.transform, Vector3.zero, 80, 7, new Vector2(0.6f, 1.2f), new Color(0.62f, 0.62f, 0.6f, 0.35f));
            var sz = h.humo.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.6f, 1, 3.2f));
            var rot = h.humo.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            var ruido = h.humo.noise; ruido.enabled = true; ruido.strength = 0.35f; ruido.frequency = 0.4f;
            Particulas.Desvanecer(h.humo, 0.1f, 0.4f, 1);
            var sh = h.humo.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            if (conChispas)
            {
                h.chispas = Particulas.Crear("chispas", go.transform, Vector3.zero, 30, 1.2f, new Vector2(0.03f, 0.06f), new Color(1f, 0.6f, 0.2f, 1), true);
                var main = h.chispas.main; main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f); main.gravityModifier = -0.15f;
                var sh2 = h.chispas.shape; sh2.shapeType = ParticleSystemShapeType.Cone; sh2.angle = 18; sh2.radius = 0.1f; sh2.rotation = new Vector3(-90, 0, 0);
                var ruido2 = h.chispas.noise; ruido2.enabled = true; ruido2.strength = 0.8f; ruido2.frequency = 1.5f;
                Particulas.Desvanecer(h.chispas, 0.05f, 0.5f, 1);
            }
            return h;
        }

        void Update()
        {
            if (humo == null) return;
            float h = Ambiente.Hora;
            float cocina = !chimenea ? 1 : Mathf.Max(Franja(h, 5.5f, 8.5f), Franja(h, 11.2f, 13.2f), Franja(h, 18.5f, 22.5f));
            float lluvia = Ambiente.Lluvia;
            var em = humo.emission; em.rateOverTime = 5 * intensidad * cocina * (1 - lluvia * 0.4f);
            // el humo no recibe luz: de noche se oscurece con el ambiente (salvo el resplandor del fogón)
            var main = humo.main;
            float c = Ambiente.Claridad;
            main.startColor = new Color(0.62f * c + (chimenea ? 0 : 0.08f), 0.62f * c + (chimenea ? 0 : 0.05f), 0.6f * c, 0.35f);
            var v = Ambiente.Viento;
            var vel = humo.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = v.x * 4.5f; vel.y = 0.9f * (1 - v.magnitude * 0.6f); vel.z = v.y * 4.5f;
            if (chispas != null)
            {
                var ec = chispas.emission;
                ec.rateOverTime = (6 + 10 * Ambiente.LucesDeNoche) * (1 - lluvia) * llama * llama;
                var vc = chispas.velocityOverLifetime; vc.enabled = true; vc.space = ParticleSystemSimulationSpace.World;
                vc.x = v.x * 2; vc.y = 0; vc.z = v.y * 2;
            }
        }

        static float Franja(float h, float a, float b) => h >= a && h <= b ? Mathf.Clamp01(Mathf.Min(h - a, b - h) * 2) : 0;
    }
}
