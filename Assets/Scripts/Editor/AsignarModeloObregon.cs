using UnityEditor;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Asigna el modelo de Blender (Assets/Modelos/Personajes/Obregon.fbx) a la ficha de Obregón con las herramientas de Unity
    /// (no a mano en el archivo). Corre solo si la ficha todavía no tiene modelo; también está en el menú.
    /// </summary>
    static class AsignarModeloObregon
    {
        const string FICHA = "Assets/Datos/Personajes/obregon.asset";
        const string MODELO = "Assets/Modelos/Personajes/Obregon.fbx";

        [InitializeOnLoadMethod]
        static void Automatico() => EditorApplication.delayCall += () => { if (!Application.isPlaying) Asignar(false); };

        [MenuItem("Argentina 1860/Personajes/Asignar modelo de Blender a Obregón")]
        static void Menu() => Asignar(true);

        static void Asignar(bool forzar)
        {
            var ficha = AssetDatabase.LoadAssetAtPath<FichaPersonaje>(FICHA);
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(MODELO);
            if (ficha == null || modelo == null) { if (forzar) Debug.LogWarning("[Obregón] Falta la ficha o el FBX."); return; }

            bool falta = ficha.modeloBlender == null || !ficha.conversacionCine || ficha.aspecto.expresion == Expresion.Neutra;
            if (!falta && !forzar) return;
            var so = new SerializedObject(ficha);
            so.FindProperty("modeloBlender").objectReferenceValue = modelo;
            so.FindProperty("conversacionCine").boolValue = true;
            if (ficha.aspecto.expresion == Expresion.Neutra) so.FindProperty("aspecto.expresion").enumValueIndex = (int)Expresion.Cansada;   // militar, correcto y cansado     // las conversaciones con él usan la cámara de cine (planos general, medio y corto)
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ficha);
            AssetDatabase.SaveAssets();
            Debug.Log("[Obregón] Ficha actualizada: modelo de Blender y cámara de cine en sus conversaciones.");
        }
    }
}
