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

        /// Normales suaves en las formas redondas (y más lados en brazos, piernas y cuello): look más blando sin más polígonos visibles. Se puede apagar para comparar.
        public static bool Suavizar = true;

        /// Material con detalle de superficie (manchas, trama de tela, mugre) en vez de colores lisos. Se puede apagar para comparar.
        public static bool ConDetalle = true;

        public static AnimPersonaje Construir(Transform padre, Aspecto o, Func<Mesh, Mesh> registrar)
        {
            string C(Color c) => Conv.Clave(c);
            string piel = C(o.piel), pielOsc = C(Conv.Aclara(o.piel, 0.82f)), pelo = C(o.pelo);
            const string negro = "#1f1a17", cuero = "#6b4528", crema = "#f4eedc", oro = "#d6ac3f";
            string ropa = C(o.ropa), ropa2 = C(o.ropa2);
            var estilo = o.estilo;

            float cx = o.contextura > 0 ? o.contextura : 1;   // flaco o robusto: más ancho de hombros y de cintura
            int va = Mathf.Clamp(o.variante, 0, 3);
            var g = new Nodo("modelo") { esc = v(0.88f * o.escala * cx, 0.99f * o.escala, 0.88f * o.escala * Mathf.Lerp(1, cx, 0.8f)) };   // más alto y flaco que antes (0,92 parejo): menos juguete
            var cuerpo = g.Hijo("cuerpo");
            bool conPiernas = estilo == Estilo.Levita || estilo == Estilo.Poncho || estilo == Estilo.Pregonero || estilo == Estilo.Quillango || estilo == Estilo.Soldado;
            var piernas = new List<Nodo>(); var rodillas = new List<Nodo>(); var brazos = new List<Nodo>(); var codos = new List<Nodo>();
            const float CAD = 0.86f;

            Geo Tronco(float rs, float ri, float h, int seg = 6, bool abierto = false) { var t = Geo.Cilindro(rs, ri, h, seg, abierto); return Suavizar ? t.Suave() : t; }
            int Lados(int n) => Suavizar ? 8 : n;   // lados de piernas, brazos y cuello

            // ---------- piernas flacas y botas grandes ----------
            if (conPiernas)
                foreach (var s in new[] { -1f, 1f })
                {
                    var cad = cuerpo.Hijo(s < 0 ? "cadera_I" : "cadera_D", v(s * 0.085f, CAD, 0));
                    bool gaucho = estilo == Estilo.Poncho || (estilo == Estilo.Soldado && va == 2);
                    bool quillango = estilo == Estilo.Quillango;
                    string mP = gaucho ? "#efe6d0" : C(o.pantalon);                           // con quillango, el chiripá
                    cad.Pieza(Tronco(gaucho ? 0.1f : 0.072f, gaucho ? 0.09f : 0.056f, 0.44f, Lados(5)), mP, v(0, -0.2f, 0));
                    var rod = cad.Hijo("rodilla", v(0, -0.42f, 0));
                    rod.Pieza(Tronco(gaucho ? 0.088f : 0.056f, gaucho ? 0.07f : 0.046f, 0.4f, Lados(5)), mP, v(0, -0.18f, 0));
                    if (gaucho)
                    {
                        // calzoncillo cribado: termina en flecos sobre la bota de potro
                        rod.Pieza(Tronco(0.075f, 0.1f, 0.07f, 7, true), crema, v(0, -0.2f, 0));
                        for (int k = 0; k < 7; k++)
                        {
                            float a = k * PI * 2 / 7;
                            rod.Pieza(Geo.Caja(0.02f, 0.06f, 0.008f), crema, v(Mathf.Sin(a) * 0.098f, -0.255f, Mathf.Cos(a) * 0.098f), v(0, a, 0));
                        }
                    }
                    string mB = quillango || gaucho ? "#c4a07a" : negro;                        // bota de potro: cuero crudo
                    rod.Pieza(Tronco(0.1f, 0.066f, 0.2f, 6), mB, v(0, -0.3f, 0));            // caña acampanada
                    rod.Pieza(Geo.Caja(0.1f, 0.08f, 0.24f), mB, v(0, -0.4f, 0.05f));          // pie
                    rod.Pieza(Geo.Caja(0.105f, 0.03f, 0.26f), "#2b2220", v(0, -0.44f, 0.05f)); // suela
                    if (gaucho)   // espuela nazarena
                    {
                        rod.Pieza(Geo.Toro(0.05f, 0.008f, 3, 8), "#b8bcc0", v(0, -0.39f, -0.1f), v(PI / 2, 0, 0));
                        rod.Pieza(Geo.Caja(0.012f, 0.012f, 0.08f), "#b8bcc0", v(0, -0.39f, -0.06f));
                    }
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
                    if (va == 1) torso.Pieza(Tronco(0.15f, 0.21f, 0.2f, 6, true), abrigo, v(0, 0.8f, 0), default, v(1, 1, 0.8f));   // saco corto
                    else torso.Pieza(Tronco(0.15f, 0.27f, 0.46f, 6, true), abrigo, v(0, 0.67f, 0), default, v(1, 1, 0.8f));     // faldón de la levita
                    torso.Pieza(Geo.Caja(0.5f, 0.07f, 0.2f), abrigo, v(0, 1.36f, 0));                                    // hombros
                    torso.Pieza(Geo.Caja(0.13f, 0.3f, 0.04f), va == 2 ? "#e8e0c8" : C(o.chaleco), v(0, 1.1f, 0.14f));     // chaleco
                    foreach (var s in new[] { -1f, 1f })
                        torso.Pieza(Geo.Caja(0.07f, 0.34f, 0.02f), C(Conv.Aclara(o.ropa, 1.5f)), v(s * 0.085f, 1.16f, 0.15f), v(0, 0, -s * 0.33f));   // solapas
                    torso.Pieza(Geo.Cono(0.06f, 0.12f, 3), crema, v(0, 1.31f, 0.14f), v(PI, 0, 0));                      // corbata
                    torso.Pieza(Geo.Toro(0.05f, 0.006f, 3, 8, PI), oro, v(0.035f, 1.03f, 0.165f), v(PI / 2, 0, PI));      // leontina del reloj
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
                    if (va != 1 && va != 2) torso.Pieza(Geo.Cono(0.2f, 0.62f, 3), crema, v(0, 0.5f, 0.33f), v(PI, 0, 0), v(1.1f, 1, 0.12f));   // delantal
                    if (va == 2) torso.Pieza(Tronco(0.55f, 0.6f, 0.09f, 8, true), C(o.chaleco), v(0, 0.13f, 0));          // franja de la pollera
                    if (va == 3) torso.Pieza(Tronco(0.2f, 0.36f, 0.5f, 6, true), ropa2, v(0, 1.0f, 0), default, v(1, 1, 0.85f));   // mantón largo
                    Pecho(0.15f, 0.12f, 0.34f, 1.05f, C(Conv.Aclara(o.ropa, 1.2f)), 0.7f);                                // corpiño
                    torso.Pieza(Tronco(0.17f, 0.3f, 0.3f, 6, true), ropa2, v(0, 1.2f, 0), default, v(1, 1, 0.85f));      // chal
                    torso.Pieza(Tronco(0.31f, 0.32f, 0.04f, 6), "#d4c39a", v(0, 1.05f, 0), default, v(1, 1, 0.85f));      // flecos
                    torso.Pieza(Tronco(0.6f, 0.635f, 0.05f, 8, true), "#ece4d2", v(0, 0.04f, 0));                         // la enagua asoma abajo
                    foreach (var s in new[] { -1f, 1f })                                                                   // las puntas del rebozo, cruzadas al frente
                        torso.Pieza(Geo.Caja(0.08f, 0.42f, 0.02f), ropa2, v(s * 0.07f, 0.98f, 0.2f), v(-0.15f, 0, s * 0.18f));
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
                    // gaucho: camisa, chiripá sobre el calzoncillo cribado, faja, tirador de cuero con monedas
                    // y facón atravesado atrás; el poncho puesto o doblado sobre el hombro
                    Pecho(0.17f, 0.14f, 0.5f, 1.12f, "#efe6d0");
                    string chiripa = C(o.ropa), poncho = C(o.poncho);
                    torso.Pieza(Tronco(0.2f, 0.27f, 0.42f, 6, true), chiripa, v(0, 0.66f, 0), default, v(1, 1, 0.8f));     // chiripá
                    torso.Pieza(Geo.Caja(0.03f, 0.4f, 0.02f), C(Conv.Aclara(o.ropa, 0.7f)), v(0, 0.66f, 0.2f));            // el pliegue de adelante
                    torso.Pieza(Tronco(0.2f, 0.2f, 0.07f, 6), "#2f6f8f", v(0, 0.84f, 0), default, v(1, 1, 0.75f));          // faja
                    torso.Pieza(Tronco(0.205f, 0.205f, 0.13f, 8), "#4a3020", v(0, 0.93f, 0), default, v(1, 1, 0.78f));     // tirador
                    for (int k = 0; k < 4; k++)                                                                             // monedas del tirador
                        torso.Pieza(Geo.Octa(0.024f), "#d9dde0", v(-0.09f + k * 0.06f, 0.93f, 0.165f));
                    torso.Pieza(Geo.Caja(0.03f, 0.12f, 0.03f), "#d9dde0", v(0.08f, 1.0f, -0.18f), v(0, 0, 0.9f));          // facón: cabo de plata
                    torso.Pieza(Geo.Caja(0.035f, 0.3f, 0.015f), "#2a1c14", v(0.18f, 0.88f, -0.18f), v(0, 0, 0.9f));        // y vaina
                    torso.Pieza(Geo.Cono(0.1f, 0.16f, 3), "#7a2a22", v(0, 1.3f, 0.12f), v(PI, 0, 0));                      // pañuelo al cuello
                    if (o.ponchoAlHombro)
                    {
                        torso.Pieza(Geo.Caja(0.16f, 0.07f, 0.42f), poncho, v(-0.2f, 1.37f, 0));                              // doblado sobre el hombro
                        torso.Pieza(Geo.Caja(0.17f, 0.5f, 0.05f), poncho, v(-0.22f, 1.12f, 0.17f), v(0.05f, 0, 0.08f));
                        torso.Pieza(Geo.Caja(0.17f, 0.55f, 0.05f), poncho, v(-0.22f, 1.1f, -0.17f), v(-0.05f, 0, 0.08f));
                        torso.Pieza(Geo.Caja(0.175f, 0.05f, 0.06f), "#2a1c14", v(-0.22f, 0.87f, 0.17f));                    // la guarda
                    }
                    else
                    {
                        torso.Pieza(Tronco(0.12f, 0.5f, 0.74f, 4, true), poncho, v(0, 1.0f, 0), v(0, PI / 4, 0));            // forma de rombo
                        torso.Pieza(Tronco(0.5f, 0.5f, 0.08f, 4, true), "#2a1c14", v(0, 0.7f, 0), v(0, PI / 4, 0));          // guarda
                        torso.Pieza(Tronco(0.48f, 0.48f, 0.04f, 4, true), crema, v(0, 0.78f, 0), v(0, PI / 4, 0));
                        string franja = C(o.chaleco);
                        if (va == 1)        // a rayas
                            foreach (var y in new[] { 0.95f, 1.12f })
                                torso.Pieza(Tronco(Mathf.Lerp(0.12f, 0.5f, (1.37f - y) / 0.74f) + 0.01f, Mathf.Lerp(0.12f, 0.5f, (1.37f - y + 0.05f) / 0.74f) + 0.01f, 0.05f, 4, true), franja, v(0, y, 0), v(0, PI / 4, 0));
                        else if (va == 2)   // guarda ancha de dos colores
                            torso.Pieza(Tronco(0.455f, 0.49f, 0.07f, 4, true), franja, v(0, 0.75f, 0), v(0, PI / 4, 0));
                        else if (va == 3)   // poncho pampa: la cruz tejida al frente y atrás
                            foreach (var z in new[] { 1f, -1f })
                            {
                                torso.Pieza(Geo.Caja(0.05f, 0.5f, 0.02f), crema, v(0, 0.95f, z * 0.3f), v(z * 0.25f, 0, 0));
                                torso.Pieza(Geo.Caja(0.36f, 0.05f, 0.02f), crema, v(0, 0.93f, z * 0.32f), v(z * 0.25f, 0, 0));
                            }
                    }
                    if (o.ponchoAlHombro && va > 0)
                        torso.Pieza(Geo.Caja(0.175f, 0.06f, 0.06f), C(o.chaleco), v(-0.22f, 1.0f, 0.17f));                     // franja del poncho doblado
                    mangas = "#efe6d0"; punos = false; sinHombro = !o.ponchoAlHombro; guante = piel;
                    break;
                }
                case Estilo.Soldado:
                {
                    // soldado de línea de la frontera: chaqueta corta con botones y cuello colorado, correaje blanco
                    // cruzado, cinturón con cartuchera; gastado de tanto servicio. Variantes: 1 con poncho encima,
                    // 2 con chiripá y botas de potro (muchos andaban así), 3 en camisa
                    if (va == 3) { Pecho(0.18f, 0.15f, 0.5f, 1.12f, "#e6dccb"); ropa = C(Conv.Aclara(o.ropa, 1.0f)); }
                    else Pecho(0.19f, 0.16f, 0.5f, 1.12f, ropa);
                    if (va == 1)
                    {
                        torso.Pieza(Tronco(0.13f, 0.48f, 0.6f, 4, true), C(o.ropa2), v(0, 1.07f, 0), v(0, PI / 4, 0));     // poncho encima
                        torso.Pieza(Tronco(0.48f, 0.48f, 0.06f, 4, true), "#2a1c14", v(0, 0.79f, 0), v(0, PI / 4, 0));
                    }
                    if (va == 2) torso.Pieza(Tronco(0.2f, 0.27f, 0.42f, 6, true), C(o.ropa2), v(0, 0.66f, 0), default, v(1, 1, 0.8f));   // chiripá
                    torso.Pieza(Tronco(0.17f, 0.21f, 0.14f, 6, true), ropa, v(0, 0.84f, 0), default, v(1, 1, 0.8f));       // faldón corto
                    torso.Pieza(Geo.Caja(0.48f, 0.07f, 0.2f), ropa, v(0, 1.36f, 0));                                        // hombros
                    torso.Pieza(Tronco(0.1f, 0.11f, 0.07f, 6), "#a02a20", v(0, 1.37f, 0));                                  // cuello colorado
                    for (int k = 0; k < 5; k++) torso.Pieza(Geo.Octa(0.018f), oro, v(0, 0.95f + k * 0.085f, 0.15f));        // botones
                    foreach (var s in new[] { -1f, 1f })
                        torso.Pieza(Geo.Caja(0.05f, 0.64f, 0.02f), "#e8e4d8", v(s * 0.02f, 1.1f, 0.155f), v(0, 0, s * 0.5f)); // correaje cruzado
                    torso.Pieza(Tronco(0.2f, 0.2f, 0.06f, 8), "#e8e4d8", v(0, 0.9f, 0), default, v(1, 1, 0.78f));          // cinturón
                    torso.Pieza(Geo.Caja(0.2f, 0.13f, 0.08f), "#1f1a17", v(0, 0.92f, -0.18f));                              // cartuchera
                    mangas = va == 3 ? "#e6dccb" : ropa; guante = null;
                    break;
                }
                case Estilo.Mirinaque:
                {
                    // señora del pueblo o de la estancia, de visita: pollera acampanada sobre el miriñaque, con volados,
                    // corpiño ajustado, mangas pagoda y un chal de encaje
                    torso.Pieza(Tronco(0.17f, 0.8f, 0.96f, 10, true), ropa, v(0, 0.5f, 0));                                // pollera sobre el miriñaque
                    torso.Pieza(Tronco(0.8f, 0.8f, 0.03f, 10), C(Conv.Aclara(o.ropa, 0.75f)), v(0, 0.03f, 0));
                    if (va == 0 || va == 2)                                                                                  // volados
                        foreach (var (y, r) in new[] { (0.18f, 0.66f), (0.42f, 0.5f), (0.66f, 0.35f) })
                            torso.Pieza(Tronco(r, r + 0.03f, 0.05f, 10, true), va == 2 ? C(o.chaleco) : C(Conv.Aclara(o.ropa, 1.18f)), v(0, y, 0));
                    else if (va == 1)                                                                                        // franjas oscuras
                        foreach (var (y, r) in new[] { (0.1f, 0.72f), (0.22f, 0.63f) })
                            torso.Pieza(Tronco(r, r + 0.02f, 0.035f, 10, true), C(Conv.Aclara(o.ropa, 0.6f)), v(0, y, 0));
                    if (va == 3) torso.Pieza(Tronco(0.17f, 0.28f, 0.3f, 8, true), C(o.chaleco), v(0, 0.98f, 0), default, v(1, 1, 0.8f));   // chaquetilla
                    Pecho(0.15f, 0.11f, 0.36f, 1.06f, ropa, 0.7f);                                                          // corpiño
                    torso.Pieza(Tronco(0.12f, 0.13f, 0.05f, 6), ropa2, v(0, 1.37f, 0));                                     // cuello de encaje
                    torso.Pieza(Tronco(0.18f, 0.3f, 0.22f, 6, true), ropa2, v(0, 1.22f, 0), default, v(1, 1, 0.85f));       // chal
                    torso.Pieza(Geo.Octa(0.03f), oro, v(0, 1.32f, 0.13f));                                                  // camafeo
                    anchoHombro = 0.2f; mangas = ropa;
                    break;
                }
            }

            if (estilo == Estilo.Quillango)
            {
                // quillango: manto de cueros de guanaco cosidos, con el pelo hacia adentro y el cuero pintado
                // hacia afuera; envuelve el cuerpo desde los hombros y se cierra con la mano
                string manto = ropa, borde = ropa2, pintura = C(o.chaleco);
                bool largo = o.PeinadoReal == Peinado.Trenzas;     // las mujeres lo llevan hasta el tobillo
                float L = largo ? 1.22f : 0.98f, arriba = 1.38f;
                Pecho(0.17f, 0.14f, 0.5f, 1.12f, C(Conv.Aclara(o.ropa, 0.8f)));
                torso.Pieza(Tronco(0.25f, 0.37f, L, 7, true), manto, v(0, arriba - L / 2, 0), default, v(1, 1, 0.84f));
                torso.Pieza(Tronco(0.375f, 0.38f, 0.07f, 7, true), borde, v(0, arriba - L + 0.035f, 0), default, v(1, 1, 0.84f));   // pelo del borde
                torso.Pieza(Tronco(0.24f, 0.28f, 0.12f, 7, true), borde, v(0, arriba - 0.04f, 0), default, v(1, 1, 0.86f));        // cuello de piel
                if (va != 2)
                {
                    torso.Pieza(Tronco(0.335f, 0.345f, 0.06f, 7, true), pintura, v(0, arriba - L * 0.62f, 0), default, v(1, 1, 0.84f)); // franja pintada
                    torso.Pieza(Tronco(0.3f, 0.31f, 0.04f, 7, true), pintura, v(0, arriba - L * 0.4f, 0), default, v(1, 1, 0.84f));
                }
                if (va == 1)   // poncho pampa tejido, a rayas, encima del quillango
                {
                    torso.Pieza(Tronco(0.13f, 0.44f, 0.56f, 4, true), C(o.poncho), v(0, 1.1f, 0), v(0, PI / 4, 0));
                    foreach (var y in new[] { 0.92f, 1.06f })
                        torso.Pieza(Tronco(0.3f, 0.34f, 0.04f, 4, true), crema, v(0, y, 0), v(0, PI / 4, 0));
                }
                torso.Pieza(Geo.Caja(0.07f, L * 0.92f, 0.03f), borde, v(0.06f, arriba - L * 0.5f, 0.3f), v(0, 0, 0.05f));        // donde se cruza al frente
                mangas = manto; punos = false; sinHombro = true; anchoHombro = 0.23f;
            }

            // ---------- brazos ----------
            foreach (var s in new[] { -1f, 1f })
            {
                var hom = cuerpo.Hijo(s < 0 ? "hombro_I" : "hombro_D", v(s * anchoHombro, 1.33f, 0));
                if (!sinHombro) hom.Pieza(Geo.Octa(0.06f), mangas, Vector3.zero);
                hom.Pieza(Tronco(0.05f, 0.042f, 0.3f, Lados(5)), mangas, v(0, -0.14f, 0));
                var cod = hom.Hijo("codo", v(0, -0.29f, 0));
                cod.Pieza(Tronco(0.042f, 0.036f, 0.28f, Lados(5)), antebrazoPiel ? piel : mangas, v(0, -0.13f, 0));
                if (punos) cod.Pieza(Tronco(0.058f, 0.044f, 0.07f, 5), estilo == Estilo.Soldado ? "#a02a20" : crema, v(0, -0.26f, 0));   // puño
                if (estilo == Estilo.Mirinaque) cod.Pieza(Tronco(0.05f, 0.12f, 0.2f, 6, true), mangas, v(0, -0.17f, 0));   // manga pagoda
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
            var cab = cuerpo.Hijo("cabeza", v(0, 1.37f, 0));   // más baja que antes: cuello corto
            cab.Pieza(Tronco(0.05f, 0.056f, 0.12f, Lados(5)), piel, v(0, 0.04f, 0));
            // cabeza modelada por anillos (mentón, mandíbula, pómulos, frente, cráneo): una cara con volumen, no un triángulo.
            // El Loft corre a lo largo de z: se gira para que z sea la altura; "y" del anillo es hacia atrás (-profundidad).
            var anillos = new[]
            {
                new Geo.Seccion(0.055f, 0.040f, 0.050f, -0.030f),   // mentón
                new Geo.Seccion(0.100f, 0.092f, 0.092f, -0.020f),   // mandíbula
                new Geo.Seccion(0.170f, 0.122f, 0.118f, -0.010f),   // pómulos
                new Geo.Seccion(0.250f, 0.128f, 0.124f, -0.006f),   // ojos y ceja
                new Geo.Seccion(0.320f, 0.124f, 0.124f, 0.000f),    // sien
                new Geo.Seccion(0.385f, 0.080f, 0.090f, 0.010f),    // coronilla
            };
            cab.Pieza(Suavizar ? Geo.Loft(anillos, 12).Suave() : Geo.Loft(anillos, 12), piel, Vector3.zero, v(-PI / 2, 0, 0));
            foreach (var s in new[] { -1f, 1f })
                cab.Pieza(Geo.Caja(0.018f, 0.062f, 0.04f), pielOsc, v(s * 0.127f, 0.215f, 0.0f), v(0, 0, s * 0.12f));       // orejas
            cab.Pieza(Geo.Caja(0.03f, 0.075f, 0.03f), C(Conv.Aclara(o.piel, 0.9f)), v(0, 0.215f, 0.138f));                                       // puente de la nariz
            cab.Pieza(Geo.Caja(0.044f, 0.024f, 0.04f), pielOsc, v(0, 0.178f, 0.14f));                                        // punta de la nariz
            cab.Pieza(Geo.Caja(0.05f, 0.01f, 0.03f), C(Color.Lerp(o.piel, Conv.Hex("#5a2c24"), 0.4f)), v(0, 0.158f, 0.125f));   // sombra bajo la nariz
            const float zc = 0.12f;

            // facciones mínimas: ceja marcada, ojos hundidos en sombra, pómulos y una boca apretada; con el pelo y el sombrero
            // encima hacen una mirada seria y cansada en vez de una bola de color
            // (inclinación de las cejas: + el extremo de adentro baja = enojo; - sube = tristeza), párpado, comisuras de la boca (+ sonrisa), ojeras
            float ceja = 0, ceyY = 0, parpado = 0.006f, comisura = 0, ojeras = 0, bocaAncho = 0.05f;
            switch (o.expresion)
            {
                case Expresion.Dura:    ceja = 0.32f; ceyY = -0.004f; parpado = 0.011f; comisura = -0.18f; bocaAncho = 0.06f; break;
                case Expresion.Cansada: ceja = -0.2f; ceyY = 0f; parpado = 0.014f; comisura = -0.25f; ojeras = 1; break;
                case Expresion.Amable:  ceja = -0.12f; ceyY = 0.008f; parpado = 0.004f; comisura = 0.45f; bocaAncho = 0.06f; break;
                case Expresion.Triste:  ceja = -0.42f; ceyY = 0.004f; parpado = 0.012f; comisura = -0.4f; ojeras = 0.6f; break;
                case Expresion.Enojado: ceja = 0.55f; ceyY = -0.008f; parpado = 0.016f; comisura = -0.2f; bocaAncho = 0.055f; break;
            }
            string cejaMat = C(Color.Lerp(o.pelo, Conv.Hex("#1b130f"), 0.45f)), ojo = "#17100c", boca = C(Color.Lerp(o.piel, Conv.Hex("#6e2f28"), 0.65f)), sombraOjos = C(Conv.Aclara(o.piel, 0.6f));
            foreach (var s in new[] { -1f, 1f })
            {
                cab.Pieza(Geo.Caja(0.07f, 0.04f, 0.012f), "#ece4d0", v(s * 0.054f, 0.238f, 0.121f));                          // blanco del ojo
                cab.Pieza(Geo.Caja(0.03f, 0.038f, 0.016f), ojo, v(s * 0.054f, 0.238f, 0.124f));                                 // pupila
                cab.Pieza(Geo.Caja(0.076f, parpado + 0.008f, 0.018f), pielOsc, v(s * 0.054f, 0.262f - parpado * 0.4f, 0.123f), v(0, 0, s * ceja * 0.25f));   // párpado
                cab.Pieza(Geo.Caja(0.082f, 0.02f, 0.014f), cejaMat, v(s * 0.056f, 0.286f + ceyY, 0.118f), v(0, 0, s * ceja));    // ceja
                if (ojeras > 0) cab.Pieza(Geo.Caja(0.062f, 0.014f * ojeras + 0.004f, 0.01f), sombraOjos, v(s * 0.054f, 0.214f, 0.12f));   // ojera
                cab.Pieza(Geo.Caja(bocaAncho * 0.6f, 0.017f, 0.016f), boca, v(s * bocaAncho * 0.6f, 0.136f + comisura * 0.026f, 0.112f), v(0, 0, s * comisura * 0.9f));   // comisura
            }
            cab.Pieza(Geo.Caja(bocaAncho * 0.7f, 0.018f, 0.016f), boca, v(0, 0.136f, 0.115f));                                  // labios

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
                    cab.Pieza(Geo.Ico(0.14f), pelo, v(0, 0.34f, 0f), default, v(1.0f, 0.56f, 1.0f));
                    Puas(7, 0.1f, 0.4f, 0.12f, 0.045f);
                    break;
                case Peinado.Corto:
                    cab.Pieza(Geo.Ico(0.14f), pelo, v(0, 0.34f, 0f), default, v(1.0f, 0.56f, 1.0f));
                    break;
                case Peinado.Canoso:
                    foreach (var s in new[] { -1f, 1f }) cab.Pieza(Geo.Cono(0.06f, 0.16f, 4), pelo, v(s * 0.14f, 0.2f, -0.02f), v(0, 0, s * 1.3f));
                    cab.Pieza(Geo.Cono(0.07f, 0.16f, 4), pelo, v(0, 0.2f, -0.13f), v(-1.3f, 0, 0));
                    break;
                case Peinado.Largo:
                    cab.Pieza(Geo.Ico(0.14f), pelo, v(0, 0.34f, 0f), default, v(1.0f, 0.56f, 1.0f));
                    cab.Pieza(Geo.Caja(0.27f, 0.32f, 0.08f), pelo, v(0, 0.1f, -0.12f));
                    foreach (var s in new[] { -1f, 1f }) cab.Pieza(Geo.Caja(0.04f, 0.18f, 0.08f), pelo, v(s * 0.13f, 0.2f, 0.03f));
                    break;
                case Peinado.Trenzas:
                    cab.Pieza(Geo.Ico(0.14f), pelo, v(0, 0.34f, 0f), default, v(1.0f, 0.56f, 1.0f));
                    cab.Pieza(Geo.Caja(0.25f, 0.22f, 0.06f), pelo, v(0, 0.17f, -0.12f));
                    foreach (var s in new[] { -1f, 1f })                                                                   // dos trenzas al frente
                    {
                        cab.Pieza(Geo.Caja(0.05f, 0.46f, 0.05f), pelo, v(s * 0.13f, -0.04f, 0.06f), v(0.12f, 0, s * 0.06f));
                        cab.Pieza(Geo.Octa(0.03f), "#c0392b", v(s * 0.135f, -0.28f, 0.09f));                                 // atadura
                    }
                    break;
                case Peinado.Rodete:
                    cab.Pieza(Geo.Ico(0.14f), pelo, v(0, 0.34f, 0f), default, v(1.0f, 0.56f, 1.0f));
                    foreach (var s in new[] { -1f, 1f }) cab.Pieza(Geo.Caja(0.045f, 0.24f, 0.12f), pelo, v(s * 0.13f, 0.17f, -0.02f));
                    cab.Pieza(Geo.Ico(0.09f), pelo, v(0, 0.4f, -0.14f));                                                   // rodete
                    bool tapada = o.sombrero == Sombrero.Panuelo || o.sombrero == Sombrero.Mantilla;
                    if (estilo == Estilo.Mirinaque && !tapada)                                                              // peineta de carey, solo las señoras
                        cab.Pieza(Geo.Caja(0.16f, 0.14f, 0.02f), "#8a5a2a", v(0, 0.46f, -0.1f), v(-0.5f, 0, 0));
                    if (!tapada) cab.Pieza(Geo.Octa(0.035f), "#c0392b", v(0.06f, 0.45f, -0.06f));                          // flor
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
                    float ala = va == 3 ? 0.34f : va == 1 ? 0.46f : 0.42f, copa = va == 2 ? 0.24f : va == 1 ? 0.12f : 0.16f;
                    cab.Pieza(Tronco(ala, ala, 0.025f, 8), f, v(0, t0 - 0.02f, 0), v(0.05f + va * 0.03f, 0, 0.05f));
                    cab.Pieza(Tronco(0.14f, 0.18f, copa, 6), f, v(0, t0 - 0.01f + copa / 2, 0));
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
                case Sombrero.Vincha:
                    cab.Pieza(Tronco(0.152f, 0.156f, 0.06f, 8), C(o.colorSombrero), v(0, 0.3f, -0.01f), default, v(1, 1, 1.08f));
                    break;
                case Sombrero.Panuelo:
                {
                    // pañuelo atado en la cabeza, con el nudo atrás
                    string t = C(o.colorSombrero);
                    cab.Pieza(Geo.Ico(0.16f), t, v(0, 0.3f, -0.03f), default, v(0.97f, 0.72f, 1.06f));
                    cab.Pieza(Geo.Cono(0.07f, 0.13f, 3), t, v(0, 0.2f, -0.17f), v(-2.4f, 0, 0));
                    break;
                }
                case Sombrero.Mantilla:
                    // mantilla negra de encaje sobre una peineta, cayendo por la espalda y los hombros
                    cab.Pieza(Geo.Caja(0.2f, 0.16f, 0.02f), "#3a2a1c", v(0, 0.46f, -0.08f), v(-0.4f, 0, 0));
                    // abierta adelante: cae por la espalda y los hombros y deja la cara a la vista
                    cab.Pieza(Geo.Ico(0.165f), "#1d1a1c", v(0, 0.3f, -0.05f), default, v(0.98f, 0.8f, 1.02f));                           // cubre la cabeza
                    cab.Pieza(Geo.CilindroArco(0.2f, 0.36f, 8, PI * 1.25f), "#1d1a1c", v(0, 0.12f, -0.02f), v(0, PI * 0.375f, 0), v(1, 1, 1.05f));
                    cab.Pieza(Geo.CilindroArco(0.3f, 0.3f, 8, PI * 1.25f), "#1d1a1c", v(0, -0.18f, -0.02f), v(0, PI * 0.375f, 0), v(1.15f, 1, 1.0f));
                    break;
                case Sombrero.Teja:
                {
                    // sombrero de teja del cura: ala larga con los costados enrollados, copa baja
                    const string n = "#241f1c";
                    cab.Pieza(Tronco(0.3f, 0.3f, 0.02f, 8), n, v(0, t0, 0), default, v(0.62f, 1, 1.5f));
                    foreach (var s in new[] { -1f, 1f }) cab.Pieza(Tronco(0.03f, 0.03f, 0.84f, 5), n, v(s * 0.19f, t0 + 0.03f, 0), v(PI / 2, 0, 0));
                    cab.Pieza(Tronco(0.13f, 0.15f, 0.08f, 6), n, v(0, t0 + 0.05f, 0));
                    break;
                }
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
                case Accesorio.Lanza:
                {
                    // lanza de caña tacuara con moharra de hierro y una borla
                    var f = codos[1].Hijo("lanza", v(0, -0.33f, 0.05f));
                    f.Pieza(Tronco(0.018f, 0.022f, 3.4f, 5), "#b8a46a", v(0, 0.95f, 0));
                    f.Pieza(Geo.Cono(0.03f, 0.24f, 3), "#8a8e92", v(0, 2.77f, 0));
                    f.Pieza(Geo.Ico(0.045f), "#c0392b", v(0, 2.55f, 0), default, v(1, 1.6f, 1));
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
            Paleta.Detalle = ConDetalle;
            try { g.Instanciar(frente, registrar, mapa); } finally { Paleta.Detalle = false; }

            var anim = new AnimPersonaje { cuerpo = mapa[cuerpo], torso = mapa[torso], cab = mapa[cab], vestido = estilo == Estilo.Vestido || estilo == Estilo.Quillango || estilo == Estilo.Mirinaque };
            foreach (var p in piernas) anim.piernas.Add(mapa[p]);
            foreach (var r in rodillas) anim.rodillas.Add(mapa[r]);
            foreach (var b in brazos) anim.brazos.Add(mapa[b]);
            foreach (var c in codos) anim.codos.Add(mapa[c]);
            anim.Animar(0, 0);
            return anim;
        }
    }
}
