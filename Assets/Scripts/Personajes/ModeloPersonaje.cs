using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Muestra un personaje armado por código. Si tiene ficha, usa el aspecto de la ficha; si no, el aspecto propio.
    /// Cambiar el aspecto en el Inspector rearma el modelo al instante.
    /// </summary>
    public class ModeloPersonaje : ProceduralBase
    {
        public FichaPersonaje ficha;
        [Tooltip("Se usa solo si no hay ficha")]
        public Aspecto aspecto = new Aspecto();

        public AnimPersonaje Anim { get; private set; }
        public Aspecto AspectoActual => ficha != null ? ficha.aspecto : aspecto;
        public float Escala => AspectoActual.escala;

        protected override void Construir(Transform raiz)
        {
            Anim = ConstructorPersonaje.Construir(raiz, AspectoActual, Registrar);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Anim = null;
        }
    }
}
