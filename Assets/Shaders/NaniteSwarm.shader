// Nanite Bombs' blast on a 1-unit circle sprite scaled up by Explosion: a wave of dark grey nanites. The front is a
// dense band of fine grain that shimmers and twitches with flickering glints; behind it the swarm thins out in
// drifting patches.
// The grain is measured in world units, so a bigger blast shows a bigger wave, not coarser grain. As Explosion fades
// the sprite's alpha, the wave falls apart grain by grain. Alpha-blended; the sprite colour tints.
Shader "Custom/NaniteSwarm"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _DarkColor ("Dark Grey", Color) = (0.3, 0.32, 0.36, 1)
        _LightColor ("Silver", Color) = (0.82, 0.86, 0.9, 1)
        _GlintColor ("Glint", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.9
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _BandWidth ("Wave Depth (share of radius)", Range(0, 1)) = 0.45
        _Patchiness ("Patchiness Of The Thinning Trail", Range(0, 1)) = 0.8
        _TrailScale ("Trail Patches Per World Unit", Float) = 1.5
        _GrainScale ("Grain Clumps Per World Unit", Float) = 8
        _GrainDrift ("Grain Crawl Speed", Float) = 1.5
        _GlintShare ("Share Of Grains That Glint", Range(0, 0.2)) = 0.04
        _GlintSpeed ("Glint Flicker Speed", Float) = 12
        _ShimmerSpeed ("Grain Shimmer Speed", Float) = 8
        _ShimmerAmount ("Grain Shimmer Amount", Range(0, 1)) = 0.5
        _TwitchRate ("Grain Twitches Per Second", Float) = 6
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Noise2D.cginc"

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

            fixed4 _DarkColor;
            fixed4 _LightColor;
            fixed4 _GlintColor;
            float _Opacity;
            float _PixelsPerUnit;
            float _BandWidth;
            float _Patchiness;
            float _TrailScale;
            float _GrainScale;
            float _GrainDrift;
            float _GlintShare;
            float _GlintSpeed;
            float _ShimmerSpeed;
            float _ShimmerAmount;
            float _TwitchRate;

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
                float2 pixel = _PixelsPerUnit > 0 ? floor(p * _PixelsPerUnit) : floor(p * 50);
                if (_PixelsPerUnit > 0)
                    p = (pixel + 0.5) / _PixelsPerUnit;

                float r = length(p) / max(IN.outerRadius, 1e-3); // 0 at the centre, 1 at the blast's edge
                float t = _Time.y;
                float life = IN.color.a;
                float2 seed = unity_ObjectToWorld._m03_m13 * 0.37;

                // A sharp front just inside the edge. Behind it the swarm thins out: fewer grains the further back,
                // trailing off in irregular patches that drift, instead of ending at an edge.
                float front = 1 - smoothstep(0.93, 0.97, r);
                float behind = saturate((0.95 - r) / max(_BandWidth, 1e-3)); // 0 at the front, 1 deep inside
                float patches = fbm2((p + seed) * _TrailScale + float2(-t, t) * 0.4) - 0.5;
                float keep = saturate(1 - behind + patches * 2 * _Patchiness);

                // Metallic grain crawling in world units, plus a speckle per pixel that twitches to a new value at
                // its own random moments, so the swarm never sits still.
                float clumps = fbm2((p + seed) * _GrainScale + float2(t, -t) * _GrainDrift);
                float own = hash21(pixel + floor(seed * 10));
                float speckle = hash21(pixel + floor(t * _TwitchRate + own * 7.3) * 1.37);
                float grain = saturate(clumps * 0.7 + speckle * 0.4);

                // Each grain catches the light on its own rhythm.
                float shimmer = 0.5 + 0.5 * sin(t * _ShimmerSpeed * (0.5 + own) + own * 40);
                grain = saturate(grain * lerp(1, shimmer * 1.6, _ShimmerAmount));
                float3 rgb = lerp(_DarkColor.rgb, _LightColor.rgb, grain);

                // A few grains glint, each on its own flicker.
                float glint = step(1 - _GlintShare, speckle)
                            * step(0.5, frac(t * _GlintSpeed * (0.5 + hash21(pixel + 3.7)) + speckle * 10));
                rgb = lerp(rgb, _GlintColor.rgb, glint);

                // Each grain is there if the trail keeps it, and falls away at its own moment as the blast fades.
                float present = step(hash21(pixel + 5.5), keep) * step(1 - life, hash21(pixel + 11.3));
                float density = front * saturate(0.55 + clumps * 0.6) * present;
                return fixed4(rgb * IN.color.rgb, density * _Opacity);
            }
            ENDCG
        }
    }
}
