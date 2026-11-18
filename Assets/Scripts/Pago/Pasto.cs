using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argentina1860
{
    /// <summary>
    /// Pasto de la pampa de 1867 alrededor del jugador (campo.js): matas altas y duras que se mecen con el viento
    /// y cubren todo ("mar de pasto"); en el pajonal llegan a dos metros. Cada mata es un manojo de once hojas.
    /// Se regenera al caminar y se dibuja con instancias por GPU (shader Argentina/Pasto).
    /// </summary>
    public class Pasto : MonoBehaviour
    {
        public Transform jugador;
        public Material material;
        [Tooltip("Máximo de matas dibujadas")]
        public int capacidad = 26000;

        const int LOTE = 1023;
        Mesh malla;
        readonly List<Matrix4x4[]> lotesM = new List<Matrix4x4[]>();
        readonly List<MaterialPropertyBlock> lotesC = new List<MaterialPropertyBlock>();
        readonly List<int> lotesN = new List<int>();
        Vector2 ultimo = new Vector2(1e9f, 1e9f);
        static readonly int idJugador = Shader.PropertyToID("_PastoJugador"), idColor = Shader.PropertyToID("_ColorInst");

        void Start()
        {
            malla = CrearMalla();
            if (material == null)
            {
                var sh = Shader.Find("Argentina/Pasto");
                if (sh != null) material = new Material(sh) { enableInstancing = true, name = "Pasto" };
            }
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
                    float dens = (pajonal ? 0.75f : 0.3f) * parche * (d > R * 0.7f ? 1 - (d - R * 0.7f) / (R * 0.3f) : 1);
                    int k = Conv.RoundJS(dens * CELDA * CELDA);
                    for (int i = 0; i < k && n < capacidad; i++)
                    {
                        double seed = gx * 7919.0 + gz * 104729.0 + i * 13.0;
                        float x = (gx + MapaPago.Hs(seed)) * CELDA, z = (gz + MapaPago.Hs(seed + 1)) * CELDA;
                        if (!MapaPago.PuedeCrecer(x, z) || !MapaPago.LibreDeSitios(x, z, 0.8f)) continue;
                        float alto = (pajonal ? 1.45f : 0.8f) * MapaPago.Mix(0.75f, 1.45f, MapaPago.Hs(seed + 2)), ancho = MapaPago.Mix(0.85f, 1.45f, MapaPago.Hs(seed + 7));
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
                        colores[lote][j] = ColorHSL(MapaPago.Mix(0.11f, 0.24f, MapaPago.Hs(seed + 4) * (pajonal ? 0.7f : 1)), MapaPago.Mix(0.3f, 0.6f, MapaPago.Hs(seed + 5)), MapaPago.Mix(0.34f, 0.54f, MapaPago.Hs(seed + 6)));
                        lotesN[lote] = j + 1;
                        n++;
                    }
                }
            for (int i = 0; i < lotesN.Count; i++) lotesC[i].SetVectorArray(idColor, colores[i]);
            ultimo = new Vector2(px, pz);
        }

        readonly List<Vector4[]> colores = new List<Vector4[]>();

        void Update()
        {
            if (jugador == null || material == null || malla == null) return;
            var p = jugador.position;
            Shader.SetGlobalVector(idJugador, p);
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
