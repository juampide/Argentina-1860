using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// La ronda del fogón: cuando llega la hora de cocinar, sale gente de las casas cercanas y camina hasta el fuego.
    /// El primero en llegar es el que cocina (revuelve la olla, ceba con la pava o atiende el asado) y lo prende;
    /// los demás se sientan en los troncos a charlar. Cuando pasa la hora se van yendo de a uno a sus casas.
    /// Son paisanos sin nombre (o soldados, en el fortín) armados al azar con la misma semilla en cada partida;
    /// no se puede hablar con ellos. Lo agrega Fogon al armarse en el juego; no se guarda en la escena.
    /// </summary>
    public class RondaFogon : MonoBehaviour
    {
        const float VELOCIDAD = 1.15f;

        enum Estado { EnCasa, Yendo, EnLaRonda, Volviendo }

        class Comensal
        {
            public Aspecto aspecto;
            public bool cocinero, falda;
            public int asiento;
            public Vector3 casa;
            public Estado estado;
            public float demora;           // hora del reloj de la ronda en que sale
            public bool salir;
            public GameObject go;
            public ModeloPersonaje modelo;
            public float fase, gesto, ritmoGesto, mira;
            public int vueltas;
        }

        readonly List<Comensal> gente = new List<Comensal>();
        Transform carpeta;
        float reloj;                 // horas de juego desde que arrancó la ronda (no da la vuelta a las 24)
        bool activa, armada;
        System.Random rnd;

        /// El cocinero ya está en su puesto (con eso se prende el fuego); si se demora, lo prende otro.
        public bool CocineroEnPuesto { get; private set; }
        float desdeQueEmpezo;

        public void Actualizar(Fogon f, bool horario, float dh, bool salto)
        {
            if (!armada) Armar(f);
            if (gente.Count == 0) return;
            reloj += dh;

            if (salto)
            {
                // arranque o salto de hora: cada uno queda donde le corresponde, sin caminar
                foreach (var c in gente) { Quitar(c); c.salir = false; }
                activa = horario;
                if (horario) foreach (var c in gente) { Aparecer(c, f, true); }
            }
            else if (horario && !activa)
            {
                activa = true;
                desdeQueEmpezo = reloj;
                foreach (var c in gente)
                {
                    c.salir = false;
                    c.demora = reloj + (c.cocinero ? 0 : 0.08f + (float)rnd.NextDouble() * 0.4f);
                }
            }
            else if (!horario && activa)
            {
                activa = false;
                foreach (var c in gente)
                {
                    c.salir = true;
                    c.demora = reloj + (c.cocinero ? 0.3f + (float)rnd.NextDouble() * 0.1f : (float)rnd.NextDouble() * 0.35f);
                }
            }

            CocineroEnPuesto = activa && reloj - desdeQueEmpezo > 0.5f;
            float dt = Time.deltaTime;
            foreach (var c in gente)
            {
                switch (c.estado)
                {
                    case Estado.EnCasa:
                        if (activa && reloj >= c.demora) Aparecer(c, f, false);
                        break;
                    case Estado.Yendo:
                        if (Caminar(c, Destino(c, f), dt, f.transform.position)) c.estado = Estado.EnLaRonda;
                        if (c.salir && reloj >= c.demora) c.estado = Estado.Volviendo;
                        break;
                    case Estado.EnLaRonda:
                        if (c.cocinero) CocineroEnPuesto = true;
                        Quieto(c, f, dt);
                        if (c.salir && reloj >= c.demora) c.estado = Estado.Volviendo;
                        break;
                    case Estado.Volviendo:
                        if (Caminar(c, c.casa, dt, f.transform.position)) { Quitar(c); c.estado = Estado.EnCasa; }
                        break;
                }
            }
        }

        // ---------- quiénes son y de dónde vienen ----------

        void Armar(Fogon f)
        {
            armada = true;
            var p = f.transform.position;
            rnd = new System.Random(Mathf.Abs(Mathf.RoundToInt(p.x * 7.3f + p.z * 13.1f)) + 1);
            var carp = new GameObject("Ronda del fogón");
            carp.hideFlags = HideFlags.DontSave;
            carpeta = carp.transform;

            // casas cercanas de donde sale la gente
            var casas = new List<Vector3>();
            void Sumar(Component c, float radio)
            {
                var q = c.transform.position;
                float d = Vector3.Distance(q, p);
                if (d > 8 && d < 60) casas.Add(q + (p - q).normalized * radio);
            }
            foreach (var c in FindObjectsByType<Casa>()) Sumar(c, 5);
            foreach (var c in FindObjectsByType<Casona>()) Sumar(c, 16);
            foreach (var c in FindObjectsByType<EdificioConInterior>()) Sumar(c, 7);
            casas.Sort((a, b) => Vector3.Distance(a, p).CompareTo(Vector3.Distance(b, p)));
            if (casas.Count > 3) casas.RemoveRange(3, casas.Count - 3);
            if (casas.Count == 0) casas.Add(p + Quaternion.Euler(0, (float)rnd.NextDouble() * 360, 0) * Vector3.forward * 30);

            int n = Mathf.Min(f.gente, f.Asientos + 1);
            for (int i = 0; i < n; i++)
            {
                bool cocinero = i == 0 && f.coccion != Coccion.Nada;
                bool mujer = !f.soldados && (cocinero ? f.coccion == Coccion.Olla : rnd.NextDouble() < 0.3);
                var a = f.soldados ? Soldado() : mujer ? Paisana() : Paisano(cocinero && f.coccion == Coccion.Asado);
                gente.Add(new Comensal
                {
                    aspecto = a,
                    cocinero = cocinero,
                    falda = a.estilo == Estilo.Vestido,
                    asiento = cocinero ? -1 : (f.coccion == Coccion.Nada ? i : i - 1),
                    casa = casas[i % casas.Count] + new Vector3((float)rnd.NextDouble() * 2 - 1, 0, (float)rnd.NextDouble() * 2 - 1),
                    ritmoGesto = 0.2f + (float)rnd.NextDouble() * 0.25f,
                    fase = (float)rnd.NextDouble() * 10,
                });
            }
        }

        static readonly string[] PONCHOS = { "#7a2e22", "#5a4632", "#3e4a5a", "#8a6a3a", "#4a3a2e", "#6e5a44", "#2f3a2e" };
        static readonly string[] PIELES = { "#e8b48c", "#d29a72", "#b97d58", "#a06a48", "#f0c19a" };
        static readonly string[] PELOS = { "#1d1714", "#2e221a", "#3a2a1c", "#4a3a2a", "#8a8078" };
        static readonly string[] VESTIDOS = { "#5a4a3e", "#3e3a44", "#6a4a3a", "#4a5048", "#7a6450", "#3a3a3a" };

        string Uno(string[] l) => l[rnd.Next(l.Length)];
        Color Hex(string[] l) => Conv.Hex(Uno(l));

        Aspecto Paisano(bool asador)
        {
            var a = new Aspecto { estilo = Estilo.Poncho, piel = Hex(PIELES), pelo = Hex(PELOS), poncho = Hex(PONCHOS) };
            a.sombrero = rnd.NextDouble() < 0.55 ? Sombrero.Ala : rnd.NextDouble() < 0.5 ? Sombrero.Gorra : Sombrero.Ninguno;
            a.colorSombrero = Conv.Hex(rnd.NextDouble() < 0.5 ? "#2e2620" : "#5a4a38");
            a.bigote = rnd.NextDouble() < 0.45 ? Bigote.Fino : Bigote.Ninguno;
            a.barba = !asador && rnd.NextDouble() < 0.3;
            a.escala = 0.94f + (float)rnd.NextDouble() * 0.1f;
            return a;
        }

        Aspecto Paisana()
        {
            var a = new Aspecto { estilo = Estilo.Vestido, piel = Hex(PIELES), pelo = Hex(PELOS), ropa = Hex(VESTIDOS) };
            a.ropa2 = Conv.Hex(rnd.NextDouble() < 0.5 ? "#d9cfb8" : "#8a3a2a");   // chal
            a.escala = 0.9f + (float)rnd.NextDouble() * 0.07f;
            return a;
        }

        Aspecto Soldado()
        {
            var a = new Aspecto { estilo = Estilo.Levita, piel = Hex(PIELES), pelo = Hex(PELOS) };
            a.ropa = Conv.Hex("#2f3a55");
            a.chaleco = Conv.Hex("#2f3a55");
            a.pantalon = Conv.Hex(rnd.NextDouble() < 0.6 ? "#c8c0a8" : "#2f3a55");
            a.sombrero = Sombrero.Kepi;
            a.colorSombrero = Conv.Hex("#3a3a48");
            a.bigote = rnd.NextDouble() < 0.6 ? Bigote.Fino : Bigote.Ninguno;
            a.escala = 0.95f + (float)rnd.NextDouble() * 0.08f;
            return a;
        }

        // ---------- moverse ----------

        Vector3 Destino(Comensal c, Fogon f) => c.cocinero ? f.PuestoCocinero : f.Asiento(Mathf.Clamp(c.asiento, 0, f.Asientos - 1));

        void Aparecer(Comensal c, Fogon f, bool sentado)
        {
            if (c.go == null)
            {
                c.go = new GameObject(c.cocinero ? "cocinero del fogón" : "paisano del fogón");
                c.go.hideFlags = HideFlags.DontSave;
                c.go.transform.SetParent(carpeta, false);
                c.modelo = c.go.AddComponent<ModeloPersonaje>();
                c.modelo.aspecto = c.aspecto;
                var cap = c.go.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0, 0.9f, 0); cap.radius = 0.35f; cap.height = 1.8f;
                Pisadas.Registrar(c.go.transform, 0.4f);
            }
            var p = sentado ? Destino(c, f) : c.casa;
            p.y = Suelo(p);
            c.go.transform.position = p;
            c.go.transform.rotation = Quaternion.LookRotation(Plano(f.transform.position - p));
            c.go.SetActive(true);
            // al desactivarse el modelo suelta sus mallas: si vuelve a salir de la casa, se rearma
            if (c.vueltas++ > 0 && !c.modelo.Generado) c.modelo.Regenerar();
            c.estado = sentado ? Estado.EnLaRonda : Estado.Yendo;
        }

        void Quitar(Comensal c)
        {
            if (c.go != null) c.go.SetActive(false);
        }

        /// Camina hacia el destino; devuelve true al llegar.
        bool Caminar(Comensal c, Vector3 destino, float dt, Vector3 fuego)
        {
            var t = c.go.transform;
            float dist = Plano(destino - t.position).magnitude;
            if (dist < 0.08f) return true;
            var d = Plano(Desvio(t.position, destino, fuego) - t.position);
            float tramo = d.magnitude;
            if (tramo < 0.01f) return true;
            dist = tramo;
            var paso = d / dist * Mathf.Min(dist, VELOCIDAD * dt);
            var p = t.position + paso;
            p.y = Suelo(p);
            t.position = p;
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(d), Mathf.Min(1, dt * 6));
            c.fase += dt * 6.5f;
            c.modelo.Anim?.Animar(c.fase, 0.85f);
            return false;
        }

        void Quieto(Comensal c, Fogon f, float dt)
        {
            var t = c.go.transform;
            var hacia = Plano(f.transform.position - t.position);
            if (hacia.sqrMagnitude > 0.001f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(hacia), Mathf.Min(1, dt * 3));
            c.fase += dt;
            var anim = c.modelo.Anim;
            if (anim == null) return;
            if (c.cocinero && f.coccion != Coccion.Pava)
            {
                anim.Cocinando(c.fase, f.coccion == Coccion.Asado ? 0.12f : 0.45f);
                return;
            }
            // charla: cada tanto alguien cuenta algo con las manos y los demás lo miran
            float cuenta = Mathf.Sin(c.fase * c.ritmoGesto + c.asiento * 1.7f);
            c.gesto = Mathf.MoveTowards(c.gesto, cuenta > 0.55f ? 1 : 0, dt * 1.5f);
            float mira = Mathf.Sin(c.fase * 0.23f + c.asiento) * 0.6f;
            c.mira = Mathf.Lerp(c.mira, mira, dt);
            anim.Sentado(c.fase, c.gesto, c.mira, c.falda ? 0.36f : 0.44f);
        }

        /// Si el camino derecho pasa por el fuego, apunta a un punto al costado para rodearlo.
        static Vector3 Desvio(Vector3 p, Vector3 destino, Vector3 fuego)
        {
            const float RADIO = 1.4f;
            var d = Plano(destino - p);
            float largo = d.magnitude;
            if (largo < 0.01f) return destino;
            var u = d / largo;
            var aF = Plano(fuego - p);
            float t = Vector3.Dot(aF, u);
            if (t <= 0 || t >= largo) return destino;
            var cerca = aF - u * t;                       // del camino al centro del fuego
            if (cerca.magnitude >= RADIO) return destino;
            var lado = cerca.sqrMagnitude < 1e-4f ? Vector3.Cross(Vector3.up, u) : -cerca.normalized;
            var rodeo = fuego + lado * (RADIO + 0.3f);
            rodeo.y = p.y;
            return Plano(rodeo - p).magnitude < 0.3f ? destino : rodeo;
        }

        static Vector3 Plano(Vector3 v) { v.y = 0; return v; }

        static float Suelo(Vector3 p)
        {
            var ter = Terrain.activeTerrain;
            return ter != null ? ter.SampleHeight(p) + ter.transform.position.y : p.y;
        }

        void OnDestroy()
        {
            if (carpeta != null) Destroy(carpeta.gameObject);
        }
    }
}
