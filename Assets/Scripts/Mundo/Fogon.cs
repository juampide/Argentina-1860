using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// Qué se cocina en el fogón.
    public enum Coccion { Nada, Olla, Asado, Pava }

    /// <summary>
    /// Fogón de piedras con leña, llamas que se mueven con el viento, brasas y ceniza (fogon() de campo.js).
    /// Se prende en los horarios de cocinar (a media mañana hasta el mediodía y a la tarde hasta las primeras
    /// horas de la noche) y después se apaga: las llamas bajan, quedan las brasas un rato y al final la ceniza.
    /// Encima puede tener una olla colgada del trípode (con el caldo que hierve), una pava sobre las brasas
    /// o un asado a la cruz. Alrededor hay troncos para sentarse y, con el fuego prendido, se arma la ronda
    /// (RondaFogon): llega gente de las casas cercanas, uno cocina y los demás charlan.
    /// </summary>
    public class Fogon : ProceduralBase
    {
        public bool luz = true;

        [Header("Horario (horas del día)")]
        [Tooltip("De media mañana al mediodía")]
        public Vector2 manana = new Vector2(9.5f, 12.75f);
        [Tooltip("De la tarde a las primeras horas de la noche")]
        public Vector2 tarde = new Vector2(17f, 21.75f);
        [Tooltip("Prendido siempre, de día y de noche (por ejemplo, una guardia)")]
        public bool siempre;

        [Header("Cocina y ronda")]
        public Coccion coccion = Coccion.Olla;
        [Tooltip("Cuánta gente se junta alrededor cuando está prendido (el primero cocina)")]
        [Range(0, 6)] public int gente = 3;
        [Tooltip("La ronda es de soldados (para el fortín)")]
        public bool soldados;

        const int LENGUAS = 5;
        const float R_ASIENTO = 1.85f;

        Light llama;
        float semilla;
        readonly Transform[] lenguas = new Transform[LENGUAS];
        readonly Vector3[] escLengua = new Vector3[LENGUAS];
        Renderer[] rLenguas = new Renderer[LENGUAS];
        Renderer rBrasas, rCarne, rCostillar;
        Transform caldo;
        float yCaldo;
        Humo humo;
        ParticleSystem burbujas, vapor;
        RondaFogon ronda;
        MaterialPropertyBlock mpb;

        float horaPrev = -1;
        /// 0 apagado, 1 fuego pleno.
        public float Fuego { get; private set; }
        /// Brillo de las brasas: dura un rato después de que se apagan las llamas.
        public float Brasa { get; private set; }
        /// 0 frío, 1 hirviendo (olla y pava).
        public float Hervor { get; private set; }
        /// Cuánto se cocinó el asado desde que se prendió (0 crudo, 1 dorado).
        public float Dorado { get; private set; }

        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);

        protected override void Construir(Transform raiz)
        {
            var n = new Nodo("fogon");
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f;
                n.Pieza(Geo.Ico(1, 0), "piedra", v(Mathf.Cos(a) * 0.55f, 0.1f, Mathf.Sin(a) * 0.55f), default, Vector3.one * 0.17f);
            }
            n.Pieza(Geo.Cilindro(0.48f, 0.52f, 0.04f, 9), "#4a4540", v(0, 0.02f, 0));                      // cama de ceniza
            for (int i = 0; i < 4; i++)                                                                       // leña medio quemada
                n.Pieza(Geo.Cilindro(0.05f, 0.05f, 0.9f, 5), i % 2 == 0 ? "madera" : "#2c211a", v(0, 0.12f, 0), v(1.45f, i * 0.8f, 0));

            // brasas: un puñado de piedritas que brillan
            var brasas = n.Hijo("brasas");
            var rnd = new System.Random(7);
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2, r = 0.05f + (float)rnd.NextDouble() * 0.26f;
                brasas.Pieza(Geo.Ico(1, 0), "llama", v(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r), v(a, a * 2, 0), Vector3.one * (0.05f + (float)rnd.NextDouble() * 0.04f));
            }

            // llamas: una lengua central y cuatro alrededor, cada una en su nodo para animarla
            float alto = coccion == Coccion.Olla ? 0.62f : 0.82f;
            var nl = new Nodo[LENGUAS];
            for (int i = 0; i < LENGUAS; i++)
            {
                float a = i * Mathf.PI * 2 / (LENGUAS - 1) + 0.4f;
                var p = i == 0 ? v(0, 0.1f, 0) : v(Mathf.Cos(a) * 0.14f, 0.1f, Mathf.Sin(a) * 0.14f);
                nl[i] = n.Hijo("lengua" + i, p);
                float h = i == 0 ? alto : alto * 0.62f, r = i == 0 ? 0.2f : 0.12f;
                nl[i].Pieza(Geo.Cono(r, h, 5), "llama", v(0, h / 2, 0));
            }

            // troncos para sentarse, del lado contrario al de cocinar
            int asientos = Mathf.Max(2, Mathf.Min(gente, 5));
            for (int k = 0; k < asientos; k++)
            {
                float a = AnguloAsiento(k);
                n.Pieza(Geo.Cilindro(0.17f, 0.18f, 0.75f, 6), "madera", v(Mathf.Cos(a) * (R_ASIENTO + 0.05f), 0.16f, Mathf.Sin(a) * (R_ASIENTO + 0.05f)), v(Mathf.PI / 2, -a, 0));
            }

            Nodo nCaldo = null, nCarne = null, nHueso = null;
            switch (coccion)
            {
                case Coccion.Olla:
                {
                    // trípode de fierro sobre el fuego, con la olla colgada de una cadena
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * Mathf.PI * 2 / 3 + 0.3f;
                        var pie = v(Mathf.Cos(a) * 0.78f, 0, Mathf.Sin(a) * 0.78f);
                        var tope = v(0, 1.45f, 0);
                        var medio = (pie + tope) / 2;
                        var d = tope - pie;
                        float incl = Mathf.Atan2(new Vector2(d.x, d.z).magnitude, d.y);
                        n.PiezaM(Geo.Cilindro(0.022f, 0.025f, d.magnitude, 4), "hierro",
                            Matrix4x4.TRS(medio, Quaternion.FromToRotation(Vector3.up, d.normalized), Vector3.one));
                    }
                    n.Pieza(Geo.Cilindro(0.01f, 0.01f, 0.5f, 3), "hierro", v(0, 1.2f, 0));                     // cadena
                    n.Pieza(Geo.Cilindro(0.27f, 0.22f, 0.32f, 9, true), "hierro", v(0, 0.8f, 0));            // olla
                    n.Pieza(Geo.Cilindro(0.22f, 0.12f, 0.06f, 9), "hierro", v(0, 0.61f, 0));                 // fondo
                    n.Pieza(Geo.Toro(0.27f, 0.025f, 4, 12), "hierro", v(0, 0.96f, 0), v(Mathf.PI / 2, 0, 0)); // borde
                    nCaldo = n.Hijo("caldo", v(0, 0.9f, 0));
                    nCaldo.Pieza(Geo.Cilindro(0.255f, 0.255f, 0.02f, 9), "#7a5532", Vector3.zero);
                    break;
                }
                case Coccion.Pava:
                {
                    // pava de fierro sobre las brasas, del lado de quien ceba
                    var c = v(0.36f, 0, 0);
                    n.Pieza(Geo.Cilindro(0.11f, 0.13f, 0.2f, 8), "hierro", c + v(0, 0.16f, 0));
                    n.Pieza(Geo.Cilindro(0.04f, 0.11f, 0.06f, 8), "hierro", c + v(0, 0.29f, 0));
                    n.Pieza(Geo.Cilindro(0.015f, 0.03f, 0.18f, 4), "hierro", c + v(-0.14f, 0.24f, 0), v(0, 0, 0.9f));   // pico
                    n.Pieza(Geo.Toro(0.08f, 0.012f, 3, 8, Mathf.PI), "hierro", c + v(0, 0.33f, 0));                     // asa
                    nCaldo = n.Hijo("caldo", c + v(0, 0.32f, 0));
                    n.Pieza(Geo.Cilindro(0.17f, 0.18f, 0.36f, 6), "madera", v(1.3f, 0.18f, 0));   // tronco parado donde se sienta el que ceba
                    break;
                }
                case Coccion.Asado:
                {
                    // asado a la cruz: la estaca clavada al lado del fuego, inclinada hacia las llamas
                    var asador = n.Hijo("asador", v(0.95f, 0, 0));
                    asador.rot = v(0, 0, 0.32f);
                    asador.Pieza(Geo.Cilindro(0.025f, 0.03f, 1.7f, 5), "hierro", v(0, 0.85f, 0));
                    asador.Pieza(Geo.Cilindro(0.018f, 0.018f, 0.75f, 4), "hierro", v(0, 1.05f, 0), v(Mathf.PI / 2, 0, 0));
                    nCarne = asador.Hijo("carne", v(0, 1.0f, 0));
                    nCarne.Pieza(Geo.Caja(0.08f, 0.62f, 0.66f, 1.5f), "#ffffff", Vector3.zero);
                    nHueso = asador.Hijo("costillar", v(0.01f, 0.82f, 0));
                    nHueso.Pieza(Geo.Caja(0.1f, 0.12f, 0.6f, 1.5f), "#efe2d0", Vector3.zero);
                    break;
                }
            }

            var mapa = new Dictionary<Nodo, Transform>();
            n.Instanciar(raiz, Registrar, mapa);
            for (int i = 0; i < LENGUAS; i++)
            {
                lenguas[i] = mapa[nl[i]];
                escLengua[i] = lenguas[i].localScale;
                rLenguas[i] = lenguas[i].GetComponent<Renderer>();
                if (rLenguas[i] != null) rLenguas[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            rBrasas = mapa[brasas].GetComponent<Renderer>();
            if (rBrasas != null) rBrasas.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            caldo = nCaldo != null ? mapa[nCaldo] : null;
            if (caldo != null) yCaldo = caldo.localPosition.y;
            rCarne = nCarne != null ? mapa[nCarne].GetComponent<Renderer>() : null;
            rCostillar = nHueso != null ? mapa[nHueso].GetComponent<Renderer>() : null;

            if (luz)
            {
                var go = new GameObject("luz");
                go.transform.SetParent(raiz, false);
                go.transform.localPosition = v(0, 0.9f, 0);
                llama = go.AddComponent<Light>();
                llama.type = LightType.Point;
                llama.color = new Color(1f, 0.6f, 0.25f);
                llama.range = 7;
                llama.intensity = 2.5f;
                llama.shadows = LightShadows.None;
            }
            semilla = Random.value * 100;

            if (Application.isPlaying)
            {
                humo = Humo.Poner(raiz, v(0, 0.7f, 0), false, 1, true);   // humo y chispas
                if (caldo != null)
                {
                    if (coccion == Coccion.Olla)
                    {
                        burbujas = Particulas.Crear("burbujas", caldo, v(0, 0.015f, 0), 40, 0.5f, new Vector2(0.035f, 0.08f), new Color(0.78f, 0.62f, 0.42f, 0.9f));
                        var sh = burbujas.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.2f; sh.rotation = v(90, 0, 0);
                        var sz = burbujas.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 0.1f), new Keyframe(0.75f, 1), new Keyframe(1, 1.3f)));
                        Particulas.Desvanecer(burbujas, 0.1f, 0.85f, 1);
                    }
                    vapor = Particulas.Crear("vapor", caldo, v(0, 0.05f, 0), 40, 2.4f, new Vector2(0.18f, 0.32f), new Color(0.92f, 0.92f, 0.9f, 0.2f));
                    var shv = vapor.shape; shv.enabled = true; shv.shapeType = ParticleSystemShapeType.Circle; shv.radius = coccion == Coccion.Olla ? 0.18f : 0.03f; shv.rotation = v(90, 0, 0);
                    var szv = vapor.sizeOverLifetime; szv.enabled = true; szv.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.5f, 1, 2.6f));
                    Particulas.Desvanecer(vapor, 0.15f, 0.4f, 1);
                    if (coccion == Coccion.Pava) vapor.transform.localPosition = v(-0.2f, 0.0f, 0);   // sale por el pico
                }
                if (ronda == null) ronda = GetComponent<RondaFogon>();
                if (ronda == null) ronda = gameObject.AddComponent<RondaFogon>();
                ronda.hideFlags = HideFlags.DontSave;
            }
        }

        /// Ángulo (radianes, en el plano del prototipo) del asiento k, repartidos del lado opuesto a la cocina.
        public float AnguloAsiento(int k)
        {
            int n = Mathf.Max(2, Mathf.Min(gente, 5));
            float abre = Mathf.PI * (n <= 2 ? 0.7f : 1.25f);
            return Mathf.PI - abre / 2 + abre * (n == 1 ? 0.5f : k / (float)(n - 1));
        }

        /// Punto del mundo a cierta distancia y ángulo del centro del fogón (ángulo en el plano del prototipo).
        public Vector3 Punto(float angulo, float r) => transform.TransformPoint(Conv.P(Mathf.Cos(angulo) * r, 0, Mathf.Sin(angulo) * r));

        public Vector3 Asiento(int k) => Punto(AnguloAsiento(k), R_ASIENTO);
        public int Asientos => Mathf.Max(2, Mathf.Min(gente, 5));

        /// Dónde se para (o se sienta) el que cocina.
        public Vector3 PuestoCocinero => coccion switch
        {
            Coccion.Asado => Punto(0.75f, 1.45f),
            Coccion.Pava => Punto(0, 1.25f),
            _ => Punto(0.15f, 1.18f),
        };

        public bool EnHorario(float h) => siempre || (h >= manana.x && h < manana.y) || (h >= tarde.x && h < tarde.y);

        void Update()
        {
            if (!Application.isPlaying || lenguas[0] == null) return;
            float h = Ambiente.Hora;
            float dh = horaPrev < 0 ? 0 : Mathf.Repeat(h - horaPrev, 24);
            bool salto = horaPrev < 0 || dh > 0.5f;   // arranque o salto de hora: el fuego queda como corresponde
            horaPrev = h;

            bool horario = EnHorario(h);
            // el fuego lo prende el que cocina cuando llega; sin ronda, se prende solo
            bool prender = horario && (ronda == null || gente == 0 || ronda.CocineroEnPuesto || salto);
            if (salto)
            {
                Fuego = horario ? 1 : 0;
                Brasa = Fuego;
                Hervor = horario ? 1 : 0;
                Dorado = horario ? 0.5f : 0;
            }
            else
            {
                Fuego = Mathf.Clamp01(Fuego + (prender ? dh / 0.25f : -dh / 0.4f));
                Brasa = Fuego > 0 ? Mathf.Max(Brasa, Fuego) : Mathf.Clamp01(Brasa - dh / 1.6f);
                Hervor = Mathf.Clamp01(Hervor + (Fuego > 0.55f ? dh / 0.45f : -dh / 0.6f));
                if (Fuego > 0.3f) Dorado = Mathf.Clamp01(Dorado + dh / 2.2f);
                else if (Brasa <= 0) Dorado = 0;   // al otro día, carne nueva
            }
            ronda?.Actualizar(this, horario, dh, salto);
            Animar(Time.time);
        }

        void Animar(float time)
        {
            mpb ??= new MaterialPropertyBlock();
            float t = time * 7 + semilla;
            var viento = Ambiente.Viento;
            float noche = Ambiente.LucesDeNoche;

            // llamas: crecen y se achican cada una a su ritmo y se inclinan con el viento
            var inclina = new Vector3(viento.x, 0, viento.y) * 0.5f;
            for (int i = 0; i < LENGUAS; i++)
            {
                var L = lenguas[i];
                float ti = t * (1.1f + i * 0.17f) + i * 2.3f;
                float alto = Fuego * (0.72f + 0.2f * Mathf.Sin(ti) + 0.12f * Mathf.Sin(ti * 2.7f + 1) + 0.06f * Mathf.Sin(ti * 6.1f));
                float ancho = Mathf.Lerp(0.4f, 1, Fuego) * (1 + 0.1f * Mathf.Sin(ti * 1.9f + 3));
                bool ve = alto > 0.03f;
                if (rLenguas[i] != null) rLenguas[i].enabled = ve;
                if (!ve) continue;
                L.localScale = Vector3.Scale(escLengua[i], new Vector3(ancho, alto, ancho));
                var vaiven = new Vector3(Mathf.Sin(ti * 1.3f) * 0.08f, 0, Mathf.Cos(ti * 1.1f) * 0.08f);
                var arriba = (Vector3.up + transform.InverseTransformDirection(inclina) + vaiven).normalized;
                L.localRotation = Quaternion.FromToRotation(Vector3.up, arriba) * Quaternion.Euler(0, ti * 8, 0);
                // más amarilla cuando está fuerte, más roja cuando baja
                mpb.Clear();
                var em = Color.Lerp(new Color(1.0f, 0.12f, 0.0f), new Color(1.6f, 0.55f, 0.08f), Fuego) * (0.9f + 0.25f * Mathf.Sin(ti * 3.3f)) * (i == 0 ? 1.15f : 1);
                mpb.SetColor("_EmissionColor", em);
                rLenguas[i].SetPropertyBlock(mpb);
            }

            // brasas: rojo que respira; se apagan de a poco
            if (rBrasas != null)
            {
                mpb.Clear();
                float b = Brasa * (0.75f + 0.25f * Mathf.Sin(t * 0.5f) * Mathf.Sin(t * 0.33f + 1));
                mpb.SetColor("_BaseColor", Color.Lerp(new Color(0.16f, 0.12f, 0.1f), new Color(0.7f, 0.2f, 0.05f), Brasa));
                mpb.SetColor("_EmissionColor", new Color(1.2f, 0.22f, 0.02f) * b * 1.4f);
                rBrasas.SetPropertyBlock(mpb);
            }

            if (llama != null)
            {
                float tit = 1 + Mathf.Sin(t) * 0.12f + Mathf.Sin(t * 2.7f) * 0.07f + Mathf.Sin(t * 5.3f) * 0.04f;
                llama.intensity = (2.2f * Fuego * tit + 0.5f * Brasa) * (1 + noche * 1.8f);
                llama.range = 2.5f + Fuego * (4.5f + noche * 9);
                llama.enabled = llama.intensity > 0.02f;
            }

            if (humo != null)
            {
                // con fuego, humo y chispas; con brasas, un hilo de humo
                humo.intensidad = Fuego > 0.02f ? 0.25f + 0.75f * Fuego : Brasa * 0.25f;
                humo.llama = Fuego;
            }

            // olla y pava: el caldo tiembla y burbujea, el vapor sale cuando hierve
            if (caldo != null)
            {
                if (coccion == Coccion.Olla)
                    caldo.localPosition = new Vector3(caldo.localPosition.x, yCaldo + Hervor * 0.006f * Mathf.Sin(time * 23) * Mathf.Sin(time * 7.7f), caldo.localPosition.z);
                if (burbujas != null) { var e = burbujas.emission; e.rateOverTime = 26 * Hervor * Hervor; }
                if (vapor != null)
                {
                    var e = vapor.emission; e.rateOverTime = (coccion == Coccion.Pava ? 5 : 7) * Mathf.Clamp01(Hervor * 1.5f - 0.3f);
                    var m = vapor.main; float c = Ambiente.Claridad;
                    m.startColor = new Color(0.92f * c, 0.92f * c, 0.9f * c, 0.2f);
                    var vel = vapor.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = viento.x * 2.5f; vel.y = 0.55f; vel.z = viento.y * 2.5f;
                }
            }

            // asado: va tomando color con las horas de fuego
            if (rCarne != null)
            {
                mpb.Clear();
                mpb.SetColor("_BaseColor", Color.Lerp(new Color(0.46f, 0.2f, 0.16f), new Color(0.34f, 0.19f, 0.1f), Dorado));
                rCarne.SetPropertyBlock(mpb);
                // cuando se apaga y se van, se llevan la carne
                bool hay = Fuego > 0.01f || Brasa > 0.85f;
                rCarne.enabled = hay;
                if (rCostillar != null) rCostillar.enabled = hay;
            }
        }
    }
}
