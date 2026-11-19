// Nanites look (StatusVisual, over the hull): dark grey grain crawling over the ship in drifting patches, inside its
// sprite. The grain is the Nanite Bombs' wave's (Custom/NaniteSwarm): metallic clumps, twitching speckle, each grain
// shimmering on its own rhythm, and a few glints. Alpha-blended; the sprite alpha (StatusVisual's fade) fades it.
Shader "Custom/StatusNanites"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _DarkColor ("Dark Grey", Color) = (0.11, 0.11, 0.12, 1)
        _LightColor ("Light Grey", Color) = (0.36, 0.37, 0.39, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _Coverage ("Share Of The Hull Covered", Range(0, 1)) = 0.5
        _PatchScale ("Patches Per World Unit", Float) = 3
        _CrawlSpeed ("Crawl Speed", Float) = 0.6
        _GlintColor ("Glint", Color) = (0.75, 0.77, 0.8, 1)
        _GrainScale ("Grain Clumps Per World Unit", Float) = 8
        _GrainDrift ("Grain Drift Speed", Float) = 1.5
        _ShimmerSpeed ("Grain Shimmer Speed", Float) = 8
        _ShimmerAmount ("Grain Shimmer Amount", Range(0, 1)) = 0.5
        _TwitchRate ("Grain Twitches Per Second", Float) = 6
        _GlintShare ("Share Of Grains That Glint", Range(0, 0.2)) = 0.04
        _GlintSpeed ("Glint Flicker Speed", Float) = 12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "DisableBatching"="True" }
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

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float2 p : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _DarkColor;
            fixed4 _LightColor;
            float _Opacity;
            float _PixelsPerUnit;
            float _Coverage;
            float _PatchScale;
            float _CrawlSpeed;
            fixed4 _GlintColor;
            float _GrainScale;
            float _GrainDrift;
            float _ShimmerSpeed;
            float _ShimmerAmount;
            float _TwitchRate;
            float _GlintShare;
            float _GlintSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                // World units from the ship's centre, so the grain moves with it.
                OUT.p = mul(unity_ObjectToWorld, IN.vertex).xy - unity_ObjectToWorld._m03_m13;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float mask = tex2D(_MainTex, IN.uv).a;
                float2 p = IN.p;
                float2 pixel = floor(p * (_PixelsPerUnit > 0 ? _PixelsPerUnit : 50));
                float t = _Time.y;

                // Patches of grain crawling over the hull.
                float patches = fbm2(p * _PatchScale + float2(t, -t * 0.7) * _CrawlSpeed);
                float keep = saturate((patches - (1 - _Coverage)) * 4 + 0.5);

                // The wave's grain: metallic clumps drifting, plus a speckle per pixel that twitches to a new value.
                float clumps = fbm2(p * _GrainScale + float2(t, -t) * _GrainDrift);
                float own = hash21(pixel);
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

                float present = step(hash21(pixel + 5.5), keep);
                return fixed4(rgb, present * mask * _Opacity * IN.color.a);
            }
            ENDCG
        }
    }
}
