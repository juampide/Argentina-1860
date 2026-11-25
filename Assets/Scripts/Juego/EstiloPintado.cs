using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

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

    /// <summary>Se agrega al renderer de URP (menú Argentina 1860 → Estilo → Agregar pintura al renderer).</summary>
    public class PinturaRenderFeature : ScriptableRendererFeature
    {
        PinturaPass pase;
        Material material;

        public override void Create()
        {
            var sh = Shader.Find("Hidden/Argentina/Pintura");
            if (sh == null) { Debug.LogWarning("PinturaRenderFeature: falta el shader Hidden/Argentina/Pintura."); return; }
            material = CoreUtils.CreateEngineMaterial(sh);
            pase = new PinturaPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing, material = material };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!EstiloPintado.Activo || pase == null || material == null) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            renderer.EnqueuePass(pase);
        }

        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); }

        class PinturaPass : ScriptableRenderPass
        {
            public Material material;

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var recursos = frameData.Get<UniversalResourceData>();
                if (recursos.isActiveTargetBackBuffer) return;   // hace falta una textura intermedia
                var origen = recursos.activeColorTexture;
                var desc = renderGraph.GetTextureDesc(origen);
                desc.name = "_Pintura";
                desc.clearBuffer = false;
                var destino = renderGraph.CreateTexture(desc);
                material.SetFloat("_Radio", EstiloPintado.Radio);
                material.SetFloat("_Intensidad", EstiloPintado.Intensidad);
                material.SetFloat("_Tinta", EstiloPintado.Tinta);
                material.SetFloat("_Grano", EstiloPintado.Grano);
                material.SetFloat("_Vineta", EstiloPintado.Vineta);
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(origen, destino, material, 0), "Pintura");
                recursos.cameraColor = destino;
            }
        }
    }
}
