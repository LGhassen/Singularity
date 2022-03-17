// Allows for rendering multiple singularities such that their distorsion/lensing effects stack
// Only used on the scaledSpace camera, can be re-done for other cameras if needed but be careful not apply on cubemap cameras

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Singularity
{
	public class StackedLensingRenderer : MonoBehaviour
	{
		static StackedLensingRenderer lensingRenderer;
		private Material copyCameraDepthMaterial, lensingCopyMaterial;

		public static void Create()
		{
			lensingRenderer = (StackedLensingRenderer) ScaledCamera.Instance.cam.gameObject.AddComponent(typeof(StackedLensingRenderer));
			lensingRenderer.copyCameraDepthMaterial = new Material (Singularity.LoadedShaders["Singularity/CopyCameraDepth"]);
			lensingRenderer.lensingCopyMaterial = new Material(Singularity.LoadedShaders ["Singularity/IntermediateLensingCopy"]);
		}
		
		// Pairs of singularity meshrenderers and their materials, sorted by distance, for rendering farthest to closest        
		SortedList<float, Tuple<Renderer, Material>> renderersAdded = new SortedList<float, Tuple<Renderer, Material>>();

		bool renderingEnabled = false;
		private List<CommandBuffer> commandBuffersAdded = new List<CommandBuffer>();

		public static void RenderForThisFrame(MeshRenderer mr, Material mat)
		{
			lensingRenderer.renderersAdded.Add((mr.gameObject.transform.position - ScaledCamera.Instance.cam.transform.position).magnitude, new Tuple<Renderer, Material>(mr, mat));
			lensingRenderer.renderingEnabled = true;
		}

		void OnPreRender()
		{
			if (renderingEnabled)
			{
				//start by copying scene depth to our target for depth testing, and to stackingDepthBuffer as input fot black hole shader
				{
					CommandBuffer copyCB = new CommandBuffer();
					
					//blit by itself draws a quad with zwite off, use a material with zwrite on and which outputs to depth
					//source: support.unity.com/hc/en-us/articles/115000229323-Graphics-Blit-does-not-copy-RenderTexture-depth
					copyCB.Blit (null, Singularity.Instance.screenBufferFlip.depthBuffer, copyCameraDepthMaterial, 0);
					copyCB.Blit (null, Singularity.Instance.stackingDepthBuffer, copyCameraDepthMaterial, 0); //TODO: attempt to remove this step and do the first rendering operation using BuiltinRenderTextureType.Depth to get at the camera's builtin depth
																										      //If it doesn't work consider replacing this with multitarget blit

					copyCB.SetGlobalTexture("singularityFinalStackedBuffer", Singularity.Instance.screenBufferFlop.colorBuffer); //Expose result buffer to copy shader

					ScaledCamera.Instance.cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, copyCB);
					commandBuffersAdded.Add(copyCB);
				}

				//sort singularities by decreasing distance to camera and render them farthest to closest
				int cnt = 0;

				foreach (var elt in renderersAdded.Reverse())
				{
					CommandBuffer renderCB = new CommandBuffer();

					renderCB.SetGlobalTexture("SingularityScreenBuffer", Singularity.Instance.screenBufferFlip.colorBuffer);
					renderCB.SetGlobalTexture("SingularityDepthTexture", Singularity.Instance.stackingDepthBuffer);

					renderCB.SetRenderTarget(Singularity.Instance.screenBufferFlop, Singularity.Instance.screenBufferFlip.depthBuffer);

					renderCB.DrawRenderer(elt.Value.Item1, elt.Value.Item2);	//Draw singularity with real shader

					//If there are other singularities, copy back Color and depth to use as input for next singularity
					if (cnt < renderersAdded.Count-1)	
					{
						renderCB.SetRenderTarget(Singularity.Instance.screenBufferFlip.colorBuffer, Singularity.Instance.stackingDepthBuffer);
						renderCB.DrawRenderer(elt.Value.Item1, lensingCopyMaterial);
					}

					ScaledCamera.Instance.cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, renderCB);
					commandBuffersAdded.Add(renderCB);

					cnt++;
				}

				Singularity.Instance.SwitchSingularitiesToCopyMode();
			}
		}
		
		void OnPostRender()
		{
			if (renderingEnabled)
			{
				foreach (var cb in commandBuffersAdded)
				{
					ScaledCamera.Instance.cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, cb);
				}
				commandBuffersAdded.Clear();
				renderersAdded.Clear();

				renderingEnabled = false;

				Singularity.Instance.SwitchSingularitiesToNormalMode();
			}
		}
		
		public void OnDestroy()
		{
			OnPostRender();
		}
	}
}

