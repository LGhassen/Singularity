Shader "Singularity/BlackHoleAccretionDisk"
{
    Properties
    {
        CubeMap ("Cubemap", CUBE) = "" {}
        //AccretionDisk ("AccretionDisk", 2D) = "white" {} 
    }
    SubShader
    {
			Tags {"QUEUE"="Geometry+1" "IgnoreProjector"="True" "RenderType"="Transparent"}

    	 	ZWrite On
    	 	ZTest On
    	 	cull Front
    	 
    		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0

            #pragma multi_compile ACCRETION_DISK_OFF ACCRETION_DISK_ON
            #pragma multi_compile RADIAL_DISK_MAPPING_OFF RADIAL_DISK_MAPPING_ON

            #pragma multi_compile WORMHOLE_OFF WORMHOLE_ON

            #pragma multi_compile GALAXYCUBEMAPONLY_OFF GALAXYCUBEMAPONLY_ON

            #include "UnityCG.cginc"

            #define ID_BLACKHOLE 0
			#define ID_BLACKHOLE_DISK 1

			#define INFINITY 1000000.0
			#define lightSpeed 0.2 // Not actually representing light speed :P
			#define TWOPI 6.28318530718

			uniform float blackHoleRadius;
			uniform float gravity;

			uniform sampler2D _CameraDepthTexture;
			uniform sampler2D screenBuffer;

			uniform samplerCUBE CubeMap;		//galaxy cubemap
			uniform samplerCUBE objectCubeMap;	//scaledSpace objects cubemap (no background)

#if defined (WORMHOLE_ON)
			uniform samplerCUBE wormholeCubemap;
#endif

			uniform float useScreenBuffer; 		//if we should use the screenBuffer, only for the main scaled camera

			uniform float3 galaxyFadeColor;
			float4x4 cubeMapRotation;

			uniform sampler2D AccretionDisk;

			uniform float3 diskNormal;
			uniform float diskInnerRadius;
			uniform float diskOuterRadius;

			uniform float rotationSpeed;
			uniform float universalTime;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex  : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                float4 blackHoleOrigin : TEXCOORD1;

            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.blackHoleOrigin = mul(unity_ObjectToWorld, float4(0,0,0,1));
                return o;
            }


			float3 accretionDiskColor(float3 pos, float3 base1, float3 base2, float3 blackHoleOrigin)
			{

				pos = pos - blackHoleOrigin;

#if defined (RADIAL_DISK_MAPPING_ON)
				//TODO: make this rotate, move TWOPI to defines
				//need to rotate base1 and base2?
				float dist = length(pos);

				float v = clamp((dist - diskInnerRadius) / (diskOuterRadius - diskInnerRadius), 0.0, 1.0);

//			    float3 base = cross(blackholeDisk.xyz, float3(0.0, 0.0, 1.0));
				float angle = acos(dot(normalize(base1), normalize(pos)));
				if (dot(cross(base1, pos), diskNormal) < 0.0) angle = -angle;
				angle-= universalTime * rotationSpeed;

				float u = 0.5 - angle / TWOPI;

				float4 color = tex2Dlod(AccretionDisk, float4(u, v,0.0,0.0));
#else
				float u = dot (base1,normalize(pos)) * (length(pos)-diskInnerRadius) / (diskOuterRadius-diskInnerRadius);
				float v = dot (base2,normalize(pos)) * (length(pos)-diskInnerRadius) / (diskOuterRadius-diskInnerRadius);

				float2 UV = float2(u,v);

				//can simplify these and move them out of this loop
//				float sinX = sin ( -_Time.y * rotationSpeed );
//            	float cosX = cos ( -_Time.y * rotationSpeed );

				float sinX = sin ( universalTime * rotationSpeed );
            	float cosX = cos ( universalTime * rotationSpeed );

				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);
				UV = mul(rotationMatrix, UV);

				UV = 0.5 - 0.5 * UV;
				UV = clamp (UV,0.0,1.0);

				float4 color = tex2Dlod(AccretionDisk, float4(UV,0.0,0.0));
#endif
				return color.rgb*color.a;
			}

			inline float sphereDistance(float3 rayPosition, float3 rayDirection, float4 sphere)
			{
				float3 v;
				float p, d;
				v = rayPosition - sphere.xyz;
				p = dot(rayDirection, v);
				d = p * p + sphere.w * sphere.w - dot(v, v);

				//return d < 0.0 ? -1.0 : -p - sqrt(d);
				return d < 0.0 ? INFINITY : -p - sqrt(d);
			}

			//simplify this?
			void testDistance(int i, float distance, inout float currentDistance, inout int currentObject, float maxDistance)
			{
			  float EPSILON = 0.0001;

			  if (distance >= EPSILON && distance < currentDistance && distance < maxDistance)
			  {
			    currentDistance = distance;
			    currentObject = i;
			  }
			}

			// inigo quilez plane intersect, saves like 15% performance on the whole shader
			// plane designed by p (p.xyz must be normalized)
			float plaIntersect( float3 ro, float3 rd, float3 p )
			{
    			return -(dot(ro,p))/dot(rd,p);
			}

			//original ringIntersect function, overly complicated and slow
			inline float ringDistance(float3 rayPosition, float3 rayDirection, float3 blackHoleOrigin)
			{
			  float EPSILON = 0.0001;

			  float denominator = dot(rayDirection, diskNormal);
			  float constant = -dot(blackHoleOrigin, diskNormal);
			  float distanceToCenter;
			  if (abs(denominator) < EPSILON)
			  {
			    return -1.0;
			  }
			  else
			  {
			    float t = -(dot(rayPosition, diskNormal) + constant) / denominator;
			    if (t < 0.0) return -1.0;

			    float3 intersection = rayPosition + t * rayDirection;
			    distanceToCenter = length(intersection - blackHoleOrigin);
			    if (distanceToCenter >= diskInnerRadius && distanceToCenter <= diskOuterRadius)
			    {
			      return t;
			    }
			    return -1.0;
			  }
			}

			// we check if object is on the screen buffer or not
			inline bool onScreenBuffer(float3 position, float3 blackHoleOrigin, out float depth, out float3 screenColor)
			{
				float4 clipPos = UnityWorldToClipPos(float4(position,1.0));
  				float4 screenPos = ComputeScreenPos(clipPos);
  				screenPos.xyz/=screenPos.w;

  				depth =  tex2D(_CameraDepthTexture, screenPos.xy);
				screenColor = tex2D(screenBuffer,screenPos.xy);

				float4 depthClipPos = float4(screenPos.xy, 1.0-depth, 1.0);
    			depthClipPos.xyz = 2.0f * depthClipPos.xyz - 1.0f;

				//position of the fragment we are getting from the screen texture
				float4 camPos = mul(unity_CameraInvProjection, depthClipPos);
				camPos.xyz /= camPos.w;
				camPos.z *= -1;

				float3 forward = mul((float3x3) unity_CameraToWorld, float3(0,0,1));
				float screenBufferDistance = length(camPos.xyz);
				bool behindBlackHole = (screenBufferDistance > length(_WorldSpaceCameraPos.xyz-blackHoleOrigin)) || (dot(blackHoleOrigin-_WorldSpaceCameraPos.xyz,forward) < 0.0 );

				return(behindBlackHole && screenPos.x >= 0.0 && screenPos.x <= 1.0 && screenPos.y >= 0.0 && screenPos.y <= 1.0 && (dot(forward,position-_WorldSpaceCameraPos) > 0.0)) ; //idk why couldn't check with z
			}

			float4 raytrace(float3 rayPosition, float3 rayDirection, float3 blackHoleOrigin)
			{				
				float currentDistance = INFINITY;
				int   currentObject = -1;
				float3  hitPosition;

				float stepSize, rayDistance;
				float3 gravityVector, rayAccel;
				float objectDistance;

				float3 rayNextPosition = rayPosition;
				
				float3 originalRayDir = rayDirection;

				//float4 color = float4(1.0/255.0, 1.0/255.0, 1.0/255.0, 1.0); //HACK: make it one level above absolute black, so other blackholes in the cubemap don't get masked out
				float4 color = float4(0.0, 0.0, 0.0, 1.0);

				bool wormholeHit = false;

#if defined (ACCRETION_DISK_ON)
				//acretion disk base vectors
				float3 base1 = normalize(cross(diskNormal, diskNormal.zxy)); //move this to plugin precomputation, check if 2nd vector is equal to first and re-change it
				float3 base2 = normalize(cross(base1, diskNormal)); 		 //move this to precomputation?
#endif

				for (int i = 0; i < 35; i++)
				{
					currentObject = -1;
			  		currentDistance = INFINITY;

			  		// Bend the light towards the black hole
			  		gravityVector = blackHoleOrigin - rayPosition;
			  		rayDistance = length(gravityVector);

			  		// 0.05: rate of smaller steps when approaching blackhole
			  		stepSize = rayDistance - (blackHoleRadius *0.05);

			  		rayAccel = normalize(gravityVector) * gravity / (rayDistance * rayDistance);

					float accelFadeOut = clamp( ( length(rayAccel) - 0.001 ) / 0.001, 0.0, 1.0); //we fade out acceleration over the last 0.001 so we can have a smaller enclosing mesh
			  		rayAccel = lerp(0.0, rayAccel, accelFadeOut);

			  		rayDirection=normalize(rayDirection * lightSpeed * stepSize + rayAccel * stepSize);

			  		objectDistance = sphereDistance(rayPosition, rayDirection, float4(blackHoleOrigin, blackHoleRadius * 1.0));
			  		testDistance(ID_BLACKHOLE, objectDistance, currentDistance, currentObject, lightSpeed*stepSize);

#if defined (ACCRETION_DISK_ON)
			  		//objectDistance = ringDistance(rayPosition, rayDirection, blackHoleOrigin);
			  		objectDistance = plaIntersect(rayPosition.xyz-blackHoleOrigin.xyz, rayDirection, diskNormal);
			  		testDistance(ID_BLACKHOLE_DISK, objectDistance, currentDistance, currentObject, lightSpeed*stepSize);
#endif
			  		// Check if we hit any object, and if so, stop integrating
			  		if (currentObject != -1 && currentDistance <= rayDistance)
			  		{
#if defined (ACCRETION_DISK_ON)
			  		   //But if it's something transparent, get its color, and continue
			  		  if (currentObject == ID_BLACKHOLE_DISK)
			  		  {
			  		    hitPosition = rayPosition + rayDirection * currentDistance;
			  		    color.rgb += (1.0-color.rgb) * accretionDiskColor(hitPosition,base1,base2,blackHoleOrigin).rgb; //soft blend them
			  		    currentObject = -1;
			  		  }
			  		  else
#endif
			  		  {
			  		  //add && currentDistance <= rayDistance
#if defined (WORMHOLE_ON)
			  		    wormholeHit = true;
#else
			  		    break;
#endif
			  		  }
			  		}

					//move forward
					rayPosition += lightSpeed * stepSize * rayDirection;
				}

				if (currentObject != ID_BLACKHOLE && length(rayPosition) > blackHoleRadius && !wormholeHit)
				{
					//consider enabling/disabling mipMaps and texcube Bias (possibly negative, to strike balance between jittering and clarity, maybe even a crisper image when the light is blueshifted
					float3 galaxyCubeMapColor = texCUBE(CubeMap, mul(cubeMapRotation,float4(rayDirection,1.0)).xyz) * galaxyFadeColor;

#if defined (GALAXYCUBEMAPONLY_OFF)
					float3 infinityPos = rayPosition + rayDirection * 2000.0; //we take an assumption here about the distance of a distinguishable object

					float3 objectCubeMapDir = normalize(infinityPos - blackHoleOrigin); //is this even necessary?
					float4 objectCubeMapColor = texCUBE(objectCubeMap,objectCubeMapDir);
					bool onObjectCubeMap = (objectCubeMapColor.r != 0.0) || (objectCubeMapColor.g != 0.0) || (objectCubeMapColor.b != 0.0) || (objectCubeMapColor.a != 0.0); // check if the objectCubeMap has the object
					//maybe in this case also do some blending over the last 0.05-0.1?
					objectCubeMapColor.rgb*=galaxyFadeColor;

					float3 screenColor = 0.0;
					bool onScreen = false;
					float depth = 0.0;

					if (useScreenBuffer == 1.0) //if should be fine since all units will be running the same branch I think, maybe make shader variant instead if in doubt?
					{
						onScreen = onScreenBuffer(infinityPos, blackHoleOrigin, depth, screenColor);
					}

					if (length(_WorldSpaceCameraPos.xyz-blackHoleOrigin) > 4 * blackHoleRadius)
					{
						// on screen -> use screenColor if actually an object or galaxyColor, object off screen -> use objectCubeMapColor if actually an object or galaxyColor
						screenColor = onScreen ? ( depth > 0.0 ? screenColor : galaxyCubeMapColor ) : ( onObjectCubeMap ? objectCubeMapColor.rgb : galaxyCubeMapColor);
					}
					else
					{
						// below a certain altitude we just use the cubemap all the time because the heavy distorsion causes differences to be visible between the two
						screenColor =  onObjectCubeMap ? objectCubeMapColor.rgb : galaxyCubeMapColor;
					}

					color.rgb += (1.0 - color.rgb) * screenColor;
#else
					color.rgb += (1.0 - color.rgb) * galaxyCubeMapColor;
#endif
				}
#if defined (WORMHOLE_ON)
				else
				{
					float3 galaxyCubeMapColor = texCUBE(CubeMap, mul(cubeMapRotation,float4(rayDirection,1.0)).xyz) * galaxyFadeColor;

					float4 wormholeColor = texCUBE(wormholeCubemap,rayDirection);
					bool onWormholeCubeMap = (wormholeColor.r != 0.0) || (wormholeColor.g != 0.0) || (wormholeColor.b != 0.0) || (wormholeColor.a != 0.0);

					color.rgb =  onWormholeCubeMap ? wormholeColor.rgb : galaxyCubeMapColor;
				}
#endif
				return color;
			}

            float4 frag (v2f i) : SV_Target
            {
            	i.worldPos.xyz/=i.worldPos.w;
            	float3 viewDir = normalize(i.worldPos.xyz-_WorldSpaceCameraPos);

  				float4 color = raytrace(_WorldSpaceCameraPos, viewDir, i.blackHoleOrigin.xyz/i.blackHoleOrigin.w);

  				return color;
            }
            ENDCG
        }
    }
}
