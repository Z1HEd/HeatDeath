// Shock Field blast on a 1-unit circle sprite scaled up by Explosion: a ring of separate lightning arcs along the
// rim over a glow that runs from nothing in the middle to a hot rim, like the EMP's gradient. Every arc re-rolls its
// shape on a timer and has a thinner second strand. Colours come from the material; the SpriteRenderer colour tints,
// and its alpha (Explosion's fade) fades.
Shader "Custom/LightningRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _CoreColor ("Core Color", Color) = (0.85, 0.95, 1, 1)
        _GlowColor ("Glow Color (alpha = intensity)", Color) = (0.3, 0.6, 1, 0.8)
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50

        [Header(Arcs)]
        _CoreWidth ("Core Width (world units)", Float) = 0.05
        _GlowWidth ("Glow Width (world units)", Float) = 0.2
        _Amplitude ("Amplitude (world units)", Float) = 0.25
        _Frequency ("Kinks per world unit", Float) = 5
        _Taper ("End Taper (world units)", Float) = 0.2
        _SecondaryStrength ("Second Strand Strength", Range(0, 1)) = 0.7
        _Arcs ("Arcs Along The Rim", Float) = 16
        _ArcGap ("Gap Between Arcs (share of an arc)", Range(0, 0.9)) = 0.1
        _JitterInterval ("Seconds Between Shape Changes", Float) = 0.03

        [Header(Gradient)]
        _GradientDepth ("Depth (share of radius)", Range(0, 1)) = 0.3
        _GradientPower ("Gradient Curve", Float) = 2.5
        _GradientStrength ("Gradient Strength", Float) = 0.45
        _RimPower ("Rim Tightness", Float) = 10
        _RimStrength ("Rim Strength", Float) = 0.5
        _EdgeSoftness ("Fade Past The Rim (world units)", Float) = 0.3
        _Unevenness ("Unevenness Of Inner Side", Range(0, 1)) = 0.25
        _JagDensity ("Jags Per World Unit Of Rim", Float) = 1.5
        _JagInterval ("Seconds To Morph To A New Jag Shape", Float) = 0.07
        _FadeSoftness ("Softness Of The Fade Edge (world units)", Float) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            // Reads its scale from unity_ObjectToWorld, which batching replaces with identity.
            "DisableBatching"="True"
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "LightningCommon.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex      : SV_POSITION;
                fixed4 color       : COLOR;
                float2 fromCentre  : TEXCOORD0; // world units
                float  outerRadius : TEXCOORD1; // world units
            };

            fixed4 _CoreColor;
            fixed4 _GlowColor;
            float _CoreWidth;
            float _GlowWidth;
            float _SecondaryStrength;
            float _Arcs;
            float _ArcGap;
            float _JitterInterval;
            float _GradientDepth;
            float _GradientPower;
            float _GradientStrength;
            float _RimPower;
            float _RimStrength;
            float _EdgeSoftness;
            float _Unevenness;
            float _JagDensity;
            float _JagInterval;
            float _FadeSoftness;

            // As in EMPWave: random points around the circle joined by straight lines, with a whole point count so it
            // wraps at the seam. Returns -1 to 1.
            float ringNoise(float aroundRim, float points, float seed)
            {
                float position = aroundRim * points;
                float i = floor(position);
                float a = hash11(fmod(i, points) + seed);
                float b = hash11(fmod(i + 1, points) + seed);
                return lerp(a, b, position - i) * 2 - 1;
            }

            float jaggedShape(float aroundRim, float points, float seed)
            {
                return ringNoise(aroundRim, points, seed) * 0.6
                     + ringNoise(aroundRim, points * 2, seed + 13.1) * 0.28
                     + ringNoise(aroundRim, points * 5, seed + 29.7) * 0.12;
            }

            // Jagged variation around the rim, -1 to 1, morphing from one random shape to the next.
            float jaggedEdge(float aroundRim, float arcLength)
            {
                float points = max(floor(arcLength * _JagDensity), 3);
                float clock = _Time.y / max(_JagInterval, 1e-3);
                float tick = floor(clock);
                return lerp(jaggedShape(aroundRim, points, fmod(tick, 97) * 7.1),
                            jaggedShape(aroundRim, points, fmod(tick + 1, 97) * 7.1),
                            clock - tick);
            }

            v2f vert(appdata_t IN)
            {
                float scale = length(unity_ObjectToWorld._m00_m10_m20);

                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.fromCentre = IN.vertex.xy * scale;
                OUT.outerRadius = scale * 0.5;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = IN.fromCentre;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                // The rim sits inside the sprite's edge, so the zigzag and glow fit on it.
                float rimRadius = max(IN.outerRadius - _Amplitude - _GlowWidth, 1e-3);
                float radius = length(p);
                float angle = atan2(p.y, p.x) + UNITY_PI;

                // One curve from the transparent middle to the hot rim, with a jagged inner side that morphs like
                // the EMP's. Past the rim it fades out under the arcs instead of ending at a hard edge.
                float depth = rimRadius - radius; // positive inside the rim
                float aroundRim = angle / UNITY_TWO_PI;
                float jag = jaggedEdge(aroundRim, UNITY_TWO_PI * rimRadius);
                float reach = rimRadius * _GradientDepth * (1 + jag * _Unevenness);
                float wave = depth >= 0
                    ? saturate(1 - depth / max(reach, 1e-3))
                    : saturate(1 + depth / max(_EdgeSoftness, 1e-3));

                // As in the EMP, the glow fades as an edge sweeping from its deepest point out to the rim.
                float fadeEdge = lerp(-_FadeSoftness, reach + _FadeSoftness, IN.color.a);
                float visible = saturate((fadeEdge - depth) / max(_FadeSoftness, 1e-3));
                float3 rgb = (_GlowColor.rgb * pow(wave, _GradientPower) * _GradientStrength
                           + _CoreColor.rgb * pow(wave, _RimPower) * _RimStrength) * visible;

                // The arc this angle falls in: along the rim from the arc's start, and across it as the distance
                // from the rim. The gap is split between both ends.
                float shape = fmod(floor(_Time.y / max(_JitterInterval, 1e-3)), 97);
                float arcStep = UNITY_TWO_PI / max(_Arcs, 1);
                float arc = floor(angle / arcStep);
                float arcLength = arcStep * rimRadius * (1 - _ArcGap);
                float alongArc = (angle - arc * arcStep) * rimRadius - arcStep * rimRadius * _ArcGap * 0.5;
                float acrossArc = radius - rimRadius;
                float arcSeed = arc * 5.3 + shape * 11.3 + 41.0;

                float2 primary = strand(strandDistance(alongArc, acrossArc, arcLength, arcSeed), _CoreWidth, _GlowWidth);
                float2 secondary = strand(strandDistance(alongArc, acrossArc, arcLength, arcSeed + 37.1),
                    _CoreWidth * 0.5, _GlowWidth * 0.6) * _SecondaryStrength;

                float core = max(primary.x, secondary.x);
                float glow = max(primary.y, secondary.y);
                // The arcs fade with the sprite's alpha; the glow already faded with its sweep.
                rgb += glow * _GlowColor.rgb * _GlowColor.a * IN.color.a;
                rgb = lerp(rgb, _CoreColor.rgb * IN.color.a, core);

                // Additive: sprite colour tints.
                return fixed4(rgb * IN.color.rgb, 1);
            }
            ENDCG
        }
    }
}
