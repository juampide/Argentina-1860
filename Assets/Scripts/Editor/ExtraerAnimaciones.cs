using System.IO;
using UnityEditor;
using UnityEngine;

namespace Argentina1860.Editor
{
    /// Convierte los FBX de Assets/Modelos/Animaciones (animaciones de Mixamo pasadas al esqueleto del personaje con Arte/blender/retarget_mixamo.py)
    /// en clips sueltos en Assets/Resources/Animaciones/<Nombre>.anim, que es lo que carga el juego.
    /// El nombre del clip sale del archivo: Obregon_Hablar.fbx -> "Hablar". Se ejecuta solo al abrir Unity o recompilar, y si falta algún clip.
    public static class ExtraerAnimaciones
    {
        const string ORIGEN = "Assets/Modelos/Animaciones", DESTINO = "Assets/Resources/Animaciones";

        [InitializeOnLoadMethod]
        static void Auto() { EditorApplication.delayCall += () => Extraer(false); }

        [MenuItem("Argentina/Extraer animaciones de Mixamo")]
        public static void Manual() { Extraer(true); }

        static void Extraer(bool todo)
        {
            if (!Directory.Exists(ORIGEN)) return;
            Directory.CreateDirectory(DESTINO);
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ORIGEN }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                int guion = nombre.IndexOf('_');
                string clipNombre = guion >= 0 ? nombre.Substring(guion + 1) : nombre;
                string destino = DESTINO + "/" + clipNombre + ".anim";
                if (!todo && File.Exists(destino) && File.GetLastWriteTimeUtc(destino) >= File.GetLastWriteTimeUtc(ruta)) continue;

                var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (imp != null && (imp.animationType != ModelImporterAnimationType.Generic || imp.importAnimation == false || imp.materialImportMode != ModelImporterMaterialImportMode.None))
                {
                    imp.animationType = ModelImporterAnimationType.Generic;
                    imp.importAnimation = true;
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.SaveAndReimport();
                }
                AnimationClip mejor = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview__") && (mejor == null || c.length > mejor.length)) mejor = c;
                if (mejor == null) { Debug.LogWarning("ExtraerAnimaciones: " + ruta + " no trae animación"); continue; }

                var copia = Object.Instantiate(mejor);
                copia.name = clipNombre;
                var aj = AnimationUtility.GetAnimationClipSettings(copia);
                aj.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(copia, aj);
                copia.wrapMode = WrapMode.Loop;
                AssetDatabase.DeleteAsset(destino);
                AssetDatabase.CreateAsset(copia, destino);
                Debug.Log("ExtraerAnimaciones: " + destino + " (" + mejor.length.ToString("0.0") + " s)");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
