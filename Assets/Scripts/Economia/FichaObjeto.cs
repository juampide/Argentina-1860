using UnityEngine;

namespace Argentina1860
{
    /// Para qué sirve un objeto (ordena el morral y decide qué se puede hacer con él).
    public enum TipoObjeto { Mercaderia, Herramienta, Carnada, Pescado, Comida, Material }

    /// <summary>
    /// Ficha de un objeto que se puede llevar en el morral: mercadería de la pulpería, herramientas, carnada,
    /// pescados, comida y materiales. La edita el diseño. Tiene que estar en <c>Assets/Resources/Objetos</c>
    /// para que el juego la encuentre.
    /// </summary>
    [CreateAssetMenu(menuName = "Argentina 1860/Ficha de objeto")]
    public class FichaObjeto : ScriptableObject
    {
        [Tooltip("Identificador corto, sin espacios ni acentos (lo usan el código, las recetas y la partida guardada). No cambiarlo una vez en uso")]
        public string id;
        public string nombre;
        [TextArea(2, 4)] public string descripcion;
        public TipoObjeto tipo;
        [Tooltip("Grupo para las recetas: por ejemplo, 'pescado' sirve para cualquier receta que pida un pescado")]
        public string categoria;

        [Header("Peso y precio")]
        [Tooltip("Kilos (el morral tiene un límite)")]
        [Min(0)] public float peso = 0.5f;
        [Tooltip("Precio de referencia en pesos; la pulpería lo mueve con las noticias")]
        [Min(0)] public float precio = 5;
        [Tooltip("Don Ramón lo vende en la pulpería")]
        public bool pulperiaVende;
        [Tooltip("Don Ramón lo compra")]
        public bool pulperiaCompra;
        [Tooltip("Las noticias del camino mueven su precio (llegó la carreta, se cortó el camino...)")]
        public bool conNoticias;

        [Header("Comida")]
        [Tooltip("Cuánta hambre saca al comerlo (0 nada, 1 llena del todo). 0 = no se come")]
        [Range(0, 1)] public float alimenta;
        [Tooltip("Lo que dice el Gallego al comerlo (vacío = aviso genérico)")]
        public string alComer;

        [Header("Pesca (solo pescados)")]
        [Tooltip("Pica en la laguna")]
        public bool enLaguna;
        [Tooltip("Pica en el arroyo")]
        public bool enArroyo;
        [Tooltip("Cuánto pica comparado con los otros (0 nunca, 1 normal, 3 muy común)")]
        [Range(0, 3)] public float abundancia = 1;
        [Tooltip("Cuánto tira al sacarlo (0 se deja, 1 pelea mucho)")]
        [Range(0, 1)] public float fuerza = 0.4f;
        [Tooltip("Cuándo pica más")]
        public HoraPesca hora;
        [Tooltip("Nivel de pesca desde el que empieza a picar")]
        [Min(1)] public int nivelMinimo = 1;

        public bool SeCome => alimenta > 0;
    }

    /// Cuándo pica más un pez.
    public enum HoraPesca
    {
        [InspectorName("A cualquier hora")] Siempre,
        [InspectorName("Al alba y al atardecer")] AlbaYAtardecer,
        [InspectorName("De noche")] Noche,
        [InspectorName("De día")] Dia,
    }
}
