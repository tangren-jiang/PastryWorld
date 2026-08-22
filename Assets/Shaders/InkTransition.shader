// 水墨转场 Shader（T12）。
// 全屏 UI Image 使用：墨迹从屏幕中心向边缘扩散（覆盖）/收缩（揭示）。
// 边缘用噪声纹理扰动模拟毛笔晕染感。
// 技术预判报告：_TransitionProgress + _InkEdgeSoftness + Simplex/Perlin 噪声扰动。
Shader "PastryWorld/InkTransition"
{
    Properties
    {
        _Progress ("Progress (0=透明 1=全覆盖)", Range(0, 1)) = 0
        _NoiseTex ("Noise Texture" 2D) = "gray" {}
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 3.0
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0, 0.5)) = 0.15
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.3)) = 0.06
        _InkColor ("Ink Color", Color) = (0.05, 0.04, 0.08, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _Progress;
            float _EdgeNoiseScale;
            float _EdgeNoiseStrength;
            float _EdgeSoftness;
            fixed4 _InkColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 径向距离（屏幕中心为原点），0.75 覆盖到四角
                float dist = length(i.uv - 0.5);

                // Perlin 噪声扰动 [-1, 1]，制造毛笔晕染边缘
                float noise = tex2D(_NoiseTex, i.uv * _EdgeNoiseScale).r * 2.0 - 1.0;
                float perturbedDist = dist + noise * _EdgeNoiseStrength;

                // 墨迹边界随 _Progress 扩散
                float inkEdge = _Progress * 0.75;
                float alpha = smoothstep(inkEdge + _EdgeSoftness, inkEdge - _EdgeSoftness, perturbedDist);

                fixed4 col = _InkColor;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
