// Exposed look (StatusVisual, over the hull): red-hot cracks split across the hull plating, pulsing as if the inside
// shows through. The cracks are edges of an irregular cell pattern in world units, kept only on some cells so they
// read as cracks, not a grid; they open wider as the look fades in. Additive, inside the ship's sprite.
Shader "Custom/StatusExposed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _CoreColor ("Crack Core", Color) = (1, 0.75, 0.3, 1)
        _GlowColor ("Crack Glow (alpha = strength)", Color) = (1, 0.15, 0.05, 0.8)
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _Scale ("Plates Per World Unit", Float) = 4
        _CrackShare ("Share Of Plates Cracked", Range(0, 1)) = 0.55
        _CrackWidth ("Crack Width (world units)", Float) = 0.02
        _GlowWidth ("Glow Width (world units)", Float) = 0.07
        _PulseSpeed ("Pulse Speed", Float) = 4
        _FlowScale ("Hot Spots Per World Unit", Float) = 1.5
        _FlowSpeed ("Flow Speed", Float) = 0.8
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
            fixed4 _CoreColor;
            fixed4 _GlowColor;
            float _PixelsPerUnit;
            float _Scale;
            float _CrackShare;
            float _CrackWidth;
            float _GlowWidth;
            float _PulseSpeed;
            float _FlowScale;
            float _FlowSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                // In the ship's own space at world scale, so the cracks stay put on the hull as it turns.
                float scale = length(unity_ObjectToWorld._m00_m10_m20);
                OUT.p = IN.vertex.xy * scale;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float mask = tex2D(_MainTex, IN.uv).a;
                float2 p = IN.p;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                // Distance to the nearest and second-nearest plate centre: their difference is small along cracks.
                float2 cell = floor(p * _Scale);
                float2 inCell = frac(p * _Scale);
                float nearest = 8;
                float second = 8;
                float2 nearestId = cell;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 offset = float2(x, y);
                    float2 centre = offset + float2(hash21(cell + offset), hash21(cell + offset + 19.1));
                    float d = length(centre - inCell);
                    if (d < nearest)
                    {
                        second = nearest;
                        nearest = d;
                        nearestId = cell + offset;
                    }
                    else if (d < second)
                        second = d;
                }
                float fromCrack = (second - nearest) / _Scale; // world units

                float fade = IN.color.a;
                float cracked = step(hash21(nearestId + 7.3), _CrackShare);
                float core = 1 - smoothstep(0, max(_CrackWidth * fade, 1e-4), fromCrack);
                float glow = pow(saturate(1 - fromCrack / max(_GlowWidth * fade, 1e-4)), 2);
                float pulse = 0.8 + 0.2 * sin(_Time.y * _PulseSpeed + hash21(nearestId) * 6.28);

                // Hot spots drifting through the cracks, like molten metal running along them.
                float t = _Time.y * _FlowSpeed;
                float flow = lerp(0.3, 1.8, fbm2(p * _FlowScale + float2(t, -t * 0.6)));

                float3 rgb = (_GlowColor.rgb * _GlowColor.a * glow + _CoreColor.rgb * core) * flow * pulse * cracked;
                return fixed4(rgb * mask * fade, 1);
            }
            ENDCG
        }
    }
}
