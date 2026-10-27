Shader "BlockBlaster/Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Edge ("Edge glow", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed _Edge;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float shade = 0.70 + 0.30 * abs(dot(normalize(normal + 0.0001), normalize(float3(0.4,1,-0.3))));
                o.color = _Color * v.color;
                o.color.rgb *= shade;
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = i.color * tex2D(_MainTex, i.uv);
                clip(c.a - 0.01);
                // A bright rim near each face's border reads as a neon bevel on the falling cubes.
                float rim = max(abs(i.uv.x - 0.5), abs(i.uv.y - 0.5)) * 2;
                c.rgb += _Edge * smoothstep(0.80, 0.94, rim) * 0.6;
                return c;
            }
            ENDCG
        }
    }
}
