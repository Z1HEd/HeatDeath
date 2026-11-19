// Nuke's toxic zone on a 1-unit circle sprite scaled up by HazardZone: a drifting cloud of layered noise that
// slowly drifts and bubbles, darker toward the middle, with a ragged edge that breathes around the hitbox's edge. HazardZone lowers the
// sprite's alpha over the zone's life, and the cloud thins out with it. The sprite colour tints.
Shader "Custom/ToxicCloud"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _DarkColor ("Dark Color", Color) = (0.08, 0.3, 0.06, 1)
        _BrightColor ("Bright Color", Color) = (0.55, 1, 0.3, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.6
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _NoiseScale ("Puffs Per World Unit", Float) = 0.8
        _DriftSpeed ("Drift Speed", Float) = 0.15
        _BubbleSpeed ("Bubble Speed", Float) = 0.6
        _EdgeRagged ("Edge Raggedness (share of radius)", Range(0, 0.5)) = 0.08
        _EdgeSoftness ("Edge Blur (share of radius)", Range(0.01, 1)) = 0.35
        _Dissolve ("Thinning Over Its Life", Range(0, 1)) = 0.7
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
            fixed4 _BrightColor;
            float _Opacity;
            float _PixelsPerUnit;
            float _NoiseScale;
            float _DriftSpeed;
            float _BubbleSpeed;
            float _EdgeRagged;
            float _EdgeSoftness;
            float _Dissolve;

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

                float2 q = p / max(IN.outerRadius, 1e-3); // -1 to 1 across the zone, for the shape
                float r = length(q);
                float t = _Time.y;

                // The puffs are measured in world units, so a bigger cloud shows more of them instead of stretching
                // them. Each cloud gets its own patch of noise from where it sits.
                float2 seed = unity_ObjectToWorld._m03_m13 * 0.37;
                float puffs = fbm2((p + seed) * _NoiseScale + float2(t, -t * 0.7) * _DriftSpeed);
                float bubbles = fbm2((p + seed) * _NoiseScale * 2.3 - float2(0, t * _BubbleSpeed));
                float density = saturate(puffs * 0.8 + bubbles * 0.45 - 0.2);

                // A ragged edge that breathes around the hitbox's edge, blurring across it.
                float edgeNoise = fbm2(normalize(q + 1e-4) * 2 + seed + t * 0.25);
                float edgeRadius = 1 - _EdgeRagged * (edgeNoise - 0.5);
                density *= 1 - smoothstep(edgeRadius - _EdgeSoftness, edgeRadius, r);

                // Thins out as the zone's life runs down.
                float life = IN.color.a;
                density = saturate(density - (1 - life) * _Dissolve) * saturate(life * 4);

                float3 rgb = lerp(_DarkColor.rgb, _BrightColor.rgb, saturate(puffs * 1.2 + bubbles * 0.3))
                           * lerp(0.6, 1, saturate(r));
                return fixed4(rgb * IN.color.rgb, density * _Opacity);
            }
            ENDCG
        }
    }
}
