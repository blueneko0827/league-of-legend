Shader "MidLane/RiotParticle"
{
    // Unlit particle shader for the official Riot emitters (RiotVfxPlayer).
    // UVs arrive in LoL texture space (v down) and are flipped here for Unity textures.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ErosionTex ("Erosion map", 2D) = "white" {}
        _MultTex ("Multiplied texture", 2D) = "white" {}
        _UV2 ("Multiplied UV scale/offset", Vector) = (1,1,0,0)
        _UseMult ("Use multiplied texture", Float) = 0
        _Color ("Color", Color) = (1,1,1,1)
        _UV ("UV scale/offset", Vector) = (1,1,0,0)
        _Erosion ("Erosion (drive, feather in, feather out, use alpha)", Vector) = (0,0,0.1,0)
        _UseErosion ("Use erosion", Float) = 0
        _Premultiply ("Premultiply color by alpha", Float) = 0
        _AlphaClip ("Alpha clip", Float) = 0
        _Inflate ("Normal inflate (world units)", Float) = 0
        _VertexColor ("Use vertex color", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_ErosionTex); SAMPLER(sampler_ErosionTex);
            TEXTURE2D(_MultTex); SAMPLER(sampler_MultTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _ErosionTex_ST; float4 _MultTex_ST;
                float4 _Color; float4 _UV; float4 _UV2; float4 _Erosion;
                float _UseErosion; float _UseMult; float _Premultiply; float _AlphaClip; float _Inflate; float _VertexColor;
                float _SrcBlend; float _DstBlend; float _Cull;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 raw:TEXCOORD1; float2 uv2:TEXCOORD2; float4 color:COLOR; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                world += TransformObjectToWorldNormal(i.normalOS) * _Inflate;
                o.positionCS = TransformWorldToHClip(world);
                float2 uv = i.uv * _UV.xy + _UV.zw;
                o.uv = float2(uv.x, 1 - uv.y);
                o.raw = float2(i.uv.x, 1 - i.uv.y);
                float2 uv2 = i.uv * _UV2.xy + _UV2.zw;
                o.uv2 = float2(uv2.x, 1 - uv2.y);
                // Skinned character meshes (attached-mesh glow) carry no vertex colors.
                o.color = lerp(float4(1, 1, 1, 1), i.color, _VertexColor);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color * i.color;
                if (_UseMult > 0.5) color *= SAMPLE_TEXTURE2D(_MultTex, sampler_MultTex, i.uv2);
                if (_UseErosion > 0.5)
                {
                    half4 map = SAMPLE_TEXTURE2D(_ErosionTex, sampler_ErosionTex, i.raw);
                    half value = lerp(map.r, map.a, _Erosion.w);
                    half feather = max(_Erosion.z, 0.01);
                    half threshold = _Erosion.x * (1 + feather);
                    color.a *= saturate((value - threshold + feather) / feather);
                }
                clip(color.a - _AlphaClip);
                if (_Premultiply > 0.5) color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }
}
