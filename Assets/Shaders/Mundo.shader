// Material del mundo: URP con iluminación física, más la suciedad procedural de suciedad.js (barro, hollín,
// chorreaduras, grano, grietas del adobe y raspones) y el recorte tramado de xray.js (hueco alrededor de la línea
// cámara→jugador para árboles y cercos). Las variables _Xray* las pone la cámara isométrica.
Shader "Argentina/Mundo"
{
    Properties
    {
        [MainTexture] _BaseMap("Textura", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Fuerza del relieve", Float) = 1
        _Smoothness("Suavidad", Range(0, 1)) = 0
        _Metallic("Metálico", Range(0, 1)) = 0
        [HDR] _EmissionColor("Emisión", Color) = (0, 0, 0, 1)
        _Suciedad("Suciedad", Range(0, 2)) = 1
        _Grietas("Grietas", Range(0, 1)) = 0
        _Absorbe("Se moja con la lluvia (0 = nada, 1 = suelo de tierra)", Range(0, 1)) = 0.35
        [Toggle] _Xray("Recorte tramado cuando tapa al jugador", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Caras", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half _BumpScale;
            half _Smoothness;
            half _Metallic;
            half _Suciedad;
            half _Grietas;
            half _Absorbe;
            half _Xray;
            half _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

        float4 _XrayJug;   // xyz: pecho del jugador, w: altura de sus pies
        float4 _XrayDir;   // dirección cámara → jugador
        float _XrayRad;    // radio del hueco (0 = apagado)
        float _Humedad;    // 0 seco, 1 empapado (la pone Ambiente.cs con la lluvia)

        float sH(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        float sN(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(sH(i), sH(i + float2(1, 0)), f.x), lerp(sH(i + float2(0, 1)), sH(i + float2(1, 1)), f.x), f.y);
        }
        float sV(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            float d1 = 8.0, d2 = 8.0;
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 o = float2(sH(i + g), sH(i + g + 17.3));
                    float2 r = g + o - f;
                    float d = dot(r, r);
                    if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                }
            return sqrt(d2) - sqrt(d1);
        }
        float sF(float2 p) { return 0.55 * sN(p) + 0.3 * sN(p * 2.13 + 3.1) + 0.15 * sN(p * 4.37 + 7.7); }

        float Hueco(float3 wp)
        {
            if (_Xray < 0.5 || _XrayRad <= 0.0) return 0.0;
            float3 rel = wp - _XrayJug.xyz;
            float ad = dot(rel, -_XrayDir.xyz);          // cuánto está hacia la cámara
            float3 lat = rel + _XrayDir.xyz * ad;         // distancia al rayo cámara→jugador
            float dd = length(lat);
            return (1.0 - smoothstep(_XrayRad * 0.55, _XrayRad, dd)) * smoothstep(0.4, 2.0, ad) * smoothstep(0.7, 1.7, wp.y - _XrayJug.w);
        }

        float Trama(float4 positionCS) { return frac(52.9829189 * frac(dot(positionCS.xy, float2(0.06711056, 0.00583715)))); }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float fog : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Atributos i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                VertexPositionInputs vp = GetVertexPositionInputs(i.positionOS.xyz);
                VertexNormalInputs vn = GetVertexNormalInputs(i.normalOS, i.tangentOS);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS = vn.normalWS;
                o.tangentWS = float4(vn.tangentWS, i.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fog = ComputeFogFactor(vp.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, bool frente : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float h = Hueco(i.positionWS);
                clip(Trama(i.positionCS) - h);
                if (!frente && h > 0.02)   // interior de lo que se ve por el hueco: oscuro, como una sección
                {
                    float k = saturate((i.positionWS.y - _XrayJug.w) / 3.2);
                    return half4(lerp(half3(0.075, 0.055, 0.04), half3(0.2, 0.15, 0.11), k), 1);
                }

                float3 n = normalize(i.normalWS) * (frente ? 1.0 : -1.0);
                #if defined(_NORMALMAP)
                    float3 nts = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                    float3 t = normalize(i.tangentWS.xyz);
                    float3 b = i.tangentWS.w * cross(n, t);
                    n = normalize(TransformTangentToWorld(nts, half3x3(t, b, n)));
                #endif

                half3 alb = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;

                // ---------- mugre y desgaste (suciedad.js) ----------
                float3 g = i.positionWS;
                float vertical = 1.0 - abs(n.y);                 // 1 en paredes, 0 en el suelo
                float b1 = sF(g.xz * 0.9 + g.y * 0.8), b2 = sF(g.xz * 0.16 + g.y * 0.35 + 7.0);
                float chor = sN(float2((g.x + g.z) * 2.4, g.y * 0.3)) * vertical;   // chorreaduras verticales
                float d = saturate(b1 * 0.55 + b2 * 0.7 + chor * 0.3 - 0.62) * _Suciedad;
                alb = lerp(alb, alb * half3(0.34, 0.26, 0.18) * 1.5, clamp(d * 1.5, 0.0, 0.85));
                alb *= lerp(1.0, 0.8 + 0.2 * sN(g.xz * 7.0 + g.y * 6.0), saturate(_Suciedad));   // grano fino
                if (_Grietas > 0.0)
                {
                    float2 cp = float2(g.x + g.z, g.y) * 3.4;
                    cp += 0.35 * float2(sN(cp * 1.7), sN(cp * 1.7 + 9.0));
                    float cr = 1.0 - smoothstep(0.0, 0.05, sV(cp));
                    cr = max(cr, (1.0 - smoothstep(0.0, 0.04, sV(cp * 2.3 + 5.0))) * 0.5);
                    alb *= 1.0 - _Grietas * cr * vertical;
                }
                float desgaste = smoothstep(0.78, 0.9, sF(g.xz * 1.7 + g.y * 1.3 + 21.0));   // raspones claros, como cal caída
                alb = lerp(alb, alb * 1.25 + 0.03, desgaste * 0.4 * _Suciedad);

                InputData id = (InputData)0;
                id.positionWS = i.positionWS;
                id.normalWS = n;
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                id.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                id.fogCoord = i.fog;
                id.bakedGI = SampleSH(n);
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                id.shadowMask = half4(1, 1, 1, 1);

                // mojado: la tierra se oscurece y brilla; las paredes, menos
                float moja = _Humedad * _Absorbe * lerp(0.45, 1.0, saturate(n.y));
                alb *= lerp(1.0, 0.62, moja);

                SurfaceData s = (SurfaceData)0;
                s.albedo = alb;
                s.alpha = 1;
                s.metallic = _Metallic;
                s.smoothness = max(_Smoothness, 0.55 * moja);
                s.normalTS = half3(0, 0, 1);
                s.occlusion = 1;
                #if defined(_EMISSION)
                    s.emission = _EmissionColor.rgb;
                #endif

                half4 c = UniversalFragmentPBR(id, s);
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vs
            #pragma fragment fs
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Atributos { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };

            float4 vs(Atributos i) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 wp = TransformObjectToWorld(i.positionOS.xyz);
                float3 nw = TransformObjectToWorldNormal(i.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(wp, nw, _MainLightPosition.xyz));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs;
            }

            half4 fs() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vd
            #pragma fragment fd
            #pragma multi_compile_instancing

            struct Atributos { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 wp : TEXCOORD0; };

            Varyings vd(Atributos i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.wp = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.wp);
                return o;
            }

            half fd(Varyings i) : SV_Target
            {
                clip(Trama(i.positionCS) - Hueco(i.wp));
                return i.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vn
            #pragma fragment fn
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Atributos { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 wp : TEXCOORD0; float3 n : TEXCOORD1; };

            Varyings vn(Atributos i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.wp = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.wp);
                o.n = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }

            half4 fn(Varyings i, bool frente : SV_IsFrontFace) : SV_Target
            {
                clip(Trama(i.positionCS) - Hueco(i.wp));
                float3 n = normalize(i.n) * (frente ? 1.0 : -1.0);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
                #else
                    return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
