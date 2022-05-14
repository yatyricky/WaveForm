Shader "Custom/WaveForm"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BeatStrength ("BeatStrength", float) = 0
        _ColorBG ("ColorBG", Color) = (0,0,0,0)
        _Color1 ("Color1", Color) = (1,1,1,1)
        _Color2 ("Color2", Color) = (1,1,1,1)
        _Color3 ("Color3", Color) = (1,1,1,1)
        _Color4 ("Color4", Color) = (1,1,1,1)
        _Color5 ("Color5", Color) = (1,1,1,1)
    }
    SubShader
    {
        // No culling or depth
//        Cull Off ZWrite Off ZTest Always
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _BeatStrength;
            sampler2D _MainTex;
            fixed4 _ColorBG;
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;
            fixed4 _Color4;
            fixed4 _Color5;

            fixed4 frag(v2f i) : SV_Target
            {
                const fixed4 zero4 = fixed4(0, 0, 0, 0);
                const fixed4 one4 = fixed4(1, 1, 1, 1);
                const float2 size = float2(0.0, 0.02);
                const float2 step = float2(0.00, 0.025);
                float2 offset = float2(0, 0);
                
                fixed4 col = tex2D(_MainTex, i.uv);
                const fixed4 col1 = tex2D(_MainTex, i.uv + size);
                col = clamp((col1 - col) * _Color1, zero4, one4);

                // iteration 1
                offset += step;
                fixed4 col_shade_1 = tex2D(_MainTex, i.uv + offset);
                const fixed4 col1_shade_1 = tex2D(_MainTex, i.uv + size + offset);
                col_shade_1 = clamp((col1_shade_1 - col_shade_1) * _Color2, zero4, one4);
                col += col_shade_1;

                // iteration 2
                offset += step;
                fixed4 col_shade_2 = tex2D(_MainTex, i.uv + offset);
                const fixed4 col1_shade_2 = tex2D(_MainTex, i.uv + size + offset);
                col_shade_2 = clamp((col1_shade_2 - col_shade_2) * _Color3, zero4, one4);
                col += col_shade_2;

                // iteration 3
                offset += step;
                fixed4 col_shade_3 = tex2D(_MainTex, i.uv + offset);
                const fixed4 col1_shade_3 = tex2D(_MainTex, i.uv + size + offset);
                col_shade_3 = clamp((col1_shade_3 - col_shade_3) * _Color4, zero4, one4);
                col += col_shade_3;
                
                // iteration 4
                offset += step;
                fixed4 col_shade_4 = tex2D(_MainTex, i.uv + offset);
                const fixed4 col1_shade_4 = tex2D(_MainTex, i.uv + size + offset);
                col_shade_4 = clamp((col1_shade_4 - col_shade_4) * _Color5, zero4, one4);
                col += col_shade_4;

                // fixed3 toClip = clamp(col.rgb - 0.001, fixed3(0,0,0), fixed3(1,1,1));
                // clip(col.r+col.g+col.b - 1);

                col = col + _ColorBG * floor((4 - dot(col.rgb, zero4.rgb) + 0.001) / 4) * (_BeatStrength * 0.5 + 0.25);

                // col *= fixed4(_ColorBG.rgb, _BeatStrength);

                return col;
            }
            ENDCG
        }
    }
}