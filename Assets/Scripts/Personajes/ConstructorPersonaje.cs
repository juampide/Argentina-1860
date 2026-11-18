using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// Arma el personaje low poly a partir de su Aspecto, pieza por pieza como crearPersonaje() de personaje.js:
    /// formas angulares, piernas flacas con botas grandes, bloques de color en el tronco y sombreros con silueta clara.
    /// </summary>
    public static class ConstructorPersonaje
    {
        static Vector3 v(float x, float y, float z) => new Vector3(x, y, z);
        static Vector3 S(float s) => new Vector3(s, s, s);
        const float PI = Mathf.PI;

        public static AnimPersonaje Construir(Transform padre, Aspecto o, Func<Mesh, Mesh> registrar)
        {
            string C(Color c) => Conv.Clave(c);
            string piel = C(o.piel), pielOsc = C(Conv.Aclara(o.piel, 0.82f)), pelo = C(o.pelo);
            const string negro = "#1f1a17", cuero = "#6b4528", crema = "#f4eedc", oro = "#d6ac3f";
            string ropa = C(o.ropa), ropa2 = C(o.ropa2);
            var estilo = o.estilo;

            var g = new Nodo("modelo") { esc = S(0.92f * o.escala) };
            var cuerpo = g.Hijo("cuerpo");
            bool conPiernas = estilo == Estilo.Levita || estilo == Estilo.Poncho || estilo == Estilo.Pregonero;
            var piernas = new List<Nodo>(); var rodillas = new List<Nodo>(); var brazos = new List<Nodo>(); var codos = new List<Nodo>();
            const float CAD = 0.86f;

            Geo Tronco(float rs, float ri, float h, int seg = 6, bool abierto = false) => Geo.Cilindro(rs, ri, h, seg, abierto);

            // ---------- piernas flacas y botas grandes ----------
            if (conPiernas)
                foreach (var s in new[] { -1f, 1f })
                {
                    var cad = cuerpo.Hijo(s < 0 ? "cadera_I" : "cadera_D", v(s * 0.085f, CAD, 0));
                    bool gaucho = estilo == Estilo.Poncho;
                    string mP = gaucho ? "#efe6d0" : C(o.pantalon);
                    cad.Pieza(Tronco(gaucho ? 0.1f : 0.072f, gaucho ? 0.09f : 0.056f, 0.44f, 5), mP, v(0, -0.2f, 0));
                    var rod = cad.Hijo("rodilla", v(0, -0.42f, 0));
                    rod.Pieza(Tronco(gaucho ? 0.088f : 0.056f, gaucho ? 0.07f : 0.046f, 0.4f, 5), mP, v(0, -0.18f, 0));
                    string mB = gaucho ? "#a06a3a" : negro;
                    rod.Pieza(Tronco(0.1f, 0.066f, 0.2f, 6), mB, v(0, -0.3f, 0));            // caña acampanada
                    rod.Pieza(Geo.Caja(0.1f, 0.08f, 0.24f), mB, v(0, -0.4f, 0.05f));          // pie
                    rod.Pieza(Geo.Caja(0.105f, 0.03f, 0.26f), "#2b2220", v(0, -0.44f, 0.05f)); // suela
                    piernas.Add(cad); rodillas.Add(rod);
                }

            // ---------- tronco: bloques de color ----------
            var torso = cuerpo.Hijo("torso");
            string mangas = ropa, guante = null;
            bool punos = true, antebrazoPiel = false, sinHombro = false;
            float anchoHombro = 0.235f;
            void Pecho(float rs, float ri, float h, float y, string mat, float sz = 0.68f) => torso.Pieza(Tronco(rs, ri, h, 6), mat, v(0, y, 0), default, v(1, 1, sz));

            switch (estilo)
            {
                case Estilo.Levita:
                {
                    string abrigo = ropa; mangas = abrigo;
                    Pecho(0.2f, 0.15f, 0.5f, 1.12f, abrigo);
                    torso.Pieza(Tronco(0.15f, 0.27f, 0.46f, 6, true), abrigo, v(0, 0.67f, 0), default, v(1, 1, 0.8f));   // faldón
                    torso.Pieza(Geo.Caja(0.5f, 0.07f, 0.2f), abrigo, v(0, 1.36f, 0));                                    // hombros
                    torso.Pieza(Geo.Caja(0.13f, 0.3f, 0.04f), C(o.chaleco), v(0, 1.1f, 0.14f));                           // chaleco
                    foreach (var s in new[] { -1f, 1f })
                        torso.Pieza(Geo.Caja(0.07f, 0.34f, 0.02f), C(Conv.Aclara(o.ropa, 1.5f)), v(s * 0.085f, 1.16f, 0.15f), v(0, 0, -s * 0.33f));   // solapas
                    torso.Pieza(Geo.Cono(0.06f, 0.12f, 3), crema, v(0, 1.31f, 0.14f), v(PI, 0, 0));                      // corbata
                    break;
                }
                case Estilo.Pregonero:
                    Pecho(0.19f, 0.145f, 0.5f, 1.12f, ropa2);
                    Pecho(0.2f, 0.16f, 0.34f, 1.06f, ropa, 0.72f);                                                       // chaleco
                    torso.Pieza(Geo.Caja(0.46f, 0.06f, 0.19f), ropa2, v(0, 1.36f, 0));
                    torso.Pieza(Geo.Cono(0.1f, 0.16f, 3), "#c0392b", v(0, 1.3f, 0.1f), v(PI, 0, 0));                      // pañuelo rojo
                    torso.Pieza(Geo.Caja(0.045f, 0.62f, 0.015f), cuero, v(0.03f, 1.1f, 0.145f), v(0, 0, -0.62f));         // correa
                    torso.Pieza(Geo.Caja(0.34f, 0.28f, 0.14f), cuero, v(-0.3f, 0.86f, 0.02f));                           // bolso
                    torso.Pieza(Geo.Caja(0.07f, 0.22f, 0.06f), "#d9d2bd", v(-0.3f, 0.7f, 0.02f), v(0, 0, 0.15f));
                    for (int i = 0; i < 3; i++)
                        torso.Pieza(Geo.Caja(0.26f, 0.02f, 0.2f), crema, v(-0.3f, 1.01f + i * 0.022f, 0.02f), v(0, (i - 1) * 0.2f, 0.1f));   // diarios
                    mangas = ropa2; punos = false; antebrazoPiel = true;
                    break;
                case Estilo.Vestido:
                    torso.Pieza(Tronco(0.15f, 0.62f, 0.86f, 8, true), ropa, v(0, 0.45f, 0));                              // falda
                    torso.Pieza(Tronco(0.64f, 0.64f, 0.05f, 8), C(Conv.Aclara(o.ropa, 0.72f)), v(0, 0.03f, 0));
                    torso.Pieza(Geo.Cono(0.2f, 0.62f, 3), crema, v(0, 0.5f, 0.33f), v(PI, 0, 0), v(1.1f, 1, 0.12f));       // delantal
                    Pecho(0.15f, 0.12f, 0.34f, 1.05f, C(Conv.Aclara(o.ropa, 1.2f)), 0.7f);                                // corpiño
                    torso.Pieza(Tronco(0.17f, 0.3f, 0.3f, 6, true), ropa2, v(0, 1.2f, 0), default, v(1, 1, 0.85f));      // chal
                    torso.Pieza(Tronco(0.31f, 0.32f, 0.04f, 6), "#d4c39a", v(0, 1.05f, 0), default, v(1, 1, 0.85f));      // flecos
                    anchoHombro = 0.2f; mangas = ropa;
                    break;
                case Estilo.Sotana:
                    torso.Pieza(Tronco(0.17f, 0.34f, 1.12f, 7, true), "#2b2622", v(0, 0.58f, 0), default, v(1, 1, 0.85f));
                    torso.Pieza(Tronco(0.2f, 0.31f, 0.54f, 7, true), ropa2, v(0, 0.95f, 0), default, v(1, 1, 0.9f));      // roquete
                    torso.Pieza(Geo.Caja(0.5f, 0.06f, 0.2f), "#2b2622", v(0, 1.36f, 0));
                    torso.Pieza(Tronco(0.32f, 0.34f, 0.05f, 7), crema, v(0, 0.69f, 0), default, v(1, 1, 0.9f));           // encaje
                    torso.Pieza(Geo.Caja(0.02f, 0.14f, 0.02f), oro, v(0, 1.12f, 0.2f));
                    torso.Pieza(Geo.Caja(0.08f, 0.025f, 0.02f), oro, v(0, 1.16f, 0.2f));
                    mangas = "#2b2622";
                    break;
                case Estilo.Poncho:
                {
                    Pecho(0.17f, 0.14f, 0.5f, 1.12f, "#efe6d0");
                    string rojo = C(o.poncho);
                    torso.Pieza(Tronco(0.12f, 0.5f, 0.74f, 4, true), rojo, v(0, 1.0f, 0), v(0, PI / 4, 0));               // forma de rombo
                    torso.Pieza(Tronco(0.5f, 0.5f, 0.08f, 4, true), "#2a1c14", v(0, 0.7f, 0), v(0, PI / 4, 0));           // guarda
                    torso.Pieza(Tronco(0.48f, 0.48f, 0.04f, 4, true), crema, v(0, 0.78f, 0), v(0, PI / 4, 0));
                    torso.Pieza(Geo.Cono(0.1f, 0.16f, 3), "#2f6f8f", v(0, 1.3f, 0.12f), v(PI, 0, 0));                     // pañuelo
                    torso.Pieza(Tronco(0.19f, 0.19f, 0.09f, 6), "#2f6f8f", v(0, 0.88f, 0), default, v(1, 1, 0.7f));       // faja
                    torso.Pieza(Geo.Caja(0.26f, 0.4f, 0.02f), "#4f6178", v(0, 0.66f, 0.13f));                              // chiripá
                    torso.Pieza(Geo.Caja(0.26f, 0.4f, 0.02f), "#4f6178", v(0, 0.66f, -0.13f));
                    mangas = "#efe6d0"; punos = false; sinHombro = true; guante = piel;
                    break;
                }
            }

            // ---------- brazos ----------
            foreach (var s in new[] { -1f, 1f })
            {
                var hom = cuerpo.Hijo(s < 0 ? "hombro_I" : "hombro_D", v(s * anchoHombro, 1.33f, 0));
                if (!sinHombro) hom.Pieza(Geo.Octa(0.06f), mangas, Vector3.zero);
                hom.Pieza(Tronco(0.05f, 0.042f, 0.3f, 5), mangas, v(0, -0.14f, 0));
                var cod = hom.Hijo("codo", v(0, -0.29f, 0));
                cod.Pieza(Tronco(0.042f, 0.036f, 0.28f, 5), antebrazoPiel ? piel : mangas, v(0, -0.13f, 0));
                if (punos) cod.Pieza(Tronco(0.058f, 0.044f, 0.07f, 5), crema, v(0, -0.26f, 0));                          // puño acampanado
                cod.Pieza(Geo.Octa(0.055f), guante ?? piel, v(0, -0.34f, 0.01f), default, v(0.85f, 1.25f, 0.8f));        // mano grande
                brazos.Add(hom); codos.Add(cod);
            }
            if (o.baston)
            {
                var b = codos[1].Hijo("baston", v(0, -0.33f, 0.05f));
                b.Pieza(Tronco(0.012f, 0.012f, 0.86f, 4), "#2f1f14", v(0, -0.34f, 0));
                b.Pieza(Geo.Octa(0.035f), "#d9dde0", v(0, 0.1f, 0));
            }

            // ---------- cabeza: faceta alargada, sin cara ----------
            var cab = cuerpo.Hijo("cabeza", v(0, 1.4f, 0));
            cab.Pieza(Tronco(0.045f, 0.05f, 0.12f, 5), piel, v(0, 0.03f, 0));
            cab.Pieza(Geo.Ico(0.14f), piel, v(0, 0.22f, 0), default, v(0.88f, 1.28f, 0.98f));
            cab.Pieza(Geo.Cono(0.03f, 0.08f, 3), pielOsc, v(0, 0.2f, 0.135f), v(PI / 2, 0, 0));                           // nariz: cuña
            const float zc = 0.12f;

            void Puas(int n, float r, float y, float largo, float ancho)
            {
                for (int i = 0; i < n; i++)
                {
                    float a = (float)i / n * PI * 2;
                    cab.Pieza(Geo.Cono(ancho, largo, 4), pelo, v(Mathf.Sin(a) * r, y + (i % 2) * 0.03f, Mathf.Cos(a) * r * 0.9f - 0.02f), v(Mathf.Cos(a) * 0.9f, 0, -Mathf.Sin(a) * 0.9f));
                }
            }
            switch (o.PeinadoReal)
            {
                case Peinado.Puas:
                    cab.Pieza(Geo.Ico(0.14f), pelo, v(0, 0.3f, -0.03f), default, v(0.9f, 0.7f, 1.0f));
                    Puas(7, 0.1f, 0.4f, 0.12f, 0.045f);
                    break;
                case Peinado.Corto:
                    cab.Pieza(Geo.Ico(0.145f), pelo, v(0, 0.3f, -0.025f), default, v(0.92f, 0.75f, 1.02f));
                    break;
                case Peinado.Canoso:
                    foreach (var s in new[] { -1f, 1f }) cab.Pieza(Geo.Cono(0.06f, 0.16f, 4), pelo, v(s * 0.14f, 0.2f, -0.02f), v(0, 0, s * 1.3f));
                    cab.Pieza(Geo.Cono(0.07f, 0.16f, 4), pelo, v(0, 0.2f, -0.13f), v(-1.3f, 0, 0));
                    break;
                case Peinado.Largo:
                    cab.Pieza(Geo.Ico(0.145f), pelo, v(0, 0.29f, -0.03f), default, v(0.94f, 0.8f, 1.02f));
                    cab.Pieza(Geo.Caja(0.27f, 0.32f, 0.08f), pelo, v(0, 0.1f, -0.12f));
                    foreach (var s in new[] { -1f, 1f }) cab.Pieza(Geo.Caja(0.04f, 0.18f, 0.08f), pelo, v(s * 0.13f, 0.2f, 0.03f));
                    break;
                case Peinado.Rodete:
                    cab.Pieza(Geo.Ico(0.15f), pelo, v(0, 0.28f, -0.03f), default, v(0.96f, 0.85f, 1.05f));
                    foreach (var s in new[] { -1f, 1f }) cab.Pieza(Geo.Caja(0.045f, 0.24f, 0.12f), pelo, v(s * 0.13f, 0.17f, -0.02f));
                    cab.Pieza(Geo.Ico(0.09f), pelo, v(0, 0.4f, -0.14f));                                                   // rodete
                    cab.Pieza(Geo.Caja(0.16f, 0.14f, 0.02f), "#e6c26a", v(0, 0.46f, -0.1f), v(-0.5f, 0, 0));               // peineta dorada
                    cab.Pieza(Geo.Octa(0.035f), "#c0392b", v(0.06f, 0.45f, -0.06f));                                       // flor
                    break;
            }
            if (o.barba)
            {
                cab.Pieza(Geo.Ico(0.1f), pelo, v(0, 0.08f, 0.075f), default, v(1.15f, 0.9f, 0.9f));
                cab.Pieza(Geo.Caja(0.3f, 0.05f, 0.18f), pelo, v(0, 0.1f, 0.01f));
            }
            if (o.bigote == Bigote.Manubrio)
                foreach (var s in new[] { -1f, 1f }) cab.Pieza(Geo.Cono(0.02f, 0.16f, 3), pelo, v(s * 0.075f, 0.145f, zc + 0.02f), v(0, 0, -s * (PI / 2 - 0.45f)));
            else if (o.bigote == Bigote.Fino)
                cab.Pieza(Geo.Caja(0.13f, 0.03f, 0.035f), pelo, v(0, 0.145f, zc + 0.02f));

            // ---------- sombreros: siluetas inconfundibles ----------
            const float t0 = 0.4f;
            switch (o.sombrero)
            {
                case Sombrero.Copa:
                {
                    const string seda = "#241f1c";
                    cab.Pieza(Tronco(0.3f, 0.3f, 0.025f, 6), seda, v(0, t0, 0));
                    cab.Pieza(Tronco(0.15f, 0.18f, 0.42f, 6), seda, v(0, t0 + 0.22f, 0));
                    cab.Pieza(Tronco(0.185f, 0.185f, 0.07f, 6), "#b9bcc2", v(0, t0 + 0.05f, 0));
                    break;
                }
                case Sombrero.Ala:
                {
                    string f = C(o.colorSombrero);
                    cab.Pieza(Tronco(0.42f, 0.42f, 0.025f, 8), f, v(0, t0 - 0.02f, 0), v(0.05f, 0, 0.05f));
                    cab.Pieza(Tronco(0.14f, 0.18f, 0.16f, 6), f, v(0, t0 + 0.07f, 0));
                    cab.Pieza(Tronco(0.185f, 0.185f, 0.045f, 6), "#c0392b", v(0, t0 + 0.02f, 0));
                    break;
                }
                case Sombrero.Bonete:
                {
                    const string n = "#241f1c";
                    cab.Pieza(Geo.Caja(0.3f, 0.13f, 0.3f), n, v(0, t0 + 0.02f, 0));
                    foreach (var (x, z) in new[] { (1f, 1f), (-1f, 1f), (1f, -1f), (-1f, -1f) })
                        cab.Pieza(Geo.Caja(0.04f, 0.1f, 0.04f), n, v(x * 0.13f, t0 + 0.11f, z * 0.13f));
                    cab.Pieza(Geo.Octa(0.045f), n, v(0, t0 + 0.17f, 0));
                    break;
                }
                case Sombrero.Kepi:
                    cab.Pieza(Tronco(0.2f, 0.25f, 0.14f, 8), ropa, v(0, t0 - 0.02f, 0));
                    cab.Pieza(Tronco(0.255f, 0.255f, 0.035f, 8), "#d6ac3f", v(0, t0 - 0.08f, 0));
                    cab.Pieza(Geo.Caja(0.22f, 0.02f, 0.13f), "#1f1a17", v(0, t0 - 0.1f, zc + 0.15f), v(0.18f, 0, 0));
                    cab.Pieza(Geo.Octa(0.035f), "#d6ac3f", v(0, t0 + 0.07f, 0.1f));
                    break;
                case Sombrero.Gorra:
                {
                    string t = C(o.colorSombrero);
                    cab.Pieza(Tronco(0.24f, 0.2f, 0.1f, 8), t, v(0, t0 - 0.03f, 0));
                    cab.Pieza(Geo.Caja(0.24f, 0.02f, 0.16f), "#3a342d", v(0, t0 - 0.08f, zc + 0.12f), v(0.2f, 0, 0));
                    cab.Pieza(Geo.Octa(0.03f), t, v(0, t0 + 0.03f, 0));
                    break;
                }
            }

            // ---------- accesorios ----------
            switch (o.accesorio)
            {
                case Accesorio.Fusil:
                {
                    var f = codos[1].Hijo("fusil", v(0, -0.33f, 0.05f));
                    f.Pieza(Tronco(0.02f, 0.022f, 0.62f, 5), "#6b4528", v(0, -0.52f, 0));     // culata
                    f.Pieza(Tronco(0.012f, 0.014f, 0.7f, 5), "#6a6e72", v(0, 0.15f, 0));      // caño
                    f.Pieza(Geo.Cono(0.016f, 0.16f, 3), "#c9ced2", v(0, 0.58f, 0));           // bayoneta
                    break;
                }
                case Accesorio.Canasta:
                {
                    var c = codos[0].Hijo("canasta", v(0, -0.36f, 0.08f));
                    c.Pieza(Tronco(0.15f, 0.11f, 0.13f, 7), "#b98a4a", Vector3.zero);
                    c.Pieza(Tronco(0.145f, 0.145f, 0.03f, 7), "#f6efdc", v(0, 0.07f, 0));
                    foreach (var (x, z) in new[] { (-0.05f, 0f), (0.05f, 0.03f), (0f, -0.05f) })
                        c.Pieza(Geo.Ico(0.04f), "#e8b86a", v(x, 0.1f, z), default, v(1, 0.7f, 1));
                    c.Pieza(Geo.Toro(0.12f, 0.012f, 4, 8, PI), "#8a6a3a", v(0, 0.04f, 0));
                    break;
                }
                case Accesorio.Barril:
                    torso.Pieza(Tronco(0.19f, 0.17f, 0.5f, 7), "#9a6a3c", v(0, 1.0f, -0.32f));
                    foreach (var y in new[] { -0.15f, 0.15f })
                        torso.Pieza(Geo.Toro(0.18f, 0.012f, 4, 8), "#2a2724", v(0, 1.0f + y, -0.32f), v(PI / 2, 0, 0));
                    torso.Pieza(Geo.Caja(0.04f, 0.4f, 0.01f), "#6b4528", v(0.12f, 1.15f, -0.1f), v(0.2f, 0, 0.5f));
                    break;
                case Accesorio.Guitarra:
                {
                    var gu = torso.Hijo("guitarra", v(0.1f, 0.92f, 0.22f));
                    gu.rot = v(0, 0, -0.55f);
                    gu.Pieza(Geo.Ico(0.2f), "#b07a3d", Vector3.zero, default, v(0.9f, 1.15f, 0.32f));
                    gu.Pieza(Geo.Ico(0.07f), "#2a1a10", v(0, 0.02f, 0.1f));
                    gu.Pieza(Geo.Caja(0.06f, 0.55f, 0.04f), "#5a3a22", v(0, 0.42f, 0));
                    gu.Pieza(Geo.Caja(0.09f, 0.12f, 0.05f), "#3a2616", v(0, 0.72f, 0));
                    break;
                }
                case Accesorio.Cajon:
                    codos[0].Pieza(Geo.Caja(0.32f, 0.14f, 0.22f), "#6b4528", v(-0.04f, -0.34f, 0.12f));
                    codos[0].Pieza(Tronco(0.03f, 0.03f, 0.12f, 5), "#2a2724", v(-0.04f, -0.24f, 0.12f));
                    break;
            }

            // El prototipo mira hacia +z del prototipo, que en Unity es -z: se gira 180° para que mire hacia +z (forward).
            var frente = new GameObject("frente").transform;
            frente.SetParent(padre, false);
            frente.localRotation = Quaternion.Euler(0, 180, 0);
            var mapa = new Dictionary<Nodo, Transform>();
            g.Instanciar(frente, registrar, mapa);

            var anim = new AnimPersonaje { cuerpo = mapa[cuerpo], torso = mapa[torso], cab = mapa[cab], vestido = estilo == Estilo.Vestido };
            foreach (var p in piernas) anim.piernas.Add(mapa[p]);
            foreach (var r in rodillas) anim.rodillas.Add(mapa[r]);
            foreach (var b in brazos) anim.brazos.Add(mapa[b]);
            foreach (var c in codos) anim.codos.Add(mapa[c]);
            anim.Animar(0, 0);
            return anim;
        }
    }
}
