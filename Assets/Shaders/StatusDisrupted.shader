// Disrupted look (StatusVisual, over the hull, enlarged like the ship's shield): the ship's own shield, shattered. Drawn
// like Custom/ShipShield, a bright rim along the ship's silhouette and a faint fill inside, but broken into shards
// with gaps between them. A few shards at a time flicker into view, each on its own slow clock, and break up into
// static before they go.
// Premultiplied, like Custom/ShipShield: rim and fill add light, and the fill also covers the hull by _Cover, since
// added red light alone barely shows on a dark hull (purple shields had the same problem). The sprite alpha
// (StatusVisual's fade) fades it.
Shader "Custom/StatusDisrupted"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _RimColor ("Rim Color (alpha = intensity)", Color) = (0.55, 0.85, 1, 1)
        _FillColor ("Fill Color (alpha = intensity)", Color) = (0.2, 0.55, 1, 0.3)
        _Cover ("Fill Covers Hull", Range(0, 1)) = 1
        _RimWidth ("Rim Width (texels)", Float) = 2
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _ShardScale ("Shards Per World Unit", Float) = 4
        _Gap ("Gap Between Shards (share of a shard)", Range(0, 0.5)) = 0.08
        _OnShare ("Share Of Shards Showing", Range(0, 1)) = 0.25
        _FlickerRate ("Shard Changes Per Second", Float) = 2.5
        _Static ("Static Breakup", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "DisableBatching"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

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
            float4 _MainTex_TexelSize;
            fixed4 _RimColor;
            fixed4 _FillColor;
            float _Cover;
            float _RimWidth;
            float _PixelsPerUnit;
            float _ShardScale;
            float _Gap;
            float _OnShare;
            float _FlickerRate;
            float _Static;

            static const float2 Neighbours[8] =
            {
                float2(-1, -1), float2(0, -1), float2(1, -1),
                float2(-1,  0),                float2(1,  0),
                float2(-1,  1), float2(0,  1), float2(1,  1)
            };

            // As in Custom/ShipShield: everything outside the texture counts as empty. Clamped sampling would
            // repeat art that touches the texture's edge, which hid the rim there.
            float spriteAlpha(float2 uv)
            {
                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                return tex2D(_MainTex, uv).a * inside;
            }

            // 1 on silhouette pixels with an empty neighbour exactly width texels away.
            float edgeAt(float2 uv, float alpha, float width)
            {
                float2 offset = _MainTex_TexelSize.xy * width;
                float eroded = alpha;
                for (int i = 0; i < 8; i++)
                    eroded = min(eroded, spriteAlpha(uv + Neighbours[i] * offset));
                return saturate(alpha - eroded);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                // In the ship's own space at world scale, so the shards stay put on the shield as it turns.
                float scale = length(unity_ObjectToWorld._m00_m10_m20);
                OUT.p = IN.vertex.xy * scale;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // The silhouette, and its rim: where the alpha drops within a few texels. Whole texels at full
                // strength, any fraction as a dimmer inner ring, like the shield's.
                float alpha = tex2D(_MainTex, IN.uv).a;
                float wholeWidth = floor(_RimWidth);
                float rim = max(edgeAt(IN.uv, alpha, wholeWidth),
                                edgeAt(IN.uv, alpha, wholeWidth + 1) * (_RimWidth - wholeWidth));

                float2 p = IN.p;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                // Shards: cells of an irregular pattern, with gaps along their edges.
                float2 cell = floor(p * _ShardScale);
                float2 inCell = frac(p * _ShardScale);
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
                float inShard = step(_Gap, second - nearest);

                // Each shard shows on its own clock, and breaks into static as it's about to go.
                float clock = _Time.y * _FlickerRate + hash21(nearestId + 3.3) * 10;
                float tick = floor(clock);
                float showing = step(hash21(nearestId + tick * 1.7), _OnShare);
                float2 pixel = floor(p * (_PixelsPerUnit > 0 ? _PixelsPerUnit : 50));
                float breakup = step(lerp(1, 0.2, _Static * frac(clock)), hash21(pixel + tick));

                float shown = inShard * showing * (1 - breakup) * IN.color.a;
                float fill = _FillColor.a * alpha * shown;
                float3 rgb = _FillColor.rgb * fill + _RimColor.rgb * _RimColor.a * rim * shown;
                return fixed4(rgb, _Cover * fill);
            }
            ENDCG
        }
    }
}
