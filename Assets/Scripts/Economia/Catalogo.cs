using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Todos los objetos y recetas del juego, leídos de las fichas en <c>Resources/Objetos</c> y <c>Resources/Recetas</c>.
    /// Se cargan la primera vez que se usan.
    /// </summary>
    public static class Catalogo
    {
        public const string CARPETA_OBJETOS = "Objetos", CARPETA_RECETAS = "Recetas";

        static Dictionary<string, FichaObjeto> objetos;
        static List<FichaReceta> recetas;

        static void Asegurar()
        {
            if (objetos != null) return;
            Usar(Resources.LoadAll<FichaObjeto>(CARPETA_OBJETOS), Resources.LoadAll<FichaReceta>(CARPETA_RECETAS));
        }

        /// Reemplaza el catálogo (lo usan los tests para no depender de las fichas del diseño).
        public static void Usar(IEnumerable<FichaObjeto> objs, IEnumerable<FichaReceta> recs)
        {
            objetos = new Dictionary<string, FichaObjeto>();
            foreach (var o in objs)
            {
                if (o == null || string.IsNullOrEmpty(o.id)) continue;
                if (objetos.ContainsKey(o.id)) { Debug.LogWarning($"Catálogo: hay dos objetos con id '{o.id}' ({objetos[o.id].name} y {o.name})."); continue; }
                objetos[o.id] = o;
            }
            recetas = recs.Where(r => r != null).ToList();
        }

        /// Vuelve a leer las fichas la próxima vez (después de un test, o si el diseño agregó fichas en Play).
        public static void Olvidar() { objetos = null; recetas = null; }

        public static IEnumerable<FichaObjeto> Objetos { get { Asegurar(); return objetos.Values; } }
        public static IReadOnlyList<FichaReceta> Recetas { get { Asegurar(); return recetas; } }

        public static FichaObjeto Buscar(string id)
        {
            Asegurar();
            return id != null && objetos.TryGetValue(id, out var o) ? o : null;
        }

        public static IEnumerable<FichaReceta> RecetasEn(LugarReceta lugar) => Recetas.Where(r => r.lugar == lugar);
        public static IEnumerable<FichaReceta> QueEnsena(string idPersonaje) => Recetas.Where(r => r.ensena != null && r.ensena.id == idPersonaje);
    }
}
