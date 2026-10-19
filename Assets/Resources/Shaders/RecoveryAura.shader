Shader "MidLane/RecoveryAura"
{
    Properties
    {
        _Tint ("Team light", Color) = (0.12,0.65,1,1)
        _Mode ("Floor or beams", Float) = 0
        _Radius ("Healing boundary", Float) = 0.9375
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
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Mode;
                float _Radius;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float alpha;
                float pulse = 0.8 + 0.2 * sin(_Time.y * 2);
                if (_Mode < 0.5)
                {
                    float2 p = input.uv * 2 - 1;
                    float r = length(p);
                    float ring = exp(-abs(r - _Radius) * 100);
                    float halo = exp(-abs(r - _Radius) * 20) * 0.22;
                    float innerRing = exp(-abs(r - _Radius * 0.91) * 100) * 0.3;
                    float angle = atan2(p.y, p.x);
                    float sparks = pow(saturate(sin(angle * 32 + _Time.y * 0.8)), 16) * exp(-abs(r - _Radius * 0.98) * 50);
                    alpha = (ring + halo + innerRing + sparks * 0.55) * pulse;
                    alpha += saturate(1 - r / _Radius) * 0.06;
                }
                else
                {
                    float center = pow(saturate(1 - abs(input.uv.x * 2 - 1)), 2);
                    float shimmer = 0.45 + 0.55 * pow(saturate(sin(_Time.y * 2.5 + input.positionWS.x * 7 + input.positionWS.z * 5)), 2);
                    alpha = center * pow(1 - input.uv.y, 2) * shimmer * 0.6;
                }
                return half4(_Tint.rgb * 1.7 + half3(0.12,0.12,0.12), saturate(alpha));
            }
            ENDHLSL
        }
    }
}
