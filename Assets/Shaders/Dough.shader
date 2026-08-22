// 点心世界 - 面团材质着色器
// 技术预判报告 T3/A2：材质属性动画，_CookProgress + _Smoothness
// URP 2D 兼容，移动端 Fragment 指令数 ≤128
Shader "PastryWorld/Dough"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Texture", 2D) = "white" {}
        _SmoothnessMap ("Smoothness Mask", 2D) = "white" {}

        [HDR] _BaseColor ("Base Color", Color) = (1,1,1,1)
        _RawColor ("Raw Dough Color", Color) = (0.82, 0.72, 0.55, 1)
        _CookedColor ("Cooked Dough Color", Color) = (0.95, 0.88, 0.72, 1)

        _CookProgress ("Cook Progress", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1

        _RimPower ("Rim Light Power", Range(0.5, 8)) = 3.0
        _RimColor ("Rim Light Color", Color) = (1, 0.95, 0.8, 0.3)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "DoughForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS  : NORMAL;
                float3 viewDirWS  : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SmoothnessMap);
            SAMPLER(sampler_SmoothnessMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _SmoothnessMap_ST;
                half4 _BaseColor;
                half4 _RawColor;
                half4 _CookedColor;
                half  _CookProgress;
                half  _Smoothness;
                half  _RimPower;
                half4 _RimColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetCameraPositionWS() - worldPos;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 基础纹理
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // 蒸制颜色渐变（生→熟 Lerp）
                half3 doughColor = lerp(_RawColor.rgb, _CookedColor.rgb, _CookProgress);

                // 光滑度遮罩 × 全局光滑度参数
                half smoothMask = SAMPLE_TEXTURE2D(_SmoothnessMap, sampler_SmoothnessMap, IN.uv).r;
                half smoothness = smoothMask * _Smoothness;

                // Rim 光照（光滑面团有高光边缘）
                half3 normalWS = normalize(IN.normalWS);
                half3 viewDir = normalize(IN.viewDirWS);
                half rim = 1.0 - max(0.0, dot(normalWS, viewDir));
                rim = pow(rim, _RimPower);
                half3 rimColor = _RimColor.rgb * rim * smoothness;

                // 合成
                half3 finalColor = baseTex.rgb * doughColor * _BaseColor.rgb + rimColor;

                // 雾
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, baseTex.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/2D/Sprite"
}
