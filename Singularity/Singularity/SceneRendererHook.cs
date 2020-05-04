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
}

