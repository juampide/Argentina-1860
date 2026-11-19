using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argentina1860
{
    /// <summary>
    /// Pasto de la pampa de 1867 alrededor del jugador (campo.js): matas altas y duras que se mecen con el viento
    /// y cubren todo ("mar de pasto"); en el pajonal llegan a dos metros. Cada mata es un manojo de once hojas.
    /// Se regenera al caminar y se dibuja con instancias por GPU (shader Argentina/Pasto).
    ///
    /// Cerca de las casas y a la orilla de los caminos el pasto está comido y pisoteado: es más corto y más parejo
    /// (ver MapaPago.Pastoreo). No crece en los sembrados.
    ///
    /// Pisadas: todo lo anotado en Pisadas (jugador, personajes, animales grandes, carretas) dobla las matas hacia
    /// afuera al pasar. Quedan aplastadas y se levantan de a poco, así se ve el rastro. Se guarda en una textura
    /// que acompaña al jugador y el shader la lee.
    /// </summary>
    public class Pasto : MonoBehaviour
    {
        public Transform jugador;
        public Material material;
        [Tooltip("Máximo de matas dibujadas")]
        public int capacidad = 26000;

        [Header("Pasto comido cerca de la gente")]
        [Tooltip("Altura del pasto al lado de las casas, comparada con el campo abierto (1 = igual)")]
        [Range(0.1f, 1)] public float alturaPastoreo = 0.3f;
        [Tooltip("Hasta dónde llega el pasto corto alrededor de las casas y los caminos (1 = normal)")]
        [Range(0, 3)] public float alcancePastoreo = 1;
        [Tooltip("Densidad de matas en el pasto corto (en campo abierto es 0,3)")]
        [Range(0.1f, 1)] public float densidadPastoreo = 0.45f;

        [Header("Pisadas")]
        [Tooltip("Segundos que tarda una mata aplastada en volver a levantarse")]
        public float recuperacion = 40;
        [Tooltip("Cuánto se dobla una mata pisada (0 = nada, 1 = queda tirada)")]
        [Range(0, 1)] public float fuerzaPisada = 0.8f;

        const int LOTE = 1023;
        Mesh malla;
        readonly List<Matrix4x4[]> lotesM = new List<Matrix4x4[]>();
        readonly List<MaterialPropertyBlock> lotesC = new List<MaterialPropertyBlock>();
        readonly List<int> lotesN = new List<int>();
        Vector2 ultimo = new Vector2(1e9f, 1e9f);
        static readonly int idColor = Shader.PropertyToID("_ColorInst"), idPisadaTex = Shader.PropertyToID("_PisadaTex"),
            idPisadaOrigen = Shader.PropertyToID("_PisadaOrigen"), idPisadaFuerza = Shader.PropertyToID("_PisadaFuerza");

        void Start()
        {
            malla = CrearMalla();
            if (material == null)
            {
                var sh = Shader.Find("Argentina/Pasto");
                if (sh != null) material = new Material(sh) { enableInstancing = true, name = "Pasto", hideFlags = HideFlags.DontSave };
            }
            if (jugador != null) Pisadas.Registrar(jugador, 0.65f);
            IniciarPisadas();
        }

        void OnDestroy()
        {
            if (texPisada != null) Destroy(texPisada);
        }

        /// Manojo de once hojas en cono, base oscura y punta clara.
        static Mesh CrearMalla()
        {
            var mt = new MallaTres();
            const int n = 11;
            for (int i = 0; i < n; i++)
            {
                float ang = i * 2.399f, r = 0.05f + 0.32f * Mathf.Sqrt((i + 0.5f) / n), h = 0.75f + 0.55f * MapaPago.Hs(i * 3.7 + 1), lean = 0.12f + 0.25f * MapaPago.Hs(i * 5.1 + 2);
                mt.Agregar(Geo.Cono(0.075f, h, 3), "pasto", Matrix4x4.TRS(new Vector3(Mathf.Cos(ang) * r, h / 2, Mathf.Sin(ang) * r), Conv.Q3(new Vector3(Mathf.Sin(ang) * lean, MapaPago.Hs(i) * 3, -Mathf.Cos(ang) * lean)), Vector3.one));
            }
            var m = mt.Construir("mata de pasto", out _);
            var vs = m.vertices;
            var cs = new Color[vs.Length];
            for (int i = 0; i < vs.Length; i++)
            {
                float t = Mathf.Clamp01(vs[i].y / 1.2f), v = 0.45f + 0.65f * t;
                cs[i] = new Color(v, v, v * 0.95f, 1);
            }
            m.colors = cs;
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 4);
            return m;
        }

        static Vector4 ColorHSL(float h, float s, float l)
        {
            var c = Conv.HSL(h, s, l).linear;   // three.js deja el color HSL en espacio lineal
            return new Vector4(c.r, c.g, c.b, 1);
        }

        void Regenerar(float px, float pz)
        {
            const float CELDA = 6, R = 78;
            lotesN.Clear();
            int n = 0, lote = -1;
            for (int gx = Mathf.FloorToInt((px - R) / CELDA); gx <= Mathf.FloorToInt((px + R) / CELDA) && n < capacidad; gx++)
                for (int gz = Mathf.FloorToInt((pz - R) / CELDA); gz <= Mathf.FloorToInt((pz + R) / CELDA) && n < capacidad; gz++)
                {
                    float ccx = (gx + 0.5f) * CELDA, ccz = (gz + 0.5f) * CELDA, d = Mathf.Sqrt((ccx - px) * (ccx - px) + (ccz - pz) * (ccz - pz));
                    if (d > R) continue;
                    float kx = (ccx + 1600) / 1000, ky = (ccz + 1200) / 1000;
                    bool pajonal = kx < 1.55f && ky > 1.5f;
                    float parche = MapaPago.Mix(0.35f, 1.5f, MapaPago.Ruido(ccx * 0.02, ccz * 0.02)) * MapaPago.Mix(0.7f, 1.3f, MapaPago.Ruido(ccx * 0.09 + 5, ccz * 0.09));   // manchones más ralos y más tupidos
                    float comidoCelda = MapaPago.Pastoreo(ccx, ccz, alcancePastoreo);
                    float dens = MapaPago.Mix((pajonal ? 0.75f : 0.3f) * parche, densidadPastoreo, comidoCelda) * (d > R * 0.7f ? 1 - (d - R * 0.7f) / (R * 0.3f) : 1);
                    int k = Conv.RoundJS(dens * CELDA * CELDA);
                    for (int i = 0; i < k && n < capacidad; i++)
                    {
                        double seed = gx * 7919.0 + gz * 104729.0 + i * 13.0;
                        float x = (gx + MapaPago.Hs(seed)) * CELDA, z = (gz + MapaPago.Hs(seed + 1)) * CELDA;
                        if (!MapaPago.PuedeCrecer(x, z) || !MapaPago.LibreDeSitios(x, z, 0.8f) || Sembrado.Ocupa(x, z)) continue;
                        float comido = MapaPago.Pastoreo(x, z, alcancePastoreo);
                        float alto = (pajonal ? 1.45f : 0.8f) * MapaPago.Mix(0.75f, 1.45f, MapaPago.Hs(seed + 2)) * MapaPago.Mix(1, alturaPastoreo, comido);
                        float ancho = MapaPago.Mix(0.85f, 1.45f, MapaPago.Hs(seed + 7)) * MapaPago.Mix(1, 1.25f, comido);
                        var pos = new Vector3(x, Terreno.Altura(new Vector3(x, 0, -z)), -z);
                        if (n % LOTE == 0)
                        {
                            lote++;
                            if (lote >= lotesM.Count)
                            {
                                lotesM.Add(new Matrix4x4[LOTE]);
                                var b = new MaterialPropertyBlock();
                                b.SetVectorArray(idColor, new Vector4[LOTE]);
                                lotesC.Add(b);
                            }
                            lotesN.Add(0);
                        }
                        int j = n % LOTE;
                        lotesM[lote][j] = Matrix4x4.TRS(pos, Conv.Yaw(MapaPago.Hs(seed + 3) * 6.28f), new Vector3(ancho, alto, ancho));
                        // el pasto comido rebrota más verde y más parejo
                        float hue = MapaPago.Mix(0.11f, 0.24f, MapaPago.Hs(seed + 4) * (pajonal ? 0.7f : 1));
                        hue = MapaPago.Mix(hue, MapaPago.Mix(0.19f, 0.26f, MapaPago.Hs(seed + 4)), comido * 0.7f);
                        colores[lote][j] = ColorHSL(hue, MapaPago.Mix(0.3f, 0.6f, MapaPago.Hs(seed + 5)), MapaPago.Mix(0.34f, 0.54f, MapaPago.Hs(seed + 6)));
                        lotesN[lote] = j + 1;
                        n++;
                    }
                }
            for (int i = 0; i < lotesN.Count; i++) lotesC[i].SetVectorArray(idColor, colores[i]);
            ultimo = new Vector2(px, pz);
        }

        readonly List<Vector4[]> colores = new List<Vector4[]>();

        // ---------- pisadas ----------
        // Textura de TEX × TEX celdas de CEL metros que sigue al jugador (en coordenadas de Unity).
        // R: cuánto está aplastado; G y B: hacia dónde se dobló (x, z).
        const int TEX = 320;
        const float CEL = 0.5f, CORRER = 8, CADA = 0.05f;
        Texture2D texPisada;
        float[] apl, dirX, dirZ;
        Color32[] px32;
        Vector2 origen;   // esquina de la textura en el mundo (x, z de Unity)
        float reloj;
        readonly Dictionary<Transform, Vector3> antes = new Dictionary<Transform, Vector3>();

        void IniciarPisadas()
        {
            int n = TEX * TEX;
            apl = new float[n]; dirX = new float[n]; dirZ = new float[n]; px32 = new Color32[n];
            texPisada = new Texture2D(TEX, TEX, TextureFormat.RGBA32, false, true)
            {
                name = "Pisadas", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            var p = jugador != null ? jugador.position : Vector3.zero;
            origen = Ajustar(new Vector2(p.x, p.z));
            SubirPisadas();
        }

        static Vector2 Ajustar(Vector2 centro)
        {
            float mitad = TEX * CEL / 2;
            return new Vector2(Mathf.Round((centro.x - mitad) / CEL) * CEL, Mathf.Round((centro.y - mitad) / CEL) * CEL);
        }

        /// Corre la textura cuando el jugador se aleja del centro, conservando lo que ya estaba pisado.
        void Recentrar(Vector3 p)
        {
            float mitad = TEX * CEL / 2;
            var centro = origen + new Vector2(mitad, mitad);
            if (Mathf.Abs(p.x - centro.x) < CORRER && Mathf.Abs(p.z - centro.y) < CORRER) return;
            var nuevo = Ajustar(new Vector2(p.x, p.z));
            int sx = Mathf.RoundToInt((nuevo.x - origen.x) / CEL), sz = Mathf.RoundToInt((nuevo.y - origen.y) / CEL);
            var a2 = new float[apl.Length]; var x2 = new float[apl.Length]; var z2 = new float[apl.Length];
            for (int z = 0; z < TEX; z++)
            {
                int oz = z + sz;
                if (oz < 0 || oz >= TEX) continue;
                for (int x = 0; x < TEX; x++)
                {
                    int ox = x + sx;
                    if (ox < 0 || ox >= TEX) continue;
                    int k = z * TEX + x, o = oz * TEX + ox;
                    a2[k] = apl[o]; x2[k] = dirX[o]; z2[k] = dirZ[o];
                }
            }
            apl = a2; dirX = x2; dirZ = z2;
            origen = nuevo;
        }

        void Pisar(Vector3 c, float radio, Vector2 marcha)
        {
            float rIn = radio * 0.5f, rOut = radio * 1.7f;
            int cx = Mathf.FloorToInt((c.x - origen.x) / CEL), cz = Mathf.FloorToInt((c.z - origen.y) / CEL), rr = Mathf.CeilToInt(rOut / CEL);
            for (int z = Mathf.Max(0, cz - rr); z <= Mathf.Min(TEX - 1, cz + rr); z++)
                for (int x = Mathf.Max(0, cx - rr); x <= Mathf.Min(TEX - 1, cx + rr); x++)
                {
                    float wx = origen.x + (x + 0.5f) * CEL - c.x, wz = origen.y + (z + 0.5f) * CEL - c.z;
                    float d = Mathf.Sqrt(wx * wx + wz * wz);
                    if (d > rOut) continue;
                    float v = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(rIn, rOut, d));
                    int k = z * TEX + x;
                    if (v <= apl[k]) continue;
                    // se dobla hacia afuera del que pisa y un poco hacia donde va
                    var dir = (d > 0.01f ? new Vector2(wx, wz) / d : Vector2.zero) + marcha * 0.9f;
                    if (dir.sqrMagnitude > 1e-4f) dir.Normalize();
                    apl[k] = v; dirX[k] = dir.x; dirZ[k] = dir.y;
                }
        }

        void ActualizarPisadas(Vector3 pj, float dt)
        {
            reloj += dt;
            if (reloj < CADA) return;
            float paso = reloj;
            reloj = 0;
            Recentrar(pj);
            float baja = paso / Mathf.Max(1, recuperacion);
            for (int i = 0; i < apl.Length; i++) if (apl[i] > 0) apl[i] = Mathf.Max(0, apl[i] - baja);

            float lim = TEX * CEL / 2 - 2;
            foreach (var s in Pisadas.Activos())
            {
                if (!s.t.gameObject.activeInHierarchy) continue;
                var c = s.t.position;
                if (Mathf.Abs(c.x - pj.x) > lim || Mathf.Abs(c.z - pj.z) > lim) continue;
                var marcha = Vector2.zero;
                if (antes.TryGetValue(s.t, out var a))
                {
                    var m = new Vector2(c.x - a.x, c.z - a.z);
                    if (m.sqrMagnitude > 1e-4f) marcha = m.normalized;
                }
                antes[s.t] = c;
                Pisar(c, s.radio, marcha);
            }
            SubirPisadas();
        }

        void SubirPisadas()
        {
            for (int i = 0; i < apl.Length; i++)
                px32[i] = new Color32((byte)(apl[i] * 255), (byte)((dirX[i] * 0.5f + 0.5f) * 255), (byte)((dirZ[i] * 0.5f + 0.5f) * 255), 255);
            texPisada.SetPixels32(px32);
            texPisada.Apply(false);
            Shader.SetGlobalTexture(idPisadaTex, texPisada);
            Shader.SetGlobalVector(idPisadaOrigen, new Vector4(origen.x, origen.y, 1f / (TEX * CEL), 0));
            Shader.SetGlobalFloat(idPisadaFuerza, fuerzaPisada);
        }

        void Update()
        {
            if (jugador == null || material == null || malla == null) return;
            var p = jugador.position;
            if (apl != null) ActualizarPisadas(p, Time.deltaTime);
            float px = p.x, pz = -p.z;
            if (Vector2.Distance(new Vector2(px, pz), ultimo) > 14)
            {
                while (colores.Count < capacidad / LOTE + 1) colores.Add(new Vector4[LOTE]);
                Regenerar(px, pz);
            }
            var rp = new RenderParams(material)
            {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                worldBounds = new Bounds(p, new Vector3(200, 60, 200)),
            };
            for (int i = 0; i < lotesN.Count; i++)
            {
                if (lotesN[i] == 0) continue;
                rp.matProps = lotesC[i];
                Graphics.RenderMeshInstanced(rp, malla, 0, lotesM[i], lotesN[i]);
            }
        }
    }
}
