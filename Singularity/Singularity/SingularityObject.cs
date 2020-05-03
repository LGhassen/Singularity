using System;
using UnityEngine;
using System.Collections.Generic;

namespace Singularity
{
	public class SingularityObject : MonoBehaviour
	{
		[Persistent] public string name;
		
		[Persistent] public float gravity = 1f;
		[Persistent] public float radius = 10f;

		[Persistent] public bool hideCelestialBody = true;

		[Persistent] public bool useAccretionDisk = false;
		[Persistent] public string accretionDiskTexturePath = "";
		[Persistent] public bool useRadialTextureMapping = false;
		
		[Persistent] public Vector3 accretionDiskNormal = Vector3.up;
		[Persistent] public float accretionDiskInnerRadius = 1f;
		[Persistent] public float accretionDiskOuterRadius = 5f;

		float scaledRadius = 1f;
		float enclosingMeshRadius = 1f;

		Material singularityMaterial;
		Texture2D AccretionDiskTexture;

		MeshRenderer scaledPlanetMeshRenderer;

		GameObject singularityGO;
		SingularityCenteredCubeMap singularityCubeMap;

		public SingularityObject ()
		{

		}

		public void Init(ConfigNode _cn)
		{
			Utils.LogDebug ("Initializing object from config:\r\n" + _cn.ToString ());

			ConfigNode.LoadObjectFromConfig (this, _cn);

			singularityMaterial = new Material(Singularity.LoadedShaders ["Singularity/BlackHoleAccretionDisk"]);

			scaledRadius = radius / 6000f;
			singularityMaterial.SetFloat("blackHoleRadius", scaledRadius);

			enclosingMeshRadius = Mathf.Sqrt (Mathf.Abs(gravity)) / 0.00836f; // The radius (in scaled Space) at which the gravity no longer warps the image
																   // Serves as the radius of our enclosing mesh, mostly trial and error

			singularityMaterial.SetFloat("gravity", gravity);
			singularityMaterial.renderQueue = 3005;

			if (useAccretionDisk)
			{
				ConfigureAccretionDisk ();
			}

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
			singularityMaterial.SetTexture ("screenBuffer", Singularity.Instance.screenBuffer);

			//TODO: if wormHole -> delay for a few frames then grab the renderCube of target singularity
		}

		void ConfigureAccretionDisk ()
		{
			if (String.IsNullOrEmpty (accretionDiskTexturePath))
			{
				Utils.LogError ("Accretion disk enabled but no accretion disk texture, disabling accretion disk");
				useAccretionDisk = false;
				return;
			}

			if (!System.IO.File.Exists (Singularity.Instance.GameDataPath + accretionDiskTexturePath))
			{
				Utils.LogError ("Accretion disk enabled but texture can't be located at: "+accretionDiskTexturePath+", disabling accretion disk");
				useAccretionDisk = false;
				return;
			}

			AccretionDiskTexture = new Texture2D (1, 1);
			AccretionDiskTexture.LoadImage (System.IO.File.ReadAllBytes (Singularity.Instance.GameDataPath + accretionDiskTexturePath));
			AccretionDiskTexture.wrapMode = TextureWrapMode.Repeat;
			singularityMaterial.SetTexture ("AccretionDisk", AccretionDiskTexture);

			if (useRadialTextureMapping)
			{
				singularityMaterial.DisableKeyword ("RADIAL_DISK_MAPPING_OFF");
				singularityMaterial.EnableKeyword ("RADIAL_DISK_MAPPING_ON");
			}
			else
			{
				singularityMaterial.DisableKeyword ("RADIAL_DISK_MAPPING_ON");
				singularityMaterial.EnableKeyword ("RADIAL_DISK_MAPPING_OFF");
			}

			singularityMaterial.SetVector ("diskNormal", accretionDiskNormal);
			singularityMaterial.SetFloat ("diskInnerRadius", accretionDiskInnerRadius / 6000f); //change to scaledSpace scale
			singularityMaterial.SetFloat ("diskOuterRadius", accretionDiskOuterRadius / 6000f);
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

			MeshRenderer singularityMR = singularityGO.GetComponent<MeshRenderer> ();
			singularityMR.material = singularityMaterial;
			singularityMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			singularityMR.receiveShadows = false;
			singularityMR.enabled = true;

			singularityCubeMap = singularityGO.AddComponent<SingularityCenteredCubeMap> ();
			singularityCubeMap.singularityMaterial = singularityMaterial;
			singularityCubeMap.singularityGO = singularityGO;
		}

		public void Update()
		{
			singularityMaterial.renderQueue = 2999; //same renderqueue as scatterer sky, so it can render below or on top of it, depending on which is in front, EVE clouds are handled by depth-testing 

			// Is this needed eevery frame?
			if (hideCelestialBody)
				HideCelestialBody ();

			singularityMaterial.SetColor("galaxyFadeColor", Singularity.Instance.galaxyCubeControlMPB.GetColor (PropertyIDs._Color));
			singularityMaterial.SetMatrix ("cubeMapRotation", Matrix4x4.Rotate (Planetarium.Rotation).inverse);
		}

		public void ApplyFromUI(ConfigNode _cn)
		{
			Utils.LogDebug ("Applying config from UI:\r\n" + _cn.ToString ());

			if (!ConfigNode.LoadObjectFromConfig (this, _cn))
			{
				Utils.LogError("Apply failed");
				return;
			}

			scaledRadius = radius / 6000f;
			singularityMaterial.SetFloat("blackHoleRadius", scaledRadius);

			singularityMaterial.SetFloat("gravity", gravity);
			
			if (useAccretionDisk)
			{
				ConfigureAccretionDisk ();
			}
			
			if (hideCelestialBody)
			{
				HideCelestialBody ();
			}
			else
			{
				UnHideCelestialBody();
			}

			enclosingMeshRadius = Mathf.Sqrt (Mathf.Abs(gravity)) / 0.00836f;
			singularityGO.transform.localScale = new Vector3 (enclosingMeshRadius / gameObject.transform.localScale.x, enclosingMeshRadius / gameObject.transform.localScale.y, enclosingMeshRadius / gameObject.transform.localScale.z);
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
	}
}