using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Piezas compartidas de la interfaz: cuadros de esquinas redondeadas (se estiran sin deformar las esquinas gracias al
    /// borde de 9 partes de GUIStyle) y el aro de los mapas redondos.
    /// </summary>
    public static class Ui
    {
        static Texture2D papel, oscuro, pildora, capsula, disco, aro;

        /// Borde de 9 partes para los cuadros redondeados.
        public static RectOffset Borde => new RectOffset(14, 14, 14, 14);
        public static RectOffset BordePildora => new RectOffset(12, 12, 12, 12);
        public static RectOffset BordeCapsula => new RectOffset(16, 16, 16, 16);

        /// La interfaz se dibuja en un "mundo" de pixeles virtuales y se agranda según el alto de la pantalla, para que el texto se lea igual en cualquier resolución.
        public static float Escala => Mathf.Clamp(Screen.height / 720f, 1f, 2.5f);
        public static float Ancho => Screen.width / Escala;
        public static float Alto => Screen.height / Escala;
        /// Ponerla al empezar cada OnGUI.
        public static Matrix4x4 Matriz { get { float e = Escala; return Matrix4x4.Scale(new Vector3(e, e, 1)); } }

        public static Texture2D Papel => papel != null ? papel : (papel = Redondeada(new Color(0.945f, 0.902f, 0.804f, 0.94f), 40, 14));
        public static Texture2D Oscuro => oscuro != null ? oscuro : (oscuro = Redondeada(new Color(0.06f, 0.05f, 0.04f, 0.6f), 40, 14));
        public static Texture2D Pildora => pildora != null ? pildora : (pildora = Redondeada(new Color(0.06f, 0.05f, 0.04f, 0.62f), 32, 12));
        /// Pastilla de 32 píxeles de alto, totalmente redonda en las puntas.
        public static Texture2D Capsula => capsula != null ? capsula : (capsula = Redondeada(new Color(0.06f, 0.05f, 0.04f, 0.62f), 32, 16));
        /// Círculo lleno (blanco: se tiñe con GUI.color).
        public static Texture2D Disco => disco != null ? disco : (disco = Redondeada(Color.white, 128, 64));
        /// Aro fino (blanco: se tiñe con GUI.color).
        public static Texture2D Aro
        {
            get
            {
                if (aro != null) return aro;
                const int n = 256; const float r = 126, g = 3;
                aro = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var c0 = new Vector2(n / 2f, n / 2f);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c0);
                        aro.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(g * 0.5f + 0.5f - Mathf.Abs(d - r + g * 0.5f))));
                    }
                aro.Apply();
                return aro;
            }
        }

        /// Rectángulo de esquinas redondeadas con radio r, de n × n píxeles, para usar con borde de r píxeles.
        public static Texture2D Redondeada(Color c, int n, float r)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var q = new Vector2(Mathf.Clamp(p.x, r, n - r), Mathf.Clamp(p.y, r, n - r));
                    float a = Mathf.Clamp01(r - Vector2.Distance(p, q) + 0.5f);
                    t.SetPixel(x, y, new Color(c.r, c.g, c.b, c.a * a));
                }
            t.Apply();
            return t;
        }
    }
}
