using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Arma la escena de Buenos Aires (menú "Argentina 1860 → Ciudad"): materiales y texturas de la ciudad, fichas de los
    /// personajes de la plaza y la escena con la ciudad, el jugador, la cámara, la luz y el juego. Las fichas existentes no se pisan.
    /// </summary>
    public static class GeneradorCiudad
    {
        const string ESCENA = "Assets/Escenas/BuenosAires.unity", ESCENA_PAGO = "Assets/Escenas/Pago.unity";
        const string TEX = "Assets/Arte/Texturas", MAT = "Assets/Arte/Materiales/Ciudad", DATOS = "Assets/Datos/Ciudad", FICHAS = "Assets/Datos/Personajes/Ciudad";

        [MenuItem("Argentina 1860/Ciudad/Generar Buenos Aires", priority = 40)]
        public static void Todo()
        {
            // no se guarda nada de la escena abierta (el pago es del diseño): si tiene cambios sin guardar, no se sigue
            if (SceneManager.GetActiveScene().isDirty) { Debug.LogWarning("[Ciudad] Guardá o descartá los cambios de la escena abierta antes de generar Buenos Aires."); return; }
            Materiales();
            var fichas = Fichas();
            Escena(fichas);
            BuildSettings();
        }

        static void Carpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;
            var padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            Carpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }

        static Texture2D Tex(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/{n}.jpg") ?? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/{n}.png");

        // ---------- materiales ----------
        public static void Materiales()
        {
            Carpeta(MAT);
            foreach (var n in new[] { "calle_n", "vereda_n", "ladrillo_n", "sillar_n", "travertino_n" })
                if (AssetImporter.GetAtPath($"{TEX}/{n}.jpg") is TextureImporter imp && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
            if (AssetImporter.GetAtPath($"{DATOS}/suelo.png") is TextureImporter m && !m.isReadable)
            {
                m.isReadable = true; m.mipmapEnabled = false; m.textureCompression = TextureImporterCompression.Uncompressed; m.filterMode = FilterMode.Point; m.SaveAndReimport();
            }
            // texturas pintadas por código: atlas de vanos y arcos de recova
            GuardarPng(TexturasCiudad.Atlas(), $"{MAT}/vanos.png");
            GuardarPng(TexturasCiudad.Arcada(1), $"{MAT}/arcada1.png");
            GuardarPng(TexturasCiudad.Arcada(2), $"{MAT}/arcada2.png");
            AssetDatabase.Refresh();
            foreach (var p in new[] { "vanos", "arcada1", "arcada2" })
                if (AssetImporter.GetAtPath($"{MAT}/{p}.png") is TextureImporter ti && (!ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Repeat))
                { ti.alphaIsTransparency = true; ti.wrapMode = p == "vanos" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.SaveAndReimport(); }

            var lista = new List<(string, Material)>
            {
                ("revoque", Mundo("Revoque", Color.white, Tex("revoque_c"), Tex("revoque_n"), 0.15f)),
                ("ladrillo", Mundo("Ladrillo", Color.white, Tex("ladrillo_c"), Tex("ladrillo_n"))),
                ("sillar", Mundo("Sillar", Color.white, Tex("sillar_c"), Tex("sillar_n"))),
                ("travertino", Mundo("Travertino", Color.white, Tex("travertino_c"), Tex("travertino_n"))),
                ("vereda", Mundo("Vereda", Color.white, Tex("vereda_c"), Tex("vereda_n"))),
                ("calle", Mundo("Calle", Conv.Aclara(Color.white, 1.15f), Tex("calle_c"), Tex("calle_n"))),
                ("patio", Mundo("Patio", Color.white, Tex("patio_c"), null)),
                ("suelo_ciudad", Mundo("Suelo", Conv.Hex("#d9c9a8"), Tex("tierra_c"), Tex("tierra_n"))),
                ("agua_ciudad", Mundo("Agua", Conv.Hex("#66a1b5"), null, null, 0, 0.6f)),
                ("vanos", Recorte("Vanos", AssetDatabase.LoadAssetAtPath<Texture2D>($"{MAT}/vanos.png"))),
                ("arco1", Recorte("Arcada1", AssetDatabase.LoadAssetAtPath<Texture2D>($"{MAT}/arcada1.png"))),
                ("arco2", Recorte("Arcada2", AssetDatabase.LoadAssetAtPath<Texture2D>($"{MAT}/arcada2.png"))),
                ("haz", Haz()),
            };
            var mm = AssetDatabase.LoadAssetAtPath<MaterialesMundo>("Assets/Resources/MaterialesMundo.asset");
            foreach (var (n, mat) in lista)
            {
                var ya = mm.materiales.Find(x => x.nombre == n);
                if (ya == null) mm.materiales.Add(new MaterialesMundo.Entrada { nombre = n, material = mat });
                else if (ya.material == null) ya.material = mat;
            }
            mm.Invalidar();
            EditorUtility.SetDirty(mm);
            AssetDatabase.SaveAssets();
        }

        static void GuardarPng(Texture2D t, string ruta)
        {
            if (!File.Exists(ruta)) File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        static Material Mundo(string nombre, Color color, Texture2D tex, Texture2D normal, float suciedad = 1, float suavidad = 0)
        {
            var ruta = $"{MAT}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            m = new Material(Shader.Find("Argentina/Mundo") ?? Shader.Find("Universal Render Pipeline/Lit")) { name = nombre, enableInstancing = true };
            m.SetColor("_BaseColor", color);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Cull", 0); m.SetFloat("_Suciedad", suciedad); m.SetFloat("_Smoothness", suavidad);
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        /// URP Lit con recorte por transparencia (vanos y arcadas) y dos caras.
        static Material Recorte(string nombre, Texture2D tex)
        {
            var ruta = $"{MAT}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre, enableInstancing = true };
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0); m.SetFloat("_Smoothness", 0.1f);
            m.renderQueue = (int)RenderQueue.AlphaTest;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        static Material Haz()
        {
            var ruta = $"{MAT}/Haz.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Haz" };
            m.SetColor("_BaseColor", new Color(1f, 0.48f, 0.23f, 0.3f));
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_Cull", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
            m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        // ---------- personajes de la plaza ----------
        class Def { public string id, nombre, lugar; public float px, py; public Aspecto a; public string[] l; }

        static List<FichaPersonaje> Fichas()
        {
            Carpeta(FICHAS);
            Aspecto A(Estilo e, string ropa = null, string ropa2 = null, string chaleco = null, string pantalon = null, string poncho = null, Sombrero sb = Sombrero.Ninguno, string cs = null,
                bool barba = false, Bigote bi = Bigote.Ninguno, string pelo = null, Peinado pe = Peinado.SegunEstilo, string piel = null, Accesorio ac = Accesorio.Ninguno, float esc = 1) =>
                DatosPago.A(e, ropa, ropa2, chaleco, pantalon, poncho, sb, cs, barba, bi, pelo, pe, piel, false, ac, esc);
            var defs = new[]
            {
                new Def { id = "anselmo", nombre = "Don Anselmo", lugar = "Plaza de la Victoria (vende diarios)", px = 10335, py = 9530,
                    a = A(Estilo.Pregonero, "#8a5a3a", "#f1ead8", pantalon: "#7a7468", sb: Sombrero.Gorra, cs: "#6f7a68", bi: Bigote.Manubrio, pelo: "#5a3d26", pe: Peinado.Corto),
                    l = new[] { "Buen día, forastero. Soy Anselmo, vendo diarios y noticias en la Plaza de la Victoria. Acá se entera uno de todo antes que nadie.", "Mire, justo necesitaba a alguien de confianza. Esta carta es para Doña Remedios, la pulpera. Es urgente y a mí no me da el tiempo.", "Se la encuentra hacia el oeste, cerca de una de las iglesias. Pregunte, que todos la conocen." } },
                new Def { id = "remedios", nombre = "Doña Remedios", lugar = "Pulpería del oeste", px = 10024, py = 9380,
                    a = A(Estilo.Vestido, "#7a3f8f", "#f1e3c4", pelo: "#2a1a12"),
                    l = new[] { "En la pulpería hay de todo: yerba, caña, tabaco... y chismes, que son gratis." } },
                new Def { id = "sacristan", nombre = "El sacristán", lugar = "Iglesia del oeste", px = 9575, py = 9640,
                    a = A(Estilo.Sotana, sb: Sombrero.Bonete, pelo: "#bdbdb8", piel: "#f0c19a"),
                    l = new[] { "Vaya con Dios." } },
                new Def { id = "paseante", nombre = "Un paisano", lugar = "Plaza de la Victoria", px = 10450, py = 9420,
                    a = A(Estilo.Poncho, poncho: "#b53a2a", sb: Sombrero.Ala, cs: "#3b2f27", barba: true, pelo: "#2a1c14", piel: "#d9a27a"),
                    l = new[] { "Dicen que el río llegaba hasta acá no hace mucho. Fíjese en la barranca: la ciudad se levantó sobre lo que el agua dejó.", "Y no vaya muy lejos hacia las afueras, que las calles se acaban pronto." } },
                new Def { id = "soldado", nombre = "El centinela", lugar = "Frente al Cabildo", px = 10380, py = 9400,
                    a = A(Estilo.Levita, "#2c3e6b", chaleco: "#c9a227", pantalon: "#b3342b", sb: Sombrero.Kepi, bi: Bigote.Manubrio, pelo: "#3a2a1c", pe: Peinado.Corto, ac: Accesorio.Fusil),
                    l = new[] { "Alto ahí, ciudadano... ah, es usted. Circule nomás, circule.", "El Cabildo ya no es lo que era, pero la guardia es la guardia. Cuando cae el sol, esta plaza se pone brava." } },
                new Def { id = "aguatero", nombre = "El aguatero", lugar = "Plaza de la Victoria", px = 10424, py = 9529,
                    a = A(Estilo.Poncho, poncho: "#3f7a8a", sb: Sombrero.Ala, cs: "#b89a6a", barba: true, pelo: "#5a4a3a", piel: "#d9a27a", ac: Accesorio.Barril),
                    l = new[] { "¡Agua fresca del río, a un real el balde!", "Cada año tengo que bajar más lejos con la carreta: el río se aleja y la ciudad se va comiendo la costa." } },
                new Def { id = "vendedora", nombre = "La pastelera", lugar = "Plaza de la Victoria", px = 10276, py = 9567,
                    a = A(Estilo.Vestido, "#d18a3a", "#f6efdc", pelo: "#3a2415", ac: Accesorio.Canasta),
                    l = new[] { "¡Pastelitos calientes, criollos y de membrillo!", "Si busca a Doña Remedios, vive hacia el oeste, donde la calle se pone más tranquila. Llévele pasteles, que le encantan." } },
                new Def { id = "lustra", nombre = "El lustrabotas", lugar = "Plaza de la Victoria", px = 10298, py = 9441,
                    a = A(Estilo.Pregonero, "#7a8a5a", "#efe6d0", pantalon: "#8a7a60", sb: Sombrero.Gorra, cs: "#5a4a3a", pelo: "#4a3220", pe: Peinado.Puas, ac: Accesorio.Cajon, esc: 0.72f),
                    l = new[] { "¿Le lustro las botas, caballero? Con medio real quedan como espejos.", "Desde acá se ve todo: quién entra al Cabildo, quién sale de misa... y quién no paga." } },
            };
            var fichas = new List<FichaPersonaje>();
            foreach (var d in defs)
            {
                var ruta = $"{FICHAS}/{d.id}.asset";
                var f = AssetDatabase.LoadAssetAtPath<FichaPersonaje>(ruta);
                if (f == null)
                {
                    f = ScriptableObject.CreateInstance<FichaPersonaje>();
                    f.id = d.id; f.nombre = d.nombre; f.lugar = d.lugar; f.aspecto = d.a; f.lineas = d.l;
                    AssetDatabase.CreateAsset(f, ruta);
                }
                fichas.Add(f);
                posiciones[d.id] = new Vector2((d.px - 10335) * DatosCiudad.ESC, (d.py - 9485) * DatosCiudad.ESC);
            }
            AssetDatabase.SaveAssets();
            return fichas;
        }

        static readonly Dictionary<string, Vector2> posiciones = new Dictionary<string, Vector2>();

        // ---------- escena ----------
        static void Escena(List<FichaPersonaje> fichas)
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            int capa = LayerMask.NameToLayer("Personaje");

            var ciudadGo = new GameObject("Ciudad");
            var ciudad = ciudadGo.AddComponent<Ciudad>();
            ciudad.json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DATOS}/ciudad.json");
            ciudad.pak = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DATOS}/ciudad_pak.bytes");
            ciudad.mascaraAgua = AssetDatabase.LoadAssetAtPath<Texture2D>($"{DATOS}/suelo.png");
            ciudadGo.isStatic = true;

            var personajes = new GameObject("Personajes").transform;
            foreach (var f in fichas)
            {
                var p = posiciones[f.id];
                var go = new GameObject(f.nombre);
                go.transform.SetParent(personajes, false);
                go.transform.position = new Vector3(p.x, 0, -p.y);
                if (capa >= 0) go.layer = capa;
                go.AddComponent<ModeloPersonaje>().ficha = f;
                go.AddComponent<NPC>();
                var col = go.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0); col.radius = 0.4f; col.height = 1.8f;
            }

            // el forastero recién llegado a la ciudad
            var jug = new GameObject("Jugador");
            jug.transform.position = new Vector3(0, 0.1f, -8);
            if (capa >= 0) jug.layer = capa;
            var cc = jug.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.9f, 0); cc.radius = 0.42f; cc.height = 1.8f; cc.stepOffset = 0.35f; cc.slopeLimit = 50; cc.skinWidth = 0.05f;
            var asp = DatosPago.A(Estilo.Levita, "#2f4a7a", chaleco: "#e0b84e", pantalon: "#9a948a", sombrero: Sombrero.Copa, bigote: Bigote.Fino, pelo: "#4a3220", peinado: Peinado.Corto, baston: true);
            jug.AddComponent<ModeloPersonaje>().aspecto = asp;
            var control = jug.AddComponent<ControlJugador>();

            var cam = Camera.main;
            cam.orthographic = true; cam.orthographicSize = 13; cam.nearClipPlane = 1; cam.farClipPlane = 700;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Conv.Hex("#f6e6c8");
            var iso = cam.gameObject.AddComponent<CamaraIsometrica>();
            iso.objetivo = jug.transform;
            control.camara = iso;
            var datosCam = cam.GetUniversalAdditionalCameraData();
            datosCam.renderPostProcessing = true;
            datosCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var sol = Object.FindObjectsByType<Light>().First(l => l.type == LightType.Directional);
            sol.name = "Sol";
            sol.transform.rotation = Quaternion.LookRotation(new Vector3(0.679f, -0.559f, -0.476f));
            sol.color = Conv.Hex("#ffe2b4"); sol.intensity = 2.0f; sol.shadows = LightShadows.Soft;
            RenderSettings.sun = sol;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Conv.Hex("#cfe0f5");
            RenderSettings.ambientEquatorColor = Conv.Hex("#d6c7ad");
            RenderSettings.ambientGroundColor = Conv.Hex("#b99d78");
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Conv.Hex("#f6e6c8");
            RenderSettings.fogStartDistance = 170; RenderSettings.fogEndDistance = 1800;

            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Mundo/Pago/PerfilPago.asset");
            if (perfil != null) { var vol = new GameObject("PostProceso").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = perfil; }

            var juegoGo = new GameObject("Juego");
            var juego = juegoGo.AddComponent<JuegoCiudad>();
            juego.jugador = control; juego.camara = iso; juego.ciudad = ciudad;
            var arboles = juegoGo.AddComponent<ArbolesCiudad>();
            arboles.ciudad = ciudad; arboles.jugador = jug.transform;
            var rutaMat = $"{MAT}/Arboles.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (mat == null) { mat = new Material(Shader.Find("Argentina/Instanciado")) { enableInstancing = true, name = "Arboles" }; AssetDatabase.CreateAsset(mat, rutaMat); }
            arboles.material = mat;

            EditorSceneManager.SaveScene(escena, ESCENA);
        }

        static void BuildSettings()
        {
            var escenas = EditorBuildSettings.scenes.ToList();
            foreach (var r in new[] { ESCENA_PAGO, ESCENA })
                if (!escenas.Exists(s => s.path == r)) escenas.Add(new EditorBuildSettingsScene(r, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
