using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Base de los objetos que se arman por código (casas, cercos, personajes...).
    /// En la escena solo se guardan los parámetros del componente; la geometría se genera al abrir la escena,
    /// al cambiar un valor en el Inspector y al arrancar el juego. Por eso no hay que editar los hijos "_generado":
    /// se cambian los parámetros y la pieza se rearma sola.
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    public abstract class ProceduralBase : MonoBehaviour
    {
        public const string NOMBRE_GENERADO = "_generado";
        const HideFlags FLAGS = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;

        readonly List<Object> generados = new List<Object>();
        public bool Generado { get; private set; }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Pedir();
#endif
        }

        protected virtual void Start()
        {
            if (Application.isPlaying) Regenerar();
        }

        protected virtual void OnDisable()
        {
            // solo se liberan las mallas; los objetos hijos no se guardan y desaparecen con la escena
            foreach (var o in generados) if (o != null) Destruir(o);
            generados.Clear();
            Generado = false;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!Application.isPlaying && isActiveAndEnabled) Pedir();
        }

        // Cola de regeneración en modo edición. No se puede regenerar dentro de OnEnable/OnValidate
        // (Unity no deja crear ni destruir objetos ahí) y delayCall se pierde al recargar el dominio.
        static readonly HashSet<ProceduralBase> pendientes = new HashSet<ProceduralBase>();

        [UnityEditor.InitializeOnLoadMethod]
        static void Instalar()
        {
            UnityEditor.EditorApplication.update -= Procesar;
            UnityEditor.EditorApplication.update += Procesar;
        }

        static void Procesar()
        {
            if (pendientes.Count == 0 || Application.isPlaying) return;
            var copia = new List<ProceduralBase>(pendientes);
            pendientes.Clear();
            foreach (var p in copia)
                if (p != null && p.isActiveAndEnabled) p.Regenerar();
        }

        void Pedir()
        {
            pendientes.Add(this);
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif

        [ContextMenu("Regenerar")]
        public void Regenerar()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name == NOMBRE_GENERADO) Destruir(c.gameObject);
            }
            foreach (var o in generados) if (o != null) Destruir(o);
            generados.Clear();

            var raiz = new GameObject(NOMBRE_GENERADO).transform;
            raiz.SetParent(transform, false);
            Construir(raiz);
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.hideFlags = FLAGS;
                t.gameObject.layer = gameObject.layer;   // la silueta del jugador depende de su capa
            }
            Generado = true;
            AlGenerar();
        }

        protected abstract void Construir(Transform raiz);
        protected virtual void AlGenerar() { }

        /// Registra una malla creada por código para liberarla después.
        protected Mesh Registrar(Mesh m)
        {
            m.hideFlags = HideFlags.DontSave;
            generados.Add(m);
            return m;
        }

        protected static void Destruir(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        /// Caja de colisión hija, en coordenadas de Unity.
        protected static void Colision(Transform padre, Vector3 centro, Vector3 tam, Quaternion rot = default)
        {
            var go = new GameObject("colision");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = centro;
            go.transform.localRotation = rot == default ? Quaternion.identity : rot;
            go.AddComponent<BoxCollider>().size = tam;
        }
    }
}
