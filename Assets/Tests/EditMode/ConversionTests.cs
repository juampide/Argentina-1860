using NUnit.Framework;
using UnityEngine;

namespace Argentina1860.Tests
{
    /// Coordenadas, colores y geometría: si esto se rompe, todo el mundo queda espejado, de otro color o con caras al revés.
    public class ConversionTests
    {
        [Test]
        public void P_DaVueltaLaZ()
        {
            Assert.AreEqual(new Vector3(1, 2, -3), Conv.P(1, 2, 3));
            Assert.AreEqual(new Vector3(-4, 0, 5), Conv.P(new Vector3(-4, 0, -5)));
        }

        [TestCase("#ff8000")]
        [TestCase("#3a2a22")]
        [TestCase("#5c6168")]
        [TestCase("#000000")]
        public void Hex_YClave_VanYVuelven(string hex)
        {
            Assert.AreEqual(hex, Conv.Clave(Conv.Hex(hex)));
        }

        [Test]
        public void Hex_Corto_YMalFormado()
        {
            Assert.AreEqual(Conv.Hex("#ffaa00"), Conv.Hex("#fa0"));
            Assert.AreEqual(Color.magenta, Conv.Hex("cualquiera"));
            Assert.AreEqual(Color.magenta, Conv.Hex(null));
        }

        [Test]
        public void RoundJS_RedondeaComoJavaScript()
        {
            // Math.round de JS redondea .5 hacia arriba, también en negativos (C# redondea al par)
            Assert.AreEqual(3, Conv.RoundJS(2.5f));
            Assert.AreEqual(-2, Conv.RoundJS(-2.5f));
            Assert.AreEqual(1, Conv.RoundJS(0.5f));
        }

        [Test]
        public void K_CentroDelMapaEsElOrigen()
        {
            Assert.Less(Vector2.Distance(Vector2.zero, MapaPago.K(1.6f, 1.2f)), 0.01f);
            Assert.Less(Vector2.Distance(new Vector2(1000, -1000), MapaPago.K(2.6f, 0.2f)), 0.01f, "un km al este y uno al norte (z hacia el sur)");
        }

        /// Cada triángulo de la malla convertida tiene que mirar hacia afuera (orden de vértices de Unity).
        /// Si la conversión de three.js invierte mal el orden, las caras se ven desde adentro o desaparecen.
        [Test]
        public void MallaTres_CarasHaciaAfuera()
        {
            var mt = new MallaTres();
            mt.Agregar(Geo.Caja(2, 3, 4), "#ffffff", Matrix4x4.identity);
            mt.Agregar(Geo.Ico(1, 1), "#ffffff", Matrix4x4.Translate(new Vector3(10, 0, 0)));
            var m = mt.Construir("prueba", out _);
            try
            {
                var v = m.vertices; var t = m.triangles; var n = m.normals;
                Assert.Greater(t.Length, 0);
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                    var centro = (a + b + c) / 3;
                    var origen = centro.x > 5 ? new Vector3(10, 0, 0) : Vector3.zero;
                    var cara = Vector3.Cross(b - a, c - a);
                    Assert.Greater(Vector3.Dot(cara, centro - origen), 0, $"triángulo {i / 3} mira hacia adentro");
                    Assert.Greater(Vector3.Dot(n[t[i]], cara), 0, $"la normal del triángulo {i / 3} no coincide con su cara");
                }
            }
            finally { Object.DestroyImmediate(m); }
        }
    }
}
