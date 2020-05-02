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
		public MeshRenderer singularityMR;
		public Material singularityMaterial;

		public SingularityCenteredCubeMap ()
		{

		}

		void Awake()
		{
			objectCamera = gameObject.AddComponent<Camera> ();
			//objectCamera.cullingMask = (1 << 9) | (1 << 10);
			objectCamera.cullingMask = (1 << 10);
			objectCamera.renderingPath = ScaledCamera.Instance.cam.renderingPath;
			objectCamera.depthTextureMode = DepthTextureMode.None;
			objectCamera.farClipPlane = ScaledCamera.Instance.cam.farClipPlane;
			objectCamera.nearClipPlane = ScaledCamera.Instance.cam.nearClipPlane;

			objectCamera.clearFlags = CameraClearFlags.Color;
			objectCamera.backgroundColor = Color.black;
			//objectCamera.clearFlags = CameraClearFlags.Depth;
			objectCamera.enabled = false;

			objectCamera.transform.position = gameObject.transform.position;
			objectCamera.transform.parent = gameObject.transform;

			singularityCubemap = new RenderTexture(2048, 2048, 16, RenderTextureFormat.ARGB32, 9);
			singularityCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
			singularityCubemap.autoGenerateMips = true;
			//objectCubemap.antiAliasing = 1;
			singularityCubemap.Create ();


			objectCamera.targetTexture = null;
		}

		void Update()
		{
			objectCamera.enabled = false; //probably remove this
			singularityMR.enabled = true;
		}

		public void OnWillRenderObject()
		{
			//ScaledCamera.Instance.galaxyCamera.RenderToCubemap (objectCubemap); //absolutely broken

			//singularityMR.enabled = false; //it doesn't seem to like these? wtf? I'm suspecting some kind of recursive rendering
			objectCamera.RenderToCubemap (singularityCubemap);
			singularityMaterial.SetTexture ("objectCubeMap", singularityCubemap); //change this out of OnWillRenderObject?
			//singularityMR.enabled = true;
		}
		
	}
}

