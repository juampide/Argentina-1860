using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Ajustes del aspecto pictórico (posproceso que pinta con manchas, marca bordes con tinta, agrega grano y viñeta).
    /// En juego, F4 lo prende y lo apaga para comparar; los valores se ajustan en el objeto "Estilo pintado" que se crea solo.
    /// </summary>
    public class EstiloPintado : MonoBehaviour
    {
        public static bool Activo = true;
        public static float Radio = 1.5f, Intensidad = 0.85f, Tinta = 0.45f, Grano = 0.012f, Vineta = 0.3f;

        [Header("Se prende y apaga con F4")]
        [Range(1, 4)] public float radio = 1.5f;
        [Tooltip("0 es la imagen sin pintar; 1, toda pintada")]
        [Range(0, 1)] public float intensidad = 0.85f;
        [Tooltip("Qué tan oscura es la tinta de los bordes")]
        [Range(0, 1)] public float tinta = 0.45f;
        [Range(0, 0.2f)] public float grano = 0.012f;
        [Range(0, 1)] public float vineta = 0.3f;

        float avisoT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Arrancar()
        {
            if (FindAnyObjectByType<EstiloPintado>() == null) new GameObject("Estilo pintado (F4)").AddComponent<EstiloPintado>();
        }

        void Update()
        {
            Radio = radio; Intensidad = intensidad; Tinta = tinta; Grano = grano; Vineta = vineta;
            var kb = Keyboard.current;
            if (kb != null && kb.f4Key.wasPressedThisFrame) { Activo = !Activo; avisoT = 2.5f; }
            if (avisoT > 0) avisoT -= Time.deltaTime;
        }

        void OnGUI()
        {
            if (avisoT <= 0) return;
            GUI.matrix = Ui.Matriz;
            GUI.Box(new Rect(16, Ui.Alto - 44, 360, 28), "Pintura: " + (Activo ? "sí" : "no") + "   (F4)");
        }
    }
}
