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
        /// La cara con formas (gestos, parpadeo, boca al hablar), si el modelo es de Blender y las trae.
        public CaraBlender Cara { get; private set; }
        public Aspecto AspectoActual => ficha != null ? ficha.aspecto : aspecto;
        public float Escala => AspectoActual.escala;

        protected override void Construir(Transform raiz)
        {
            if (ficha != null && ficha.modeloBlender != null)
            {
                var inst = Instantiate(ficha.modeloBlender, raiz);
                inst.name = "modelo_blender";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one * ficha.escalaModelo;
                MaterialesBlender.Aplicar(inst);
                Anim = AnimPersonaje.DesdeHuesos(inst.transform);
                var cara = inst.AddComponent<CaraBlender>();
                Cara = cara.Iniciar(AspectoActual.expresion) ? cara : null;
                return;
            }
            Anim = ConstructorPersonaje.Construir(raiz, AspectoActual, Registrar);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Anim?.Liberar();
            Anim = null;
        }
    }
}
