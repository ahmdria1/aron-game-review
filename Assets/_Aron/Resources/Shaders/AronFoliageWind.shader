// Foliage that moves in the wind (URP, forward). Vertex sway from the global wind, trunk to tip; palm fronds flutter more;
// gusts pulse. Works with the SRP Batcher (all material data is in UnityPerMaterial) and with GPU instancing.
// The Low tier compiles a cheaper single-sine path (global keyword ARON_FOLIAGE_LOW, set by FoliageWind.cs).
// Shadows follow the swaying vertices. Not verified in the Unity editor yet: open it once and check the console.
Shader "Aron/FoliageWind"
{
    Properties
    {
        [MainColor] _BaseColor ("Colour", Color) = (0.3, 0.5, 0.25, 1)
        _Gloss ("Sun sheen", Range(0, 1)) = 0.1
        [KeywordEnum(Height, VertexColor, UV2)] _Mask ("Sway weight from", Float) = 0
        _HeightRange ("Height range (m), height mode", Float) = 4
        _SwayScale ("Sway scale", Range(0, 2)) = 1
        _FlutterScale ("Flutter scale", Range(0, 2)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Gloss;
            float _HeightRange, _SwayScale, _FlutterScale, _Cull;
        CBUFFER_END
        #include "AronFoliageWind.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ARON_FOLIAGE_LOW
            #pragma shader_feature_local _MASK_HEIGHT _MASK_VERTEXCOLOR _MASK_UV2
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half fog : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionWS = AronFoliageSway(v.positionOS.xyz, v.color, v.uv2, _HeightRange, _SwayScale, _FlutterScale);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light l = GetMainLight(shadowCoord);
                // Leaves are lit from both sides a little: a soft wrap instead of a hard terminator.
                half ndl = saturate(dot(n, l.direction) * 0.8 + 0.2);
                half3 light = l.color * (ndl * l.shadowAttenuation * l.distanceAttenuation);
                half3 ambient = SampleSH(n);
                half3 col = _BaseColor.rgb * (light + ambient);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                half sheen = pow(saturate(dot(n, normalize(l.direction + viewDir))), 24.0) * _Gloss;
                col += l.color * sheen * l.shadowAttenuation;
                col = MixFog(col, i.fog);
                return half4(col, 1);
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
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ARON_FOLIAGE_LOW
            #pragma shader_feature_local _MASK_HEIGHT _MASK_VERTEXCOLOR _MASK_UV2
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionWS = AronFoliageSway(v.positionOS.xyz, v.color, v.uv2, _HeightRange, _SwayScale, _FlutterScale);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE * cs.w);
            #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE * cs.w);
            #endif
                o.positionCS = cs;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ARON_FOLIAGE_LOW
            #pragma shader_feature_local _MASK_HEIGHT _MASK_VERTEXCOLOR _MASK_UV2

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformWorldToHClip(AronFoliageSway(v.positionOS.xyz, v.color, v.uv2, _HeightRange, _SwayScale, _FlutterScale));
                return o;
            }

            half frag(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
