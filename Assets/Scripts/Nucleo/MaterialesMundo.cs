using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Materiales con nombre del mundo (adobe, paja, teja, madera...). Vive en Resources/MaterialesMundo.asset
    /// y lo crea el generador del pago. El diseño puede cambiar texturas y colores acá sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Argentina 1860/Materiales del mundo")]
    public class MaterialesMundo : ScriptableObject
    {
        [Serializable]
        public class Entrada
        {
            public string nombre;
            public Material material;
        }

        [Tooltip("Material base (URP Lit, mate, de dos caras) del que salen los colores lisos.")]
        public Material baseColor;
        public List<Entrada> materiales = new List<Entrada>();

        Dictionary<string, Material> indice;
        static MaterialesMundo instancia;

        public static MaterialesMundo Instancia
        {
            get
            {
                if (instancia == null) instancia = Resources.Load<MaterialesMundo>("MaterialesMundo");
                return instancia;
            }
        }

        void OnEnable() { indice = null; }
        void OnValidate() { indice = null; }

        /// Rehace el índice de nombres (después de agregar materiales por código).
        public void Invalidar() { indice = null; }

        public Material Buscar(string nombre)
        {
            if (indice == null)
            {
                indice = new Dictionary<string, Material>();
                foreach (var e in materiales)
                    if (e != null && !string.IsNullOrEmpty(e.nombre) && e.material != null) indice[e.nombre] = e.material;
            }
            return indice.TryGetValue(nombre, out var m) ? m : null;
        }
    }

    /// <summary>
    /// Devuelve el material para una clave: un nombre de MaterialesMundo ("adobe") o un color ("#7d5f40").
    /// Los colores lisos se crean en memoria a partir del material base y se reutilizan.
    /// </summary>
    public static class Paleta
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        /// Si es true, los colores lisos ("#rrggbb") salen con el material de personaje (manchas, trama de tela y mugre) en vez del liso.
        public static bool Detalle;
        static readonly HashSet<string> sinDetalle = new HashSet<string> { "#ece4d0", "#17100c", "#1b130f" };   // ojos

        static Material MatDetalle(string clave)
        {
            string k = "detalle|" + clave;
            if (cache.TryGetValue(k, out var c) && c != null) return c;
            var sh = Shader.Find("Argentina/Personaje");
            if (sh == null) return null;
            var m = new Material(sh) { name = k, hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", Conv.Hex(clave));
            cache[k] = m;
            return m;
        }

        public static Material Mat(string clave)
        {
            var mm = MaterialesMundo.Instancia;
            if (mm != null)
            {
                var m = mm.Buscar(clave);
                if (m != null) return m;
            }
            if (Detalle && clave.StartsWith("#") && !sinDetalle.Contains(clave)) { var md = MatDetalle(clave); if (md != null) return md; }
            if (cache.TryGetValue(clave, out var c) && c != null) return c;

            // "base*#rrggbb": copia del material con nombre "base" teñida con ese color (paredes pastel, tejas de distinto tono)
            int estrella = clave.IndexOf('*');
            if (estrella > 0)
            {
                var b = Mat(clave.Substring(0, estrella));
                var t = new Material(b) { name = clave, hideFlags = HideFlags.DontSave };
                t.color = b.color * Conv.Hex(clave.Substring(estrella + 1));
                cache[clave] = t;
                return t;
            }

            Material mat;
            if (mm != null && mm.baseColor != null) mat = new Material(mm.baseColor);
            else
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetFloat("_Smoothness", 0f);
                mat.SetFloat("_Cull", 0f);
            }
            mat.color = clave.StartsWith("#") ? Conv.Hex(clave) : Color.magenta;
            mat.name = clave;
            mat.hideFlags = HideFlags.DontSave;
            cache[clave] = mat;
            return mat;
        }
    }
}
