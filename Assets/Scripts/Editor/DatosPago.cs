using System.Collections.Generic;
using UnityEngine;

namespace Argentina1860.Herramientas
{
    /// Datos del pago tomados del prototipo (elenco.js): los quince personajes con nombre, su lugar, su aspecto y su presentación.
    /// Solo se usan para crear las fichas la primera vez; después se editan las fichas en Assets/Datos/Personajes.
    public static class DatosPago
    {
        public class Entrada
        {
            public string id, nombre, lugar;
            public Vector2 xz;           // posición en metros del prototipo
            public Aspecto aspecto;
            public string[] lineas;
        }

        static Color H(string hex) => Conv.Hex(hex);

        public static Aspecto A(Estilo estilo, string ropa = null, string ropa2 = null, string chaleco = null, string pantalon = null, string poncho = null,
            Sombrero sombrero = Sombrero.Ninguno, string colorSombrero = null, bool barba = false, Bigote bigote = Bigote.Ninguno,
            string pelo = null, Peinado peinado = Peinado.SegunEstilo, string piel = null, bool baston = false, Accesorio accesorio = Accesorio.Ninguno, float escala = 1)
        {
            var a = new Aspecto { estilo = estilo, sombrero = sombrero, barba = barba, bigote = bigote, peinado = peinado, baston = baston, accesorio = accesorio, escala = escala };
            if (ropa != null) a.ropa = H(ropa);
            if (ropa2 != null) a.ropa2 = H(ropa2);
            if (chaleco != null) a.chaleco = H(chaleco);
            if (pantalon != null) a.pantalon = H(pantalon);
            if (poncho != null) a.poncho = H(poncho);
            if (colorSombrero != null) a.colorSombrero = H(colorSombrero);
            if (pelo != null) a.pelo = H(pelo);
            if (piel != null) a.piel = H(piel);
            return a;
        }

        /// Peón rural: el protagonista en el pago.
        public static Aspecto Peon() => A(Estilo.Poncho, poncho: "#8a5a3a", sombrero: Sombrero.Ala, colorSombrero: "#4a3a2a", bigote: Bigote.Fino, pelo: "#3a2a1c", peinado: Peinado.Corto, piel: "#d9a27a");

