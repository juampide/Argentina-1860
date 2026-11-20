using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Pulpería de la posta (campo.js): frente recto con frontón alto y cartel pintado, ladrillo a la vista donde cayó el revoque,
    /// alero de vigas, puertas dobles oscuras, ventanas con rejas; adentro mostrador con reja, balanza, vitrina,
    /// estantes hasta el techo, barriles, mesas con sillas, sillas colgadas, reloj y faroles.
    /// </summary>
    public class Pulperia : EdificioConInterior
    {
        public override float Ancho => W;
        public override float Largo => L;
        const float W = 22, L = 9, H = 3.6f, T = 0.4f, PUERTA = 1.8f, HD = 2.5f;
        const float iw = W / 2 - T, il = L / 2 - T;

        protected override bool Dentro(float lx, float lz) => Mathf.Abs(lx) < W / 2 - T / 2 && lz > -L / 2 && lz < L / 2 + 0.25f;

        static float Hs(int i) => MapaPago.Hs(i);

        protected override void Construir(Transform raiz)
        {
            PrepararListas();
            var g = new Nodo("pulperia");
            const string oscuro = "#3a2a1c", puertaM = "#2c2017", claro = "#b08a5c", ladri = "#9a5638", ladri2 = "#7a4630", humedad = "#6c685a", hierroM = "#26231f", blanco = "blanco";
            void Caja(Nodo par, float w, float h, float l, string m, float x, float y, float z, float esc = 1.5f, Vector3 rot = default) => par.Pieza(Geo.Caja(w, h, l, esc), m, v(x, y, z), rot);
            void Fino(Nodo par, Geo geo, string m, float x, float y, float z, Vector3 rot = default) => par.Pieza(geo, m, v(x, y, z), rot);

            // ---------- paredes (cada una se baja cuando mira a la cámara y estás adentro) ----------
            var lista = new List<(Nodo n, float nx, float nz)>();
            Nodo Pared(float nx, float nz, float x, float z, (float pw, float ph, float pl, float qx, float qy, float qz)[] piezas)
            {
                var grp = g.Hijo("pared", v(x, 0, z));
                foreach (var q in piezas) Caja(grp, q.pw, q.ph, q.pl, blanco, q.qx, q.qy, q.qz);
                lista.Add((grp, nx, nz));
                return grp;
            }
            float lado = W / 2 - PUERTA / 2;
            var frente = Pared(0, 1, 0, L / 2 - T / 2, new[] { (lado, H, T, -(PUERTA / 2 + lado / 2), H / 2, 0f), (lado, H, T, PUERTA / 2 + lado / 2, H / 2, 0f), (PUERTA, H - HD, T, 0f, HD + (H - HD) / 2, 0f) });
            var fondo = Pared(0, -1, 0, -(L / 2 - T / 2), new[] { (W, H, T, 0f, H / 2, 0f) });
            var izq = Pared(-1, 0, -(W / 2 - T / 2), 0, new[] { (T, H, L - 2 * T, 0f, H / 2, 0f) });
            var der = Pared(1, 0, W / 2 - T / 2, 0, new[] { (T, H, L - 2 * T, 0f, H / 2, 0f) });
            Obst(raiz, 0, -(L / 2 - T / 2), W, T, H); Obst(raiz, -(W / 2 - T / 2), 0, T, L, H); Obst(raiz, W / 2 - T / 2, 0, T, L, H);
            Obst(raiz, -(PUERTA / 2 + lado / 2), L / 2 - T / 2, lado, T, H); Obst(raiz, PUERTA / 2 + lado / 2, L / 2 - T / 2, lado, T, H);

            // ladrillo a la vista donde se cayó el revoque, y manchas de humedad al pie
            int sd = 7;
            void Parchear(Nodo grp, bool ejeZ, float signo, float largo, int n, bool evitarPuerta)
            {
                float off = signo * (T / 2 + 0.012f);
                for (int i = 0; i < n; i++)
                {
                    float pw = 0.35f + Hs(sd++) * 1.3f, ph = 0.3f + Hs(sd++) * 0.8f, u = (Hs(sd++) - 0.5f) * (largo - pw);
                    float y = Hs(sd++) < 0.55f ? ph / 2 + Hs(sd++) * 1.0f : ph / 2 + Hs(sd++) * (H - ph - 0.1f);
                    if (evitarPuerta && Mathf.Abs(u) < PUERTA / 2 + pw / 2 + 0.15f && y < HD + ph / 2) continue;
                    string col = Hs(sd++) < 0.5f ? ladri : ladri2;
                    if (ejeZ) Fino(grp, Geo.Caja(pw, ph, 0.03f), col, u, y, off); else Fino(grp, Geo.Caja(0.03f, ph, pw), col, off, y, u);
                }
                for (int i = 0; i < 3; i++)
                {
                    float pw = 1.5f + Hs(sd++) * 2.2f, ph = 0.35f + Hs(sd++) * 0.4f, u = (Hs(sd++) - 0.5f) * (largo - pw);
                    if (evitarPuerta && Mathf.Abs(u) < PUERTA / 2 + pw / 2) continue;
                    float o2 = signo * (T / 2 + 0.008f);
                    if (ejeZ) Fino(grp, Geo.Caja(pw, ph, 0.02f), humedad, u, ph / 2, o2); else Fino(grp, Geo.Caja(0.02f, ph, pw), humedad, o2, ph / 2, u);
                }
            }
            Parchear(frente, true, 1, W, 18, true); Parchear(izq, false, -1, L, 8, false); Parchear(der, false, 1, L, 8, false); Parchear(fondo, true, -1, W, 8, false);

            // marco y puertas dobles oscuras, abiertas hacia afuera
            foreach (var sx in new[] { -1f, 1f }) Caja(frente, 0.14f, HD, T + 0.1f, puertaM, sx * (PUERTA / 2 + 0.07f), HD / 2, 0);
            Caja(frente, PUERTA + 0.4f, 0.2f, T + 0.12f, puertaM, 0, HD + 0.05f, 0);
            foreach (var sx in new[] { -1f, 1f })
            {
                var piv = frente.Hijo("hoja", v(sx * PUERTA / 2, 0, T / 2));
                piv.rot = v(0, sx * 1.3f, 0);
                Caja(piv, 0.88f, HD - 0.05f, 0.06f, puertaM, -sx * 0.44f, (HD - 0.05f) / 2, 0);
                foreach (var y in new[] { 0.75f, 1.75f }) Caja(piv, 0.62f, y < 1 ? 0.9f : 0.55f, 0.02f, "#20170f", -sx * 0.44f, y, 0.04f);
                Fino(piv, Geo.Caja(0.05f, 0.12f, 0.05f), "#b08a3a", -sx * 0.1f, 1.1f, 0.05f);
            }
            // ventanas con rejas y postigos entornados
            foreach (var vx in new[] { -9f, -5.5f, 5.5f, 9f })
            {
                Caja(frente, 1.0f, 1.6f, 0.12f, "#14100c", vx, 1.85f, T / 2 + 0.02f);
                Caja(frente, 1.2f, 0.1f, 0.2f, claro, vx, 1.02f, T / 2 + 0.06f); Caja(frente, 1.2f, 0.1f, 0.16f, puertaM, vx, 2.7f, T / 2 + 0.05f);
                for (int i = 0; i < 5; i++) Fino(frente, Geo.Caja(0.03f, 1.6f, 0.03f), "#1a1a1a", vx - 0.4f + i * 0.2f, 1.85f, T / 2 + 0.1f);
                foreach (var sx in new[] { -1f, 1f })
                {
                    var p = frente.Hijo("postigo", v(vx + sx * 0.52f, 1.85f, T / 2 + 0.03f));
                    p.rot = v(0, -sx * 1.15f, 0);
                    Caja(p, 0.5f, 1.6f, 0.04f, "#3b2a1c", sx * 0.25f, 0, 0);
                }
            }
            foreach (var (y, c) in new[] { (1.55f, "#8a8a82"), (1.9f, "#262626") }) Fino(frente, Geo.Caja(0.34f, 0.24f, 0.02f), c, -1.7f, y, T / 2 + 0.03f);

            // ---------- techo plano con frontón alto y cartel pintado ----------
            var techoG = g.Hijo("techo");
            Caja(techoG, W + 0.5f, 0.3f, L + 0.5f, "#6b5f55", 0, H + 0.1f, 0);
            Caja(techoG, W + 0.5f, 0.55f, 0.28f, blanco, 0, H + 0.45f, -(L / 2 + 0.11f));
            foreach (var sx in new[] { -1f, 1f }) Caja(techoG, 0.28f, 0.55f, L + 0.5f, blanco, sx * (W / 2 + 0.11f), H + 0.45f, 0);
            Caja(techoG, W + 0.5f, 2.1f, 0.34f, blanco, 0, H + 1.1f, L / 2 + 0.1f);
            Caja(techoG, W + 0.8f, 0.22f, 0.66f, blanco, 0, H + 0.08f, L / 2 + 0.2f);
            Caja(techoG, W + 0.7f, 0.2f, 0.56f, blanco, 0, H + 2.2f, L / 2 + 0.15f);
            for (int i = 0; i < 9; i++)   // ladrillo a la vista en el frontón
            {
                float pw = 0.4f + Hs(sd++) * 1.3f, ph = 0.3f + Hs(sd++) * 0.5f;
                string col = Hs(sd++) < 0.5f ? ladri : ladri2;
                float x = (Hs(sd++) - 0.5f) * (W - pw), y = H + 0.5f + Hs(sd++) * 1.2f;
                Fino(techoG, Geo.Caja(pw, ph, 0.03f), col, x, y, L / 2 + 0.28f);
            }
            Fino(techoG, Geo.Cilindro(0.02f, 0.02f, 0.5f, 4), hierroM, 0, H + 0.3f, L / 2 + 0.55f, v(0, 0, Mathf.PI / 2));   // pescante del farol

            // ---------- alero de vigas sobre la vereda de ladrillo ----------
            var aleroG = g.Hijo("alero");
            float zA = L / 2 + 2.6f;
            Caja(g, W + 1.0f, 0.06f, 3.2f, "#8d6b58", 0, 0.01f, L / 2 + 1.7f);
            const int nP = 7;
            for (int i = 0; i < nP; i++) { float x = -W / 2 + 0.4f + i * (W - 0.8f) / (nP - 1); if (Mathf.Abs(x) < 1.2f) continue; Caja(aleroG, 0.14f, 3.05f, 0.14f, oscuro, x, 1.52f, zA); }
            Caja(aleroG, W + 0.4f, 0.18f, 0.2f, oscuro, 0, 3.1f, zA);
            for (float x = -W / 2 + 0.3f; x < W / 2; x += 0.9f) Caja(aleroG, 0.1f, 0.12f, 2.75f, oscuro, x, 3.27f, L / 2 + 1.3f, 1.5f, v(0.134f, 0, 0));
            Caja(aleroG, W + 0.6f, 0.07f, 2.9f, "teja", 0, 3.4f, L / 2 + 1.35f, 2, v(0.134f, 0, 0));

            // ---------- interior ----------
            Caja(g, W - 2 * T, 0.08f, L - 2 * T, "#8f6e49", 0, 0, 0, 1.4f);
            for (int i = -4; i <= 4; i++) Caja(g, 0.04f, 0.02f, L - 2 * T, oscuro, i * 2.4f, 0.045f, 0);
            const float CZ = -0.9f;   // mostrador de madera con paneles, y reja arriba
            Caja(g, 12, 1.05f, 0.9f, oscuro, 0, 0.525f, CZ); Caja(g, 12.2f, 0.07f, 1.1f, claro, 0, 1.08f, CZ); Obst(raiz, 0, CZ, 12.2f, 1.1f, 1.2f);
            for (int i = 0; i < 8; i++) { Caja(g, 1.25f, 0.75f, 0.03f, "#241911", -5.25f + i * 1.5f, 0.55f, CZ + 0.46f); Caja(g, 1.05f, 0.55f, 0.03f, "#34261a", -5.25f + i * 1.5f, 0.55f, CZ + 0.48f); }
            for (int i = 0; i <= 33; i++) { float x = -5.8f + i * (11.6f / 33); if (Mathf.Abs(x) < 1.0f) continue; Fino(g, Geo.Caja(0.04f, 1.1f, 0.04f), oscuro, x, 1.66f, CZ); }
            Caja(g, 4.8f, 0.06f, 0.08f, oscuro, -3.45f, 2.22f, CZ); Caja(g, 4.8f, 0.06f, 0.08f, oscuro, 3.45f, 2.22f, CZ);
            // balanza de hierro colorado
            {
                float bx = -4.2f, by = 1.12f;
                Fino(g, Geo.Caja(0.55f, 0.1f, 0.38f), "#6e2518", bx, by + 0.05f, CZ);
                Fino(g, Geo.Cilindro(0.12f, 0.17f, 0.5f, 8), "#8a2e1c", bx, by + 0.35f, CZ);
                Fino(g, Geo.Cilindro(0.2f, 0.2f, 0.07f, 14), "#efe6d0", bx, by + 0.62f, CZ + 0.15f, v(Mathf.PI / 2, 0, 0));
                Fino(g, Geo.Caja(0.02f, 0.15f, 0.02f), "#222222", bx, by + 0.65f, CZ + 0.2f);
                Fino(g, Geo.Caja(0.95f, 0.03f, 0.03f), hierroM, bx, by + 0.78f, CZ);
                foreach (var sx in new[] { -1f, 1f })
                {
                    Fino(g, Geo.Cilindro(0.01f, 0.01f, 0.28f, 4), hierroM, bx + sx * 0.45f, by + 0.64f, CZ);
                    Fino(g, Geo.Cilindro(0.17f, 0.15f, 0.025f, 12), "#b8a070", bx + sx * 0.45f, by + 0.5f, CZ);
                }
            }
            // vitrina de vidrio con mercadería
            {
                float vx = 3.5f, vy = 1.12f;
                Fino(g, Geo.Caja(1.8f, 0.5f, 0.7f), "vidrio", vx, vy + 0.25f, CZ);
                foreach (var dx in new[] { -0.9f, 0.9f }) foreach (var dz in new[] { -0.35f, 0.35f }) Fino(g, Geo.Caja(0.04f, 0.5f, 0.04f), oscuro, vx + dx, vy + 0.25f, CZ + dz);
                Caja(g, 1.84f, 0.04f, 0.74f, oscuro, vx, vy + 0.52f, CZ);
                string[] cs = { "#d9c48a", "#a8483a", "#486a8a", "#efe6d0" };
                for (int i = 0; i < 6; i++) Fino(g, Geo.Caja(0.22f, 0.16f, 0.2f), cs[i % 4], vx - 0.7f + i * 0.28f, vy + 0.1f, CZ + (i % 2 == 1 ? 0.12f : -0.12f));
            }
            // estantes del fondo hasta el techo, con fondo de madera oscura y mercadería a rebosar
            float fondoZ = -il + 0.28f;
            Caja(g, 16.4f, 3.4f, 0.06f, "#2a1e14", 0, 1.7f, -il + 0.03f);
            float[] NIV = { 0.45f, 1.0f, 1.55f, 2.1f, 2.65f, 3.2f };
            foreach (var y in NIV) Caja(g, 16, 0.07f, 0.5f, claro, 0, y, fondoZ);
            foreach (var sx in new[] { -8f, -4f, 0f, 4f, 8f }) Caja(g, 0.08f, 3.3f, 0.5f, claro, sx, 1.65f, fondoZ);
            {
                int q = 0;
                string[] telas = { "#a8483a", "#486a8a", "#d9d0b8", "#6a8a52", "#c9a84a" }, botellas = { "#2f6b3a", "#7a4a22", "#cfe0d4" }, latas = { "#b8b8b0", "#8a8a82", "#a8483a" }, cajas = { "#d9c48a", "#c2a46a", "#e8e0cc" };
                foreach (var y in NIV)
                    for (float x = -7.8f; x < 7.8f; x += 0.3f + Hs(q + 3) * 0.3f)
                    {
                        q++;
                        float t = MapaPago.Hs(q * 7.3);
                        bool izqZona = x < -2.2f;
                        if (izqZona && t < 0.6f) Fino(g, Geo.Cilindro(0.12f, 0.12f, 0.42f, 7), telas[q % 5], x, y + 0.2f, fondoZ, v(0, 0, Mathf.PI / 2));   // rollos de tela
                        else if (t < 0.35f) Fino(g, Geo.Cilindro(0.07f, 0.09f, 0.4f, 6), botellas[q % 3], x, y + 0.24f, fondoZ);                         // botellas
                        else if (t < 0.65f) Fino(g, Geo.Cilindro(0.1f, 0.1f, 0.18f, 7), latas[q % 3], x, y + 0.13f, fondoZ);                              // latas
                        else if (t < 0.9f) Fino(g, Geo.Caja(0.34f, 0.28f, 0.26f), cajas[q % 3], x, y + 0.18f, fondoZ);                                    // cajas y paquetes
                    }
            }
            // barriles, bolsas y cajones
            foreach (var (bx, bz, r, h) in new[] { (-9.2f, -3.1f, 0.45f, 1.0f), (-8.2f, -3.5f, 0.4f, 0.9f), (9.2f, -3.2f, 0.45f, 1.0f), (8.2f, -3.5f, 0.4f, 0.9f), (9.3f, -1.7f, 0.4f, 0.9f) })
            {
                Fino(g, Geo.Cilindro(r, r * 0.9f, h, 9), "madera", bx, h / 2, bz);
                foreach (var hy in new[] { 0.25f, 0.75f }) Fino(g, Geo.Cilindro(r * 1.02f, r * 1.02f, 0.05f, 9), "hierro", bx, h * hy, bz);
                Obst(raiz, bx, bz, r * 2, r * 2, h);
            }
            foreach (var (bx, bz, e) in new[] { (-9.0f, -0.2f, 1f), (-8.0f, 0.4f, 0.9f), (-9.1f, 1.0f, 0.95f), (-7.6f, -0.9f, 0.8f) })
            {
                g.Pieza(Geo.Ico(1, 0), "#cdb98c", v(bx, 0.3f * e, bz), default, v(0.55f * e, 0.42f * e, 0.4f * e));
                Obst(raiz, bx, bz, 0.9f, 0.7f, 0.6f);
            }
            Caja(g, 1.1f, 0.7f, 0.8f, claro, -6.6f, 0.35f, -3.5f); Caja(g, 0.9f, 0.55f, 0.7f, oscuro, -6.5f, 0.98f, -3.5f); Obst(raiz, -6.6f, -3.5f, 1.2f, 0.9f, 1.3f);
            // mesas con sillas
            void Silla(float sx, float sz, float rot, float y0 = 0)
            {
                var s = g.Hijo("silla", v(sx, y0, sz));
                s.rot = v(0, rot, 0);
                Caja(s, 0.42f, 0.05f, 0.42f, claro, 0, 0.46f, 0); Caja(s, 0.42f, 0.5f, 0.04f, oscuro, 0, 0.75f, -0.2f);
                foreach (var dx in new[] { -0.19f, 0.19f }) foreach (var dz in new[] { -0.19f, 0.19f }) Caja(s, 0.04f, 0.46f, 0.04f, oscuro, dx, 0.23f, dz);
            }
            foreach (var (mx, mz) in new[] { (7.3f, 1.1f), (7.3f, 3.0f) })
            {
                Caja(g, 1.6f, 0.07f, 1.0f, claro, mx, 0.8f, mz);
                foreach (var sx in new[] { -0.7f, 0.7f }) foreach (var sz in new[] { -0.38f, 0.38f }) Caja(g, 0.07f, 0.8f, 0.07f, oscuro, mx + sx, 0.4f, mz + sz);
                Obst(raiz, mx, mz, 1.6f, 1.0f, 0.9f);
                Silla(mx - 1.05f, mz, Mathf.PI / 2); Silla(mx + 1.05f, mz, -Mathf.PI / 2);
                Fino(g, Geo.Cilindro(0.07f, 0.06f, 0.16f, 7), "#8a4a2a", mx - 0.4f, 0.92f, mz);
            }
            // sillas colgadas de la pared izquierda, como en las pulperías de la época, y un reloj en la derecha
            for (int i = 0; i < 4; i++) Silla(-iw + 0.22f, -2.4f + i * 1.6f, Mathf.PI / 2, 1.5f);
            Fino(g, Geo.Cilindro(0.26f, 0.26f, 0.06f, 16), "#efe6d0", iw - 0.05f, 2.5f, 0.2f, v(0, 0, Mathf.PI / 2));
            Fino(g, Geo.Cilindro(0.29f, 0.29f, 0.04f, 16), oscuro, iw - 0.02f, 2.5f, 0.2f, v(0, 0, Mathf.PI / 2));
            Fino(g, Geo.Caja(0.02f, 0.2f, 0.025f), "#111111", iw - 0.1f, 2.56f, 0.2f);
            Fino(g, Geo.Caja(0.02f, 0.025f, 0.15f), "#111111", iw - 0.1f, 2.5f, 0.27f);
            // percha de apero junto a la puerta
            Caja(g, 0.1f, 1.5f, 0.1f, oscuro, -5.2f, 0.75f, 3.6f); Caja(g, 0.1f, 1.5f, 0.1f, oscuro, -3.6f, 0.75f, 3.6f); Caja(g, 1.7f, 0.1f, 0.1f, oscuro, -4.4f, 1.5f, 3.6f);
            Fino(g, Geo.Cilindro(0.28f, 0.28f, 0.9f, 7), "#6a3a22", -4.4f, 1.7f, 3.6f, v(0, 0, Mathf.PI / 2));
            Fino(g, Geo.Caja(0.9f, 0.12f, 0.6f), "#a8553a", -4.4f, 1.5f, 3.6f);
            Obst(raiz, -4.4f, 3.6f, 1.8f, 0.3f, 1.6f);
            // faroles de pared con brillo
            foreach (var lx in new[] { -6f, 0f, 6f })
            {
                Fino(g, Geo.Caja(0.06f, 0.06f, 0.3f), hierroM, lx, 2.95f, -il + 0.5f);
                Fino(g, Geo.Cilindro(0.1f, 0.12f, 0.26f, 6), "farol", lx, 2.8f, -il + 0.62f);
            }

            var mapa = new Dictionary<Nodo, Transform>();
            g.Instanciar(raiz, Registrar, mapa);
            foreach (var p in lista) paredes.Add(new Pared { t = mapa[p.n], nx = p.nx, nz = p.nz });
            ocultar.Add(mapa[techoG]);
            ocultar.Add(mapa[aleroG]);

            // cartel pintado en el frontón y tablas colgadas de la viga
            var letrero = new GameObject("letrero PULPERIA").transform;
            letrero.SetParent(mapa[techoG], false);
            letrero.localPosition = Conv.P(0, H + 1.1f, L / 2 + 0.31f);
            Cartel.Letras(letrero, "PULPERIA", 11, 11 * 180f / 1024 * 1.4f, Conv.Hex("#1f2a4a"), 0);
            foreach (var (txt, bx) in new[] { ("YERBA · CAÑA", -5.3f), ("TABACO", 5.3f) })
            {
                var t = new GameObject("cartel " + txt).transform;
                t.SetParent(mapa[aleroG], false);
                t.localPosition = Conv.P(bx, 2.45f, zA + 0.12f);
                var tabla = new Nodo("tabla");
                tabla.Pieza(Geo.Caja(2.4f, 0.6f, 0.04f), "#e8dcc0", Vector3.zero);
                tabla.Instanciar(t, Registrar);
                Cartel.Letras(t, txt, 2.2f, 0.5f, Conv.Hex("#7a1d14"), -0.035f);
            }
            // luz cálida adentro
            var luz = new GameObject("luz interior").AddComponent<Light>();
            luz.transform.SetParent(raiz, false);
            luz.transform.localPosition = Conv.P(0, 2.6f, -1.5f);
            luz.type = LightType.Point; luz.range = 12; luz.intensity = 2.2f; luz.color = new Color(1f, 0.75f, 0.45f); luz.shadows = LightShadows.None;
        }
    }
}
