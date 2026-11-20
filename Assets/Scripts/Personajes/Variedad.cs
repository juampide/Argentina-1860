using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Gente sin nombre que no se repite: arma el aspecto de un tipo de personaje (gaucho, paisana, señora,
    /// caballero, soldado, cura, vendedor, gente de la toldería) eligiendo al azar la piel, el pelo, la barba,
    /// la contextura, el alto, los colores de época, el sombrero y la variante de la ropa. Con la misma semilla
    /// sale siempre la misma persona. Lo usan los fogones, la toldería y los transeúntes de la ciudad.
    /// Los colores son de tintes de la época: tierras, azules índigo, colorados de grana, grises, negros y crudos.
    /// </summary>
    public static class Variedad
    {
        // pieles de la campaña y de la ciudad: criollos, mestizos, afroargentinos, inmigrantes
        static readonly string[] PIELES = { "#f0c19a", "#e8b48c", "#d9a27a", "#d29a72", "#c48a62", "#b97d58", "#a06a48", "#8a5a3c", "#6e4630", "#5a3826" };
        static readonly string[] PELOS = { "#141010", "#1d1714", "#2e221a", "#3a2a1c", "#4a3a2a", "#6a4a2a", "#8a6a40", "#7a7670", "#a9a9a4" };
        static readonly string[] PONCHOS = { "#7a2e22", "#5a4632", "#3e4a5a", "#8a6a3a", "#4a3a2e", "#6e5a44", "#2f3a2e", "#2a2a30", "#8a3a2a", "#c8b48e" };
        static readonly string[] FRANJAS = { "#efe6d0", "#2a1c14", "#c0392b", "#2f3a55", "#c8a050" };
        static readonly string[] CHIRIPAS = { "#2f3a55", "#5a2a22", "#3a3a32", "#4a3a2a", "#2a2a2a", "#6a4a3a" };
        static readonly string[] SOMBREROS = { "#2e2620", "#3a2a20", "#5a4a38", "#6a5a48", "#2a2a2a" };
        static readonly string[] POLLERAS = { "#5a4a3e", "#3e3a44", "#6a4a3a", "#4a5048", "#7a6450", "#3a3a3a", "#5a3a3a", "#2f3a4a", "#6a5a3a" };
        static readonly string[] REBOZOS = { "#d9cfb8", "#8a3a2a", "#2f3a4a", "#c8b48e", "#5a4a3a", "#e6dcc8" };
        static readonly string[] PANUELOS = { "#d9cfb8", "#7a2a22", "#2f3a4a", "#c8a050", "#efe6d6" };
        static readonly string[] SEDAS = { "#5a2a3a", "#2f4a5a", "#3a4a32", "#6a3a2a", "#2a2a3a", "#7a5a6a", "#4a3a5a", "#8a6a4a", "#1f1a1c" };
        static readonly string[] ENCAJES = { "#efe6d6", "#e6dcc8", "#f4eedc", "#1d1a1c" };
        static readonly string[] PANOS = { "#2a2622", "#2c3e6b", "#3d4a3a", "#4a3a2a", "#3a3036", "#5a4a3a", "#1f1d1c" };
        static readonly string[] CHALECOS = { "#d9b45a", "#c4cfa4", "#e8d9a0", "#8a2a22", "#2f3a2e", "#e0d0b0" };
        static readonly string[] PANTALONES = { "#8a8478", "#a09a90", "#6e6d73", "#5a5048", "#c8c0a8" };

        static float Al(System.Random r) => (float)r.NextDouble();
        static string Uno(System.Random r, string[] l) => l[r.Next(l.Length)];
        static Color H(System.Random r, string[] l) => Conv.Hex(Uno(r, l));
        static bool P(System.Random r, float p) => r.NextDouble() < p;

        static void Cuerpo(System.Random r, Aspecto a, float alto0 = 0.93f, float alto1 = 1.07f)
        {
            a.piel = H(r, PIELES);
            a.pelo = H(r, PELOS);
            a.escala = Mathf.Lerp(alto0, alto1, Al(r));
            a.contextura = Mathf.Lerp(0.88f, 1.16f, Al(r) * Al(r) + Al(r) * 0.3f);   // la mayoría flacos
        }

        static void CaraDeHombre(System.Random r, Aspecto a, float pBarba = 0.25f)
        {
            float b = Al(r);
            a.bigote = b < 0.35f ? Bigote.Fino : b < 0.55f ? Bigote.Manubrio : Bigote.Ninguno;
            a.barba = P(r, pBarba);
        }

        public static Aspecto Gaucho(System.Random r)
        {
            var a = new Aspecto { estilo = Estilo.Poncho, poncho = H(r, PONCHOS), chaleco = H(r, FRANJAS), ropa = H(r, CHIRIPAS) };
            Cuerpo(r, a);
            CaraDeHombre(r, a, 0.3f);
            a.ponchoAlHombro = P(r, 0.45f);
            a.variante = r.Next(4);
            float s = Al(r);
            a.sombrero = s < 0.5f ? Sombrero.Ala : s < 0.68f ? Sombrero.Gorra : s < 0.8f ? Sombrero.Vincha : s < 0.9f ? Sombrero.Panuelo : Sombrero.Ninguno;
            a.colorSombrero = a.sombrero == Sombrero.Vincha || a.sombrero == Sombrero.Panuelo ? H(r, PANUELOS) : H(r, SOMBREROS);
            float pe = Al(r);
            a.peinado = pe < 0.5f ? Peinado.Largo : pe < 0.8f ? Peinado.Corto : Peinado.Puas;
            if (P(r, 0.05f)) a.accesorio = Accesorio.Guitarra;
            return a;
        }

        public static Aspecto Paisana(System.Random r)
        {
            var a = new Aspecto { estilo = Estilo.Vestido, ropa = H(r, POLLERAS), ropa2 = H(r, REBOZOS), chaleco = H(r, FRANJAS) };
            Cuerpo(r, a, 0.88f, 0.98f);
            a.variante = r.Next(4);
            if (P(r, 0.45f)) { a.sombrero = Sombrero.Panuelo; a.colorSombrero = H(r, PANUELOS); }
            float pe = Al(r);
            a.peinado = pe < 0.4f ? Peinado.Trenzas : pe < 0.7f ? Peinado.Rodete : Peinado.Largo;
            if (P(r, 0.12f)) a.accesorio = Accesorio.Canasta;
            return a;
        }

        public static Aspecto Senora(System.Random r)
        {
            var a = new Aspecto { estilo = Estilo.Mirinaque, ropa = H(r, SEDAS), ropa2 = H(r, ENCAJES), chaleco = H(r, SEDAS) };
            Cuerpo(r, a, 0.88f, 0.98f);
            a.piel = Conv.Hex(PIELES[r.Next(5)]);   // en la clase alta porteña predominaban los criollos de origen europeo
            a.variante = r.Next(4);
            if (P(r, 0.4f)) a.sombrero = Sombrero.Mantilla;
            a.peinado = Peinado.Rodete;
            return a;
        }

        public static Aspecto Caballero(System.Random r)
        {
            var a = new Aspecto { estilo = Estilo.Levita, ropa = H(r, PANOS), chaleco = H(r, CHALECOS), pantalon = H(r, PANTALONES) };
            Cuerpo(r, a);
            a.piel = Conv.Hex(PIELES[r.Next(5)]);
            CaraDeHombre(r, a, 0.35f);
            a.variante = r.Next(3);
            float s = Al(r);
            a.sombrero = s < 0.5f ? Sombrero.Copa : s < 0.8f ? Sombrero.Ala : Sombrero.Ninguno;
            a.colorSombrero = H(r, SOMBREROS);
            a.peinado = P(r, 0.6f) ? Peinado.Corto : Peinado.Canoso;
            a.baston = P(r, 0.2f);
            return a;
        }

        public static Aspecto Soldado(System.Random r)
        {
            var a = new Aspecto { estilo = Estilo.Soldado, ropa = Conv.Hex(P(r, 0.7f) ? "#2f3a55" : "#3a4258"), ropa2 = H(r, PONCHOS), sombrero = Sombrero.Kepi, colorSombrero = Conv.Hex("#3a3a48") };
            Cuerpo(r, a);
            CaraDeHombre(r, a, 0.2f);
            a.pantalon = Conv.Hex(P(r, 0.6f) ? "#c8c0a8" : "#2f3a55");
            float v = Al(r);
            a.variante = v < 0.4f ? 0 : v < 0.65f ? 1 : v < 0.9f ? 2 : 3;
            if (P(r, 0.35f)) a.accesorio = Accesorio.Fusil;
            return a;
        }

        public static Aspecto Cura(System.Random r)
        {
            var a = new Aspecto { estilo = Estilo.Sotana, sombrero = P(r, 0.6f) ? Sombrero.Teja : Sombrero.Bonete };
            Cuerpo(r, a);
            a.peinado = Peinado.Canoso;
            return a;
        }

        public static Aspecto Vendedor(System.Random r)
        {
            var a = new Aspecto { estilo = Estilo.Pregonero, ropa = H(r, CHIRIPAS), ropa2 = Conv.Hex("#efe6d0"), pantalon = H(r, PANTALONES), sombrero = P(r, 0.6f) ? Sombrero.Gorra : Sombrero.Ala, colorSombrero = H(r, SOMBREROS) };
            Cuerpo(r, a, 0.75f, 1.02f);
            CaraDeHombre(r, a);
            float c = Al(r);
            a.accesorio = c < 0.3f ? Accesorio.Canasta : c < 0.5f ? Accesorio.Barril : c < 0.65f ? Accesorio.Cajon : Accesorio.Ninguno;
            return a;
        }

        // gente de la toldería
        static readonly string[] PIELES_TA = { "#a8704a", "#9a6440", "#b47c52", "#8c5a3a", "#c08a5e" };
        static readonly string[] MANTOS = { "#8a6a48", "#7a5a3c", "#a07850", "#6a4e36", "#9a8060", "#5a4430" };
        static readonly string[] BORDES = { "#e6dcc8", "#d8c8a8", "#c8b48e", "#efe6d6" };
        static readonly string[] PINTURAS = { "#7a2a22", "#2f3a4a", "#c8a050", "#5a2a1a", "#3a2a22" };
        static readonly string[] VINCHAS = { "#e8e0d0", "#c0392b", "#d8d0c0", "#7a2a22", "#2f3a4a" };

        public static Aspecto Indigena(System.Random r, bool mujer, bool chico)
        {
            var a = new Aspecto
            {
                estilo = Estilo.Quillango, piel = H(r, PIELES_TA), pelo = Conv.Hex(P(r, 0.85f) ? "#141010" : "#2e221a"),
                ropa = H(r, MANTOS), ropa2 = H(r, BORDES), chaleco = H(r, PINTURAS), poncho = H(r, PONCHOS),
                pantalon = Conv.Hex(P(r, 0.5f) ? "#5a4a3a" : "#7a2a22"),
                peinado = mujer ? Peinado.Trenzas : Peinado.Largo,
                sombrero = mujer ? Sombrero.Ninguno : Sombrero.Vincha, colorSombrero = H(r, VINCHAS),
            };
            a.escala = chico ? 0.6f + Al(r) * 0.14f : mujer ? 0.9f + Al(r) * 0.07f : 0.98f + Al(r) * 0.1f;
            a.contextura = Mathf.Lerp(0.95f, 1.18f, Al(r));
            a.variante = r.Next(3);
            if (!mujer && !chico) a.bigote = P(r, 0.15f) ? Bigote.Fino : Bigote.Ninguno;
            return a;
        }

        /// Alguien que pasa por la calle de la ciudad: de todas las clases.
        public static Aspecto Transeunte(System.Random r)
        {
            float t = Al(r);
            return t < 0.24f ? Caballero(r) : t < 0.42f ? Senora(r) : t < 0.6f ? Paisana(r) : t < 0.74f ? Gaucho(r)
                 : t < 0.86f ? Vendedor(r) : t < 0.94f ? Soldado(r) : Cura(r);
        }
    }
}
