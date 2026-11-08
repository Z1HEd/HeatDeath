// EMP blast on a 1-unit circle sprite scaled up by Explosion: a shock front. One continuous
// gradient runs from nothing in the middle to a hot rim at the blast's hit radius, with a sharp
// leading edge and a jagged trailing side. A wall of short electric tongues rises from the rim
// and points inward, like flames. It fades out from the inner side toward the rim, so the rim
// goes last. Colours come from the material; SpriteRenderer alpha drives the fade.
Shader "Custom/EMPWave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _CoreColor ("Core Color", Color) = (0.85, 0.95, 1, 1)
        _GlowColor ("Glow Color (alpha = tongue glow)", Color) = (0.3, 0.6, 1, 0.8)
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50

        [Header(Gradient)]
        _WaveDepth ("Depth (share of radius)", Range(0, 1)) = 0.27
        _Unevenness ("Unevenness Of Inner Side", Range(0, 1)) = 0.25
        _JagDensity ("Jags Per World Unit Of Rim", Float) = 1.5
        _JagInterval ("Seconds To Morph To A New Jag Shape", Float) = 0.07
        _FadeSoftness ("Softness Of The Fade Edge (world units)", Float) = 0.3
        _GradientPower ("Gradient Curve", Float) = 2.5
        _GradientStrength ("Gradient Strength", Float) = 0.42
        _RimPower ("Rim Tightness", Float) = 10
        _RimStrength ("Rim Strength", Float) = 0.54

        [Header(Electric Tongues)]
        _TongueDensity ("Tongues Per World Unit Of Rim", Float) = 3.5
        _TongueCoverage ("Share Of Tongues Lit", Range(0, 1)) = 0.9
        _TongueMinHeight ("Min Height (world units)", Float) = 0.1
        _TongueMaxHeight ("Max Height (world units)", Float) = 0.45
        _TongueWidth ("Core Width At The Base (world units)", Float) = 0.03
        _TongueGlow ("Glow Width At The Base (world units)", Float) = 0.12
        _TongueKinks ("Kinks Per World Unit", Float) = 14
        _TongueWobble ("Zigzag Size At The Tip (world units)", Float) = 0.06
        _TongueJitter ("Seconds Between Shape Changes", Float) = 0.04
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
            float _WaveDepth;
            float _Unevenness;
            float _JagDensity;
            float _JagInterval;
            float _FadeSoftness;
            float _GradientPower;
            float _GradientStrength;
            float _RimPower;
            float _RimStrength;
            float _TongueDensity;
            float _TongueCoverage;
            float _TongueMinHeight;
            float _TongueMaxHeight;
            float _TongueWidth;
            float _TongueGlow;
            float _TongueKinks;
            float _TongueWobble;
            float _TongueJitter;

            // Random points around the circle joined by straight lines, so it has sharp corners.
            // The point count is whole, so it wraps cleanly at the seam. Returns -1 to 1.
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

            // x = core, y = glow of the tongue in this stretch of the rim: a main strand and two
            // shorter ones forking off to its sides. Each is rooted at the rim, narrows toward its
            // tip, and zigzags more the further inward it gets.
            float2 electricTongue(float aroundRim, float depth, float arcLength, float density,
                                  float heightScale, float layerSeed)
            {
                float cells = max(floor(arcLength * density), 3);
                float position = aroundRim * cells;
                float cell = floor(position);

                // Each tongue re-rolls on its own clock, so the wall flickers instead of
                // changing all at once.
                float clock = _Time.y / max(_TongueJitter, 1e-3) + hash11(cell + layerSeed);
                float id = cell + layerSeed + fmod(floor(clock / 2), 97) * 13.7;
                float shapeId = id + fmod(floor(clock), 97) * 7.3;

                float exists = step(hash11(id + 23.9), _TongueCoverage);
                float tall = hash11(id + 5.1);
                float height = lerp(_TongueMinHeight, _TongueMaxHeight, tall * tall) * heightScale;

                float cellWidth = arcLength / cells;
                float across = (position - cell - 0.5 - (hash11(id + 11.3) - 0.5) * 0.4) * cellWidth;

                // At least one pixel wide, or the thin tips would break up.
                float minCore = _PixelsPerUnit > 0 ? 1 / _PixelsPerUnit : 0;

                float core = 0;
                float glow = 0;
                for (int i = 0; i < 3; i++)
                {
                    float strandHeight = height * (1 - i * 0.25);
                    float alongStrand = saturate(depth / strandHeight);
                    float narrowing = 1 - alongStrand;

                    float side = i == 0 ? 0 : (i == 1 ? -1 : 1);
                    float fork = side * cellWidth * (0.18 + 0.12 * hash11(id + i * 3.7)) * alongStrand;
                    float zigzag = boltNoise(depth * _TongueKinks, shapeId + i * 19.7) * _TongueWobble * alongStrand;

                    float d = abs(across - fork - zigzag);
                    float2 lit = strand(d, max(_TongueWidth * narrowing, minCore), _TongueGlow * narrowing)
                               * step(depth, strandHeight);
                    core = max(core, lit.x);
                    glow += lit.y;
                }

                return float2(core, saturate(glow)) * exists;
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

                float fade = IN.color.a;
                float radius = length(p);
                float ringRadius = max(IN.outerRadius - 0.03, 1e-3);
                float depth = ringRadius - radius; // positive inside the rim
                float inside = step(0, depth);
                float aroundRim = (atan2(p.y, p.x) + UNITY_PI) / UNITY_TWO_PI;
                float arcLength = UNITY_TWO_PI * ringRadius;

                // One curve from the transparent middle to the hot rim, with a jagged inner side.
                float jag = jaggedEdge(aroundRim, arcLength);
                float reach = ringRadius * _WaveDepth * (1 + jag * _Unevenness);
                float wave = saturate(1 - depth / max(reach, 1e-3)) * inside;

                float3 rgb = _GlowColor.rgb * pow(wave, _GradientPower) * _GradientStrength
                           + _CoreColor.rgb * pow(wave, _RimPower) * _RimStrength;

                // Layers of different spacing, so the wall doesn't look like a regular comb:
                // the main one, a sparser taller one, and a denser shorter one packing the base.
                float2 mainLayer = electricTongue(aroundRim, depth, arcLength, _TongueDensity, 1, 3.1);
                float2 tallLayer = electricTongue(aroundRim, depth, arcLength, _TongueDensity * 0.63, 1.35, 61.7);
                float2 denseLayer = electricTongue(aroundRim, depth, arcLength, _TongueDensity * 1.7, 0.7, 127.3);

                float tongueCore = max(max(mainLayer.x, tallLayer.x), denseLayer.x) * inside;
                float tongueGlow = saturate(mainLayer.y + tallLayer.y + denseLayer.y) * inside;

                rgb += tongueGlow * _GlowColor.rgb * _GlowColor.a;
                rgb = lerp(rgb, _CoreColor.rgb, tongueCore);

                // The fade is an edge sweeping from the deepest point out to the rim.
                float deepest = max(reach, _TongueMaxHeight * 1.35);
                float fadeEdge = lerp(-_FadeSoftness, deepest + _FadeSoftness, fade);
                float visible = saturate((fadeEdge - depth) / max(_FadeSoftness, 1e-3));

                // Additive. The sprite's own colour is ignored; its alpha only drives the fade.
                return fixed4(rgb * visible, 1);
            }
            ENDCG
        }
    }
}
