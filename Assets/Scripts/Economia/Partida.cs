using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Argentina1860
{
    /// Qué está haciendo el Gallego (para el hambre y el cansancio).
    public enum Actividad { Quieto, Caminando, Corriendo, Montado, Trabajando, Descansando }

    /// <summary>
    /// El estado del jugador que pasa de un mundo al otro: plata, morral, habilidades, recetas que sabe, hambre y
    /// cansancio. (Todavía no se guarda en disco: se pierde al cerrar el juego.)
    ///
    /// Necesidades suaves: el hambre y el cansancio suben despacio y solo dan desventajas (correr menos, rendir
    /// menos al trabajar). Nunca matan.
    /// </summary>
    public static class Partida
    {
        // ---------- plata ----------
        public static int Monedas;

        // ---------- morral ----------
        static readonly Dictionary<string, int> objetos = new Dictionary<string, int>();
        static readonly Dictionary<string, float> costo = new Dictionary<string, float>();

        /// Kilos que se pueden llevar a pie y con el caballo (alforjas).
        public const float CARGA_A_PIE = 15, CARGA_CON_CABALLO = 40;

        // ---------- habilidades ----------
        public const string ARREO = "arreo", COMERCIO = "comercio", PESCA = "pesca";
        /// Experiencia por nivel.
        public const float XP_NIVEL = 6;
        static readonly Dictionary<string, float> xp = new Dictionary<string, float>();

        // ---------- recetas ----------
        static readonly HashSet<string> recetas = new HashSet<string>();

        // ---------- necesidades (0 bien, 1 al límite) ----------
        public static float Hambre, Cansancio;
        /// Minutos reales para pasar de lleno a muerto de hambre.
        public const float MINUTOS_HAMBRE = 30;
        /// Desde dónde se nota (avisos y desventajas).
        public const float UMBRAL = 0.7f;

        /// Avisos para mostrar en pantalla (hambre, objetos que no entran, niveles).
        public static event Action<string> Aviso;

        // ---------- arranque ----------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void AlArrancar()
        {
            // el proyecto no recarga el dominio al dar Play: sin esto, la partida anterior seguiría viva
            Aviso = null;
            Catalogo.Olvidar();
            Reiniciar();
        }

        /// Partida nueva: sin plata, morral vacío, solo las recetas que se saben desde el principio.
        public static void Reiniciar()
        {
            Monedas = 0;
            objetos.Clear(); costo.Clear(); xp.Clear(); recetas.Clear();
            Hambre = 0.15f; Cansancio = 0;
            avisoHambre = avisoCansancio = 0;
        }

        static void Avisar(string t) => Aviso?.Invoke(t);

        // ---------- morral ----------
        public static int Cantidad(string id) => id != null && objetos.TryGetValue(id, out var n) ? n : 0;
        public static int Cantidad(FichaObjeto o) => o != null ? Cantidad(o.id) : 0;

        /// Lo que hay en el morral, ordenado por tipo y nombre.
        public static List<(FichaObjeto o, int n)> Morral()
        {
            var l = new List<(FichaObjeto, int)>();
            foreach (var kv in objetos)
            {
                if (kv.Value <= 0) continue;
                var o = Catalogo.Buscar(kv.Key);
                if (o != null) l.Add((o, kv.Value));
            }
            return l.OrderBy(x => x.Item1.tipo).ThenBy(x => x.Item1.nombre).ToList();
        }

        /// Cantidad total de una categoría (por ejemplo, cuántos pescados hay).
        public static int CantidadCategoria(string cat) => Morral().Where(x => x.o.categoria == cat).Sum(x => x.n);

        public static float Peso => Morral().Sum(x => x.o.peso * x.n);
        public static float CargaMaxima => Caballeriza.TieneCaballo ? CARGA_CON_CABALLO : CARGA_A_PIE;
        public static bool Entra(FichaObjeto o, int n = 1) => o != null && Peso + o.peso * n <= CargaMaxima + 1e-4f;

        /// Pone objetos en el morral. Si no entran, no pone nada y devuelve false (con <paramref name="forzar"/>, entran igual).
        public static bool Dar(FichaObjeto o, int n = 1, bool forzar = false)
        {
            if (o == null || n <= 0) return false;
            if (!forzar && !Entra(o, n)) { Avisar($"No te entra en el morral: {o.nombre}."); return false; }
            objetos[o.id] = Cantidad(o.id) + n;
            return true;
        }

        public static bool Dar(string id, int n = 1, bool forzar = false) => Dar(Catalogo.Buscar(id), n, forzar);

        /// Saca objetos del morral; si no hay tantos, no saca nada.
        public static bool Sacar(string id, int n = 1)
        {
            int c = Cantidad(id);
            if (n <= 0 || c < n) return false;
            if (c == n) { objetos.Remove(id); costo.Remove(id); } else objetos[id] = c - n;
            return true;
        }

        public static bool Sacar(FichaObjeto o, int n = 1) => o != null && Sacar(o.id, n);

        /// Lo que pagó en promedio por lo que tiene (para decirle si gana o pierde al vender). Lo que juntó gratis vale 0.
        public static float CostoPromedio(string id) => costo.TryGetValue(id, out var c) ? c : 0;

        public static void RegistrarCompra(string id, float precio, int antes)
        {
            costo[id] = (CostoPromedio(id) * antes + precio) / (antes + 1);
        }

        // ---------- habilidades ----------
        public static float Xp(string hab) => xp.TryGetValue(hab, out var v) ? v : 0;
        public static int Nivel(string hab) => 1 + Mathf.FloorToInt(Xp(hab) / XP_NIVEL);

        /// Suma experiencia; devuelve true si subió de nivel (y avisa).
        public static bool Ganar(string hab, float cuanto, bool avisar = true)
        {
            int antes = Nivel(hab);
            xp[hab] = Xp(hab) + Mathf.Max(0, cuanto);
            bool sube = Nivel(hab) > antes;
            if (sube && avisar) Avisar($"{NombreHabilidad(hab)} sube a nivel {Nivel(hab)}.");
            return sube;
        }

        public static string NombreHabilidad(string hab) => hab switch
        {
            ARREO => "Arreo", COMERCIO => "Comercio", PESCA => "Pesca",
            _ => hab.Length > 0 ? char.ToUpper(hab[0]) + hab.Substring(1) : hab,
        };

        public static IEnumerable<string> Habilidades => xp.Keys;

        // ---------- recetas ----------
        public static bool Sabe(FichaReceta r) => r != null && (r.sabidaDesdeElPrincipio || recetas.Contains(r.id));

        /// Aprende una receta; devuelve false si ya la sabía.
        public static bool Aprender(FichaReceta r)
        {
            if (r == null || Sabe(r)) return false;
            recetas.Add(r.id);
            Avisar($"Aprendiste: {r.nombre}.");
            return true;
        }

        // ---------- necesidades ----------
        static int avisoHambre, avisoCansancio;

        /// Hace pasar el tiempo para el hambre y el cansancio.
        public static void Pasar(float dt, Actividad a)
        {
            float hambre = dt / (MINUTOS_HAMBRE * 60) * (a == Actividad.Trabajando ? 1.5f : a == Actividad.Corriendo ? 1.3f : 1);
            Hambre = Mathf.Clamp01(Hambre + hambre);

            float c = a switch
            {
                Actividad.Corriendo => dt / 240f,
                Actividad.Trabajando => dt / 600f,
                Actividad.Caminando => dt / 2700f,
                Actividad.Montado => dt / 3600f,
                Actividad.Quieto => -dt / 600f,
                Actividad.Descansando => -dt / 25f,
                _ => 0,
            };
            // con hambre no se descansa del todo
            if (c < 0 && Hambre > UMBRAL) c *= 0.4f;
            Cansancio = Mathf.Clamp01(Cansancio + c);

            avisoHambre = AvisoUmbral(Hambre, avisoHambre, "Te está picando el hambre.", "Tenés hambre: así no das para correr ni para trabajar.");
            avisoCansancio = AvisoUmbral(Cansancio, avisoCansancio, "Te vas cansando.", "Estás molido: sentate un rato junto a un fogón.");
        }

        static int AvisoUmbral(float v, int dado, string leve, string fuerte)
        {
            int nivel = v >= 0.88f ? 2 : v >= UMBRAL ? 1 : 0;
            if (nivel > dado) Avisar(nivel == 2 ? fuerte : leve);
            return v < UMBRAL - 0.1f ? 0 : Mathf.Max(dado, nivel);   // vuelve a avisar solo después de recuperarse
        }

        /// Cuánto de la velocidad de correr le queda (1 entera; 0 = ya solo camina).
        public static float FactorCorrer => 1 - Mathf.InverseLerp(UMBRAL, 1, Mathf.Max(Hambre, Cansancio));

        /// Cuánto rinde trabajando (1 normal; baja hasta 0,6 con hambre o cansancio).
        public static float FactorTrabajo => 1 - 0.4f * Mathf.InverseLerp(UMBRAL, 1, Mathf.Max(Hambre, Cansancio));

        /// Come algo del morral. Devuelve false si no es comida o no tiene.
        public static bool Comer(FichaObjeto o)
        {
            if (o == null || !o.SeCome || !Sacar(o.id)) return false;
            Hambre = Mathf.Clamp01(Hambre - o.alimenta);
            Avisar(!string.IsNullOrEmpty(o.alComer) ? o.alComer : Hambre < 0.1f ? $"Comiste {o.nombre}. Quedaste lleno." : $"Comiste {o.nombre}.");
            return true;
        }
    }
}
