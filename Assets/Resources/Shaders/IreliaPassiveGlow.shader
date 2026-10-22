Shader "MidLane/IreliaPassiveGlow"
{
    // Ionian Fervor at four stacks: a gold glow added on top of Irelia's model. LoL drives the
    // blades' material bloom to orange-gold (Irelia_Blades_Passive_inst: Bloom_Color 0,0,1 ->
    // 1,0.62,0 at four IreliaPassiveStacks); without a bloom pass this draws the blades again,
    // slightly inflated, adding the gold as a fill plus a brighter rim.
    Properties
    {
        _GlowColor ("Glow color", Color) = (1,0.62,0,1)
        _Intensity ("Intensity", Float) = 1
        _Fill ("Fill (facing the camera)", Float) = 0.3
        _RimPower ("Rim power", Float) = 2
        _Inflate ("Inflate (world units)", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor; float _Intensity; float _Fill; float _RimPower; float _Inflate;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 viewWS:TEXCOORD1; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
                float3 positionWS = TransformObjectToWorld(i.positionOS.xyz) + normalWS * _Inflate;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = normalWS;
                o.viewWS = GetWorldSpaceViewDir(positionWS);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                float rim = pow(1 - facing, _RimPower);
                return half4(_GlowColor.rgb * _Intensity * (_Fill + rim), 1);
            }
            ENDHLSL
        }
    }
}
