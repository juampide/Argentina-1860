using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Argentina1860
{
    /// <summary>
    /// Agrega el efecto de pintura (PinturaRenderFeature) a los renderers de URP del proyecto, con las herramientas de Unity
    /// (no a mano en el archivo). Se corre solo una vez al abrir el proyecto si falta; también está en el menú.
    /// </summary>
    static class InstalarPintura
    {
        [InitializeOnLoadMethod]
        static void Automatico() => EditorApplication.delayCall += () => { if (!Application.isPlaying) Agregar(false); };

        [MenuItem("Argentina 1860/Estilo/Agregar pintura al renderer")]
        static void Menu() => Agregar(true);

        static void Agregar(bool avisar)
        {
            var pipe = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipe == null) return;
            var lista = new SerializedObject(pipe).FindProperty("m_RendererDataList");
            int nuevos = 0;
            for (int i = 0; i < lista.arraySize; i++)
            {
                var datos = lista.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                if (datos == null) continue;
                bool ya = false;
                foreach (var f in datos.rendererFeatures) if (f is PinturaRenderFeature) ya = true;
                if (ya) continue;
                var feature = ScriptableObject.CreateInstance<PinturaRenderFeature>();
                feature.name = "Pintura";
                AssetDatabase.AddObjectToAsset(feature, datos);
                var so = new SerializedObject(datos);
                var features = so.FindProperty("m_RendererFeatures");
                var mapa = so.FindProperty("m_RendererFeatureMap");
                int n = features.arraySize;
                features.arraySize = n + 1;
                features.GetArrayElementAtIndex(n).objectReferenceValue = feature;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id);
                mapa.arraySize = n + 1;
                mapa.GetArrayElementAtIndex(n).longValue = id;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(datos);
                nuevos++;
            }
            if (nuevos > 0) { AssetDatabase.SaveAssets(); Debug.Log("[Pintura] Efecto agregado a " + nuevos + " renderer(s) de URP."); }
            else if (avisar) Debug.Log("[Pintura] Ya estaba agregado.");
        }
    }
}
