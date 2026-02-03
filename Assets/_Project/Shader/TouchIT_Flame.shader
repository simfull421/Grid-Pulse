Shader "TouchIT/Flame"
{
    Properties
    {
        _Color ("Core Color", Color) = (0.2, 1, 0.2, 1) // 초록 불
        _RimColor ("Rim Color", Color) = (0.8, 1, 0.5, 1) // 가장자리 밝게
        _MainTex ("Noise Tex", 2D) = "white" {}
        _Speed ("Burn Speed", Float) = 2.0
        _Turbulence ("Turbulence", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha One // Additive Blend (빛처럼 밝게 합성)

        CGPROGRAM
        #pragma surface surf Standard vertex:vert alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _RimColor;
        float _Speed;
        float _Turbulence;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        void vert (inout appdata_full v) 
        {
            // 위로 갈수록(Y축) 더 많이 흔들림 (불꽃 모양)
            float noise = sin(_Time.y * _Speed + v.vertex.y * 5.0) * _Turbulence;
            // 위쪽 정점을 좌우로 흔듦
            v.vertex.x += noise * v.vertex.y * 0.5; 
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 노이즈 텍스처를 위로 흘려보냄 (타오르는 느낌)
            float2 scrollUV = IN.uv_MainTex;
            scrollUV.y -= _Time.y * _Speed;
            
            fixed4 c = tex2D (_MainTex, scrollUV) * _Color;
            
            // 림 라이트 (가장자리 발광)
            float rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));
            
            o.Albedo = c.rgb;
            o.Emission = _RimColor.rgb * pow(rim, 3.0); // 가장자리가 빛남
            o.Alpha = c.a * (0.5 + rim); // 중심은 약간 투명, 가장자리는 불투명
        }
        ENDCG
    }
}