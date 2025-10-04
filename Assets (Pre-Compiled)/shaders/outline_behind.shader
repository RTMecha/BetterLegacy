Shader"Unlit/OutlineBehindTransparentShader"
{
    Properties
    {
		_OutlineColor ("Outline Color", Color) = (1,0,0,0.5)
		_OutlineWidth ("Outline Width", Range(0.0, 2.0)) = 0
    }
    SubShader
    {
		Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
		Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
		ZWrite Off

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
				float4 pos : SV_POSITION;
			};
            
            float4 _OutlineColor;
            float _OutlineWidth;

			v2f vert(appdata v)
            {
				appdata original = v;

				float3 scaleDir = normalize(v.vertex.xyz - float4(0,0,0,1));
                v.vertex.xyz += scaleDir * _OutlineWidth;

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			half4 frag(v2f i) : COLOR
            {
				return _OutlineColor;
			}

			ENDCG
		}
    }
}