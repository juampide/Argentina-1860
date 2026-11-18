using UnityEngine;

namespace Argentina1860
{
    /// Pozo de agua con brocal de piedra y travesaño (pozo() de campo.js).
    public class Pozo : ProceduralBase
    {
        protected override void Construir(Transform raiz)
        {
            var n = new Nodo("pozo");
            n.Pieza(Geo.Cilindro(0.7f, 0.8f, 0.7f, 8), "piedra", new Vector3(0, 0.35f, 0));
            foreach (var sx in new[] { -1f, 1f })
                n.Pieza(Geo.Cilindro(0.07f, 0.07f, 1.7f, 5), "madera", new Vector3(sx * 0.75f, 1.0f, 0));
            n.Pieza(Geo.Cilindro(0.05f, 0.05f, 1.6f, 5), "madera", new Vector3(0, 1.8f, 0), new Vector3(0, 0, Mathf.PI / 2));
            n.Instanciar(raiz, Registrar);
            Colision(raiz, new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1, 1.6f));
        }
    }
}
