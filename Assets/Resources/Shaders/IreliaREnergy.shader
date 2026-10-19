Shader "MidLane/IreliaREnergy"
{
    Properties
    {
        _BaseMap ("Official energy texture", 2D) = "white" {}
        _Tint ("Energy light", Color) = (0.25,0.65,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float energy=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).r;
                float pulse=.88+.12*sin(_Time.y*5+i.uv.x*20);
                return half4(_Tint.rgb*1.7,energy*_Opacity*pulse);
            }
            ENDHLSL
        }
    }
}
