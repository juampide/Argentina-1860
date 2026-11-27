using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Ficha de un personaje con nombre: quién es, cómo se ve y qué dice.
    /// La edita el diseño; más adelante la va a leer también el servicio de NPC con LLM
    /// (personalidad, lo que sabe y lo que no puede saber en 1867).
    /// </summary>
    [CreateAssetMenu(menuName = "Argentina 1860/Ficha de personaje")]
    public class FichaPersonaje : ScriptableObject
    {
        public string id;
        public string nombre;
        [Tooltip("Dónde vive o trabaja")]
        public string lugar;
        public Aspecto aspecto = new Aspecto();

        [Header("Modelo propio (Blender)")]
        [Tooltip("Si tiene un modelo (FBX con esqueleto de Arte/blender), el personaje se ve con él en vez del armado por código. El Aspecto se sigue usando para el resto.")]
        public GameObject modeloBlender;
        [Tooltip("Escala del modelo propio: Obregón mide 1,69 m; los personajes de código, 1,74 m")]
        public float escalaModelo = 1.10f;

        [Header("Cámara")]
        [Tooltip("Las conversaciones con este personaje usan la cámara de cine (planos general, medio y corto). Si no está tildada, quedan con la cámara isométrica. Ver Docs/guion/cinematicas.md")]
        public bool conversacionCine;

        [Header("Diálogo (provisorio)")]
        [TextArea(2, 5)] public string[] lineas;

        [Header("Para el NPC con LLM (a diseñar)")]
        [TextArea(3, 8)] public string personalidad;
        [TextArea(3, 8)] public string sabe;
        [TextArea(3, 8)] public string noSabe;
    }
}
