using UnityEngine;

namespace Argentina1860
{
    /// Personaje con el que se puede hablar. Mira al jugador cuando está cerca y respira en reposo.
    [RequireComponent(typeof(ModeloPersonaje))]
    public class NPC : MonoBehaviour
    {
        ModeloPersonaje modelo;
        float fase;

        public ModeloPersonaje Modelo => modelo != null ? modelo : (modelo = GetComponent<ModeloPersonaje>());
        public FichaPersonaje Ficha => Modelo.ficha;
        public string Nombre => Ficha != null ? Ficha.nombre : name;
        public string[] Lineas => Ficha != null && Ficha.lineas != null && Ficha.lineas.Length > 0 ? Ficha.lineas : new[] { "..." };
        /// Altura de la etiqueta con el nombre, sobre la cabeza.
        public float AltoEtiqueta => 2.3f * Modelo.Escala;

        void Start()
        {
            fase = Random.value * 10;
            Pisadas.Registrar(transform, 0.4f);
        }

        /// Lo mueve otro sistema (por ejemplo Pascual, que camina junto a su carreta): no gira hacia el jugador.
        public bool ControladoAfuera { get; set; }

        public void Actualizar(float dt, Vector3 jugador)
        {
            if (ControladoAfuera) return;
            var d = jugador - transform.position;
            d.y = 0;
            // solo se da vuelta hacia el jugador cuando conversa con él (antes todos lo seguían con la vista apenas se acercaba)
            if (CamaraDialogo.Interlocutor == transform && d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 240f * dt);
            fase += dt * 1.2f;
            var anim = Modelo.Anim;
            if (anim != null && CamaraDialogo.Interlocutor == transform) anim.Conversar(Time.time, CamaraDialogo.HablaElOtro);   // gesticula y se acomoda
            else anim?.Animar(fase, 0);
        }
    }
}
