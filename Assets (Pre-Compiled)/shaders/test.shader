Shader"Unlit/NormalTransparentTestShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", COLOR) = (1,1,1,1)
        _BlendMode ("Blend Mode", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100
        
        // Grab the screen behind the object into _BackgroundTexture
        GrabPass
        {
            "_BackgroundTexture"
        }

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
            };

            struct v2f
            {
                float3 objectPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            float4 _MainTex_ST;
            float _BlendMode;
            
            sampler2D _BackgroundTexture;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objectPos = ComputeGrabScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 target = _Color;
                float4 blend = tex2D(_BackgroundTexture, i.objectPos);
                // multply
                if (_BlendMode == 1.0) {
                    return target * blend;
                }
                // additive
                if (_BlendMode == 2.0) {

                }
                // color burn
                if (_BlendMode == 3.0) {
                    return 1 - (1 - target) / blend;
                }
                // color dodge
                if (_BlendMode == 4.0) {
                    return target / (1 - blend);
                }
                // reflect
                if (_BlendMode == 5.0) {

                }
                // glow
                if (_BlendMode == 6.0) {

                }
                // overlay
                if (_BlendMode == 7.0) {
                    //return (target > 0.5) * (1 - (1 - 2 * (target - 0.5)) * (1 - blend)) + (target <= 0.5) * ((2 * target) * blend);
                    if (blend < 0.5) {
                        return 2 * target * blend;
                    }
                    else {
                        return 2 * blend * (1 - target) + sqrt(blend) * (2 * target - 1);
                    }
                }
                // difference
                if (_BlendMode == 7.0) {
                    return target - blend;
                }

                return (1 - blend) * target;
            }
            ENDCG
        }
    }
}
