Shader "Singularity/IntermediateLensingCopy"
{
	Properties
	{
		CubeMap ("Cubemap", CUBE) = "" {}
		AccretionDisk ("AccretionDisk", 2D) = "white" {} 
	}
	SubShader
	{
		Tags {"QUEUE"="Geometry+1" "IgnoreProjector"="True" "RenderType"="Transparent"}

		ZWrite On
		ZTest On
		cull Front

		Blend SrcAlpha OneMinusSrcAlpha

		Pass //Pass 0 - copy pass with resizing of the mesh, for copying to internal buffer not final output buffer
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
				v.vertex.xyz*=1.02; //make this mesh slightly bigger so that when we have AA and copy from our buffer without AA without we don't get artifacts
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
