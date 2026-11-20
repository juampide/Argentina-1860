using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Argentina1860.Tests
{
    /// Carga cada mundo y revisa lo básico. Cualquier error o excepción en la consola hace fallar el test.
    public class EscenasTests
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

        // ---------- ayudas compartidas con CaballoTests ----------
        public static IEnumerator Cargar(string escena, float maxSegundos = 90)
        {
            SceneManager.LoadScene(escena);
            yield return null;
            float t = 0;
            while (Object.FindAnyObjectByType<ControlJugador>() == null && t < maxSegundos) { t += Time.unscaledDeltaTime; yield return null; }
            if (escena == CambioDeMundo.CIUDAD)
                while ((Ciudad.Instancia == null || !Ciudad.Instancia.Generado) && t < maxSegundos) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.Less(t, maxSegundos, $"{escena} tardó demasiado en cargar");
            yield return new WaitForSeconds(1.5f);   // el jugador cae al piso y la caballeriza termina de prepararse
        }

        public static void AssertEnElPiso(ControlJugador jug)
        {
            var p = jug.transform.position;
            Assert.IsTrue(Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var h, 3f, ~0, QueryTriggerInteraction.Ignore), $"no hay piso bajo el jugador en {p}");
            Assert.Less(p.y - h.point.y, 0.3f, $"el jugador está flotando en {p}");
        }

        // ---------- pago ----------
        [UnityTest]
        public IEnumerator Pago_CargaConJugadorYPersonajes()
        {
            yield return Cargar(CambioDeMundo.PAGO);
            var jug = Object.FindAnyObjectByType<ControlJugador>();
            AssertEnElPiso(jug);
            Assert.GreaterOrEqual(Object.FindObjectsByType<NPC>().Length, 10, "faltan personajes en el pago");
            Assert.IsFalse(ControlJugador.AguaEn(jug.transform.position.x, -jug.transform.position.z), "el jugador arranca en el agua");
        }

        /// Un rayo a la altura del pecho, desde dos metros, tiene que chocar con el tronco de cada árbol (la cortadera no:
        /// es una mata). Así se encontró que solo el ombú tenía colisión.
        [UnityTest]
        public IEnumerator Pago_LosArbolesBloquean()
        {
            yield return Cargar(CambioDeMundo.PAGO);
            var t = Terrain.activeTerrain;
            var td = t.terrainData;
            var protos = td.treePrototypes;
            var probados = new int[protos.Length];
            foreach (var ti in td.treeInstances)
            {
                string esp = protos[ti.prototypeIndex].prefab.name;
                if (esp == "cortadera" || probados[ti.prototypeIndex] >= 15 || ti.heightScale < 0.8f) continue;
                var p = Vector3.Scale(ti.position, td.size) + t.transform.position;
                var desde = p + new Vector3(2f, 1f, 0);
                if (Physics.CheckSphere(desde, 0.3f, ~0, QueryTriggerInteraction.Ignore)) continue;   // otro árbol pegado o una loma: probar otro
                Assert.IsTrue(Physics.Raycast(desde, Vector3.left, out var h, 2.5f, ~0, QueryTriggerInteraction.Ignore), $"se atraviesa el {esp} en {p}");
                probados[ti.prototypeIndex]++;
            }
            for (int i = 0; i < protos.Length; i++)
                if (protos[i].prefab.name != "cortadera") Assert.Greater(probados[i], 0, $"no se probó ningún {protos[i].prefab.name}");
        }

        // ---------- ciudad ----------
        [UnityTest]
        public IEnumerator Ciudad_CargaConJugadorYPersonajes()
        {
            yield return Cargar(CambioDeMundo.CIUDAD);
            var c = Ciudad.Instancia;
            Assert.Greater(c.bloques.Count, 3000, "faltan manzanas");
            AssertEnElPiso(Object.FindAnyObjectByType<ControlJugador>());
            Assert.GreaterOrEqual(Object.FindObjectsByType<NPC>().Length, 8, "faltan personajes en la ciudad");
        }

        /// Desde la calle, un rayo hacia el centro de cada manzana tiene que chocar con la pared, y la vereda tiene que
        /// tener piso a 16 cm. (Así se encontró que la mitad de las casas se podían atravesar.)
        [UnityTest]
        public IEnumerator Ciudad_LasCasasBloquean_YLasVeredasSeSuben()
        {
            yield return Cargar(CambioDeMundo.CIUDAD);
            int probadas = 0, conVereda = 0;
            foreach (var b in Ciudad.Instancia.bloques)
            {
                var cen = (b[0] + b[2]) / 2;
                var mid = (b[0] + b[1]) / 2;
                var sal = (mid - cen).normalized;
                var fuera = mid + sal * 4f;
                if (Ciudad.Instancia.Campo.Ocupado(fuera.x, fuera.y)) continue;   // pegada a otra manzana o a una iglesia
                var o = new Vector3(fuera.x, 1f, -fuera.y);
                var dir = (new Vector3(cen.x, 1f, -cen.y) - o).normalized;
                Assert.IsTrue(Physics.Raycast(o, dir, out _, 12f, ~0, QueryTriggerInteraction.Ignore), $"se atraviesa la manzana con centro {cen}");
                probadas++;

                float lu = Vector2.Distance(b[0], b[1]), lv = Vector2.Distance(b[0], b[3]);
                if (cen.magnitude < 500 && lu > 50 && lv > 50)   // manzanas del centro, con lotes y vereda
                {
                    var pv = mid + sal * 0.8f;
                    Assert.IsTrue(Physics.Raycast(new Vector3(pv.x, 3f, -pv.y), Vector3.down, out var h, 5f, ~0, QueryTriggerInteraction.Ignore));
                    Assert.Greater(h.point.y, 0.1f, $"la vereda de la manzana {cen} no tiene piso elevado");
                    conVereda++;
                }
            }
            Assert.Greater(probadas, 1000, "se probaron muy pocas manzanas");
            Assert.Greater(conVereda, 10, "se probaron muy pocas veredas");
        }
    }
}
