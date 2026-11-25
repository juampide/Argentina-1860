using System;
using UnityEngine;

namespace Argentina1860
{
    public enum Estilo { Levita, Vestido, Sotana, Poncho, Pregonero, Quillango, Soldado, Mirinaque }
    public enum Sombrero { Ninguno, Copa, Ala, Bonete, Kepi, Gorra, Vincha, Panuelo, Mantilla, Teja }
    public enum Peinado { SegunEstilo, Puas, Corto, Canoso, Largo, Rodete, Trenzas }
    public enum Bigote { Ninguno, Fino, Manubrio }
    public enum Expresion { Neutra, Dura, Cansada, Amable, Triste, Enojado }
    public enum Accesorio { Ninguno, Fusil, Canasta, Barril, Guitarra, Cajon, Lanza }

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
        [Tooltip("Gesto de la cara: cejas, párpados y boca")]
        public Expresion expresion = Expresion.Neutra;

        [Header("Ropa")]
        [Tooltip("Levita, vestido, chaleco del pregonero, kepi")]
        public Color ropa = Conv.Hex("#2f3f66");
        [Tooltip("Camisa, chal, roquete")]
        public Color ropa2 = Conv.Hex("#f4eedc");
        public Color chaleco = Conv.Hex("#d9b45a");
        public Color pantalon = Conv.Hex("#8a8478");
        public Color poncho = Conv.Hex("#c0392b");
        [Tooltip("Gaucho: el poncho doblado sobre el hombro, así se ven el tirador, el facón y el chiripá")]
        public bool ponchoAlHombro;

        [Header("Sombrero y accesorios")]
        public Sombrero sombrero = Sombrero.Ninguno;
        public Color colorSombrero = Conv.Hex("#3b2f27");
        public bool baston;
        public Accesorio accesorio = Accesorio.Ninguno;
        [Range(0.5f, 1.3f)] public float escala = 1;
        [Tooltip("Flaco (0,85) a robusto (1,2)")]
        [Range(0.85f, 1.2f)] public float contextura = 1;
        [Tooltip("Variante de la ropa dentro del estilo (0 a 3): guardas del poncho, delantal o franja de la pollera, volados del miriñaque, levita o saco, uniforme, poncho encima o chiripá del soldado...")]
        [Range(0, 3)] public int variante;

        public Peinado PeinadoReal => peinado != Peinado.SegunEstilo ? peinado
            : estilo == Estilo.Vestido || estilo == Estilo.Mirinaque ? Peinado.Rodete
            : estilo == Estilo.Soldado ? Peinado.Corto
            : estilo == Estilo.Poncho || estilo == Estilo.Quillango ? Peinado.Largo
            : estilo == Estilo.Sotana ? Peinado.Canoso
            : Peinado.Puas;
    }
}
