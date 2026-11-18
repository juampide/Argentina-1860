using UnityEngine;

namespace Argentina1860
{
    /// Altura del suelo en un punto del mundo de Unity: usa el Terrain si hay (así respeta lo que se esculpió a mano)
    /// y si no, la fórmula del pago.
    public static class Terreno
    {
        public static float Altura(Vector3 mundo)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null) continue;
                var p = t.transform.position;
                var s = t.terrainData.size;
                if (mundo.x >= p.x && mundo.x <= p.x + s.x && mundo.z >= p.z && mundo.z <= p.z + s.z)
                    return t.SampleHeight(mundo) + p.y;
            }
            return MapaPago.Altura(mundo.x, -mundo.z);
        }
    }
}
