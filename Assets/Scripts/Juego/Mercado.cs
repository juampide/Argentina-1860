using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Mercado de la pulpería: los precios se mueven solos (ruido + noticias que los empujan y se disipan).
    /// El comercio sube con el uso: a más nivel, menos diferencia entre lo que paga y lo que cobra Isidro.
    /// </summary>
    public class Mercado
    {
        public class Bien
        {
            public string id, nombre;
            public float base_, ruido = 1, mult = 1;
            public int precio;
        }

        static readonly (string id, string nombre, float base_)[] BIENES =
        {
            ("yerba", "Yerba", 6), ("tabaco", "Tabaco", 8), ("cana", "Caña", 11), ("tela", "Tela", 15), ("sal", "Sal", 3),
        };

        static readonly (float f, Func<string, string> t)[] NOTICIAS =
        {
            (0.65f, n => $"Llegó una carreta cargada: {n} está barata."),
            (1.55f, n => $"Se corta el camino: escasea {n}."),
            (0.75f, n => $"Hay mucha oferta de {n} en la pulpería."),
            (1.4f, n => $"Pasó una tropa y se llevó todo el stock de {n}."),
        };

        public readonly List<Bien> bienes = new List<Bien>();
        readonly Action<string> aviso;
        float prox;

        public Mercado(Action<string> aviso)
        {
            this.aviso = aviso;
            foreach (var b in BIENES) bienes.Add(new Bien { id = b.id, nombre = b.nombre, base_ = b.base_, precio = (int)b.base_ });
            prox = 50 + UnityEngine.Random.value * 40;
            Recalcular();
        }

        public Bien PorId(string id) => bienes.Find(b => b.id == id);

        void Recalcular()
        {
            foreach (var b in bienes) b.precio = Mathf.Max(1, Conv.RoundJS(b.base_ * b.mult * b.ruido));
        }

        /// Lo que cobra Isidro cuando le comprás.
        public int Compra(Bien b, int nivel) => Mathf.Max(b.precio + 1, Conv.RoundJS(b.precio * Mathf.Max(1.05f, 1.18f - 0.012f * nivel)));
        /// Lo que paga Isidro cuando le vendés.
        public int Venta(Bien b, int nivel) => Mathf.Max(1, Conv.RoundJS(b.precio * Mathf.Min(0.9f, 0.78f + 0.012f * nivel)));

        public void Actualizar(float dt)
        {
            foreach (var b in bienes)
            {
                b.ruido += (1 - b.ruido) * 0.05f * dt + (UnityEngine.Random.value - 0.5f) * 0.05f * Mathf.Sqrt(dt);
                b.ruido = Mathf.Clamp(b.ruido, 0.8f, 1.25f);
                b.mult += (1 - b.mult) * 0.012f * dt;            // las noticias se disipan en un minuto o dos
            }
            prox -= dt;
            if (prox <= 0)
            {
                prox = 70 + UnityEngine.Random.value * 60;
                var b = bienes[UnityEngine.Random.Range(0, bienes.Count)];
                var n = NOTICIAS[UnityEngine.Random.Range(0, NOTICIAS.Length)];
                b.mult = Mathf.Clamp(b.mult * n.f, 0.55f, 1.8f);
                aviso?.Invoke(n.t(b.nombre));
            }
            Recalcular();
        }
    }
}
