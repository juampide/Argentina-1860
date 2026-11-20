using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Argentina1860.Tests
{
    /// El caballo del jugador: montar, galopar, bajarse, silbarle y cambiar de mundo montado.
    /// Mueve al jugador con ControlJugador.EntradaPrueba (con Unity sin foco el teclado no llega).
    public class CaballoTests
    {
        [SetUp]
        public void Preparar()
        {
            Caballeriza.Reiniciar();
            ControlJugador.EntradaPrueba = null;
            ControlJugador.CorrerPrueba = false;
        }

        [TearDown]
        public void Limpiar()
        {
            Caballeriza.Reiniciar();
            ControlJugador.EntradaPrueba = null;
            ControlJugador.CorrerPrueba = false;
        }

        static Vector2 Plano(Vector3 p) => new Vector2(p.x, p.z);

        static IEnumerator Montado(System.Action<ControlJugador, CaballoMontable> listo)
        {
            yield return EscenasTests.Cargar(CambioDeMundo.PAGO);
            Caballeriza.Instancia.DarCaballo();
            var c = Object.FindAnyObjectByType<CaballoMontable>();
            Assert.IsNotNull(c, "DarCaballo no puso el caballo en el mundo");
            var jug = Object.FindAnyObjectByType<ControlJugador>();
            jug.Montar(c);
            yield return null;
            Assert.AreSame(c, jug.Montura);
            Assert.AreSame(jug.transform, c.transform.parent, "montado, el caballo cuelga del jugador");
            listo(jug, c);
        }

        [UnityTest]
        public IEnumerator Galopa_YGastaAliento()
        {
            ControlJugador jug = null; CaballoMontable c = null;
            yield return Montado((j, cab) => { jug = j; c = cab; });
            var p0 = jug.transform.position;
            ControlJugador.EntradaPrueba = new Vector2(0.3f, 1);   // hacia campo abierto desde el rancho
            ControlJugador.CorrerPrueba = true;
            yield return new WaitForSeconds(4f);   // ~2 s para llegar a galope tendido
            float recorrido = Vector2.Distance(Plano(p0), Plano(jug.transform.position));
            Assert.Greater(recorrido, 20f, "en 4 segundos de galope tendría que recorrer más de 20 m");
            Assert.Less(c.Aliento, 0.95f, "galopar gasta aliento");
            Assert.Greater(jug.VelocidadMontado, c.trote, "va más rápido que al trote");

            ControlJugador.EntradaPrueba = null;
            yield return new WaitForSeconds(2f);
            Assert.Less(jug.VelocidadMontado, 0.5f, "sin teclas, el caballo frena");
            EscenasTests.AssertEnElPiso(jug);
        }

        [UnityTest]
        public IEnumerator SeBaja_YVieneCuandoLeSilban()
        {
            ControlJugador jug = null; CaballoMontable c = null;
            yield return Montado((j, cab) => { jug = j; c = cab; });
            var t = jug.transform;
            bool bajo = false;
            foreach (var lado in new[] { -t.right * 1.4f, t.right * 1.4f, -t.forward * 2.2f })
                if (Caballeriza.Libre(t.position + lado, 0.35f, t.position.y, out var p)) { jug.Desmontar(p + Vector3.up * 0.05f); bajo = true; break; }
            Assert.IsTrue(bajo, "no encontró lugar para bajarse");
            yield return null;
            Assert.IsNull(jug.Montura);
            Assert.IsNull(c.transform.parent, "suelto, el caballo no cuelga del jugador");
            Assert.AreEqual(CaballoMontable.Estado.Suelto, c.Actual);

            // el jugador se aleja 25 m y le silba
            Assert.IsTrue(Caballeriza.LugarLibre(c.transform.position, 25, out var lejos), "no hay lugar libre a 25 m");
            var cc = jug.GetComponent<CharacterController>();
            cc.enabled = false; jug.transform.position = lejos + Vector3.up * 0.05f; cc.enabled = true;
            yield return null;
            c.Llamar(jug.transform);
            float espera = 0;
            while (c.Actual == CaballoMontable.Estado.Viniendo && espera < 20) { espera += Time.deltaTime; yield return null; }
            Assert.Less(Vector2.Distance(Plano(c.transform.position), Plano(jug.transform.position)), 4f, "el caballo no llegó hasta el jugador");
        }

        [UnityTest]
        public IEnumerator CambiaDeMundo_Montado()
        {
            ControlJugador jug = null;
            yield return Montado((j, cab) => jug = j);
            yield return null;   // la caballeriza anota que va montado
            yield return EscenasTests.Cargar(CambioDeMundo.CIUDAD);
            float espera = 0;
            ControlJugador nuevo = null;
            while (espera < 10 && ((nuevo = Object.FindAnyObjectByType<ControlJugador>()) == null || nuevo.Montura == null)) { espera += Time.deltaTime; yield return null; }
            Assert.IsNotNull(nuevo);
            Assert.IsNotNull(nuevo.Montura, "llegó a la ciudad a pie");
            Assert.AreEqual(1, Object.FindObjectsByType<CaballoMontable>().Length, "tiene que haber un solo caballo");
            Assert.AreEqual(CambioDeMundo.CIUDAD, SceneManager.GetActiveScene().name);
        }
    }
}
