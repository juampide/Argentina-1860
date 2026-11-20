using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Mangrullo: la torre de vigía del fortín, hecha con cuatro troncos que se cierran hacia arriba, crucetas
    /// atadas con tiento, una plataforma con baranda y una escalera de palos. Desde ahí el centinela miraba
    /// la pampa para avisar si venía un malón. Se cambian los valores y se rearma solo.
    /// </summary>
    public class Mangrullo : ProceduralBase
    {
        [Min(3)] public float alto = 7.5f;
        [Min(1)] public float baseAncha = 3.4f;
        [Min(1)] public float plataforma = 2.2f;

        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
        const string PALO = "#8a7558", TIENTO = "#5a4632";

        protected override void Construir(Transform raiz)
        {
            var n = new Nodo("mangrullo");
            float b = baseAncha / 2, t = plataforma / 2, h = alto;
            var pies = new[] { v(-b, 0, -b), v(b, 0, -b), v(b, 0, b), v(-b, 0, b) };
            var topes = new[] { v(-t, h + 1.1f, -t), v(t, h + 1.1f, -t), v(t, h + 1.1f, t), v(-t, h + 1.1f, t) };
            for (int i = 0; i < 4; i++) Palo(n, pies[i], topes[i], 0.13f, PALO);

            // crucetas en X en cada cara, en dos tramos
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                foreach (var (c0, c1) in new[] { (0.05f, 0.45f), (0.45f, 0.85f) })
                {
                    var a0 = Vector3.Lerp(pies[i], topes[i], c0); var a1 = Vector3.Lerp(pies[i], topes[i], c1);
                    var b0 = Vector3.Lerp(pies[j], topes[j], c0); var b1 = Vector3.Lerp(pies[j], topes[j], c1);
                    Palo(n, a0, b1, 0.05f, PALO);
                    Palo(n, b0, a1, 0.05f, PALO);
                    Palo(n, a1, b1, 0.06f, PALO);   // travesaño
                }
            }

            // plataforma de tablas y baranda
            var pl = Vector3.Lerp(pies[0], topes[0], h / (h + 1.1f));
            float lado = Mathf.Abs(pl.x) * 2 + 0.5f;
            for (int k = 0; k < 7; k++)
                n.Pieza(Geo.Caja(lado, 0.06f, lado / 7 - 0.03f), "madera", v(0, h, -lado / 2 + (k + 0.5f) * lado / 7), v(0, 0, (k % 2 - 0.5f) * 0.03f));
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                var pa = Vector3.Lerp(pies[i], topes[i], (h + 1.0f) / (h + 1.1f));
                var pb = Vector3.Lerp(pies[j], topes[j], (h + 1.0f) / (h + 1.1f));
                Palo(n, pa, pb, 0.05f, PALO);
                Palo(n, (pa + v(0, -0.5f, 0)), (pb + v(0, -0.5f, 0)), 0.04f, PALO);
            }

            // escalera de palos, apoyada en una cara
            var e0 = v(-0.35f, 0, b + 1.1f); var e1 = v(-0.35f, h + 0.3f, t + 0.1f);
            var f0 = v(0.35f, 0, b + 1.1f); var f1 = v(0.35f, h + 0.3f, t + 0.1f);
            Palo(n, e0, e1, 0.05f, PALO);
            Palo(n, f0, f1, 0.05f, PALO);
            int pelda = Mathf.RoundToInt(h / 0.42f);
            for (int k = 1; k < pelda; k++)
            {
                float f = (float)k / pelda;
                Palo(n, Vector3.Lerp(e0, e1, f), Vector3.Lerp(f0, f1, f), 0.03f, TIENTO);
            }

            n.Instanciar(raiz, Registrar);
            Colision(raiz, v(0, h / 2, 0), v(baseAncha, h, baseAncha));
        }

        /// Un palo (cilindro) entre dos puntos del espacio del prototipo.
        static void Palo(Nodo n, Vector3 a, Vector3 b, float r, string mat)
        {
            var d = b - a;
            float L = d.magnitude;
            if (L < 0.01f) return;
            n.PiezaM(Geo.Cilindro(r * 0.85f, r, L, 5), mat, Matrix4x4.TRS((a + b) / 2, Quaternion.FromToRotation(Vector3.up, d / L), Vector3.one));
        }
    }
}
