using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Argentina1860.Tests
{
    /// Agua del pago, obstáculos y veredas de la ciudad, datos del plano de 1867.
    public class MundoTests
    {
        // ---------- pago ----------
        [Test]
        public void Pago_LagunaEsAgua_YElRanchoNo()
        {
            var laguna = MapaPago.K(MapaPago.LAGUNA_C);
            Assert.IsTrue(MapaPago.EnAgua(laguna.x, laguna.y), "el centro de la laguna tiene que ser agua");
            var rancho = MapaPago.K(0.59f, 0.9f);   // donde arranca el jugador
            Assert.IsFalse(MapaPago.EnAgua(rancho.x, rancho.y), "el punto de partida no puede ser agua");
        }

        [Test]
        public void Pago_FueraDelMapaNoSeCamina()
        {
            Assert.IsTrue(MapaPago.EnAgua(MapaPago.ANCHO, 0), "el borde del mapa se trata como agua para no salirse");
        }

        // ---------- obstáculos de la ciudad ----------
        [Test]
        public void Campo_CuadradoOcupaSuInterior()
        {
            var c = new CampoCiudad();
            c.Rect(10, 10, 20, 20);
            Assert.IsTrue(c.Ocupado(15, 15));
            Assert.IsFalse(c.Ocupado(25, 15));
            Assert.IsFalse(c.Libre(20.5f, 15), "Libre deja un margen alrededor de las paredes");
            Assert.IsTrue(c.Libre(21, 15));
        }

        [Test]
        public void Campo_FuncionaEnCoordenadasNegativasYEntreCeldas()
        {
            var c = new CampoCiudad();
            c.Rect(-70, -5, -60, 5);      // cruza el borde de las celdas de la rejilla
            c.Rect(-100, -100, 100, 100); // grande, ocupa muchas celdas
            Assert.IsTrue(c.Ocupado(-65, -1));
            Assert.IsTrue(c.Ocupado(-65, 4));
            Assert.IsTrue(c.Ocupado(99, 99));
            Assert.IsTrue(c.Ocupado(-99, -99));
            Assert.IsFalse(c.Ocupado(101, 0));
        }

        [Test]
        public void Campo_PoligonoGiradoNoOcupaSuCaja()
        {
            var c = new CampoCiudad();
            c.Registrar(new[] { new Vector2(0, -10), new Vector2(10, 0), new Vector2(0, 10), new Vector2(-10, 0) });   // rombo
            Assert.IsTrue(c.Ocupado(0, 0));
            Assert.IsFalse(c.Ocupado(9, 9), "la esquina de la caja que encierra al rombo está libre");
        }

        [Test]
        public void Campo_VeredaMasAltaQueLaCalle()
        {
            var c = new CampoCiudad();
            c.RegistrarAltura(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10) }, 0.16f);
            Assert.AreEqual(0.16f, c.Altura(5, 5), 1e-5f);
            Assert.AreEqual(0f, c.Altura(15, 5), 1e-5f);
        }

        // ---------- datos del plano ----------
        static DatosCiudad Plano()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Datos/Ciudad/ciudad.json");
            var pak = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Datos/Ciudad/ciudad_pak.bytes");
            Assert.IsNotNull(json, "falta ciudad.json");
            Assert.IsNotNull(pak, "falta ciudad_pak.bytes");
            return DatosCiudad.Cargar(json, pak);
        }

        [Test]
        public void Plano_SeLeeCompleto()
        {
            var d = Plano();
            Assert.Greater(d.CantManzanas, 3000);
            Assert.AreEqual(35660, d.CantArboles);
            Assert.AreEqual(16, d.iglesias.Length);
            Assert.AreEqual(Vector2.zero, d.AMundo(d.Plaza.x, d.Plaza.y), "la Plaza de la Victoria es el origen del mundo");
            Assert.IsTrue(d.Plaza.x > d.Marco.x && d.Plaza.x < d.Marco.z && d.Plaza.y > d.Marco.y && d.Plaza.y < d.Marco.w, "la plaza cae dentro del plano");
        }

        [Test]
        public void Plano_ElRioQuedaAlEsteDeLaPlaza()
        {
            var d = Plano();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Datos/Ciudad/suelo.png");
            Assert.IsNotNull(tex, "falta suelo.png");
            Assert.IsTrue(tex.isReadable, "suelo.png tiene que ser legible (Read/Write) para la máscara de agua");
            var m = new MascaraAgua(tex, d);
            Assert.IsFalse(m.EnAgua(0, 0), "la plaza no es agua");
            long agua = 0; double sumaX = 0;
            for (int iy = 0; iy < m.H; iy += 4)
                for (int ix = 0; ix < m.W; ix += 4)
                    if (m.AguaPx(ix, iy)) { agua++; sumaX += ix; }
            Assert.Greater(agua, 0, "la máscara no tiene agua");
            float xMedioPx = (float)(sumaX / agua) / m.W * d.Ancho;   // en píxeles del plano
            Assert.Greater(d.AMundo(xMedioPx, d.Plaza.y).x, 0, "el Río de la Plata tiene que quedar al este");
        }
    }
}
