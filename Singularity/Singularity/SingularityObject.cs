using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;

namespace Singularity
{
	public class SingularityObject : MonoBehaviour
	{
		[Persistent] public string name;
		
		[Persistent] public float gravity = 1f;
		[Persistent] public float schwarzschildRadius = 0f;

		[Persistent] public bool hideCelestialBody = true;

		[Persistent] public bool useAccretionDisk = false;
		[Persistent] public bool useRadialTextureMapping = false;
		
		[Persistent] public Vector3 accretionDiskNormal = Vector3.up;
		[Persistent] public float accretionDiskInnerRadius = 1f;
		[Persistent] public float accretionDiskOuterRadius = 5f;

		[Persistent] public float accretionDiskRotationSpeed = 60f;
		
		[Persistent] public string wormholeTarget = "";
		[Persistent] public string accretionDiskTexturePath = "";

		[Persistent] public float scaleEnclosingMesh = 1f;

		[Persistent] public bool depthWrite = true;

		[Persistent] public float dopplerEffectIntensityRate = 0f;
		[Persistent] public float dopplerEffectIntensityFactor = 0.8f;
		[Persistent] public float dopplerEffectIntensityOffset = 1.0f;
		[Persistent] public float dopplerEffectColorFactor = 0f;

		float scaledRadius = 1f;
		float enclosingMeshRadius = 1f;

		Material singularityMaterial, stackedCopyMaterial;
		Texture2D AccretionDiskTexture;

		MeshRenderer scaledPlanetMeshRenderer;

		GameObject singularityGO;
		public MeshRenderer singularityMeshRenderer; //change this to be non public?
		public SingularityCenteredCubeMap singularityCubeMap;

		bool hasWormhole = false;
		SingularityCenteredCubeMap wormholeCubeMap;

		public SingularityObject ()
		{

		}

		public void Init(ConfigNode _cn)
		{
			Utils.LogDebug ("Initializing object from config:\r\n" + _cn.ToString ());

			ConfigNode.LoadObjectFromConfig (this, _cn);

			singularityMaterial = new Material(Singularity.LoadedShaders ["Singularity/BlackHoleAccretionDisk"]);

			if (!_cn.HasValue ("schwarzschildRadius"))
			{
				schwarzschildRadius = 32400f * Mathf.Sqrt(Mathf.Abs(gravity));
			}
			//scaledRadius = Mathf.Sqrt (Mathf.Max(gravity,0f)) * 5f;								// The apparent radius (in scaled Space) of the black hole (or event horizon), not physically correct
			scaledRadius = schwarzschildRadius / 6000f * 0.926f;								// The apparent radius (in scaled Space) of the black hole (or event horizon), not physically correct
			singularityMaterial.SetFloat("blackHoleRadius", scaledRadius);

			//enclosingMeshRadius = scaleEnclosingMesh * Mathf.Sqrt (Mathf.Abs(gravity)) * 120f;	// The radius (in scaled Space) at which the gravity no longer warps the image
			enclosingMeshRadius = schwarzschildRadius / 27f;	// The radius (in scaled Space) at which the gravity no longer warps the image
																   								// Serves as the radius of our enclosing mesh, value finetuned manually
			singularityMaterial.SetFloat("enclosingMeshRadius", enclosingMeshRadius);
			singularityMaterial.SetFloat("gravity", gravity);
			singularityMaterial.SetFloat("schwarzschildRadius", schwarzschildRadius);
			singularityMaterial.renderQueue = 2501; //No need to be same renderqueue as scatterer atmos, i's treated as an opaque object, when atmos/clouds are behind it they are included in the re-rendered scaledSpace scene
													//Otherwise they are handled by depth-testing 

			singularityMaterial.EnableKeyword ("GALAXYCUBEMAPONLY_OFF");
			singularityMaterial.DisableKeyword ("GALAXYCUBEMAPONLY_ON");

			ConfigureAccretionDisk ();

			scaledPlanetMeshRenderer = gameObject.GetComponent<MeshRenderer> ();


//			// When not hiding the celestialBody, objects write to depth buffer which messes up the lensing, try to disable it through renderType Tags
//			// But it's not just the depth, we also need to disable the actual object when pre-rendering the screen
//			// Didn't work, but since we shouldn't have planets that close to stars, whatever
//			if (!ReferenceEquals (scaledPlanetMeshRenderer, null) && !ReferenceEquals (scaledPlanetMeshRenderer.material, null))
//			{
//				scaledPlanetMeshRenderer.material.SetOverrideTag ("RenderType", "Transparent")
//			}

			if (hideCelestialBody)
			{
				HideCelestialBody ();
			}

			SetupGameObject ();

			singularityMaterial.SetTexture ("CubeMap", Singularity.Instance.galaxyCubemap);
			if (Singularity.Instance.lensingStacking)
			{
				ScaledCamera.Instance.cam.forceIntoRenderTexture = true;
				singularityMaterial.EnableKeyword ("CUSTOM_DEPTH_TEXTURE_ON");
				singularityMaterial.DisableKeyword ("CUSTOM_DEPTH_TEXTURE_OFF");
			}
			else
			{
				singularityMaterial.SetTexture ("singularityScreenBuffer", Singularity.Instance.screenBufferFlip);
				singularityMaterial.EnableKeyword ("CUSTOM_DEPTH_TEXTURE_OFF");
				singularityMaterial.DisableKeyword ("CUSTOM_DEPTH_TEXTURE_ON");
			}

			stackedCopyMaterial = new Material(Singularity.LoadedShaders ["Singularity/StackedLensingCopy"]);
			stackedCopyMaterial.SetInt ("_ZwriteVariable", depthWrite ? 1 : 0);
			stackedCopyMaterial.renderQueue = 2501;

			StartCoroutine (SetupWormhole ());
		}