        public static List<Entrada> Elenco()
        {
            Vector2 K(float x, float y) => MapaPago.K(x, y);
            var e = K(0.75f, 0.75f);   // estancia La Soledad
            var r = K(0.59f, 0.9f);    // rancho de peones
            var p = K(2.15f, 1.25f);   // posta y pulpería
            var f = K(0.12f, 1.15f);   // fortín
            var b = K(0.55f, 1.85f);   // puesto del baqueano
            var c = K(1.17f, 0.5f);    // rancho de Casiana, junto al arroyo
            Vector2 V(Vector2 o, float dx, float dz) => new Vector2(o.x + dx, o.y + dz);

            return new List<Entrada>
            {
                new Entrada { id = "celestino", nombre = "Don Celestino Aguirre", lugar = "Estancia La Soledad", xz = V(e, -3, 6.8f),
                    aspecto = A(Estilo.Levita, ropa: "#34343a", chaleco: "#9a4a30", pantalon: "#6e6b66", sombrero: Sombrero.Ala, colorSombrero: "#1f1b18", barba: true, pelo: "#a8a8a4", peinado: Peinado.Corto, baston: true),
                    lineas = new[] { "Vos sos el muchacho nuevo. En La Soledad el que trabaja bien no pasa hambre, y el que no, se va por donde vino.", "Hablá con Fermín, que él reparte las tareas. Y cuidá mis caballos como si fueran tuyos." } },
                new Entrada { id = "mercedes", nombre = "Doña Mercedes", lugar = "Estancia La Soledad", xz = V(e, -19, -9),
                    aspecto = A(Estilo.Vestido, ropa: "#5b6f5a", ropa2: "#efe6d0", pelo: "#6a4a2a"),
                    lineas = new[] { "Pasá, pasá, que tenés cara de no haber probado bocado. Siempre sobra algo de guiso para los peones.", "Mi marido es duro, pero no es mal hombre. Lo preocupan más las deudas que los peones." } },
                new Entrada { id = "carmen", nombre = "Carmen", lugar = "Estancia La Soledad", xz = V(e, 11, 10),
                    aspecto = A(Estilo.Vestido, ropa: "#c9748a", ropa2: "#f4eedc", pelo: "#2a1a12", peinado: Peinado.Largo, escala: 0.9f),
                    lineas = new[] { "¿Es verdad que en Buenos Aires hay calles empedradas y teatros? Me lo contó un carretero y no le creí.", "Algún día me voy de acá. Pero no se lo cuentes a mi padre." } },
                new Entrada { id = "fermin", nombre = "Fermín Lucero", lugar = "Corrales de la estancia", xz = V(e, 31, 30),
                    aspecto = A(Estilo.Poncho, poncho: "#4a4a50", sombrero: Sombrero.Ala, colorSombrero: "#2a2420", bigote: Bigote.Manubrio, pelo: "#1f1a17", piel: "#c99a74"),
                    lineas = new[] { "Escuchame bien: acá se llega temprano y se trabaja hasta que yo diga. Si hacés bien lo tuyo, nos vamos a llevar bien.", "Hoy hay faena en el corral grande. Ya veremos qué sabés hacer." } },
                new Entrada { id = "anacleto", nombre = "Don Anacleto", lugar = "Corral de doma", xz = V(e, -26, 9.5f),
                    aspecto = A(Estilo.Poncho, poncho: "#6a4a30", sombrero: Sombrero.Ala, colorSombrero: "#8a7a5a", barba: true, pelo: "#dcdcd8", piel: "#c99a74", accesorio: Accesorio.Guitarra, escala: 0.97f),
                    lineas = new[] { "Mirá, muchacho, un caballo no se doma a los gritos. Se lo escucha primero, y recién después se le habla.", "Cuando te sientas listo, venite al corral. Y a la noche, si hay guitarra, hay versos." } },
                new Entrada { id = "nicasio", nombre = "Nicasio", lugar = "Ranchos de peones", xz = V(r, 3, 9),
                    aspecto = A(Estilo.Pregonero, ropa: "#6a7a4a", ropa2: "#efe6d0", pantalon: "#7a7060", sombrero: Sombrero.Ala, colorSombrero: "#d8c07a", peinado: Peinado.Puas, pelo: "#3a2a1c", piel: "#d9a27a"),
                    lineas = new[] { "¡Che, qué tal! Yo soy Nicasio, hace dos años que estoy acá. Sentate junto al fogón, que hay mate.", "Si me acompañás en la faena te enseño a sacar un cuero entero sin cortarlo. Pero no le cuentes a Fermín que te lo enseñé yo." } },
                // Isidro atiende detrás del mostrador de la pulpería.
                new Entrada { id = "isidro", nombre = "Don Isidro Barral", lugar = "Pulpería de la posta", xz = V(p, -0.5f, -14.6f),
                    aspecto = A(Estilo.Pregonero, ropa: "#3a4a6a", ropa2: "#f1ead8", pantalon: "#5a5348", sombrero: Sombrero.Gorra, colorSombrero: "#2a2a2a", bigote: Bigote.Manubrio, pelo: "#4a3220", peinado: Peinado.Corto),
                    lineas = new[] { "Buenas, buenas. Tengo yerba, tabaco, telas, aguardiente y lo que se le ocurra al cliente. Y si no tiene plata, ya veremos.", "Todo se anota en la libreta. Yo anoto y no olvido." } },
                new Entrada { id = "juana", nombre = "Doña Juana", lugar = "Posta", xz = V(p, 12, 5),
                    aspecto = A(Estilo.Vestido, ropa: "#a8553a", ropa2: "#f4eedc", pelo: "#3a2415", accesorio: Accesorio.Canasta),
                    lineas = new[] { "Hay puchero y empanadas. Los viajeros pagan, los vecinos se anotan en la libreta, y los peones comen lo que sobra.", "Acá una se entera de todo. Pero también sabe cuándo callarse." } },
                new Entrada { id = "cipriano", nombre = "Cipriano Baigorria", lugar = "Posta", xz = V(p, -10, 4),
                    aspecto = A(Estilo.Levita, ropa: "#5a4a3a", chaleco: "#d9b45a", pantalon: "#8a8478", sombrero: Sombrero.Ala, colorSombrero: "#3a2e26", bigote: Bigote.Fino, pelo: "#3a2a1c", peinado: Peinado.Corto),
                    lineas = new[] { "¡Caballos de remuda, los mejores del camino! Cambio, alquilo o vendo. Pero rápido, que el coche no espera.", "Los viajeros traen noticias del este. Dicen que se viene tiempo de leva." } },
                new Entrada { id = "wenceslao", nombre = "Don Wenceslao Ibáñez", lugar = "Puesto del baqueano", xz = V(b, -6, 4),
                    aspecto = A(Estilo.Poncho, poncho: "#4a5a4a", sombrero: Sombrero.Ala, colorSombrero: "#5a4a3a", barba: true, pelo: "#6e6e6a", piel: "#b98a66"),
                    lineas = new[] { "El campo habla, muchacho, solo hay que saber leerlo. Mirá esa huella: venado, de esta mañana.", "Y fijate en el sur. Hace días que los pájaros se levantan sin motivo. No me gusta nada." } },
                new Entrada { id = "casiana", nombre = "Casiana", lugar = "Rancho junto al arroyo", xz = V(c, 5, 6),
                    aspecto = A(Estilo.Vestido, ropa: "#4a3a5a", ropa2: "#d8c9a8", pelo: "#9a9a96", peinado: Peinado.Rodete, accesorio: Accesorio.Canasta, escala: 0.95f),
                    lineas = new[] { "Tenés cara de cansado. Si te duele algo, tengo yuyos para todo, menos para los males del alma.", "No me gustan los uniformes ni los papeles. Los de la autoridad siempre llegan cuando ya es tarde." } },
                new Entrada { id = "remigio", nombre = "Don Remigio Páez", lugar = "De paso por la pulpería", xz = V(p, -8, -4.8f),
                    aspecto = A(Estilo.Levita, ropa: "#2a2d3a", chaleco: "#8a8a86", pantalon: "#5a5a60", sombrero: Sombrero.Copa, bigote: Bigote.Manubrio, pelo: "#3a3a3a", peinado: Peinado.Corto, baston: true),
                    lineas = new[] { "Soy el juez de paz. Si buscás trabajo, andá con tu papeleta de conchabo en regla; no quiero problemas.", "Ando de paso por la pulpería. Los tiempos están difíciles y el gobierno pide hombres." } },
                new Entrada { id = "obregon", nombre = "Teniente Obregón", lugar = "Fortín de la frontera", xz = V(f, 2, -3),
                    aspecto = A(Estilo.Levita, ropa: "#2c3e6b", chaleco: "#c9a227", pantalon: "#b3342b", sombrero: Sombrero.Kepi, bigote: Bigote.Fino, pelo: "#2b2018", peinado: Peinado.Corto),
                    lineas = new[] { "Teniente Obregón, a su servicio. Este fortín tiene pocos hombres y menos provisiones, pero la frontera no se cuida sola.", "Si ve rastros o gente extraña, avíseme. Y no ande solo de noche." } },
                new Entrada { id = "lorenzo", nombre = "Lorenzo", lugar = "Posta", xz = V(p, 26, 2),
                    aspecto = A(Estilo.Poncho, poncho: "#7a5a3a", sombrero: Sombrero.Ala, colorSombrero: "#8a6a4a", pelo: "#2a1c14", peinado: Peinado.Largo, piel: "#c2906a"),
                    lineas = new[] { "Yo hablo con los de acá y con los de allá. Se pueden hacer buenos tratos si uno sabe escuchar.", "No todo lo que se dice de la frontera es verdad. Ni de un lado ni del otro." } },
                // Pascual camina junto a la primera carreta; hasta portar la fauna queda quieto en el camino.
                new Entrada { id = "pascual", nombre = "Pascual", lugar = "Carretero del camino real", xz = V(p, -40, -10),
                    aspecto = A(Estilo.Pregonero, ropa: "#8a6a3a", ropa2: "#efe6d0", pantalon: "#7a6a50", sombrero: Sombrero.Ala, colorSombrero: "#6a5a3a", bigote: Bigote.Manubrio, pelo: "#3a2a1c", peinado: Peinado.Corto),
                    lineas = new[] { "Llevo cuero y lana a Buenos Aires. Es un camino largo, pero se paga bien.", "Si algún día necesitás ir a la ciudad, hablame. Siempre hay lugar en la carreta para uno más, si es gente de bien." } },
            };
        }
    }
}
