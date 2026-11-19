using UnityEngine;

namespace Argentina1860
{
    /// Fogón de piedras con leña y llama (fogon() de campo.js), con una luz cálida que titila.
    public class Fogon : ProceduralBase
    {
        public bool luz = true;
        Light llama;
        float semilla;

        protected override void Construir(Transform raiz)
        {
            var n = new Nodo("fogon");
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f;
                n.Pieza(Geo.Ico(1, 0), "piedra", new Vector3(Mathf.Cos(a) * 0.55f, 0.1f, Mathf.Sin(a) * 0.55f), default, Vector3.one * 0.17f);
            }
            for (int i = 0; i < 4; i++)
                n.Pieza(Geo.Cilindro(0.05f, 0.05f, 0.9f, 5), "madera", new Vector3(0, 0.18f, 0), new Vector3(1.3f, i * 0.8f, 0));
            n.Pieza(Geo.Cono(0.28f, 0.8f, 5), "llama", new Vector3(0, 0.5f, 0));
            n.Instanciar(raiz, Registrar);

            if (luz)
            {
                var go = new GameObject("luz");
                go.transform.SetParent(raiz, false);
                go.transform.localPosition = new Vector3(0, 0.9f, 0);
                llama = go.AddComponent<Light>();
                llama.type = LightType.Point;
                llama.color = new Color(1f, 0.6f, 0.25f);
                llama.range = 7;
                llama.intensity = 2.5f;
                llama.shadows = LightShadows.None;
                semilla = Random.value * 100;
            }
        }

        void Update()
        {
            if (llama == null || !Application.isPlaying) return;
            float t = Time.time * 7 + semilla;
            // de noche el fuego alumbra más lejos (Ambiente.LucesDeNoche va de 0 de día a 1 de noche)
            float noche = Ambiente.LucesDeNoche;
            llama.intensity = (2.2f + Mathf.Sin(t) * 0.25f + Mathf.Sin(t * 2.7f) * 0.15f) * (1 + noche * 1.8f);
            llama.range = 7 + noche * 9;
        }
    }
}