		void ConfigureAccretionDisk ()
		{
			singularityMaterial.DisableKeyword ("ACCRETION_DISK_ON");
			singularityMaterial.EnableKeyword ("ACCRETION_DISK_OFF");

			if (useAccretionDisk)
			{
				if (String.IsNullOrEmpty (accretionDiskTexturePath))
				{
					Utils.LogError ("Accretion disk enabled but no accretion disk texture, disabling accretion disk");
					useAccretionDisk = false;
					return;
				}
				
				if (!System.IO.File.Exists (Singularity.Instance.GameDataPath + accretionDiskTexturePath))
				{
					Utils.LogError ("Accretion disk enabled but texture can't be located at: " + accretionDiskTexturePath + ", disabling accretion disk");
					useAccretionDisk = false;
					return;
				}
				 
				if (Path.GetExtension(accretionDiskTexturePath) == ".dds")
				{
					AccretionDiskTexture = Utils.LoadDDSTexture(System.IO.File.ReadAllBytes (Singularity.Instance.GameDataPath + accretionDiskTexturePath), accretionDiskTexturePath);
				}
				else
				{
					AccretionDiskTexture = new Texture2D (1, 1);
					AccretionDiskTexture.LoadImage (System.IO.File.ReadAllBytes (Singularity.Instance.GameDataPath + accretionDiskTexturePath));
				}

				AccretionDiskTexture.wrapMode = useRadialTextureMapping ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
				singularityMaterial.SetTexture ("AccretionDisk", AccretionDiskTexture);
				
				if (useRadialTextureMapping)
				{
					singularityMaterial.DisableKeyword ("RADIAL_DISK_MAPPING_OFF");
					singularityMaterial.EnableKeyword ("RADIAL_DISK_MAPPING_ON");
				} else
				{
					singularityMaterial.DisableKeyword ("RADIAL_DISK_MAPPING_ON");
					singularityMaterial.EnableKeyword ("RADIAL_DISK_MAPPING_OFF");
				}

				singularityMaterial.SetVector ("diskNormal", accretionDiskNormal);
				singularityMaterial.SetFloat ("diskInnerRadius", accretionDiskInnerRadius / 6000f); //change to scaledSpace scale
				singularityMaterial.SetFloat ("diskOuterRadius", accretionDiskOuterRadius / 6000f);

				singularityMaterial.SetFloat ("dopplerIntensityRate", dopplerEffectIntensityRate);
				singularityMaterial.SetFloat ("dopplerIntensityFactor", dopplerEffectIntensityFactor);
				singularityMaterial.SetFloat ("dopplerIntensityOffset", dopplerEffectIntensityOffset);
				singularityMaterial.SetFloat ("dopplerColorFactor", dopplerEffectColorFactor);

				//convert from RPM to rad/s
				singularityMaterial.SetFloat("rotationSpeed", accretionDiskRotationSpeed * (Mathf.PI * 2) / 60);

				singularityMaterial.DisableKeyword ("ACCRETION_DISK_OFF");
				singularityMaterial.EnableKeyword ("ACCRETION_DISK_ON");
			}
		}

		void HideCelestialBody ()
		{
			if (!ReferenceEquals (scaledPlanetMeshRenderer, null))
			{
				scaledPlanetMeshRenderer.enabled = false;
			}
		}
		
		void UnHideCelestialBody ()
		{
			if (!ReferenceEquals (scaledPlanetMeshRenderer, null))
			{
				scaledPlanetMeshRenderer.enabled = true;
			}
		}
		
		void SetupGameObject ()
		{
			singularityGO = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			singularityGO.name = name + " singularity";

			singularityGO.layer = 10;

			singularityGO.transform.position = gameObject.transform.position;
			singularityGO.transform.parent = gameObject.transform;

			GameObject.Destroy (singularityGO.GetComponent<Collider> ());

			singularityGO.transform.localScale = new Vector3 (enclosingMeshRadius / gameObject.transform.localScale.x, enclosingMeshRadius / gameObject.transform.localScale.y, enclosingMeshRadius / gameObject.transform.localScale.z);

			singularityMeshRenderer = singularityGO.GetComponent<MeshRenderer> ();
			singularityMeshRenderer.material = singularityMaterial;
			singularityMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			singularityMeshRenderer.receiveShadows = false;
			singularityMeshRenderer.enabled = true;

			singularityCubeMap = singularityGO.AddComponent<SingularityCenteredCubeMap> ();
			singularityCubeMap.singularityMaterial = singularityMaterial;
			singularityCubeMap.parentSingularity = this;
		}

