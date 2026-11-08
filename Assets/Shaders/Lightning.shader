// Procedural lightning drawn inside a LineRenderer quad. The bolt is computed per pixel
// from the hop's real world endpoints (_Start, _End, set per renderer through a
// MaterialPropertyBlock), with noise sampled in world units along the hop, so hops of any
// length get the same kink density and nothing stretches. The quad must be wide enough to
// hold 2 * (_Amplitude + _GlowWidth).
Shader "Custom/Lightning"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (0.85, 0.95, 1, 1)
        _GlowColor ("Glow Color (alpha = intensity)", Color) = (0.3, 0.6, 1, 0.8)
        _CoreWidth ("Core Width (world units)", Float) = 0.04
        _GlowWidth ("Glow Width (world units)", Float) = 0.2
        _Amplitude ("Amplitude (world units)", Float) = 0.25
        _Frequency ("Kinks per world unit", Float) = 3
        _Taper ("End Taper (world units)", Float) = 0.3
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _SecondaryStrength ("Secondary Strand Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "LightningCommon.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 worldPos : TEXCOORD0;
            };

            fixed4 _CoreColor;
            fixed4 _GlowColor;
            float _CoreWidth;
            float _GlowWidth;
            float _SecondaryStrength;

            // Per renderer, from MaterialPropertyBlock.
            float4 _Start;
            float4 _End;
            float _Seed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                OUT.color = IN.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = IN.worldPos;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                float2 hop = _End.xy - _Start.xy;
                float len = max(length(hop), 1e-4);
                float2 dir = hop / len;
                float2 rel = p - _Start.xy;
                float t = dot(rel, dir);
                float s = dot(rel, float2(-dir.y, dir.x));

                float2 primary = strand(strandDistance(t, s, len, _Seed), _CoreWidth, _GlowWidth);
                float2 secondary = strand(strandDistance(t, s, len, _Seed + 37.1), _CoreWidth * 0.5, _GlowWidth * 0.6)
                                 * _SecondaryStrength;

                float core = max(primary.x, secondary.x);
                float glow = max(primary.y, secondary.y);
                float3 rgb = lerp(glow * _GlowColor.rgb * _GlowColor.a, _CoreColor.rgb, core);

                // Additive: vertex colour tints, vertex alpha fades.
                return fixed4(rgb * IN.color.rgb * IN.color.a, 1);
            }
            ENDCG
        }
    }
}
