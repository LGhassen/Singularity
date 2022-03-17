using UnityEngine;

namespace Singularity
{
	public class SceneRendererHook : MonoBehaviour
	{
		public ScaledSceneBufferRenderer sceneRenderer;

		public SceneRendererHook ()
		{
		}

		public void OnPostRender()
		{
			sceneRenderer.resetForNewFrame ();
		}
	}

	//For compatibility with scatterer's disableAmbientLight
	[DefaultExecutionOrder(100)]
	public class AmbientLightHook : MonoBehaviour
	{
		public ScaledSceneBufferRenderer sceneRenderer;
		
		public AmbientLightHook ()
		{
		}
		
		public void OnPreRender()
		{
			sceneRenderer.scaledAmbientLight = RenderSettings.ambientLight;
		}
	}
}

