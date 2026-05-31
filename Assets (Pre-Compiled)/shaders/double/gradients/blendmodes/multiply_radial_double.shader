Shader"Unlit/NoCullRadialTransparentShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", COLOR) = (1,1,1,1)
        _ColorSecondary ("Secondary Color", COLOR) = (1,1,1,1)
        _Scale ("Scale", Float) = 1

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Pass", Float) = 0
        _StencilFail("Stencil Fail", Float) = 0
        _StencilZFail("Stencil Z Fail", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
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
        Cull Off

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            Fail[_StencilFail]
            ZFail[_StencilZFail]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
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
            float4 _ColorSecondary;
            float _Scale;
            float4 _MainTex_ST;
            
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
                return tex2D(_MainTex, TRANSFORM_TEX(i.objectPos.xy, _MainTex)) * _Color;
            }
            ENDCG
        }
    }
}
