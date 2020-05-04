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
		int screenBufferProperty;
		int cubemapFaceToUpdate = 0;


		public SingularityCenteredCubeMap ()
		{

		}

		void Awake()
		{
			objectCamera = gameObject.AddComponent<Camera> ();
			objectCamera.cullingMask = (1 << 9) | (1 << 10);
			objectCamera.renderingPath = ScaledCamera.Instance.cam.renderingPath;
			objectCamera.depthTextureMode = DepthTextureMode.None;
			objectCamera.farClipPlane = ScaledCamera.Instance.cam.farClipPlane;
			objectCamera.nearClipPlane = ScaledCamera.Instance.cam.nearClipPlane;

			objectCamera.clearFlags = CameraClearFlags.Color;
			objectCamera.backgroundColor = Color.black;
			objectCamera.enabled = false;

			objectCamera.transform.position = gameObject.transform.position;
			objectCamera.transform.parent = gameObject.transform;

			singularityCubemap = new RenderTexture(2048, 2048, 16, RenderTextureFormat.ARGB32, 9);
			singularityCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
			singularityCubemap.autoGenerateMips = true;
			singularityCubemap.filterMode = FilterMode.Trilinear;
			singularityCubemap.Create ();
			StartCoroutine (SetMaterialTexture ());

			objectCamera.targetTexture = null;

			screenBufferProperty = Shader.PropertyToID("useScreenBuffer");
		}
		
		IEnumerator SetMaterialTexture()
		{
			yield return new WaitForFixedUpdate ();
			singularityMaterial.SetTexture ("objectCubeMap", singularityCubemap);
		}

		public void Update()
		{
			cubeMapUpdated = false;
		}

		public void OnWillRenderObject()
		{

			if (Camera.current == ScaledCamera.Instance.cam)
			{
				singularityMaterial.SetFloat(screenBufferProperty,1f); //use screenBuffer only on scaledSpace camera
				UpdateCubeMap (); //update only when called by scaledCamera (or in future by wormhole), to avoid singularities calling it on each other and being disabled in each other's cubemaps
			}

			//works on first black hole but breaks second one,and black holes always hide each other when using screenBuffer mode which is a shame
			//add switch for black holes which can show other black holes? how do I handle this? Like for the case of murph and the wormhole? what to do then
//			else
//			{
//				singularityMaterial.SetFloat(screenBufferProperty,0f);
//			}
		}
		
		public void UpdateCubeMap ()
		{
			//limit to 1 cubeMap update per frame
			if (!cubeMapUpdated)
			{
				Singularity.Instance.scaledSceneBufferRenderer.RenderSceneIfNeeded();

				cubemapFaceToUpdate = (cubemapFaceToUpdate+1) % 6; //update one face per cubemap per frame, later change it to only 1 face of ONE cubemap per frame
				int updateMask = 1 << cubemapFaceToUpdate;	
				//int updateMask = (TimeWarp.CurrentRate > 4) ? 63 : (1 << cubemapFaceToUpdate);						

				//ScaledCamera.Instance.galaxyCamera.RenderToCubemap (objectCubemap); // broken
				parentSingularity.DisableForSceneOrCubemap();
				objectCamera.RenderToCubemap (singularityCubemap, updateMask);
				parentSingularity.ReEnable();

				//TODO: here notify target wormhole to update

				cubeMapUpdated = true;
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

