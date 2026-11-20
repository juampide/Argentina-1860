using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argentina1860
{
    /// <summary>
    /// Los árboles de la ciudad según el plano (construirArboles() de ciudad.js): copa facetada más clara arriba y tronco,
    /// dibujados con instancias por GPU (shader Argentina/Instanciado). Solo se dibujan los que están cerca del jugador.
    /// </summary>
    public class ArbolesCiudad : MonoBehaviour
    {
        public Ciudad ciudad;
        public Material material;
        public Transform jugador;
        [Tooltip("Distancia hasta la que se dibujan los árboles (metros)")]
        public float alcance = 600;

        const int LOTE = 1023;
        Mesh copa, tronco;
        Vector3[] pos;
        Matrix4x4[] mCopa, mTronco;
        Vector4[] colores;
        readonly Matrix4x4[] lc = new Matrix4x4[LOTE], lt = new Matrix4x4[LOTE];
        readonly Vector4[] lcol = new Vector4[LOTE], blanco = new Vector4[LOTE];
        MaterialPropertyBlock bCopa, bTronco;
        static readonly int idColor = Shader.PropertyToID("_ColorInst");

        static float Hash(double i) { double x = Math.Sin(i * 127.1 + 311.7) * 43758.5453; return (float)(x - Math.Floor(x)); }

        void Start()
        {
            if (material == null) { var sh = Shader.Find("Argentina/Instanciado"); if (sh != null) material = new Material(sh) { enableInstancing = true }; }
            if (ciudad == null) ciudad = Ciudad.Instancia;
            if (ciudad == null || ciudad.Datos == null) return;
            CrearMallas();
            var d = ciudad.Datos;
            int n = d.CantArboles;
            pos = new Vector3[n]; mCopa = new Matrix4x4[n]; mTronco = new Matrix4x4[n]; colores = new Vector4[n];
            for (int i = 0; i < n; i++)
            {
                var p = d.AMundo(d.arboles[i * 3], d.arboles[i * 3 + 1]);
                float r = Mathf.Clamp(d.arboles[i * 3 + 2] * 0.4f, 1.3f, 2.6f) * (0.85f + Hash(i) * 0.35f);
                var rot = Conv.Yaw(Hash(i + 7) * 6.28f);
                pos[i] = new Vector3(p.x, 0, -p.y);
                mCopa[i] = Matrix4x4.TRS(new Vector3(p.x, r * 0.9f + 1.7f, -p.y), rot, Vector3.one * r);
                mTronco[i] = Matrix4x4.TRS(pos[i], rot, new Vector3(1, r * 0.9f + 1.9f, 1));
                var c = Conv.HSL(0.27f + Hash(i + 3) * 0.07f, 0.45f + Hash(i + 5) * 0.15f, 0.42f + Hash(i + 9) * 0.1f).linear;
                colores[i] = new Vector4(c.r, c.g, c.b, 1);
            }
            for (int i = 0; i < LOTE; i++) blanco[i] = Vector4.one;
            bCopa = new MaterialPropertyBlock();
            bTronco = new MaterialPropertyBlock();
            bTronco.SetVectorArray(idColor, blanco);
        }

        void CrearMallas()
        {
            // copa: icosaedro deformado con pocas caras, más claro arriba
            var mt = new MallaTres();
            mt.Agregar(Geo.Ico(1, 1), "copa", Matrix4x4.identity);
            copa = mt.Construir("copa", out _);
            var vs = copa.vertices;
            var cs = new Color[vs.Length];
            for (int i = 0; i < vs.Length; i++)
            {
                var v = vs[i];
                float r = 1 + 0.16f * Mathf.Sin(v.x * 5.1f - v.z * 3.3f) * Mathf.Cos(v.y * 4.7f);
                vs[i] = new Vector3(v.x * r, (v.y * 0.88f + 0.12f) * r, v.z * r);
                float luz = 0.7f + 0.3f * ((v.y + 1) / 2);
                cs[i] = new Color(luz, luz, luz);
            }
            copa.vertices = vs;
            copa.colors = cs;
            copa.RecalculateNormals();
            copa.RecalculateBounds();
            var mt2 = new MallaTres();
            mt2.Agregar(Geo.Cilindro(0.14f, 0.24f, 1, 5).Trasladar(0, 0.5f, 0), "tronco", Matrix4x4.identity);
            tronco = mt2.Construir("tronco", out _);
            var col = new Color[tronco.vertexCount];
            var ct = Conv.Hex("#7a5535").linear;
            for (int i = 0; i < col.Length; i++) col[i] = ct;
            tronco.colors = col;
        }

        void Update()
        {
            if (pos == null || material == null || bCopa == null) return;   // bCopa se pierde si Unity recompila en Play
            var centro = jugador != null ? jugador.position : Vector3.zero;
            float a2 = alcance * alcance;
            var rp = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, worldBounds = new Bounds(centro, Vector3.one * alcance * 2.2f) };
            int k = 0;
            for (int i = 0; i < pos.Length; i++)
            {
                float dx = pos[i].x - centro.x, dz = pos[i].z - centro.z;
                if (dx * dx + dz * dz > a2) continue;
                lc[k] = mCopa[i]; lt[k] = mTronco[i]; lcol[k] = colores[i];
                if (++k == LOTE) { Dibujar(rp, k); k = 0; }
            }
            if (k > 0) Dibujar(rp, k);
        }

        void Dibujar(RenderParams rp, int n)
        {
            bCopa.SetVectorArray(idColor, lcol);
            rp.matProps = bCopa;
            Graphics.RenderMeshInstanced(rp, copa, 0, lc, n);
            rp.matProps = bTronco;
            Graphics.RenderMeshInstanced(rp, tronco, 0, lt, n);
        }
    }
}
