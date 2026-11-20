using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Datos de Buenos Aires tomados del plano del Departamento Topográfico de 1867 (prototipo: ciudad.json y ciudad.pak).
    /// Las coordenadas del plano son píxeles (0,675 m cada uno); se pasan a metros del prototipo con la Plaza de la Victoria en (0, 0),
    /// x hacia el este y z hacia el sur. En Unity la z cambia de signo (ver Conv).
    /// </summary>
    public class DatosCiudad
    {
        [Serializable] class Plano { public float ancho, alto, metro; public float[] plaza, marco; }
        [Serializable] public class Iglesia { public float x, y, ux, uy, largo, ancho, frente; }
        [Serializable] class Json { public Plano plano; public int manzanas, arboles; public Iglesia[] iglesias; }

        public const float ESC = 0.675f;   // metros por píxel del plano
        public float Ancho, Alto;          // tamaño del plano en píxeles
        public Vector2 Plaza;              // Plaza de la Victoria en píxeles
        public Vector4 Marco;              // zona dibujada del plano (x0, y0, x1, y1) en píxeles
        public float[] manzanas;           // 10 floats por manzana: 4 esquinas (x, y) y dos valores más
        public float[] arboles;            // 3 floats por árbol: x, y, tamaño
        public Iglesia[] iglesias;

        public int CantManzanas => manzanas.Length / 10;
        public int CantArboles => arboles.Length / 3;

        /// Píxeles del plano a metros del prototipo (x este, z sur), con la plaza en el origen.
        public Vector2 AMundo(float px, float py) => new Vector2((px - Plaza.x) * ESC, (py - Plaza.y) * ESC);

        public static DatosCiudad Cargar(TextAsset json, TextAsset pak)
        {
            var j = JsonUtility.FromJson<Json>(json.text);
            var d = new DatosCiudad
            {
                Ancho = j.plano.ancho, Alto = j.plano.alto,
                Plaza = new Vector2(j.plano.plaza[0], j.plano.plaza[1]),
                Marco = new Vector4(j.plano.marco[0], j.plano.marco[1], j.plano.marco[2], j.plano.marco[3]),
                iglesias = j.iglesias,
            };
            var partes = LeerPak(pak.bytes);
            d.manzanas = Floats(partes["manzanas"]);
            d.arboles = Floats(partes["arboles"]);
            return d;
        }

        [Serializable] class Parte { public string nombre; public int bytes, registro; }
        [Serializable] class Cabecera { public Parte[] partes; }

        /// ciudad.pak: gzip con una cabecera JSON y partes guardadas "transpuestas" (byte k de cada registro junto) para comprimir mejor.
        static Dictionary<string, byte[]> LeerPak(byte[] comprimido)
        {
            byte[] buf;
            using (var gz = new GZipStream(new MemoryStream(comprimido), CompressionMode.Decompress))
            using (var ms = new MemoryStream()) { gz.CopyTo(ms); buf = ms.ToArray(); }
            int n = BitConverter.ToInt32(buf, 0);
            var cab = JsonUtility.FromJson<Cabecera>(System.Text.Encoding.UTF8.GetString(buf, 4, n));
            int off = 4 + n;
            var salida = new Dictionary<string, byte[]>();
            foreach (var p in cab.partes)
            {
                int c = p.bytes / p.registro;
                var s = new byte[p.bytes];
                for (int e = 0; e < p.registro; e++)
                {
                    int t = off + e * c;
                    for (int k = 0; k < c; k++) s[k * p.registro + e] = buf[t + k];
                }
                salida[p.nombre] = s;
                off += p.bytes;
            }
            return salida;
        }

        static float[] Floats(byte[] b)
        {
            var f = new float[b.Length / 4];
            Buffer.BlockCopy(b, 0, f, 0, f.Length * 4);
            return f;
        }
    }

    /// Agua del Río de la Plata según la máscara del plano (rojo fuerte y poco verde = agua).
    public class MascaraAgua
    {
        readonly Color32[] px;
        readonly int w, h;
        readonly DatosCiudad d;

        public MascaraAgua(Texture2D tex, DatosCiudad datos)
        {
            px = tex.GetPixels32(); w = tex.width; h = tex.height; d = datos;
        }

        public int W => w;
        public int H => h;

        /// ¿Es agua el píxel (ix, iy) de la máscara? iy cuenta desde arriba, como en el canvas del prototipo.
        public bool AguaPx(int ix, int iy)
        {
            if (ix < 0 || iy < 0 || ix >= w || iy >= h) return true;
            var c = px[(h - 1 - iy) * w + ix];
            return c.r > 170 && c.g < 110;
        }

        /// ¿Hay agua en el punto (x, z) del prototipo?
        public bool EnAgua(float x, float z)
        {
            float pxl = x / DatosCiudad.ESC + d.Plaza.x, pyl = z / DatosCiudad.ESC + d.Plaza.y;
            return AguaPx(Mathf.FloorToInt(pxl / d.Ancho * w), Mathf.FloorToInt(pyl / d.Alto * h));
        }
    }

    /// <summary>
    /// Obstáculos de la ciudad (manzanas, iglesias, monumentos, carros) en una rejilla espacial, y altura del piso
    /// (las veredas están 16 cm más altas). Equivale a colision.js del prototipo; coordenadas del prototipo.
    /// </summary>
    public class CampoCiudad
    {
        const float CELDA = 64;
        class Poli { public Vector2[] p; public float minx, maxx, minz, maxz, h; }
        readonly List<Poli> obst = new List<Poli>(), alturas = new List<Poli>();
        readonly Dictionary<long, List<int>> rejO = new Dictionary<long, List<int>>(), rejA = new Dictionary<long, List<int>>();
        public MascaraAgua agua;

        static long Clave(int cx, int cz) => cx * 100003L + cz;

        static Poli Crear(Vector2[] pts, float h = 0)
        {
            var o = new Poli { p = pts, h = h, minx = float.MaxValue, minz = float.MaxValue, maxx = float.MinValue, maxz = float.MinValue };
            foreach (var q in pts) { o.minx = Mathf.Min(o.minx, q.x); o.maxx = Mathf.Max(o.maxx, q.x); o.minz = Mathf.Min(o.minz, q.y); o.maxz = Mathf.Max(o.maxz, q.y); }
            return o;
        }

        static void Indexar(Dictionary<long, List<int>> rej, Poli o, int id)
        {
            for (int cx = Mathf.FloorToInt(o.minx / CELDA); cx <= Mathf.FloorToInt(o.maxx / CELDA); cx++)
                for (int cz = Mathf.FloorToInt(o.minz / CELDA); cz <= Mathf.FloorToInt(o.maxz / CELDA); cz++)
                {
                    var k = Clave(cx, cz);
                    if (!rej.TryGetValue(k, out var l)) rej[k] = l = new List<int>();
                    l.Add(id);
                }
        }

        public void Registrar(Vector2[] pts) { var o = Crear(pts); Indexar(rejO, o, obst.Count); obst.Add(o); }
        public void Rect(float x0, float z0, float x1, float z1) => Registrar(new[] { new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1) });
        public void RegistrarAltura(Vector2[] pts, float h) { var o = Crear(pts, h); Indexar(rejA, o, alturas.Count); alturas.Add(o); }

        static bool Dentro(Poli o, float x, float z)
        {
            if (x < o.minx || x > o.maxx || z < o.minz || z > o.maxz) return false;
            bool c = false;
            var p = o.p;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > z) != (p[j].y > z) && x < (p[j].x - p[i].x) * (z - p[i].y) / (p[j].y - p[i].y) + p[i].x) c = !c;
            return c;
        }

        public bool Ocupado(float x, float z)
        {
            if (!rejO.TryGetValue(Clave(Mathf.FloorToInt(x / CELDA), Mathf.FloorToInt(z / CELDA)), out var l)) return false;
            foreach (var id in l) if (Dentro(obst[id], x, z)) return true;
            return false;
        }

        public bool EnAgua(float x, float z) => agua != null && agua.EnAgua(x, z);

        const float R = 0.7f;
        public bool Libre(float x, float z) =>
            !(Ocupado(x, z) || Ocupado(x + R, z) || Ocupado(x - R, z) || Ocupado(x, z + R) || Ocupado(x, z - R)) && !EnAgua(x, z);

        public float Altura(float x, float z)
        {
            if (!rejA.TryGetValue(Clave(Mathf.FloorToInt(x / CELDA), Mathf.FloorToInt(z / CELDA)), out var l)) return 0;
            foreach (var id in l) if (Dentro(alturas[id], x, z)) return alturas[id].h;
            return 0;
        }

        /// Recorre los polígonos de obstáculos (para dibujar el mapa).
        public IEnumerable<Vector2[]> Obstaculos() { foreach (var o in obst) yield return o.p; }
    }
}
