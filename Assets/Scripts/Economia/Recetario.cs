using System.Collections.Generic;
using System.Linq;

namespace Argentina1860
{
    /// Reglas para hacer recetas con lo que hay en el morral (la interfaz está en <see cref="Oficios"/>).
    public static class Recetario
    {
        /// Qué objetos del morral usaría la receta (para un ingrediente por categoría, primero los más baratos).
        /// Devuelve null si falta algo; en <paramref name="falta"/> dice qué.
        public static List<(FichaObjeto o, int n)> Elegir(FichaReceta r, out string falta)
        {
            falta = null;
            var usa = new List<(FichaObjeto o, int n)>();
            var quedan = Partida.Morral().ToDictionary(x => x.o, x => x.n);
            foreach (var ing in r.ingredientes)
            {
                int faltan = ing.cantidad;
                foreach (var o in quedan.Keys.Where(ing.Sirve).OrderBy(o => o.precio).ToList())
                {
                    int toma = System.Math.Min(faltan, quedan[o]);
                    if (toma <= 0) continue;
                    usa.Add((o, toma));
                    quedan[o] -= toma;
                    faltan -= toma;
                    if (faltan == 0) break;
                }
                if (faltan > 0) { falta = $"te falta {(ing.cantidad > 1 ? faltan + " de " : "")}{ing.Nombre}"; return null; }
            }
            return usa;
        }

        /// Si se puede hacer ahora, estando en <paramref name="aca"/>. Si no, en <paramref name="porQue"/> dice por qué.
        public static bool PuedeHacer(FichaReceta r, LugarReceta aca, out string porQue)
        {
            porQue = null;
            if (r == null || r.resultado == null) { porQue = "receta incompleta"; return false; }
            if (!Partida.Sabe(r)) { porQue = "no la sabés"; return false; }
            if (r.lugar != LugarReceta.Cualquiera && r.lugar != aca) { porQue = r.lugar switch { LugarReceta.Fogon => "hace falta un fogón encendido", LugarReceta.Banco => "hace falta el banco del rancho", _ => "hace falta la herrería" }; return false; }
            var usa = Elegir(r, out porQue);
            if (usa == null) return false;
            float pesoDespues = Partida.Peso - usa.Sum(x => x.o.peso * x.n) + r.resultado.peso * r.cantidad;
            if (pesoDespues > Partida.CargaMaxima + 1e-4f) { porQue = "no te entraría en el morral"; return false; }
            return true;
        }

        /// Hace la receta: saca los ingredientes y pone el resultado. Devuelve false (sin tocar nada) si no se puede.
        public static bool Hacer(FichaReceta r, LugarReceta aca)
        {
            if (!PuedeHacer(r, aca, out _)) return false;
            foreach (var (o, n) in Elegir(r, out _)) Partida.Sacar(o, n);
            Partida.Dar(r.resultado, r.cantidad, forzar: true);
            return true;
        }
    }
}
