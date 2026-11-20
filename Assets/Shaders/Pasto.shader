// Matas de pasto de la pampa: se mecen con el viento y se doblan donde pasó alguien (campo.js).
// Las pisadas vienen en _PisadaTex (Pasto.cs): R = cuánto está aplastada, GB = hacia dónde se dobló.
// Se dibujan con Graphics.RenderMeshInstanced; cada instancia trae su color en _ColorInst.
Shader "Argentina/Pasto"
{
    Properties { }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_PisadaTex);
            SAMPLER(sampler_PisadaTex);
            float4 _PisadaOrigen;   // x, z de la esquina y 1 / tamaño en metros
            float _PisadaFuerza;
            float4 _Viento;         // xy: hacia dónde sopla (x, z de Unity), z: fuerza de 0 a 1 (la pone Ambiente.cs)

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorInst)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                float fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Atributos i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float4x4 m = UNITY_MATRIX_M;
                float3 ip = float3(m._m03, m._m13, m._m23);
                float3 p = i.positionOS.xyz;
                float hh = p.y;

                // pisada: cuánto está aplastada esta mata y hacia dónde
                float2 uvp = (ip.xz - _PisadaOrigen.xy) * _PisadaOrigen.z;
                float4 pis = SAMPLE_TEXTURE2D_LOD(_PisadaTex, sampler_PisadaTex, uvp, 0);
                float dentro = step(0, uvp.x) * step(uvp.x, 1) * step(0, uvp.y) * step(uvp.y, 1);
                float a = pis.r * dentro * _PisadaFuerza;
                float2 dir = pis.gb * 2 - 1;

                float t = _Time.y;
                float fuerza = _Viento.z;
                float2 dirV = dot(_Viento.xy, _Viento.xy) > 0.01 ? normalize(_Viento.xy) : float2(0.6, 0.8);
                // ráfagas que recorren el campo en la dirección del viento
                float fase = dot(ip.xz, dirV) * 0.12 - t * (1.2 + 2.5 * fuerza);
                float rafaga = 0.5 + 0.5 * sin(fase) * sin(fase * 0.37 + ip.x * 0.05);
                float temblor = sin(t * (1.7 + fuerza * 3) + ip.x * 0.21 + ip.z * 0.17) * 0.5 + sin(t * 2.9 + ip.x * 0.5 - ip.z * 0.4) * 0.25;

                float3 pw = TransformObjectToWorld(p);
                float alto = pw.y - ip.y;
                float doblez = (temblor * (0.08 + 0.14 * fuerza) + rafaga * (0.08 + fuerza * 0.75) + fuerza * 0.12) * (1 - a);
                pw.xz += dirV * doblez * alto * alto * 0.9;
                pw.xz += float2(-dirV.y, dirV.x) * temblor * 0.03 * alto * alto * (1 - a);
                // la hoja se acuesta: baja la punta y la corre hacia afuera, más cuanto más alta
                pw.y = ip.y + alto * lerp(1.0, 0.18, a);
                pw.xz += dir * a * alto * 0.9;

                VertexPositionInputs vp = GetVertexPositionInputs(TransformWorldToObject(pw));
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _ColorInst);
                o.color.rgb *= lerp(1.0, 0.8, a);   // la pisada se ve más oscura y amarillenta
                // la ráfaga dobla las matas y muestra el envés, más claro: una ola de color que recorre el campo
                o.color.rgb *= 1.0 + (rafaga - 0.45) * (0.14 + 0.32 * fuerza) * (1 - a);
                o.color.b *= lerp(1.0, 0.75, a);
                o.fog = ComputeFogFactor(vp.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, bool frente : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS) * (frente ? 1 : -1);
                Light luz = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, luz.direction)) * 0.6 + 0.4;
                half3 c = i.color.rgb * (luz.color * ndl * luz.shadowAttenuation + SampleSH(n));
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    // oclusión ambiental: el pasto se oscurece al pie de las paredes, bajo los árboles y en los rincones
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                    c *= lerp(1.0, ao.indirectAmbientOcclusion, 0.8);
                #endif
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
