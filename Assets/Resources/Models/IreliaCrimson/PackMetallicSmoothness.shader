Shader "Hidden/MidLane/PackMetallicSmoothness"
{
    Properties { _MainTex ("Metallic", 2D) = "black" {} _Roughness ("Roughness", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _Roughness;
            float4 frag(v2f_img input) : SV_Target
            { return float4(tex2D(_MainTex, input.uv).r, 0, 0, 1 - tex2D(_Roughness, input.uv).r); }
            ENDHLSL
        }
    }
}
