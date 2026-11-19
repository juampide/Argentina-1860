using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    public enum UsoLote { CampoAbierto, Casco, Quinta, Chacra, Potrero, Posta, SinOcupar, Monte }
    public enum Linde { Ninguno, Mojones, Zanja, CercoVivo, Existente }
    public enum TipoCamino { Real, Callejon, Senda, Huella }

    /// <summary>
    /// Loteo del pago: de quién es cada pedazo de tierra, para qué se usa, cómo se marca el límite y por dónde
    /// van los caminos. Es la base del mundo: los caminos, el pasto, las zanjas, las tranqueras y los sembrados
    /// salen de acá. Vive en Resources/LoteoPago.asset y lo edita el diseño (ver Docs/mundo/loteo-pago.md).
    /// Coordenadas en km del plano, como en MapaPago: x hacia el este y y hacia el sur, de (0, 0) a (3,2; 2,4).
    /// </summary>
    [CreateAssetMenu(menuName = "Argentina 1860/Loteo del pago")]
    public class LoteoPago : ScriptableObject
    {
        [Serializable]
        public class Lote
        {
            public string nombre;
            [Tooltip("id de la ficha del dueño (vacío si no tiene)")]
            public string duenio;
            public UsoLote uso;
            public Linde linde;
            [Tooltip("Vértices del lote en km, en orden alrededor del borde")]
            public Vector2[] km;
            [Tooltip("Tranqueras sobre el borde, en km (cortan la zanja)")]
            public Vector2[] tranqueras = new Vector2[0];
        }

        [Serializable]
        public class Camino
        {
            public string nombre;
            public TipoCamino tipo;
            [Tooltip("Puntos del recorrido en km")]
            public Vector2[] km;
            [Tooltip("Al final hay un patio de tierra pisada (en la puerta de una casa, una tranquera, la orilla del agua)")]
            public bool patioAlFinal = true;
        }

        public List<Camino> caminos = new List<Camino>();
        public List<Lote> lotes = new List<Lote>();

        public static float MedioAncho(TipoCamino t) => t == TipoCamino.Real ? 3.5f : t == TipoCamino.Callejon ? 2.8f : t == TipoCamino.Senda ? 1.6f : 1.2f;

        static LoteoPago instancia;
        static bool buscado;

        public static LoteoPago Instancia
        {
            get
            {
                if (instancia == null && !buscado)
                {
                    buscado = true;
                    instancia = Resources.Load<LoteoPago>("LoteoPago");
                }
                return instancia;
            }
        }

        void OnEnable() { buscado = false; }
        void OnValidate() { MapaPago.Invalidar(); }

        // ---------- consultas ----------

        public Camino CaminoReal()
        {
            foreach (var c in caminos) if (c.tipo == TipoCamino.Real) return c;
            return null;
        }

        /// Lote que contiene el punto (en metros del prototipo). Null si no cae en ninguno.
        public Lote LoteEn(float x, float z)
        {
            var k = new Vector2(x / 1000f + 1.6f, z / 1000f + 1.2f);
            Lote r = null;
            foreach (var l in lotes)
                if (l.km != null && l.km.Length >= 3 && Dentro(k, l.km))
                    // si se pisan, gana el más chico (una quinta adentro del campo de la estancia)
                    if (r == null || Area(l.km) < Area(r.km)) r = l;
            return r;
        }

        public static bool Dentro(Vector2 p, Vector2[] poli)
        {
            bool c = false;
            for (int i = 0, j = poli.Length - 1; i < poli.Length; j = i++)
                if ((poli[i].y > p.y) != (poli[j].y > p.y) && p.x < (poli[j].x - poli[i].x) * (p.y - poli[i].y) / (poli[j].y - poli[i].y) + poli[i].x)
                    c = !c;
            return c;
        }

        public static float Area(Vector2[] poli)
        {
            float a = 0;
            for (int i = 0, j = poli.Length - 1; i < poli.Length; j = i++) a += (poli[j].x + poli[i].x) * (poli[j].y - poli[i].y);
            return Mathf.Abs(a / 2);
        }

        // ---------- loteo inicial ----------

        static Vector2[] V(params float[] f)
        {
            var r = new Vector2[f.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = new Vector2(f[i * 2], f[i * 2 + 1]);
            return r;
        }

        static Vector2[] Rect(float x0, float y0, float x1, float y1) => V(x0, y0, x1, y0, x1, y1, x0, y1);

        /// Altura (km, hacia el sur) del camino real en x, para que los lotes de chacra den frente al camino.
        static float YCamino(Vector2[] camino, float x)
        {
            for (int i = 0; i + 1 < camino.Length; i++)
                if (x >= camino[i].x && x <= camino[i + 1].x)
                    return Mathf.Lerp(camino[i].y, camino[i + 1].y, (x - camino[i].x) / (camino[i + 1].x - camino[i].x));
            return camino[camino.Length - 1].y;
        }

        /// Loteo de partida (ver Docs/mundo/loteo-pago.md). Se usa para crear el asset la primera vez.
        public void CargarPorDefecto()
        {
            caminos.Clear();
            lotes.Clear();
            var real = V(-0.05f, 1.50f, 0.40f, 1.46f, 0.80f, 1.38f, 1.15f, 1.30f, 1.60f, 1.29f, 2.00f, 1.305f, 2.30f, 1.29f, 2.70f, 1.16f, 3.25f, 1.00f);
            caminos.Add(new Camino { nombre = "Camino real", tipo = TipoCamino.Real, km = real, patioAlFinal = false });
            caminos.Add(new Camino { nombre = "Callejón de La Soledad", tipo = TipoCamino.Callejon, km = V(0.80f, 1.38f, 0.792f, 1.12f, 0.778f, 0.92f, 0.756f, 0.81f) });
            caminos.Add(new Camino { nombre = "Senda a los ranchos de peones", tipo = TipoCamino.Senda, km = V(0.756f, 0.812f, 0.70f, 0.845f, 0.64f, 0.875f, 0.613f, 0.89f) });
            caminos.Add(new Camino { nombre = "Senda a los potreros", tipo = TipoCamino.Senda, km = V(0.80f, 0.74f, 0.80f, 0.62f, 0.76f, 0.50f, 0.72f, 0.43f) });
            caminos.Add(new Camino { nombre = "Senda al rancho de Jacinta", tipo = TipoCamino.Senda, km = V(0.80f, 0.70f, 0.92f, 0.62f, 1.05f, 0.56f, 1.15f, 0.515f) });
            caminos.Add(new Camino { nombre = "Huella al rancho abandonado", tipo = TipoCamino.Huella, km = V(0.792f, 1.12f, 0.88f, 1.07f, 0.95f, 1.025f, 0.985f, 1.008f) });
            caminos.Add(new Camino { nombre = "Senda al fortín", tipo = TipoCamino.Senda, km = V(0.16f, 1.483f, 0.15f, 1.36f, 0.13f, 1.25f, 0.125f, 1.20f) });
            caminos.Add(new Camino { nombre = "Senda al puesto del baqueano", tipo = TipoCamino.Senda, km = V(0.52f, 1.443f, 0.53f, 1.60f, 0.54f, 1.74f, 0.545f, 1.825f) });
            caminos.Add(new Camino { nombre = "Entrada de la posta", tipo = TipoCamino.Huella, km = V(2.15f, 1.30f, 2.15f, 1.28f, 2.15f, 1.265f) });
            caminos.Add(new Camino { nombre = "Callejón al monte", tipo = TipoCamino.Callejon, km = V(2.255f, 1.296f, 2.258f, 1.00f, 2.265f, 0.78f, 2.28f, 0.66f) });
            caminos.Add(new Camino { nombre = "Callejón a la laguna", tipo = TipoCamino.Callejon, km = V(2.255f, 1.30f, 2.252f, 1.55f, 2.25f, 1.76f, 2.10f, 1.86f, 1.95f, 1.92f, 1.86f, 1.945f) });

            // ---- estancia La Soledad (noroeste, hasta el camino real y el arroyo) ----
            lotes.Add(new Lote { nombre = "Campo de La Soledad", duenio = "celestino", uso = UsoLote.CampoAbierto, linde = Linde.Mojones, km = V(0.0f, 0.0f, 1.30f, 0.0f, 1.20f, 0.55f, 1.12f, 1.05f, 1.12f, 1.30f, 0.80f, 1.37f, 0.40f, 1.45f, 0.0f, 1.49f) });
            lotes.Add(new Lote { nombre = "Casco de La Soledad", duenio = "celestino", uso = UsoLote.Casco, linde = Linde.Ninguno, km = Rect(0.66f, 0.70f, 0.85f, 0.83f) });
            lotes.Add(new Lote { nombre = "Quinta de La Soledad", duenio = "mercedes", uso = UsoLote.Quinta, linde = Linde.CercoVivo, km = Rect(0.58f, 0.68f, 0.68f, 0.78f), tranqueras = V(0.68f, 0.73f) });
            lotes.Add(new Lote { nombre = "Potreros", duenio = "celestino", uso = UsoLote.Potrero, linde = Linde.Existente, km = Rect(0.17f, 0.08f, 0.86f, 0.42f) });
            lotes.Add(new Lote { nombre = "Trigal de la estancia", duenio = "celestino", uso = UsoLote.Chacra, linde = Linde.Zanja, km = Rect(0.90f, 0.10f, 1.16f, 0.44f), tranqueras = V(1.03f, 0.44f) });
            lotes.Add(new Lote { nombre = "Maizal de la estancia", duenio = "celestino", uso = UsoLote.Chacra, linde = Linde.Zanja, km = Rect(0.28f, 0.52f, 0.54f, 0.82f), tranqueras = V(0.54f, 0.70f) });
            lotes.Add(new Lote { nombre = "Ranchos de peones", duenio = "celestino", uso = UsoLote.Casco, linde = Linde.Ninguno, km = Rect(0.55f, 0.86f, 0.64f, 0.95f) });

            // ---- campo abierto al sur del camino y monte ----
            lotes.Add(new Lote { nombre = "Campo abierto del sur", uso = UsoLote.CampoAbierto, linde = Linde.Ninguno, km = V(0.0f, 1.49f, 0.40f, 1.45f, 0.80f, 1.37f, 1.15f, 1.31f, 1.45f, 1.30f, 1.45f, 2.40f, 0.0f, 2.40f) });
            lotes.Add(new Lote { nombre = "Monte", uso = UsoLote.Monte, linde = Linde.Ninguno, km = V(1.98f, 0.30f, 2.35f, 0.22f, 2.62f, 0.38f, 2.60f, 0.62f, 2.30f, 0.68f, 2.02f, 0.58f) });

            // ---- chacras de la posta: lotes angostos y largos con frente al camino real ----
            void Chacra(string nombre, float x0, float x1, bool norte, float fondo, UsoLote uso = UsoLote.Chacra, Linde linde = Linde.Zanja)
            {
                float y0 = YCamino(real, x0), y1 = YCamino(real, x1), m = 0.012f;
                float xm = (x0 + x1) / 2, ym = YCamino(real, xm);
                var poli = norte ? V(x0, y0 - m, x1, y1 - m, x1, y1 - m - fondo, x0, y0 - m - fondo) : V(x0, y0 + m, x1, y1 + m, x1, y1 + m + fondo, x0, y0 + m + fondo);
                var tranq = linde == Linde.Zanja ? new[] { new Vector2(xm, norte ? ym - m : ym + m) } : new Vector2[0];
                lotes.Add(new Lote { nombre = nombre, uso = uso, linde = linde, km = poli, tranqueras = tranq });
            }
            Chacra("Chacra del trigal", 1.42f, 1.62f, true, 0.47f);
            Chacra("Lote sin ocupar", 1.62f, 1.80f, true, 0.47f, UsoLote.SinOcupar, Linde.Mojones);
            Chacra("Chacra del maizal", 1.80f, 2.05f, true, 0.47f);
            lotes.Add(new Lote { nombre = "Posta y pulpería", duenio = "cipriano", uso = UsoLote.Posta, linde = Linde.Ninguno, km = V(2.05f, 1.285f, 2.24f, 1.285f, 2.24f, 1.16f, 2.05f, 1.16f) });
            Chacra("Chacra de las parvas", 2.28f, 2.47f, true, 0.50f);
            Chacra("Chacra recién arada", 2.47f, 2.62f, true, 0.42f);
            Chacra("Chacra del trigo verde", 2.62f, 2.82f, true, 0.40f);
            Chacra("Chacra en barbecho", 2.82f, 2.98f, true, 0.36f, UsoLote.Chacra, Linde.Zanja);
            Chacra("Chacra del maíz nuevo", 1.46f, 1.66f, false, 0.44f);
            Chacra("Chacra del trigo brotando", 1.66f, 1.90f, false, 0.44f);
            Chacra("Lote del italiano", 1.90f, 2.05f, false, 0.44f, UsoLote.SinOcupar, Linde.Mojones);
            Chacra("Chacra del maíz sembrado", 2.28f, 2.50f, false, 0.40f);
            Chacra("Chacra del rastrojo de maíz", 2.50f, 2.72f, false, 0.42f);
            Chacra("Chacra del trigo maduro", 2.72f, 2.95f, false, 0.44f);
        }
    }
}
