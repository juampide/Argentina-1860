// Cielo de la pampa: degradado del horizonte (que se funde con la niebla) al cénit, sol con halo, luna, estrellas y nubes
// que el viento arrastra y que reciben la luz del sol. Lo maneja Ambiente.cs (hora, clima y estación).
Shader "Argentina/Cielo"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float3 _CieloCenit, _CieloHorizonte, _CieloSuelo, _CieloSolColor, _CieloSolDir, _CieloLunaDir;
            float _CieloNubes, _CieloNoche, _CieloLuz;
            float2 _CieloViento;

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }
            float fbm(float2 p)
            {
                float a = 0.5, s = 0;
                for (int k = 0; k < 5; k++) { s += a * noise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;

                // degradado: el horizonte (color de la niebla) sube de a poco hacia el cénit
                float3 col = lerp(_CieloHorizonte, _CieloCenit, pow(saturate(h), 0.5));
                if (h < 0) col = lerp(_CieloHorizonte, _CieloSuelo, saturate(-h * 5));

                // sol: halo ancho (más fuerte cuando está bajo, como el sol de invierno) y disco
                float s = saturate(dot(d, _CieloSolDir));
                float cubierto = _CieloNubes;
                col += _CieloSolColor * (pow(s, 5) * 0.22 + pow(s, 40) * 0.45) * (1 - 0.7 * cubierto);
                float disco = smoothstep(0.99955, 0.9998, s) * (1 - saturate(cubierto * 1.4)) * step(0, _CieloSolDir.y + 0.02);
                col = lerp(col, _CieloSolColor * 3.5, disco);

                // luna y estrellas, solo de noche
                float noche = _CieloNoche;
                if (noche > 0.01 && h > 0)
                {
                    float m = smoothstep(0.99935, 0.9996, saturate(dot(d, _CieloLunaDir)));
                    float2 e = d.xz / (h + 0.12) * 160;
                    float est = step(0.9965, hash(floor(e))) * (0.5 + 0.5 * sin(_Time.y * 2 + hash(floor(e)) * 40));
                    col += (float3(0.9, 0.93, 1.0) * m * 1.2 + float3(1, 1, 1) * est * 0.8) * noche * (1 - cubierto);
                }

                // nubes: un plano alto que corre con el viento; se aclaran del lado del sol y se oscurecen con la cobertura
                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.22) * 1.5 + _CieloViento;
                    float c = fbm(uv);
                    float umbral = lerp(0.72, 0.3, cubierto);
                    float nube = smoothstep(umbral, umbral + 0.2, c) * smoothstep(0.0, 0.22, h);
                    float c2 = fbm(uv + _CieloSolDir.xz * 0.07);
                    float luz = saturate((c - c2) * 7 + 0.55);
                    float3 sombra = _CieloHorizonte * 0.6 * lerp(1, 0.55, cubierto) + 0.02 * _CieloLuz;
                    float3 claro = (_CieloSolColor * 0.55 + _CieloHorizonte * 0.55) * _CieloLuz;
                    col = lerp(col, lerp(sombra, claro, luz), nube * 0.92);
                }
                return float4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
