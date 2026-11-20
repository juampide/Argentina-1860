using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    public enum Cultivo { Trigo, Maiz }

    /// Momento del ciclo del sembrado, de la tierra recién arada al rastrojo.
    public enum Etapa { Arado, Sembrado, Brotando, Creciendo, Maduro, Cosechado }

    /// <summary>
    /// Sembrado de una chacra, apoyado en el terreno. Se elige el cultivo, la etapa del ciclo y el tamaño en el
    /// Inspector, y la pieza se rearma sola. Pensado para campos grandes (cientos de metros):
    /// - El suelo y la masa del cultivo (el "manto") son superficies con textura de surcos o de hileras,
    ///   con relieve y fallas. Las plantas una por una solo se dibujan en la orilla, que es lo que se ve de costado.
    /// - Trigo: en 1867 se sembraba al voleo, sin hileras. Madura dorado; cosechado deja rastrojo y parvas.
    /// - Maíz: en surcos, con la planta alta. Cosechado quedan las cañas secas y quebradas.
    /// - La malla se parte en cuadros de 50 m para que solo se dibuje lo que entra en cámara.
    /// El pasto no crece adentro. Los materiales (surcos, trigo, maizal, rastrojo...) están en MaterialesMundo.
    /// </summary>
    public class Sembrado : ProceduralBase
    {
        public Cultivo cultivo = Cultivo.Trigo;
        public Etapa etapa = Etapa.Creciendo;
        [Tooltip("Ancho en metros (eje X del objeto). En el maíz los surcos van a lo largo (eje Z)")]
        [Min(4)] public float ancho = 40;
        [Tooltip("Largo en metros (eje Z del objeto)")]
        [Min(4)] public float largo = 60;
        [Tooltip("Fallas: manchones donde no nació nada (0 = parejo)")]
        [Range(0, 1)] public float fallas = 0.15f;
        [Tooltip("Ancho de la orilla donde se dibujan las plantas una por una, en metros")]
        [Range(0, 6)] public float orillaConPlantas = 1.5f;
        [Tooltip("Cambia la variación de alturas, fallas y parvas")]
        public int semilla = 1;

        // ---------- registro (para que el pasto no crezca adentro) ----------
        static readonly List<Sembrado> todos = new List<Sembrado>();

        protected override void OnEnable()
        {
            if (!todos.Contains(this)) todos.Add(this);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            todos.Remove(this);
            base.OnDisable();
        }

        /// ¿El punto (en coordenadas del prototipo: x este, z sur) cae adentro de algún sembrado?
        public static bool Ocupa(float x, float z)
        {
            if (todos.Count == 0) return false;
            var w = new Vector3(x, 0, -z);
            foreach (var s in todos)
            {
                if (s == null) continue;
                var l = s.transform.InverseTransformPoint(w);
                if (Mathf.Abs(l.x) < s.ancho / 2 + 0.5f && Mathf.Abs(l.z) < s.largo / 2 + 0.5f) return true;
            }
            return false;
        }

        // ---------- materiales ----------
        static string Mat(string nombre, string color)
        {
            var mm = MaterialesMundo.Instancia;
            return mm != null && mm.Buscar(nombre) != null ? nombre : color;
        }

        const string VERDE_TIERNO = "#7fa04a", VERDE = "#5d8838", VERDE_OSCURO = "#4a7030";
        const string DORADO = "#c9a54a", ESPIGA = "#dcc070", PAJA = "#b89a58", PARVA = "#c4a35a";
        const string CANA_SECA = "#b39a6a", HOJA_SECA = "#a88c5c", MAZORCA = "#d8b850", PENACHO = "#c8a878";

        float semBase;
        float R(double i) => MapaPago.Hs(i * 1.37 + semBase);

        float Alto(float lx, float lz) => Terreno.Altura(transform.TransformPoint(lx, 0, lz)) - transform.position.y;
        Vector3 P(float lx, float lz, float dy) => new Vector3(lx, Alto(lx, lz) + dy, lz);

        /// Cuánto adentro del sembrado está un punto: 0 en el borde, 1 pasando la orilla (irregular).
        float Orilla(float lx, float lz)
        {
            float d = Mathf.Min(ancho / 2 - Mathf.Abs(lx), largo / 2 - Mathf.Abs(lz));
            float m = 1.2f + 2.2f * MapaPago.Ruido(lx * 0.15 + semilla * 3.1, lz * 0.15 - semilla * 5.7);
            return Mathf.Clamp01(d / m);
        }

        float DistBorde(float lx, float lz) => Mathf.Min(ancho / 2 - Mathf.Abs(lx), largo / 2 - Mathf.Abs(lz));

        /// Manchones sin plantas, con borde irregular; en los campos grandes, manchones más grandes.
        bool Falla(float lx, float lz)
        {
            if (fallas <= 0) return false;
            float esc = 0.12f / Mathf.Max(1, Mathf.Sqrt(ancho * largo) / 120);
            float n = MapaPago.Ruido(lx * esc + semilla * 13.1, lz * esc - semilla * 7.3);
            return n < fallas * 0.45f;
        }

        // ---------- cuadros de 50 m ----------
        const float CUADRO = 50;
        Dictionary<(int, int), ObraMalla> cuadros;

        ObraMalla Obra(float lx, float lz)
        {
            var k = (Mathf.FloorToInt(lx / CUADRO), Mathf.FloorToInt(lz / CUADRO));
            if (!cuadros.TryGetValue(k, out var o)) cuadros[k] = o = new ObraMalla { metrosTextura = 7.2f };
            return o;
        }

        protected override void Construir(Transform raiz)
        {
            semBase = semilla * 97.13f;
            cuadros = new Dictionary<(int, int), ObraMalla>();

            Suelo();
            if (cultivo == Cultivo.Trigo) Trigo();
            else Maiz();

            int n = 0;
            foreach (var kv in cuadros) kv.Value.Instanciar($"cuadro {n++}", raiz, Registrar);
            cuadros = null;
        }

        // ---------- suelo ----------
        string MatSuelo()
        {
            switch (etapa)
            {
                case Etapa.Arado: return Mat("surco_arado", "#4a3626");
                case Etapa.Sembrado: return Mat(cultivo == Cultivo.Maiz ? "surco" : "tierra_rastreada", "#6a5440");
                case Etapa.Brotando: return Mat(cultivo == Cultivo.Maiz ? "brote_maiz" : "brote_trigo", "#6f7a45");
                case Etapa.Cosechado: return Mat(cultivo == Cultivo.Maiz ? "rastrojo_maiz" : "rastrojo_trigo", "#a08850");
                default: return Mat(cultivo == Cultivo.Maiz ? "surco" : "tierra_rastreada", "#6a5440");
            }
        }

        void Suelo()
        {
            string mat = MatSuelo();
            float paso = ancho * largo > 20000 ? 2.5f : 1.25f;
            float a = ancho / 2, l = largo / 2;
            int nx = Mathf.CeilToInt(ancho / paso), nz = Mathf.CeilToInt(largo / paso);
            float hBase = etapa == Etapa.Arado ? 0.18f : 0.14f;   // un poco por encima: el terreno lejano se simplifica y sube
            float H(float x, float z) => MapaPago.Mix(-0.12f, hBase + 0.04f * MapaPago.Hs(Mathf.Round(x * 3) * 31.7 + Mathf.Round(z * 3) * 17.3 + semBase), Orilla(x, z));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x0 = -a + i * ancho / nx, x1 = -a + (i + 1) * ancho / nx, z0 = -l + j * largo / nz, z1 = -l + (j + 1) * largo / nz;
                    Obra((x0 + x1) / 2, (z0 + z1) / 2).Quad(mat, P(x0, z0, H(x0, z0)), P(x0, z1, H(x0, z1)), P(x1, z1, H(x1, z1)), P(x1, z0, H(x1, z0)));
                }
        }

        /// Manto: la superficie del cultivo, facetada, con fallas que dejan ver la tierra y bajando hacia la orilla.
        void Manto(float alto, string mat)
        {
            float paso = ancho * largo > 20000 ? 2f : 0.9f;
            float a = ancho / 2, l = largo / 2;
            int nx = Mathf.CeilToInt(ancho / paso), nz = Mathf.CeilToInt(largo / paso);
            var hs = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                {
                    float x = -a + i * ancho / nx, z = -l + j * largo / nz;
                    float orilla = Orilla(x, z);
                    if (Falla(x, z)) orilla *= 0.55f;   // manchón ralo y más bajo, no un pozo
                    float h = alto * (0.85f + 0.25f * MapaPago.Ruido(x * 0.12 + semilla, z * 0.12) + 0.08f * R(i * 977 + j * 131));
                    hs[i, j] = MapaPago.Mix(0.02f, h, Mathf.SmoothStep(0, 1, orilla));
                }
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float h00 = hs[i, j], h01 = hs[i, j + 1], h11 = hs[i + 1, j + 1], h10 = hs[i + 1, j];
                    if (Mathf.Max(Mathf.Max(h00, h01), Mathf.Max(h11, h10)) < alto * 0.12f) continue;   // orilla: se ve la tierra
                    float x0 = -a + i * ancho / nx, x1 = -a + (i + 1) * ancho / nx, z0 = -l + j * largo / nz, z1 = -l + (j + 1) * largo / nz;
                    var o = Obra((x0 + x1) / 2, (z0 + z1) / 2);
                    o.Quad(mat, P(x0, z0, h00), P(x0, z1, h01), P(x1, z1, h11), P(x1, z0, h10));
                }
        }

        /// Recorre los puntos de la orilla (una franja del ancho pedido) con separación sep.
        void EnOrilla(float sep, System.Action<float, float, double> f)
        {
            float a = ancho / 2, l = largo / 2, banda = Mathf.Max(sep, orillaConPlantas);
            int nx = Mathf.FloorToInt(ancho / sep), nz = Mathf.FloorToInt(largo / sep);
            int nb = Mathf.Max(1, Mathf.CeilToInt(banda / sep));
            var hechos = new HashSet<(int, int)>();
            void Celda(int i, int j)
            {
                if (i < 0 || j < 0 || i >= nx || j >= nz || !hechos.Add((i, j))) return;
                double s = i * 7919.0 + j * 104729.0;
                float x = -a + (i + R(s)) * sep, z = -l + (j + R(s + 1)) * sep;
                if (Falla(x, z) || DistBorde(x, z) > banda) return;
                if (R(s + 8) > Orilla(x, z) * 1.6f + 0.15f) return;   // en la orilla misma las plantas ralean
                f(x, z, s);
            }
            for (int i = 0; i < nx; i++) for (int k = 0; k < nb; k++) { Celda(i, k); Celda(i, nz - 1 - k); }
            for (int j = 0; j < nz; j++) for (int k = 0; k < nb; k++) { Celda(k, j); Celda(nx - 1 - k, j); }
        }

        // ---------- trigo ----------
        void Trigo()
        {
            switch (etapa)
            {
                case Etapa.Creciendo: Manto(0.55f, Mat("trigo_verde", VERDE)); break;
                case Etapa.Maduro: Manto(0.95f, Mat("trigo_maduro", DORADO)); break;
                case Etapa.Cosechado: Manto(0.12f, Mat("rastrojo_trigo", PAJA)); break;
            }
            if (etapa == Etapa.Arado || etapa == Etapa.Sembrado) return;
            EnOrilla(0.4f, (x, z, s) =>
            {
                var o = Obra(x, z);
                var b = P(x, z, 0.02f);
                float h = R(s + 2);
                switch (etapa)
                {
                    case Etapa.Brotando:
                        for (int k = 0; k < 3; k++) Hoja(o, k == 0 ? VERDE : VERDE_TIERNO, b, 0.1f + 0.1f * h, 0.035f, R(s + 3 + k) * 6.28f, 0.05f);
                        break;
                    case Etapa.Creciendo:
                        for (int k = 0; k < 3; k++) Hoja(o, k == 0 ? VERDE_OSCURO : VERDE, b, 0.5f + 0.25f * h, 0.05f, R(s + 3 + k) * 6.28f, 0.12f);
                        break;
                    case Etapa.Maduro:
                        for (int k = 0; k < 3; k++)
                        {
                            float ang = R(s + 3 + k) * 6.28f, alto = 0.85f + 0.3f * R(s + 6 + k);
                            Hoja(o, DORADO, b, alto, 0.04f, ang, 0.1f);
                            Espiga(o, b + new Vector3(Mathf.Sin(ang) * 0.1f, alto, -Mathf.Cos(ang) * 0.1f), ang);
                        }
                        break;
                    case Etapa.Cosechado:
                        for (int k = 0; k < 2; k++) Hoja(o, PAJA, b, 0.1f + 0.1f * h, 0.05f, R(s + 3 + k) * 6.28f, 0.04f);
                        break;
                }
            });
            if (etapa == Etapa.Cosechado) Parvas();
        }

        static void Hoja(ObraMalla o, string c, Vector3 b, float alto, float anchoHoja, float ang, float incl)
        {
            var lado = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * anchoHoja / 2;
            var punta = b + new Vector3(Mathf.Sin(ang) * incl, alto, -Mathf.Cos(ang) * incl);
            o.Tri(c, b - lado, punta, b + lado);
        }

        static void Espiga(ObraMalla o, Vector3 base_, float ang)
        {
            var lado = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 0.035f;
            var arriba = base_ + new Vector3(0, 0.12f, 0);
            var medio = base_ + new Vector3(0, 0.06f, 0);
            o.Tri(ESPIGA, base_, medio - lado, arriba);
            o.Tri(ESPIGA, base_, arriba, medio + lado);
        }

        /// Parvas de trigo segado y gavillas paradas, repartidas por el rastrojo.
        void Parvas()
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(ancho * largo / 2500), 2, 40);
            for (int i = 0; i < n; i++)
            {
                float x = (R(i * 3.1 + 500) - 0.5f) * (ancho - 10), z = (R(i * 5.7 + 600) - 0.5f) * (largo - 10);
                var o = Obra(x, z);
                if (R(i + 700) < 0.45f) Parva(o, P(x, z, -0.1f), 2.4f + R(i + 800) * 1.2f, 3.0f + R(i + 900) * 1.2f);
                else
                    for (int k = 0; k < 5; k++)
                    {
                        float ang = k * 1.256f;
                        Gavilla(o, P(x + Mathf.Cos(ang) * 0.45f, z + Mathf.Sin(ang) * 0.45f, 0), 1.1f, ang);
                    }
            }
        }

        static void Parva(ObraMalla o, Vector3 c, float r, float h)
        {
            const int lados = 8;
            var cima = c + new Vector3(0, h, 0);
            for (int i = 0; i < lados; i++)
            {
                float a0 = i * Mathf.PI * 2 / lados, a1 = (i + 1) * Mathf.PI * 2 / lados;
                var p0 = c + new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
                var p1 = c + new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
                var q0 = c + new Vector3(Mathf.Cos(a0) * r * 0.85f, h * 0.55f, Mathf.Sin(a0) * r * 0.85f);
                var q1 = c + new Vector3(Mathf.Cos(a1) * r * 0.85f, h * 0.55f, Mathf.Sin(a1) * r * 0.85f);
                o.Quad(PARVA, p0, q0, q1, p1);
                o.Tri(PAJA, q0, cima, q1);
            }
        }

        static void Gavilla(ObraMalla o, Vector3 b, float h, float ang)
        {
            for (int k = 0; k < 3; k++)
            {
                float a = ang + k * 2.09f;
                var lado = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.18f;
                var punta = b + new Vector3(-Mathf.Cos(ang) * 0.12f, h, -Mathf.Sin(ang) * 0.12f);
                o.Tri(PARVA, b - lado, punta, b + lado);
            }
        }

        // ---------- maíz ----------
        void Maiz()
        {
            switch (etapa)
            {
                case Etapa.Creciendo: Manto(1.2f, Mat("maizal_verde", VERDE)); break;
                case Etapa.Maduro: Manto(2.1f, Mat("maizal_seco", HOJA_SECA)); break;
            }
            if (etapa == Etapa.Arado || etapa == Etapa.Sembrado) return;
            const float sep = 0.9f, entre = 0.5f;
            float a = ancho / 2, l = largo / 2, banda = Mathf.Max(sep, orillaConPlantas);
            int filas = Mathf.Max(1, Mathf.FloorToInt(ancho / sep));
            int nz = Mathf.FloorToInt(largo / entre);
            bool soloOrilla = etapa != Etapa.Brotando || ancho * largo > 20000;
            for (int f = 0; f < filas; f++)
            {
                float x = -a + f * sep + sep / 2;   // sobre el lomo del surco
                bool filaDeOrilla = x < -a + banda || x > a - banda;
                for (int j = 0; j < nz; j++)
                {
                    float z = -l + (j + 0.5f) * entre;
                    // adentro de la fila solo las puntas (las cabeceras), salvo las filas de la orilla
                    if (soloOrilla && !filaDeOrilla && z > -l + banda && z < l - banda)
                    {
                        int salto = Mathf.FloorToInt((largo - banda) / entre);   // saltea hasta la cabecera de enfrente
                        if (j < salto) j = salto - 1;
                        continue;
                    }
                    double s = f * 7919.0 + j * 104729.0;
                    z += (R(s) - 0.5f) * 0.15f;
                    if (Falla(x, z) || R(s + 8) > Orilla(x, z) * 1.6f + 0.1f) continue;
                    var o = Obra(x, z);
                    var b = P(x + (R(s + 1) - 0.5f) * 0.08f, z, 0.07f);
                    float h = R(s + 2), giro = R(s + 3) * 6.28f;
                    switch (etapa)
                    {
                        case Etapa.Brotando:
                            Hoja(o, VERDE_TIERNO, b, 0.15f + 0.1f * h, 0.06f, giro, 0.08f);
                            Hoja(o, VERDE_TIERNO, b, 0.12f + 0.1f * h, 0.06f, giro + 3.14f, 0.08f);
                            break;
                        case Etapa.Creciendo:
                            Planta(o, b, 0.9f + 0.5f * h, giro, VERDE, VERDE_OSCURO, false, false);
                            break;
                        case Etapa.Maduro:
                            Planta(o, b, 1.9f + 0.6f * h, giro, h < 0.5f ? HOJA_SECA : VERDE, CANA_SECA, true, true);
                            break;
                        case Etapa.Cosechado:
                            if (R(s + 5) < 0.75f) Cana(o, b, 0.5f + 0.6f * h, giro, R(s + 6) < 0.3f);
                            break;
                    }
                }
            }
        }

        /// Planta de maíz: caña de tres caras, hojas largas alternadas y, madura, mazorca y penacho.
        static void Planta(ObraMalla o, Vector3 b, float alto, float giro, string hoja, string cana, bool mazorca, bool penacho)
        {
            Cana(o, b, alto, giro, false, cana);
            int hojas = 5;
            for (int k = 0; k < hojas; k++)
            {
                float t = (k + 1f) / (hojas + 1), ang = giro + k * Mathf.PI;
                var base_ = b + new Vector3(0, alto * t, 0);
                var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                float largoHoja = 0.55f + 0.25f * (1 - Mathf.Abs(t - 0.5f) * 2);
                var medio = base_ + dir * largoHoja * 0.55f + new Vector3(0, 0.18f, 0);
                var punta = base_ + dir * largoHoja + new Vector3(0, -0.08f, 0);
                var lado = Vector3.Cross(dir, Vector3.up) * 0.05f;
                o.Quad(hoja, base_ - lado * 0.4f, medio - lado, medio + lado, base_ + lado * 0.4f);
                o.Tri(hoja, medio - lado, punta, medio + lado);
            }
            if (mazorca)
            {
                var dir = new Vector3(Mathf.Cos(giro + 1.2f), 0, Mathf.Sin(giro + 1.2f));
                var m0 = b + new Vector3(0, alto * 0.5f, 0) + dir * 0.06f;
                var m1 = m0 + dir * 0.12f + new Vector3(0, 0.22f, 0);
                var lado = Vector3.Cross(dir, Vector3.up) * 0.05f;
                o.Quad(MAZORCA, m0 - lado, m1 - lado * 0.5f, m1 + lado * 0.5f, m0 + lado);
                o.Quad(HOJA_SECA, m0 + lado, m1 + lado * 0.5f, m1 - lado * 0.5f, m0 - lado);
            }
            if (penacho)
            {
                var cima = b + new Vector3(0, alto, 0);
                for (int k = 0; k < 3; k++)
                {
                    float ang = giro + k * 2.09f;
                    var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                    var lado = Vector3.Cross(dir, Vector3.up) * 0.015f;
                    o.Tri(PENACHO, cima - lado, cima + dir * 0.18f + new Vector3(0, 0.2f, 0), cima + lado);
                }
            }
        }

        static void Cana(ObraMalla o, Vector3 b, float alto, float giro, bool quebrada, string color = CANA_SECA)
        {
            const float r = 0.025f;
            var cima = b + new Vector3(0, alto, 0);
            if (quebrada) cima = b + new Vector3(Mathf.Cos(giro) * alto * 0.7f, alto * 0.35f, Mathf.Sin(giro) * alto * 0.7f);
            for (int k = 0; k < 3; k++)
            {
                float a0 = giro + k * 2.094f, a1 = giro + (k + 1) * 2.094f;
                var d0 = new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
                var d1 = new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
                o.Quad(color, b + d0, cima + d0 * 0.5f, cima + d1 * 0.5f, b + d1);
            }
            if (!quebrada && color == CANA_SECA)
            {
                var dir = new Vector3(Mathf.Cos(giro + 2f), 0, Mathf.Sin(giro + 2f));
                var base_ = b + new Vector3(0, alto * 0.6f, 0);
                var lado = Vector3.Cross(dir, Vector3.up) * 0.05f;
                o.Tri(HOJA_SECA, base_ - lado, base_ + dir * 0.4f + new Vector3(0, -0.35f, 0), base_ + lado);
            }
        }
    }
}
