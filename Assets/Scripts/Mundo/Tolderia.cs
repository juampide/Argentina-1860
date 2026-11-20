using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Toldería de tierra adentro, del otro lado del cañadón de la frontera. Los toldos son armazones de palos
    /// cubiertos con cueros de potro cosidos, bajos y abiertos hacia el este. Alrededor hay cueros estaqueados
    /// secándose, lanzas de tacuara apoyadas, la caballada pastando y la gente: los hombres con quillango,
    /// vincha y el pelo largo, las mujeres con el manto hasta el tobillo y trenzas, y los chicos.
    /// No se puede llegar: el cañadón frena al jugador. Se ve desde la orilla del pago.
    /// El fuego es un Fogon hijo del objeto. Las posiciones son relativas al objeto, que no debe rotarse.
    /// </summary>
    public class Tolderia : ProceduralBase
    {
        [Range(1, 8)] public int toldos = 5;
        [Range(0, 20)] public int gente = 11;
        [Range(0, 16)] public int caballos = 9;
        public int semilla = 1867;

        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
        static readonly string[] CUEROS = { "#a07850", "#7a5a3c", "#c8a87a", "#5a4632", "#b89068", "#8a6a48", "#d2bc96" };

        System.Random rnd;
        float Al() => (float)rnd.NextDouble();
        float y0;
        float Alt(float x, float z) => Terreno.Altura(transform.TransformPoint(new Vector3(x, 0, z))) - y0;

        readonly List<(Vector2 p, float ang)> puertas = new List<(Vector2, float)>();

        protected override void Construir(Transform raiz)
        {
            rnd = new System.Random(semilla);
            y0 = transform.position.y;
            puertas.Clear();
            var n = new Nodo("tolderia");

            // los toldos en media luna, con la entrada hacia el este (+x de Unity)
            for (int i = 0; i < toldos; i++)
            {
                float a = Mathf.Lerp(-1.2f, 1.2f, toldos == 1 ? 0.5f : (float)i / (toldos - 1));
                var c = new Vector2(-Mathf.Cos(a) * 14 + (Al() - 0.5f) * 3, Mathf.Sin(a) * 15 + (Al() - 0.5f) * 3);
                float giro = -a * 0.6f + (Al() - 0.5f) * 0.3f;
                Toldo(n, c, giro, 4 + Al() * 1.6f, 3.2f + Al() * 1.0f, 1.9f + Al() * 0.5f);
                puertas.Add((c + new Vector2(Mathf.Cos(giro), Mathf.Sin(giro)) * 3.2f, giro));
            }

            // cueros estaqueados, secándose al sol
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector2(4 + Al() * 8, -10 + Al() * 20);
                float ang = Al() * Mathf.PI;
                float y = Alt(p.x, p.y);
                string col = CUEROS[rnd.Next(CUEROS.Length)];
                n.Pieza(Geo.Caja(1.9f, 0.03f, 1.4f), col, v(p.x, y + 0.08f, -p.y), v(0, ang, 0));
                for (int k = 0; k < 6; k++)
                {
                    float t = k * Mathf.PI / 3;
                    var e = new Vector2(Mathf.Cos(t) * 1.05f, Mathf.Sin(t) * 0.8f);
                    var r = new Vector2(e.x * Mathf.Cos(ang) + e.y * Mathf.Sin(ang), -e.x * Mathf.Sin(ang) + e.y * Mathf.Cos(ang));
                    n.Pieza(Geo.Cilindro(0.02f, 0.02f, 0.3f, 3), "#6a5640", v(p.x + r.x, y + 0.1f, -(p.y + r.y)));
                }
            }

            // lanzas apoyadas contra los toldos y clavadas en el suelo
            for (int i = 0; i < 6; i++)
            {
                var (pu, g) = puertas[i % puertas.Count];
                var p = pu + new Vector2(-Mathf.Sin(g), Mathf.Cos(g)) * (1.4f + Al()) * (i % 2 == 0 ? 1 : -1);
                float y = Alt(p.x, p.y);
                var inclina = v((Al() - 0.5f) * 0.3f, 0, (Al() - 0.5f) * 0.3f);
                var medio = v(p.x, y + 1.7f, -p.y);
                n.Pieza(Geo.Cilindro(0.018f, 0.022f, 3.6f, 4), "#b8a46a", medio, inclina);
                n.Pieza(Geo.Cono(0.03f, 0.24f, 3), "#8a8e92", medio + Conv.Q3(inclina) * v(0, 1.9f, 0), inclina);
            }

            n.Instanciar(raiz, Registrar);
            if (Application.isPlaying) Poblar(raiz);
        }

        /// Un toldo: palos en cumbrera, dos faldones de cueros cosidos, el fondo cerrado y la entrada abierta.
        void Toldo(Nodo padre, Vector2 c, float giro, float largo, float ancho, float alto)
        {
            var t = padre.Hijo("toldo", v(c.x, Alt(c.x, c.y), -c.y));
            t.rot = v(0, giro + Mathf.PI / 2, 0);   // el largo (z del toldo) queda hacia la entrada
            float a = Mathf.Atan2(alto, ancho / 2);
            float falda = Mathf.Sqrt(alto * alto + ancho * ancho / 4);
            int tiras = 3, filas = 2;
            foreach (var lado in new[] { -1f, 1f })
                for (int i = 0; i < tiras; i++)
                    for (int f = 0; f < filas; f++)
                    {
                        float z = -largo / 2 + (i + 0.5f) * largo / tiras;
                        float s = (f + 0.5f) / filas;    // de la cumbrera al borde
                        var p = v(lado * ancho / 2 * s, alto * (1 - s), z);
                        string col = CUEROS[rnd.Next(CUEROS.Length)];
                        t.Pieza(Geo.Caja(falda / filas + 0.15f, 0.05f, largo / tiras + 0.12f), col, p, v(0, 0, -lado * a + (Al() - 0.5f) * 0.05f));
                    }
            // fondo cerrado (triángulo de cuero) y oscuridad adentro
            t.Pieza(Geo.Cono(ancho * 0.52f, alto * 1.02f, 4), CUEROS[rnd.Next(CUEROS.Length)], v(0, alto * 0.5f, -largo / 2 + 0.05f), default, v(1, 1, 0.06f));
            t.Pieza(Geo.Cono(ancho * 0.44f, alto * 0.9f, 4), "oscuro", v(0, alto * 0.45f, largo / 2 - 0.4f), default, v(1, 1, 0.04f));
            // cumbrera y palos que asoman cruzados adelante
            t.Pieza(Geo.Cilindro(0.05f, 0.05f, largo + 0.6f, 5), "#6a5640", v(0, alto + 0.04f, 0), v(Mathf.PI / 2, 0, 0));
            foreach (var lado in new[] { -1f, 1f })
                t.Pieza(Geo.Cilindro(0.045f, 0.05f, alto * 1.45f, 5), "#6a5640", v(lado * ancho * 0.18f, alto * 0.66f, largo / 2 + 0.1f), v(0, 0, lado * 0.42f));
            // un poncho pampa colgado a la entrada
            if (Al() < 0.5f)
                t.Pieza(Geo.Caja(0.9f, 1.1f, 0.03f), Al() < 0.5f ? "#7a2a22" : "#2f3a4a", v(ancho * 0.28f, alto * 0.5f, largo / 2 + 0.05f), v(0, 0, 0.1f));
        }

        // ---------- la gente y la caballada (solo en el juego) ----------

        enum Hace { Sentado, Parado, Trabajando, Vigia }

        class Persona { public ModeloPersonaje m; public Hace hace; public float fase, mira; public Vector3 hacia; }
        readonly List<Persona> personas = new List<Persona>();
        readonly List<(Bicho b, float fase, float pasto)> tropilla = new List<(Bicho, float, float)>();
        Transform jugador;

        string Uno(string[] l) => l[rnd.Next(l.Length)];

        void Poblar(Transform raiz)
        {
            var fuego = GetComponentInChildren<Fogon>();
            var centro = fuego != null ? fuego.transform.localPosition : Vector3.zero;
            var cj = FindAnyObjectByType<ControlJugador>();
            jugador = cj != null ? cj.transform : null;

            for (int i = 0; i < gente; i++)
            {
                bool mujer = i % 3 == 1, chico = i % 5 == 4;
                var a = Variedad.Indigena(rnd, mujer, chico);
                Hace hace = chico ? Hace.Parado : mujer ? (i % 2 == 0 ? Hace.Trabajando : Hace.Sentado) : (i % 4 == 0 ? Hace.Vigia : i % 4 == 2 ? Hace.Parado : Hace.Sentado);
                if (hace == Hace.Vigia) a.accesorio = Accesorio.Lanza;

                // dónde: sentados alrededor del fuego, trabajando junto a los cueros, parados en las entradas
                Vector3 p; Vector3 mira;
                switch (hace)
                {
                    case Hace.Sentado:
                    {
                        float ang = i * 1.3f;
                        p = centro + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 1.9f;
                        mira = centro;
                        break;
                    }
                    case Hace.Trabajando:
                        p = new Vector3(5 + Al() * 7, 0, -9 + Al() * 18);
                        mira = p + new Vector3(Al() - 0.5f, 0, Al() - 0.5f);
                        break;
                    case Hace.Vigia:
                        // mirando hacia el pago, del lado del cañadón
                        p = new Vector3(-6 + Al() * 12, 0, 15 + Al() * 5);
                        mira = p + new Vector3(0, 0, 10);
                        break;
                    default:
                    {
                        var (pu, _) = puertas[rnd.Next(puertas.Count)];
                        p = new Vector3(pu.x + (Al() - 0.5f) * 2, 0, pu.y + (Al() - 0.5f) * 2);
                        mira = p + new Vector3(Al() - 0.5f, 0, Al() - 0.5f);
                        break;
                    }
                }
                var go = new GameObject(mujer ? "mujer de la toldería" : chico ? "chico de la toldería" : "hombre de la toldería");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(raiz, false);
                p.y = Alt(p.x, p.z);
                go.transform.localPosition = p;
                var d = mira - p; d.y = 0;
                if (d.sqrMagnitude > 0.001f) go.transform.localRotation = Quaternion.LookRotation(d);
                var m = go.AddComponent<ModeloPersonaje>();
                m.aspecto = a;
                personas.Add(new Persona { m = m, hace = hace, fase = Al() * 10 });
            }

            // la caballada, pastando entre los toldos y el cañadón
            string[] pelajes = { "#6b4a2a", "#3a2a1e", "#a8804a", "#d8ccb4", "#8a5a32", "#5a4a3a", "#c8b08a" };
            for (int i = 0; i < caballos; i++)
            {
                var b = ModelosFauna.Caballo(raiz, Uno(pelajes), Al() < 0.3f);
                var p = new Vector3(2 + Al() * 18, 0, 6 + Al() * 12);
                p.y = Alt(p.x, p.z);
                b.raiz.localPosition = p;
                b.raiz.localRotation = Quaternion.Euler(0, Al() * 360, 0);
                tropilla.Add((b, Al() * 10, 0.4f + Al() * 0.6f));
            }
        }

        void Update()
        {
            if (!Application.isPlaying || personas.Count + tropilla.Count == 0) return;
            if (jugador != null && (jugador.position - transform.position).sqrMagnitude > 260 * 260) return;   // lejos, no se anima
            float dt = Time.deltaTime, t = Time.time;
            foreach (var p in personas)
            {
                var anim = p.m != null ? p.m.Anim : null;
                if (anim == null) continue;
                p.fase += dt;
                switch (p.hace)
                {
                    case Hace.Sentado:
                        p.mira = Mathf.Lerp(p.mira, Mathf.Sin(p.fase * 0.21f) * 0.6f, dt);
                        anim.Sentado(p.fase, Mathf.Clamp01(Mathf.Sin(p.fase * 0.3f) * 2 - 1), p.mira, 0.44f);
                        break;
                    case Hace.Trabajando:
                        anim.Cocinando(p.fase, 0.25f);
                        break;
                    default:
                        anim.Animar(p.fase * 1.2f, 0);
                        break;
                }
            }
            foreach (var (b, fase, pasto) in tropilla)
                b.AnimarCaballo(t + fase, pasto * (0.6f + 0.4f * Mathf.Sin(t * 0.2f + fase)), false);
        }
    }
}
