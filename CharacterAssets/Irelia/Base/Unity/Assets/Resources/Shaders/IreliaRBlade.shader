Shader "MidLane/IreliaRBlade"
{
    Properties
    {
        _BaseMap ("Official blade atlas", 2D) = "white" {}
        _Tint ("Edge light", Color) = (0.3,0.7,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.uv=i.uv; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                clip(_Opacity-.01);
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                float rim=pow(1-abs(dot(normalize(i.normalWS),GetWorldSpaceNormalizeViewDir(i.positionWS))),2);
                return half4(color*1.25+_Tint.rgb*(.12+rim*.8),_Opacity);
            }
            ENDHLSL
        }
    }
}
