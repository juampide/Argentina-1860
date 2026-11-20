using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Argentina1860.Tests
{
    /// Morral, recetas, hambre y cansancio, y el lance de pesca: reglas que no necesitan abrir una escena.
    /// Los primeros usan un catálogo de prueba; los últimos revisan las fichas reales del diseño.
    public class EconomiaTests
    {
        readonly List<Object> creados = new List<Object>();

        FichaObjeto Obj(string id, float peso = 1, float precio = 1, string cat = null, float alimenta = 0)
        {
            var o = ScriptableObject.CreateInstance<FichaObjeto>();
            o.id = id; o.nombre = id; o.peso = peso; o.precio = precio; o.categoria = cat; o.alimenta = alimenta;
            creados.Add(o);
            return o;
        }

        FichaReceta Receta(string id, FichaObjeto res, LugarReceta lugar, params Ingrediente[] ing)
        {
            var r = ScriptableObject.CreateInstance<FichaReceta>();
            r.id = id; r.nombre = id; r.resultado = res; r.lugar = lugar; r.ingredientes = ing.ToList();
            creados.Add(r);
            return r;
        }

        [SetUp]
        public void Preparar()
        {
            Caballeriza.Reiniciar();
            Partida.Reiniciar();
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (var o in creados) Object.DestroyImmediate(o);
            creados.Clear();
            Catalogo.Olvidar();
            Partida.Reiniciar();
        }

        // ---------- morral ----------
        [Test]
        public void Morral_DarYSacar_ConLimiteDePeso()
        {
            var piedra = Obj("piedra", peso: 4);
            Catalogo.Usar(new[] { piedra }, new FichaReceta[0]);
            Assert.IsTrue(Partida.Dar(piedra, 3));
            Assert.AreEqual(12, Partida.Peso, 1e-4f);
            Assert.IsFalse(Partida.Dar(piedra), "a pie no se pueden llevar más de 15 kg");
            Assert.AreEqual(3, Partida.Cantidad(piedra));
            Caballeriza.TieneCaballo = true;
            Assert.IsTrue(Partida.Dar(piedra), "con el caballo entran las alforjas");
            Assert.IsFalse(Partida.Sacar(piedra, 5), "no se saca más de lo que hay");
            Assert.AreEqual(4, Partida.Cantidad(piedra));
            Assert.IsTrue(Partida.Sacar(piedra, 4));
            Assert.AreEqual(0, Partida.Morral().Count);
        }

        [Test]
        public void Habilidades_SubenDeNivel()
        {
            Assert.AreEqual(1, Partida.Nivel(Partida.PESCA));
            Assert.IsFalse(Partida.Ganar(Partida.PESCA, Partida.XP_NIVEL - 0.1f, false));
            Assert.IsTrue(Partida.Ganar(Partida.PESCA, 0.2f, false));
            Assert.AreEqual(2, Partida.Nivel(Partida.PESCA));
        }

        // ---------- recetas ----------
        [Test]
        public void Receta_HayQueSaberla_YConsumeLoQuePide()
        {
            var bagre = Obj("bagre", 1.2f, 4, "pescado");
            var mojarra = Obj("mojarra_t", 0.1f, 1, "pescado");
            var sal = Obj("sal_t", 1, 3);
            var salado = Obj("salado_t", 0.25f, 9);
            var r = Receta("salar", salado, LugarReceta.Cualquiera,
                new Ingrediente { categoria = "pescado", cantidad = 2 }, new Ingrediente { objeto = sal, cantidad = 1 });
            r.cantidad = 2;
            Catalogo.Usar(new[] { bagre, mojarra, sal, salado }, new[] { r });
            Partida.Dar(bagre, 2); Partida.Dar(mojarra, 1); Partida.Dar(sal, 1);

            Assert.IsFalse(Recetario.PuedeHacer(r, LugarReceta.Cualquiera, out var porQue));
            StringAssert.Contains("no la sabés", porQue);
            Assert.IsTrue(Partida.Aprender(r));
            Assert.IsFalse(Partida.Aprender(r), "no se aprende dos veces");

            Assert.IsTrue(Recetario.Hacer(r, LugarReceta.Cualquiera));
            Assert.AreEqual(0, Partida.Cantidad(mojarra), "para 'un pescado' usa primero el más barato");
            Assert.AreEqual(1, Partida.Cantidad(bagre));
            Assert.AreEqual(0, Partida.Cantidad(sal));
            Assert.AreEqual(2, Partida.Cantidad(salado));

            Assert.IsFalse(Recetario.Hacer(r, LugarReceta.Cualquiera), "sin sal no se puede");
            Assert.AreEqual(1, Partida.Cantidad(bagre), "si no se puede, no se toca nada");
        }

        [Test]
        public void Receta_DeFogon_SoloJuntoAlFogon()
        {
            var pez = Obj("pez", 0.3f, 4, "pescado");
            var asado = Obj("asado_t", 0.3f, 0, alimenta: 0.4f);
            var r = Receta("asar", asado, LugarReceta.Fogon, new Ingrediente { categoria = "pescado" });
            r.sabidaDesdeElPrincipio = true;
            Catalogo.Usar(new[] { pez, asado }, new[] { r });
            Partida.Dar(pez);
            Assert.IsFalse(Recetario.Hacer(r, LugarReceta.Cualquiera));
            Assert.IsTrue(Recetario.Hacer(r, LugarReceta.Fogon));
            Assert.AreEqual(1, Partida.Cantidad(asado));
        }

        // ---------- hambre y cansancio ----------
        [Test]
        public void Necesidades_SubenDespacio_YNuncaPasanDelLimite()
        {
            Partida.Hambre = 0; Partida.Cansancio = 0;
            for (int i = 0; i < 15 * 60; i++) Partida.Pasar(1, Actividad.Caminando);   // 15 minutos
            Assert.AreEqual(0.5f, Partida.Hambre, 0.02f, "en 15 minutos llega a la mitad del hambre");
            Assert.AreEqual(1f, Partida.FactorCorrer, 1e-4f, "con poca hambre corre normal");
            for (int i = 0; i < 3 * 3600; i++) Partida.Pasar(1, Actividad.Corriendo);
            Assert.AreEqual(1f, Partida.Hambre, 1e-4f);
            Assert.AreEqual(1f, Partida.Cansancio, 1e-4f);
            Assert.AreEqual(0f, Partida.FactorCorrer, 1e-4f, "muerto de hambre y molido ya no corre");
            Assert.Greater(Partida.FactorTrabajo, 0.5f, "pero sigue pudiendo trabajar");
        }

        [Test]
        public void Necesidades_ComerYDescansar()
        {
            var galleta = Obj("galleta_t", 0.2f, 2, alimenta: 0.3f);
            var piedra = Obj("piedra_t");
            Catalogo.Usar(new[] { galleta, piedra }, new FichaReceta[0]);
            Partida.Hambre = 0.8f; Partida.Cansancio = 0.8f;
            Partida.Dar(galleta); Partida.Dar(piedra);
            Assert.IsFalse(Partida.Comer(piedra));
            Assert.IsTrue(Partida.Comer(galleta));
            Assert.AreEqual(0.5f, Partida.Hambre, 1e-4f);
            Assert.IsFalse(Partida.Comer(galleta), "ya no queda");
            for (int i = 0; i < 30; i++) Partida.Pasar(1, Actividad.Descansando);
            Assert.AreEqual(0f, Partida.Cansancio, 1e-4f, "medio minuto junto al fogón alcanza");
        }

        // ---------- pesca ----------
        static EstadoLance Jugar(System.Func<LancePesca, bool> aprieta, float fuerza, int semilla, int nivel = 1, float rinde = 1, float maxSeg = 60)
        {
            var l = new LancePesca(null, fuerza, 2, nivel, rinde, new System.Random(semilla));
            bool antes = false;
            for (float t = 0; t < maxSeg && !l.Terminado; t += 1 / 60f)
            {
                bool a = aprieta(l);
                l.Paso(1 / 60f, a, a && !antes);
                antes = a;
            }
            return l.Estado;
        }

        static bool BuenPescador(LancePesca l) => l.Estado == EstadoLance.Pica || (l.Estado == EstadoLance.Peleando && l.Tension < (l.ZonaMin + l.ZonaMax) / 2);

        [Test]
        public void Pesca_ConMano_SeSacanTodos()
        {
            foreach (float fuerza in new[] { 0.1f, 0.4f, 0.75f, 1f })
                for (int s = 0; s < 20; s++)
                    Assert.AreEqual(EstadoLance.Sacado, Jugar(BuenPescador, fuerza, s, rinde: 0.6f), $"fuerza {fuerza}, semilla {s}");
        }

        [Test]
        public void Pesca_TirarSinParar_CortaLaLinea()
        {
            for (int s = 0; s < 10; s++)
                Assert.AreEqual(EstadoLance.Cortado, Jugar(l => l.Estado >= EstadoLance.Pica, 0.4f, s));
        }

        [Test]
        public void Pesca_NoRecoger_SeEscapa()
        {
            for (int s = 0; s < 10; s++)
                Assert.AreEqual(EstadoLance.Escapo, Jugar(l => l.Estado == EstadoLance.Pica, 0.4f, s));
        }

        [Test]
        public void Pesca_TardeOTemprano()
        {
            Assert.AreEqual(EstadoLance.SinCarnada, Jugar(l => false, 0.4f, 1), "si no tira cuando pica, se come la carnada");
            Assert.AreEqual(EstadoLance.Temprano, Jugar(l => true, 0.4f, 1), "si tira antes de que pique, no hay nada");
        }

        // ---------- las fichas del diseño ----------
        [Test]
        public void Fichas_ObjetosYRecetasCompletos()
        {
            Catalogo.Olvidar();
            var objs = Resources.LoadAll<FichaObjeto>(Catalogo.CARPETA_OBJETOS);
            Assert.Greater(objs.Length, 0, "no hay fichas de objeto en Resources/Objetos");
            foreach (var o in objs)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(o.id), $"la ficha {o.name} no tiene id");
                Assert.IsFalse(string.IsNullOrWhiteSpace(o.nombre), $"la ficha {o.id} no tiene nombre");
                Assert.GreaterOrEqual(o.peso, 0, o.id);
                if (o.pulperiaVende || o.pulperiaCompra) Assert.GreaterOrEqual(o.precio, 1, $"{o.id} se compra o vende en la pulpería sin precio");
                if (o.tipo == TipoObjeto.Pescado) Assert.IsTrue(o.enLaguna || o.enArroyo, $"el pez {o.id} no vive en ningún lado");
            }
            var repetidos = objs.GroupBy(o => o.id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.IsEmpty(repetidos, "ids repetidos: " + string.Join(", ", repetidos));

            foreach (var r in Resources.LoadAll<FichaReceta>(Catalogo.CARPETA_RECETAS))
            {
                Assert.IsNotNull(r.resultado, $"la receta {r.id} no da nada");
                Assert.IsNotEmpty(r.ingredientes, $"la receta {r.id} no pide nada");
                foreach (var i in r.ingredientes)
                    Assert.IsTrue(objs.Any(i.Sirve), $"la receta {r.id} pide '{i.Nombre}' y no hay ningún objeto que sirva");
                Assert.IsTrue(r.sabidaDesdeElPrincipio || r.ensena != null, $"la receta {r.id} no se puede aprender: nadie la enseña");
            }
        }

        [Test]
        public void Fichas_SePuedePescarDesdeCero()
        {
            Catalogo.Olvidar();
            Assert.IsTrue(Catalogo.Buscar("linea")?.pulperiaVende ?? false, "la pulpería tiene que vender la línea");
            Assert.IsTrue(Catalogo.Buscar("anzuelo")?.pulperiaVende ?? false, "la pulpería tiene que vender anzuelos");
            Assert.IsNotNull(Catalogo.Buscar("lombrices"), "faltan las lombrices (carnada gratis de la orilla)");
            var rnd = new System.Random(3);
            foreach (bool laguna in new[] { true, false })
                for (float hora = 0; hora < 24; hora += 1.5f)
                {
                    var (pez, espera) = Oficios.ElegirPez(laguna, 1, hora, rnd);
                    Assert.IsNotNull(pez, $"no pica nada en {(laguna ? "la laguna" : "el arroyo")} a las {hora}");
                    Assert.IsTrue(laguna ? pez.enLaguna : pez.enArroyo, $"{pez.id} picó donde no vive");
                    Assert.That(espera, Is.InRange(2f, 15f));
                }
            Assert.IsTrue(Catalogo.Objetos.Any(o => o.tipo == TipoObjeto.Pescado && o.pulperiaCompra), "la pulpería tiene que comprar algún pescado");
        }
    }
}
