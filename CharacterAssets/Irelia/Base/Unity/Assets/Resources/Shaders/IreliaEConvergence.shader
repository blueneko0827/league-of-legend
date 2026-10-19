Shader "MidLane/IreliaEConvergence"
{
    Properties
    {
        _BaseMap ("Official E texture", 2D) = "white" {}
        _MaskMap ("Official beam mask", 2D) = "white" {}
        _UseMask ("Use beam mask", Float) = 0
        _Tint ("Light", Color) = (.45,.8,1,1)
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
            TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint; float _Opacity, _UseMask;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 authoredUV:TEXCOORD1; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.uv=float2(i.positionOS.z+.5, i.positionOS.x+.5); o.authoredUV=i.uv; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float4 source=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                float light=source.r*source.a;
                float mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,i.authoredUV+float2(_Time.y*.8,0)).r;
                light*=lerp(1,.55+.45*mask,_UseMask);
                return half4(_Tint.rgb*1.8,light*_Opacity);
            }
            ENDHLSL
        }
    }
}