		IEnumerator SetupWormhole()
		{
			yield return new WaitForFixedUpdate ();

			singularityMaterial.DisableKeyword ("WORMHOLE_ON");
			singularityMaterial.EnableKeyword ("WORMHOLE_OFF");
			hasWormhole = false;

			if (!String.IsNullOrEmpty (wormholeTarget))
			{
				SingularityObject wormholeTargetSingularity = Singularity.Instance.loadedObjects.SingleOrDefault (_so => _so.name == wormholeTarget);
				
				if (ReferenceEquals (wormholeTargetSingularity, null))
				{
					Utils.LogError ("Wormhole target not found, disabling");
					yield break;
				}
				singularityMaterial.SetTexture ("wormholeCubemap", wormholeTargetSingularity.singularityCubeMap.singularityCubemap);
				singularityMaterial.DisableKeyword ("WORMHOLE_OFF");
				singularityMaterial.EnableKeyword ("WORMHOLE_ON");
				wormholeCubeMap = wormholeTargetSingularity.singularityCubeMap;
				hasWormhole = true;
			}
		}

		public void Update()
		{
			// Is this needed every frame?
			if (hideCelestialBody)
				HideCelestialBody ();

			singularityMaterial.SetColor("galaxyFadeColor", Singularity.Instance.galaxyCubeControlMPB.GetColor (PropertyIDs._Color));
			singularityMaterial.SetMatrix ("cubeMapRotation", Matrix4x4.Rotate (Planetarium.Rotation).inverse);

			if (useAccretionDisk)
				singularityMaterial.SetFloat ("universalTime", Singularity.Instance.getTime ());
		}

		// Disable rendering from our cubeMap (so no recursive rendering) or sceneBuffer
		// Called from onWillRender, disabling MR or GO here will break rendering, so use layer
		public void DisableForSceneOrCubemap()
		{
			singularityGO.layer = 0;
		}

		public void ReEnable()
		{
			singularityGO.layer = 10;
		}

		public void SwitchToNormalMode()
		{
			singularityMeshRenderer.material = singularityMaterial;
		}

		public void SwitchToCopyMode()
		{
			singularityMeshRenderer.material = stackedCopyMaterial;
		}

		public void UpdateTargetWormhole()
		{
			if (hasWormhole)
			{
				wormholeCubeMap.UpdateCubeMapAndScreenBuffer();
			}
		}

		public void ApplyFromUI(ConfigNode _cn)
		{
			Utils.LogDebug ("Applying config from UI:\r\n" + _cn.ToString ());

			if (!ConfigNode.LoadObjectFromConfig (this, _cn))
			{
				Utils.LogError("Apply failed");
				return;
			}

			//scaledRadius = Mathf.Sqrt (Mathf.Max(gravity,0f)) * 5f;
			if (!_cn.HasValue ("schwarzschildRadius"))
			{
				schwarzschildRadius = 32400f * Mathf.Sqrt(Mathf.Abs(gravity));
			}
			scaledRadius = schwarzschildRadius / 6000f * 0.926f;
			singularityMaterial.SetFloat("blackHoleRadius", scaledRadius);

			singularityMaterial.SetFloat("gravity", gravity);
			singularityMaterial.SetFloat("schwarzschildRadius", schwarzschildRadius);

			ConfigureAccretionDisk ();
			
			if (hideCelestialBody)
			{
				HideCelestialBody ();
			}
			else
			{
				UnHideCelestialBody();
			}

			//enclosingMeshRadius = scaleEnclosingMesh * Mathf.Sqrt (Mathf.Abs(gravity)) * 120f;
			enclosingMeshRadius = schwarzschildRadius / 27f;
			singularityMaterial.SetFloat("enclosingMeshRadius", enclosingMeshRadius);
			singularityGO.transform.localScale = new Vector3 (enclosingMeshRadius / gameObject.transform.localScale.x, enclosingMeshRadius / gameObject.transform.localScale.y, enclosingMeshRadius / gameObject.transform.localScale.z);

			stackedCopyMaterial.SetInt ("_ZwriteVariable", depthWrite ? 1 : 0);

			StartCoroutine (SetupWormhole ());
		}

		public void OnDestroy()
		{
			if (!ReferenceEquals (singularityGO, null))
			{
				UnityEngine.Object.Destroy(singularityGO);
			}

			if (!ReferenceEquals(scaledPlanetMeshRenderer,null))
			{
				scaledPlanetMeshRenderer.enabled = true;
			}
		}

		public float GetSizeInpixels(Camera cam)
		{
			return Utils.DistanceAndDiameterToPixelSize ((gameObject.transform.position - cam.transform.position).magnitude, enclosingMeshRadius, ScaledCamera.Instance.cam);
		}		
	}
}