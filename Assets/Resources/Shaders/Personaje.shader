// Material de los personajes armados por código: iluminación normal de URP (sombras, luces, niebla) más detalle de superficie
// calculado desde la posición en el modelo (no hace falta UV): variación de tono por manchas, trama de hilos, arrugas finas y mugre.
// Las sombras y la profundidad usan los pases del Lit de URP.
Shader "Argentina/Personaje"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        _Detalle ("Detalle", Range(0,1)) = 1
        _Suciedad ("Suciedad", Range(0,1)) = 0.6
        _Trama ("Trama de tela", Range(0,1)) = 0.6
        _Brillo ("Brillo", Range(0,1)) = 0.04
        [NoScaleOffset] _BaseMap ("Textura (horneada en Blender)", 2D) = "white" {}
        _UsarMapa ("Usar textura", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Detalle, _Suciedad, _Trama, _Brillo, _UsarMapa;
            CBUFFER_END
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Atributos { float4 posOS : POSITION; float3 nOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float3 posWS : TEXCOORD0;
                float3 nWS : TEXCOORD1;
                float3 posOS : TEXCOORD2;
                float3 nOS : TEXCOORD3;
                half fog : TEXCOORD4;
                float2 uv : TEXCOORD5;
            };

            Varyings Vert(Atributos i)
            {
                Varyings o;
                VertexPositionInputs vp = GetVertexPositionInputs(i.posOS.xyz);
                o.posCS = vp.positionCS;
                o.posWS = vp.positionWS;
                o.nWS = TransformObjectToWorldNormal(i.nOS);
                o.posOS = i.posOS.xyz;
                o.nOS = i.nOS;
                o.fog = ComputeFogFactor(vp.positionCS.z);
                o.uv = i.uv;
                return o;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + float3(0.1, 0.2, 0.3));
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Ruido(float3 x)
            {
                float3 i = floor(x), f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            float Fbm(float3 p) { return Ruido(p) * 0.55 + Ruido(p * 2.1) * 0.3 + Ruido(p * 4.3) * 0.15; }

            half4 Frag(Varyings i, FRONT_FACE_TYPE cara : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float lado = IS_FRONT_VFACE(cara, 1.0, -1.0);
                float3 p = i.posOS;
                float3 an = abs(i.nOS);
                float2 uv = (an.x > an.y && an.x > an.z) ? p.yz : (an.y > an.z ? p.xz : p.xy);

                float manchas = Fbm(p * 14.0);        // manchas de unos 7 cm
                float fino = Fbm(p * 45.0);           // arrugas y grano
                float3 col = _BaseColor.rgb;
                float det = _Detalle * lerp(1.0, 0.3, _UsarMapa);        // con textura horneada el detalle fino ya viene en la imagen
                if (_UsarMapa > 0.5) col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                col *= 1.0 + (manchas - 0.5) * 0.55 * det;
                float hilos = abs(sin(uv.y * 3.14159 * 34.0)) * (0.6 + 0.4 * abs(sin(uv.x * 3.14159 * 34.0)));
                col *= 1.0 - _Trama * det * 0.12 * smoothstep(0.55, 1.0, hilos);
                col *= 1.0 + (fino - 0.5) * 0.3 * det;
                float mugre = saturate((Fbm(p * 9.0 + 7.3) - 0.5) * 3.0) * _Suciedad * det;
                col = lerp(col, col * float3(0.5, 0.42, 0.33), mugre);

                SurfaceData s = (SurfaceData)0;
                s.albedo = saturate(col);
                s.metallic = 0;
                s.specular = 0;
                s.smoothness = _Brillo;
                s.occlusion = 1.0 - mugre * 0.25;
                s.alpha = 1;
                s.normalTS = float3(0, 0, 1);

                InputData d = (InputData)0;
                d.positionWS = i.posWS;
                d.positionCS = i.posCS;
                d.normalWS = normalize(i.nWS) * lado;
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.posWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.posWS);
                d.fogCoord = i.fog;
                d.vertexLighting = half3(0, 0, 0);
                d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.posCS);
                d.shadowMask = half4(1, 1, 1, 1);

                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, d.fogCoord);
                c.a = 1;
                return c;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }
    Fallback Off
}
