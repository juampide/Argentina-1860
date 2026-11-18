// Matas de pasto de la pampa: se mecen con el viento y se aplastan cerca del jugador (campo.js).
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 _PastoJugador;

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
                float3 ip = float3(m._m03, 0, m._m23);
                float3 p = i.positionOS.xyz;
                float hh = p.y;
                float t = _Time.y;
                float w = sin(t * 1.7 + ip.x * 0.21 + ip.z * 0.17) * 0.5 + sin(t * 2.9 + ip.x * 0.5 - ip.z * 0.4) * 0.25;
                p.x += w * 0.09 * hh * hh;
                p.z += w * 0.05 * hh * hh;
                float dj = distance(ip.xz, _PastoJugador.xz);
                p.y *= lerp(0.3, 1.0, smoothstep(0.8, 2.8, dj));   // las que quedan junto al jugador se aplastan
                VertexPositionInputs vp = GetVertexPositionInputs(p);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _ColorInst);
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
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
