Shader "TouchIT/Jelly"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        // [핵심] 꿀렁거림 제어 변수
        _WobbleAmount ("Wobble Amount", Range(0, 0.5)) = 0.1
        _WobbleSpeed ("Wobble Speed", Range(0, 20)) = 5.0
        _Frequency ("Frequency", Range(0, 10)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        float _WobbleAmount;
        float _WobbleSpeed;
        float _Frequency;

        // [정점 변형 함수] 여기가 핵심입니다.
        void vert (inout appdata_full v) 
        {
            // 시간과 위치를 기반으로 사인파(Sine Wave)를 만듭니다.
            // 물 스피커처럼 꿀렁거리는 효과
            float wave = sin(_Time.y * _WobbleSpeed + v.vertex.y * _Frequency);
            
            // 법선(Normal) 방향으로 정점을 밀고 당깁니다.
            v.vertex.xyz += v.normal * wave * _WobbleAmount;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}