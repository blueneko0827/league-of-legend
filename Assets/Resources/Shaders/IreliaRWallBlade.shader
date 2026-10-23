Shader "MidLane/IreliaRWallBlade"
{
    // The R wall's blades (IreliaBlades unit) as LoL draws them with Irelia_bladeglow_inst:
    // Irelia's blades texture with its albedo remapped (0.1..1), self-illuminated, plus two
    // fresnel rims (lavender and blue) and a little of the material's blue bloom, which make the
    // blades read pale violet with bright edges in the game footage. Two-sided, opaque: sinking
    // blades are hidden by the ground as in LoL.
    Properties
    {
        _BaseMap ("Blades texture (irelia_base_blades_tx_cm)", 2D) = "white" {}
        _AlbedoMin ("Albedo remap min", Float) = 0.1
        _AlbedoMax ("Albedo remap max", Float) = 1
        _Fresnel1Color ("Fresnel 1 color", Color) = (0.706,0.667,1,1)
        _Fresnel1Size ("Fresnel 1 size", Float) = 3.161
        _Fresnel1Bias ("Fresnel 1 bias", Float) = -1
        _Fresnel2Color ("Fresnel 2 color", Color) = (0.22,0.337,1,1)
        _Fresnel2Size ("Fresnel 2 size", Float) = 0.881
        _Fresnel2Bias ("Fresnel 2 bias", Float) = -0.54
        _BloomColor ("Bloom color", Color) = (0.251,0.227,1,1)
        _BloomIntensity ("Bloom intensity", Float) = 0.8
        _BloomFill ("Bloom drawn as fill (no bloom pass)", Float) = 0.15
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _AlbedoMin, _AlbedoMax;
                float4 _Fresnel1Color; float _Fresnel1Size, _Fresnel1Bias;
                float4 _Fresnel2Color; float _Fresnel2Size, _Fresnel2Bias;
                float4 _BloomColor; float _BloomIntensity, _BloomFill;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half3 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half3 albedo = lerp(_AlbedoMin.xxx, _AlbedoMax.xxx, texel);
                // Two-sided: the rim is measured on whichever face the camera sees.
                float edge = 1 - abs(dot(normalize(i.normalWS), GetWorldSpaceNormalizeViewDir(i.positionWS)));
                float fresnel1 = saturate(edge * _Fresnel1Size + _Fresnel1Bias);
                float fresnel2 = saturate(edge * _Fresnel2Size + _Fresnel2Bias);
                half3 color = albedo + _Fresnel1Color.rgb * fresnel1 + _Fresnel2Color.rgb * fresnel2 +
                    _BloomColor.rgb * _BloomIntensity * _BloomFill;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
