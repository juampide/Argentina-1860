using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Argentina1860.Tests
{
    /// Pesca de verdad en la laguna del pago, y la plata y el morral que pasan de un mundo al otro.
    public class PescaTests
    {
        [SetUp]
        public void Preparar()
        {
            Caballeriza.Reiniciar();
            Partida.Reiniciar();
            Catalogo.Olvidar();
            ControlJugador.EntradaPrueba = null;
        }

        [TearDown]
        public void Limpiar()
        {
            Oficios.ApretarPrueba = null;
            Oficios.Instancia?.Terminar();
            Time.timeScale = 1;
            Caballeriza.Reiniciar();
            Partida.Reiniciar();
        }

        /// Para al jugador en la orilla de la laguna, mirando al agua.
        static void ALaOrilla(ControlJugador jug)
        {
            var c = MapaPago.K(MapaPago.LAGUNA_C);
            float x = c.x;
            while (MapaPago.DistLaguna(x, c.y) < 0) x += 1;
            x += 3;   // unos pasos afuera del agua
            Assert.IsFalse(MapaPago.Bloqueado(x, c.y), "la orilla quedó bloqueada");
            var p = new Vector3(x, 0, -c.y);
            p.y = Terreno.Altura(p) + 0.1f;
            var cc = jug.GetComponent<CharacterController>();
            cc.enabled = false;
            jug.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Vector3.left));   // al oeste, hacia el centro de la laguna
            cc.enabled = true;
        }

        [UnityTest]
        public IEnumerator Pesca_EnLaLaguna_SacaUnPez()
        {
            yield return EscenasTests.Cargar(CambioDeMundo.PAGO);
            var jug = Object.FindAnyObjectByType<ControlJugador>();
            ALaOrilla(jug);
            yield return null;
            Assert.IsTrue(Oficios.AguaAdelante(jug.transform.position, jug.transform.forward, out _, out int tipo), "desde la orilla no se ve el agua");
            Assert.AreEqual(1, tipo, "tendría que ser la laguna");

            Partida.Dar("linea"); Partida.Dar("anzuelo"); Partida.Dar("lombrices", 3);
            Oficios.ApretarPrueba = l => l.Estado == EstadoLance.Pica || (l.Estado == EstadoLance.Peleando && l.Tension < (l.ZonaMin + l.ZonaMax) / 2);
            Time.timeScale = 4;   // que no tarde tanto en picar
            Assert.IsTrue(Oficios.Instancia.EmpezarPesca(), "no pudo tirar la línea");
            Assert.IsTrue(Oficios.Ocupado);
            Assert.IsTrue(jug.Bloqueado, "pescando no se camina");

            float t = 0;
            while (Oficios.Ocupado && t < 60) { t += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1;
            Assert.IsFalse(Oficios.Ocupado, "el lance no terminó");
            Assert.IsFalse(jug.Bloqueado, "al terminar vuelve a caminar");
            Assert.AreEqual(2, Partida.Cantidad("lombrices"), "gastó una carnada");
            Assert.AreEqual(1, Partida.Cantidad("anzuelo"), "con buena mano no se corta la línea");
            int peces = Partida.CantidadCategoria("pescado") + Partida.Cantidad("mojarra");
            Assert.AreEqual(1, peces, "tendría que haber sacado un pez");
            Assert.Greater(Partida.Xp(Partida.PESCA), 0);
        }

        [UnityTest]
        public IEnumerator LaPlataYElMorral_PasanALaCiudad()
        {
            yield return EscenasTests.Cargar(CambioDeMundo.PAGO);
            Partida.Monedas = 37;
            Partida.Dar("pejerrey", 2);
            yield return EscenasTests.Cargar(CambioDeMundo.CIUDAD);
            Assert.AreEqual(37, Object.FindAnyObjectByType<JuegoCiudad>().monedas);
            Assert.AreEqual(2, Partida.Cantidad("pejerrey"));
        }
    }
}
