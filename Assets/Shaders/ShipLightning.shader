// Lightning crawling over a ship's hull, drawn on a copy of the ship's sprite and masked by its
// alpha. Strands are chords between seeded points on a circle around the ship (_Center, _Radius,
// _Seed set by ShipLightningOverlay), computed in world units like Custom/Lightning.
Shader "Custom/ShipLightning"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _CoreColor ("Core Color", Color) = (0.85, 0.95, 1, 1)
        _GlowColor ("Glow Color (alpha = intensity)", Color) = (0.3, 0.6, 1, 0.8)
        _CoreWidth ("Core Width (world units)", Float) = 0.03
        _GlowWidth ("Glow Width (world units)", Float) = 0.12
        _Amplitude ("Amplitude (world units)", Float) = 0.12
        _Frequency ("Kinks per world unit", Float) = 6
        _Taper ("End Taper (world units)", Float) = 0.1
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _HullGlow ("Whole Hull Glow", Float) = 0.15
        _StrandCount ("Strand Count (1 to 3)", Range(1, 3)) = 3
        _ChordSpread ("Strand Direction Spread (radians)", Float) = 1.2
        _EndpointScatter ("Endpoint Scatter (0 = on circle, 1 = anywhere inside)", Range(0, 1)) = 0
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _CoreColor;
            fixed4 _GlowColor;
            float _CoreWidth;
            float _GlowWidth;
            float _HullGlow;
            float _StrandCount;
            float _ChordSpread;
            float _EndpointScatter;

            // Per overlay, set by ShipLightningOverlay.
            float4 _Center;
            float _Radius;
            float _Seed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            float2 strandEnd(float angle, float seed)
            {
                float reach = _Radius * lerp(1, hash11(seed), _EndpointScatter);
                return _Center.xy + float2(cos(angle), sin(angle)) * reach;
            }

            // x = core coverage, y = glow falloff, for one chord across the hull.
            float2 hullStrand(float2 p, float seed)
            {
                float startAngle = hash11(seed) * UNITY_TWO_PI;
                float endAngle = startAngle + UNITY_PI + (hash11(seed + 7.7) - 0.5) * _ChordSpread;
                float2 start = strandEnd(startAngle, seed + 3.1);
                float2 hop = strandEnd(endAngle, seed + 9.4) - start;

                float len = max(length(hop), 1e-4);
                float2 dir = hop / len;
                float2 rel = p - start;
                float t = dot(rel, dir);
                float s = dot(rel, float2(-dir.y, dir.x));
                return strand(strandDistance(t, s, len, seed), _CoreWidth, _GlowWidth);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float mask = tex2D(_MainTex, IN.texcoord).a;

                float2 p = IN.worldPos;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                float2 lit = 0;
                for (int i = 0; i < 3; i++)
                    lit = max(lit, hullStrand(p, _Seed + i * 41.3) * step(i + 1, _StrandCount));

                float3 rgb = lerp(lit.y * _GlowColor.rgb * _GlowColor.a, _CoreColor.rgb, lit.x)
                           + _GlowColor.rgb * _HullGlow;

                // Additive: vertex alpha fades.
                return fixed4(rgb * IN.color.rgb * IN.color.a * mask, 1);
            }
            ENDCG
        }
    }
}
