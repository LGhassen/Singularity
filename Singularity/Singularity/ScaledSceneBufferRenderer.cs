// Re-renders the scaled scene when needed, for use with the singularity Objects
// For blending and draw order reasons (mostly with EVE+scatterer), and limitations on where we can place commandbuffers in forward rendering
// we have to re-render the scaledSpace scene. This is still faster than a grabpass though due to how empty the scaledSpace scene is (we don't re-render the galaxy background)

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
	public class ScaledSceneBufferRenderer : MonoBehaviour
	{
		Camera sceneCamera;
		GameObject sceneCameraGO;

		bool sceneRendered = false;
		SceneRendererHook renderHook;

		// TODO: should it handle other cameras?
		public ScaledSceneBufferRenderer ()
		{

		}

		public void Init()
		{
			sceneCameraGO = new GameObject ("Singularity scene camera");

			sceneCamera = sceneCameraGO.AddComponent<Camera> ();
			sceneCamera.enabled = false;
			
			sceneCamera.transform.position = ScaledCamera.Instance.cam.transform.position;
			sceneCamera.transform.parent = ScaledCamera.Instance.cam.transform;
			
			sceneCamera.targetTexture = Singularity.Instance.screenBufferFlip;

			renderHook = ScaledCamera.Instance.galaxyCamera.gameObject.AddComponent<SceneRendererHook> ();
			renderHook.sceneRenderer = this;
		}

		public void RenderSceneIfNeeded()
		{
			if (!sceneRendered)
			{
				//Enable depthtextureMode on the scaled camera, because sometimes the stock game disables it randomly
				ScaledCamera.Instance.cam.depthTextureMode = ScaledCamera.Instance.cam.depthTextureMode | DepthTextureMode.Depth;

				sceneCamera.CopyFrom(ScaledCamera.Instance.cam);
				sceneCamera.depthTextureMode = DepthTextureMode.None;				
				sceneCamera.clearFlags = CameraClearFlags.Depth; 		//No need to clear color since we use depth to pick what we use anyway
				sceneCamera.enabled = false;
				sceneCamera.targetTexture = Singularity.Instance.screenBufferFlip;

				Singularity.Instance.DisableSingularitiesForSceneBuffer();
				sceneCamera.Render(); 									//this seems to pre-render a depth texture anyway for some reason, maybe shadows?
				Singularity.Instance.ReEnableSingularities();

				sceneRendered = true;
			}
		}

		public void resetForNewFrame()
		{
			sceneRendered = false;
		}

		public void Cleanup()
		{
			Component.Destroy (renderHook);
		}
	}
}

