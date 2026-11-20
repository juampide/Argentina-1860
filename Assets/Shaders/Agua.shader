// Agua de la pampa: arroyo, laguna, cañadón y bañado. Turbia, entre verde y gris, con ondas que corren con el viento
// (Ambiente: _Viento), destellos de sol, el reflejo del cielo cuando se mira de costado, la orilla que se funde con
// el barro (alfa del color de vértice) y anillos de gotas cuando llueve (_Lluvia).
Shader "Argentina/Agua"
{
    Properties
    {
        _Hondo("Color en lo hondo", Color) = (0.29, 0.31, 0.25, 1)
        _Bajo("Color en la orilla", Color) = (0.52, 0.49, 0.38, 1)
        _Transparencia("Opacidad", Range(0, 1)) = 0.86
        _Ondas("Tamaño de las ondas", Range(0.1, 4)) = 1
        _Brillo("Destellos de sol", Range(0, 3)) = 1.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Hondo;
                half4 _Bajo;
                half _Transparencia;
                half _Ondas;
                half _Brillo;
            CBUFFER_END

            float4 _Viento;   // xy: hacia dónde sopla, z: fuerza (Ambiente.cs)
            float _Lluvia;    // 0 a 1 (Ambiente.cs)

            struct Atributos { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 wp : TEXCOORD0; float4 color : COLOR; float fog : TEXCOORD1; };

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float ruido(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            // altura de las ondas: dos capas que corren con el viento y una tercera, chica, cruzada
            float Ondas(float2 p, float t, float2 dv, float f)
            {
                float2 q = p * _Ondas;
                float h = ruido(q * 0.55 - dv * t * (0.35 + 0.9 * f)) * 0.55;
                h += ruido(q * 1.4 + float2(-dv.y, dv.x) * t * 0.3 - dv * t * (0.6 + 1.2 * f)) * 0.3;
                h += ruido(q * 3.7 - dv * t * 1.6) * 0.15 * (0.4 + f);
                return h;
            }

            // anillos de las gotas de lluvia
            float Gotas(float2 p, float t)
            {
                float2 c = floor(p * 1.6), f = frac(p * 1.6) - 0.5;
                float v = 0;
                for (int k = 0; k < 2; k++)
                {
                    float2 o = float2(hash(c + k * 7.1), hash(c + k * 3.3)) - 0.5;
                    float fase = frac(t * 0.9 + hash(c + k * 11.7));
                    float r = length(f - o * 0.5);
                    v += smoothstep(0.05, 0.0, abs(r - fase * 0.45)) * (1 - fase);
                }
                return v;
            }

            Varyings vert(Atributos i)
            {
                Varyings o;
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                float f = saturate(_Viento.z);
                wp.y += (ruido(wp.xz * 0.3 + _Time.y * 0.4) - 0.5) * 0.03 * (0.3 + f);   // apenas se mueve
                o.wp = wp;
                o.positionCS = TransformWorldToHClip(wp);
                o.color = i.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float f = saturate(_Viento.z);
                float2 dv = dot(_Viento.xy, _Viento.xy) > 0.01 ? normalize(_Viento.xy) : float2(0.6, 0.8);

                // normal a partir de las ondas
                const float e = 0.15;
                float h0 = Ondas(i.wp.xz, t, dv, f);
                float hx = Ondas(i.wp.xz + float2(e, 0), t, dv, f);
                float hz = Ondas(i.wp.xz + float2(0, e), t, dv, f);
                float fuerza = 0.45 + 0.7 * f;
                float g = Gotas(i.wp.xz, t) * _Lluvia;
                float3 n = normalize(float3(-(hx - h0) / e * fuerza, 1, -(hz - h0) / e * fuerza) + float3(0, 0, 0));
                n = normalize(n + float3(ddx(g), 0, ddy(g)) * 3);

                float3 v = normalize(GetWorldSpaceViewDir(i.wp));
                Light luz = GetMainLight(TransformWorldToShadowCoord(i.wp));
                float sombra = luz.shadowAttenuation;

                // color del agua: más clara en la orilla, más oscura en lo hondo; turbia
                float orilla = 1 - saturate(i.color.a);
                half3 agua = lerp(_Hondo.rgb, _Bajo.rgb, orilla * 0.7 + h0 * 0.15);
                // vetas de corriente y reflejos de las crestas: el agua no es lisa
                agua *= 0.92 + 0.16 * h0 + 0.08 * ruido(i.wp.xz * 0.15 - dv * t * 0.25);
                half3 ambiente = SampleSH(float3(0, 1, 0));
                half3 c = agua * (luz.color * sombra * (saturate(dot(n, luz.direction)) * 0.5 + 0.3) + ambiente * 0.8);

                // reflejo del cielo, sobre todo mirando de costado
                float fres = pow(1 - saturate(dot(n, v)), 4);
                c = lerp(c, ambiente * 0.95 + luz.color * 0.04, fres * 0.16 + 0.02);

                // destellos de sol en las crestas
                float3 hv = normalize(luz.direction + v);
                float esp = pow(saturate(dot(n, hv)), 140) * _Brillo * 0.7 + pow(saturate(dot(n, hv)), 700) * _Brillo * 1.6;
                c += luz.color * esp * sombra;

                // espuma sucia en la orilla y gotas de lluvia
                c = lerp(c, half3(0.62, 0.6, 0.52), saturate(orilla * 1.2 - 0.6) * (0.4 + 0.3 * ruido(i.wp.xz * 2 + t * 0.2)));
                c += g * 0.12;

                c = MixFog(c, i.fog);
                return half4(c, saturate(i.color.a) * _Transparencia);
            }
            ENDHLSL
        }
    }
}
