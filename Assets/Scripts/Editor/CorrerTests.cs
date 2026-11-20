using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Menú Argentina 1860 → Tests: corre los tests y deja el resumen en Logs/tests.txt (también cuando se corren
    /// desde la ventana Test Runner). Sirve para que los agentes corran los tests por el MCP y lean el resultado.
    /// </summary>
    [InitializeOnLoad]
    static class CorrerTests
    {
        public const string ARCHIVO = "Logs/tests.txt";

        static CorrerTests()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Resultados());
        }

        [MenuItem("Argentina 1860/Tests/Correr tests de lógica (rápidos)")]
        static void Logica() => Correr(TestMode.EditMode);

        [MenuItem("Argentina 1860/Tests/Correr tests del juego (cargan las escenas)")]
        static void Juego() => Correr(TestMode.PlayMode);

        static void Correr(TestMode modo)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Salir de Play antes de correr los tests."); return; }
            if (modo == TestMode.PlayMode && UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("Guardar la escena antes de correr los tests del juego.");
                return;
            }
            Escribir($"Corriendo tests ({modo})...\n");
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter { testMode = modo }));
        }

        static void Escribir(string s)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ARCHIVO));
            File.WriteAllText(ARCHIVO, s);
        }

        class Resultados : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor r)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{(r.FailCount == 0 ? "OK" : "FALLAN")}: {r.PassCount} pasan, {r.FailCount} fallan, {r.SkipCount} saltados ({r.Duration:F1} s)");
                Fallas(r, sb);
                Escribir(sb.ToString());
                if (r.FailCount == 0) Debug.Log("Tests: " + sb.ToString().Trim());
                else Debug.LogWarning("Tests: " + sb);
            }

            static void Fallas(ITestResultAdaptor r, StringBuilder sb)
            {
                if (!r.HasChildren)
                {
                    if (r.TestStatus == TestStatus.Failed) sb.AppendLine($"\n✗ {r.FullName}\n  {r.Message?.Trim()}");
                    return;
                }
                foreach (var c in r.Children) Fallas(c, sb);
            }
        }
    }
}
