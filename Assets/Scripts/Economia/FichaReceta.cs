using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// Dónde se hace una receta.
    public enum LugarReceta
    {
        [InspectorName("En cualquier lado")] Cualquiera,
        [InspectorName("Junto a un fogón encendido")] Fogon,
        [InspectorName("En el banco del rancho")] Banco,
        [InspectorName("En la herrería")] Herreria,
    }

    /// Lo que pide o da una receta: un objeto concreto, o cualquiera de una categoría (por ejemplo, "un pescado").
    [Serializable]
    public class Ingrediente
    {
        [Tooltip("Un objeto concreto (dejar vacío si vale cualquiera de la categoría)")]
        public FichaObjeto objeto;
        [Tooltip("Cualquier objeto de esta categoría (por ejemplo 'pescado'); se usa solo si no hay objeto")]
        public string categoria;
        [Min(1)] public int cantidad = 1;

        public bool Sirve(FichaObjeto o) => o != null && (objeto != null ? o == objeto || o.id == objeto.id : !string.IsNullOrEmpty(categoria) && o.categoria == categoria);
        public string Nombre => objeto != null ? objeto.nombre : categoria;
    }

    /// <summary>
    /// Ficha de una receta: qué se hace, con qué, dónde y quién la enseña. La edita el diseño.
    /// Tiene que estar en <c>Assets/Resources/Recetas</c>.
    ///
    /// Las recetas se aprenden: si tiene <see cref="ensena"/>, ese personaje la enseña al hablar con él
    /// (después de sus líneas de siempre dice <see cref="alEnsenar"/>). Las que el Gallego ya sabe al llegar
    /// llevan <see cref="sabidaDesdeElPrincipio"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Argentina 1860/Receta")]
    public class FichaReceta : ScriptableObject
    {
        public string id;
        public string nombre;
        [TextArea(2, 4)] public string descripcion;
        public LugarReceta lugar = LugarReceta.Fogon;
        public List<Ingrediente> ingredientes = new List<Ingrediente>();
        public FichaObjeto resultado;
        [Min(1)] public int cantidad = 1;
        [Tooltip("Segundos que lleva hacerla")]
        [Min(0)] public float segundos = 6;

        [Header("Cómo se aprende")]
        [Tooltip("El Gallego ya la sabe al llegar al pago")]
        public bool sabidaDesdeElPrincipio;
        [Tooltip("Personaje que la enseña (su ficha)")]
        public FichaPersonaje ensena;
        [Tooltip("Lo que dice el personaje al enseñarla")]
        [TextArea(2, 5)] public string[] alEnsenar;
    }
}
