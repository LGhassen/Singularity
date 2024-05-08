Shader "Singularity/StackedLensingCopy"
{
	Properties
	{
		CubeMap ("Cubemap", CUBE) = "" {}
		AccretionDisk ("AccretionDisk", 2D) = "white" {} 
	}
	SubShader
	{
		Tags {"QUEUE"="Geometry+1" "IgnoreProjector"="True" "RenderType"="Transparent"}

		ZWrite [_ZwriteVariable]
		ZTest On
		cull Front

		Blend SrcAlpha OneMinusSrcAlpha
			
		Pass //Pass 0 - simple copy pass of already rendered spheres
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			uniform sampler2D singularityFinalStackedBuffer;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex  : SV_POSITION;
				float4 screenPos : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float2 uv = i.screenPos.xy / i.screenPos.w;
				float4 color = tex2D(singularityFinalStackedBuffer, uv);
				return float4(color.rgb,1.0);
			}
			ENDCG
		}
	}
}
