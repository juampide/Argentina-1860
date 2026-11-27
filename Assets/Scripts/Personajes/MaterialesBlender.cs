using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Cambia los materiales lisos de un modelo hecho en Blender (colores simples) por los del juego: tela con trama y mugre, cuero gastado, piel.
    /// El oro y los ojos se dejan como vienen. Mismo material "de detalle" que usan los personajes armados por código.
    /// </summary>
    public static class MaterialesBlender
    {
        static readonly System.Collections.Generic.Dictionary<string, Material> conTextura = new System.Collections.Generic.Dictionary<string, Material>();

        /// Si hay una imagen horneada en Blender (Assets/Resources/Personajes/<nombre del material>.png), material con esa textura.
        static Material Textura(string nombre)
        {
            if (conTextura.TryGetValue(nombre, out var m) && m != null) return m;
            var tex = Resources.Load<Texture2D>("Personajes/" + nombre);
            var sh = Shader.Find("Argentina/Personaje");
            if (tex == null || sh == null) return null;
            m = new Material(sh) { name = nombre + " (textura)", hideFlags = HideFlags.DontSave };
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_UsarMapa", 1f);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Suciedad", 0.15f);
            conTextura[nombre] = m;
            return m;
        }

        public static void Aplicar(GameObject modelo)
        {
            Paleta.Detalle = true;
            try
            {
                foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m == null) continue;
                        if (m.name.Contains("_oro") || m.name.Contains("_ojo")) continue;
                        var nuevo = Textura(m.name) ?? Paleta.Mat("#" + ColorUtility.ToHtmlStringRGB(m.color));
                        if (nuevo != null) mats[i] = nuevo;
                    }
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                    if (r is SkinnedMeshRenderer sk) sk.updateWhenOffscreen = true;   // los límites del esqueleto importado a veces quedan chicos
                }
            }
            finally { Paleta.Detalle = false; }
        }
    }
}
