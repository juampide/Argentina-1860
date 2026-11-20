using UnityEngine;

namespace Argentina1860
{
    public enum EstadoLance
    {
        Esperando,   // la línea en el agua, el corcho quieto
        Pica,        // el corcho se hunde: hay que tirar ya
        Peleando,    // el pez tira: mantener la tensión en la franja buena
        Sacado,      // salió el pez
        Escapo,      // la línea quedó floja y el pez se soltó
        Cortado,     // demasiada tensión: se cortó la línea y se perdió el anzuelo
        SinCarnada,  // no tiró a tiempo: el pez se comió la carnada
        Temprano,    // tiró antes de que picara: recoge la línea sin nada
    }

    /// <summary>
    /// Un lance de pesca, sin nada de pantalla (lo dibuja <see cref="Oficios"/>; los tests lo juegan solo).
    /// Primero se espera a que pique; cuando el corcho se hunde hay que tirar enseguida. Después, mientras se
    /// aprieta la tecla se recoge línea y sube la tensión; al soltar, baja. Hay que mantenerla en la franja
    /// buena hasta sacar el pez: si se pasa, se corta la línea; si queda floja mucho rato, el pez se suelta.
    /// </summary>
    public class LancePesca
    {
        public EstadoLance Estado { get; private set; } = EstadoLance.Esperando;
        /// 0 floja, 1 se corta.
        public float Tension { get; private set; } = 0.35f;
        /// 0 recién picó, 1 afuera.
        public float Avance { get; private set; }
        public float ZonaMin { get; }
        public float ZonaMax { get; }
        public float VentanaPica { get; }
        /// El pez tirando fuerte en este momento (para sacudir la caña).
        public bool Tiron => tironT > 0;
        public bool Terminado => Estado >= EstadoLance.Sacado;
        public FichaObjeto Pez { get; }

        readonly float fuerza, velAvance;
        readonly System.Random rnd;
        float espera, t, floja, tironT, proxTiron, fase;

        public const float TENSION_MAX = 1, FLOJA = 0.12f, SEGUNDOS_FLOJA = 1.5f;

        /// <param name="pez">El que va a picar (lo elige quien pesca según el lugar y la hora).</param>
        /// <param name="fuerza">0 a 1: cuánto tira.</param>
        /// <param name="espera">Segundos hasta que pica.</param>
        /// <param name="nivel">Nivel de pesca: más ventana para reaccionar, franja más ancha y se saca más rápido.</param>
        /// <param name="rinde">Partida.FactorTrabajo: con hambre o cansancio la franja se angosta.</param>
        public LancePesca(FichaObjeto pez, float fuerza, float espera, int nivel, float rinde, System.Random rnd)
        {
            Pez = pez;
            this.fuerza = Mathf.Clamp01(fuerza);
            this.espera = Mathf.Max(0.5f, espera);
            this.rnd = rnd ?? new System.Random();
            VentanaPica = Mathf.Min(1.3f, 0.7f + 0.05f * nivel);
            float ancho = (0.42f + Mathf.Min(0.16f, 0.015f * nivel)) * Mathf.Clamp(rinde, 0.5f, 1);
            ZonaMin = 0.55f - ancho / 2;
            ZonaMax = 0.55f + ancho / 2;
            velAvance = (0.2f + 0.02f * Mathf.Min(nivel, 10)) / (0.6f + this.fuerza);
            fase = (float)this.rnd.NextDouble() * 6;
            proxTiron = 0.8f + (float)this.rnd.NextDouble();
        }

        /// <param name="aprieta">La tecla está apretada (recoger línea).</param>
        /// <param name="recienAprieta">Se apretó en este cuadro (para clavar el anzuelo cuando pica).</param>
        public void Paso(float dt, bool aprieta, bool recienAprieta)
        {
            if (Terminado) return;
            t += dt;
            switch (Estado)
            {
                case EstadoLance.Esperando:
                    if (recienAprieta) { Estado = EstadoLance.Temprano; return; }
                    espera -= dt;
                    if (espera <= 0) { Estado = EstadoLance.Pica; t = 0; }
                    break;

                case EstadoLance.Pica:
                    if (recienAprieta) { Estado = EstadoLance.Peleando; t = 0; return; }
                    if (t > VentanaPica) Estado = EstadoLance.SinCarnada;
                    break;

                case EstadoLance.Peleando:
                    Pelear(dt, aprieta);
                    break;
            }
        }

        void Pelear(float dt, bool aprieta)
        {
            // el pez tira con un vaivén y, cada tanto, un tirón fuerte
            proxTiron -= dt;
            if (proxTiron <= 0) { tironT = 0.35f + fuerza * 0.25f; proxTiron = 1f + (float)rnd.NextDouble() * 1.6f; }
            if (tironT > 0) tironT -= dt;
            float tira = fuerza * (0.5f + 0.5f * Mathf.Sin(t * 2.3f + fase)) + (tironT > 0 ? fuerza * 1.2f : 0);

            Tension += (aprieta ? 0.7f + 0.8f * tira : -0.9f + 0.5f * tira) * dt;
            Tension = Mathf.Max(0, Tension);
            if (Tension >= TENSION_MAX) { Estado = EstadoLance.Cortado; return; }

            if (Tension >= ZonaMin && Tension <= ZonaMax) Avance += velAvance * dt;
            else if (Tension < ZonaMin) Avance = Mathf.Max(0, Avance - 0.05f * dt);   // el pez gana línea
            if (Avance >= 1) { Avance = 1; Estado = EstadoLance.Sacado; return; }

            floja = Tension < FLOJA ? floja + dt : 0;
            if (floja > SEGUNDOS_FLOJA) Estado = EstadoLance.Escapo;
        }
    }
}
