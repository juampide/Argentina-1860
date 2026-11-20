using UnityEngine;

namespace Argentina1860
{
    /// Texturas pintadas por código para la ciudad (ciudad.js): atlas de vanos y arcos de recova. El generador las guarda como PNG.
    public static class TexturasCiudad
    {
        // pintor mínimo con y hacia abajo, como el canvas del prototipo
        class Lienzo
        {
            public readonly int w, h;
            public readonly Color32[] px;
            public Lienzo(int w, int h, Color32 fondo) { this.w = w; this.h = h; px = new Color32[w * h]; for (int i = 0; i < px.Length; i++) px[i] = fondo; }
            public void Px(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) px[(h - 1 - y) * w + x] = c; }
            public void Rect(float x, float y, float rw, float rh, Color32 c)
            {
                for (int j = Mathf.RoundToInt(y); j < Mathf.RoundToInt(y + rh); j++)
                    for (int i = Mathf.RoundToInt(x); i < Mathf.RoundToInt(x + rw); i++) Px(i, j, c);
            }
            public void Linea(float x0, float y0, float x1, float y1, float ancho, Color32 c)
            {
                int n = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)));
                for (int k = 0; k <= n; k++) { float t = n == 0 ? 0 : (float)k / n; Rect(Mathf.Lerp(x0, x1, t) - ancho / 2, Mathf.Lerp(y0, y1, t) - ancho / 2, ancho, ancho, c); }
            }
            public void Marco(float x, float y, float rw, float rh, float ancho, Color32 c)
            {
                Linea(x, y, x + rw, y, ancho, c); Linea(x + rw, y, x + rw, y + rh, ancho, c); Linea(x + rw, y + rh, x, y + rh, ancho, c); Linea(x, y + rh, x, y, ancho, c);
            }
            public void Disco(float cx, float cy, float r, Color32 c)
            {
                for (int j = Mathf.FloorToInt(cy - r); j <= Mathf.CeilToInt(cy + r); j++)
                    for (int i = Mathf.FloorToInt(cx - r); i <= Mathf.CeilToInt(cx + r); i++)
                        if ((i - cx) * (i - cx) + (j - cy) * (j - cy) <= r * r) Px(i, j, c);
            }
            public Texture2D Textura(string nombre)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = nombre, wrapMode = TextureWrapMode.Repeat };
                t.SetPixels32(px); t.Apply(true);
                return t;
            }
        }

        static Color32 C(string hex) => Conv.Hex(hex);
        static readonly Color32 NADA = new Color32(0, 0, 0, 0);

        /// Ventana con postigos, puerta de madera y ventana con balcón: 4 celdas de 256 × 512.
        public static Texture2D Atlas()
        {
            var c = new Lienzo(1024, 512, NADA);
            void Ventana(int x0, bool balcon)
            {
                c.Rect(x0 + 30, 70, 196, 420, C("#efe6d0"));                              // marco claro
                c.Rect(x0 + 56, 96, 144, 368, C("#1f2a2b"));                              // vidrio
                c.Linea(x0 + 128, 96, x0 + 128, 464, 3, C("#6b7a78")); c.Linea(x0 + 56, 230, x0 + 200, 230, 3, C("#6b7a78"));
                foreach (var sx in new[] { x0 + 56, x0 + 150 })                            // postigos verdes
                {
                    c.Rect(sx, 96, 50, 368, C("#3f6049"));
                    for (int y = 104; y < 460; y += 12) c.Linea(sx + 4, y, sx + 46, y, 2, C("#2a4233"));
                }
                c.Rect(x0 + 18, 56, 220, 18, C("#d9cdb2"));                               // cornisa
                c.Rect(x0 + 22, 490, 212, 14, C("#d9cdb2"));                              // alféizar
                var hierro = C("#22201d");
                if (balcon)
                {
                    c.Rect(x0, 400, 256, 12, C("#2b2824"));
                    for (int x = 6; x < 256; x += 14) c.Linea(x0 + x, 340, x0 + x, 400, 3, hierro);
                    c.Linea(x0, 340, x0 + 256, 340, 3, hierro);
                }
                else
                {
                    for (int x = 66; x < 200; x += 18) c.Linea(x0 + x, 240, x0 + x, 490, 3, hierro);
                    c.Linea(x0 + 56, 330, x0 + 200, 330, 3, hierro);
                }
            }
            Ventana(0, false);
            const int x1 = 256;   // puerta de madera con tableros y aldaba
            c.Rect(x1 + 20, 30, 216, 482, C("#e9dfc7"));
            c.Rect(x1 + 40, 60, 176, 452, C("#4a3220"));
            foreach (var (px, py, pw, ph) in new[] { (54, 76, 70, 170), (132, 76, 70, 170), (54, 266, 70, 220), (132, 266, 70, 220) }) c.Marco(x1 + px, py, pw, ph, 3, C("#2d1e12"));
            c.Disco(x1 + 118, 300, 6, C("#b08a3e"));
            c.Rect(x1 + 10, 20, 236, 16, C("#d6cbb0"));
            Ventana(512, true);
            return c.Textura("Vanos");
        }

        /// Arco de recova (una crujía) con transparencia; pisos = 1 o 2.
        public static Texture2D Arcada(int pisos)
        {
            const int W = 288, Hp = 400;
            var c = new Lienzo(W, Hp * pisos, C("#eadfc6"));
            c.Rect(0, 0, W, 14, C("#d4c7a8"));
            for (int p = 0; p < pisos; p++)
            {
                int y0 = (pisos - 1 - p) * Hp;   // el piso 0 es el de abajo
                float pie = y0 + Hp - (p == 0 ? 0 : 30);
                for (int y = y0; y < pie; y++)   // hueco del arco: pilares a los lados, medio punto arriba
                    for (int x = 48; x < 240; x++)
                    {
                        bool hueco = y >= y0 + 150 || (x - 144) * (x - 144) + (y - (y0 + 150)) * (y - (y0 + 150)) <= 96 * 96;
                        if (hueco) c.Px(x, y, NADA);
                    }
                c.Rect(0, y0 + 20, W, 8, C("#cdbf9f"));   // imposta
                if (p > 0)                                 // balaustrada en la galería alta
                {
                    c.Rect(46, y0 + Hp - 52, 196, 10, C("#e3d8bd"));
                    for (int x = 52; x < 240; x += 18) c.Rect(x, y0 + Hp - 44, 8, 30, C("#e3d8bd"));
                    c.Rect(46, y0 + Hp - 16, 196, 16, C("#e3d8bd"));
                }
            }
            return c.Textura("Arcada" + pisos);
        }
    }

    /// Carro de madera tirado por un caballo (construirCarros() de ciudad.js). Mira hacia +z del prototipo.
    public static class ModelosCiudad
    {
        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);

        public static Transform Carro(Transform padre, string carga)
        {
            var g = new Nodo("carro", v(0, 0, -0.5f));
            const string mad = "#8a5a34", mad2 = "#6f4527", rue = "#3f2b1c", hierro = "#2a2724";
            g.Pieza(Geo.Caja(1.35f, 0.14f, 2.1f), mad, v(0, 0.86f, -2.1f));
            foreach (var x in new[] { -0.69f, 0.69f }) g.Pieza(Geo.Caja(0.08f, 0.38f, 2.1f), mad2, v(x, 1.08f, -2.1f));
            g.Pieza(Geo.Caja(1.35f, 0.38f, 0.08f), mad2, v(0, 1.08f, -3.14f));
            g.Pieza(Geo.Caja(1.35f, 0.3f, 0.08f), mad2, v(0, 1.04f, -1.08f));
            foreach (var x in new[] { -0.82f, 0.82f })
            {
                g.Pieza(Geo.Cilindro(0.58f, 0.58f, 0.09f, 8), rue, v(x, 0.58f, -2.1f), v(0, 0, Mathf.PI / 2));
                g.Pieza(Geo.Caja(0.11f, 0.22f, 0.22f), hierro, v(x, 0.58f, -2.1f));
                for (int k = 0; k < 4; k++) g.Pieza(Geo.Caja(0.05f, 1.1f, 0.06f), rue, v(x, 0.58f, -2.1f), v(k * Mathf.PI / 4, 0, 0));   // rayos
            }
            g.Pieza(Geo.Caja(1.95f, 0.07f, 0.07f), hierro, v(0, 0.58f, -2.1f));                                  // eje
            foreach (var x in new[] { -0.46f, 0.46f }) g.Pieza(Geo.Caja(0.07f, 0.07f, 2.0f), mad, v(x, 0.95f, -0.2f), v(-0.06f, 0, 0));   // varas hacia el caballo
            if (carga == "heno")
            {
                g.Pieza(Geo.Caja(1.2f, 0.75f, 1.8f), "#e0c46a", v(0, 1.5f, -2.1f));
                g.Pieza(Geo.Octa(0.5f), "#d4b85c", v(0, 2.0f, -2.1f), default, v(1.2f, 0.6f, 1.5f));
            }
            else if (carga == "barriles")
                foreach (var (x, z) in new[] { (-0.32f, -1.7f), (0.32f, -1.7f), (0f, -2.6f) })
                {
                    g.Pieza(Geo.Cilindro(0.3f, 0.27f, 0.75f, 7), "#9a6a3c", v(x, 1.32f, z));
                    foreach (var y in new[] { -0.2f, 0.2f }) g.Pieza(Geo.Toro(0.29f, 0.012f, 4, 7), hierro, v(x, 1.32f + y, z), v(Mathf.PI / 2, 0, 0));
                }
            else
            {
                g.Pieza(Geo.Caja(0.85f, 0.55f, 0.85f), "#b58a52", v(-0.22f, 1.25f, -1.75f));
                g.Pieza(Geo.Caja(0.65f, 0.45f, 0.65f), "#a67a44", v(0.27f, 1.2f, -2.6f));
                g.Pieza(Geo.Caja(0.5f, 0.3f, 0.5f), "#c29a62", v(-0.2f, 1.7f, -1.75f));
            }
            g.Pieza(Geo.Caja(1.35f, 0.1f, 0.42f), "#3a5a7a", v(0, 1.5f, -1.3f));   // asiento
            return g.Instanciar(padre, null);
        }
    }
}
