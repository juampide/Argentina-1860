using UnityEngine;

namespace Argentina1860
{
    /// Mástil con bandera de franjas (el del fortín: celeste, blanco, celeste).
    public class Mastil : ProceduralBase
    {
        [Min(2)] public float alto = 9;
        public Color[] franjas = { Conv.Hex("#79b7e6"), Conv.Hex("#ffffff"), Conv.Hex("#79b7e6") };

        protected override void Construir(Transform raiz)
        {
            var n = new Nodo("mastil");
            n.Pieza(Geo.Cilindro(0.12f, 0.2f, alto, 6), "madera", new Vector3(0, alto / 2, 0));
            for (int i = 0; i < franjas.Length; i++)
                n.Pieza(Geo.Caja(2.4f, 0.5f, 0.05f), Conv.Clave(franjas[i]), new Vector3(1.3f, alto - 0.5f - i * 0.5f, 0));
            n.Instanciar(raiz, Registrar);
            Colision(raiz, new Vector3(0, alto / 2, 0), new Vector3(0.5f, alto, 0.5f));
        }
    }
}
