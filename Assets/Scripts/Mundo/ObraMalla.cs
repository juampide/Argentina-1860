using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argentina1860
{
    /// <summary>
    /// Arma mallas low poly directamente en coordenadas locales de Unity (sin pasar por el espacio del prototipo),
    /// con triángulos sueltos para que el sombreado quede facetado y una submalla por material de la Paleta.
    /// La usan las piezas grandes que se apoyan en el terreno: sembrados y huellas del camino.
    /// Las coordenadas de textura salen de la posición vista desde arriba (x, z), así una textura de suelo
    /// (tierra, barro) se repite pareja sin costuras de un tramo a otro.
    /// </summary>
    public class ObraMalla
    {
        readonly List<string> orden = new List<string>();
        readonly Dictionary<string, List<Vector3>> pos = new Dictionary<string, List<Vector3>>();

        public int Triangulos { get; private set; }

        List<Vector3> Lista(string mat)
        {
            if (!pos.TryGetValue(mat, out var l))
            {
                l = new List<Vector3>();
                pos[mat] = l;
                orden.Add(mat);
            }
            return l;
        }

        /// Triángulo en sentido horario visto desde arriba (la cara que mira hacia +Y en Unity).
        public void Tri(string mat, Vector3 a, Vector3 b, Vector3 c)
        {
            var l = Lista(mat);
            l.Add(a); l.Add(b); l.Add(c);
            Triangulos++;
        }

        public void Quad(string mat, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(mat, a, b, c);
            Tri(mat, a, c, d);
        }

        /// Escala de la textura: metros por repetición.
        public float metrosTextura = 4;
        public Vector3 origenTextura;

        Vector2 Uv(Vector3 p) => new Vector2(p.x + origenTextura.x, p.z + origenTextura.z) / metrosTextura;

        /// Crea un objeto con la malla. Devuelve null si no hay nada.
        public GameObject Instanciar(string nombre, Transform padre, System.Func<Mesh, Mesh> registrar, bool sombras = true)
        {
            if (orden.Count == 0) return null;
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var uv = new List<Vector2>();
            var sub = new List<List<int>>();
            foreach (var k in orden)
            {
                var l = pos[k];
                var tris = new List<int>(l.Count);
                for (int i = 0; i + 2 < l.Count; i += 3)
                {
                    var n = Vector3.Cross(l[i + 1] - l[i], l[i + 2] - l[i]);
                    if (n.sqrMagnitude < 1e-14f) continue;
                    n.Normalize();
                    int b = vs.Count;
                    vs.Add(l[i]); vs.Add(l[i + 1]); vs.Add(l[i + 2]);
                    uv.Add(Uv(l[i])); uv.Add(Uv(l[i + 1])); uv.Add(Uv(l[i + 2]));
                    ns.Add(n); ns.Add(n); ns.Add(n);
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                }
                sub.Add(tris);
            }
            var m = new Mesh { name = nombre, indexFormat = vs.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(vs);
            m.SetNormals(ns);
            m.SetUVs(0, uv);
            m.subMeshCount = sub.Count;
            for (int i = 0; i < sub.Count; i++) m.SetTriangles(sub[i], i, false);
            m.RecalculateBounds();
            if (registrar != null) m = registrar(m);

            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = orden.Select(Paleta.Mat).ToArray();
            mr.shadowCastingMode = sombras ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;
            return go;
        }
    }
}
