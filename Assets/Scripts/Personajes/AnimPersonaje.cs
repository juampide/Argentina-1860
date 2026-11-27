using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Argentina1860
{
    /// Animación procedural del personaje: piernas, rodillas, brazos y codos que se balancean al caminar,
    /// y respiración y cabeceo en reposo (userData.animar del prototipo).
    /// Los modelos de Blender además reproducen clips de animación (Resources/Animaciones, de Mixamo): el clip se mezcla por peso sobre la pose procedural.
    public class AnimPersonaje
    {
        // ---------- clips (animaciones de Mixamo pasadas al esqueleto) ----------
        PlayableGraph grafo;
        AnimationPlayableOutput salidaClips;
        AnimationMixerPlayable mezclador;
        string[] nombresClips; AnimationClipPlayable[] playables; float[] pesos, objetivos;
        Transform[] todosHuesos; Vector3[] posReposo, escReposo; Quaternion[] rotReposo;

        void IniciarClips(Transform raiz)
        {
            var cargados = Resources.LoadAll<AnimationClip>("Animaciones");
            if (cargados.Length == 0) return;
            var animator = raiz.GetComponent<Animator>();
            if (animator == null) animator = raiz.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = null; animator.applyRootMotion = false;
            grafo = PlayableGraph.Create("clips_" + raiz.name);
            salidaClips = AnimationPlayableOutput.Create(grafo, "clips", animator);
            mezclador = AnimationMixerPlayable.Create(grafo, cargados.Length);
            int n = cargados.Length;
            nombresClips = new string[n]; playables = new AnimationClipPlayable[n]; pesos = new float[n]; objetivos = new float[n];
            for (int i = 0; i < n; i++)
            {
                nombresClips[i] = cargados[i].name;
                playables[i] = AnimationClipPlayable.Create(grafo, cargados[i]);
                grafo.Connect(playables[i], 0, mezclador, i);
                mezclador.SetInputWeight(i, 0f);
            }
            salidaClips.SetSourcePlayable(mezclador);
            salidaClips.SetWeight(0f);
            todosHuesos = raiz.GetComponentsInChildren<Transform>(true);
            posReposo = new Vector3[todosHuesos.Length]; rotReposo = new Quaternion[todosHuesos.Length]; escReposo = new Vector3[todosHuesos.Length];
            for (int i = 0; i < todosHuesos.Length; i++) { posReposo[i] = todosHuesos[i].localPosition; rotReposo[i] = todosHuesos[i].localRotation; escReposo[i] = todosHuesos[i].localScale; }
            grafo.Play();
        }

        int IndiceClip(string nombre) => nombresClips == null ? -1 : System.Array.IndexOf(nombresClips, nombre);
        public bool TieneClip(string nombre) => grafo.IsValid() && IndiceClip(nombre) >= 0;
        bool ConLocomocion => TieneClip("Reposo") && TieneClip("Caminar");

        /// Todos los huesos vuelven al reposo (la base sobre la que se pone la pose procedural o el clip).
        void ReposoLimpio()
        {
            if (todosHuesos == null) return;
            for (int i = 0; i < todosHuesos.Length; i++)
            {
                var t = todosHuesos[i];
                if (t == null) continue;
                t.localPosition = posReposo[i]; t.localRotation = rotReposo[i]; t.localScale = escReposo[i];
            }
        }

        /// Pide que un clip pese esto en la mezcla de este cuadro (los pesos se suavizan solos: las transiciones entre clips duran unos 0,25 s).
        void Pedir(string nombre, float peso) { int i = IndiceClip(nombre); if (i >= 0) objetivos[i] = Mathf.Max(objetivos[i], peso); }

        /// Aplica lo pedido. Sin pedidos, los clips se apagan y manda la pose procedural.
        void AplicarClips()
        {
            if (!grafo.IsValid()) return;
            float dt = Time.deltaTime, suma = 0;
            for (int i = 0; i < pesos.Length; i++)
            {
                // solo "Hablar" arranca desde el principio; reposo y marcha siguen su ciclo (Reposo empieza con la cabeza gacha y daba un sacudón)
                if (pesos[i] < 0.001f && objetivos[i] > 0f && nombresClips[i] == "Hablar") playables[i].SetTime(0);
                pesos[i] = Mathf.MoveTowards(pesos[i], objetivos[i], dt * 3f);
                suma += pesos[i];
            }
            for (int i = 0; i < pesos.Length; i++) { mezclador.SetInputWeight(i, suma > 0.001f ? pesos[i] / suma : 0f); objetivos[i] = 0f; }
            salidaClips.SetWeight(Mathf.Min(1f, suma));
        }

        void ApagarClips()
        {
            if (!grafo.IsValid()) return;
            for (int i = 0; i < pesos.Length; i++) { pesos[i] = 0; mezclador.SetInputWeight(i, 0f); }
            salidaClips.SetWeight(0f);
        }

        public void Liberar() { if (grafo.IsValid()) grafo.Destroy(); }

        public Transform cuerpo, torso, cab;
        public readonly List<Transform> piernas = new List<Transform>(), rodillas = new List<Transform>();
        public readonly List<Transform> brazos = new List<Transform>(), codos = new List<Transform>();
        public bool vestido;
        /// Cuánto se cierran los brazos hacia el cuerpo respecto del reposo: un esqueleto exportado en pose A los trae abiertos (unos 0,34 rad).
        float aducir;
        float hablaW, cuerpoX;

        // ---------- manos y dedos (solo modelos con esqueleto) ----------
        public enum PoseMano { Relajada, Abierta, Senalar, Cuenta, PalmaArriba, Puno }
        // curvatura de cada dedo (0 estirado, 1 cerrado): pulgar, índice, medio, anular, meñique
        static readonly float[][] POSES =
        {
            new[] { 0.20f, 0.30f, 0.35f, 0.40f, 0.45f },   // Relajada
            new[] { 0.00f, 0.04f, 0.05f, 0.08f, 0.10f },   // Abierta
            new[] { 0.30f, 0.00f, 0.85f, 0.90f, 0.95f },   // Señalar
            new[] { 0.20f, 0.05f, 0.10f, 0.85f, 0.90f },   // Cuenta (dos dedos)
            new[] { 0.05f, 0.12f, 0.16f, 0.20f, 0.26f },   // PalmaArriba
            new[] { 0.70f, 0.90f, 0.95f, 0.95f, 0.95f },   // Puño
        };
        static readonly float[] MAX_DEDO = { 80f, 95f, 70f }, MAX_PULGAR = { 20f, 30f, 25f };   // grados por falange con curvatura 1
        struct Falange { public Transform t; public Quaternion reposo, cc; public Vector3 eje; }
        Falange[][,] dedos;          // [mano][dedo, falange]
        Falange[] muneca;            // [mano]
        float[][] curva;             // curvatura actual de cada dedo
        float[] flexMuneca;
        static readonly string[] NOMBRES_DEDO = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

        Falange Enlazar(Transform t, Transform hijo, Vector3 dirPrevia)
        {
            var inv = Quaternion.Inverse(raizHuesos.rotation);
            var f = new Falange { t = t, reposo = t.localRotation, cc = inv * t.rotation };
            Vector3 d = hijo != null ? inv * (hijo.position - t.position) : dirPrevia;
            if (d.sqrMagnitude < 1e-8f) d = Vector3.down;
            d.Normalize();
            float x = (inv * (t.position - raizHuesos.position)).x;
            Vector3 palma = x > 0 ? Vector3.left : Vector3.right;           // la palma mira hacia el cuerpo
            var eje = Vector3.Cross(d, palma);
            f.eje = eje.sqrMagnitude < 1e-8f ? Vector3.forward : eje.normalized;   // gira de la dirección del dedo hacia la palma
            dirDedo = d;
            return f;
        }
        Vector3 dirDedo;

        void EnlazarManos(Transform raiz)
        {
            var d = new Falange[2][,];
            var m = new Falange[2];
            for (int i = 0; i < 2; i++)
            {
                string lado = i == 0 ? "R" : "L";
                d[i] = new Falange[5, 3];
                for (int j = 0; j < 5; j++)
                {
                    var falanges = new Transform[3];
                    for (int k = 0; k < 3; k++) falanges[k] = Buscar(raiz, NOMBRES_DEDO[j] + "_" + (k + 1) + "_" + lado);
                    if (System.Array.IndexOf(falanges, null) >= 0) return;   // el modelo no trae dedos
                    Vector3 previa = Vector3.down;
                    for (int k = 0; k < 3; k++)
                    {
                        d[i][j, k] = Enlazar(falanges[k], k < 2 ? falanges[k + 1] : null, previa);
                        previa = dirDedo;
                    }
                }
                var hueso = Buscar(raiz, "Hand_" + lado);
                var medio = Buscar(raiz, "Middle_1_" + lado);
                if (hueso == null) return;
                m[i] = Enlazar(hueso, medio, Vector3.down);
            }
            dedos = d; muneca = m;
            curva = new[] { (float[])POSES[0].Clone(), (float[])POSES[0].Clone() };
            flexMuneca = new float[2];
        }

        /// Lleva los dedos de la mano i (0 el lado +X, 1 el otro) hacia una pose, con transición suave. flex: flexión de la muñeca en radianes.
        public void Mano(int i, PoseMano pose, float flex = 0f)
        {
            if (dedos == null || i < 0 || i >= 2) return;
            float dt = Time.deltaTime;
            var objetivo = POSES[(int)pose];
            for (int j = 0; j < 5; j++)
            {
                curva[i][j] = Mathf.MoveTowards(curva[i][j], objetivo[j], dt * 4.5f);
                for (int k = 0; k < 3; k++)
                {
                    var f = dedos[i][j, k];
                    float ang = curva[i][j] * (j == 0 ? MAX_PULGAR[k] : MAX_DEDO[k]);
                    f.t.localRotation = f.reposo * Quaternion.Inverse(f.cc) * Quaternion.AngleAxis(ang, f.eje) * f.cc;
                }
            }
            flexMuneca[i] = Mathf.MoveTowards(flexMuneca[i], flex, dt * 2.5f);
            var w = muneca[i];
            w.t.localRotation = w.reposo * Quaternion.Inverse(w.cc) * Quaternion.AngleAxis(flexMuneca[i] * Mathf.Rad2Deg, w.eje) * w.cc;
        }
        float Abre(float a) => a - aducir;

        // ---------- esqueleto (modelos hechos en Blender) ----------
        // El modelo de código tiene todos sus nodos con rotación de reposo nula y orientados como el "marco del prototipo" (girado 180° sobre Y).
        // Un esqueleto importado tiene sus propios ejes, así que cada hueso guarda su pose de reposo y las mismas rotaciones se le aplican en ese marco.
        struct Hueso { public Quaternion reposo, cb; public Vector3 pos; }
        Dictionary<Transform, Hueso> huesos;
        Transform raizHuesos;

        Quaternion R(Transform t, float x, float y, float z)
        {
            var q = Conv.Rot(x, y, z);
            if (huesos != null && t != null && huesos.TryGetValue(t, out var h)) return h.reposo * Quaternion.Inverse(h.cb) * q * h.cb;
            return q;
        }

        Vector3 Pos(Transform t, float y)
        {
            var p = Conv.P(0, y, 0);
            if (huesos != null && t != null && huesos.TryGetValue(t, out var h))
            {
                var mundo = raizHuesos.TransformVector(p);
                return h.pos + (t.parent != null ? t.parent.InverseTransformVector(mundo) : mundo);
            }
            return p;
        }

        Vector3 Esc(Transform t, float x, float y, float z)
        {
            if (huesos != null && t != null && huesos.ContainsKey(t)) return Vector3.one * y;   // un hueso no escala por eje: respiración pareja
            return new Vector3(x, y, z);
        }

        void Guardar(Transform t)
        {
            if (t == null || huesos.ContainsKey(t)) return;
            var marco = raizHuesos.rotation * Quaternion.Euler(0, 180, 0);
            huesos[t] = new Hueso { reposo = t.localRotation, cb = Quaternion.Inverse(marco) * t.rotation, pos = t.localPosition };
        }

        static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) if (t.name == nombre) return t;
            return null;
        }

        /// Arma el animador sobre un esqueleto con los huesos nombrados como en Arte/blender/rig.py (Hips, Spine, Head, UpperLeg_R...).
        /// El índice 0 de piernas y brazos es el lado +X, igual que en el modelo de código.
        public static AnimPersonaje DesdeHuesos(Transform raiz)
        {
            var a = new AnimPersonaje { raizHuesos = raiz, huesos = new Dictionary<Transform, Hueso>() };
            a.cuerpo = Buscar(raiz, "Hips"); a.torso = Buscar(raiz, "Spine"); a.cab = Buscar(raiz, "Head");
            foreach (var lado in new[] { "R", "L" })
            {
                a.piernas.Add(Buscar(raiz, "UpperLeg_" + lado)); a.rodillas.Add(Buscar(raiz, "LowerLeg_" + lado));
                a.brazos.Add(Buscar(raiz, "UpperArm_" + lado)); a.codos.Add(Buscar(raiz, "LowerArm_" + lado));
            }
            if (a.cuerpo == null || a.cab == null || a.piernas.Contains(null) || a.brazos.Contains(null)) { Debug.LogWarning("AnimPersonaje: faltan huesos en " + raiz.name); return null; }
            foreach (var t in new[] { a.cuerpo, a.torso, a.cab }) a.Guardar(t);
            foreach (var l in new[] { a.piernas, a.rodillas, a.brazos, a.codos }) foreach (var t in l) a.Guardar(t);
            a.aducir = 0.34f;
            a.EnlazarManos(raiz);
            a.IniciarClips(raiz);
            return a;
        }

        /// Sentado a caballo: muslos hacia adelante y abiertos, rodillas dobladas, manos en las riendas.
        public void Montar()
        {
            for (int i = 0; i < piernas.Count; i++) piernas[i].localRotation = R(piernas[i], -1.3f, 0, i == 0 ? -0.5f : 0.5f);
            foreach (var r in rodillas) r.localRotation = R(r, 1.25f, 0, 0);
            for (int i = 0; i < brazos.Count; i++) brazos[i].localRotation = R(brazos[i], -0.7f, 0, (i == 0 ? -1 : 1) * 0.1f);
            foreach (var c in codos) c.localRotation = R(c, -0.9f, 0, 0);
        }

        /// Jinete en movimiento: la pose de Montar con el vaivén del paso. fase: la del caballo; g: 0 quieto o al trote,
        /// 1 al galope (se inclina hacia adelante y acompaña la hamaca); t: tiempo, para la cabeza en reposo.
        public void Jinete(float fase, float v, float g, float t)
        {
            if (cuerpo == null) return;
            ApagarClips();
            Montar();
            float trote = Mathf.Min(1, v) * (1 - g);
            cuerpo.localPosition = Pos(cuerpo, Mathf.Abs(Mathf.Sin(fase)) * (0.05f * trote + 0.025f * g));
            cuerpo.localRotation = R(cuerpo, 0.12f * g + Mathf.Sin(fase) * 0.05f * g, 0, 0);   // positivo: hacia adelante (como Cocinando)
            torso.localScale = Esc(torso, 1, 1, 1);
            float reposo = 1 - Mathf.Min(1, v);
            cab.localRotation = R(cab, 0, Mathf.Sin(t * 0.37f) * 0.25f * reposo, 0);
        }

        /// Sentado en un tronco, charlando: muslos adelante, rodillas dobladas, manos sobre las rodillas.
        /// gesto (0 a 1) levanta una mano como quien cuenta algo; mira gira la cabeza hacia los otros (radianes).
        /// baja: cuánto baja la cadera (con vestido, menos, porque no hay piernas).
        public void Sentado(float t, float gesto, float mira, float baja = 0.44f)
        {
            if (cuerpo == null) return;
            ApagarClips();
            for (int i = 0; i < piernas.Count; i++)
            {
                piernas[i].localRotation = R(piernas[i], -1.45f, 0, (i == 0 ? -1 : 1) * 0.14f);
                rodillas[i].localRotation = R(rodillas[i], 1.4f, 0, 0);
            }
            float abre = vestido ? 0.2f : 0.1f;
            for (int i = 0; i < brazos.Count; i++)
            {
                float dir = i == 0 ? -1 : 1, g = i == 1 ? gesto : 0;
                brazos[i].localRotation = R(brazos[i], -0.5f - g * 0.75f + Mathf.Sin(t * 4.1f) * 0.12f * g, 0, dir * abre);
                codos[i].localRotation = R(codos[i], -0.75f - g * 0.6f, 0, 0);
            }
            cuerpo.localPosition = Pos(cuerpo, -baja);
            cuerpo.localRotation = R(cuerpo, 0.12f, 0, 0);
            torso.localScale = Esc(torso, 1, 1 + Mathf.Sin(t * 0.9f) * 0.006f, 1);
            cab.localRotation = R(cab, 0.05f + Mathf.Sin(t * 0.31f) * 0.05f, mira, 0);
        }

        /// Parado e inclinado sobre la olla o el asado: un brazo revuelve (o acomoda la carne) y el otro descansa.
        /// ritmo: vueltas del cucharón por segundo (más lento para el asado).
        public void Cocinando(float t, float ritmo = 0.45f)
        {
            if (cuerpo == null) return;
            ApagarClips();
            foreach (var p in piernas) p.localRotation = R(p, -0.12f, 0, 0);
            foreach (var r in rodillas) r.localRotation = R(r, 0.22f, 0, 0);
            float w = t * ritmo * Mathf.PI * 2;
            float abre = vestido ? 0.24f : 0.08f;
            if (brazos.Count > 1)
            {
                brazos[1].localRotation = R(brazos[1], -1.0f + Mathf.Sin(w) * 0.16f, 0, 0.12f + Mathf.Cos(w) * 0.18f);
                codos[1].localRotation = R(codos[1], -0.55f + Mathf.Cos(w) * 0.1f, 0, 0);
                brazos[0].localRotation = R(brazos[0], -0.25f, 0, -abre);
                codos[0].localRotation = R(codos[0], -0.9f, 0, 0);   // la mano en la cintura
            }
            cuerpo.localPosition = Pos(cuerpo, -0.04f);
            cuerpo.localRotation = R(cuerpo, 0.24f, 0, 0);
            torso.localScale = Esc(torso, 1, 1, 1);
            cab.localRotation = R(cab, 0.28f, Mathf.Sin(t * 0.27f) * 0.2f, 0);
        }

        /// Parado, conversando: se acomoda sobre una pierna y respira; al hablar gesticula con una mano y el antebrazo, asiente y se inclina un poco hacia
        /// quien escucha. habla: si es él quien habla ahora. t: tiempo en segundos.
        public void Conversar(float t, bool habla)
        {
            if (cuerpo == null) return;
            float dt = Time.deltaTime;
            hablaW = Mathf.MoveTowards(hablaW, habla ? 1f : 0f, dt * 2.5f);
            float w = hablaW;
            ReposoLimpio();
            if (ConLocomocion && TieneClip("Hablar")) { Pedir(habla ? "Hablar" : "Reposo", 1f); Mano(0, PoseMano.Relajada, 0f); Mano(1, habla ? PoseMano.Abierta : PoseMano.Relajada, 0f); AplicarClips(); return; }
            // postura: el peso pasa de una pierna a la otra muy despacio
            float peso = Mathf.Sin(t * 0.42f);
            for (int i = 0; i < piernas.Count; i++)
            {
                float lado = i == 0 ? 1 : -1;
                piernas[i].localRotation = R(piernas[i], -0.03f + lado * peso * 0.02f, 0, lado * peso * 0.035f);
                rodillas[i].localRotation = R(rodillas[i], 0.03f + Mathf.Max(0, lado * peso) * 0.07f, 0, 0);
            }
            cuerpo.localPosition = Pos(cuerpo, -Mathf.Abs(peso) * 0.006f);
            cuerpo.localRotation = R(cuerpo, 0.02f + w * 0.02f, Mathf.Sin(t * 0.31f) * 0.05f, peso * 0.03f);
            torso.localScale = Esc(torso, 1, 1 + Mathf.Sin(t * 1.3f) * 0.007f, 1);
            if (torso != null) torso.localRotation = R(torso, 0.03f + w * (0.05f + 0.025f * Mathf.Sin(t * 1.9f)), Mathf.Sin(t * 0.37f + 1f) * 0.06f, -peso * 0.02f);
            // brazos: reposo con vaivén mínimo; al hablar, el derecho dibuja gestos y el izquierdo acompaña a veces
            for (int i = 0; i < brazos.Count; i++)
            {
                float dir = i == 0 ? -1 : 1;
                float gesto = i == 1 ? w : w * 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 0.9f + 2f));
                float oleada = 0.5f + 0.5f * Mathf.Sin(t * (i == 1 ? 2.3f : 1.4f) + i);
                float abre = Abre(vestido ? 0.24f : 0.05f);
                brazos[i].localRotation = R(brazos[i], -0.05f - gesto * (0.45f + 0.35f * oleada) + Mathf.Sin(t * 0.5f + i) * 0.015f, 0, dir * (abre + 0.04f) + dir * gesto * 0.22f * Mathf.Sin(t * 1.7f + i));
                codos[i].localRotation = R(codos[i], -0.18f - gesto * (0.55f + 0.45f * Mathf.Sin(t * 2.9f + 1f + i) * 0.5f), 0, 0);
            }
            // manos: al hablar, la activa cambia de seña cada tanto (abierta, señalando, palma arriba, contando); la otra descansa
            if (dedos != null)
            {
                int tramo = Mathf.FloorToInt(t * 0.5f);
                PoseMano[] senas = { PoseMano.Abierta, PoseMano.Senalar, PoseMano.PalmaArriba, PoseMano.Cuenta, PoseMano.Abierta, PoseMano.Relajada, PoseMano.PalmaArriba };
                var sena = senas[Mathf.Abs(tramo * 7919 + 13) % senas.Length];
                Mano(1, w > 0.4f ? sena : PoseMano.Relajada, w * (0.15f + 0.2f * Mathf.Sin(t * 1.9f)));
                Mano(0, w > 0.4f && Mathf.Sin(t * 0.7f + 2f) > 0.6f ? PoseMano.Abierta : PoseMano.Relajada, w * 0.08f * Mathf.Sin(t * 1.3f));
            }
            // cabeza: asiente al hablar, mueve apenas al escuchar
            cab.localRotation = R(cab, 0.03f + w * 0.07f * Mathf.Sin(t * 2.1f), Mathf.Sin(t * 0.33f) * (0.12f - 0.07f * w), Mathf.Sin(t * 0.23f) * 0.04f);
        }

        /// t: fase del paso; v: 0 quieto, 1 caminando, 1.35 corriendo.
        public void Animar(float t, float v)
        {
            if (cuerpo == null) return;
            ReposoLimpio();
            if (ConLocomocion)
            {
                if (v <= 0.02f) Pedir("Reposo", 1f);
                else if (v <= 1f || !TieneClip("Correr")) { Pedir("Caminar", Mathf.Min(1f, v)); Pedir("Reposo", 1f - Mathf.Min(1f, v)); }
                else { float k = Mathf.Clamp01((v - 1f) / 0.35f); Pedir("Caminar", 1 - k); Pedir("Correr", k); }
                Mano(0, PoseMano.Relajada, 0f); Mano(1, PoseMano.Relajada, 0f);
                AplicarClips();
                return;
            }
            float a = Mathf.Sin(t) * 0.6f * v;
            for (int i = 0; i < piernas.Count; i++)
            {
                float fase = i == 0 ? t : t + Mathf.PI;
                piernas[i].localRotation = R(piernas[i], Mathf.Sin(fase) * 0.6f * v, 0, 0);
                rodillas[i].localRotation = R(rodillas[i], Mathf.Max(0, Mathf.Sin(fase - 1.3f)) * 0.9f * v, 0, 0);
            }
            float abre = vestido ? 0.24f : 0.05f;
            for (int i = 0; i < brazos.Count; i++)
            {
                float dir = i == 0 ? -1 : 1;
                brazos[i].localRotation = R(brazos[i], dir * a * 0.75f, 0, dir * Abre(abre));
                codos[i].localRotation = R(codos[i], -0.18f - Mathf.Max(0, dir * -a) * 0.9f * (v != 0 ? 1 : 0), 0, 0);
            }
            Mano(0, PoseMano.Relajada, 0f); Mano(1, PoseMano.Relajada, 0f);
            float reposo = 1 - Mathf.Min(1, v);
            cuerpo.localPosition = Pos(cuerpo, Mathf.Abs(Mathf.Sin(t)) * 0.04f * v);
            cuerpo.localRotation = R(cuerpo, 0, Mathf.Sin(t) * 0.08f * v, 0);
            torso.localScale = Esc(torso, 1, 1 + Mathf.Sin(t * 0.9f) * 0.006f * reposo, 1);
            cab.localRotation = R(cab, 0, Mathf.Sin(t * 0.37f) * 0.25f * reposo, Mathf.Sin(t * 0.23f) * 0.05f * reposo);
        }
    }
}
