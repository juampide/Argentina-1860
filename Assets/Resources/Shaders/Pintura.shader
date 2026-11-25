// Posproceso pictórico: pinta la imagen con manchas (filtro Kuwahara), marca los bordes con tinta, agrega grano de película y viñeta.
// El filtro Kuwahara está inspirado en el de Acerola / TxN (MIT: https://github.com/TxN/UnityURP_Kuwahara), reescrito para URP 17 con Render Graph.
Shader "Hidden/Argentina/Pintura"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "Pintura"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Radio, _Intensidad, _Tinta, _Grano, _Vineta;

            float3 Muestra(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb; }
            float Luz(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            // promedio de color en .rgb y varianza de la luz en .a, en un cuadrante de la ventana
            float4 Cuadrante(float2 uv, int x1, int x2, int y1, int y2, float2 paso)
            {
                float suma = 0, suma2 = 0;
                float3 col = 0;
                float n = (x2 - x1 + 1) * (y2 - y1 + 1);
                [loop] for (int x = x1; x <= x2; ++x)
                {
                    [loop] for (int y = y1; y <= y2; ++y)
                    {
                        float3 s = Muestra(uv + float2(x, y) * paso);
                        float l = Luz(s);
                        suma += l; suma2 += l * l; col += s;
                    }
                }
                float m = suma / n;
                return float4(col / n, abs(suma2 / n - m * m));
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float3 original = Muestra(uv);
                float2 paso = _BlitTexture_TexelSize.xy;

                // manchas: el cuadrante más parejo manda (el radio se escala con la resolución)
                int R = (int)clamp(round(_Radio * _ScreenParams.y / 1080.0), 1, 6);
                float4 q1 = Cuadrante(uv, -R, 0, -R, 0, paso);
                float4 q2 = Cuadrante(uv, 0, R, -R, 0, paso);
                float4 q3 = Cuadrante(uv, 0, R, 0, R, paso);
                float4 q4 = Cuadrante(uv, -R, 0, 0, R, paso);
                // mezcla suave por varianza (en vez de elegir uno solo, que titila en las sombras)
                float4 w = 1.0 / (1.0 + 400.0 * float4(q1.a, q2.a, q3.a, q4.a));
                w = w * w;
                float3 pintado = (q1.rgb * w.x + q2.rgb * w.y + q3.rgb * w.z + q4.rgb * w.w) / (w.x + w.y + w.z + w.w);

                // tinta en los bordes (Sobel sobre la imagen original)
                float l00 = Luz(Muestra(uv + float2(-1, -1) * paso)), l10 = Luz(Muestra(uv + float2(0, -1) * paso)), l20 = Luz(Muestra(uv + float2(1, -1) * paso));
                float l01 = Luz(Muestra(uv + float2(-1, 0) * paso)), l21 = Luz(Muestra(uv + float2(1, 0) * paso));
                float l02 = Luz(Muestra(uv + float2(-1, 1) * paso)), l12 = Luz(Muestra(uv + float2(0, 1) * paso)), l22 = Luz(Muestra(uv + float2(1, 1) * paso));
                float gx = -l00 - 2 * l01 - l02 + l20 + 2 * l21 + l22;
                float gy = -l00 - 2 * l10 - l20 + l02 + 2 * l12 + l22;
                float borde = saturate((sqrt(gx * gx + gy * gy) - 0.22) * 3.0);   // umbral alto: solo contornos, no la textura del suelo
                float3 col = lerp(pintado, float3(0.07, 0.05, 0.04), borde * _Tinta);

                // grano de película y viñeta
                // ruido entero (sin senos, que dejan rayas diagonales), cambia ~10 veces por segundo y con grano de 2 px
                uint3 p = uint3(uint2(uv * _ScreenParams.xy * 0.5), 0u);   // grano fijo: no titila
                p = p * 1664525u + 1013904223u;
                p.x += p.y * p.z; p.y += p.z * p.x; p.z += p.x * p.y;
                p ^= p >> 16u;
                p.x += p.y * p.z;
                float ruido = (p.x & 0xFFFFu) / 65535.0 - 0.5;
                col += ruido * _Grano;
                col *= 1.0 - smoothstep(0.35, 0.95, distance(uv, float2(0.5, 0.5))) * _Vineta;

                return float4(lerp(original, saturate(col), _Intensidad), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
