using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argentina1860
{
    /// <summary>
    /// Junta piezas de Geo (en el espacio del prototipo) en una sola malla de Unity, con una submalla por material.
    /// Al construir convierte z → -z, invierte el orden de los triángulos y calcula normales planas.
    /// </summary>
    public class MallaTres
    {
        readonly List<string> orden = new List<string>();
        readonly Dictionary<string, List<Vector3>> pos = new Dictionary<string, List<Vector3>>();
        readonly Dictionary<string, List<Vector2>> uvs = new Dictionary<string, List<Vector2>>();
        readonly Dictionary<string, List<bool>> suaves = new Dictionary<string, List<bool>>();
        const float ANGULO_SUAVE = 50f;   // aristas más cerradas que esto quedan duras

        public bool Vacia => orden.Count == 0;

        public void Agregar(Geo g, string mat, Matrix4x4 m)
        {
            if (!pos.TryGetValue(mat, out var lp))
            {
                lp = new List<Vector3>(); pos[mat] = lp; uvs[mat] = new List<Vector2>(); suaves[mat] = new List<bool>(); orden.Add(mat);
            }
            var lu = uvs[mat];
            for (int i = 0; i < g.v.Count; i++)
            {
                lp.Add(m.MultiplyPoint3x4(g.v[i]));
                lu.Add(g.uv[i]);
                suaves[mat].Add(g.suave);
            }
        }

        /// Promedia las normales de los vértices que coinciden en posición, solo entre caras suaves y con ángulo menor al límite.
        static void SuavizarNormales(List<Vector3> vs, List<Vector3> ns, int v0, List<bool> esSuave, List<float> area)
        {
            bool alguna = false;
            foreach (var b in esSuave) if (b) { alguna = true; break; }
            if (!alguna) return;
            float cosLim = Mathf.Cos(ANGULO_SUAVE * Mathf.Deg2Rad);
            int n = esSuave.Count;
            var planas = new Vector3[n];
            for (int i = 0; i < n; i++) planas[i] = ns[v0 + i];
            var grupos = new Dictionary<Vector3Int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                if (!esSuave[i]) continue;
                var p = vs[v0 + i];
                var key = new Vector3Int(Mathf.RoundToInt(p.x * 1000f), Mathf.RoundToInt(p.y * 1000f), Mathf.RoundToInt(p.z * 1000f));
                if (!grupos.TryGetValue(key, out var l)) { l = new List<int>(); grupos[key] = l; }
                l.Add(i);
            }
            foreach (var l in grupos.Values)
                foreach (int i in l)
                {
                    Vector3 suma = Vector3.zero;
                    foreach (int j in l) if (Vector3.Dot(planas[i], planas[j]) > cosLim) suma += planas[j] * area[j];
                    if (suma.sqrMagnitude > 1e-12f) ns[v0 + i] = suma.normalized;
                }
        }

        public Mesh Construir(string nombre, out string[] materiales)
        {
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var us = new List<Vector2>();
            var sub = new List<List<int>>();
            foreach (var k in orden)
            {
                var lp = pos[k];
                var lu = uvs[k];
                var tris = new List<int>();
                var ls = suaves[k];
                int v0 = vs.Count;
                var esSuave = new List<bool>();
                var area = new List<float>();
                for (int i = 0; i + 2 < lp.Count; i += 3)
                {
                    // a Unity: z cambia de signo y el triángulo se recorre al revés
                    Vector3 a = Conv.P(lp[i]), b = Conv.P(lp[i + 2]), c = Conv.P(lp[i + 1]);
                    var n = Vector3.Cross(b - a, c - a);
                    if (n.sqrMagnitude < 1e-14f) continue;
                    float ar = n.magnitude;
                    n.Normalize();
                    int b0 = vs.Count;
                    for (int q = 0; q < 3; q++) { esSuave.Add(ls[i]); area.Add(ar); }
                    vs.Add(a); vs.Add(b); vs.Add(c);
                    ns.Add(n); ns.Add(n); ns.Add(n);
                    us.Add(lu[i]); us.Add(lu[i + 2]); us.Add(lu[i + 1]);
                    tris.Add(b0); tris.Add(b0 + 1); tris.Add(b0 + 2);
                }
                SuavizarNormales(vs, ns, v0, esSuave, area);
                sub.Add(tris);
            }
            var mesh = new Mesh { name = nombre };
            if (vs.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vs);
            mesh.SetNormals(ns);
            mesh.SetUVs(0, us);
            mesh.subMeshCount = sub.Count;
            for (int i = 0; i < sub.Count; i++) mesh.SetTriangles(sub[i], i, false);
            mesh.RecalculateBounds();
            materiales = orden.ToArray();
            return mesh;
        }
    }

    /// <summary>
    /// Un nodo del prototipo (THREE.Group o Mesh): posición, rotación y escala en el espacio del prototipo,
    /// con piezas de geometría y nodos hijos. Instanciar() lo convierte en GameObjects de Unity.
    /// </summary>
    public class Nodo
    {
        public string nombre;
        public Vector3 pos, rot;
        public Vector3 esc = Vector3.one;
        public readonly List<(Geo g, string mat, Matrix4x4 m)> piezas = new List<(Geo, string, Matrix4x4)>();
        public readonly List<Nodo> hijos = new List<Nodo>();

        public Nodo(string nombre, Vector3 pos = default)
        {
            this.nombre = nombre; this.pos = pos;
        }

        public Nodo Hijo(string n, Vector3 p = default)
        {
            var h = new Nodo(n, p);
            hijos.Add(h);
            return h;
        }

        /// Agrega una pieza con posición, rotación (Euler del prototipo) y escala.
        public void Pieza(Geo g, string mat, Vector3 p, Vector3 r = default, Vector3? e = null)
        {
            piezas.Add((g, mat, Matrix4x4.TRS(p, Conv.Q3(r), e ?? Vector3.one)));
        }

        public void PiezaM(Geo g, string mat, Matrix4x4 m) => piezas.Add((g, mat, m));

        public Transform Instanciar(Transform padre, Func<Mesh, Mesh> registrar, Dictionary<Nodo, Transform> mapa = null)
        {
            var go = new GameObject(nombre);
            var t = go.transform;
            t.SetParent(padre, false);
            t.localPosition = Conv.P(pos);
            t.localRotation = Conv.Rot(rot);
            t.localScale = esc;
            if (piezas.Count > 0)
            {
                var mt = new MallaTres();
                foreach (var p in piezas) mt.Agregar(p.g, p.mat, p.m);
                var mesh = mt.Construir(nombre, out var mats);
                if (registrar != null) mesh = registrar(mesh);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = mats.Select(Paleta.Mat).ToArray();
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            }
            mapa?.Add(this, t);
            foreach (var h in hijos) h.Instanciar(t, registrar, mapa);
            return t;
        }
    }
}
