// Stun look (StatusVisual, over the hull): dims the ship to a dull grey, like a ship whose systems went dead. The
// shocks come from ShockSparks on the same prefab. Multiplies what's behind it inside the ship's sprite; the sprite
// alpha (StatusVisual's fade) blends the dimming in and out.
Shader "Custom/StatusStun"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Dim ("Dimmed Color", Color) = (0.55, 0.55, 0.58, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "DisableBatching"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend DstColor Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            fixed4 _Dim;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float amount = tex2D(_MainTex, IN.uv).a * IN.color.a;
                return fixed4(lerp(float3(1, 1, 1), _Dim.rgb, amount), 1);
            }
            ENDCG
        }
    }
}
