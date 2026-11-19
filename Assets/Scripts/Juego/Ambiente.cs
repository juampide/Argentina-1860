using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Argentina1860
{
    /// <summary>
    /// Hora del día y tiempo del pago.
    /// - El sol hace el recorrido de un día de fin de primavera en la provincia de Buenos Aires (latitud 34,6° sur):
    ///   sale por el este, pasa por el norte y se pone por el oeste. De noche alumbra la luna y los fogones crecen.
    /// - Clima: despejado, nublado, tormenta y pampero (el viento frío del sudoeste que limpia el cielo después
    ///   de la tormenta). Cambia solo si está en automático, o se elige a mano en el Inspector.
    /// - Las nubes son sombras que pasan sobre el campo con el viento; el viento mueve el pasto; la lluvia moja
    ///   la tierra (caminos y sembrados se oscurecen) y el polvo vuela cuando sopla fuerte y está seco.
    /// Otros sistemas leen los valores estáticos (Hora, LucesDeNoche, Viento, Lluvia, Humedad).
    /// </summary>
    public class Ambiente : MonoBehaviour
    {
        public enum Tiempo { Despejado, Nublado, Tormenta, Pampero }

        [Header("Hora")]
        [Range(0, 24)] public float hora = 8.5f;
        [Tooltip("Minutos reales que dura un día entero del juego")]
        [Min(1)] public float minutosPorDia = 24;
        public bool detenerHora;

        [Header("Clima")]
        public Tiempo tiempo = Tiempo.Despejado;
        [Tooltip("El tiempo cambia solo, con duraciones al azar")]
        public bool climaAutomatico = true;
        [Range(0, 1)] public float nubosidad = 0.15f;
        [Range(0, 1)] public float lluvia;
        [Range(0, 1)] public float viento = 0.25f;
        [Tooltip("Hacia dónde sopla el viento, en grados (0 = norte, 90 = este)")]
        public float rumboViento = 225;

        [Header("Luces (si faltan, se buscan o se crean)")]
        public Light sol;
        public Light luna;

        // ---------- lo que leen los demás sistemas ----------
        public static float Hora { get; private set; } = 9;
        /// 0 de día, 1 de noche cerrada (para fogones, faroles y la rutina de la gente y los animales).
        public static float LucesDeNoche { get; private set; }
        public static bool EsDeNoche => LucesDeNoche > 0.6f;
        /// Viento en el plano de Unity (x, z): dirección hacia donde sopla por la fuerza (0 a 1).
        public static Vector2 Viento { get; private set; }
        public static float Lluvia { get; private set; }
        /// Qué tan mojado está el suelo (sube con la lluvia y baja de a poco con el sol y el viento).
        public static float Humedad { get; private set; }
        public static Tiempo TiempoActual { get; private set; }
        /// Cuánta niebla baja hay (0 a 1): sale a la madrugada en los bajos.
        public static float Madrugada { get; private set; }
        /// Qué tan cubierto está el cielo (0 a 1).
        public static float TiempoNubes { get; private set; }
        /// Cuánta luz hay (0 de noche cerrada a 1 a pleno día): para oscurecer partículas que no reciben luz.
        public static float Claridad => Mathf.Lerp(0.2f, 1, 1 - LucesDeNoche) * (1 - 0.3f * TiempoNubes);

        static readonly int idViento = Shader.PropertyToID("_Viento"), idHumedad = Shader.PropertyToID("_Humedad"), idLucesNoche = Shader.PropertyToID("_LucesNoche");

        [Header("Mirada (pampa gris y melancólica)")]
        [Tooltip("Color de la niebla de día; el horizonte se funde con él")]
        public Color nieblaDia = new Color(0.72f, 0.74f, 0.71f);
        [Tooltip("Niebla baja que se junta en los bajos al amanecer y después de llover")]
        [Range(0, 1)] public float nieblaMatinal = 0.7f;
        const float LAT = -34.6f, DECL = -19f;

        // destinos del clima actual
        float nubObj, lluviaObj, vientoObj, rumboObj, duracion, humedad;
        float relampago, proxRelampago = 8;
        Color niebla0;
        float nieblaIni0, nieblaFin0;

        // nubes: textura de sombra (cookie) que se corre con el viento
        Texture2D nubes;
        float nubesHechas = -1;
        Vector2 corrimiento;
        UniversalAdditionalLightData datosSol;

        ParticleSystem gotas, polvo;
        Transform foco;

        void Start()
        {
            if (sol == null) { var go = GameObject.Find("Sol"); if (go != null) sol = go.GetComponent<Light>(); }
            if (luna == null)
            {
                var go = new GameObject("Luna");
                go.transform.SetParent(transform, false);
                luna = go.AddComponent<Light>();
                luna.type = LightType.Directional;
                luna.color = new Color(0.55f, 0.65f, 0.95f);
                luna.shadows = LightShadows.Soft;
                luna.intensity = 0;
            }
            niebla0 = nieblaDia;
            nieblaIni0 = RenderSettings.fogStartDistance; nieblaFin0 = RenderSettings.fogEndDistance;
            var cj = FindAnyObjectByType<ControlJugador>();
            foco = cj != null ? cj.transform : null;
            if (sol != null)
            {
                datosSol = sol.GetComponent<UniversalAdditionalLightData>();
                if (datosSol == null) datosSol = sol.gameObject.AddComponent<UniversalAdditionalLightData>();
            }
            humedad = Humedad;
            ElegirDestinos(tiempo, true);
            gotas = CrearLluvia();
            if (GetComponent<Aire>() == null) gameObject.AddComponent<Aire>();   // pelusas, polvo, bichitos y niebla baja
            polvo = CrearPolvo();
        }

        void OnDestroy()
        {
            if (nubes != null) Destroy(nubes);
            if (sol != null) sol.cookie = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float horasPorSegundo = 24f / (minutosPorDia * 60f);
            if (!detenerHora) hora = Mathf.Repeat(hora + dt * horasPorSegundo, 24);
            Hora = hora;

            Clima(dt, dt * horasPorSegundo);
            Sol(dt);
            Nubes(dt);
            Particulas();

            var dir = Rumbo(rumboViento);
            Viento = dir * viento;
            Lluvia = lluvia;
            TiempoActual = tiempo;
            TiempoNubes = nubosidad;
            // la tierra se moja rápido y se seca despacio, más ligero si hay sol y viento
            if (lluvia > 0.05f) humedad = Mathf.Min(1, humedad + lluvia * dt / 40f);
            else humedad = Mathf.Max(0, humedad - dt / 300f * (0.3f + viento + (1 - LucesDeNoche) * (1 - nubosidad)));
            Humedad = humedad;
            Shader.SetGlobalVector(idViento, new Vector4(dir.x, dir.y, viento, 0));
            Shader.SetGlobalFloat(idHumedad, humedad);
            Shader.SetGlobalFloat(idLucesNoche, LucesDeNoche);
            Posproceso();
        }

        static Vector2 Rumbo(float grados)
        {
            float r = grados * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));   // x este, z norte
        }

        // ---------- clima ----------
        void ElegirDestinos(Tiempo t, bool deGolpe)
        {
            tiempo = t;
            switch (t)
            {
                case Tiempo.Despejado: nubObj = 0.3f + Random.value * 0.2f; lluviaObj = 0; vientoObj = 0.15f + Random.value * 0.2f; rumboObj = 200 + Random.value * 50; duracion = 6 + Random.value * 8; break;   // brisa del noreste
                case Tiempo.Nublado: nubObj = 0.55f + Random.value * 0.2f; lluviaObj = 0; vientoObj = 0.35f + Random.value * 0.15f; rumboObj = 170 + Random.value * 40; duracion = 2 + Random.value * 4; break;
                case Tiempo.Tormenta: nubObj = 0.95f; lluviaObj = 0.7f + Random.value * 0.3f; vientoObj = 0.55f + Random.value * 0.2f; rumboObj = 120 + Random.value * 120; duracion = 1 + Random.value * 1.5f; break;
                case Tiempo.Pampero: nubObj = 0.2f; lluviaObj = 0; vientoObj = 0.85f + Random.value * 0.15f; rumboObj = 45; duracion = 3 + Random.value * 3; break;   // del sudoeste hacia el noreste
            }
            if (deGolpe) { nubosidad = nubObj; lluvia = lluviaObj; viento = vientoObj; rumboViento = rumboObj; }
        }

        Tiempo tiempoVisto;

        void Clima(float dt, float horas)
        {
            if (tiempo != tiempoVisto) { ElegirDestinos(tiempo, false); tiempoVisto = tiempo; }   // cambiado a mano
            if (climaAutomatico)
            {
                duracion -= horas;
                if (duracion <= 0)
                {
                    Tiempo sig = tiempo == Tiempo.Despejado ? Tiempo.Nublado
                        : tiempo == Tiempo.Nublado ? (Random.value < 0.5f ? Tiempo.Tormenta : Tiempo.Despejado)
                        : tiempo == Tiempo.Tormenta ? Tiempo.Pampero : Tiempo.Despejado;
                    ElegirDestinos(sig, false);
                    tiempoVisto = sig;
                }
            }
            // todo cambia de a poco
            float k = 1 - Mathf.Exp(-dt / 25f);
            nubosidad = Mathf.Lerp(nubosidad, nubObj, k);
            lluvia = Mathf.Lerp(lluvia, lluviaObj, 1 - Mathf.Exp(-dt / (lluviaObj > lluvia ? 40f : 15f)));
            viento = Mathf.Lerp(viento, vientoObj + Mathf.Sin(Time.time * 0.37f) * 0.08f, k);
            rumboViento = Mathf.LerpAngle(rumboViento, rumboObj, k * 0.5f);

            // relámpagos en la tormenta
            relampago = Mathf.Max(0, relampago - dt * 5);
            if (lluvia > 0.5f)
            {
                proxRelampago -= dt;
                if (proxRelampago <= 0) { relampago = 1; proxRelampago = 5 + Random.value * 15; }
            }
        }

        // ---------- sol, luna, cielo y niebla ----------
        void Sol(float dt)
        {
            // la hora del reloj va unos 40 minutos adelante del sol: el mediodía solar cae a las 12:40 y el sol se pone pasadas las 19:30
            float h = (hora - 12.65f) * 15 * Mathf.Deg2Rad, fi = LAT * Mathf.Deg2Rad, de = DECL * Mathf.Deg2Rad;
            float este = -Mathf.Cos(de) * Mathf.Sin(h);
            float norte = Mathf.Cos(fi) * Mathf.Sin(de) - Mathf.Sin(fi) * Mathf.Cos(de) * Mathf.Cos(h);
            float arriba = Mathf.Sin(fi) * Mathf.Sin(de) + Mathf.Cos(fi) * Mathf.Cos(de) * Mathf.Cos(h);
            var haciaSol = new Vector3(este, arriba, norte).normalized;

            float dia = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.09f, 0.18f, arriba));   // 0 de noche, 1 de día pleno (el crepúsculo dura)
            float bajo = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.0f, 0.35f, arriba)); // sol bajo: luz dorada
            LucesDeNoche = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.06f, 0.1f, arriba));
            float cubierto = Mathf.Clamp01(nubosidad * 1.1f - 0.1f);

            if (sol != null)
            {
                // de noche el sol se queda bajo el horizonte; se apaga
                var dirSol = arriba > -0.05f ? haciaSol : new Vector3(haciaSol.x, 0.02f, haciaSol.z).normalized;
                sol.transform.rotation = Quaternion.LookRotation(-dirSol);
                var dorado = new Color(1f, 0.58f, 0.32f);
                var mediodia = new Color(1f, 0.9f, 0.74f);
                sol.color = Color.Lerp(Color.Lerp(mediodia, dorado, bajo), new Color(0.8f, 0.82f, 0.86f), cubierto * 0.6f);
                sol.intensity = 2.1f * dia * (1 - 0.65f * cubierto) + relampago * 2.5f;
                sol.shadowStrength = Mathf.Lerp(1, 0.35f, cubierto);
            }
            if (luna != null)
            {
                // la luna, del lado contrario, alta en el cielo de la noche
                var dirLuna = new Vector3(-haciaSol.x * 0.6f, 0.75f, -haciaSol.z * 0.6f + 0.3f).normalized;
                luna.transform.rotation = Quaternion.LookRotation(-dirLuna);
                luna.intensity = 1.15f * LucesDeNoche * (1 - 0.5f * cubierto);
                luna.shadowStrength = 0.6f;
            }
            RenderSettings.sun = dia > 0.15f || luna == null ? sol : luna;

            // cielo y niebla: día polvoriento, atardecer naranja, noche azul, tormenta gris
            var cieloDia = new Color(0.81f, 0.88f, 0.96f);
            var cieloAtardecer = new Color(0.95f, 0.63f, 0.45f);
            var cieloNoche = new Color(0.27f, 0.31f, 0.42f);
            var cieloGris = new Color(0.55f, 0.57f, 0.6f);
            var cielo = Color.Lerp(Color.Lerp(cieloNoche, cieloAtardecer, dia), cieloDia, dia * (1 - bajo));
            cielo = Color.Lerp(cielo, cieloGris * Mathf.Lerp(0.25f, 1, dia), cubierto * 0.8f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = cielo * (1 + relampago * 2);
            RenderSettings.ambientEquatorColor = Color.Lerp(cielo, new Color(0.75f, 0.68f, 0.55f), 0.4f) * Mathf.Lerp(0.7f, 1, dia);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.3f, 0.22f) * Mathf.Lerp(0.5f, 1, dia);

            var nieblaAtardecer = new Color(0.95f, 0.66f, 0.48f);
            var nieblaNoche = new Color(0.09f, 0.1f, 0.16f);
            var nieblaGris = new Color(0.6f, 0.61f, 0.62f);
            var nb = Color.Lerp(Color.Lerp(nieblaNoche, nieblaAtardecer, dia), niebla0, dia * (1 - bajo));
            nb = Color.Lerp(nb, nieblaGris * Mathf.Lerp(0.2f, 1, dia), cubierto * 0.7f + lluvia * 0.3f);
            RenderSettings.fogColor = nb;
            // al amanecer la niebla baja se cierra (y más si llovió)
            float madrugada = Mathf.Clamp01(1 - Mathf.Abs(hora - 6.3f) / 2.2f) * nieblaMatinal * (0.5f + 0.5f * Mathf.Max(humedad, 1 - viento));
            Madrugada = madrugada;
            RenderSettings.fogStartDistance = Mathf.Lerp(Mathf.Lerp(nieblaIni0, 30, madrugada), 40, lluvia);
            RenderSettings.fogEndDistance = Mathf.Lerp(Mathf.Lerp(nieblaFin0, 420, madrugada), 600, lluvia) * (viento > 0.8f && humedad < 0.2f ? 0.75f : 1);   // el pampero levanta tierra
            nb = Color.Lerp(nb, new Color(0.78f, 0.8f, 0.8f) * Mathf.Lerp(0.3f, 1, dia), madrugada * 0.6f);
            RenderSettings.fogColor = nb;
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = nb;   // el horizonte se funde con la niebla
        }

        // ---------- posproceso según la hora y el tiempo ----------
        UnityEngine.Rendering.Volume volumen;
        UnityEngine.Rendering.Universal.ColorAdjustments ajuste;
        UnityEngine.Rendering.Universal.Vignette vineta;
        float exp0, sat0;
        Color filtro0;

        void Posproceso()
        {
            if (volumen == null)
            {
                volumen = FindAnyObjectByType<UnityEngine.Rendering.Volume>();
                if (volumen == null) return;
                var perfil = volumen.profile;   // en juego es una copia: no toca el asset
                perfil.TryGet(out ajuste);
                perfil.TryGet(out vineta);
                if (ajuste != null) { exp0 = ajuste.postExposure.value; sat0 = ajuste.saturation.value; filtro0 = ajuste.colorFilter.value; }
            }
            float noche = LucesDeNoche, gris = Mathf.Clamp01(nubosidad * 1.2f - 0.2f + lluvia * 0.4f);
            float h = hora, atardecer = Mathf.Clamp01(1 - Mathf.Abs(h - 18.6f) / 1.3f) + Mathf.Clamp01(1 - Mathf.Abs(h - 6.0f) / 1.0f) * 0.6f;
            if (ajuste != null)
            {
                ajuste.postExposure.value = exp0 + noche * 0.22f - gris * 0.1f;
                ajuste.saturation.value = Mathf.Lerp(sat0, -50, Mathf.Max(noche * 0.6f, gris * 0.5f));
                var filtro = Color.Lerp(filtro0, new Color(1.06f, 0.92f, 0.82f), atardecer * (1 - gris) * 0.7f);
                filtro = Color.Lerp(filtro, new Color(0.82f, 0.88f, 1.04f), noche * 0.8f);
                ajuste.colorFilter.value = filtro;
            }
            if (vineta != null) vineta.intensity.value = 0.28f + noche * 0.08f + lluvia * 0.06f;
        }

        // ---------- nubes ----------
        void Nubes(float dt)
        {
            if (sol == null || datosSol == null) return;
            if (nubes == null || Mathf.Abs(nubosidad - nubesHechas) > 0.03f) HacerNubes(nubosidad);
            if (sol.cookie != nubes) sol.cookie = nubes;
            const float tam = 900;
            datosSol.lightCookieSize = new Vector2(tam, tam);
            // las nubes van más rápido que el viento de abajo
            var v = Rumbo(rumboViento) * (2 + viento * 14);
            corrimiento += v * dt;
            datosSol.lightCookieOffset = corrimiento;
        }

        void HacerNubes(float cobertura)
        {
            const int S = 256;
            if (nubes == null)
                nubes = new Texture2D(S, S, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "Nubes", hideFlags = HideFlags.DontSave };
            var px = new Color32[S * S];
            float umbral = Mathf.Lerp(0.78f, 0.28f, cobertura);
            float sombra = Mathf.Lerp(0.55f, 0.35f, cobertura);   // cuánta luz pasa bajo una nube
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (float)x / S, w = (float)y / S;
                    float n = 0.55f * VR(u, w, 4, 1) + 0.3f * VR(u, w, 8, 2) + 0.15f * VR(u, w, 16, 3);
                    float c = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(umbral - 0.06f, umbral + 0.1f, n));
                    byte b = (byte)(255 * Mathf.Lerp(1, sombra, c));
                    px[y * S + x] = new Color32(b, b, b, b);
                }
            nubes.SetPixels32(px);
            nubes.Apply(false);
            nubesHechas = cobertura;
        }

        /// Ruido periódico (se repite sin costura).
        static float VR(float u, float v, int f, int sem)
        {
            float x = u * f, y = v * f;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float H(int a, int b) { a = ((a % f) + f) % f; b = ((b % f) + f) % f; return MapaPago.Hs(a * 127.1 + b * 311.7 + sem * 17.3); }
            return Mathf.Lerp(Mathf.Lerp(H(x0, y0), H(x0 + 1, y0), tx), Mathf.Lerp(H(x0, y0 + 1), H(x0 + 1, y0 + 1), tx), ty);
        }

        // ---------- lluvia y polvo ----------
        static Material MaterialParticula(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(sh) { name = "Partícula", hideFlags = HideFlags.DontSave };
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", c);
            return m;
        }

        ParticleSystem CrearLluvia()
        {
            var go = new GameObject("Lluvia");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.loop = true; main.startLifetime = 1.4f; main.startSpeed = 0; main.startSize = 0.06f;
            main.maxParticles = 6000; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.78f, 0.82f, 0.9f, 0.55f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(70, 1, 70);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = 0; vel.y = -24; vel.z = 0;
            var em = ps.emission; em.rateOverTime = 0;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 1;
            r.sharedMaterial = MaterialParticula(new Color(0.8f, 0.85f, 0.95f, 0.5f));
            ps.Play();
            return ps;
        }

        ParticleSystem CrearPolvo()
        {
            var go = new GameObject("Polvo");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.loop = true; main.startLifetime = 6; main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(3, 8);
            main.maxParticles = 300; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.75f, 0.62f, 0.45f, 0.12f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(90, 0.5f, 90);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var em = ps.emission; em.rateOverTime = 0;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = MaterialParticula(new Color(0.78f, 0.66f, 0.5f, 0.35f));
            ps.Play();
            return ps;
        }

        void Particulas()
        {
            var centro = foco != null ? foco.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            var w = Rumbo(rumboViento) * viento;
            if (gotas != null)
            {
                gotas.transform.position = centro + new Vector3(-w.x * 6, 22, -w.y * 6);
                var em = gotas.emission; em.rateOverTime = lluvia * 4500;
                var vel = gotas.velocityOverLifetime; vel.x = w.x * 9; vel.y = -24; vel.z = w.y * 9;
            }
            if (polvo != null)
            {
                polvo.transform.position = centro + new Vector3(0, 1.2f, 0);
                float seco = Mathf.Clamp01(1 - humedad * 3) * (1 - lluvia);
                var em = polvo.emission; em.rateOverTime = Mathf.Max(0, viento - 0.55f) * 120 * seco;
                var vel = polvo.velocityOverLifetime; vel.x = w.x * 12; vel.y = 0.4f; vel.z = w.y * 12;
            }
        }
    }
}
