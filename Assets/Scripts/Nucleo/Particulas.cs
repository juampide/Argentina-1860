using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Ayudas para las partículas del ambiente (humo, chispas, pelusas, niebla baja, bichitos de luz):
    /// una textura redonda y suave, materiales de URP con transparencia o suma, y un armador de sistemas
    /// con los valores que se repiten. Todo se crea en el juego y no se guarda.
    /// </summary>
    public static class Particulas
    {
        static Texture2D suave;
        static readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();

        /// Mancha redonda que se desvanece hacia el borde.
        public static Texture2D Suave
        {
            get
            {
                if (suave != null) return suave;
                const int S = 64;
                suave = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Partícula suave", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = (x + 0.5f) / S * 2 - 1, dy = (y + 0.5f) / S * 2 - 1;
                        float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                        a = a * a * (3 - 2 * a);
                        px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                suave.SetPixels32(px);
                suave.Apply(false);
                return suave;
            }
        }

        /// Material de partícula: con transparencia común o sumando luz (chispas, bichitos de luz).
        public static Material Material(bool suma)
        {
            string k = suma ? "suma" : "alfa";
            if (materiales.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Partícula " + k, hideFlags = HideFlags.HideAndDontSave };
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", suma ? 2 : 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(suma ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (suma) m.EnableKeyword("_BLENDMODE_ADD");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetTexture("_BaseMap", Suave);
            m.SetColor("_BaseColor", Color.white);
            materiales[k] = m;
            return m;
        }

        /// Sistema de partículas básico (en espacio del mundo, sin emitir hasta que se le pida).
        public static ParticleSystem Crear(string nombre, Transform padre, Vector3 pos, int max, float vida, Vector2 tam, Color color, bool suma = false)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.maxParticles = max;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vida * 0.7f, vida);
            main.startSize = new ParticleSystem.MinMaxCurve(tam.x, tam.y);
            main.startSpeed = 0;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var em = ps.emission; em.rateOverTime = 0;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Material(suma);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// Aparece y se desvanece a lo largo de la vida.
        public static void Desvanecer(ParticleSystem ps, float entra = 0.15f, float sale = 0.6f, float alfa = 1)
        {
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(alfa, entra), new GradientAlphaKey(alfa, sale), new GradientAlphaKey(0, 1) });
            col.color = g;
        }
    }
}
