using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Limita los cuadros por segundo: sin límite, Unity dibuja cientos por segundo (más de 500 en la ciudad) y la
    /// máquina trabaja al máximo sin que se note en pantalla. F10 muestra cuadros por segundo y milisegundos por cuadro.
    /// Se instala solo; no hace falta ponerlo en las escenas.
    /// </summary>
    public class Rendimiento : MonoBehaviour
    {
        /// Cuadros por segundo del juego. 60 alcanza para este estilo; subirlo gasta más batería y ventilador.
        public const int CUADROS_POR_SEGUNDO = 60;

        bool ver;
        float acum, promedio, peor, peorVentana;
        int cuadros;
        GUIStyle estilo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Instalar()
        {
            QualitySettings.vSyncCount = 0;   // el límite lo pone targetFrameRate, igual en el editor y en el juego armado
            Application.targetFrameRate = CUADROS_POR_SEGUNDO;
            var go = new GameObject("Rendimiento");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<Rendimiento>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f10Key.wasPressedThisFrame) ver = !ver;
            float dt = Time.unscaledDeltaTime;
            acum += dt; cuadros++; peorVentana = Mathf.Max(peorVentana, dt);
            if (acum >= 0.5f) { promedio = acum / cuadros; peor = peorVentana; acum = 0; cuadros = 0; peorVentana = 0; }
        }

        void OnGUI()
        {
            if (!ver || promedio <= 0) return;
            estilo ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 13 };
            GUI.Box(new Rect(Screen.width - 190, 10, 180, 26), $" {1 / promedio:F0} fps · {promedio * 1000:F1} ms (peor {peor * 1000:F0})", estilo);
        }
    }
}
