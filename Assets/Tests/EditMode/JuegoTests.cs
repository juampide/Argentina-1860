using NUnit.Framework;
using UnityEngine;

namespace Argentina1860.Tests
{
    /// Reglas del juego que no necesitan abrir una escena.
    public class JuegoTests
    {
        /// Si la pulpería pagara por algo tanto como lo cobra, el jugador haría plata infinita comprando y vendiendo.
        [Test]
        public void Pulperia_NuncaPagaMasDeLoQueCobra()
        {
            var m = new Mercado(null);
            foreach (var b in m.bienes)
                for (int precio = 1; precio <= 300; precio++)
                    for (int nivel = 0; nivel <= 60; nivel++)
                    {
                        b.precio = precio;
                        Assert.Greater(m.Compra(b, nivel), m.Venta(b, nivel), $"{b.nombre} a ${precio}, comercio nivel {nivel}");
                        Assert.GreaterOrEqual(m.Venta(b, nivel), 1);
                    }
        }

        [Test]
        public void Pulperia_MasNivelMejorPrecio()
        {
            var m = new Mercado(null);
            var b = m.bienes[0];
            b.precio = 100;
            Assert.LessOrEqual(m.Compra(b, 20), m.Compra(b, 0));
            Assert.GreaterOrEqual(m.Venta(b, 20), m.Venta(b, 0));
        }

        [Test]
        public void Pulperia_LosPreciosNoSeDisparan()
        {
            var m = new Mercado(null);
            for (int i = 0; i < 20000; i++) m.Actualizar(0.5f);   // casi tres horas de juego
            foreach (var b in m.bienes)
            {
                Assert.GreaterOrEqual(b.precio, 1, b.nombre);
                Assert.LessOrEqual(b.precio, Mathf.CeilToInt(b.base_ * 1.8f * 1.25f) + 1, b.nombre);
            }
        }

        // ---------- aliento del caballo ----------
        static CaballoMontable Caballo(out GameObject go)
        {
            go = new GameObject("caballo de prueba");   // en modo edición no corre Awake: no arma el modelo
            return go.AddComponent<CaballoMontable>();
        }

        [Test]
        public void Caballo_GalopeGastaAliento_YSeCansa()
        {
            var c = Caballo(out var go);
            try
            {
                float dt = 0.1f;
                for (float t = 0; t < c.segundosGalope * 0.5f; t += dt) c.Esfuerzo(dt, true, c.galope);
                Assert.AreEqual(0.5f, c.Aliento, 0.02f, "a mitad del tiempo de galope queda la mitad del aliento");
                Assert.IsFalse(c.Cansado);
                for (float t = 0; t < c.segundosGalope; t += dt) c.Esfuerzo(dt, true, c.galope);
                Assert.AreEqual(0f, c.Aliento, 1e-4f);
                Assert.IsTrue(c.Cansado, "sin aliento queda cansado");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Caballo_SeRecupera_MasRapidoQuieto()
        {
            var c = Caballo(out var go);
            try
            {
                float dt = 0.1f;
                for (float t = 0; t < c.segundosGalope + 1; t += dt) c.Esfuerzo(dt, true, c.galope);
                Assert.IsTrue(c.Cansado);
                // al trote, sigue cansado hasta recuperar un tercio
                for (float t = 0; t < c.segundosRecupera * 0.25f; t += dt) c.Esfuerzo(dt, false, c.trote);
                Assert.IsTrue(c.Cansado, "con un cuarto del aliento todavía no galopa");
                for (float t = 0; t < c.segundosRecupera * 0.15f; t += dt) c.Esfuerzo(dt, false, c.trote);
                Assert.IsFalse(c.Cansado, "pasado un tercio vuelve a galopar");

                float alTrote = c.Aliento;
                c.Esfuerzo(1, false, c.trote);
                float ganaTrotando = c.Aliento - alTrote;
                float antes = c.Aliento;
                c.Esfuerzo(1, false, 0);
                Assert.Greater(c.Aliento - antes, ganaTrotando * 1.5f, "quieto se recupera más rápido que al trote");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
