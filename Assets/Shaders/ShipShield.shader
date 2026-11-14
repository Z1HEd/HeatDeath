// Shield overlay: drawn on a slightly enlarged copy of the ship's own sprite. The sprite's
// alpha is the silhouette: its edge gets a bright rim, its inside a faint fill with soft rings
// rippling out from the hit point (or a hex grid, for comparison: raise _HexStrength). Additive by default, so
// the hull stays visible; purple shields switch _DstBlend to alpha blending, so their dark colour covers the hull.
// SpriteRenderer alpha fades the whole thing.
Shader "Custom/ShipShield"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _RimColor ("Rim Color (alpha = intensity)", Color) = (0.55, 0.85, 1, 1)
        _FillColor ("Fill Color (alpha = intensity)", Color) = (0.2, 0.55, 1, 0.25)
        _RimWidth ("Rim Width (texels)", Float) = 2
        _BandStrength ("Band Strength", Float) = 1.5
        _BandScale ("Band Scale", Float) = 6
        _BandSpeed ("Band Speed", Float) = 2
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _HexSize ("Hex Cell Width (texels)", Float) = 10
        _HexLineWidth ("Hex Line Width (texels)", Float) = 0.5
        _HexStrength ("Hex Strength (0 = off)", Float) = 0
        _RippleStrength ("Ripple Strength (0 = off)", Float) = 0.55
        _RippleCount ("Ripple Wave Count", Float) = 2
        _RippleFalloff ("Brightness Of Each Next Wave", Range(0, 1)) = 0.3333
        _RippleSize ("Ripple Ring Spacing (texels)", Float) = 18
        _RippleSpeed ("Ripple Speed (rings per second)", Float) = 6
        _RippleHeadStart ("Ripple Head Start (seconds)", Float) = 0.17
        [HideInInspector] _RippleOrigin ("Ripple Origin (texels)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RippleAge ("Ripple Age (seconds)", Float) = 0
        // 1 = One (additive), 10 = OneMinusSrcAlpha (alpha blending).
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
            float4 _MainTex_TexelSize;
            fixed4 _RimColor;
            fixed4 _FillColor;
            float _RimWidth;
            float _BandStrength;
            float _BandScale;
            float _BandSpeed;
            float _PixelsPerUnit;
            float _HexSize;
            float _HexLineWidth;
            float _HexStrength;
            float _RippleStrength;
            float _RippleCount;
            float _RippleFalloff;
            float _RippleSize;
            float _RippleSpeed;
            float _RippleHeadStart;
            float4 _RippleOrigin;
            float _RippleAge;

            static const float2 Neighbours[8] =
            {
                float2(-1, -1), float2(0, -1), float2(1, -1),
                float2(-1,  0),                float2(1,  0),
                float2(-1,  1), float2(0,  1), float2(1,  1)
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            // 1 on the lines of a hexagonal grid, 0 inside the cells. p is in texels.
            float hexLines(float2 p)
            {
                const float2 s = float2(1, 1.7320508);
                p /= _HexSize;
                float2 a = p - s * floor(p / s) - s * 0.5;
                float2 q = p + s * 0.5;
                float2 b = q - s * floor(q / s) - s * 0.5;
                float2 g = abs(dot(a, a) < dot(b, b) ? a : b);
                float fromCentre = max(dot(g, float2(0.5, 0.8660254)), g.x);
                return step((0.5 - fromCentre) * _HexSize, _HexLineWidth);
            }

            // Soft rings spreading from the hit point, set per hit by ShipCoreModule. The leading
            // wave is the brightest and each one behind it is _RippleFalloff times as bright.
            // Each wave fades to zero at its own edges, so the steps between them don't show.
            // p is in texels.
            float rippleRings(float2 p)
            {
                float travelled = (_RippleAge + _RippleHeadStart) * _RippleSpeed;
                float behindFront = travelled - length(p - _RippleOrigin.xy) / _RippleSize;
                if (behindFront < 0 || behindFront >= _RippleCount)
                    return 0;

                float wave = 0.5 - 0.5 * cos(UNITY_TWO_PI * behindFront);
                return wave * wave * pow(_RippleFalloff, floor(behindFront));
            }

            // Everything outside the texture counts as empty. Clamped sampling would repeat art
            // that touches the texture's edge, which hid the rim there.
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

            fixed4 frag(v2f IN) : SV_Target
            {
                float alpha = tex2D(_MainTex, IN.texcoord).a;

                // Whole texels at full strength, any fraction as a dimmer inner ring: point
                // sampling can't place a half-texel offset evenly on both sides.
                float wholeWidth = floor(_RimWidth);
                float rim = max(edgeAt(IN.texcoord, alpha, wholeWidth),
                                edgeAt(IN.texcoord, alpha, wholeWidth + 1) * (_RimWidth - wholeWidth));

                float2 p = IN.worldPos;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                float wave = sin(dot(p, float2(0.6, 0.8)) * _BandScale + _Time.y * _BandSpeed);
                float band = smoothstep(0.7, 1, wave);

                // In sprite texels, so the patterns stick to the ship and line up with its pixels.
                float2 texel = floor(IN.texcoord * _MainTex_TexelSize.zw) + 0.5;
                float hex = _HexStrength > 0 ? hexLines(texel) : 0;
                float ripple = _RippleStrength > 0 ? rippleRings(texel) : 0;

                // The diagonal band only lights the hex lines, so it doesn't fight the rings.
                float fill = _FillColor.a + hex * _HexStrength * (1 + band * _BandStrength);
                float rings = saturate(ripple * _RippleStrength);

                fixed3 rgb = lerp(_FillColor.rgb, _RimColor.rgb, max(max(rim, hex), rings));
                float intensity = saturate(max(rim * _RimColor.a, fill));

                // Rings hold full brightness until the flash is half faded, so they stay readable.
                float fade = IN.color.a;
                float shown = saturate(intensity * fade + rings * saturate(fade * 2));

                return fixed4(rgb * IN.color.rgb, shown * alpha);
            }
            ENDCG
        }
    }
}
