using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Rancho con interior (habitacion() + amueblarRancho() de campo.js): paredes de adobe con puerta y ventana,
    /// techo de paja, y adentro fogón con campana y olla, catre, mesa con banquito, tinaja, baúl y herramientas colgadas.
    /// </summary>
    public class Habitacion : EdificioConInterior
    {
        [Min(3)] public float ancho = 7;
        [Min(3)] public float largo = 4.5f;
        [Min(2)] public float alto = 2.6f;
        public float caida = 2.2f;
        public bool amueblado = true;

        public override float Ancho => ancho;
        public override float Largo => largo;
        const float T = 0.35f, PU = 1.4f;

        protected override bool Dentro(float lx, float lz) => Mathf.Abs(lx) < ancho / 2 - T / 2 && lz > -largo / 2 && lz < largo / 2 + 0.25f;

        protected override void Construir(Transform raiz)
        {
            PrepararListas();
            float w = ancho, l = largo, h = alto, lado = w / 2 - PU / 2;
            var g = new Nodo("rancho");
            var tm = Techo(g, w, l, h, caida, "paja");
            var lista = new List<(Nodo n, float nx, float nz)>();

            Nodo MkPared(float nx, float nz, float px, float pz, (float pw, float ph, float pl, float qx, float qy, float qz)[] piezas, (float len, float qx, bool horiz)[] vara)
            {
                var grp = g.Hijo("pared", v(px, 0, pz));
                foreach (var q in piezas) grp.Pieza(Geo.Caja(q.pw, q.ph, q.pl, 3), "adobe", v(q.qx, q.qy, q.qz));
                foreach (var vr in vara)
                    foreach (var y in new[] { 0.75f, 1.65f })
                    {
                        if (y > h - 0.3f) continue;
                        var geo = vr.horiz ? Geo.Caja(vr.len, 0.05f, 0.05f) : Geo.Caja(0.05f, 0.05f, vr.len);
                        grp.Pieza(geo, "vara", vr.horiz ? v(vr.qx, y, (nz > 0 ? 1 : -1) * (T / 2 + 0.03f)) : v((nx > 0 ? 1 : -1) * (T / 2 + 0.03f), y, vr.qx));
                    }
                lista.Add((grp, nx, nz));
                return grp;
            }

            var frente = MkPared(0, 1, 0, l / 2 - T / 2,
                new[] { (lado, h, T, -(PU / 2 + lado / 2), h / 2, 0f), (lado, h, T, PU / 2 + lado / 2, h / 2, 0f), (PU, h - 2.0f, T, 0f, 2.0f + (h - 2.0f) / 2, 0f) },
                new[] { (lado, -(PU / 2 + lado / 2), true), (lado, PU / 2 + lado / 2, true) });
            frente.Pieza(Geo.Caja(0.55f, 0.6f, 0.1f, 3), "oscuro", v(PU / 2 + lado * 0.5f, 1.5f, T / 2 + 0.03f));
            frente.Pieza(Geo.Caja(0.6f, 0.64f, 0.06f, 1.5f), "madera", v(PU / 2 + lado * 0.5f, 1.5f, T / 2 + 0.07f));
            MkPared(0, -1, 0, -(l / 2 - T / 2), new[] { (w, h, T, 0f, h / 2, 0f) }, new[] { (w, 0f, true) });
            MkPared(-1, 0, -(w / 2 - T / 2), 0, new[] { (T, h, l - 2 * T, 0f, h / 2, 0f) }, new[] { (l - 2 * T, 0f, false) });
            MkPared(1, 0, w / 2 - T / 2, 0, new[] { (T, h, l - 2 * T, 0f, h / 2, 0f) }, new[] { (l - 2 * T, 0f, false) });
            Obst(raiz, 0, -(l / 2 - T / 2), w, T); Obst(raiz, -(w / 2 - T / 2), 0, T, l); Obst(raiz, w / 2 - T / 2, 0, T, l);
            Obst(raiz, -(PU / 2 + lado / 2), l / 2 - T / 2, lado, T); Obst(raiz, PU / 2 + lado / 2, l / 2 - T / 2, lado, T);

            g.Pieza(Geo.Caja(w - 2 * T, 0.08f, l - 2 * T, 2), "#7a6246", Vector3.zero);   // piso de tierra apisonada
            if (amueblado) Amueblar(g, raiz);

            var mapa = new Dictionary<Nodo, Transform>();
            g.Instanciar(raiz, Registrar, mapa);
            foreach (var p in lista) paredes.Add(new Pared { t = mapa[p.n], nx = p.nx, nz = p.nz });
            ocultar.Add(mapa[tm]);
        }

        void Amueblar(Nodo g, Transform raiz)
        {
            const string oscuro = "#3a2a1c", cal = "#e9e2cf", hierro = "#26231f", barro = "#a8663a";
            void Caja(float bw, float bh, float bd, string m, float x, float y, float z, float esc = 1.5f) => g.Pieza(Geo.Caja(bw, bh, bd, esc), m, v(x, y, z));
            // fogón con campana y olla colgada, como en las fotos
            Caja(1.5f, 0.8f, 0.9f, cal, -2.35f, 0.4f, -1.45f, 2); Caja(1.2f, 0.05f, 0.7f, "#3a3530", -2.35f, 0.82f, -1.45f);
            g.Pieza(Geo.Cilindro(0.3f, 0.75f, 0.8f, 4), cal, v(-2.35f, 1.4f, -1.55f), v(0, Mathf.PI / 4, 0));
            Caja(0.4f, 1.2f, 0.4f, cal, -2.35f, 2.2f, -1.65f, 2);
            g.Pieza(Geo.Cilindro(0.22f, 0.22f, 0.01f, 8), "llama", v(-2.35f, 0.86f, -1.45f));
            g.Pieza(Geo.Cilindro(0.012f, 0.012f, 0.8f, 4), hierro, v(-2.35f, 1.5f, -1.45f));
            g.Pieza(Geo.Cilindro(0.26f, 0.2f, 0.28f, 8), hierro, v(-2.35f, 1.0f, -1.45f));
            Obst(raiz, -2.35f, -1.45f, 1.5f, 0.9f);
            // catre con manta, mesa con banquito, tinaja y baúl
            Caja(1.9f, 0.12f, 0.9f, oscuro, 2.2f, 0.38f, -1.35f);
            foreach (var sx in new[] { -0.85f, 0.85f }) foreach (var sz in new[] { -0.4f, 0.4f }) Caja(0.08f, 0.38f, 0.08f, oscuro, 2.2f + sx, 0.19f, -1.35f + sz);
            Caja(1.8f, 0.1f, 0.82f, "#a8553a", 2.2f, 0.49f, -1.35f); Caja(0.5f, 0.12f, 0.6f, "#d9cfb2", 2.9f, 0.58f, -1.35f); Obst(raiz, 2.2f, -1.35f, 1.95f, 0.95f);
            Caja(1.1f, 0.07f, 0.7f, "#b08a5c", 0.4f, 0.78f, -1.3f);
            foreach (var sx in new[] { -0.45f, 0.45f }) foreach (var sz in new[] { -0.28f, 0.28f }) Caja(0.07f, 0.78f, 0.07f, oscuro, 0.4f + sx, 0.39f, -1.3f + sz);
            Obst(raiz, 0.4f, -1.3f, 1.1f, 0.7f); Caja(0.45f, 0.4f, 0.35f, "#b08a5c", 0.4f, 0.2f, -0.5f); Obst(raiz, 0.4f, -0.5f, 0.45f, 0.35f);
            g.Pieza(Geo.Cilindro(0.05f, 0.04f, 0.1f, 6), "#6a8a52", v(0.2f, 0.86f, -1.3f));     // mate
            g.Pieza(Geo.Cilindro(0.1f, 0.13f, 0.18f, 7), hierro, v(0.65f, 0.88f, -1.3f));       // pava
            g.Pieza(Geo.Cilindro(0.3f, 0.22f, 0.72f, 9), barro, v(-0.95f, 0.36f, -1.65f)); Obst(raiz, -0.95f, -1.65f, 0.6f, 0.6f);   // tinaja
            Caja(0.85f, 0.5f, 0.5f, "#7a4a2a", 2.7f, 0.25f, 1.4f); Obst(raiz, 2.7f, 1.4f, 0.85f, 0.5f);                              // baúl
            // palas, cucharón y ristra colgados de la pared del fondo
            foreach (var (hx, hy) in new[] { (-0.3f, 1.5f), (0.1f, 1.6f), (1.0f, 1.7f) }) { Caja(0.18f, 0.4f, 0.03f, "#8a6a44", hx, hy, -1.95f); Caja(0.04f, 0.5f, 0.04f, oscuro, hx, hy + 0.4f, -1.95f); }
            g.Pieza(Geo.Cono(0.12f, 0.7f, 5), "#b3342b", v(1.7f, 1.9f, -1.9f), v(Mathf.PI, 0, 0));
        }
    }
}
