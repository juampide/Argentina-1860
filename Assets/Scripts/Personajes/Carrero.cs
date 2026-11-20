using UnityEngine;

namespace Argentina1860
{
    /// <summary>
    /// El carrero maneja la carreta sentado adelante, en la punta de la caja, con las piernas colgando sobre la lanza
    /// y la picana (una caña larga con un clavo en la punta) tendida hacia los bueyes, como en las fotos de época.
    /// Lo usan la carreta de Juan Maidana en el camino real (Fauna) y la de la llegada (LlegadaP1).
    /// </summary>
    public static class Carrero
    {
        const string NOMBRE_PICANA = "picana";

        /// Sienta al carrero en el frente de la carreta. El modelo de la carreta (ModelosFauna.Carreta) tiene los
        /// bueyes hacia -forward de su transform.
        public static void Ubicar(Transform carrero, AnimPersonaje anim, Transform carreta, float t, bool anda = true)
        {
            var frente = -carreta.forward;
            carrero.SetPositionAndRotation(carreta.position + frente * 2.55f + carreta.right * 0.25f + Vector3.up * 1.18f, Quaternion.LookRotation(frente));
            // mira a los bueyes; de vez en cuando gira la cabeza (y al andar, la mano de la picana se mueve un poco)
            anim?.Sentado(t, anda ? 0.35f : 0.1f, Mathf.Sin(t * 0.17f) * 0.5f);
            Picana(carreta);
        }

        /// Pone la picana en la carreta (una sola vez): desde la mano del carrero hasta arriba de los bueyes.
        public static void Picana(Transform carreta)
        {
            if (carreta.Find(NOMBRE_PICANA) != null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = NOMBRE_PICANA;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
            go.transform.SetParent(carreta, false);
            // de la mano (adelante, a la altura del pecho del carrero sentado) hacia la cruz de los bueyes, bajando apenas
            var desde = new Vector3(0.45f, 2.2f, -2.95f);
            var hasta = new Vector3(0.1f, 2.0f, -8.4f);
            go.transform.localPosition = (desde + hasta) / 2;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, hasta - desde);
            go.transform.localScale = new Vector3(0.035f, (hasta - desde).magnitude / 2, 0.035f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Paleta.Mat("#9a8256");   // caña de tacuara
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
