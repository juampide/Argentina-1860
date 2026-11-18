using UnityEngine;

namespace Argentina1860
{
    /// Palenque para atar caballos: postes y una vara horizontal (palenque() de campo.js).
    public class Palenque : ProceduralBase
    {
        [Range(2, 10)] public int postes = 4;

        protected override void Construir(Transform raiz)
        {
            var n = new Nodo("palenque");
            for (int i = 0; i < postes; i++)
                n.Pieza(Geo.Cilindro(0.1f, 0.12f, 1.6f, 6), "madera", new Vector3(-(postes - 1) * 0.9f + i * 1.8f, 0.8f, 0));
            n.Pieza(Geo.Caja((postes - 1) * 1.8f + 0.4f, 0.12f, 0.14f, 1.5f), "madera", new Vector3(0, 1.35f, 0));
            n.Instanciar(raiz, Registrar);
            Colision(raiz, new Vector3(0, 0.8f, 0), new Vector3((postes - 1) * 1.8f + 0.4f, 1.6f, 0.3f));
        }
    }
}
