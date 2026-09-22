// Additive, unlit, double sided shader for the guards' field of view mesh.
// Vertex alpha fades the far edge of the cone; _Color is driven per guard from a MaterialPropertyBlock.
Shader "Stealth/VisionCone"
{
    Properties
    {
        _Color ("Colour", Color) = (0.45, 0.85, 1.0, 0.25)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Blend One One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_Color.rgb * _Color.a * i.color.a, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
