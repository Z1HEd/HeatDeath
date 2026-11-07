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
            float _Amplitude;
            float _Frequency;
            float _Taper;
            float _PixelsPerUnit;
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

            // Sine-free hash, stable for large inputs.
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            // Linear (not smooth) interpolation keeps straight segments between sharp kinks.
            float valueNoise(float x, float seed)
            {
                float i = floor(x);
                float a = hash11(i + seed * 17.13);
                float b = hash11(i + 1 + seed * 17.13);
                return lerp(a, b, frac(x)) * 2 - 1;
            }

            float boltNoise(float x, float seed)
            {
                return valueNoise(x, seed) * 0.6
                     + valueNoise(x * 2.3, seed + 13.1) * 0.28
                     + valueNoise(x * 5.7, seed + 29.7) * 0.12;
            }

            // Sideways offset of the bolt at distance t along the hop; eases to 0 at both ends.
            float offsetAt(float t, float len, float seed)
            {
                float taper = max(min(_Taper, len * 0.5), 1e-4);
                float env = smoothstep(0, taper, t) * smoothstep(0, taper, len - t);
                return boltNoise(t * _Frequency, seed) * _Amplitude * env;
            }

            // Approximate perpendicular distance to the strand, with round caps past the ends.
            float strandDistance(float t, float s, float len, float seed)
            {
                const float e = 0.01;
                float tc = clamp(t, 0, len);
                float off = offsetAt(tc, len, seed);
                float slope = (offsetAt(tc + e, len, seed) - offsetAt(tc - e, len, seed)) / (2 * e);
                float d = abs(s - off) / sqrt(1 + slope * slope);
                return length(float2(d, t - tc));
            }

            // x = core coverage, y = glow falloff.
            float2 strand(float d, float coreWidth, float glowWidth)
            {
                // Hard edge when pixel snapped, anti-aliased when smooth.
                float aa = _PixelsPerUnit > 0 ? 1e-5 : max(fwidth(d), 1e-5);
                float core = saturate((coreWidth * 0.5 - d) / aa + 0.5);
                float glow = saturate(1 - d / max(glowWidth, 1e-4));
                return float2(core, glow * glow);
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
