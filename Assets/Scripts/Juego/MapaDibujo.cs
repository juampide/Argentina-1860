using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Mapa del pago (mapa.js): un dibujo del mundo entero (caminos, arroyo, laguna, monte, casas y corrales) y dos vistas:
    /// el minimapa, que gira junto con la cámara y marca los lugares con nombre y distancia, y el mapa grande (tecla M).
    /// Las coordenadas son las del prototipo (x este, z sur).
    /// </summary>
    public class MapaDibujo
    {
        public struct Lugar { public string n; public float x, z; }

        const float ANCHO = 3200, ALTO = 2400, ESC = 0.5f;   // el dibujo base va a 0,5 px por metro
        const int W = (int)(ANCHO * ESC), H = (int)(ALTO * ESC);
        static readonly Color32 PAPEL = new Color32(0xe2, 0xd3, 0xae, 255);
        static readonly Color TINTA = new Color(0.165f, 0.133f, 0.106f), ROJO = new Color(0.627f, 0.196f, 0.125f);

        public Texture2D Base { get; private set; }
        readonly Color32[] px = new Color32[W * H];
        Texture2D flecha, punto;
        GUIStyle sTxt, sTxtB;

        // ---------- pintor sobre el buffer (y hacia abajo, como un canvas) ----------
        void Mezclar(int x, int y, Color32 c, float a)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int k = (H - 1 - y) * W + x;
            var o = px[k];
            px[k] = new Color32((byte)(o.r + (c.r - o.r) * a), (byte)(o.g + (c.g - o.g) * a), (byte)(o.b + (c.b - o.b) * a), 255);
        }

        void Disco(float cx, float cy, float r, Color32 c, float a = 1)
        {
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r) Mezclar(x, y, c, a * Mathf.Clamp01(r - d + 0.5f));
                }
        }

        void Trazo(List<Vector2> km, float anchoM, Color32 c)
        {
            float r = Mathf.Max(0.6f, anchoM * ESC / 2);
            for (int i = 0; i + 1 < km.Count; i++)
            {
                Vector2 a = km[i] * 1000 * ESC, b = km[i + 1] * 1000 * ESC;
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f));
                for (int k = 0; k <= n; k++) { var p = Vector2.Lerp(a, b, (float)k / n); Disco(p.x, p.y, r, c); }
            }
        }

        void Linea(Vector2 a, Vector2 b, Color32 c, float ancho, float alfa = 1, float trazo = 0, float hueco = 0)
        {
            float L = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(L / 0.5f));
            for (int k = 0; k <= n; k++)
            {
                float s = L * k / n;
                if (trazo > 0 && s % (trazo + hueco) > trazo) continue;
                var p = Vector2.Lerp(a, b, (float)k / n);
                Disco(p.x, p.y, ancho / 2, c, alfa);
            }
        }

        void Elipse(float cx, float cy, float rx, float ry, Color32 c, float alfa = 1)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float e = (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry);
                    if (e <= 1) Mezclar(x, y, c, alfa);
                }
        }

        void ElipseBorde(float cx, float cy, float rx, float ry, Color32 c, float ancho)
        {
            int n = 400;
            for (int i = 0; i < n; i++)
            {
                float t = i * Mathf.PI * 2 / n;
                Disco(cx + Mathf.Cos(t) * rx, cy + Mathf.Sin(t) * ry, ancho / 2, c);
            }
        }

        void RectRotado(float cx, float cy, float w, float l, float rot, Color32 relleno, Color32 borde)
        {
            float c = Mathf.Cos(rot), s = Mathf.Sin(rot), r = Mathf.Sqrt(w * w + l * l) / 2 + 1;
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float dx = x - cx, dy = y - cy, lx = dx * c - dy * s, ly = dx * s + dy * c;   // rotación inversa (el canvas rota con -rot)
                    if (Mathf.Abs(lx) <= w / 2 && Mathf.Abs(ly) <= l / 2)
                        Mezclar(x, y, (Mathf.Abs(lx) > w / 2 - 1 || Mathf.Abs(ly) > l / 2 - 1) ? borde : relleno, 1);
                }
        }

        static Color32 C(string hex) { var c = Conv.Hex(hex); return c; }
        static Vector2 WX(float x, float z) => new Vector2((x + ANCHO / 2) * ESC, (z + ALTO / 2) * ESC);

        /// edificios: (x, z, ancho, largo, rot) y cercos: listas de puntos, todo en coordenadas del prototipo.
        public void Construir(List<(float x, float z, float w, float l, float rot)> edificios, List<List<Vector2>> cercos)
        {
            for (int i = 0; i < px.Length; i++) px[i] = PAPEL;
            // cuadrícula cada 500 m, para tener escala
            var grilla = new Color32(0x2a, 0x22, 0x1b, 255);
            for (float x = 0; x <= ANCHO; x += 500) Linea(new Vector2(x * ESC, 0), new Vector2(x * ESC, H), grilla, 1, 0.09f);
            for (float y = 0; y <= ALTO; y += 500) Linea(new Vector2(0, y * ESC), new Vector2(W, y * ESC), grilla, 1, 0.09f);
            Vector2 PK(Vector2 k) => k * 1000 * ESC;
            {
                var c = PK(MapaPago.MONTE_C);
                float rx = MapaPago.MONTE_RX * 1000 * ESC, ry = MapaPago.MONTE_RY * 1000 * ESC;
                Elipse(c.x, c.y, rx, ry, C("#a9b57e"));
                for (int i = 0; i < 90; i++) { float a = i * 2.4f, r = Mathf.Sqrt((i + 1) / 90f); Disco(c.x + Mathf.Cos(a) * rx * r * 0.9f, c.y + Mathf.Sin(a) * ry * r * 0.9f, 5, C("#8a9a62")); }
            }
            {
                var c = PK(MapaPago.LAGUNA_C);
                float rx = MapaPago.LAGUNA_RX * 1000 * ESC, ry = MapaPago.LAGUNA_RY * 1000 * ESC;
                Elipse(c.x, c.y, rx, ry, C("#9cc0c4"));
                ElipseBorde(c.x, c.y, rx, ry, C("#6d98a0"), 3);
            }
            Trazo(MapaPago.Suavizar(MapaPago.ARROYO), 9, C("#9cc0c4"));
            var camino = MapaPago.Suavizar(MapaPago.CAMINO);
            Trazo(camino, 8, C("#c3a674"));
            Trazo(camino, 5, C("#d3ba8a"));
            foreach (var s in MapaPago.SENDAS) Trazo(MapaPago.Suavizar(s), 3.6f, C("#cdb383"));
            // casas y corrales
            foreach (var e in edificios)
            {
                var c = WX(e.x, e.z);
                RectRotado(c.x, c.y, Mathf.Max(e.w * ESC, 5), Mathf.Max(e.l * ESC, 4), e.rot, C("#7b5a3c"), C("#3a2a1c"));
            }
            foreach (var pts in cercos)
                for (int i = 0; i < pts.Count; i++)
                    Linea(WX(pts[i].x, pts[i].y), WX(pts[(i + 1) % pts.Count].x, pts[(i + 1) % pts.Count].y), C("#6a4a2a"), 2, 1, 5, 3);

            Base = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, name = "Mapa del pago" };
            Base.SetPixels32(px);
            Base.Apply(true);

            // flecha y punto para las marcas
            const int F = 32;
            flecha = new Texture2D(F, F, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            punto = new Texture2D(F, F, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < F; y++)
                for (int x = 0; x < F; x++)
                {
                    // flecha apuntando hacia arriba (textura con origen abajo)
                    float u = (x + 0.5f) / F * 2 - 1, v = 1 - (y + 0.5f) / F * 2;   // v: -1 arriba, 1 abajo
                    bool dentro = DentroFlecha(u, v);
                    bool borde = !dentro && (DentroFlecha(u * 0.86f, v * 0.86f + 0.02f));
                    flecha.SetPixel(x, y, dentro ? Color.white : borde ? new Color(1, 0.96f, 0.89f, 1) * 0.0f + new Color(0, 0, 0, 0) : new Color(0, 0, 0, 0));
                    float d = Mathf.Sqrt(u * u + v * v);
                    punto.SetPixel(x, y, d < 0.8f ? Color.white : d < 1 ? new Color(1, 0.96f, 0.89f, 1) : new Color(0, 0, 0, 0));
                }
            flecha.Apply(); punto.Apply();
        }

        static bool DentroFlecha(float u, float v)
        {
            // triángulo con muesca: punta (0,-1), alas (±0.85, 0.9), muesca (0, 0.4)
            Vector2 p = new Vector2(u, v);
            return EnTri(p, new Vector2(0, -1), new Vector2(0.85f, 0.9f), new Vector2(0, 0.4f)) || EnTri(p, new Vector2(0, -1), new Vector2(0, 0.4f), new Vector2(-0.85f, 0.9f));
        }

        static bool EnTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        void Estilos()
        {
            if (sTxt != null) return;
            sTxt = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            sTxtB = new GUIStyle(sTxt) { fontStyle = FontStyle.Bold, fontSize = 12 };
        }

        void Texto(string t, float x, float y, Color col, bool bold = false, int tam = 0)
        {
            var st = bold ? sTxtB : sTxt;
            int t0 = st.fontSize;
            if (tam > 0) st.fontSize = tam;
            var size = st.CalcSize(new GUIContent(t));
            var r = new Rect(x - size.x / 2, y - size.y / 2, size.x, size.y);
            st.normal.textColor = new Color(0.886f, 0.827f, 0.682f, 0.95f);   // halo color papel
            foreach (var o in new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1) }) GUI.Label(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), t, st);
            st.normal.textColor = col;
            GUI.Label(r, t, st);
            st.fontSize = t0;
        }

        void Flecha(float x, float y, float angRad, float r, Color col)
        {
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angRad * Mathf.Rad2Deg, new Vector2(x, y));
            var c0 = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(x - r * 1.3f, y - r * 1.3f, r * 2.6f, r * 2.6f), flecha);
            GUI.color = c0;
            GUI.matrix = m;
        }

        void Punto(float x, float y, float r, Color col)
        {
            var c0 = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(x - r, y - r, 2 * r, 2 * r), punto);
            GUI.color = c0;
        }

        static string Dist(float d) => d >= 1000 ? (d / 1000).ToString("0.0") + " km" : (Conv.RoundJS(d / 10) * 10) + " m";

        /// Minimapa en el rectángulo dado. rumbo: hacia dónde mira el jugador; yaw: giro de la cámara (ambos en la convención del prototipo).
        public void DibujarMini(Rect area, float px, float pz, float rumbo, float yaw, List<Lugar> lugares, Lugar? destino)
        {
            if (Base == null) return;
            Estilos();
            float S = area.width, esc = 0.6f, k = esc / ESC;
            float a = Mathf.PI / 2 - yaw, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            GUI.BeginGroup(area);
            GUI.DrawTexture(new Rect(0, 0, S, S), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.886f, 0.827f, 0.682f), 0, 0);
            var m0 = GUI.matrix;
            var centro = new Vector2(S / 2, S / 2);
            GUIUtility.RotateAroundPivot(a * Mathf.Rad2Deg, centro);
            float D = S * 1.5f, span = D / k;
            var pm = WX(px, pz);
            var tc = new Rect((pm.x - span / 2) / W, 1 - (pm.y + span / 2) / H, span / W, span / H);
            GUI.DrawTextureWithTexCoords(new Rect(centro.x - D / 2, centro.y - D / 2, D, D), Base, tc);
            GUI.matrix = m0;

            Vector2 Pant(float x, float z) { float dx = x - px, dz = z - pz; return new Vector2((dx * ca - dz * sa) * esc, (dx * sa + dz * ca) * esc); }
            float M = S / 2 - 12;
            var ocupado = new List<Rect>();
            void Marca(Lugar l, bool esDest)
            {
                var sp = Pant(l.x, l.z);
                float d = Mathf.Sqrt((l.x - px) * (l.x - px) + (l.z - pz) * (l.z - pz));
                bool fuera = Mathf.Abs(sp.x) > M || Mathf.Abs(sp.y) > M;
                float X = sp.x, Y = sp.y;
                if (fuera) { float f = M / Mathf.Max(Mathf.Abs(sp.x), Mathf.Abs(sp.y)); X *= f; Y *= f; }
                X += S / 2; Y += S / 2;
                var col = esDest ? ROJO : new Color(0.227f, 0.165f, 0.11f);
                string txt = l.n + (fuera || esDest ? " · " + Dist(d) : "");
                if (fuera) Flecha(X, Y, Mathf.Atan2(sp.x, -sp.y), 6, col); else Punto(X, Y, esDest ? 6 : 4, col);
                var st = esDest ? sTxtB : sTxt;
                var tam = st.CalcSize(new GUIContent(txt));
                float tw = tam.x / 2 + 4;
                float tx = Mathf.Clamp(X, tw, S - tw), ty = Mathf.Clamp(fuera ? (Y < S / 2 ? Y + 14 : Y - 14) : Y - 12, 9, S - 9);
                var caja = new Rect(tx - tw, ty - 8, tw * 2, 16);
                if (!esDest && ocupado.Exists(o => o.Overlaps(caja))) return;
                ocupado.Add(caja);
                Texto(txt, tx, ty, col, esDest);
            }
            if (destino.HasValue) Marca(destino.Value, true);
            var orden = new List<Lugar>(lugares);
            orden.Sort((p, q) => ((p.x - px) * (p.x - px) + (p.z - pz) * (p.z - pz)).CompareTo((q.x - px) * (q.x - px) + (q.z - pz) * (q.z - pz)));
            foreach (var l in orden)
                if (!destino.HasValue || Mathf.Sqrt((l.x - destino.Value.x) * (l.x - destino.Value.x) + (l.z - destino.Value.z) * (l.z - destino.Value.z)) > 12) Marca(l, false);
            Flecha(S / 2, S / 2, Mathf.PI - rumbo + a, 8, TINTA);   // vos, mirando hacia donde caminás
            float nx = sa, ny = -ca;                               // el norte (z negativa del prototipo) en pantalla
            Texto("N", S / 2 + nx * (S / 2 - 16), S / 2 + ny * (S / 2 - 16), ROJO, true, 13);
            GUI.EndGroup();
        }

        /// Mapa grande con todo el pago a la vista.
        public void DibujarGrande(Rect area, float px, float pz, float rumbo, float yaw, List<Lugar> lugares, Lugar? destino, List<Vector2> personas)
        {
            if (Base == null) return;
            Estilos();
            GUI.BeginGroup(area);
            float eg = area.width / ANCHO;
            GUI.DrawTexture(new Rect(0, 0, area.width, area.height), Base);
            Vector2 P(float x, float z) => new Vector2((x + ANCHO / 2) * eg, (z + ALTO / 2) * eg);
            foreach (var l in lugares) { var q = P(l.x, l.z); Punto(q.x, q.y, 4, new Color(0.227f, 0.165f, 0.11f)); Texto(l.n, q.x, q.y - 13, TINTA, false, 13); }
            foreach (var n in personas) { var q = P(n.x, n.y); Punto(q.x, q.y, 2.5f, new Color(0.353f, 0.416f, 0.541f)); }
            if (destino.HasValue)
            {
                var q = P(destino.Value.x, destino.Value.z);
                Punto(q.x, q.y, 9, ROJO); Punto(q.x, q.y, 5, ROJO);
                Texto("Objetivo: " + destino.Value.n, q.x, q.y + 22, ROJO, true, 14);
            }
            var yo = P(px, pz);
            float fx = -Mathf.Cos(yaw), fz = -Mathf.Sin(yaw);   // hacia dónde mira la cámara (arriba en pantalla)
            for (int i = 1; i < 12; i += 2) Punto(yo.x + fx * i * 6, yo.y + fz * i * 6, 1.2f, new Color(0.165f, 0.133f, 0.106f, 0.5f));
            Texto("arriba en pantalla", yo.x + fx * 95, yo.y + fz * 95, new Color(0.165f, 0.133f, 0.106f, 0.75f), false, 11);
            Flecha(yo.x, yo.y, Mathf.PI - rumbo, 9, TINTA);
            Texto("Vos", yo.x, yo.y + 24, TINTA, true, 14);
            Texto("N", area.width - 24, 22, ROJO, true, 18);
            GUI.DrawTexture(new Rect(18, area.height - 22, 500 * eg, 3), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, TINTA, 0, 0);
            Texto("500 m", 18 + 250 * eg, area.height - 34, TINTA, false, 12);
            GUI.EndGroup();
        }
    }
}
