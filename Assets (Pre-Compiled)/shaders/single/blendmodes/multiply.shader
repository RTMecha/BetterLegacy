Shader"Unlit/MultiplyBlendModeTransparentShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", COLOR) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        // todo: support opacity... somehow
        //Blend SrcAlpha OneMinusSrcAlpha
        //Blend SrcColor SrcAlpha
        //BlendOp RevSub
        //Blend One Zero
        
        //Blend Off
        
        //Blend DstColor Zero, SrcAlpha OneMinusSrcAlpha
        Blend DstColor Zero
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
                float4 vertex    : POSITION;
                fixed4 color     : COLOR;
            };

            struct v2f
            {
                float3 objectPos : TEXCOORD0; 
                float4 vertex    : SV_POSITION;
                fixed4 color     : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objectPos = float3(TRANSFORM_TEX(v.vertex.xy, _MainTex), v.vertex.z);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            

            fixed4 frag (v2f i) : SV_Target
            {
                //fixed4 a = lerp(_Color, tex2D(_MainTex, i.objectPos) * _Color, _Color.a);
                //fixed4 b = tex2D(_MainTex, i.vertex) * i.color;

                //return lerp(a, a * b, i.color.a * _BlendAmount);
                
                return tex2D(_MainTex, i.objectPos) * _Color;
            }

            // a = (R = 0.5, G = 0.5, B = 0.5)
            // b = (R = 0.5, G = 0.5, B = 0.5)
            // a * b = 0.25
            
            // a = (R = 0.7, G = 0.7, B = 0.7)
            // b = (R = 0.5, G = 0.5, B = 0.5)
            // a * b = 0.35

            // Multiply:
            // R = 0.9
            // A = 0.1
            
            // Additive:
            // R = 0.1
            // A = 0.9
            
            ENDCG
        }
    }
}
