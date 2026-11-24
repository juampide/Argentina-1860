using NUnit.Framework;
using UnityEngine;

namespace Argentina1860.Tests
{
    /// Qué plano de cámara toca en cada línea de una conversación (Docs/guion/cinematicas.md).
    public class CamaraDialogoTests
    {
        /// Simula una conversación: devuelve el plano de cada línea.
        static Plano[] Secuencia(string[] lineas)
        {
            var r = new Plano[lineas.Length];
            Plano anterior = Plano.General;
            int desde = 0;
            bool habla = PlanosDialogo.HablaElGallego(lineas[0]);
            for (int i = 0; i < lineas.Length; i++)
            {
                bool h = PlanosDialogo.HablaElGallego(lineas[i]);
                r[i] = PlanosDialogo.Elegir(i, i > 0 && h != habla, PlanosDialogo.LineaFuerte(lineas[i]), anterior, desde);
                desde = r[i] == Plano.General ? 0 : desde + 1;
                anterior = r[i]; habla = h;
            }
            return r;
        }

        [Test]
        public void SiempreAbreConPlanoGeneral()
        {
            Assert.AreEqual(Plano.General, Secuencia(new[] { "Hola.", "Qué tal." })[0]);
        }

        [Test]
        public void UnSoloQueHabla_AlternaMedioYCortoYVuelveAlGeneral()
        {
            var s = Secuencia(new[] { "a", "b", "c", "d", "e", "f" });
            CollectionAssert.AreEqual(new[] { Plano.General, Plano.Medio, Plano.Corto, Plano.Medio, Plano.General, Plano.Medio }, s);
        }

        [Test]
        public void CuandoCambiaElQueHabla_VaAlPlanoMedio()
        {
            var s = Secuencia(new[] { "Teniente Obregón.", "Gallego: Aquí traigo el título.", "Allí no vive ningún cristiano." });
            Assert.AreEqual(Plano.Medio, s[1]);
            Assert.AreEqual(Plano.Medio, s[2]);
        }

        [Test]
        public void LineaFuerte_VaAlPlanoCorto()
        {
            var s = Secuencia(new[] { "Buenas.", "¡Se acabó el tiempo!" });
            Assert.AreEqual(Plano.Corto, s[1]);
        }

        [Test]
        public void NuncaPasanMasDeTresPlanosSinVolverAlGeneral()
        {
            var lineas = new string[40];
            for (int i = 0; i < lineas.Length; i++) lineas[i] = "línea " + i;
            int seguidos = 0;
            foreach (var p in Secuencia(lineas))
            {
                seguidos = p == Plano.General ? 0 : seguidos + 1;
                Assert.LessOrEqual(seguidos, PlanosDialogo.PLANOS_ENTRE_GENERALES);
            }
        }

        [Test]
        public void SoloLasLineasConGallegoLasDiceElGallego()
        {
            Assert.IsTrue(PlanosDialogo.HablaElGallego("Gallego: Buenas."));
            Assert.IsFalse(PlanosDialogo.HablaElGallego("Obregón: Buenas."));
            Assert.IsFalse(PlanosDialogo.HablaElGallego(null));
        }

        [Test]
        public void LosPersonajesImportantesTienenCamaraDeCine_YLosDemasNo()
        {
            var f = ScriptableObject.CreateInstance<FichaPersonaje>();
            f.id = "obregon";
            Assert.IsTrue(PlanosDialogo.Importante(f));
            f.id = "isidro";
            Assert.IsFalse(PlanosDialogo.Importante(f));
            f.conversacionCine = true;
            Assert.IsTrue(PlanosDialogo.Importante(f));
            Object.DestroyImmediate(f);
        }
    }
}
