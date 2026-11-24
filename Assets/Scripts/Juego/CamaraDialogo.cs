using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Argentina1860
{
    public enum Plano { General, Medio, Corto }

    /// <summary>
    /// Qué plano usar en cada línea de una conversación (Docs/guion/cinematicas.md). Lógica pura, sin escena: se prueba aparte.
    /// Siempre los mismos tres planos: general (los dos), medio del que habla con el otro en primer plano, y corto del que habla.
    /// </summary>
    public static class PlanosDialogo
    {
        /// Cuántos planos van entre un plano general y el siguiente.
        public const int PLANOS_ENTRE_GENERALES = 3;

        /// Primera línea: general. Cuando cambia el que habla: medio. Línea fuerte (con admiración): corto.
        /// Mismo que habla dos líneas seguidas: alterna medio y corto. Cada tanto vuelve el general para ubicar de nuevo.
        public static Plano Elegir(int indice, bool cambioHablante, bool lineaFuerte, Plano anterior, int desdeGeneral)
        {
            if (indice == 0 || desdeGeneral >= PLANOS_ENTRE_GENERALES) return Plano.General;
            if (lineaFuerte) return Plano.Corto;
            if (cambioHablante) return Plano.Medio;
            return anterior == Plano.Medio ? Plano.Corto : Plano.Medio;
        }

        /// Las líneas del Gallego empiezan con "Gallego:". El resto las dice el otro personaje.
        public static bool HablaElGallego(string linea) =>
            linea != null && (linea.StartsWith("Gallego:") || linea.StartsWith("El Gallego:"));

        public static bool LineaFuerte(string linea) => linea != null && linea.Contains("¡") && linea.TrimEnd().EndsWith("!");

        /// Personajes con cámara cinematográfica mientras la casilla de la ficha no esté tildada: los que pesan en la historia
        /// (Obregón, Rufino, Anselmo, Juana, Hilario, Santos y Jacinta). Ver Docs/guion/cinematicas.md.
        static readonly HashSet<string> IMPORTANTES = new HashSet<string> { "obregon", "remigio", "fermin", "juana", "wenceslao", "nicasio", "casiana" };

        public static bool Importante(FichaPersonaje f) => f != null && (f.conversacionCine || IMPORTANTES.Contains(f.id));
    }

    /// <summary>
    /// Cámara de las conversaciones importantes. Al empezar, CORTE DIRECTO (nunca fundido) a una cámara en perspectiva a la altura
    /// de los ojos; al terminar, corte de vuelta a la isométrica. Se crea sola al primer diálogo: no hay que tocar la escena.
    /// Si no hay lugar para el plano general (una pared, un árbol), no se usa y queda la isométrica acercada.
    /// </summary>
    public class CamaraDialogo : MonoBehaviour
    {
        static CamaraDialogo instancia;
        public static bool Activa => instancia != null && instancia.activa;

        Camera baseCam, cine;
        CamaraIsometrica iso;
        string tagOriginal;
        bool activa;

        Transform gallego, otro;
        string[] lineas;
        int indice, desdeGeneral;
        Plano plano = Plano.General;
        float lado = 1;
        bool hablaGallego;
        readonly Dictionary<Transform, float> ojos = new Dictionary<Transform, float>();

        const float FOV_GENERAL = 38, FOV_MEDIO = 32, FOV_CORTO = 24;

        /// Empieza la conversación. Devuelve true si se usa la cámara de cine (el personaje es importante y hay lugar).
        public static bool Iniciar(JuegoPago juego, NPC npc, string[] lineas)
        {
            if (juego == null || npc == null || juego.camara == null || juego.jugador == null || !PlanosDialogo.Importante(npc.Ficha)) return false;
            if (instancia == null) instancia = new GameObject("Cámara de diálogo (se arma sola)").AddComponent<CamaraDialogo>();
            return instancia.Empezar(juego, npc, lineas);
        }

        public static void Linea(int i) { if (Activa) instancia.IrALinea(i); }

        public static void Terminar() { if (Activa) instancia.Apagar(); }

        bool Empezar(JuegoPago juego, NPC npc, string[] ls)
        {
            iso = juego.camara;
            baseCam = iso.GetComponent<Camera>();
            gallego = juego.jugador.transform;
            otro = npc.transform;
            lineas = ls;
            ojos.Clear();
            if (cine == null) Armar();

            // los dos se miran
            var hacia = Plana(otro.position - gallego.position);
            if (hacia.sqrMagnitude > 0.01f) gallego.rotation = Quaternion.LookRotation(hacia);

            // el lado de la cámara se elige una vez por conversación (regla de los 180°): el que tenga el plano general despejado
            indice = 0; desdeGeneral = 0; plano = Plano.General;
            hablaGallego = PlanosDialogo.HablaElGallego(lineas[0]);
            lado = 1;
            bool ok = Poner(Plano.General);
            if (!ok) { lado = -1; ok = Poner(Plano.General); }
            if (!ok) return false;
            Encender();
            return true;
        }

        void Armar()
        {
            cine = new GameObject("Cámara").AddComponent<Camera>();
            cine.transform.SetParent(transform, false);
            cine.CopyFrom(baseCam);
            cine.orthographic = false;
            cine.enabled = false;
            var d0 = baseCam.GetUniversalAdditionalCameraData();
            var d1 = cine.GetUniversalAdditionalCameraData();
            d1.renderPostProcessing = d0.renderPostProcessing;
            d1.antialiasing = d0.antialiasing;
            d1.volumeLayerMask = d0.volumeLayerMask;
            d1.renderShadows = d0.renderShadows;
        }

        void Encender()
        {
            activa = true;
            tagOriginal = baseCam.tag;
            baseCam.enabled = false;
            baseCam.tag = "Untagged";
            cine.tag = "MainCamera";
            cine.enabled = true;
            iso.RecorteApagado = true;
        }

        void Apagar()
        {
            activa = false;
            cine.enabled = false;
            cine.tag = "Untagged";
            baseCam.tag = tagOriginal;
            baseCam.enabled = true;
            iso.RecorteApagado = false;
        }

        void IrALinea(int i)
        {
            if (lineas == null || i < 0 || i >= lineas.Length) return;
            bool habla = PlanosDialogo.HablaElGallego(lineas[i]);
            bool cambio = i > 0 && habla != hablaGallego;
            plano = PlanosDialogo.Elegir(i, cambio, PlanosDialogo.LineaFuerte(lineas[i]), plano, desdeGeneral);
            desdeGeneral = plano == Plano.General ? 0 : desdeGeneral + 1;
            hablaGallego = habla;
            indice = i;
            if (!Poner(plano)) Poner(Plano.General);
        }

        // Si alguien se mueve (Juan Maidana con su carreta), la cámara lo sigue.
        void LateUpdate()
        {
            if (!activa) return;
            if (otro == null || gallego == null) { Apagar(); return; }
            Poner(plano);
        }

        // ---------- geometría de los planos ----------
        static Vector3 Plana(Vector3 v) { v.y = 0; return v; }

        float Ojos(Transform t)
        {
            if (ojos.TryGetValue(t, out var h)) return h;
            var rs = t.GetComponentsInChildren<Renderer>();
            float tope = 0;
            foreach (var r in rs) if (r.enabled && !(r is ParticleSystemRenderer)) tope = Mathf.Max(tope, r.bounds.max.y - t.position.y);
            h = Mathf.Clamp(tope - 0.15f, 1.2f, 2.2f);
            ojos[t] = h;
            return h;
        }

        bool Poner(Plano p)
        {
            var A = gallego.position; var B = otro.position;
            var eje = Plana(B - A); float sep = eje.magnitude; if (sep < 0.2f) return false; eje /= sep;
            var der = Vector3.Cross(Vector3.up, eje);
            var quien = hablaGallego ? gallego : otro;
            var oyente = hablaGallego ? otro : gallego;
            var haciaOyente = Plana(oyente.position - quien.position).normalized;
            Vector3 pos, foco; float fov;
            switch (p)
            {
                case Plano.General:
                {
                    foco = (A + B) / 2 + Vector3.up * 1.3f;
                    float dist = Mathf.Clamp(sep * 1.4f + 3f, 4.5f, 8f);
                    pos = foco + (der * lado * 0.94f - eje * 0.34f).normalized * dist + Vector3.up * 0.6f;
                    fov = FOV_GENERAL;
                    break;
                }
                case Plano.Medio:
                {
                    pos = oyente.position + haciaOyente * 1.05f + der * lado * 0.5f + Vector3.up * (Ojos(oyente) + 0.05f);
                    foco = quien.position + Vector3.up * (Ojos(quien) - 0.1f);
                    fov = FOV_MEDIO;
                    break;
                }
                default:
                {
                    pos = quien.position + haciaOyente * 1.45f + der * lado * 0.35f + Vector3.up * (Ojos(quien) - 0.02f);
                    foco = quien.position + Vector3.up * (Ojos(quien) - 0.03f);
                    fov = FOV_CORTO;
                    break;
                }
            }
            if (!Despejado(foco, ref pos, p == Plano.General ? 3.5f : 1f)) return false;
            // se mira un poco más abajo del foco: los personajes quedan en el tercio de arriba y el texto va debajo, sin taparlos
            var mira = foco + Vector3.down * (p == Plano.General ? 0.5f : p == Plano.Medio ? 0.3f : 0.12f);
            cine.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira - pos));
            cine.fieldOfView = fov;
            return true;
        }

        /// Que no haya una pared o un tronco entre el foco y la cámara, ni la cámara bajo el suelo.
        static bool Despejado(Vector3 foco, ref Vector3 pos, float distMin)
        {
            int mascara = ~(LayerMask.GetMask("Personaje") | LayerMask.GetMask("Ignore Raycast"));
            if (Physics.Linecast(foco, pos, out var h, mascara, QueryTriggerInteraction.Ignore))
            {
                float d = h.distance - 0.25f;
                if (d < distMin) return false;
                pos = foco + (pos - foco).normalized * d;
            }
            var t = Terrain.activeTerrain;
            if (t != null)
            {
                float suelo = t.SampleHeight(pos) + t.transform.position.y + 0.5f;
                if (pos.y < suelo) pos.y = suelo;
            }
            return true;
        }
    }
}
