using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// Un animal ya instanciado: su raíz y las articulaciones que se animan.
    public class Bicho
    {
        public Transform raiz;
        public readonly List<Transform> patas = new List<Transform>();
        public readonly List<Transform> alas = new List<Transform>();
        public readonly List<float> basePatas = new List<float>();
        // caballo
        public Transform cuello, cabeza, cola;
        // carreta
        public readonly List<Transform> ruedas = new List<Transform>();
        public readonly List<Bicho> bueyes = new List<Bicho>();

        /// Mueve las patas en diagonal (paso o trote). Con dos patas, alterna.
        public void Bailar(float w)
        {
            if (patas.Count == 4)
            {
                patas[0].localRotation = Conv.Rot(w, 0, 0); patas[1].localRotation = Conv.Rot(w, 0, 0);
                patas[2].localRotation = Conv.Rot(-w, 0, 0); patas[3].localRotation = Conv.Rot(-w, 0, 0);
            }
            else if (patas.Count == 2)
            {
                patas[0].localRotation = Conv.Rot(w, 0, 0); patas[1].localRotation = Conv.Rot(-w, 0, 0);
            }
        }

        /// Caballo: paso o galope, las diagonales se mueven juntas.
        public void Andar(float fase, float amp)
        {
            if (patas.Count < 4) return;
            float w = Mathf.Sin(fase) * amp;
            patas[0].localRotation = Conv.Rot(basePatas[0] + w, 0, 0); patas[3].localRotation = Conv.Rot(basePatas[3] + w, 0, 0);
            patas[1].localRotation = Conv.Rot(basePatas[1] - w, 0, 0); patas[2].localRotation = Conv.Rot(basePatas[2] - w, 0, 0);
        }

        /// Caballo en reposo: cabeceo, cola, pasta con la cabeza gacha y a veces rasca el suelo.
        public void AnimarCaballo(float t, float pasto = 0, bool anda = false)
        {
            if (cuello == null) return;
            float alto = 1 - pasto;
            cuello.localRotation = Conv.Rot(-0.85f + Mathf.Sin(t * 0.5f) * 0.05f * alto + pasto * 1.5f + Mathf.Sin(t * 2.1f) * 0.03f * pasto, Mathf.Sin(t * 0.23f) * 0.12f * (1 - pasto * 0.7f), 0);
            cabeza.localRotation = Conv.Rot(1.3f + Mathf.Sin(t * 0.7f + 1) * 0.06f * alto - pasto * 0.35f, 0, 0);
            cola.localRotation = Conv.Rot(Mathf.Sin(t * 0.6f) * 0.06f, 0, Mathf.Sin(t * 0.9f) * 0.18f + Mathf.Sin(t * 3.1f) * 0.1f * pasto);
            if (!anda && patas.Count == 4)
            {
                patas[1].localRotation = Conv.Rot(basePatas[1] + Mathf.Max(0, Mathf.Sin(t * 0.4f - 1)) * 0.35f * Mathf.Max(0, Mathf.Sin(t * 0.13f)) * alto, 0, 0);
                patas[2].localRotation = Conv.Rot(basePatas[2] + Mathf.Max(0, Mathf.Sin(t * 0.21f + 2) - 0.7f) * 0.5f, 0, 0);
            }
        }
    }

    /// Modelos low poly de la fauna del pago, iguales a los del prototipo.
    public static class ModelosFauna
    {
        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
        static Vector3 S(float s) => new Vector3(s, s, s);
        const float PI = Mathf.PI;
        static Geo ICO => Geo.Ico(1, 1);
        static Geo ICO0 => Geo.Ico(1, 0);
        static string Osc(string c, float f = 0.6f) => Conv.Clave(Conv.Aclara(Conv.Hex(c), f));

        public class Spec
        {
            public float L, A, pat, grosor, cuello, cab, cola;
            public string color, claro, patas, hocico, colaColor, manchas, pezunas;
            public bool orejas, cuernos, astas;
            public List<(float x, float y, float z, float sx, float sy, float sz, string c)> parches;
        }

        // ---------- instanciado común ----------
        class Plano
        {
            public Nodo raiz;
            public readonly List<Nodo> patas = new List<Nodo>(), alas = new List<Nodo>(), ruedas = new List<Nodo>();
            public Nodo cuello, cabeza, cola;
            public readonly List<float> basePatas = new List<float>();
            public readonly List<Plano> bueyes = new List<Plano>();
        }

        static Bicho Instanciar(Plano p, Transform padre, string nombre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            var mapa = new Dictionary<Nodo, Transform>();
            p.raiz.Instanciar(go.transform, null, mapa);
            return Armar(p, mapa, go.transform);
        }

        static Bicho Armar(Plano p, Dictionary<Nodo, Transform> mapa, Transform raiz)
        {
            var b = new Bicho { raiz = raiz };
            foreach (var n in p.patas) b.patas.Add(mapa[n]);
            foreach (var n in p.alas) b.alas.Add(mapa[n]);
            foreach (var n in p.ruedas) b.ruedas.Add(mapa[n]);
            b.basePatas.AddRange(p.basePatas);
            while (b.basePatas.Count < b.patas.Count) b.basePatas.Add(0);
            if (p.cuello != null) { b.cuello = mapa[p.cuello]; b.cabeza = mapa[p.cabeza]; b.cola = mapa[p.cola]; }
            foreach (var bp in p.bueyes) b.bueyes.Add(Armar(bp, mapa, mapa[bp.raiz]));
            return b;
        }

        // ---------- cuadrúpedos: cuerpo facetado de una pieza + 4 patas que se mueven ----------
        static Plano Cuadrupedo(Spec s, string nombre = "cuadrupedo")
        {
            var p = new Plano { raiz = new Nodo(nombre) };
            var n = p.raiz;
            float bas = s.pat, A = s.A, L = s.L, gr = s.grosor, cu = s.cuello, cab = s.cab;
            n.Pieza(ICO, s.color, v(0, bas + A * 0.55f, 0), default, v(gr, A * 0.55f, L * 0.5f));
            n.Pieza(ICO, s.claro ?? s.color, v(0, bas + A * 0.28f, 0), default, v(gr * 0.92f, A * 0.3f, L * 0.42f));
            float zc0 = L * 0.4f;
            if (cu > 0) n.Pieza(Geo.Cilindro(gr * 0.34f, gr * 0.52f, cu, 5), s.color, v(0, bas + A * 0.8f + cu * 0.38f, zc0 + cu * 0.22f), v(0.65f, 0, 0));
            float yc = bas + A * 0.8f + cu * 0.78f + cab * 0.05f, zc = zc0 + cu * 0.5f + cab * 0.2f;
            n.Pieza(ICO, s.color, v(0, yc, zc), default, v(cab * 0.26f, cab * 0.28f, cab * 0.5f));
            n.Pieza(Geo.Cono(cab * 0.17f, cab * 0.5f, 4), s.hocico ?? s.color, v(0, yc - cab * 0.06f, zc + cab * 0.55f), v(PI / 2, 0, 0));
            if (s.orejas) foreach (var sx in new[] { -1f, 1f }) n.Pieza(Geo.Cono(cab * 0.09f, cab * 0.4f, 3), s.color, v(sx * cab * 0.2f, yc + cab * 0.3f, zc - cab * 0.12f), v(0, 0, -sx * 0.3f));
            if (s.astas) foreach (var sx in new[] { -1f, 1f })
                {
                    n.Pieza(Geo.Cono(0.02f, 0.35f, 3), "#cdb98e", v(sx * 0.07f, yc + 0.28f, zc - 0.05f), v(0.2f, 0, -sx * 0.35f));
                    n.Pieza(Geo.Cono(0.015f, 0.2f, 3), "#cdb98e", v(sx * 0.15f, yc + 0.4f, zc), v(0.5f, 0, -sx * 0.5f));
                }
            if (s.cuernos) foreach (var sx in new[] { -1f, 1f }) n.Pieza(Geo.Cono(0.04f, 0.34f, 4), "#eae2cc", v(sx * cab * 0.26f, yc + cab * 0.3f, zc - cab * 0.05f), v(0, 0, -sx * 1.1f));
            if (s.parches != null) foreach (var q in s.parches) n.Pieza(ICO0, q.c ?? "#1d1d20", v(q.x, q.y, q.z), default, v(q.sx, q.sy, q.sz));
            if (s.manchas != null) for (int i = 0; i < 4; i++) n.Pieza(ICO0, s.manchas, v((i % 2 == 1 ? 1 : -1) * gr * 0.8f, bas + A * (0.45f + (i % 3) * 0.12f), (i - 1.5f) * L * 0.2f), default, v(gr * 0.4f, A * 0.22f, L * 0.1f));
            if (s.cola > 0) n.Pieza(Geo.Cono(Mathf.Max(0.03f, gr * 0.2f), s.cola, 4), s.colaColor ?? s.color, v(0, bas + A * 0.75f, -L * 0.5f - s.cola * 0.28f), v(-2.0f, 0, 0));
            float rg = Mathf.Max(0.03f, gr * 0.17f), alto = bas + A * 0.2f;
            foreach (var (sx, sz) in new[] { (-1f, 1f), (1f, -1f), (1f, 1f), (-1f, -1f) })   // las diagonales se mueven juntas
            {
                var pt = n.Hijo("pata", v(sx * gr * 0.62f, alto, sz * L * 0.3f));
                pt.Pieza(Geo.Cilindro(rg, rg * 0.75f, alto, 5).Trasladar(0, -alto / 2, 0), s.patas ?? Osc(s.color), Vector3.zero);
                if (s.pezunas != null) pt.Pieza(Geo.Cilindro(rg * 1.15f, rg * 1.05f, 0.1f, 5).Trasladar(0, -alto + 0.05f, 0), s.pezunas, Vector3.zero);
                p.patas.Add(pt);
            }
            return p;
        }

        // Vaca holando: cuerpo blanco con grandes manchas negras, hocico negro, cuernos claros, patas blancas con pezuñas
        static Spec VacaSpec()
        {
            float J() => (Random.value - 0.5f) * 0.14f;
            float E = Random.value < 0.5f ? 1 : -1;
            return new Spec
            {
                L = 2.1f, A = 0.9f, pat = 0.7f, grosor = 0.45f, cuello = 0.4f, cab = 0.55f, cola = 0.7f, color = "#f1efe9", claro = "#f1efe9", patas = "#ecebe5",
                hocico = "#1d1d20", colaColor = "#1d1d20", orejas = true, cuernos = true, pezunas = "#8a8460",
                parches = new List<(float, float, float, float, float, float, string)>
                {
                    (0.34f * E + J(), 1.35f, -0.15f + J(), 0.14f, 0.38f, 0.55f, null), (-0.36f * E + J(), 1.3f, 0.12f + J(), 0.14f, 0.34f, 0.42f, null),
                    (0.02f + J(), 1.63f, -0.35f + J(), 0.34f, 0.12f, 0.42f, null), (-0.35f * E, 1.2f, -0.72f + J(), 0.14f, 0.35f, 0.3f, null),
                    (0.35f * E, 1.25f, 0.62f + J(), 0.14f, 0.45f, 0.17f, null), (0.0f, 1.7f, 0.45f + J(), 0.25f, 0.08f, 0.22f, null),
                    (0.13f * E, 1.78f, 1.16f, 0.06f, 0.1f, 0.15f, null), (-0.14f * E, 1.74f, 1.1f, 0.05f, 0.08f, 0.1f, null),
                    (0.2f * E, 1.5f, 0.9f, 0.1f, 0.22f, 0.2f, null),
                },
            };
        }

        // ---------- aves ----------
        class SpecAve { public float L, A, pat, gr, cuello, cab; public string color, claro, cabColor, pico, patas; }

        static Plano Ave(SpecAve s)
        {
            var p = new Plano { raiz = new Nodo("ave") };
            var n = p.raiz;
            float bas = s.pat;
            n.Pieza(ICO, s.color, v(0, bas + s.A * 0.5f, 0), default, v(s.gr, s.A * 0.5f, s.L * 0.5f));
            n.Pieza(ICO, s.claro ?? s.color, v(0, bas + s.A * 0.35f, s.L * 0.1f), default, v(s.gr * 0.85f, s.A * 0.32f, s.L * 0.4f));
            n.Pieza(Geo.Cilindro(s.gr * 0.2f, s.gr * 0.32f, s.cuello, 5), s.color, v(0, bas + s.A * 0.85f + s.cuello * 0.45f, s.L * 0.35f), v(0.35f, 0, 0));
            float yc = bas + s.A * 0.85f + s.cuello * 0.95f, zc = s.L * 0.35f + s.cuello * 0.2f;
            n.Pieza(ICO, s.cabColor ?? s.color, v(0, yc, zc), default, v(s.cab * 0.3f, s.cab * 0.3f, s.cab * 0.45f));
            n.Pieza(Geo.Cono(s.cab * 0.14f, s.cab * 0.55f, 4), s.pico ?? "#c8b080", v(0, yc - s.cab * 0.04f, zc + s.cab * 0.55f), v(PI / 2, 0, 0));
            n.Pieza(Geo.Cono(s.gr * 0.5f, s.L * 0.5f, 4), s.color, v(0, bas + s.A * 0.6f, -s.L * 0.5f), v(-1.9f, 0, 0));
            foreach (var sx in new[] { -1f, 1f })
            {
                var pt = n.Hijo("pata", v(sx * s.gr * 0.35f, bas, 0));
                pt.Pieza(Geo.Cilindro(0.025f, 0.02f, bas, 4).Trasladar(0, -bas / 2, 0), s.patas ?? "#8a7a60", Vector3.zero);
                p.patas.Add(pt);
            }
            return p;
        }

        static Plano Carancho()
        {
            var p = new Plano { raiz = new Nodo("carancho") };
            var n = p.raiz;
            n.Pieza(ICO, "#2b2622", v(0, 0, 0), default, v(0.16f, 0.14f, 0.34f));
            n.Pieza(ICO, "#ebe5d4", v(0, -0.02f, 0.1f), default, v(0.14f, 0.1f, 0.18f));
            n.Pieza(ICO, "#171310", v(0, 0.06f, 0.34f), default, v(0.09f, 0.09f, 0.12f));
            n.Pieza(Geo.Cono(0.04f, 0.12f, 4), "#d8b84a", v(0, 0.03f, 0.5f), v(PI / 2, 0, 0));
            n.Pieza(Geo.Cono(0.1f, 0.3f, 4), "#2b2622", v(0, 0, -0.38f), v(-1.6f, 0, 0));
            foreach (var sx in new[] { -1f, 1f })
            {
                var a = n.Hijo("ala", v(sx * 0.12f, 0.04f, 0.02f));
                a.Pieza(ICO, "#2b2622", v(sx * 0.42f, 0, 0), default, v(0.5f, 0.025f, 0.2f));
                p.alas.Add(a);
            }
            return p;
        }

        static Plano Yarara()
        {
            var p = new Plano { raiz = new Nodo("yarara") };
            for (int i = 0; i < 9; i++) p.raiz.Pieza(ICO0, i % 2 == 1 ? "#2e2519" : "#76623f", v(Mathf.Sin(i * 0.9f) * 0.18f, 0.05f, -i * 0.13f), default, v(0.06f - i * 0.003f, 0.05f, 0.08f));
            p.raiz.Pieza(ICO0, "#76623f", v(Mathf.Sin(-0.9f) * 0.18f, 0.07f, 0.12f), default, v(0.07f, 0.05f, 0.1f));
            return p;
        }

        static Plano Peludo()
        {
            var p = new Plano { raiz = new Nodo("peludo") };
            var n = p.raiz;
            n.Pieza(ICO, "#a39178", v(0, 0.2f, 0), default, v(0.22f, 0.17f, 0.34f));
            n.Pieza(ICO, "#7d6d58", v(0, 0.26f, 0.05f), default, v(0.2f, 0.1f, 0.06f));
            n.Pieza(ICO, "#7d6d58", v(0, 0.26f, -0.1f), default, v(0.2f, 0.1f, 0.06f));
            n.Pieza(ICO, "#a39178", v(0, 0.18f, 0.36f), default, v(0.1f, 0.09f, 0.14f));
            n.Pieza(Geo.Cono(0.035f, 0.12f, 3), "#c4b69c", v(0, 0.17f, 0.48f), v(PI / 2, 0, 0));
            n.Pieza(Geo.Cono(0.05f, 0.24f, 4), "#8a7a64", v(0, 0.14f, -0.4f), v(-1.9f, 0, 0));
            foreach (var (sx, sz) in new[] { (-1f, 1f), (1f, -1f), (1f, 1f), (-1f, -1f) })
            {
                var pt = n.Hijo("pata", v(sx * 0.13f, 0.12f, sz * 0.18f));
                pt.Pieza(Geo.Cilindro(0.025f, 0.02f, 0.12f, 4).Trasladar(0, -0.06f, 0), "#6e604c", Vector3.zero);
                p.patas.Add(pt);
            }
            return p;
        }

        // ---------- especies ----------
        public static Bicho Crear(string esp, Transform padre)
        {
            Plano p;
            switch (esp)
            {
                case "venado": p = Cuadrupedo(new Spec { L = 1.2f, A = 0.55f, pat = 0.6f, grosor = 0.26f, cuello = 0.55f, cab = 0.3f, cola = 0.14f, color = "#b9803f", claro = "#efe3cf", orejas = true, astas = true }); break;
                case "zorro": p = Cuadrupedo(new Spec { L = 0.75f, A = 0.26f, pat = 0.26f, grosor = 0.13f, cuello = 0.12f, cab = 0.24f, cola = 0.5f, color = "#8c8a85", claro = "#d8d4cc", orejas = true, colaColor = "#6e6b66" }); break;
                case "vizcacha": p = Cuadrupedo(new Spec { L = 0.55f, A = 0.36f, pat = 0.14f, grosor = 0.2f, cab = 0.26f, cola = 0.18f, color = "#8a7d68", claro = "#cfc7b6" }); break;
                case "carpincho": p = Cuadrupedo(new Spec { L = 1.1f, A = 0.5f, pat = 0.2f, grosor = 0.3f, cab = 0.38f, color = "#6b4a30", claro = "#8a6445", orejas = true }); break;
                case "coipo": p = Cuadrupedo(new Spec { L = 0.62f, A = 0.3f, pat = 0.1f, grosor = 0.17f, cab = 0.24f, cola = 0.5f, color = "#6a4a32", claro = "#8c6a4c", orejas = true }); break;
                case "puma": p = Cuadrupedo(new Spec { L = 1.45f, A = 0.6f, pat = 0.46f, grosor = 0.22f, cuello = 0.3f, cab = 0.34f, cola = 1.0f, color = "#c8a060", claro = "#eadcc0", orejas = true }); break;
                case "vaca": p = Cuadrupedo(VacaSpec(), "vaca"); break;
                case "nandu": p = Ave(new SpecAve { L = 0.95f, A = 0.55f, pat = 0.9f, gr = 0.28f, cuello = 0.85f, cab = 0.22f, color = "#9a8b78", claro = "#cfc3b0", pico = "#6a5a48", patas = "#7a6c58" }); break;
                case "perdiz": p = Ave(new SpecAve { L = 0.36f, A = 0.2f, pat = 0.14f, gr = 0.11f, cuello = 0.1f, cab = 0.1f, color = "#b07a3d", claro = "#e4c89a", pico = "#8a6a3a" }); break;
                case "peludo": p = Peludo(); break;
                case "yarara": p = Yarara(); break;
                case "carancho": p = Carancho(); break;
                default: throw new System.ArgumentException(esp);
            }
            return Instanciar(p, padre, esp);
        }

        /// Montículo de la vizcachera con madrigueras.
        public static Transform Vizcachera(Transform padre)
        {
            var n = new Nodo("vizcachera");
            n.Pieza(ICO, "#8d7a5e", v(0, 0.25f, 0), default, v(3.2f, 0.5f, 2.6f));
            n.Pieza(Geo.Cilindro(0.35f, 0.45f, 0.3f, 6), "#2a2118", v(1.2f, 0.45f, 0.8f));
            n.Pieza(Geo.Cilindro(0.3f, 0.4f, 0.3f, 6), "#2a2118", v(-1.0f, 0.4f, 0.5f));
            n.Pieza(Geo.Cilindro(0.3f, 0.4f, 0.3f, 6), "#2a2118", v(0.1f, 0.5f, -0.9f));
            n.Pieza(Geo.Cono(0.05f, 1.0f, 3), "#6e5a3e", v(-1.8f, 0.4f, -0.4f), v(0, 0, 0.8f));
            n.Pieza(Geo.Cono(0.05f, 0.9f, 3), "#6e5a3e", v(1.9f, 0.4f, -0.3f), v(0, 0, -0.9f));
            return n.Instanciar(padre, null);
        }

        /// Ala simple para las perdices que levantan vuelo.
        public static Transform Ala(Transform padre, float lado)
        {
            var n = new Nodo("ala") { esc = v(lado, 1, 1) };
            n.Pieza(Geo.Caja(0.5f, 0.02f, 0.2f).Trasladar(0.25f, 0, 0), "#8a6a3a", Vector3.zero);
            return n.Instanciar(padre, null);
        }

        // ---------- caballo criollo (animales.js) ----------
        static Plano PlanoCaballo(string color, bool calcetines)
        {
            var p = new Plano { raiz = new Nodo("caballo") };
            var g = p.raiz;
            var c0 = Conv.Hex(color);
            string pel = color, oscuro = Conv.Clave(Conv.Aclara(c0, 0.6f)), crin = Conv.Clave(Conv.Aclara(c0, 0.3f));
            const string pezu = "#2a2420", cuero = "#4a3220", blanco = "#efe6d2";
            string hocico = Conv.Clave(Conv.Aclara(c0, 0.42f));
            Geo.Seccion Sc(float z, float rx, float ry, float y) => new Geo.Seccion(z, rx, ry, y);
            g.Pieza(Geo.Loft(new[] { Sc(-0.98f, 0.1f, 0.15f, 1.22f), Sc(-0.82f, 0.25f, 0.32f, 1.2f), Sc(-0.46f, 0.3f, 0.37f, 1.17f), Sc(-0.05f, 0.27f, 0.33f, 1.1f), Sc(0.36f, 0.29f, 0.37f, 1.14f), Sc(0.7f, 0.22f, 0.34f, 1.2f), Sc(0.9f, 0.12f, 0.22f, 1.26f) }, 7), pel, Vector3.zero);
            foreach (var (x, z, trasera) in new[] { (-0.17f, 0.55f, false), (0.17f, 0.55f, false), (-0.2f, -0.58f, true), (0.2f, -0.58f, true) })
            {
                var cad = g.Hijo("pata", v(x, 0.92f, z));
                cad.Pieza(Geo.Cilindro(trasera ? 0.11f : 0.085f, 0.055f, 0.46f, 5), pel, v(0, -0.21f, 0));
                var rod = cad.Hijo("rodilla", v(0, -0.42f, 0));
                rod.Pieza(Geo.Cilindro(0.045f, 0.036f, 0.38f, 5), oscuro, v(0, -0.18f, 0));
                rod.Pieza(Geo.Octa(0.05f), oscuro, v(0, -0.02f, 0), default, v(1, 0.8f, 1));
                if (calcetines) rod.Pieza(Geo.Cilindro(0.055f, 0.052f, 0.13f, 5), blanco, v(0, -0.3f, 0));
                rod.Pieza(Geo.Cilindro(0.058f, 0.075f, 0.1f, 6), pezu, v(0, -0.43f, 0.01f));
                if (trasera) { cad.rot = v(-0.3f, 0, 0); rod.rot = v(0.5f, 0, 0); }
                p.patas.Add(cad);
                p.basePatas.Add(trasera ? -0.3f : 0);
            }
            var cuello = g.Hijo("cuello", v(0, 1.3f, 0.58f));
            cuello.rot = v(-0.85f, 0, 0);
            cuello.Pieza(Geo.Loft(new[] { Sc(-0.05f, 0.19f, 0.23f, 0), Sc(0.3f, 0.14f, 0.2f, 0.01f), Sc(0.55f, 0.115f, 0.16f, 0), Sc(0.75f, 0.1f, 0.13f, -0.01f) }, 7), pel, Vector3.zero);
            cuello.Pieza(Geo.Toro(0.2f, 0.05f, 4, 7), "#6b4528", v(0, 0, 0.1f), default, v(1, 1.15f, 1));
            cuello.Pieza(Geo.Loft(new[] { Sc(0.0f, 0.03f, 0.07f, 0.25f), Sc(0.3f, 0.028f, 0.07f, 0.23f), Sc(0.55f, 0.025f, 0.065f, 0.19f), Sc(0.74f, 0.022f, 0.05f, 0.15f) }, 4), crin, Vector3.zero);
            var cabeza = cuello.Hijo("cabeza", v(0, -0.01f, 0.75f));
            cabeza.rot = v(1.3f, 0, 0);
            cabeza.Pieza(Geo.Loft(new[] { Sc(-0.03f, 0.1f, 0.13f, 0), Sc(0.22f, 0.08f, 0.105f, -0.01f), Sc(0.42f, 0.058f, 0.07f, -0.025f), Sc(0.52f, 0.052f, 0.058f, -0.03f) }, 6), pel, Vector3.zero);
            cabeza.Pieza(Geo.Octa(0.055f), hocico, v(0, -0.03f, 0.5f), default, v(1, 0.9f, 1.2f));
            if (calcetines) cabeza.Pieza(Geo.Caja(0.045f, 0.014f, 0.34f), blanco, v(0, 0.115f, 0.16f), v(0.14f, 0, 0));
            foreach (var sx in new[] { -1f, 1f })
            {
                cabeza.Pieza(Geo.Octa(0.017f), pezu, v(sx * 0.093f, 0.035f, 0.1f), default, v(1, 1, 0.7f));
                cabeza.Pieza(Geo.Cono(0.042f, 0.18f, 3), pel, v(sx * 0.06f, 0.15f, 0), v(-0.1f, 0, -sx * 0.2f));
            }
            cabeza.Pieza(Geo.Cono(0.055f, 0.14f, 3), crin, v(0, 0.15f, 0.08f), v(0.5f, 0, 0));
            g.Pieza(Geo.Toro(0.31f, 0.022f, 4, 7), cuero, v(0, 1.13f, 0.15f), default, v(1, 1.2f, 1));
            g.Pieza(Geo.Caja(0.6f, 0.09f, 0.05f), cuero, v(0, 0.98f, 0.84f));
            var cola = g.Hijo("cola", v(0, 1.3f, -0.9f));
            cola.Pieza(Geo.Cono(0.09f, 0.85f, 4), crin, v(0, -0.4f, -0.2f), v(-0.32f, 0, 0));
            cola.Pieza(Geo.Octa(0.1f), crin, v(0, -0.85f, -0.32f), default, v(1, 1.6f, 1));
            p.cuello = cuello; p.cabeza = cabeza; p.cola = cola;
            return p;
        }

        public static Bicho Caballo(Transform padre, string color, bool calcetines)
        {
            var b = Instanciar(PlanoCaballo(color, calcetines), padre, "caballo");
            b.AnimarCaballo(0);
            return b;
        }

        // ---------- carreta de bueyes ----------
        public static Bicho Carreta(Transform padre)
        {
            var p = new Plano { raiz = new Nodo("carreta") };
            var g = p.raiz;
            const string mad = "#8a5a34", lona = "#d9c58a";
            g.Pieza(Geo.Caja(2.0f, 0.18f, 4.8f), mad, v(0, 1.55f, 0));
            foreach (var sx in new[] { -1f, 1f }) g.Pieza(Geo.Caja(0.1f, 0.5f, 4.8f), mad, v(sx * 1.0f, 1.85f, 0));
            // lona: medio cilindro acostado a lo largo del carro
            g.PiezaM(Geo.CilindroArco(1.1f, 4.4f, 8, PI), lona, Matrix4x4.Translate(v(0, 1.64f, 0)) * Matrix4x4.Rotate(Conv.Q3(v(0, 0, PI / 2))) * Matrix4x4.Rotate(Conv.Q3(v(PI / 2, 0, 0))));
            foreach (var sx in new[] { -1f, 1f })
            {
                var r = g.Hijo("rueda", v(sx * 1.2f, 1.25f, 0.2f));
                r.Pieza(Geo.Cilindro(1.25f, 1.25f, 0.16f, 12), mad, Vector3.zero, v(0, 0, PI / 2));
                for (int k = 0; k < 4; k++) r.Pieza(Geo.Caja(0.08f, 2.4f, 0.1f), mad, Vector3.zero, v(k * PI / 4, 0, 0));
                p.ruedas.Add(r);
            }
            g.Pieza(Geo.Caja(0.14f, 0.14f, 4.6f), mad, v(0, 1.0f, 3.8f));    // lanza
            g.Pieza(Geo.Caja(2.0f, 0.12f, 0.12f), mad, v(0, 1.55f, 6.0f));   // yugo
            foreach (var sx in new[] { -1f, 1f })
            {
                var b = Cuadrupedo(new Spec { L = 2.0f, A = 0.88f, pat = 0.7f, grosor = 0.42f, cuello = 0.4f, cab = 0.55f, cola = 0.7f, color = sx < 0 ? "#a65b32" : "#7a4a2a", claro = "#d8c6a4", orejas = true, cuernos = true }, "buey");
                b.raiz.pos = v(sx * 0.6f, 0, 7.3f);
                g.hijos.Add(b.raiz);
                p.bueyes.Add(b);
            }
            return Instanciar(p, padre, "carreta");
        }
    }
}
