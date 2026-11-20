using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argentina1860
{
    /// <summary>
    /// La superficie del agua: una malla que cubre todo lo que está pintado como agua en el terreno (capa "Agua":
    /// arroyo, laguna, cañadón, charcos del bañado, el cauce bajo el puente) y se dibuja con el shader Argentina/Agua,
    /// con ondas que corren con el viento, destellos de sol, reflejo del cielo y gotas cuando llueve.
    /// En la orilla el agua se funde con el barro (alfa según cuánto agua hay pintada).
    /// Se rearma sola; si se repinta el agua del terreno, clic derecho en el componente → Regenerar.
    /// El objeto debe quedar en el origen.
    /// </summary>
    public class AguaViva : ProceduralBase
    {
        [Tooltip("Lado de cada cuadro de la malla del agua, en metros")]
        [Min(0.5f)] public float celda = 2f;
        [Tooltip("Cuánto queda el agua por encima del suelo pintado")]
        public float sobreSuelo = 0.08f;
        [Tooltip("Desde cuánta agua pintada (0 a 1) se dibuja la superficie")]
        [Range(0.01f, 0.9f)] public float umbral = 0.12f;

        const float TROZO = 120;   // lado de cada pedazo de malla (para que la cámara descarte lo que no ve)

        protected override void Construir(Transform raiz)
        {
            var t = Terrain.activeTerrain;
            if (t == null) return;
            var mat = MaterialesMundo.Instancia != null ? MaterialesMundo.Instancia.Buscar("agua_viva") : null;
            if (mat == null) { Debug.LogWarning("AguaViva: falta el material 'agua_viva' en MaterialesMundo."); return; }

            var td = t.terrainData; var pos = t.transform.position; var size = td.size;
            int ar = td.alphamapResolution;
            var al = td.GetAlphamaps(0, 0, ar, ar);
            float Agua(float x, float z)   // x, z de Unity
            {
                float u = (x - pos.x) / size.x * ar - 0.5f, w = (z - pos.z) / size.z * ar - 0.5f;
                int i0 = Mathf.Clamp(Mathf.FloorToInt(w), 0, ar - 1), j0 = Mathf.Clamp(Mathf.FloorToInt(u), 0, ar - 1);
                int i1 = Mathf.Min(i0 + 1, ar - 1), j1 = Mathf.Min(j0 + 1, ar - 1);
                float fu = Mathf.Clamp01(u - j0), fw = Mathf.Clamp01(w - i0);
                return Mathf.Lerp(Mathf.Lerp(al[i0, j0, 3], al[i0, j1, 3], fu), Mathf.Lerp(al[i1, j0, 3], al[i1, j1, 3], fu), fw);
            }

            int nx = Mathf.CeilToInt(size.x / celda), nz = Mathf.CeilToInt(size.z / celda);
            var trozos = new Dictionary<(int, int), (List<Vector3> v, List<Color> c, List<int> tri, Dictionary<long, int> idx)>();
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x0 = pos.x + i * celda, z0 = pos.z + j * celda;
                    float a00 = Agua(x0, z0), a10 = Agua(x0 + celda, z0), a01 = Agua(x0, z0 + celda), a11 = Agua(x0 + celda, z0 + celda);
                    if (Mathf.Max(Mathf.Max(a00, a10), Mathf.Max(a01, a11)) < umbral) continue;
                    var k = (Mathf.FloorToInt((x0 - pos.x) / TROZO), Mathf.FloorToInt((z0 - pos.z) / TROZO));
                    if (!trozos.TryGetValue(k, out var tz)) trozos[k] = tz = (new List<Vector3>(), new List<Color>(), new List<int>(), new Dictionary<long, int>());
                    int V(int ii, int jj, float a)
                    {
                        long clave = ((long)ii << 32) | (uint)jj;
                        if (tz.idx.TryGetValue(clave, out var n)) return n;
                        float x = pos.x + ii * celda, z = pos.z + jj * celda;
                        // la superficie toma lo más alto del fondo alrededor: así ningún lomito del fondo la atraviesa
                        float fondo = float.MinValue;
                        for (int dx = -1; dx <= 1; dx++)
                            for (int dz = -1; dz <= 1; dz++)
                                fondo = Mathf.Max(fondo, t.SampleHeight(new Vector3(x + dx * celda * 0.75f, 0, z + dz * celda * 0.75f)));
                        var p = new Vector3(x, fondo + pos.y + sobreSuelo, z) - transform.position;
                        n = tz.v.Count;
                        tz.v.Add(p);
                        tz.c.Add(new Color(1, 1, 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(umbral, 0.65f, a))));
                        tz.idx[clave] = n;
                        return n;
                    }
                    int v00 = V(i, j, a00), v10 = V(i + 1, j, a10), v01 = V(i, j + 1, a01), v11 = V(i + 1, j + 1, a11);
                    tz.tri.Add(v00); tz.tri.Add(v01); tz.tri.Add(v11);
                    tz.tri.Add(v00); tz.tri.Add(v11); tz.tri.Add(v10);
                }

            foreach (var kv in trozos)
            {
                var (vs, cs, tris, _) = kv.Value;
                var m = new Mesh { name = "agua " + kv.Key, indexFormat = vs.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                m.SetVertices(vs); m.SetColors(cs); m.SetTriangles(tris, 0);
                var normales = new Vector3[vs.Count];
                for (int n = 0; n < normales.Length; n++) normales[n] = Vector3.up;
                m.normals = normales;
                m.RecalculateBounds();
                Registrar(m);
                var go = new GameObject("agua " + kv.Key);
                go.transform.SetParent(raiz, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
        }
    }
}
