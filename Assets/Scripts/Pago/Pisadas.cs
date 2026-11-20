using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Lo que pisa el pasto: el jugador, los personajes, los animales grandes, los caballos y las carretas.
    /// Cada uno se anota con un radio. El pasto (Pasto.cs) lee esta lista, marca por dónde pasaron
    /// y las matas se doblan, quedan aplastadas y se levantan de a poco.
    /// </summary>
    public static class Pisadas
    {
        public struct Pisador
        {
            public Transform t;
            public float radio;
        }

        static readonly List<Pisador> lista = new List<Pisador>();

        /// Radio de pisada por especie (metros). Las que no están no aplastan el pasto (perdiz, yarará, caranchos...).
        static readonly Dictionary<string, float> RADIO = new Dictionary<string, float>
        {
            ["vaca"] = 0.9f, ["caballo"] = 0.8f, ["buey"] = 0.9f, ["venado"] = 0.45f, ["nandu"] = 0.4f,
            ["carpincho"] = 0.45f, ["puma"] = 0.4f, ["zorro"] = 0.3f, ["carreta"] = 1.5f, ["oveja"] = 0.5f, ["perro"] = 0.3f,
        };

        public static void Registrar(Transform t, float radio)
        {
            if (t == null || radio <= 0) return;
            for (int i = 0; i < lista.Count; i++) if (lista[i].t == t) return;
            lista.Add(new Pisador { t = t, radio = radio });
        }

        /// Anota un animal según su especie (si es de los que aplastan el pasto).
        public static void Registrar(Transform t, string especie)
        {
            if (RADIO.TryGetValue(especie, out var r)) Registrar(t, r);
        }

        /// Recorre los pisadores vivos y activos (saca de la lista los que se destruyeron).
        public static IReadOnlyList<Pisador> Activos()
        {
            for (int i = lista.Count - 1; i >= 0; i--)
                if (lista[i].t == null) lista.RemoveAt(i);
            return lista;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => lista.Clear();
    }
}
