using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Argentina1860.Herramientas
{
    /// <summary>
    /// Arma la escena del pago a partir de los datos del prototipo de three.js.
    /// Cada paso se puede correr por separado desde el menú "Argentina 1860 → Pago".
    /// Ojo: volver a generar un paso reemplaza lo que ese paso creó en la escena (terreno, construcciones, personajes).
    /// Las fichas de personajes y los materiales existentes no se pisan.
    /// </summary>
    public static class GeneradorPago
    {
        const string ESCENA = "Assets/Escenas/Pago.unity";
        const string TEX = "Assets/Arte/Texturas";
        const string MAT = "Assets/Arte/Materiales";
        const string COLORES = "Assets/Arte/Materiales/Colores";
        const string MUNDO = "Assets/Mundo/Pago";
        const string CAPAS = "Assets/Mundo/Pago/Capas";
        const string FLORA = "Assets/Mundo/Pago/Flora";

        /// Colisión de cada árbol: una cápsula del grosor del tronco (no de la copa, para no frenarse en una rama).
        /// El terreno la escala con cada árbol. Las matas (cortadera) no llevan: se atraviesan.
        public static readonly Dictionary<string, (float radio, float alto)> TRONCOS = new Dictionary<string, (float, float)>
        {
            { "ombu", (1.3f, 6f) },
            { "tala", (0.3f, 2.6f) },
            { "espinillo", (0.22f, 2.4f) },
            { "coronillo", (0.38f, 3.2f) },
            { "sauce", (0.42f, 3.2f) },
            { "ceibo", (0.35f, 2.8f) },
        };

        /// Les pone la cápsula del tronco a los árboles ya generados, sin tocar la malla, los materiales ni el terreno.
        [MenuItem("Argentina 1860/Pago/Colisión de los troncos")]
        public static void ColisionTroncos()
        {
            foreach (var kv in TRONCOS)
            {
                string ruta = $"{FLORA}/{kv.Key}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null) continue;
                var raiz = PrefabUtility.LoadPrefabContents(ruta);
                var cc = raiz.GetComponent<CapsuleCollider>();
                if (cc == null) cc = raiz.AddComponent<CapsuleCollider>();
                cc.center = new Vector3(0, kv.Value.alto / 2, 0); cc.radius = kv.Value.radio; cc.height = kv.Value.alto; cc.direction = 1;
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            foreach (var t in Object.FindObjectsByType<Terrain>())
            {
                t.terrainData.RefreshPrototypes();
                var col = t.GetComponent<TerrainCollider>();
                if (col != null) { col.enabled = false; col.enabled = true; }   // rearma los troncos del colisionador
            }
            Debug.Log("Pago: troncos con colisión (" + string.Join(", ", TRONCOS.Keys) + ").");
        }
        const string FICHAS = "Assets/Datos/Personajes";
        const string RES = "Assets/Resources";
        static readonly Color HORIZONTE = Conv.Hex("#f6e6c8");

        /// Texturas del prototipo web (ya copiadas en Assets/Arte/Texturas; se usan solo si faltan).
        static string Prototipo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "prototipo-ciudad", "public", "tex"));

        // ---------- menú ----------
        [MenuItem("Argentina 1860/Pago/Generar todo", priority = 0)]
        public static void Todo()
        {
            AbrirEscena();
            Materiales();
            Terreno();
            Flora();
            Construcciones();
            Personajes();
            Escena();
            Guardar();
        }

        [MenuItem("Argentina 1860/Pago/1 · Materiales y texturas", priority = 20)]
        public static void MenuMateriales() { Materiales(); AssetDatabase.SaveAssets(); }

        [MenuItem("Argentina 1860/Pago/2 · Terreno", priority = 21)]
        public static void MenuTerreno() { AbrirEscena(); Materiales(); Terreno(); Guardar(); }

        [MenuItem("Argentina 1860/Pago/3 · Flora", priority = 22)]
        public static void MenuFlora() { AbrirEscena(); Materiales(); Flora(); Guardar(); }

        [MenuItem("Argentina 1860/Pago/4 · Construcciones", priority = 23)]
        public static void MenuConstrucciones() { AbrirEscena(); Materiales(); Construcciones(); Guardar(); }

        [MenuItem("Argentina 1860/Pago/5 · Personajes", priority = 24)]
        public static void MenuPersonajes() { AbrirEscena(); Materiales(); Personajes(); Guardar(); }

        [MenuItem("Argentina 1860/Pago/6 · Jugador, cámara y luz", priority = 25)]
        public static void MenuEscena() { AbrirEscena(); Escena(); Guardar(); }

        static void AbrirEscena()
        {
            if (SceneManager.GetActiveScene().path != ESCENA) EditorSceneManager.OpenScene(ESCENA);
        }

        static void Guardar()
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        static void Carpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;
            var padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            Carpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }

        static Transform NuevaRaiz(string nombre)
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == nombre) Object.DestroyImmediate(go);
            return new GameObject(nombre).transform;
        }

        // ---------- 1 · materiales ----------
        static Texture2D Tex(string nombre) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/{nombre}.jpg") ?? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/{nombre}.png");

        public static void Materiales()
        {
            foreach (var c in new[] { TEX, MAT, COLORES, CAPAS, FLORA, FICHAS, RES }) Carpeta(c);

            string[] nombres = { "revoque_c", "revoque_n", "paja_c", "paja_n", "teja_c", "teja_n", "madera_c", "madera_n", "tierra_c", "tierra_n", "pasto_c" };
            bool copiadas = false;
            foreach (var n in nombres)
            {
                string src = Path.Combine(Prototipo, n + ".jpg"), dst = $"{TEX}/{n}.jpg";
                if (!File.Exists(dst) && File.Exists(src)) { File.Copy(src, dst); copiadas = true; }
            }
            // en el prototipo la paja y la teja se multiplican por un color mayor que 1: se hornea en la textura
            copiadas |= Hornear("paja_c", TEX + "/paja_tinte.png", c => c * (Conv.Hex("#e8cf8a").linear * 2.3f));
            copiadas |= Hornear("teja_c", TEX + "/teja_tinte.png", c => c * (Conv.Hex("#f2d9cc").linear * 1.5f));
            if (copiadas) AssetDatabase.Refresh();

            foreach (var n in nombres.Concat(new[] { "paja_tinte", "teja_tinte" }))
            {
                var ruta = File.Exists($"{TEX}/{n}.jpg") ? $"{TEX}/{n}.jpg" : $"{TEX}/{n}.png";
                if (!(AssetImporter.GetAtPath(ruta) is TextureImporter imp)) continue;
                var tipo = n.EndsWith("_n") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (imp.textureType == tipo && imp.anisoLevel == 8) continue;
                imp.textureType = tipo;
                imp.anisoLevel = 8;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }

            var baseMat = MaterialLit($"{MAT}/Base.mat", Color.white);
            var lista = new List<MaterialesMundo.Entrada>();
            void E(string n, Material m) => lista.Add(new MaterialesMundo.Entrada { nombre = n, material = m });
            E("adobe", MaterialLit($"{MAT}/Adobe.mat", Conv.Hex("#e0bf94"), Tex("revoque_c"), Tex("revoque_n")));
            E("blanco", MaterialLit($"{MAT}/Encalado.mat", Conv.Hex("#f1e8d4"), Tex("revoque_c"), Tex("revoque_n")));
            E("paja", MaterialLit($"{MAT}/Paja.mat", Color.white, Tex("paja_tinte"), Tex("paja_n")));
            E("teja", MaterialLit($"{MAT}/Teja.mat", Color.white, Tex("teja_tinte"), Tex("teja_n")));
            E("madera", MaterialLit($"{MAT}/Madera.mat", Conv.Hex("#d8c4a8"), Tex("madera_c"), Tex("madera_n")));
            E("oscuro", MaterialLit($"{MAT}/Oscuro.mat", Conv.Hex("#2b2118")));
            E("postigo", MaterialLit($"{MAT}/Postigo.mat", Conv.Hex("#3f6049")));
            E("hierro", MaterialLit($"{MAT}/Hierro.mat", Conv.Hex("#26231f"), metal: 0.3f));
            E("piedra", MaterialLit($"{MAT}/Piedra.mat", Conv.Hex("#8d8478")));
            E("vara", MaterialLit($"{MAT}/Vara.mat", Conv.Hex("#7d5f40")));
            var llama = MaterialLit($"{MAT}/Llama.mat", Conv.Hex("#ff9a2a"));
            llama.EnableKeyword("_EMISSION");
            llama.SetColor("_EmissionColor", Conv.Hex("#ff6a10").linear * 1.4f);
            llama.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(llama);
            E("llama", llama);
            var farol = MaterialLit($"{MAT}/Farol.mat", Conv.Hex("#ffd28a"));
            farol.EnableKeyword("_EMISSION");
            farol.SetColor("_EmissionColor", Conv.Hex("#ff9a2a").linear * 1.2f);
            EditorUtility.SetDirty(farol);
            E("farol", farol);
            E("vidrio", MaterialVidrio($"{MAT}/Vidrio.mat"));
            E("cerco", MaterialLit($"{MAT}/Cerco.mat", Conv.Hex("#d8c4a8"), Tex("madera_c"), Tex("madera_n")));

            var ruta2 = $"{RES}/MaterialesMundo.asset";
            var mm = AssetDatabase.LoadAssetAtPath<MaterialesMundo>(ruta2);
            if (mm == null)
            {
                mm = ScriptableObject.CreateInstance<MaterialesMundo>();
                AssetDatabase.CreateAsset(mm, ruta2);
            }
            mm.baseColor = baseMat;
            // se conservan los materiales que el diseño haya cambiado o agregado
            foreach (var e in lista)
            {
                var ya = mm.materiales.Find(x => x.nombre == e.nombre);
                if (ya == null) mm.materiales.Add(e);
                else if (ya.material == null) ya.material = e.material;
            }
            mm.Invalidar();
            EditorUtility.SetDirty(mm);
            AssetDatabase.SaveAssets();
            ConvertirAMundo();
        }

        /// Pasa los materiales del mundo al shader Argentina/Mundo (suciedad y recorte tramado), conservando texturas y colores.
        static void ConvertirAMundo()
        {
            var sh = Shader.Find("Argentina/Mundo");
            if (sh == null) { Debug.LogWarning("Falta el shader Argentina/Mundo"); return; }
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MAT }))
            {
                var ruta = AssetDatabase.GUIDToAssetPath(guid);
                var nombre = Path.GetFileNameWithoutExtension(ruta);
                if (nombre == "Vidrio" || nombre == "Pasto" || nombre == "Silueta") continue;
                var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m.shader == sh) continue;
                bool normal = m.IsKeywordEnabled("_NORMALMAP"), emision = m.IsKeywordEnabled("_EMISSION");
                m.shader = sh;
                if (normal) m.EnableKeyword("_NORMALMAP");
                if (emision) m.EnableKeyword("_EMISSION");
                m.SetFloat("_Cull", 0);
                m.SetFloat("_Suciedad", nombre == "Llama" || nombre == "Farol" ? 0 : 1);
                m.SetFloat("_Grietas", nombre == "Adobe" ? 0.4f : nombre == "Encalado" ? 0.2f : 0);
                // se recortan los árboles (colores de la flora) y los cercos, como en el prototipo; las casas no
                m.SetFloat("_Xray", ruta.StartsWith(COLORES) || nombre == "Cerco" ? 1 : 0);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- silueta del jugador detrás de los edificios ----------
        const string CAPA_PERSONAJE = "Personaje";

        static int AsegurarCapa(string nombre)
        {
            int ya = LayerMask.NameToLayer(nombre);
            if (ya >= 0) return ya;
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var capas = tm.FindProperty("layers");
            for (int i = 8; i < capas.arraySize; i++)
            {
                var c = capas.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(c.stringValue)) { c.stringValue = nombre; tm.ApplyModifiedPropertiesWithoutUndo(); return i; }
            }
            Debug.LogWarning("No hay capas libres para " + nombre);
            return 0;
        }

        static void Silueta(int capa)
        {
            var ruta = $"{MAT}/Silueta.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Silueta" };
                mat.SetColor("_BaseColor", new Color(1f, 0.89f, 0.68f, 0.55f));
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(mat, ruta);
            }
            var datos = new HashSet<ScriptableRendererData>();
            void Juntar(RenderPipelineAsset rp)
            {
                if (!(rp is UniversalRenderPipelineAsset urp)) return;
                var lista = new SerializedObject(urp).FindProperty("m_RendererDataList");
                for (int i = 0; i < lista.arraySize; i++)
                    if (lista.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableRendererData d) datos.Add(d);
            }
            for (int i = 0; i < QualitySettings.count; i++) Juntar(QualitySettings.GetRenderPipelineAssetAt(i));
            Juntar(GraphicsSettings.defaultRenderPipeline);

            foreach (var d in datos)
            {
                if (d.rendererFeatures.Exists(f => f != null && f.name == "Silueta")) continue;
                // 1) marca en el stencil las partes visibles del personaje, 2) dibuja la silueta donde algo lo tapa
                var marca = ScriptableObject.CreateInstance<RenderObjects>();
                marca.name = "Silueta marca";
                marca.settings.Event = RenderPassEvent.AfterRenderingOpaques;
                marca.settings.filterSettings.LayerMask = 1 << capa;
                marca.settings.overrideDepthState = true;
                marca.settings.depthCompareFunction = CompareFunction.LessEqual;
                marca.settings.enableWrite = false;
                marca.settings.stencilSettings.overrideStencilState = true;
                marca.settings.stencilSettings.stencilReference = 1;
                marca.settings.stencilSettings.stencilCompareFunction = CompareFunction.Always;
                marca.settings.stencilSettings.passOperation = StencilOp.Replace;

                var sil = ScriptableObject.CreateInstance<RenderObjects>();
                sil.name = "Silueta";
                sil.settings.Event = RenderPassEvent.AfterRenderingOpaques;
                sil.settings.filterSettings.LayerMask = 1 << capa;
                sil.settings.overrideMaterial = mat;
                sil.settings.overrideDepthState = true;
                sil.settings.depthCompareFunction = CompareFunction.Greater;
                sil.settings.enableWrite = false;
                sil.settings.stencilSettings.overrideStencilState = true;
                sil.settings.stencilSettings.stencilReference = 1;
                sil.settings.stencilSettings.stencilCompareFunction = CompareFunction.NotEqual;

                var so = new SerializedObject(d);
                var feats = so.FindProperty("m_RendererFeatures");
                var mapa = so.FindProperty("m_RendererFeatureMap");
                foreach (var f in new ScriptableRendererFeature[] { marca, sil })
                {
                    AssetDatabase.AddObjectToAsset(f, d);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out _, out long id);
                    feats.arraySize++;
                    feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = f;
                    mapa.arraySize++;
                    mapa.GetArrayElementAtIndex(mapa.arraySize - 1).longValue = id;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(d);
            }
            AssetDatabase.SaveAssets();
        }

        /// Crea o actualiza un material URP Lit mate y de dos caras.
        static Material MaterialLit(string ruta, Color color, Texture2D tex = null, Texture2D normal = null, float metal = 0)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;   // no se pisa lo que el diseño haya ajustado
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = Path.GetFileNameWithoutExtension(ruta) };
            m.SetColor("_BaseColor", color);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Smoothness", 0f);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Cull", 0f);          // dos caras, como el DoubleSide del prototipo
            m.doubleSidedGI = true;
            m.enableInstancing = true;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        /// Vidrio transparente (URP Lit en modo transparente).
        static Material MaterialVidrio(string ruta)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Vidrio" };
            m.SetColor("_BaseColor", new Color(0.81f, 0.9f, 0.91f, 0.22f));
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_Smoothness", 0.9f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        /// Material de color liso guardado como asset (para prefabs, que no pueden usar materiales en memoria).
        static Material MaterialColor(string clave)
        {
            var mm = MaterialesMundo.Instancia;
            var nombrado = mm != null ? mm.Buscar(clave) : null;
            if (nombrado != null) return nombrado;
            var ruta = $"{COLORES}/c_{clave.TrimStart('#')}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            m = new Material(AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/Base.mat")) { name = clave };
            m.SetColor("_BaseColor", Conv.Hex(clave));
            m.SetFloat("_Xray", 1);   // los árboles se recortan cuando tapan al jugador
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        /// Lee una textura del prototipo, le aplica f en espacio lineal y la guarda como PNG. Devuelve true si la creó.
        static bool Hornear(string origen, string destino, Func<Color, Color> f, bool forzar = false)
        {
            if (File.Exists(destino) && !forzar) return false;
            var src = Path.Combine(Prototipo, origen + ".jpg");
            if (!File.Exists(src)) src = $"{TEX}/{origen}.jpg";
            if (!File.Exists(src)) { Debug.LogWarning("Falta la textura " + origen); return false; }
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(src));
            var px = t.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                var l = f(px[i].linear);
                px[i] = new Color(Mathf.Clamp01(l.r), Mathf.Clamp01(l.g), Mathf.Clamp01(l.b), 1).gamma;
            }
            t.SetPixels(px);
            File.WriteAllBytes(destino, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            return true;
        }

        // ---------- 2 · terreno ----------
        static TerrainLayer Capa(string nombre, Texture2D tex, Texture2D normal, float tam)
        {
            var ruta = $"{CAPAS}/{nombre}.terrainlayer";
            var c = AssetDatabase.LoadAssetAtPath<TerrainLayer>(ruta);
            if (c != null) return c;
            c = new TerrainLayer { diffuseTexture = tex, normalMapTexture = normal, tileSize = new Vector2(tam, tam), smoothness = 0, metallic = 0 };
            AssetDatabase.CreateAsset(c, ruta);
            return c;
        }

        public static void Terreno()
        {
            Carpeta(CAPAS);
            // capas del suelo con los mismos tonos que el shader del prototipo
            var pasto = new Color(0.82f, 1f, 0.52f) * 1.12f;
            var paja = new Color(0.56f, 0.47f, 0.27f);
            bool nuevas = Hornear("pasto_c", CAPAS + "/pasto_verde.png", c => c * pasto);
            nuevas |= Hornear("pasto_c", CAPAS + "/pasto_seco.png", c => c * pasto * 0.3f + paja * 0.7f);
            nuevas |= Hornear("tierra_c", CAPAS + "/tierra.png", c => c * 1.35f);
            nuevas |= Hornear("tierra_c", CAPAS + "/agua.png", c => new Color(0.13f, 0.36f, 0.46f));
            if (nuevas) AssetDatabase.Refresh();
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{CAPAS}/{n}.png");
            var capas = new[]
            {
                Capa("Pasto verde", T("pasto_verde"), null, 5),
                Capa("Pasto seco", T("pasto_seco"), null, 5),
                Capa("Tierra", T("tierra"), Tex("tierra_n"), 4),
                Capa("Agua", T("agua"), null, 8),
            };

            var ruta = $"{MUNDO}/TerrenoPago.asset";
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(ruta);
            if (td == null)
            {
                td = new TerrainData();
                AssetDatabase.CreateAsset(td, ruta);
            }
            td.heightmapResolution = 1025;
            td.size = new Vector3(MapaPago.ANCHO, MapaPago.RANGO, MapaPago.ALTO);
            int res = td.heightmapResolution;
            var alturas = new float[res, res];
            MapaPago.Altura(0, 0);   // prepara los campos de distancia antes de repartir el trabajo
            Parallel.For(0, res, i =>
            {
                float zU = -MapaPago.ALTO / 2 + i * MapaPago.ALTO / (res - 1);
                for (int j = 0; j < res; j++)
                {
                    float xU = -MapaPago.ANCHO / 2 + j * MapaPago.ANCHO / (res - 1);
                    alturas[i, j] = Mathf.Clamp01((MapaPago.Altura(xU, -zU) - MapaPago.YMIN) / MapaPago.RANGO);
                }
            });
            td.SetHeights(0, 0, alturas);

            td.terrainLayers = capas;
            td.alphamapResolution = 2048;
            td.baseMapResolution = 1024;
            int ar = td.alphamapResolution;
            var mapa = new float[ar, ar, 4];
            Parallel.For(0, ar, i =>
            {
                float zU = -MapaPago.ALTO / 2 + (i + 0.5f) * MapaPago.ALTO / ar;
                for (int j = 0; j < ar; j++)
                {
                    float xU = -MapaPago.ANCHO / 2 + (j + 0.5f) * MapaPago.ANCHO / ar;
                    var w = MapaPago.PesosSuelo(xU, -zU);
                    float s = Mathf.Max(1e-5f, w.x + w.y + w.z + w.w);
                    for (int k = 0; k < 4; k++) mapa[i, j, k] = w[k] / s;
                }
            });
            td.SetAlphamaps(0, 0, mapa);
            EditorUtility.SetDirty(td);
            AssetDatabase.SaveAssets();

            var raiz = NuevaRaiz("Terreno");
            Object.DestroyImmediate(raiz.gameObject);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terreno";
            go.transform.position = new Vector3(-MapaPago.ANCHO / 2, MapaPago.YMIN, -MapaPago.ALTO / 2);
            go.isStatic = true;
            var t = go.GetComponent<Terrain>();
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null && rp.defaultTerrainMaterial != null) t.materialTemplate = rp.defaultTerrainMaterial;
            t.treeDistance = 2000;
            t.treeBillboardDistance = 2000;
            t.heightmapPixelError = 4;
            t.basemapDistance = 1500;
            t.drawInstanced = true;
            var col = go.GetComponent<TerrainCollider>();
            if (col != null)
            {
                // colisión con los árboles que tienen collider (los ombúes)
                var so = new SerializedObject(col);
                var prop = so.FindProperty("m_EnableTreeColliders");
                if (prop != null) { prop.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        // ---------- 3 · flora ----------
        public static void Flora()
        {
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>($"{MUNDO}/TerrenoPago.asset");
            if (td == null) { Terreno(); td = AssetDatabase.LoadAssetAtPath<TerrainData>($"{MUNDO}/TerrenoPago.asset"); }
            Carpeta(FLORA);

            var protos = new List<TreePrototype>();
            var indice = new Dictionary<string, int>();
            foreach (var esp in Argentina1860.Flora.ESPECIES)
            {
                var nodo = Argentina1860.Flora.Especie(esp);
                var mt = new MallaTres();
                foreach (var p in nodo.piezas) mt.Agregar(p.g, p.mat, p.m);
                var nueva = mt.Construir(esp + "_malla", out var mats);

                var rutaMalla = $"{FLORA}/{esp}_malla.asset";
                var malla = AssetDatabase.LoadAssetAtPath<Mesh>(rutaMalla);
                if (malla == null) { AssetDatabase.CreateAsset(nueva, rutaMalla); malla = nueva; }
                else { EditorUtility.CopySerialized(nueva, malla); Object.DestroyImmediate(nueva); }

                var go = new GameObject(esp);
                go.AddComponent<MeshFilter>().sharedMesh = malla;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = mats.Select(MaterialColor).ToArray();
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                if (TRONCOS.TryGetValue(esp, out var tr))
                {
                    var cc = go.AddComponent<CapsuleCollider>();
                    cc.center = new Vector3(0, tr.alto / 2, 0); cc.radius = tr.radio; cc.height = tr.alto;
                }
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{FLORA}/{esp}.prefab");
                Object.DestroyImmediate(go);
                indice[esp] = protos.Count;
                protos.Add(new TreePrototype { prefab = prefab });
            }
            td.treePrototypes = protos.ToArray();

            var plantas = Argentina1860.Flora.Repartir();
            var arboles = plantas.Select(p => new TreeInstance
            {
                position = new Vector3((p.x + MapaPago.ANCHO / 2) / MapaPago.ANCHO,
                    Mathf.Clamp01((MapaPago.Altura(p.x, p.z) - MapaPago.YMIN) / MapaPago.RANGO),
                    (-p.z + MapaPago.ALTO / 2) / MapaPago.ALTO),
                widthScale = p.s,
                heightScale = p.s,
                rotation = -p.r,
                color = Color.white,
                lightmapColor = Color.white,
                prototypeIndex = indice[p.especie],
            }).ToArray();
            td.SetTreeInstances(arboles, true);
            td.RefreshPrototypes();
            EditorUtility.SetDirty(td);
            Debug.Log($"[Pago] Flora: {arboles.Length} plantas de {protos.Count} especies.");
        }

        // ---------- 4 · construcciones ----------
        static Vector2 K(float kx, float ky) => MapaPago.K(kx, ky);

        static T Obj<T>(Transform padre, string nombre, float x, float z, float rot = 0) where T : Component
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = MapaPago.AUnity(x, z);
            go.transform.rotation = Conv.Yaw(rot);
            return go.AddComponent<T>();
        }

        static Casa CasaEn(Transform padre, string nombre, float x, float z, float rot = 0, float w = 7, float l = 5, float h = 2.6f, float rise = 1.6f,
            Casa.Pared pared = Casa.Pared.Adobe, Casa.TipoTecho techo = Casa.TipoTecho.Paja, bool galeria = false, bool ruina = false, bool puerta = true)
        {
            var c = Obj<Casa>(padre, nombre, x, z, rot);
            c.ancho = w; c.largo = l; c.alto = h; c.caida = rise; c.pared = pared; c.techo = techo; c.galeria = galeria; c.ruina = ruina; c.puerta = puerta;
            return c;
        }

        /// Cerco a partir de puntos en metros del prototipo.
        static Cerco CercoEn(Transform padre, string nombre, List<Vector2> pts, int hueco = -1, float alto = 1.4f, float paso = 3)
        {
            var centro = pts.Aggregate(Vector2.zero, (a, b) => a + b) / pts.Count;
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(centro.x, 0, -centro.y);
            var c = go.AddComponent<Cerco>();
            c.puntos = pts.Select(p => new Vector2(p.x - centro.x, -(p.y - centro.y))).ToList();
            c.hueco = hueco; c.alto = alto; c.paso = paso;
            return c;
        }

        static List<Vector2> Rect4(float cx, float cz, float w, float l) => new List<Vector2>
        {
            new Vector2(cx - w / 2, cz - l / 2), new Vector2(cx + w / 2, cz - l / 2), new Vector2(cx + w / 2, cz + l / 2), new Vector2(cx - w / 2, cz + l / 2),
        };

        static Transform Grupo(Transform raiz, string nombre)
        {
            var g = new GameObject(nombre).transform;
            g.SetParent(raiz, false);
            return g;
        }

        public static void Construcciones()
        {
            var raiz = NuevaRaiz("Construcciones");

            // Estancia La Soledad
            {
                var g = Grupo(raiz, "Estancia La Soledad");
                var e = K(0.75f, 0.75f);
                CasaEn(g, "Casa principal", e.x, e.y, 0, 16, 8, 3.4f, 2.4f, Casa.Pared.Blanco, Casa.TipoTecho.Teja, galeria: true);
                CasaEn(g, "Galpón", e.x + 24, e.y - 8, 0.1f, 14, 7, 3.2f, 2.2f, puerta: false);
                CasaEn(g, "Cocina", e.x - 22, e.y - 14, -0.2f, 6, 5, 2.5f, 1.5f);
                CercoEn(g, "Corral de doma", Rect4(e.x - 26, e.y + 22, 18, 18), 0);
                CercoEn(g, "Corral grande", Rect4(e.x + 12, e.y + 34, 32, 22), 0);
                Obj<Palenque>(g, "Palenque", e.x - 6, e.y + 12, 0.1f);
                Obj<Pozo>(g, "Pozo", e.x + 14, e.y + 8);
                Obj<Fogon>(g, "Fogón", e.x - 14, e.y + 10);
                Vector2 p0 = K(0.17f, 0.08f), p1 = K(0.86f, 0.42f);
                CercoEn(g, "Potreros", new List<Vector2> { new Vector2(p0.x, p0.y), new Vector2(p1.x, p0.y), new Vector2(p1.x, p1.y), new Vector2(p0.x, p1.y) }, 2, 1.3f, 6);
                var cartel = Obj<Cartel>(g, "Cartel La Soledad", e.x - 4, e.y + 38);
                cartel.transform.position += Vector3.up * 2.2f;
                cartel.texto = "LA SOLEDAD"; cartel.ancho = 3.4f;
            }

            // Ranchos de peones (inicio del jugador)
            {
                var g = Grupo(raiz, "Ranchos de peones");
                var r = K(0.59f, 0.9f);
                Obj<Habitacion>(g, "Rancho de peones", r.x - 8, r.y, 0.15f);
                CasaEn(g, "Rancho", r.x + 9, r.y + 4, -0.1f, 6, 4.5f);
                Obj<Fogon>(g, "Fogón", r.x, r.y + 8);
                Obj<Palenque>(g, "Palenque", r.x - 2, r.y + 14, 0.2f).postes = 3;
            }

            // Rancho abandonado (será el rancho propio, etapa 4)
            {
                var g = Grupo(raiz, "Rancho abandonado");
                var a = K(1.0f, 1.0f);
                CasaEn(g, "Rancho en ruinas", a.x, a.y, 0.3f, 6, 4.5f, 2.4f, 1.4f, ruina: true);
            }

            // Posta y pulpería
            {
                var g = Grupo(raiz, "Posta y pulpería");
                var p = K(2.15f, 1.25f);
                Obj<Pulperia>(g, "Pulpería", p.x, p.y - 12);
                CercoEn(g, "Corral de remuda", Rect4(p.x - 6, p.y - 40, 26, 16), 2);
                CasaEn(g, "Rancho", p.x + 34, p.y - 20, 0.2f, 7, 5);
                CasaEn(g, "Rancho", p.x + 40, p.y + 6, -0.15f, 6, 4.5f);
                CasaEn(g, "Rancho", p.x - 36, p.y + 12, 0.1f, 7, 5);
                Obj<Palenque>(g, "Palenque", p.x - 4, p.y + 6, 0.05f).postes = 5;
                Obj<Pozo>(g, "Pozo", p.x + 22, p.y - 2);
                Obj<Fogon>(g, "Fogón", p.x + 14, p.y + 8);
            }

            // Fortín de la frontera
            {
                var g = Grupo(raiz, "Fortín");
                var f = K(0.12f, 1.15f);
                CercoEn(g, "Empalizada", Rect4(f.x, f.y, 30, 30), 2, 3, 1.6f);
                CasaEn(g, "Cuadra", f.x - 7, f.y - 6, 0, 8, 5);
                CasaEn(g, "Comandancia", f.x + 7, f.y + 5, 0.1f, 7, 5);
                Obj<Fogon>(g, "Fogón", f.x, f.y + 1);
                Obj<Mastil>(g, "Mástil", f.x + 4, f.y - 8);
            }

            // Puesto del baqueano (sobre la loma)
            {
                var g = Grupo(raiz, "Puesto del baqueano");
                var b = K(0.55f, 1.85f);
                CasaEn(g, "Puesto", b.x, b.y, 0.4f, 6, 4.5f);
                CercoEn(g, "Corral", Rect4(b.x + 14, b.y + 4, 12, 9), 0);
                Obj<Fogon>(g, "Fogón", b.x - 8, b.y + 6);
            }

            // Rancho de Casiana (curandera), junto al arroyo
            {
                var g = Grupo(raiz, "Rancho de Casiana");
                var c = K(1.17f, 0.5f);
                CasaEn(g, "Rancho", c.x, c.y, 0.5f, 6, 4.5f);
                Obj<Fogon>(g, "Fogón", c.x - 6, c.y + 5);
            }
        }

        // ---------- 5 · personajes ----------
        public static void Personajes()
        {
            Carpeta(FICHAS);
            var raiz = NuevaRaiz("Personajes");
            foreach (var d in DatosPago.Elenco())
            {
                var ruta = $"{FICHAS}/{d.id}.asset";
                var ficha = AssetDatabase.LoadAssetAtPath<FichaPersonaje>(ruta);
                if (ficha == null)   // las fichas existentes no se pisan: son del diseño
                {
                    ficha = ScriptableObject.CreateInstance<FichaPersonaje>();
                    ficha.id = d.id; ficha.nombre = d.nombre; ficha.lugar = d.lugar; ficha.aspecto = d.aspecto; ficha.lineas = d.lineas;
                    AssetDatabase.CreateAsset(ficha, ruta);
                }
                var go = new GameObject(d.nombre);
                go.transform.SetParent(raiz, false);
                go.transform.position = MapaPago.AUnity(d.xz);
                go.layer = AsegurarCapa(CAPA_PERSONAJE);   // también se ven como silueta detrás de los edificios
                go.AddComponent<ModeloPersonaje>().ficha = ficha;
                go.AddComponent<NPC>();
                var col = go.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0); col.radius = 0.4f; col.height = 1.8f;
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- 6 · jugador, cámara, luz y postproceso ----------
        public static void Escena()
        {
            Carpeta(MUNDO);
            // jugador
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects()) if (go.name == "Jugador" || go.name == "Juego") Object.DestroyImmediate(go);
            var inicio = K(0.59f, 0.9f) + new Vector2(3, 12);
            var jugadorGo = new GameObject("Jugador");
            jugadorGo.transform.position = MapaPago.AUnity(inicio) + Vector3.up * 0.05f;
            var cc = jugadorGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.9f, 0); cc.radius = 0.42f; cc.height = 1.8f; cc.stepOffset = 0.35f; cc.slopeLimit = 50; cc.skinWidth = 0.05f;
            jugadorGo.AddComponent<ModeloPersonaje>().aspecto = DatosPago.Peon();
            int capa = AsegurarCapa(CAPA_PERSONAJE);
            jugadorGo.layer = capa;
            Silueta(capa);
            var control = jugadorGo.AddComponent<ControlJugador>();

            // cámara
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.gameObject.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 13;
            cam.nearClipPlane = 1;
            cam.farClipPlane = 700;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HORIZONTE;
            var iso = cam.GetComponent<CamaraIsometrica>();
            if (iso == null) iso = cam.gameObject.AddComponent<CamaraIsometrica>();
            iso.objetivo = jugadorGo.transform;
            control.camara = iso;
            var datosCam = cam.GetUniversalAdditionalCameraData();
            datosCam.renderPostProcessing = true;
            datosCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            // sol: 34° sobre el horizonte, desde el sudoeste del prototipo
            var sol = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
            if (sol == null) sol = new GameObject("Sol").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.name = "Sol";
            sol.transform.rotation = Quaternion.LookRotation(new Vector3(0.679f, -0.559f, -0.476f));
            sol.color = Conv.Hex("#ffe2b4");
            sol.intensity = 2.0f;
            sol.shadows = LightShadows.Soft;

            RenderSettings.sun = sol;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Conv.Hex("#cfe0f5");
            RenderSettings.ambientEquatorColor = Conv.Hex("#d6c7ad");
            RenderSettings.ambientGroundColor = Conv.Hex("#b99d78");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = HORIZONTE;
            RenderSettings.fogStartDistance = 170;
            RenderSettings.fogEndDistance = 1800;

            // postproceso: ACES y colores apagados y polvorientos (la corrección de color de suciedad.js)
            var rutaPerfil = $"{MUNDO}/PerfilPago.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(rutaPerfil);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, rutaPerfil);
                var tm = perfil.Add<Tonemapping>(true);
                tm.mode.Override(TonemappingMode.ACES);
                AssetDatabase.AddObjectToAsset(tm, perfil);
                var ca = perfil.Add<ColorAdjustments>(true);
                ca.postExposure.Override(-0.15f);
                ca.contrast.Override(8f);
                ca.saturation.Override(-26f);
                ca.colorFilter.Override(new Color(1.04f, 0.99f, 0.90f));
                AssetDatabase.AddObjectToAsset(ca, perfil);
                EditorUtility.SetDirty(perfil);
            }
            var volGo = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == "PostProceso");
            if (volGo == null) volGo = new GameObject("PostProceso");
            var vol = volGo.GetComponent<Volume>();
            if (vol == null) vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = perfil;

            // sombras hasta 160 m (la cámara isométrica está a 70 m del jugador), con bias para evitar rayas en el pasto
            void Sombras(UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = 160;
                urp.shadowCascadeCount = 4;
                urp.shadowDepthBias = 2f;
                urp.shadowNormalBias = 1.5f;
                EditorUtility.SetDirty(urp);
            }
            for (int i = 0; i < QualitySettings.count; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset urp) Sombras(urp);
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpDef) Sombras(urpDef);

            var juegoGo = new GameObject("Juego");
            var juego = juegoGo.AddComponent<JuegoPago>();
            juego.jugador = control;
            juego.camara = iso;
            var fauna = juegoGo.AddComponent<Fauna>();
            fauna.juego = juego;
            juego.fauna = fauna;
            var pasto = juegoGo.AddComponent<Pasto>();
            pasto.jugador = jugadorGo.transform;
            var rutaPasto = $"{MAT}/Pasto.mat";
            var matPasto = AssetDatabase.LoadAssetAtPath<Material>(rutaPasto);
            if (matPasto == null)
            {
                matPasto = new Material(Shader.Find("Argentina/Pasto")) { enableInstancing = true, name = "Pasto" };
                AssetDatabase.CreateAsset(matPasto, rutaPasto);
            }
            pasto.material = matPasto;
        }
    }
}
