using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime;
using KSP;
using KSP.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Singularity
{
	public class SingularityCenteredCubeMap : MonoBehaviour
	{
		Camera objectCamera;
		public RenderTexture singularityCubemap;
		public SingularityObject parentSingularity;
		public Material singularityMaterial;

		bool cubeMapUpdated=false;
		int cubemapFaceToUpdate = 0;

		static int screenBufferProperty = Shader.PropertyToID("useScreenBuffer");

		void Awake()
		{
			objectCamera = gameObject.AddComponent<Camera> ();
			objectCamera.cullingMask = (1 << 9) | (1 << 10);
			objectCamera.renderingPath = ScaledCamera.Instance.cam.renderingPath;
			objectCamera.depthTextureMode = DepthTextureMode.None;
			objectCamera.farClipPlane = ScaledCamera.Instance.cam.farClipPlane;
			objectCamera.nearClipPlane = ScaledCamera.Instance.cam.nearClipPlane;

			objectCamera.clearFlags = CameraClearFlags.Color;
			objectCamera.backgroundColor = Color.clear;
			objectCamera.enabled = false;

			objectCamera.transform.position = gameObject.transform.position;
			objectCamera.transform.parent = gameObject.transform;

			singularityCubemap = new RenderTexture(Singularity.Instance.objectCubemapResolution, Singularity.Instance.objectCubemapResolution, 16, RenderTextureFormat.ARGB32, 9);
			singularityCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
			singularityCubemap.autoGenerateMips = true;
			singularityCubemap.filterMode = FilterMode.Bilinear;
			singularityCubemap.Create ();

			RenderTexture rt = RenderTexture.active;
			RenderTexture.active = singularityCubemap;
			GL.Clear(true, true, Color.clear);
			RenderTexture.active = rt;
			
			StartCoroutine (SetMaterialProperties ());

			objectCamera.targetTexture = null;
		}
		
		IEnumerator SetMaterialProperties()
		{
			yield return new WaitForFixedUpdate ();
			singularityMaterial.SetTexture ("objectCubeMap", singularityCubemap);
			singularityMaterial.SetFloat(screenBufferProperty,1f);
		}

		public void Update()
		{
			cubeMapUpdated = false;
		}


		public void OnWillRenderObject()
		{
			if (Camera.current == ScaledCamera.Instance.cam)
			{
				//this seems to trigger from way too far out, so check here that the singularity is larger than 1 pixels on-screen, maybe also add a check that the sphere intersects the view frustum (though that sounds a bit complex)
				if (parentSingularity.GetSizeInpixels(ScaledCamera.Instance.cam) > 1)
				{
					UpdateCubeMapAndScreenBuffer ();
				}

				if (Singularity.Instance.lensingStacking)
				{
					StackedLensingRenderer.RenderForThisFrame(parentSingularity.singularityMeshRenderer, singularityMaterial); //TODO, move this to dedicated thingy, as this isn't good
				}
			}
			else
			{
				//if we will render on cubemap camera -> disable screenBuffer use
				singularityMaterial.SetFloat(screenBufferProperty,0f);
			}
		}

		public void OnRenderObject()
		{
			if (Camera.current != ScaledCamera.Instance.cam)
			{
				singularityMaterial.SetFloat(screenBufferProperty,1f); //if we finished rendering on cubemap Camera -> re-enable screen buffer use for main camera
			}
		}
			
		public void UpdateCubeMapAndScreenBuffer ()
		{
			//limit to 1 cubeMap update per frame
			if (!cubeMapUpdated)
			{
				Singularity.Instance.scaledSceneBufferRenderer.RenderSceneIfNeeded();	//This probably shouldn't be called by SingularityCenteredCubemap but by SingularityObject itself?

				cubemapFaceToUpdate = (cubemapFaceToUpdate+1) % 6; //update one face per cubemap per frame, later change it to only 1 face of ONE cubemap per frame
				int updateMask = 1 << cubemapFaceToUpdate;
				//int updateMask = (TimeWarp.CurrentRate > 4) ? 63 : (1 << cubemapFaceToUpdate);						

				//ScaledCamera.Instance.galaxyCamera.RenderToCubemap (objectCubemap); // broken
				parentSingularity.DisableForSceneOrCubemap();
				objectCamera.RenderToCubemap (singularityCubemap, updateMask);
				parentSingularity.ReEnable();

				cubeMapUpdated = true;

				parentSingularity.UpdateTargetWormhole();
			}
		}

		public void OnDestroy()
		{
			Utils.LogInfo ("Singularity cubemap ondestroy");
			if (!ReferenceEquals (singularityCubemap, null))
			{
				singularityCubemap.Release();
			}
		}
	}
}

