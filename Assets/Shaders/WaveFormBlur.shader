Shader "Custom/WaveForm Blur"
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
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
        }
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

            fixed4 box_blur(sampler2D tex, float2 uv)
            {
                fixed4 s1 = tex2D(tex, float2(uv.x - 0.01, uv.y - 0.01));
                fixed4 s2 = tex2D(tex, float2(uv.x - 0.00, uv.y - 0.01));
                fixed4 s3 = tex2D(tex, float2(uv.x + 0.01, uv.y - 0.01));
                fixed4 s4 = tex2D(tex, float2(uv.x - 0.01, uv.y - 0.00));
                fixed4 s5 = tex2D(tex, float2(uv.x - 0.00, uv.y - 0.00));
                fixed4 s6 = tex2D(tex, float2(uv.x + 0.01, uv.y - 0.00));
                fixed4 s7 = tex2D(tex, float2(uv.x - 0.01, uv.y + 0.01));
                fixed4 s8 = tex2D(tex, float2(uv.x - 0.00, uv.y + 0.01));
                fixed4 s9 = tex2D(tex, float2(uv.x + 0.01, uv.y + 0.01));
                fixed4 ret = (s1 * 1 + s2 * 4 + s3 * 1 + s4 * 4 + s5 * 16 + s6 * 4 + s7 * 1 + s8 * 4 + s9 * 1) / 36;
                // ret.a = 1;
                // if (ret.r+ret.g+ret.b > 0)
                // {
                //     ret.rgb = fixed3(1,1,1);
                // }
                return ret;
            }

            float3 hue2rgb(float hue)
            {
                hue = frac(hue); //only use fractional part
                float r = abs(hue * 6.0 - 3.0) - 1.0; //red
                float g = 2.0 - abs(hue * 6.0 - 2.0); //green
                float b = 2.0 - abs(hue * 6.0 - 4.0); //blue
                float3 rgb = float3(r, g, b); //combine components
                rgb = saturate(rgb); //clamp between 0 and 1
                return rgb;
            }

            float3 hsv2rgb(float3 hsv)
            {
                float3 rgb = hue2rgb(hsv.x); //apply hue
                rgb = lerp(1.0, rgb, hsv.y); //apply saturation
                rgb = rgb * hsv.z; //apply value
                return rgb;
            }

            float3 rgb2hsv(float3 rgb)
            {
                float maxComponent = max(rgb.r, max(rgb.g, rgb.b));
                float minComponent = min(rgb.r, min(rgb.g, rgb.b));
                float diff = maxComponent - minComponent;
                float hue = 0.0;
                if (maxComponent == rgb.r)
                {
                    hue = 0.0 + (rgb.g - rgb.b) / diff;
                }
                else if (maxComponent == rgb.g)
                {
                    hue = 2.0 + (rgb.b - rgb.r) / diff;
                }
                else if (maxComponent == rgb.b)
                {
                    hue = 4.0 + (rgb.r - rgb.g) / diff;
                }
                hue = frac(hue / 6.0);
                float saturation = diff / maxComponent;
                float value = maxComponent;
                return float3(hue, saturation, value);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const fixed4 zero4 = fixed4(0, 0, 0, 0);

                float2 uv = i.uv;
                float y = uv.y * 2;
                if (y > 1)
                {
                    y = 2 - y;
                }
                uv.y = y;
                fixed4 col = box_blur(_MainTex, uv);
                fixed r = 1;
                fixed g = 1;
                fixed3 color = fixed3(r, _BeatStrength, g);
                float3 hsv = rgb2hsv(color);
                hsv.x += i.uv.x * 0.5 + _Time.y * _BeatStrength * 0.25;
                float3 back = hsv2rgb(hsv);
                col.rgb = back * frac(1 - i.uv.y);

                col.a *= _BeatStrength * 0.5 + 0.5;

                // fixed3 toClip = clamp(col.rgb - 0.001, fixed3(0,0,0), fixed3(1,1,1));
                // clip(col.r+col.g+col.b - 1);

                // col = col + _ColorBG * floor((4 - dot(col.rgb, zero4.rgb) + 0.001) / 4) * (_BeatStrength * 0.5 + 0.25);

                // col *= fixed4(_ColorBG.rgb, _BeatStrength);

                return col;
            }
            ENDCG
        }
    }
}