#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Herramienta de desarrollo (solo en el editor): con F9 en Play pone al Teniente Obregón modelado en Blender
    /// (Assets/Modelos/Personajes/Obregon.fbx) frente al jugador, con el material de detalle del juego. F10 cambia la pose:
    /// reposo, caminando, corriendo, sentado, cocinando, a caballo. Sirve para probar el esqueleto. Otro F9 lo saca.
    /// </summary>
    public class ObregonPrueba : MonoBehaviour
    {
        const string RUTA = "Assets/Modelos/Personajes/Obregon.fbx";
        [Tooltip("Obregón mide 1,69 m sin kepí; los personajes de código, 1,74 m, pero con sombreros y cabezas grandes parecen más altos")]
        public float escala = 1.10f;
        GameObject actual;
        AnimPersonaje anim;
        int pose;
        static readonly string[] NOMBRES = { "reposo", "caminando", "corriendo", "sentado", "cocinando", "a caballo" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Arrancar()
        {
            if (FindAnyObjectByType<ObregonPrueba>() == null) new GameObject("Obregón de prueba (F9 pone, F10 cambia pose)").AddComponent<ObregonPrueba>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f9Key.wasPressedThisFrame) Alternar();
            if (kb.f10Key.wasPressedThisFrame && actual != null) { pose = (pose + 1) % NOMBRES.Length; Debug.Log("[Obregón] pose: " + NOMBRES[pose]); }
            if (anim == null) return;
            float t = Time.time;
            switch (pose)
            {
                case 0: anim.Animar(t, 0); break;
                case 1: anim.Animar(t * 5f, 1); break;
                case 2: anim.Animar(t * 8f, 1.35f); break;
                case 3: anim.Sentado(t, 0.5f + 0.5f * Mathf.Sin(t * 0.8f), 0, 0.44f); break;
                case 4: anim.Cocinando(t, 0.45f); break;
                case 5: anim.Jinete(t * 5f, 0.8f, 0, t); break;
            }
        }

        void Alternar()
        {
            if (actual != null) { Destroy(actual); actual = null; anim = null; return; }
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA);
            if (modelo == null) { Debug.LogWarning("[Obregón] Falta " + RUTA + " (se genera desde Blender con Arte/blender/obregon_cuerpo.py -- --exportar)."); return; }
            var jugador = FindAnyObjectByType<ControlJugador>();
            Vector3 desde = jugador != null ? jugador.transform.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            Vector3 adelante = Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized : Vector3.forward;
            if (adelante.sqrMagnitude < 0.01f) adelante = Vector3.forward;
            actual = Instantiate(modelo, desde + adelante * 2.8f, Quaternion.LookRotation(-adelante, Vector3.up));
            actual.name = "Obregón (Blender)";
            actual.transform.localScale = Vector3.one * escala;
            MaterialesBlender.Aplicar(actual);
            anim = AnimPersonaje.DesdeHuesos(actual.transform);
            pose = 0;
            Debug.Log("[Obregón] Puesto a 2,8 m frente a la cámara. F10 cambia la pose (" + string.Join(", ", NOMBRES) + "); F9 lo saca." + (anim == null ? " (sin esqueleto reconocido)" : ""));
        }
    }
}
#endif
