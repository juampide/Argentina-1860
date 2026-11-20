using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// El aire alrededor del jugador, para que el pago no se vea quieto:
    /// - pelusas de cardo que se lleva el viento (de día, con tiempo seco);
    /// - motas de polvo que brillan donde pega el sol;
    /// - bichitos de luz al atardecer y de noche, cerca del pasto;
    /// - niebla baja a la madrugada (Ambiente.Madrugada), arrastrándose por el suelo.
    /// Lo agrega Ambiente al arrancar el juego; no se guarda en la escena.
    /// </summary>
    public class Aire : MonoBehaviour
    {
        ParticleSystem pelusas, motas, bichitos, niebla;
        Transform foco;

        void Start()
        {
            var cj = FindAnyObjectByType<ControlJugador>();
            foco = cj != null ? cj.transform : null;

            pelusas = Particulas.Crear("pelusas de cardo", transform, Vector3.zero, 90, 14, new Vector2(0.08f, 0.16f), new Color(0.95f, 0.93f, 0.86f, 0.85f));
            Caja(pelusas, new Vector3(60, 5, 60));
            Ruido(pelusas, 0.5f, 0.3f);
            Particulas.Desvanecer(pelusas, 0.1f, 0.8f, 1);

            motas = Particulas.Crear("motas de polvo", transform, Vector3.zero, 160, 8, new Vector2(0.025f, 0.05f), new Color(1f, 0.92f, 0.75f, 0.6f), true);
            Caja(motas, new Vector3(40, 4, 40));
            Ruido(motas, 0.25f, 0.6f);
            Particulas.Desvanecer(motas, 0.2f, 0.7f, 1);

            bichitos = Particulas.Crear("bichitos de luz", transform, Vector3.zero, 70, 5, new Vector2(0.07f, 0.11f), new Color(0.85f, 1f, 0.45f, 1), true);
            Caja(bichitos, new Vector3(45, 1.2f, 45));
            Ruido(bichitos, 1.2f, 0.5f);
            // prenden y apagan
            var col = bichitos.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.1f, 0.35f), new GradientAlphaKey(1, 0.55f), new GradientAlphaKey(0.05f, 0.75f), new GradientAlphaKey(0.9f, 0.88f), new GradientAlphaKey(0, 1) });
            col.color = g;

            niebla = Particulas.Crear("niebla baja", transform, Vector3.zero, 60, 22, new Vector2(9, 16), new Color(0.82f, 0.84f, 0.84f, 0.16f));
            Caja(niebla, new Vector3(90, 0.6f, 90));
            Particulas.Desvanecer(niebla, 0.3f, 0.6f, 1);
            var nr = niebla.GetComponent<ParticleSystemRenderer>();
            nr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;   // tendida sobre el suelo
            nr.sortingFudge = 10;
        }

        static void Caja(ParticleSystem ps, Vector3 tam)
        {
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = tam;
        }

        static void Ruido(ParticleSystem ps, float fuerza, float frecuencia)
        {
            var r = ps.noise; r.enabled = true; r.strength = fuerza; r.frequency = frecuencia; r.scrollSpeed = 0.3f;
        }

        static void Viento(ParticleSystem ps, Vector2 v, float escala, float subir)
        {
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = v.x * escala; vel.y = subir; vel.z = v.y * escala;
        }

        void LateUpdate()
        {
            if (foco == null) return;
            var p = foco.position;
            float noche = Ambiente.LucesDeNoche, lluvia = Ambiente.Lluvia, h = Ambiente.Hora;
            float seco = Mathf.Clamp01(1 - Ambiente.Humedad * 2) * (1 - lluvia);
            var v = Ambiente.Viento;

            pelusas.transform.position = p + new Vector3(-v.x * 15, 2.5f, -v.y * 15);   // nacen del lado de donde viene el viento
            Emitir(pelusas, 7 * (1 - noche) * seco * (0.3f + v.magnitude));
            Viento(pelusas, v, 5, 0.15f);

            motas.transform.position = p + new Vector3(0, 1.8f, 0);
            float sol = (1 - noche) * Mathf.Clamp01(1 - Ambiente.TiempoNubes * 1.2f);
            Emitir(motas, 22 * sol * seco);
            Viento(motas, v, 1.2f, 0.05f);

            bichitos.transform.position = p + new Vector3(0, 0.8f, 0);
            float horaBichos = Mathf.Clamp01((h - 18.6f) * 1.5f) * Mathf.Clamp01((23.5f - h) * 0.8f) + Mathf.Clamp01((5.2f - h) * 0.5f) * (h < 6 ? 1 : 0);
            Emitir(bichitos, 9 * horaBichos * (1 - lluvia) * (1 - Mathf.Clamp01(v.magnitude - 0.5f) * 2));

            float c = Ambiente.Claridad;
            var mp = pelusas.main; mp.startColor = new Color(0.95f * c, 0.93f * c, 0.86f * c, 0.85f);
            var mn = niebla.main; mn.startColor = new Color(0.82f * c, 0.84f * c, 0.86f * c, 0.16f);
            niebla.transform.position = p + new Vector3(0, 0.4f, 0);
            Emitir(niebla, 3.5f * Ambiente.Madrugada);
            Viento(niebla, v, 0.6f, 0);
        }

        static void Emitir(ParticleSystem ps, float tasa)
        {
            var em = ps.emission; em.rateOverTime = Mathf.Max(0, tasa);
        }
    }
}
