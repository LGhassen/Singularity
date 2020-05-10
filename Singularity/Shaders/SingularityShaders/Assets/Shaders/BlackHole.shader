Shader "Singularity/BlackHole"
{
	Properties
	{
		CubeMap ("Cubemap", CUBE) = "" {}
			AccretionDisk ("AccretionDisk", 2D) = "white" {} 
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

			#define ID_BLACKHOLE 0
			#define ID_BLACKHOLE_DISK 1

			uniform float4 blackhole;
			uniform float gravity;

			uniform samplerCUBE CubeMap;
			uniform sampler2D AccretionDisk;

			uniform float4 blackholeDisk;	//xyz normal * innerRadius, w outerRadius

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex	: SV_POSITION;
				float4 worldPos : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			float3 accretionDiskColor(float3 pos)
			{

				float TWOPI = 6.28318530718;
				pos = pos - blackhole.xyz;
				float dist = length(pos);

				float r1 = length(blackholeDisk.xyz);
				float r2 = blackholeDisk.w;

				// Important! Scale radii according to black hole
				float v = clamp((dist - r1) / (r2 - r1), 0.0, 1.0);

				float3 base = cross(blackholeDisk.xyz, float3(0.0, 0.0, 1.0));
				float angle = acos(dot(normalize(base), normalize(pos)));
				if (dot(cross(base, pos), blackholeDisk.xyz) < 0.0) angle = -angle;

				float u = 0.5 - angle / TWOPI;

				float3 color = tex2Dlod(AccretionDisk, float4(u, v,0.0,0.0)).rgb;

				return color;
			}

			float sphereDistance(float3 rayPosition, float3 rayDirection, float4 sphere)
			{
				float3 v;
				float p, d;
				v = rayPosition - sphere.xyz;
				p = dot(rayDirection, v);
				d = p * p + sphere.w * sphere.w - dot(v, v);

				return d < 0.0 ? -1.0 : -p - sqrt(d);
			}

			void testDistance(int i, float distance, inout float currentDistance, inout int currentObject)
			{
				float EPSILON = 0.0001;

				if (distance >= EPSILON && distance < currentDistance)
				{
					currentDistance = distance;
					currentObject = i;
				}
			}

			float ringDistance(float3 rayPosition, float3 rayDirection, float3 center, float4 definition)
			{

				float EPSILON = 0.0001;
				float r1 = length(definition.xyz);
				float r2 = definition.w;
				float3 normal = definition.xyz / r1;

				float denominator = dot(rayDirection, normal);
				float constant = -dot(center, normal);
				float distanceToCenter;
				if (abs(denominator) < EPSILON)
				{
					return -1.0;
				}
				else
				{
					float t = -(dot(rayPosition, normal) + constant) / denominator;
					if (t < 0.0) return -1.0;

					float3 intersection = rayPosition + t * rayDirection;
					distanceToCenter = length(intersection - center);
					if (distanceToCenter >= r1 && distanceToCenter <= r2)
					{
						return t;
					}
					return -1.0;
				}
			}

			float3 raytrace(float3 rayPosition, float3 rayDirection)
			{
				float lightSpeed = 0.2; // Not actually representing light speed :P

				float INFINITY = 1000000.0;
				float EPSILON = 0.0001;

				//float gravityBlackhole = blackhole.w	* lightSpeed * lightSpeed;
				float gravityBlackhole = gravity;

				float currentDistance = INFINITY;
				int	 currentObject = -1, prevObject = -1;
				float3	currentPosition;
				float3	normal;

				float stepSize, rayDistance;
				float3 gravityVector, rayAccel;
				float objectDistance;//blackholeDisk

				float4 color = float4(0.0, 0.0, 0.0, 1.0);

				for (int i = 0; i < 50; i++)
				{
					currentDistance = INFINITY;

					// Bend the light towards the black hole
					gravityVector = blackhole.xyz - rayPosition;
					rayDistance = length(gravityVector);

					// 0.05: rate of smaller steps when approaching blackhole
					//stepSize = rayDistance - (blackhole.w * 0.05); //stepSize = rayDistance - (blackhole.w * 0.85);
					stepSize = rayDistance - (blackholeDisk.w * 0.05); //stepSize = rayDistance - (blackhole.w * 0.85);
					//stepSize = clamp(min(stepSize,1.0*(rayDistance - (blackhole.w * 0.85))),0.0,1.0); //got some issues on the accetionDisk because of weird/uneven stepping

					rayAccel = normalize(gravityVector) * gravityBlackhole / (rayDistance * rayDistance);

					//rayDirection=normalize(rayDirection * lightSpeed + rayAccel * stepSize);
					rayDirection=normalize(rayDirection * lightSpeed * stepSize + rayAccel * stepSize);

					objectDistance = sphereDistance(rayPosition, rayDirection, float4(blackhole.xyz, blackhole.w));
					testDistance(ID_BLACKHOLE, objectDistance, currentDistance, currentObject);

					//					objectDistance = ringDistance(rayPosition, rayDirection, blackhole.xyz, blackholeDisk);
					//					testDistance(ID_BLACKHOLE_DISK, objectDistance, currentDistance, currentObject);

					rayDistance = lightSpeed * stepSize;
					//rayDistance = stepSize; //why doesn't this work?
					rayPosition = rayPosition + rayDistance * rayDirection;

					// Check if we hit any object, and if so, stop integrating
					if (currentObject != -1 && currentDistance <= rayDistance)
					{
						//						 //But if it's something transparent, get its color, and continue
						//						if (currentObject == ID_BLACKHOLE_DISK)
						//						{
						//						currentPosition = rayPosition + rayDirection * currentDistance;
						//						color.rgb += accretionDiskColor(currentPosition).rgb * color.a;
						//						currentObject = -1;
						//						prevObject = ID_BLACKHOLE_DISK;
						//						}
						//						else
						{
							break;
						}
					}
				}

				if (currentObject != ID_BLACKHOLE)
				{

					float3 cubeMapColor = texCUBE(CubeMap, rayDirection);
					color.rgb += (1.0 - color.rgb) * cubeMapColor;
					//consider enabling/disabling mipMaps and texcube Bias (possibly negative, to strike balance between jittering and clarity
					//maybe even a crisper image when the light is blueshifted, to simulate distant stars becoming clearer, but check if KSP does mipMaps on background skybox first
				}

				return color;
			}

			float4 frag (v2f i) : SV_Target
			{
				i.worldPos.xyz/=i.worldPos.w;
				float3 viewDir = normalize(i.worldPos.xyz-_WorldSpaceCameraPos);

				float3 color = raytrace(_WorldSpaceCameraPos, viewDir);

				return float4(color, 1.0);
			}
			ENDCG
		}
	}
}
