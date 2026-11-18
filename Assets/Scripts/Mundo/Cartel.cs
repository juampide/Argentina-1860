using TMPro;
using UnityEngine;

namespace Argentina1860
{
    /// Cartel pintado a mano: tabla clara con borde y letras rojas (cartel() de campo.js). Mira hacia -Z local.
    public class Cartel : ProceduralBase
    {
        public string texto = "LA SOLEDAD";
        [Min(0.5f)] public float ancho = 3.4f;
        public bool tabla = true;
        public Color colorLetras = Conv.Hex("#7a1d14");

        protected override void Construir(Transform raiz)
        {
            float alto = ancho / 4;
            if (tabla)
            {
                var n = new Nodo("tabla");
                n.Pieza(Geo.Caja(ancho, alto, 0.04f), "#e8dcc0", Vector3.zero);
                n.Pieza(Geo.Caja(ancho + 0.06f, 0.05f, 0.05f), "#4a3220", new Vector3(0, alto / 2, 0));
                n.Pieza(Geo.Caja(ancho + 0.06f, 0.05f, 0.05f), "#4a3220", new Vector3(0, -alto / 2, 0));
                n.Pieza(Geo.Caja(0.05f, alto, 0.05f), "#4a3220", new Vector3(ancho / 2, 0, 0));
                n.Pieza(Geo.Caja(0.05f, alto, 0.05f), "#4a3220", new Vector3(-ancho / 2, 0, 0));
                n.Instanciar(raiz, Registrar);
            }
            Letras(raiz, texto, ancho * 0.9f, alto * 0.8f, colorLetras, -0.035f);
        }

        /// Texto 3D que se lee desde -Z local.
        public static TextMeshPro Letras(Transform padre, string texto, float ancho, float alto, Color color, float z)
        {
            var go = new GameObject("letras");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = new Vector3(0, 0, z);
            var t = go.AddComponent<TextMeshPro>();
            t.text = texto;
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.1f;
            t.fontSizeMax = 40;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.rectTransform.sizeDelta = new Vector2(ancho, alto);
            t.characterSpacing = 8;
            return t;
        }
    }
}
