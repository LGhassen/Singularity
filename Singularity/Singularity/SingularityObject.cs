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

		public SingularityObject ()
		{

		}

		public void Init(ConfigNode _cn)
		{
			Utils.LogDebug ("Initializing object from config: " + _cn.ToString ());

			ConfigNode.LoadObjectFromConfig (this, _cn);

			singularityMaterial = new Material(Singularity.LoadedShaders ["Singularity/BlackHoleAccretionDisk"]);

			scaledRadius = radius / 6000f;

			//this one probably can get it directly in the shader itself instead of setting it every time
			singularityMaterial.SetFloat("blackHoleRadius", scaledRadius);
			singularityMaterial.SetFloat("gravity", gravity);
			singularityMaterial.renderQueue = 3005;

			if (useAccretionDisk)
			{
				ConfigureAccretionDisk ();
			}

			//arrived here but don't see any object created or Mun hidden, gotta debug the scaledTransform thing
			if (hideCelestialBody)
			{
				scaledPlanetMeshRenderer = gameObject.GetComponent<MeshRenderer>();
				if (!ReferenceEquals(scaledPlanetMeshRenderer,null))
				{
					scaledPlanetMeshRenderer.enabled = false;
				}
			}

			GameObject singularityGO = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			singularityGO.name = name + " singularity";
			singularityGO.layer = 10;

			singularityGO.transform.position = gameObject.transform.position;
			singularityGO.transform.parent = gameObject.transform;

			GameObject.Destroy (singularityGO.GetComponent<Collider> ());			
			//singularityGO.transform.localScale = Vector3.one; //I think I can just control the scale from here, instead of  messing with the mesh like in scatterer
			singularityGO.transform.localScale = new Vector3 (radius * 10f, scaledRadius * 10f, scaledRadius * 10f); //doesn't appear to work
			Utils.LogInfo ("Radius " + radius.ToString ());
			Utils.LogInfo ("Scaled Radius " + scaledRadius.ToString ());
			
			MeshRenderer singularityMR = singularityGO.GetComponent<MeshRenderer>();
			singularityMR.material = singularityMaterial;

			singularityMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			singularityMR.receiveShadows = false;
			singularityMR.enabled = true;


		}

		void ConfigureAccretionDisk ()
		{
			if (String.IsNullOrEmpty (accretionDiskTexturePath))
			{
				Utils.LogError ("Accretion disk enabled but no acrretion disk texture, disabling accretion disk");
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

		public void Update()
		{
			if (!ReferenceEquals(scaledPlanetMeshRenderer,null))
			{
				scaledPlanetMeshRenderer.enabled = false;
			}
		}
	}
}