using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Cerco de postes y dos rieles que sigue el terreno (cerco() de campo.js). Los puntos están en metros,
    /// relativos al objeto (x, z de Unity). El lado "hueco" deja una abertura de 4 m en el medio (la puerta).
    /// El objeto no debe rotarse: para mover el cerco se mueve el objeto o los puntos.
    /// </summary>
    public class Cerco : ProceduralBase
    {
        public List<Vector2> puntos = new List<Vector2> { new Vector2(-5, -5), new Vector2(5, -5), new Vector2(5, 5), new Vector2(-5, 5) };
        public bool cerrado = true;
        [Tooltip("Índice del lado con abertura de 4 m (-1 = ninguno). El lado i va del punto i al i+1.")]
        public int hueco = -1;
        [Min(0.5f)] public float alto = 1.4f;
        [Tooltip("Distancia aproximada entre postes")]
        [Min(0.5f)] public float paso = 3;

        protected override void Construir(Transform raiz)
        {
            if (puntos == null || puntos.Count < 2) return;
            var n = new Nodo("cerco");
            var poste = Geo.Cilindro(0.09f, 0.11f, 1, 5);
            var riel = Geo.Caja(0.07f, 0.08f, 1);
            float y0 = transform.position.y;
            // en el espacio del prototipo z va al revés
            Vector2 P3(Vector2 u) => new Vector2(u.x, -u.y);
            float Alt(float x3, float z3) => Terreno.Altura(transform.TransformPoint(new Vector3(x3, 0, -z3))) - y0;

            int lados = cerrado ? puntos.Count : puntos.Count - 1;
            for (int i = 0; i < lados; i++)
            {
                Vector2 a = P3(puntos[i]), b = P3(puntos[(i + 1) % puntos.Count]);
                float L = Vector2.Distance(a, b), ang = Mathf.Atan2(b.x - a.x, b.y - a.y);
                if (L < 0.01f) continue;
                int np = Mathf.Max(1, Conv.RoundJS(L / paso));
                for (int k = 0; k <= np; k++)
                {
                    var p = Vector2.Lerp(a, b, (float)k / np);
                    var m = Matrix4x4.TRS(new Vector3(p.x, Alt(p.x, p.y), p.y), Quaternion.identity, new Vector3(1, alto, 1)) * Matrix4x4.Translate(new Vector3(0, 0.5f, 0));
                    n.PiezaM(poste, "cerco", m);
                }
                var tramos = i == hueco && L > 4.5f
                    ? new[] { (0f, (1 - 4 / L) / 2), ((1 + 4 / L) / 2, 1f) }
                    : new[] { (0f, 1f) };
                foreach (var (ta, tb) in tramos)
                {
                    Vector2 pa = Vector2.Lerp(a, b, ta), pb = Vector2.Lerp(a, b, tb), mid = (pa + pb) / 2;
                    float Lt = L * (tb - ta), ym = Alt(mid.x, mid.y);
                    foreach (var f in new[] { 0.45f, 0.85f })
                        n.PiezaM(riel, "cerco", Matrix4x4.TRS(new Vector3(mid.x, ym + alto * f, mid.y), Conv.Q3(new Vector3(0, ang, 0)), new Vector3(1, 1, Lt)));
                    Colision(raiz, Conv.P(mid.x, ym + alto / 2, mid.y), new Vector3(0.5f, alto, Lt), Conv.Yaw(ang));
                }
            }
            n.Instanciar(raiz, Registrar);
        }
    }
}
