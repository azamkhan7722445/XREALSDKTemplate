Shader "Custom/YUV2RGB"
{
    Properties
    {
        _MainTex ("Y Texture",  2D) = "white" {}
        _UTex    ("U Texture",  2D) = "gray"  {}
        _VTex    ("V Texture",  2D) = "gray"  {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            // YUV_420_888: 3 separate planes (matches XREAL SDK GetYUVFormatTextures())
            // Y passed as Blit source → _MainTex, U and V set via SetTexture
            sampler2D _MainTex, _UTex, _VTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // TextureFormat.Alpha8 — data is in .a channel, not .r
                // Coefficients match XREAL SDK's own YUVTransRGB.shader
                float y = tex2D(_MainTex, i.uv).a;
                float u = tex2D(_UTex,    i.uv).a;
                float v = tex2D(_VTex,    i.uv).a;

                float r = y + 1.4022 * v - 0.7011;
                float g = y - 0.3456 * u - 0.7145 * v + 0.53005;
                float b = y + 1.771  * u - 0.8855;

                return fixed4(r, g, b, 1.0);
            }
            ENDCG
        }
    }
}
