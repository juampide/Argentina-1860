using UnityEngine;
using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Cámara isométrica ortogonal a 45° de inclinación y 45° de giro, como en el prototipo.
    /// Q / R giran la vista de a 90°, la rueda del mouse acerca o aleja.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CamaraIsometrica : MonoBehaviour
    {
        public Transform objetivo;
        [Tooltip("Mitad de la altura visible, en metros")]
        public float vista = 13;
        public float vistaMin = 6, vistaMax = 45;
        public float distancia = 70;
        [Tooltip("Radio del hueco tramado en árboles y cercos que tapan al jugador (metros)")]
        public float radioRecorte = 5.2f;
        [Tooltip("Cuánto se borran en el centro del hueco (1 = del todo; 0,75 deja ver el árbol a medias, como un fantasma)")]
        [Range(0, 1)] public float transparenciaRecorte = 0.75f;
        static readonly int idXrayJug = Shader.PropertyToID("_XrayJug"), idXrayDir = Shader.PropertyToID("_XrayDir"), idXrayRad = Shader.PropertyToID("_XrayRad"), idXrayMax = Shader.PropertyToID("_XrayMax");

        float yaw = Mathf.PI * 0.75f, yawVis, vistaAct;
        Vector3 foco;
        bool listo;
        Camera cam;

        /// Giro actual de la vista (radianes, en la convención del prototipo).
        public float YawVis => yawVis;
        /// Punto donde mirar en lugar del objetivo (por ejemplo, entre dos personajes que hablan).
        public Vector3? FocoForzado { get; set; }
        /// Vista forzada (por ejemplo, más cerca durante un diálogo).
        public float? VistaForzada { get; set; }
        /// Apaga el recorte tramado de árboles y cercos (mientras la cámara de diálogo está en uso).
        public bool RecorteApagado { get; set; }

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            listo = false;
        }

        /// Ubica la cámara de golpe sobre el objetivo (sin suavizado). Sirve en modo edición.
        [ContextMenu("Ubicar sobre el objetivo")]
        public void Ubicar()
        {
            cam = GetComponent<Camera>();
            listo = false;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (objetivo == null) return;
            float dt = Application.isPlaying ? Time.deltaTime : 1;
            if (!listo)
            {
                yawVis = yaw; vistaAct = vista; foco = objetivo.position; listo = true;
            }

            if (Application.isPlaying && Keyboard.current != null && VistaForzada == null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame) yaw -= Mathf.PI / 2;
                if (Keyboard.current.rKey.wasPressedThisFrame) yaw += Mathf.PI / 2;
                if (Mouse.current != null)
                {
                    float s = Mouse.current.scroll.ReadValue().y;
                    if (Mathf.Abs(s) > 10) s /= 120f;
                    vista = Mathf.Clamp(vista - s * 1.2f, vistaMin, vistaMax);
                }
            }

            yawVis += (yaw - yawVis) * Mathf.Min(1, dt * 7);
            float objetivoVista = VistaForzada ?? vista;
            vistaAct += (objetivoVista - vistaAct) * Mathf.Min(1, dt * 8);
            var destino = FocoForzado ?? objetivo.position;
            foco.x += (destino.x - foco.x) * Mathf.Min(1, dt * 6);
            foco.z += (destino.z - foco.z) * Mathf.Min(1, dt * 6);
            float py = objetivo.position.y;
            const float pitch = Mathf.PI / 4;
            float cp = Mathf.Cos(pitch);
            // la cámara está lejos y alta: no choca con nada
            var pos = new Vector3(foco.x + Mathf.Cos(yawVis) * cp * distancia, py + 1 + Mathf.Sin(pitch) * distancia, foco.z - Mathf.Sin(yawVis) * cp * distancia);
            var mira = new Vector3(foco.x, py + 1, foco.z);

            // recorte tramado de árboles y cercos que tapan al jugador (shader Argentina/Mundo)
            var pj = objetivo.position;
            Shader.SetGlobalVector(idXrayJug, new Vector4(pj.x, pj.y + 1, pj.z, pj.y));
            Shader.SetGlobalVector(idXrayDir, (mira - pos).normalized);
            Shader.SetGlobalFloat(idXrayRad, Application.isPlaying && !RecorteApagado ? radioRecorte : 0);
            Shader.SetGlobalFloat(idXrayMax, transparenciaRecorte);
            // en modo edición solo se toca la cámara si cambió algo, para no marcar la escena como modificada
            if (!Application.isPlaying && (pos - transform.position).sqrMagnitude < 1e-4f && Mathf.Abs(cam.orthographicSize - vistaAct) < 1e-3f) return;
            cam.orthographic = true;
            cam.orthographicSize = vistaAct;
            transform.position = pos;
            transform.LookAt(mira);
        }
    }
}
