// thank you Benjamin Swee https://youtu.be/lQ7GNoT_LrE?si=-x_Q6wjJR9DHOi2Y

Shader "Unlit/shockwave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _Center;
            float _Intensity;
            float _RingAmount;
            float2 _Scale;
            float _Rotation;
            float _Warp;
            float _Elapsed;

            // todo:
            // time

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float2 Rotate(float2 pos, float rotate)
            {
                if (rotate == 0)
				{
                    return pos;
                }
                return float2(pos.x * cos(rotate) - pos.y * sin(rotate), pos.x * sin(rotate) + pos.y * cos(rotate));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //float2 center = (i.uv * 2 - 1) - _Center;

                //float timer = frac(_Time.y);
                //float len = length(center);
                //float upperRing = smoothstep(len + _UpperFeather, len - _LowerFeather, timer);
                //float inverseRing = 1 - upperRing;
                //float finalRing = upperRing * inverseRing * _Intensity * (1 - timer); // (1 - timer) = fade
                //float2 finalUV = i.uv - center * finalRing;
                //fixed4 col = tex2D(_MainTex, finalUV.xy);
                //return fixed4(col.rgb,1);
                
                if (_Scale.x == 0 || _Scale.y == 0)
                {
                    fixed4 safety = tex2D(_MainTex, i.uv);
                    return fixed4(safety.rgb,1);
                }

                // get the center
                float2 center = Rotate(((i.uv * 2 - 1) - _Center), _Rotation) / _Scale;
                center = Rotate(center, _Warp);

                //float timer = _Time.y * 5;
                float timer = _Elapsed;
                float finalRingX = sin(length(center) * _RingAmount - timer) * 0.5 + 0.5;
                finalRingX *= 1 - length(center);
                float finalRingY = cos(length(center) * _RingAmount - timer) * 0.5 + 0.5;
                finalRingY *= 1 - length(center);

                float2 finalUV = i.uv - center * fixed2(finalRingX, finalRingY) * _Intensity;
                fixed4 col = tex2D(_MainTex, finalUV);
                return fixed4(col.rgb,1);
            }
            ENDCG
        }
    }
}
