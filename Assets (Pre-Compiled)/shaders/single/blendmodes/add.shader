Shader"Unlit/AddBlendModeTransparentShader"
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
        //Blend SrcColor OneMinusSrcColor
        //Blend SrcColor SrcAlpha
        //Blend SrcColor SrcAlpha, SrcAlpha OneMinusSrcAlpha
        //Blend One One, DstAlpha OneMinusDstAlpha // transition
        //Blend One One, One One
        //Blend One One, SrcAlpha OneMinusSrcAlpha
        Blend One One, Zero Zero
        //Blend SrcColor SrcColor, SrcAlpha SrcAlpha
        //Blend SrcColor SrcAlpha, Zero One
        //Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
        // regular blend
        //Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex    : POSITION;
                float4 color     : COLOR;
            };

            struct v2f
            {
                float2 objectPos : TEXCOORD0; 
                float4 vertex    : SV_POSITION;
                float4 color    : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objectPos = TRANSFORM_TEX(v.vertex.xy, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.objectPos) * _Color;
            }
            ENDCG
        }
    }
}
