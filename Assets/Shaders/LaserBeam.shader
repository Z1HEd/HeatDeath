// Laser beam on a stretched 1-unit square sprite: a parabolic start, brightest along the centre
// line and fading to nothing at the sides. Measured in world units from the sprite's scale, so
// the long stretch doesn't distort the cap. Colour and fade come from SpriteRenderer.color.
Shader "Custom/LaserBeam"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SideFalloff ("Side Falloff (higher = thinner core)", Float) = 1.5
        _CapLength ("Parabolic Start Length (world units)", Float) = 0.5
        _PixelsPerUnit ("Pixels Per Unit (0 = smooth)", Float) = 50
        _Rainbow ("Rainbow (1 = hue from _HueStart to _HueEnd across the beam)", Float) = 0
        [PerRendererData] _HueStart ("Hue at the left edge", Float) = 0
        [PerRendererData] _HueEnd ("Hue at the right edge", Float) = 0.8
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            // The beam's size comes from unity_ObjectToWorld. Batching merges beams into one world-space mesh
            // with an identity matrix, which made beams fired together (Yamato's copy) invisible.
            "DisableBatching"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                fixed4 color     : COLOR;
                float2 beam      : TEXCOORD0; // x = across, y = along from the start, world units
                float  halfWidth : TEXCOORD1;
            };

            float _SideFalloff;
            float _CapLength;
            float _PixelsPerUnit;
            float _Rainbow;
            float _HueStart;
            float _HueEnd;

            v2f vert(appdata_t IN)
            {
                float width = length(unity_ObjectToWorld._m00_m10_m20);
                float beamLength = length(unity_ObjectToWorld._m01_m11_m21);

                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.beam = float2(IN.vertex.x * width, (IN.vertex.y + 0.5) * beamLength);
                OUT.halfWidth = width * 0.5;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = IN.beam;
                if (_PixelsPerUnit > 0)
                    p = (floor(p * _PixelsPerUnit) + 0.5) / _PixelsPerUnit;

                // Half-width grows as a parabola over the start, then stays at full width.
                float localHalfWidth = IN.halfWidth * sqrt(saturate(p.y / max(_CapLength, 1e-4)));
                float distance = abs(p.x) / max(localHalfWidth, 1e-4);

                float intensity = pow(saturate(1 - distance), _SideFalloff);
                float3 rgb = IN.color.rgb;
                if (_Rainbow > 0.5)
                {
                    // Left edge to right edge of the beam, at full saturation and brightness.
                    float across = saturate(p.x / max(localHalfWidth, 1e-4) * 0.5 + 0.5);
                    float hue = lerp(_HueStart, _HueEnd, across);
                    rgb = saturate(abs(frac(hue + float3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
                }
                return fixed4(rgb, intensity * IN.color.a);
            }
            ENDCG
        }
    }
}
