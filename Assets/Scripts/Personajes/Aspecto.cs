using System;
using UnityEngine;

namespace Argentina1860
{
    public enum Estilo { Levita, Vestido, Sotana, Poncho, Pregonero }
    public enum Sombrero { Ninguno, Copa, Ala, Bonete, Kepi, Gorra }
    public enum Peinado { SegunEstilo, Puas, Corto, Canoso, Largo, Rodete }
    public enum Bigote { Ninguno, Fino, Manubrio }
    public enum Accesorio { Ninguno, Fusil, Canasta, Barril, Guitarra, Cajon }

    /// <summary>
    /// Aspecto de un personaje low poly: sin cara (solo la nariz); la identidad está en el sombrero,
    /// el pelo, el bigote y la ropa. Mismos parámetros que crearPersonaje() del prototipo.
    /// </summary>
    [Serializable]
    public class Aspecto
    {
        public Estilo estilo = Estilo.Levita;
        public Color piel = Conv.Hex("#f0c19a");
        public Color pelo = Conv.Hex("#3a2a1c");
        public Peinado peinado = Peinado.SegunEstilo;
        public bool barba;
        public Bigote bigote = Bigote.Ninguno;

        [Header("Ropa")]
        [Tooltip("Levita, vestido, chaleco del pregonero, kepi")]
        public Color ropa = Conv.Hex("#2f3f66");
        [Tooltip("Camisa, chal, roquete")]
        public Color ropa2 = Conv.Hex("#f4eedc");
        public Color chaleco = Conv.Hex("#d9b45a");
        public Color pantalon = Conv.Hex("#8a8478");
        public Color poncho = Conv.Hex("#c0392b");

        [Header("Sombrero y accesorios")]
        public Sombrero sombrero = Sombrero.Ninguno;
        public Color colorSombrero = Conv.Hex("#3b2f27");
        public bool baston;
        public Accesorio accesorio = Accesorio.Ninguno;
        [Range(0.5f, 1.3f)] public float escala = 1;

        public Peinado PeinadoReal => peinado != Peinado.SegunEstilo ? peinado
            : estilo == Estilo.Vestido ? Peinado.Rodete
            : estilo == Estilo.Poncho ? Peinado.Largo
            : estilo == Estilo.Sotana ? Peinado.Canoso
            : Peinado.Puas;
    }
}
