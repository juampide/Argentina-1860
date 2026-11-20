using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Argentina1860
{
    /// <summary>
    /// La tecla 9 pasa del pago a Buenos Aires y viceversa (provisorio, para probar mientras no está el paso de la historia:
    /// el malón que arrasa el pago y manda al protagonista a la ciudad). Se instala solo; no hace falta ponerlo en las escenas.
    /// </summary>
    public class CambioDeMundo : MonoBehaviour
    {
        public const string PAGO = "Pago", CIUDAD = "BuenosAires";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Instalar()
        {
            var go = new GameObject("Cambio de mundo");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<CambioDeMundo>();
        }

        void Update()
        {
            if (!Teclas.Numero(Teclas.CAMBIO_DE_MUNDO)) return;
            var actual = SceneManager.GetActiveScene().name;
            var destino = actual == CIUDAD ? PAGO : CIUDAD;
            if (Application.CanStreamedLevelBeLoaded(destino)) SceneManager.LoadScene(destino);
            else Debug.LogWarning($"La escena {destino} no está en Build Settings.");
        }
    }
}
