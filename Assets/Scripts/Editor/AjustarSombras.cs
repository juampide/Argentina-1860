using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Argentina1860
{
    /// <summary>
    /// Ajusta las sombras del sol (URP) para que no vibren con el sol bajo de invierno: mapa más grande, menos distancia
    /// (así cada píxel de sombra cubre menos terreno) y bias un poco más firme. Corre solo una vez; también está en el menú.
    /// </summary>
    static class AjustarSombras
    {
        const string Marca = "Argentina1860.SombrasAjustadas.v1";

        [InitializeOnLoadMethod]
        static void Automatico() => EditorApplication.delayCall += () => { if (!Application.isPlaying && !EditorPrefs.GetBool(Marca, false)) Aplicar(false); };

        [MenuItem("Argentina 1860/Estilo/Ajustar sombras (sol bajo)")]
        static void Menu() => Aplicar(true);

        static void Aplicar(bool avisar)
        {
            var pipe = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipe == null) return;
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                bool pc = asset.name.Contains("PC");
                var so = new SerializedObject(asset);
                so.FindProperty("m_MainLightShadowmapResolution").intValue = pc ? 4096 : 2048;
                so.FindProperty("m_ShadowDistance").floatValue = pc ? 90f : 70f;
                so.FindProperty("m_ShadowDepthBias").floatValue = 1.5f;
                so.FindProperty("m_ShadowNormalBias").floatValue = 2.0f;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            EditorPrefs.SetBool(Marca, true);
            Debug.Log("[Sombras] Ajustadas: mapa más grande, distancia 90 m (PC).");
        }
    }
}
