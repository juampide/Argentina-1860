using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Farol de aceite de mano: caja de hierro con vidrios, sombrerete y argolla, y adentro la llama que alumbra
    /// alrededor de quien lo lleva. De noche, la gente que anda fuera de los poblados lo lleva colgando de la mano.
    /// Se usa como accesorio del personaje (Accesorio.Farol) o se le cuelga en el juego a quien haga falta
    /// (por ejemplo, a Santos en la llegada). Con soloDeNoche, de día no se ve ni alumbra.
    /// </summary>
    public class Farol : MonoBehaviour
    {
        public bool soloDeNoche = true;
        Light luz;
        Renderer[] partes;
        float semilla;

        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);

        /// Arma el farol colgando de una mano (la del antebrazo del personaje) y devuelve el componente.
        public static Farol Colgar(Transform mano, bool soloDeNoche = true)
        {
            var n = new Nodo("farol", v(0, -0.36f, 0.05f));
            n.Pieza(Geo.Toro(0.035f, 0.006f, 3, 8), "hierro", v(0, 0.02f, 0), v(Mathf.PI / 2, 0, 0));        // argolla
            n.Pieza(Geo.Cono(0.07f, 0.06f, 4), "hierro", v(0, -0.04f, 0), v(0, Mathf.PI / 4, 0));             // sombrerete
            n.Pieza(Geo.Cilindro(0.055f, 0.055f, 0.14f, 4), "farol", v(0, -0.14f, 0), v(0, Mathf.PI / 4, 0)); // vidrios con la llama
            foreach (var (x, z) in new[] { (1f, 1f), (-1f, 1f), (1f, -1f), (-1f, -1f) })
                n.Pieza(Geo.Caja(0.012f, 0.15f, 0.012f), "hierro", v(x * 0.04f, -0.14f, z * 0.04f));          // parantes
            n.Pieza(Geo.Cilindro(0.065f, 0.065f, 0.025f, 4), "hierro", v(0, -0.22f, 0), v(0, Mathf.PI / 4, 0)); // base
            var t = n.Instanciar(mano, null);
            var f = t.gameObject.AddComponent<Farol>();
            f.soloDeNoche = soloDeNoche;
            var go = new GameObject("llama");
            go.transform.SetParent(t, false);
            go.transform.localPosition = Conv.P(0, -0.14f, 0);
            f.luz = go.AddComponent<Light>();
            f.luz.type = LightType.Point;
            f.luz.color = new Color(1f, 0.66f, 0.32f);
            f.luz.range = 7;
            f.luz.shadows = LightShadows.None;
            f.partes = t.GetComponentsInChildren<Renderer>();
            foreach (var r in f.partes) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.semilla = Random.value * 50;
            f.Actualizar();
            return f;
        }

        void Update() => Actualizar();

        void Actualizar()
        {
            if (luz == null) return;
            float noche = Application.isPlaying ? Ambiente.LucesDeNoche : 1;
            bool ve = !soloDeNoche || noche > 0.25f;
            foreach (var r in partes) if (r != null && r.enabled != ve) r.enabled = ve;
            luz.enabled = ve;
            if (!ve) return;
            float t = Time.time * 9 + semilla;
            float tit = 1 + Mathf.Sin(t) * 0.08f + Mathf.Sin(t * 2.3f) * 0.05f + Mathf.Sin(t * 5.1f) * 0.03f;
            luz.intensity = Mathf.Lerp(0.5f, 2.2f, noche) * tit;
            luz.range = Mathf.Lerp(4, 8, noche);
        }
    }
}
