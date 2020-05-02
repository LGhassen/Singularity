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

			singularityMaterial.SetFloat("gravity", gravity);
			singularityMaterial.renderQueue = 3005;

			if (useAccretionDisk)
			{
				ConfigureAccretionDisk ();
			}

			if (hideCelestialBody)
			{
				HideCelestialBody ();
			}

			SetupGameObject ();

			singularityMaterial.SetTexture ("CubeMap", Singularity.Instance.galaxyCubemap);
			singularityMaterial.SetTexture ("screenBuffer", Singularity.Instance.screenBuffer);
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
			scaledPlanetMeshRenderer = gameObject.GetComponent<MeshRenderer> ();
			if (!ReferenceEquals (scaledPlanetMeshRenderer, null))
			{
				scaledPlanetMeshRenderer.enabled = false;
			}
		}
		
		void SetupGameObject ()
		{
			singularityGO = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			singularityGO.name = name + " singularity";

			//singularityGO.layer = 10;
			singularityGO.layer = 9;

			singularityGO.transform.position = gameObject.transform.position;
			singularityGO.transform.parent = gameObject.transform;

			GameObject.Destroy (singularityGO.GetComponent<Collider> ());

			//singularityGO.transform.localScale = new Vector3 (scaledRadius * 10f, scaledRadius * 10f, scaledRadius * 10f); //objects come out waaay smaller than expected, localScale might have sth to do with it?
			singularityGO.transform.localScale = new Vector3 (scaledRadius * 80f / gameObject.transform.localScale.x, scaledRadius * 80f / gameObject.transform.localScale.y, scaledRadius * 80f / gameObject.transform.localScale.z);

			MeshRenderer singularityMR = singularityGO.GetComponent<MeshRenderer> ();
			singularityMR.material = singularityMaterial;
			singularityMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			singularityMR.receiveShadows = false;
			singularityMR.enabled = true;

			singularityCubeMap = singularityGO.AddComponent<SingularityCenteredCubeMap> ();
			singularityCubeMap.singularityMR = singularityMR;
			singularityCubeMap.singularityMaterial = singularityMaterial;
		}

		public void Update()
		{
			singularityMaterial.renderQueue = 2999; //same renderqueue as scatterer sky, so it can render below or on top of it, depending on which is in front, EVE clouds are handled by depth-testing 

			if (!ReferenceEquals(scaledPlanetMeshRenderer,null))
			{
				scaledPlanetMeshRenderer.enabled = false;
			}

			singularityMaterial.SetColor("galaxyFadeColor", Singularity.Instance.galaxyCubeControlMPB.GetColor (PropertyIDs._Color));
			singularityMaterial.SetMatrix ("cubeMapRotation", Matrix4x4.Rotate (Planetarium.Rotation).inverse);
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