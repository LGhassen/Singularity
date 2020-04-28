Shader "Singularity/SkyBox"
{
    Properties
    {
       CubeMap ("Cubemap", CUBE) = "" {}
    }
    SubShader
    {
			Tags {"QUEUE"="Geometry+1" "IgnoreProjector"="True" }

    	 	ZWrite Off
    	 	cull Front
    	 
    		//Blend DstColor Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "UnityCG.cginc"
          
			uniform samplerCUBE CubeMap;


            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex  : SV_POSITION;
                float4 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
      
            float4 frag (v2f i) : SV_Target
            {
            	i.worldPos.xyz/=i.worldPos.w;
            	float3 viewDir = normalize(i.worldPos.xyz-_WorldSpaceCameraPos);
            	return texCUBE(CubeMap, viewDir);
            }
            ENDCG
        }
    }
}
