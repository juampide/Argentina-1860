#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Argentina1860
{
    /// <summary>
    /// Herramienta de desarrollo (solo en el editor): con F8 en Play arma cada personaje aparte, con luz de frente y fondo
    /// neutro, y guarda hojas de contacto (retratos y cuerpo entero, con el estilo pintado puesto) en D:\Unity\capturas.
    /// Sirve para revisar caras, proporciones y ropa sin tener que recorrer el mundo.
    /// </summary>
    public class GaleriaPersonajes : MonoBehaviour
    {
        static bool corriendo;
        const int POR_HOJA = 12;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Arrancar()
        {
            if (FindAnyObjectByType<GaleriaPersonajes>() == null) new GameObject("Galería de personajes (F8)").AddComponent<GaleriaPersonajes>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame && !corriendo) StartCoroutine(Capturar());
        }

        static string Carpeta()
        {
            string d = Directory.Exists("D:/Unity") ? "D:/Unity/capturas" : Path.Combine(Application.temporaryCachePath, "capturas");
            Directory.CreateDirectory(d);
            return d;
        }

        IEnumerator Capturar()
        {
            corriendo = true;
            var aspectos = new List<(string, Aspecto)>();
            foreach (var guid in AssetDatabase.FindAssets("t:FichaPersonaje"))
            {
                var f = AssetDatabase.LoadAssetAtPath<FichaPersonaje>(AssetDatabase.GUIDToAssetPath(guid));
                if (f != null && f.aspecto != null) aspectos.Add((f.name, f.aspecto));
            }
            if (aspectos.Count == 0) { Debug.LogWarning("[Galería] No hay fichas de personajes."); corriendo = false; yield break; }

            var centro = new Vector3(0, 600, 0);
            var raiz = new GameObject("Galería (temporal)").transform;
            raiz.position = centro;

            // fondo neutro y una luz de frente que gana a la del sol (la principal es la más intensa)
            var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fondo.transform.SetParent(raiz, false);
            fondo.transform.localPosition = new Vector3(0, 1.2f, -1.6f);
            fondo.transform.localRotation = Quaternion.Euler(0, 180, 0);
            fondo.transform.localScale = new Vector3(8, 8, 1);
            Destroy(fondo.GetComponent<Collider>());
            var mf = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mf.SetColor("_BaseColor", new Color(0.46f, 0.44f, 0.40f));
            fondo.GetComponent<Renderer>().sharedMaterial = mf;

            var luzGo = new GameObject("luz galería");
            luzGo.transform.SetParent(raiz, false);
            luzGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, -0.45f, -1f));
            var luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Directional; luz.intensity = 4f; luz.color = new Color(1f, 0.93f, 0.82f); luz.shadows = LightShadows.Soft;

            var camGo = new GameObject("cámara galería");
            camGo.transform.SetParent(raiz, false);
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.46f, 0.44f, 0.40f);
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 60f;
            cam.enabled = false;

            string dir = Carpeta();
            var indice = new StringBuilder();
            int hojas = (aspectos.Count + POR_HOJA - 1) / POR_HOJA;
            for (int h = 0; h < hojas; h++)
            {
                const int RW = 480, CW = 360, CH = 640;
                int n = Mathf.Min(POR_HOJA, aspectos.Count - h * POR_HOJA);
                var hojaRet = new Texture2D(RW * 4, RW * 3, TextureFormat.RGB24, false);
                var hojaCue = new Texture2D(CW * 6, CH * 2, TextureFormat.RGB24, false);
                indice.AppendLine("Hoja " + (h + 1) + ":");
                for (int i = 0; i < n; i++)
                {
                    var (nombre, asp) = aspectos[h * POR_HOJA + i];
                    indice.AppendLine("  " + (i + 1) + ". " + nombre);
                    var modelo = new GameObject("modelo " + nombre).transform;
                    modelo.SetParent(raiz, false);
                    ConstructorPersonaje.Construir(modelo, JsonUtility.FromJson<Aspecto>(JsonUtility.ToJson(asp)), null);
                    yield return null;

                    float esc = Mathf.Max(0.5f, asp.escala);
                    // retrato
                    Foto(cam, centro + new Vector3(0, 1.72f * esc, 1.85f * esc), centro + new Vector3(0, 1.66f * esc, 0), 28, RW, RW, hojaRet, (i % 4) * RW, (2 - i / 4) * RW);
                    // cuerpo entero (6 por fila)
                    Foto(cam, centro + new Vector3(0, 1.0f * esc, 4.4f), centro + new Vector3(0, 1.0f * esc, 0), 30, CW, CH, hojaCue, (i % 6) * CW, (1 - i / 6) * CH);
                    Destroy(modelo.gameObject);
                    yield return null;
                }
                File.WriteAllBytes(Path.Combine(dir, "retratos_" + (h + 1) + ".png"), hojaRet.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(dir, "cuerpos_" + (h + 1) + ".png"), hojaCue.EncodeToPNG());
                Destroy(hojaRet); Destroy(hojaCue);
            }
            // una hoja con todas las expresiones sobre el mismo personaje (el primero sin barba ni bigote)
            {
                var base0 = aspectos[0];
                foreach (var a in aspectos) if (!a.Item2.barba && a.Item2.bigote == Bigote.Ninguno && a.Item2.PeinadoReal != Peinado.Canoso) { base0 = a; break; }
                var nomb = System.Enum.GetNames(typeof(Expresion));
                var hojaEx = new Texture2D(480 * nomb.Length, 480, TextureFormat.RGB24, false);
                indice.AppendLine("Expresiones (de " + base0.Item1 + "): " + string.Join(", ", nomb));
                for (int e = 0; e < nomb.Length; e++)
                {
                    var asp = JsonUtility.FromJson<Aspecto>(JsonUtility.ToJson(base0.Item2));
                    asp.expresion = (Expresion)e;
                    var modelo = new GameObject("expresión " + nomb[e]).transform;
                    modelo.SetParent(raiz, false);
                    ConstructorPersonaje.Construir(modelo, asp, null);
                    yield return null;
                    float esc = Mathf.Max(0.5f, asp.escala);
                    Foto(cam, centro + new Vector3(0, 1.62f * esc, 1.1f * esc), centro + new Vector3(0, 1.6f * esc, 0), 28, 480, 480, hojaEx, e * 480, 0);
                    Destroy(modelo.gameObject);
                    yield return null;
                }
                File.WriteAllBytes(Path.Combine(dir, "expresiones.png"), hojaEx.EncodeToPNG());
                Destroy(hojaEx);
            }
            File.WriteAllText(Path.Combine(dir, "indice.txt"), indice.ToString());
            Debug.Log("[Galería] Guardado en " + dir + " (" + aspectos.Count + " personajes, " + hojas + " hoja/s).");
            Destroy(raiz.gameObject);
            corriendo = false;
        }

        // saca una foto desde `desde` mirando a `mira` y la copia en la hoja, en (px, py) (origen abajo a la izquierda)
        static void Foto(Camera cam, Vector3 desde, Vector3 mira, float fov, int w, int h, Texture2D hoja, int px, int py)
        {
            cam.transform.position = desde;
            cam.transform.rotation = Quaternion.LookRotation(mira - desde);
            cam.fieldOfView = fov;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            RenderTexture.active = prev;
            hoja.SetPixels(px, py, w, h, t.GetPixels());
            Destroy(t);
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
#endif
