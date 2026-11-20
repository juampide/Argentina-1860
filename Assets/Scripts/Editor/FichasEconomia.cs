using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Crea las fichas de objeto y de receta con las que arranca la economía del pago (menú
    /// Argentina 1860 → Economía). Solo crea las que faltan: las que ya existen no se tocan, así que se puede
    /// correr después de que diseño las haya editado.
    /// </summary>
    public static class FichasEconomia
    {
        const string OBJETOS = "Assets/Resources/" + Catalogo.CARPETA_OBJETOS, RECETAS = "Assets/Resources/" + Catalogo.CARPETA_RECETAS;

        [MenuItem("Argentina 1860/Economía/Crear las fichas que falten")]
        public static void Crear()
        {
            Directory.CreateDirectory(OBJETOS);
            Directory.CreateDirectory(RECETAS);
            int nuevas = 0;

            // ---------- mercadería de la pulpería (los ids de siempre) ----------
            Obj("yerba", "Yerba", TipoObjeto.Mercaderia, 1, 6, vende: true, compra: true, noticias: true, desc: "Yerba paraguaya en un atado de cuero. Sin mate no arranca el día.");
            Obj("tabaco", "Tabaco", TipoObjeto.Mercaderia, 0.5f, 8, vende: true, compra: true, noticias: true, desc: "Tabaco negro en rollo, para picar y armar.");
            Obj("cana", "Caña", TipoObjeto.Mercaderia, 1, 11, vende: true, compra: true, noticias: true, desc: "Una botella de caña. Calienta en invierno y suelta la lengua.");
            Obj("tela", "Tela", TipoObjeto.Mercaderia, 2, 15, vende: true, compra: true, noticias: true, desc: "Unas varas de tela de algodón inglesa.");
            Obj("sal", "Sal", TipoObjeto.Mercaderia, 1, 3, vende: true, compra: true, noticias: true, desc: "Sal gruesa de las salinas. Sirve para guardar la carne y el pescado.");
            Obj("galleta", "Galleta", TipoObjeto.Comida, 0.2f, 2, vende: true, alimenta: 0.2f, desc: "Galleta de campo, dura como piedra. Se come remojada en el mate.", comer: "Te comiste una galleta. Dura, pero llena.");

            // ---------- pesca ----------
            Obj("linea", "Línea de crin", TipoObjeto.Herramienta, 0.1f, 5, vende: true, desc: "Sedal de crin de caballo trenzada, con su corcho y una caña de tacuara. Sin anzuelo no sirve.");
            Obj("anzuelo", "Anzuelo", TipoObjeto.Herramienta, 0.01f, 2, vende: true, desc: "Anzuelo de hierro traído de Buenos Aires. Si se corta la línea, se pierde.");
            Obj("lombrices", "Lombrices", TipoObjeto.Carnada, 0.02f, 0, cat: "carnada", desc: "Se juntan removiendo el barro de la orilla. La carnada de los pobres.");
            Obj("achuras", "Achuras", TipoObjeto.Carnada, 0.4f, 1, cat: "carnada", vende: true, desc: "Tripas y desperdicio de la carneada. A las tarariras y a los bagres les encantan.");

            //   id, nombre, peso, precio, laguna, arroyo, abundancia, fuerza, hora
            Pez("mojarra", "Mojarra", 0.05f, 1, true, true, 2.4f, 0.1f, HoraPesca.Siempre, cat: "carnada", desc: "Pescadito plateado que pica de a montones. No se vende: sirve de carnada.");
            Pez("pejerrey", "Pejerrey", 0.3f, 4, true, false, 1.4f, 0.3f, HoraPesca.AlbaYAtardecer, desc: "El pez de la laguna. Pica al alba y al atardecer. Carne blanca y fina.");
            Pez("tararira", "Tararira", 1.5f, 6, true, true, 0.8f, 0.75f, HoraPesca.Dia, desc: "Dientuda y peleadora. Caza de día entre los juncos. Si se tensa mucho, corta la línea.");
            Pez("bagre", "Bagre", 1.2f, 4, true, true, 0.9f, 0.5f, HoraPesca.Noche, desc: "Bigotudo y de fondo. Pica de noche, sobre todo en el arroyo.");

            // ---------- comida hecha ----------
            Obj("pescado_asado", "Pescado asado", TipoObjeto.Comida, 0.3f, 0, alimenta: 0.4f, desc: "Pescado a las brasas. Hay que comerlo en el día.", comer: "Pescado asado a las brasas. Con un poco de sal, una gloria.");
            Obj("pescado_salado", "Pescado salado", TipoObjeto.Comida, 0.25f, 9, compra: true, alimenta: 0.3f, desc: "Abierto por el lomo, salado y oreado al sol. Dura semanas, pesa poco y en la pulpería lo pagan bien.", comer: "Pescado salado. Da una sed bárbara, pero llena.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ---------- recetas ----------
            Receta("pescado_asado", "Pescado asado", LugarReceta.Fogon, 8, "pescado_asado", 1, sabida: true,
                desc: "A las brasas, como lo hace cualquiera.", ing: new[] { ((string)null, "pescado", 1) });
            Receta("pescado_salado", "Pescado salado", LugarReceta.Cualquiera, 0, "pescado_salado", 2,
                desc: "Se abre el pescado por el lomo, se frota con sal y se deja orear.", ing: new[] { ((string)null, "pescado", 2), ("sal", null, 1) },
                ensena: "Assets/Datos/Personajes/casiana.asset",
                alEnsenar: new[]
                {
                    "¿Así que andás pescando en el arroyo? Fresco no te dura ni un día, aprendé a guardarlo.",
                    "Lo abrís por el lomo, le sacás las tripas, lo frotás bien con sal y lo dejás orear al sol. Así te dura semanas, y Ramón te lo paga mejor.",
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Catalogo.Olvidar();
            Debug.Log(nuevas == 0 ? "Economía: ya estaban todas las fichas." : $"Economía: se crearon {nuevas} fichas nuevas en {OBJETOS} y {RECETAS}.");

            // ---------- ayudas ----------
            void Obj(string id, string nombre, TipoObjeto tipo, float peso, float precio, string cat = null, bool vende = false, bool compra = false,
                bool noticias = false, float alimenta = 0, string desc = null, string comer = null)
            {
                string ruta = $"{OBJETOS}/{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<FichaObjeto>(ruta) != null) return;
                var o = ScriptableObject.CreateInstance<FichaObjeto>();
                o.id = id; o.nombre = nombre; o.tipo = tipo; o.categoria = cat ?? (tipo == TipoObjeto.Pescado ? "pescado" : "");
                o.peso = peso; o.precio = precio; o.pulperiaVende = vende; o.pulperiaCompra = compra; o.conNoticias = noticias;
                o.alimenta = alimenta; o.descripcion = desc; o.alComer = comer;
                AssetDatabase.CreateAsset(o, ruta);
                nuevas++;
            }

            void Pez(string id, string nombre, float peso, float precio, bool laguna, bool arroyo, float abundancia, float fuerza, HoraPesca hora, string cat = "pescado", string desc = null)
            {
                string ruta = $"{OBJETOS}/{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<FichaObjeto>(ruta) != null) return;
                Obj(id, nombre, TipoObjeto.Pescado, peso, precio, cat: cat, compra: cat == "pescado", desc: desc);
                var o = AssetDatabase.LoadAssetAtPath<FichaObjeto>(ruta);
                o.enLaguna = laguna; o.enArroyo = arroyo; o.abundancia = abundancia; o.fuerza = fuerza; o.hora = hora;
                EditorUtility.SetDirty(o);
            }

            void Receta(string id, string nombre, LugarReceta lugar, float segundos, string resultado, int cantidad, bool sabida = false, string desc = null,
                (string obj, string cat, int n)[] ing = null, string ensena = null, string[] alEnsenar = null)
            {
                string ruta = $"{RECETAS}/{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<FichaReceta>(ruta) != null) return;
                var r = ScriptableObject.CreateInstance<FichaReceta>();
                r.id = id; r.nombre = nombre; r.lugar = lugar; r.segundos = segundos; r.descripcion = desc; r.sabidaDesdeElPrincipio = sabida;
                r.resultado = AssetDatabase.LoadAssetAtPath<FichaObjeto>($"{OBJETOS}/{resultado}.asset");
                r.cantidad = cantidad;
                r.ingredientes = new List<Ingrediente>();
                foreach (var (obj, cat, n) in ing ?? new (string, string, int)[0])
                    r.ingredientes.Add(new Ingrediente { objeto = obj != null ? AssetDatabase.LoadAssetAtPath<FichaObjeto>($"{OBJETOS}/{obj}.asset") : null, categoria = cat, cantidad = n });
                if (ensena != null) r.ensena = AssetDatabase.LoadAssetAtPath<FichaPersonaje>(ensena);
                r.alEnsenar = alEnsenar;
                AssetDatabase.CreateAsset(r, ruta);
                nuevas++;
            }
        }
    }
}
