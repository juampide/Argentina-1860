using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Conversión desde las coordenadas del prototipo en three.js a Unity.
    /// three.js es de mano derecha con z hacia el sur; Unity es de mano izquierda con z hacia el norte.
    /// Regla única: (x, y, z) del prototipo → (x, y, -z) en Unity. Los ángulos sobre X e Y cambian de signo.
    /// Así el mundo queda igual (sin espejarse) y el norte del mapa queda hacia +z.
    /// </summary>
    public static class Conv
    {
        public static Vector3 P(float x, float y, float z) => new Vector3(x, y, -z);
        public static Vector3 P(Vector3 p) => new Vector3(p.x, p.y, -p.z);

        /// Rotación con ángulos de Euler del prototipo (radianes, orden XYZ de three.js), ya convertida a Unity.
        public static Quaternion Rot(float rx, float ry, float rz) =>
            Quaternion.AngleAxis(-rx * Mathf.Rad2Deg, Vector3.right) *
            Quaternion.AngleAxis(-ry * Mathf.Rad2Deg, Vector3.up) *
            Quaternion.AngleAxis(rz * Mathf.Rad2Deg, Vector3.forward);

        public static Quaternion Rot(Vector3 r) => Rot(r.x, r.y, r.z);

        /// Giro sobre el eje vertical del prototipo (rotation.y en radianes) convertido a Unity.
        public static Quaternion Yaw(float ry) => Quaternion.AngleAxis(-ry * Mathf.Rad2Deg, Vector3.up);

        /// Rotación en el espacio del prototipo (sin convertir), para armar geometría antes de pasarla a Unity.
        public static Quaternion Q3(Vector3 r) =>
            Quaternion.AngleAxis(r.x * Mathf.Rad2Deg, Vector3.right) *
            Quaternion.AngleAxis(r.y * Mathf.Rad2Deg, Vector3.up) *
            Quaternion.AngleAxis(r.z * Mathf.Rad2Deg, Vector3.forward);

        /// "#rrggbb" a Color (sRGB). Es C# puro: se puede usar en inicializadores de campos serializados.
        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.magenta;
            var s = hex.TrimStart('#');
            if (s.Length == 3) s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
            if (s.Length != 6 || !int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out int n)) return Color.magenta;
            return new Color(((n >> 16) & 255) / 255f, ((n >> 8) & 255) / 255f, (n & 255) / 255f, 1f);
        }

        public static string Clave(Color c)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255), 0, 255), g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255), 0, 255), b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255), 0, 255);
            return "#" + r.ToString("x2") + g.ToString("x2") + b.ToString("x2");
        }

        /// THREE.Color.multiplyScalar: multiplica en espacio lineal.
        public static Color Aclara(Color c, float f)
        {
            var l = c.linear;
            return new Color(Mathf.Clamp01(l.r * f), Mathf.Clamp01(l.g * f), Mathf.Clamp01(l.b * f)).gamma;
        }

        /// THREE.Color.setHSL: en three.js r170 el resultado queda en espacio lineal; acá se pasa a sRGB para Unity.
        public static Color HSL(float h, float s, float l)
        {
            h = ((h % 1f) + 1f) % 1f;
            s = Mathf.Clamp01(s); l = Mathf.Clamp01(l);
            if (s == 0) return new Color(l, l, l).gamma;
            float p = l <= 0.5f ? l * (1 + s) : l + s - l * s;
            float q = 2 * l - p;
            return new Color(Hue(q, p, h + 1f / 3f), Hue(q, p, h), Hue(q, p, h - 1f / 3f)).gamma;
        }

        static float Hue(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1f / 6f) return p + (q - p) * 6 * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * 6 * (2f / 3f - t);
            return p;
        }

        /// Math.round de JavaScript (Mathf.Round redondea al par).
        public static int RoundJS(float x) => (int)System.Math.Floor(x + 0.5);
    }
}
