// Vacuum Cannon's shell and blast on a 1-unit circle sprite: a fake pull toward the centre. Violet rings slide inward
// and shrink, streaks spiral in, and a black core with a bright lip sits in the middle. Alpha-blended, so it darkens
// what's behind it; the sprite colour tints, and its alpha (Explosion's fade) fades it.
Shader "Custom/VacuumPull"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _CoreColor ("Core Color", Color) = (0, 0, 0, 1)
        [HDR] _RingColor ("Ring Color", Color) = (0.75, 0.4, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.9
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _Rings ("Rings Across The Radius", Float) = 4
        _RingWidth ("Ring Width (share of the gap)", Range(0, 0.5)) = 0.12
        _PullSpeed ("Rings Pulled In Per Second", Float) = 1.2
        _Streaks ("Spiral Streaks", Float) = 6
        _StreakWidth ("Streak Width (share of the gap)", Range(0, 0.5)) = 0.12
        _Twist ("Spiral Twist", Float) = 2.5
        _SpinSpeed ("Spin Speed", Float) = 0.8
        _StreakStrength ("Streak Strength", Range(0, 1)) = 0.6
        _CoreSize ("Core Size (share of radius)", Range(0, 1)) = 0.3
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
            fixed4 _RingColor;
            float _Opacity;
            float _PixelsPerUnit;
            float _Rings;
            float _RingWidth;
            float _PullSpeed;
            float _Streaks;
            float _StreakWidth;
            float _Twist;
            float _SpinSpeed;
            float _StreakStrength;
            float _CoreSize;

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

            // 1 on a thin band where the repeating phase wraps, fading over the given width.
            float band(float phase, float width)
            {
                float fromBand = min(phase, 1 - phase);
                return 1 - smoothstep(0, max(width, 1e-3), fromBand);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = IN.fromCentre;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                float2 q = p / max(IN.outerRadius, 1e-3); // -1 to 1 across the sprite
                float r = length(q);
                float t = _Time.y;
                float outsideCore = smoothstep(_CoreSize * 0.6, _CoreSize, r);
                float insideEdge = 1 - smoothstep(0.75, 1, r);

                // Rings sliding inward: the phase grows with time, so a ring's radius shrinks.
                float rings = band(frac(r * _Rings + t * _PullSpeed), _RingWidth) * insideEdge * outsideCore;

                // Streaks spiralling in toward the core.
                float around = atan2(q.y, q.x) / UNITY_TWO_PI;
                float streaks = band(frac(around * _Streaks + r * _Twist + t * _SpinSpeed), _StreakWidth)
                              * (1 - r) * outsideCore * _StreakStrength;

                // A black core with a bright violet lip.
                float core = 1 - smoothstep(_CoreSize * 0.85, _CoreSize, r);
                float lip = 1 - smoothstep(0, 0.06, abs(r - _CoreSize));

                float light = saturate(rings + streaks + lip);
                float3 rgb = lerp(_RingColor.rgb * light, _CoreColor.rgb, core * (1 - lip));
                float alpha = saturate(light + core) * _Opacity * (1 - smoothstep(0.92, 1, r));
                return fixed4(rgb * IN.color.rgb, alpha * IN.color.a);
            }
            ENDCG
        }
    }
}
