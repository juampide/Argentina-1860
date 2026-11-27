using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Cara de un modelo hecho en Blender: maneja las formas de la cara (blendshapes) del FBX.
    /// - Gesto base de la ficha (Dura, Cansada, Amable, Triste, Enojado) y parpadeo.
    /// - Habla: la boca sigue el TEXTO: cada letra da una apertura (a, e, i, o, u abren distinto; b, m, p cierran; los signos son pausas), al ritmo
    ///   de unas 14 letras por segundo, así que una frase larga dura más que una corta.
    /// - Gesto según lo que dice: cada oración se lee (pregunta, exclamación, advertencia, queja, tristeza, cortesía, duda) y la cara acompaña;
    ///   al terminar de hablar la boca se cierra con una mueca de la última oración, que dura un par de segundos y vuelve al gesto base.
    /// Los nombres de las formas vienen de Arte/blender/expresiones.py.
    /// </summary>
    public class CaraBlender : MonoBehaviour
    {
        const float SEG_POR_LETRA = 0.068f, PAUSA_COMA = 0.16f, PAUSA_PUNTO = 0.34f, DURACION_PARPADEO = 0.16f, INTENSIDAD_GESTO = 0.85f, DURACION_MUECA = 2.6f;

        struct Cuadro { public float a, o, i, t0, t1; public int oracion; }
        struct Emocion { public string forma; public float fuerza; }

        SkinnedMeshRenderer cara;
        readonly Dictionary<string, int> formas = new Dictionary<string, int>();
        readonly Dictionary<string, float> actuales = new Dictionary<string, float>();
        Expresion gestoBase = Expresion.Neutra;

        // texto en curso
        readonly List<Cuadro> linea = new List<Cuadro>();
        readonly List<Emocion> oraciones = new List<Emocion>();
        float inicioHabla = -100f, finHabla = -100f, finMueca = -100f;
        Emocion mueca;
        float proxParpadeo, inicioParpadeo = -10f;

        public Expresion Gesto => gestoBase;
        public bool Hablando => Time.time < finHabla;

        public bool Iniciar(Expresion e)
        {
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.sharedMesh == null || r.sharedMesh.blendShapeCount == 0) continue;
                cara = r;
                for (int i = 0; i < r.sharedMesh.blendShapeCount; i++)
                {
                    string n = r.sharedMesh.GetBlendShapeName(i);
                    int punto = n.LastIndexOf('.');
                    formas[punto >= 0 ? n.Substring(punto + 1) : n] = i;
                }
                break;
            }
            if (cara == null) return false;
            proxParpadeo = Time.time + Random.Range(1.5f, 4f);
            gestoBase = e;
            return true;
        }

        public void Poner(Expresion e) { gestoBase = e; }

        // ---------------- habla ----------------

        /// Habla el texto: la boca lo sigue letra por letra y la cara acompaña oración por oración.
        public void Hablar(string texto)
        {
            if (cara == null || string.IsNullOrWhiteSpace(texto)) return;
            Construir(texto);
            inicioHabla = Time.time;
            finHabla = inicioHabla + (linea.Count > 0 ? linea[linea.Count - 1].t1 : 1f);
            finMueca = finHabla + DURACION_MUECA;
            mueca = oraciones.Count > 0 ? oraciones[oraciones.Count - 1] : default;
        }

        /// Solo mueve la boca durante seg segundos (para líneas sin texto).
        public void Hablar(float seg)
        {
            linea.Clear(); oraciones.Clear();
            float t = 0; int n = 0;
            while (t < seg) { float a = 0.35f + 0.5f * Mathf.Abs(Mathf.Sin(n * 1.7f)); linea.Add(new Cuadro { a = a, t0 = t, t1 = t + 0.14f }); t += 0.14f; n++; }
            inicioHabla = Time.time; finHabla = inicioHabla + seg; finMueca = finHabla;
            mueca = default;
        }

        static string Normalizar(string s)
        {
            var d = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        void Construir(string texto)
        {
            linea.Clear(); oraciones.Clear();
            string n = Normalizar(texto);
            float t = 0;
            int oracion = 0;
            var actual = new StringBuilder();
            for (int i = 0; i < n.Length; i++)
            {
                char c = n[i];
                actual.Append(c);
                var q = new Cuadro { oracion = oracion, t0 = t };
                float dur = SEG_POR_LETRA;
                switch (c)
                {
                    case 'a': q.a = 1.0f; break;
                    case 'e': q.a = 0.55f; q.i = 0.55f; break;
                    case 'i': q.a = 0.22f; q.i = 0.95f; break;
                    case 'o': q.a = 0.65f; q.o = 0.80f; break;
                    case 'u': q.a = 0.28f; q.o = 1.00f; break;
                    case 'b': case 'm': case 'p': break;                          // labios cerrados
                    case 'f': case 'v': q.a = 0.08f; break;
                    case ' ': dur = SEG_POR_LETRA * 0.55f; break;
                    case ',': case ';': case ':': dur = PAUSA_COMA; break;
                    case '.': case '!': case '?': case '\n': dur = PAUSA_PUNTO; break;
                    default:
                        if (char.IsLetter(c)) q.a = 0.24f; else dur = 0f;
                        break;
                }
                if (dur <= 0f) continue;
                q.t1 = t + dur;
                t = q.t1;
                linea.Add(q);
                if (c == '.' || c == '!' || c == '?')
                {
                    oraciones.Add(Leer(actual.ToString()));
                    actual.Clear(); oracion++;
                }
            }
            if (actual.ToString().Trim().Length > 0) oraciones.Add(Leer(actual.ToString()));
        }

        // ---------------- qué dice -> qué cara pone ----------------
        static bool Tiene(string s, params string[] claves) { foreach (var c in claves) if (s.Contains(c)) return true; return false; }

        /// Lee una oración (ya sin tildes ni mayúsculas) y decide la forma de cara que la acompaña.
        static Emocion Leer(string o)
        {
            if (Tiene(o, "?")) return new Emocion { forma = "CejasArriba", fuerza = 0.9f };
            bool exclama = Tiene(o, "!");
            if (Tiene(o, "no ande", "cuidado", "peligro", "ojo ", "avis", "vigil", "ordeno", "prohib", "ni se le ocurra", "advierto"))
                return new Emocion { forma = exclama ? "Enojado" : "Dura", fuerza = 0.9f };
            if (exclama)
                return Tiene(o, "buenas", "bienvenid", "gracias", "salud", "viva")
                    ? new Emocion { forma = "Amable", fuerza = 0.9f }
                    : new Emocion { forma = Tiene(o, " no ", "nunca", "fuera", "basta", "jamas") ? "Enojado" : "Dura", fuerza = 0.85f };
            if (Tiene(o, "pocos", "poco ", "menos", "falta", "no llegan", "cansad", "sin ", "nadie", "no alcanza", "otra vez"))
                return new Emocion { forma = "Cansada", fuerza = 0.95f };
            if (Tiene(o, "muri", "perdi", "lastima", "triste", "pena", "llor", "extran", "duele"))
                return new Emocion { forma = "Triste", fuerza = 0.9f };
            if (Tiene(o, "a su servicio", "buenas", "gusto", "bienvenid", "gracias", "por favor", "amigo", "con gusto", "encantad"))
                return new Emocion { forma = "Amable", fuerza = 0.7f };
            if (Tiene(o, "dicen", "quien sabe", "tal vez", "quizas", "capaz", "se dice", "ya veremos", "dudo", "segun"))
                return new Emocion { forma = "Mueca", fuerza = 0.9f };
            return default;
        }

        // ---------------- por cuadro ----------------
        void Objetivo(string forma, float w)
        {
            if (string.IsNullOrEmpty(forma)) return;
            objetivos.TryGetValue(forma, out float v);
            objetivos[forma] = Mathf.Max(v, w);
        }
        readonly Dictionary<string, float> objetivos = new Dictionary<string, float>();

        void LateUpdate()
        {
            if (cara == null) return;
            float t = Time.time, dt = Time.deltaTime;
            objetivos.Clear();

            // gesto base de la ficha
            if (gestoBase != Expresion.Neutra) Objetivo(gestoBase.ToString(), INTENSIDAD_GESTO);

            float a = 0, o = 0, i = 0;
            bool habla = t < finHabla;
            if (habla)
            {
                float u = t - inicioHabla;
                int k = BuscarCuadro(u);
                if (k >= 0)
                {
                    var q = linea[k];
                    float f = Mathf.InverseLerp(q.t0, q.t1, u);
                    float s = f < 0.5f ? Mathf.SmoothStep(0, 1, f * 2) : 1f;          // la boca llega a la forma y la sostiene
                    // pasa suave entre la forma anterior y la actual (coarticulación)
                    var p = k > 0 ? linea[k - 1] : q;
                    a = Mathf.Lerp(p.a, q.a, s); o = Mathf.Lerp(p.o, q.o, s); i = Mathf.Lerp(p.i, q.i, s);
                    // la cara acompaña la oración que está diciendo
                    if (q.oracion < oraciones.Count) Objetivo(oraciones[q.oracion].forma, oraciones[q.oracion].fuerza * 0.7f);
                }
            }
            else if (t < finMueca)
            {
                // terminó de hablar: la boca se cierra con la mueca de la última oración (se va apagando)
                float resto = Mathf.Clamp01((finMueca - t) / DURACION_MUECA);
                Objetivo(mueca.forma, mueca.fuerza * Mathf.SmoothStep(0, 1, Mathf.Min(1f, resto * 3f)) );
                if (!string.IsNullOrEmpty(mueca.forma)) objetivos[gestoBase.ToString()] = 0f;     // la mueca reemplaza al gesto base mientras dura
            }

            // boca: mandíbula (Hablar) + labios (O, I)
            Objetivo("Hablar", a * 0.9f);
            Objetivo("Boca_O", o * 0.85f);
            Objetivo("Boca_I", i * 0.9f);

            // parpadeo (también uno al cortar una frase)
            if (t >= proxParpadeo) { inicioParpadeo = t; proxParpadeo = t + Random.Range(2.2f, 5.5f); }
            float pb = (t - inicioParpadeo) / DURACION_PARPADEO;
            float parpadeo = pb >= 0f && pb <= 1f ? Mathf.Sin(pb * Mathf.PI) : 0f;

            // aplicar con suavizado (las formas de la boca más rápido; los gestos de cara, más lento)
            foreach (var kv in formas)
            {
                string n = kv.Key;
                if (n == "Basis") continue;
                float meta = n == "Parpadeo" ? parpadeo : (objetivos.TryGetValue(n, out float m) ? m : 0f);
                actuales.TryGetValue(n, out float actual);
                float vel = n == "Parpadeo" ? 100f : (n == "Hablar" || n == "Boca_O" || n == "Boca_I") ? 14f : 5f;
                actual = Mathf.MoveTowards(actual, meta, dt * vel);
                actuales[n] = actual;
                cara.SetBlendShapeWeight(kv.Value, Mathf.Clamp01(actual) * 100f);
            }
        }

        int BuscarCuadro(float u)
        {
            if (linea.Count == 0) return -1;
            int lo = 0, hi = linea.Count - 1;
            if (u >= linea[hi].t1) return hi;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (linea[mid].t1 <= u) lo = mid + 1; else hi = mid;
            }
            return lo;
        }
    }
}
