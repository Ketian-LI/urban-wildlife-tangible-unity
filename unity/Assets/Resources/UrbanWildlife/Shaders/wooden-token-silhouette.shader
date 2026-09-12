Shader "UrbanWildlife/WoodenTokenSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WoodLight ("Wood Light", Color) = (0.72,0.43,0.19,1)
        _WoodDark ("Wood Dark", Color) = (0.28,0.12,0.045,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _GrainScale ("Grain Scale", Range(8,120)) = 52
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _WoodLight;
            fixed4 _WoodDark;
            fixed _Opacity;
            float _GrainScale;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed alpha = tex2D(_MainTex, input.texcoord).a * input.color.a * _Opacity;
                float grainWave = sin(
                    input.texcoord.y * _GrainScale * 6.28318 +
                    sin(input.texcoord.x * 17.0) * 2.4);
                float fineGrain = sin(
                    input.texcoord.y * _GrainScale * 13.1 +
                    input.texcoord.x * 29.0);
                fixed grain = saturate(0.52 + grainWave * 0.22 + fineGrain * 0.08);
                fixed3 wood = lerp(_WoodDark.rgb, _WoodLight.rgb, grain) * input.color.rgb;
                return fixed4(wood * alpha, alpha);
            }
            ENDCG
        }
    }
}
