// Re-renders the scaled scene when needed, for use with the singularity Objects
// For blending and draw order reasons (mostly with EVE+scatterer), and limitations on where we can place commandbuffers in forward rendering
// we have to re-render the scaledSpace scene. This is still faster than a grabpass though due to how empty the scaledSpace scene is (we don't re-render the galaxy background)

using UnityEngine;

namespace Singularity
{
	public class ScaledSceneBufferRenderer : MonoBehaviour
	{
		Camera sceneCamera;
		GameObject sceneCameraGO;

		bool sceneRendered = false;
		SceneRendererHook renderHook;
		AmbientLightHook ambientHook;

		public Color scaledAmbientLight, originalAmbientLight;

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


			renderHook = ScaledCamera.Instance.galaxyCamera.gameObject.AddOrGetComponent<SceneRendererHook> ();
			renderHook.sceneRenderer = this;

			ambientHook = ScaledCamera.Instance.cam.gameObject.AddComponent<AmbientLightHook> ();
			ambientHook.sceneRenderer = this;
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

				originalAmbientLight = RenderSettings.ambientLight;
				RenderSettings.ambientLight = scaledAmbientLight;

				sceneCamera.Render(); 									//this seems to pre-render a depth texture anyway for some reason, maybe shadows?

				RenderSettings.ambientLight = originalAmbientLight;

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
			Component.Destroy (ambientHook);
		}
	}
}

