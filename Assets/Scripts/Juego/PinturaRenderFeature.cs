using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Argentina1860
{
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
