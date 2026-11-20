using UnityEngine.InputSystem;

namespace Argentina1860
{
    /// <summary>
    /// Teclas de prueba en la fila de números (las de función, F1 a F12, chocan con el brillo, el volumen y otros
    /// atajos de muchos teclados). 1 a 8: viaje rápido · 9: cambiar de mundo · 0: cuadros por segundo.
    /// También valen las del teclado numérico.
    /// </summary>
    public static class Teclas
    {
        /// Se apretó el número <paramref name="n"/> (0 a 9) en este cuadro.
        public static bool Numero(int n)
        {
            var kb = Keyboard.current;
            if (kb == null || n < 0 || n > 9) return false;
            return kb[Key.Digit1 + ((n + 9) % 10)].wasPressedThisFrame || kb[Key.Numpad0 + n].wasPressedThisFrame;
        }

        public const int CAMBIO_DE_MUNDO = 9, RENDIMIENTO = 0;
    }
}
