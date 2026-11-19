// Jam look (StatusVisual, over each weapon): the weapon flickers red with glitchy static, inside its sprite. Whole
// flickers, a few glitching rows, and per-pixel static, each on its own clock. Additive; the sprite alpha
// (StatusVisual's fade) fades it.
Shader "Custom/StatusJam"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 0.15, 0.1, 1)
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _FlickerRate ("Flickers Per Second", Float) = 10
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.5
        _Rows ("Glitch Rows Across The Weapon", Float) = 8
        _GlitchShare ("Share Of Rows Glitching", Range(0, 1)) = 0.25
        _GlitchRate ("Glitch Changes Per Second", Float) = 14
        _Static ("Static Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "DisableBatching"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Noise2D.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float2 p : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _PixelsPerUnit;
            float _FlickerRate;
            float _FlickerStrength;
            float _Rows;
            float _GlitchShare;
            float _GlitchRate;
            float _Static;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                OUT.p = IN.vertex.xy;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float mask = tex2D(_MainTex, IN.uv).a;
                float t = _Time.y;
                float2 pixel = floor(IN.p * (_PixelsPerUnit > 0 ? _PixelsPerUnit : 50));

                float flicker = step(0.5, hash21(float2(floor(t * _FlickerRate), 3.3))) * _FlickerStrength;
                float glitchTick = floor(t * _GlitchRate);
                // Rows across the weapon in its own space, so the bands don't depend on where its sprite sits.
                float glitch = step(1 - _GlitchShare, hash21(float2(floor(IN.p.y * _Rows * 2), glitchTick)));
                float noise = hash21(pixel + glitchTick * 1.7) * _Static;

                float intensity = saturate(flicker + glitch * 0.8 + noise) * mask * IN.color.a;
                return fixed4(_Color.rgb * intensity, 1);
            }
            ENDCG
        }
    }
}
