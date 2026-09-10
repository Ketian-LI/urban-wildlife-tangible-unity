Shader "UrbanWildlife/PaletteHarmonizedSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0,1)) = 0.75
        _Brightness ("Brightness", Range(0.5,1.25)) = 0.92
        _AmbientTint ("Park Ambient Tint", Color) = (0.9,0.95,0.82,1)
        _AmbientBlend ("Park Ambient Blend", Range(0,1)) = 0.14
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
            fixed _Saturation;
            fixed _Brightness;
            fixed4 _AmbientTint;
            fixed _AmbientBlend;

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
                fixed4 colour = tex2D(_MainTex, input.texcoord) * input.color;
                fixed luminance = dot(colour.rgb, fixed3(0.299, 0.587, 0.114));
                fixed3 graded = lerp(luminance.xxx, colour.rgb, saturate(_Saturation));
                graded *= _Brightness;
                graded = lerp(graded, graded * _AmbientTint.rgb, saturate(_AmbientBlend));
                colour.rgb = graded * colour.a;
                return colour;
            }
            ENDCG
        }
    }
}
