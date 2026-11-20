using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Rearma el terreno del pago a partir de MapaPago y del loteo (LoteoPago):
    /// - Relieve: las lomas del prototipo, más lomadas chicas, partes desparejas y bajos; los caminos gastados
    ///   quedan más hondos y las zanjas de los lotes se cavan del lado de afuera. No toca los sitios con casas,
    ///   el agua ni los puntos protegidos (piezas armadas a mano).
    /// - Suelo: la pintura del prototipo (pasto, pasto seco, tierra en caminos y patios, agua) más el suelo gastado
    ///   donde hay gente y animales.
    /// - Árboles: monte de abrigo alrededor de las casas, quinta en filas, matorral solo en el campo sin uso.
    ///   Saca los árboles que caen en caminos, lotes sembrados y patios.
    /// Se usa desde el editor (con el MCP o un menú). Pisa lo que se haya esculpido o pintado a mano en el terreno.
    /// </summary>
    public static class RelievePago
    {
        static float SStep(float a, float b, float x) => MapaPago.SStep(a, b, x);
        static float N(double x, double z) => (MapaPago.Ruido(x, z) - 0.5f) * 2;

        struct Borde { public Vector2 a, b, nAfuera; public List<Vector2> tranq; }

        static List<Borde> BordesConZanja()
        {
            var r = new List<Borde>();
            var l = LoteoPago.Instancia;
            if (l == null) return r;
            foreach (var lote in l.lotes)
            {
                if (lote.km == null || lote.km.Length < 3 || (lote.linde != Linde.Zanja && lote.linde != Linde.CercoVivo)) continue;
                var poli = new List<Vector2>();
                foreach (var k in lote.km) poli.Add(MapaPago.K(k));
                var tranq = new List<Vector2>();
                if (lote.tranqueras != null) foreach (var k in lote.tranqueras) tranq.Add(MapaPago.K(k));
                float area = 0;
                for (int i = 0, j = poli.Count - 1; i < poli.Count; j = i++) area += (poli[j].x - poli[i].x) * (poli[j].y + poli[i].y);
                float adentro = area > 0 ? 1 : -1;
                for (int i = 0; i < poli.Count; i++)
                {
                    var a = poli[i]; var b = poli[(i + 1) % poli.Count];
                    var d = (b - a).normalized;
                    r.Add(new Borde { a = a, b = b, nAfuera = -new Vector2(-d.y, d.x) * adentro, tranq = tranq });
                }
            }
            return r;
        }

        /// Hondura de la zanja en un punto (0 si no hay): la zanja corre por afuera del borde del lote.
        static float Zanja(List<Borde> bordes, float x, float z)
        {
            var p = new Vector2(x, z);
            float h = 0;
            foreach (var bo in bordes)
            {
                var ab = bo.b - bo.a; float l2 = ab.sqrMagnitude; if (l2 < 1e-6f) continue;
                float u = Vector2.Dot(p - bo.a, ab) / l2;
                if (u < -0.01f || u > 1.01f) continue;
                var q = bo.a + ab * Mathf.Clamp01(u);
                float lado = Vector2.Dot(p - q, bo.nAfuera);   // positivo: afuera del lote
                float dist = (p - q).magnitude;
                if (dist > 5) continue;
                bool hueco = false;
                foreach (var t in bo.tranq) if (Vector2.Distance(q, t) < 5) hueco = true;
                if (hueco) continue;
                float centro = Mathf.Abs(lado - 1.5f);   // el eje de la zanja, a 1,5 m del borde hacia afuera
                h = Mathf.Max(h, 0.55f * (1 - SStep(0.5f, 3.2f, centro)));
            }
            return h;
        }

        public static void Relieve(Terrain t, IList<Vector2> protegidos)
        {
            var td = t.terrainData; var pos = t.transform.position; var size = td.size;
            var bordes = BordesConZanja();
            int res = td.heightmapResolution;
            var h = new float[res, res];
            for (int i = 0; i < res; i++)
                for (int j = 0; j < res; j++)
                {
                    float x = pos.x + (float)j / (res - 1) * size.x, z = -(pos.z + (float)i / (res - 1) * size.z);
                    float alto = MapaPago.Altura(x, z);
                    float w = Proteccion(x, z, protegidos);
                    if (w > 0)
                    {
                        float d = 0.32f * N(x * 0.035 + 21, z * 0.035 - 13)          // lomadas chicas
                                + 0.14f * N(x * 0.085 + 7, z * 0.085 + 3)             // desparejo
                                - 0.5f * SStep(0.66f, 0.86f, MapaPago.Ruido(x * 0.011 + 40, z * 0.011 - 9));   // bajos
                        alto += d * w;
                    }
                    if (!MapaPago.EnAgua(x, z))
                    {
                        float dc = MapaPago.DistCamino(x, z);
                        if (dc < 5) alto -= 0.38f * (1 - SStep(0.5f, 5f, dc)) * Mathf.Max(w, 0.35f);   // el camino gastado queda más hondo
                        else alto -= Zanja(bordes, x, z);
                    }
                    h[i, j] = Mathf.Clamp01((alto - pos.y) / size.y);
                }
            td.SetHeights(0, 0, h);
        }

        static float Proteccion(float x, float z, IList<Vector2> protegidos)
        {
            float w = 1;
            var p = new Vector2(x, z);
            foreach (var s in MapaPago.SITIOS) w = Mathf.Min(w, SStep(s.r * 1.0f, s.r * 1.9f, Vector2.Distance(p, s.p)));
            if (protegidos != null)
                foreach (var f in protegidos)
                {
                    float dx = Mathf.Abs(f.x - x), dz = Mathf.Abs(f.y - z);
                    if (dx < 14 && dz < 14) w = Mathf.Min(w, SStep(5, 13, Mathf.Sqrt(dx * dx + dz * dz)));
                }
            if (MapaPago.EnAgua(x, z) || MapaPago.EnAgua(x + 7, z) || MapaPago.EnAgua(x - 7, z) || MapaPago.EnAgua(x, z + 7) || MapaPago.EnAgua(x, z - 7)) w = 0;
            return w;
        }

        public static void Suelo(Terrain t)
        {
            var td = t.terrainData; var pos = t.transform.position; var size = td.size;
            var bordes = BordesConZanja();
            int ar = td.alphamapResolution;
            var a = new float[ar, ar, 4];   // pasto verde, pasto seco, tierra, agua
            for (int i = 0; i < ar; i++)
                for (int j = 0; j < ar; j++)
                {
                    float x = pos.x + (j + 0.5f) / ar * size.x, z = -(pos.z + (i + 0.5f) / ar * size.z);
                    var w = MapaPago.PesosSuelo(x, z);
                    float p = MapaPago.Pastoreo(x, z);
                    if (p > 0.05f && w.w < 0.3f)
                    {
                        // manchones pelados por los animales y pasto pisoteado y seco
                        float pelado = SStep(0.55f, 0.8f, MapaPago.Ruido(x * 0.09 + 5, z * 0.09 + 17)) * p * 0.75f;
                        float seco = SStep(0.4f, 0.7f, MapaPago.Ruido(x * 0.04 - 3, z * 0.04 + 8)) * p * 0.5f;
                        w *= 1 - seco; w.y += seco;
                        w *= 1 - pelado; w.z += pelado;
                    }
                    if (w.w < 0.3f && MapaPago.DistCamino(x, z) > 3)
                    {
                        float zj = Zanja(bordes, x, z);   // el fondo de la zanja es tierra húmeda
                        if (zj > 0.15f) { float f = Mathf.Clamp01(zj * 1.5f); w *= 1 - f; w.z += f; }
                    }
                    a[i, j, 0] = w.x; a[i, j, 1] = w.y; a[i, j, 2] = w.z; a[i, j, 3] = w.w;
                }
            td.SetAlphamaps(0, 0, a);
        }

        // ---------- árboles ----------
        static float H(double i) => MapaPago.Hs(i * 3.17 + 911);

        /// Un grupo de casas con su monte de abrigo.
        public struct Casa
        {
            public Vector2 centro;      // metros del prototipo
            public Vector2 frente;      // hacia dónde mira la entrada (ahí no se plantan árboles)
            public float radio;         // dónde empieza el monte, en metros
            public int filas;           // filas de la cortina
        }

        public static int Arboles(Terrain t, IList<Casa> casas)
        {
            var td = t.terrainData;
            var protos = td.treePrototypes;
            int Idx(string n) { for (int i = 0; i < protos.Length; i++) if (protos[i].prefab != null && protos[i].prefab.name == n) return i; return -1; }
            int ombu = Idx("ombu"), tala = Idx("tala"), espinillo = Idx("espinillo"), coronillo = Idx("coronillo"), sauce = Idx("sauce"), cortadera = Idx("cortadera"), ceibo = Idx("ceibo");
            var lista = new List<TreeInstance>();
            var ocupado = new HashSet<long>();
            long Celda(float x, float z) => ((long)Mathf.FloorToInt(x / 2f) << 32) ^ (uint)Mathf.FloorToInt(z / 2f);

            bool Prohibido(float x, float z)
            {
                if (!MapaPago.PuedeCrecer(x, z) || MapaPago.DistCamino(x, z) < 3 || Sembrado.Ocupa(x, z)) return true;
                foreach (var pt in MapaPago.Patios) if (Vector2.Distance(pt, new Vector2(x, z)) < 12) return true;
                var u = MapaPago.Uso(x, z);
                if (u == UsoLote.Chacra || u == UsoLote.Potrero) return true;
                return false;
            }

            bool Libre(float x, float z, float radio)
            {
                if (Prohibido(x, z)) return false;
                int rc = Mathf.CeilToInt(radio / 2f);
                for (int dx = -rc; dx <= rc; dx++) for (int dz = -rc; dz <= rc; dz++) if (ocupado.Contains(Celda(x + dx * 2, z + dz * 2))) return false;
                var w = new Vector3(x, Terreno.Altura(new Vector3(x, 0, -z)) + 2, -z);
                foreach (var c in Physics.OverlapSphere(w, radio * 0.8f)) if (!(c is TerrainCollider)) return false;
                return true;
            }

            void Poner(int proto, float x, float z, float escala, double sem)
            {
                if (proto < 0) return;
                var w = new Vector3(x, 0, -z);
                var n = new Vector3((w.x - t.transform.position.x) / td.size.x, t.SampleHeight(w) / td.size.y, (w.z - t.transform.position.z) / td.size.z);
                float tono = 0.82f + 0.18f * H(sem + 5);
                lista.Add(new TreeInstance { prototypeIndex = proto, position = n, widthScale = escala * (0.9f + 0.2f * H(sem + 1)), heightScale = escala * (0.85f + 0.3f * H(sem + 2)), rotation = H(sem + 3) * Mathf.PI * 2, color = new Color(tono, tono, tono, 1), lightmapColor = Color.white });
                ocupado.Add(Celda(x, z));
            }

            // 1. los que ya estaban y quedan bien (monte, orillas, pajonal); alrededor de las casas se replanta todo
            foreach (var a in td.treeInstances)
            {
                var w = Vector3.Scale(a.position, td.size) + t.transform.position;
                float x = w.x, z = -w.z;
                if (Prohibido(x, z)) continue;
                bool cerca = false;
                foreach (var c in casas) if (Vector2.Distance(c.centro, new Vector2(x, z)) < c.radio + c.filas * 8 + 25) cerca = true;
                if (cerca) continue;
                var u = MapaPago.Uso(x, z);
                bool esMata = a.prototypeIndex == espinillo || a.prototypeIndex == coronillo || a.prototypeIndex == cortadera;
                if (esMata && (u == UsoLote.Casco || u == UsoLote.Posta || u == UsoLote.Quinta)) continue;
                if (esMata && u == UsoLote.CampoAbierto && MapaPago.Pastoreo(x, z) > 0.3f) continue;
                var b = a; b.position.y = t.SampleHeight(w) / td.size.y;
                lista.Add(b);
                ocupado.Add(Celda(x, z));
            }
            int antes = lista.Count;

            // 2. monte de abrigo: cortina tupida de varias filas que rodea las casas y deja libre el frente,
            //    con algún ombú grande en el patio para la sombra
            int k0 = 0;
            foreach (var c in casas)
            {
                var fr = c.frente.sqrMagnitude > 0 ? c.frente.normalized : Vector2.up;
                for (int fila = 0; fila < c.filas; fila++)
                {
                    float r = c.radio + fila * 6.5f;
                    int n = Mathf.CeilToInt(2 * Mathf.PI * r / 5.5f);
                    for (int i = 0; i < n; i++)
                    {
                        double sem = k0 * 100003 + fila * 1009 + i;
                        float ang = (i + H(sem) * 0.6f) / n * Mathf.PI * 2;
                        var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                        float alFrente = Vector2.Dot(dir, fr);
                        if (alFrente > 0.55f) continue;   // el frente queda abierto
                        // más filas del lado del pampero (sudoeste: x negativo, z positivo en el plano)
                        float delPampero = Vector2.Dot(dir, new Vector2(-0.7f, 0.7f));
                        if (fila >= c.filas - 1 && delPampero < 0.1f && H(sem + 3) < 0.6f) continue;
                        float rr = r + (H(sem + 7) - 0.5f) * 4;
                        float x = c.centro.x + dir.x * rr, z = c.centro.y + dir.y * rr;
                        if (!Libre(x, z, 2.6f)) continue;
                        bool agua = MapaPago.EnAgua(x + 30, z) || MapaPago.EnAgua(x - 30, z) || MapaPago.EnAgua(x, z + 30) || MapaPago.EnAgua(x, z - 30);
                        float q = H(sem + 13);
                        int p = q < 0.07f ? ombu : agua && q < 0.5f ? sauce : q < 0.14f ? ceibo : tala;
                        float esc = p == ombu ? 1.3f : 1.55f + 0.6f * H(sem + 4);   // copas grandes que se tocan: una masa de monte
                        Poner(p, x, z, esc, sem);
                    }
                }
                // sombra en el patio: uno o dos ombúes a un costado de las casas
                for (int k = 0; k < 2; k++)
                {
                    double sem = k0 * 77 + k;
                    var lado = new Vector2(-fr.y, fr.x) * (k == 0 ? 1 : -1);
                    var q = c.centro + lado * c.radio * 0.6f + fr * c.radio * 0.2f;
                    if (Libre(q.x, q.y, 7)) Poner(ombu, q.x, q.y, 1.25f + 0.2f * H(sem), sem);
                }
                k0++;
            }

            // 3. quintas: frutales en filas (talas chicos hacen de durazneros y membrillos)
            var l = LoteoPago.Instancia;
            if (l != null)
                foreach (var q in l.lotes)
                {
                    if (q.uso != UsoLote.Quinta || q.km == null) continue;
                    var mn = new Vector2(1e9f, 1e9f); var mx = new Vector2(-1e9f, -1e9f);
                    foreach (var k in q.km) { var m = MapaPago.K(k); mn = Vector2.Min(mn, m); mx = Vector2.Max(mx, m); }
                    for (float x = mn.x + 8; x < mx.x - 6; x += 7)
                        for (float z = mn.y + 8; z < mx.y - 6; z += 6)
                        {
                            var kk = new Vector2(x / 1000f + 1.6f, z / 1000f + 1.2f);
                            if (!LoteoPago.Dentro(kk, q.km) || H(x * 3.1 + z) < 0.12f) continue;
                            float jx = x + (H(x + z * 2) - 0.5f) * 1.2f, jz = z + (H(z + x * 2) - 0.5f) * 1.2f;
                            if (MapaPago.DistCamino(jx, jz) < 3) continue;
                            Poner(tala, jx, jz, 0.42f + 0.12f * H(x * z), x * 7 + z);
                        }
                }

            // 4. matorral: solo en el campo sin uso y lejos de la gente
            for (float x = -1560; x < 1560; x += 22)
                for (float z = -1160; z < 1160; z += 22)
                {
                    double sem = x * 7.13 + z * 3.71;
                    float jx = x + (H(sem) - 0.5f) * 20, jz = z + (H(sem + 1) - 0.5f) * 20;
                    var u = MapaPago.Uso(jx, jz);
                    if ((u != UsoLote.CampoAbierto && u != UsoLote.Monte && u != UsoLote.SinOcupar) || MapaPago.Pastoreo(jx, jz, 2.2f) > 0.02f) continue;
                    float n = MapaPago.Ruido(jx * 0.004 + 70, jz * 0.004 - 30);
                    if (n < 0.55f || H(sem + 2) > (n - 0.55f) * 2.5f) continue;
                    int grupo = 2 + (int)(H(sem + 3) * 6);
                    for (int k = 0; k < grupo; k++)
                    {
                        float px = jx + (H(sem + 10 + k) - 0.5f) * 14, pz = jz + (H(sem + 20 + k) - 0.5f) * 14;
                        if (!Libre(px, pz, 1.6f)) continue;
                        float q = H(sem + 30 + k);
                        int p = q < 0.4f ? espinillo : q < 0.6f ? coronillo : q < 0.92f ? cortadera : tala;
                        float esc = p == cortadera ? 0.8f + 0.4f * H(sem + 40 + k) : p == tala ? 0.55f + 0.2f * H(sem + 40 + k) : 0.45f + 0.35f * H(sem + 40 + k);
                        Poner(p, px, pz, esc, sem + 50 + k);
                    }
                }

            td.SetTreeInstances(lista.ToArray(), true);
            return lista.Count - antes;
        }
    }
}
