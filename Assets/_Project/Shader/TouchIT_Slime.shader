Shader "TouchIT/Slime"
{
    Properties
    {
        _Color ("Color", Color) = (0.2, 1, 0.2, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.8 // 빤질빤질하게
        _Metallic ("Metallic", Range(0,1)) = 0.1
        
        // C# 스크립트에서 넣어줄 관성 벡터 (이동 방향의 반대 힘)
        _Inertia ("Inertia Vector", Vector) = (0,0,0,0)
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
        float4 _Inertia; // XYZ: 방향 및 강도

        // [정점 변형] 물리가 적용되는 곳
        void vert (inout appdata_full v) 
        {
            // 월드 공간에서의 관성 적용
            // 구체의 중심에서 멀수록(표면일수록) 관성을 더 많이 받음
            // 즉, 중심은 고정되고 껍데기만 밀리는 효과
            
            // 간단하게: 관성 벡터 방향으로 정점을 밉니다.
            // 정점의 로컬 위치를 기준으로 반대편은 덜 밀리게 하여 찌그러짐 유도
            float lag = dot(v.vertex.xyz, normalize(_Inertia.xyz + float3(0.001,0,0))); 
            
            // 관성 적용 (이동 반대 방향으로 늘어짐)
            v.vertex.xyz -= _Inertia.xyz * (1.0 + lag) * 0.5;
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