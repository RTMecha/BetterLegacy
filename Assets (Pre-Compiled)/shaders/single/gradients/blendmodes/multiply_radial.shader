Shader"Unlit/NoCullRadialTransparentShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", COLOR) = (1,1,1,1)
        _ColorSecondary ("Secondary Color", COLOR) = (1,1,1,1)
        _Scale ("Scale", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
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
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 objectPos : TEXCOORD0; 
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            float4 _ColorSecondary;
            float _Scale;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objectPos = v.vertex.xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //float4 col = tex2D(_MainTex, i.uv);
                float dist = distance(i.objectPos.xy, float2(0,0)) * 2 * _Scale;
                dist = smoothstep(0, 1, dist);
               
                _Color = lerp(_Color, _ColorSecondary, dist);
                return tex2D(_MainTex, i.objectPos) * _Color;
            }

            ENDCG
        }
    }
}
