// Allows for rendering multiple singularities such that their distorsion/lensing effects stack
// Only used on the scaledSpace camera, can be re-done for other cameras if needed but be careful not apply on cubemap cameras

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Singularity
{
    public struct RendererMaterialPair
    {
        public readonly Renderer Renderer;
        public readonly Material Material;
        public RendererMaterialPair(Renderer renderer, Material material) { Renderer = renderer; Material = material; }
    }

    public class StackedLensingRenderer : MonoBehaviour
    {
        static StackedLensingRenderer lensingRenderer;
        private Material copyCameraDepthMaterial, lensingCopyMaterial;

        static readonly int singularityFinalStackedBufferProperty = Shader.PropertyToID("singularityFinalStackedBuffer");
        static readonly int singularityScreenBufferProperty = Shader.PropertyToID("SingularityScreenBuffer");
        static readonly int singularityDepthTextureProperty = Shader.PropertyToID("SingularityDepthTexture");

        public static void Create()
        {
            lensingRenderer = (StackedLensingRenderer)ScaledCamera.Instance.cam.gameObject.AddComponent(typeof(StackedLensingRenderer));
            lensingRenderer.copyCameraDepthMaterial = new Material(Singularity.LoadedShaders["Singularity/CopyCameraDepth"]);
            lensingRenderer.lensingCopyMaterial = new Material(Singularity.LoadedShaders["Singularity/IntermediateLensingCopy"]);
        }

        // Pairs of singularity meshrenderers and their materials, sorted by distance, for rendering farthest to closest
        SortedList<float, RendererMaterialPair> renderersAdded = new SortedList<float, RendererMaterialPair>();

        bool renderingEnabled = false;

        // Pool of reusable CommandBuffers: index 0 is the depth-copy CB, indices 1.. are per-singularity render CBs.
        // Grown on demand and cleared/repopulated each frame to avoid per-frame allocations.
        private CommandBuffer depthCopyCB;
        private readonly List<CommandBuffer> renderCBPool = new List<CommandBuffer>();
        private int activeRenderCBCount = 0;

        public static void RenderForThisFrame(MeshRenderer mr, Material mat)
        {
            lensingRenderer.renderersAdded.Add((mr.gameObject.transform.position - ScaledCamera.Instance.cam.transform.position).magnitude, new RendererMaterialPair(mr, mat));
            lensingRenderer.renderingEnabled = true;
        }

        void OnPreRender()
        {
            if (renderingEnabled)
            {
                //start by copying scene depth to our target for depth testing, and to stackingDepthBuffer as input fot black hole shader
                {
                    if (depthCopyCB == null)
                        depthCopyCB = new CommandBuffer();
                    else
                        depthCopyCB.Clear();

                    //blit by itself draws a quad with zwite off, use a material with zwrite on and which outputs to depth
                    //source: support.unity.com/hc/en-us/articles/115000229323-Graphics-Blit-does-not-copy-RenderTexture-depth
                    depthCopyCB.Blit(null, Singularity.Instance.screenBufferFlip.depthBuffer, copyCameraDepthMaterial, 0);
                    depthCopyCB.Blit(null, Singularity.Instance.stackingDepthBuffer, copyCameraDepthMaterial, 0); //TODO: attempt to remove this step and do the first rendering operation using BuiltinRenderTextureType.Depth to get at the camera's builtin depth
                                                                                                                 //If it doesn't work consider replacing this with multitarget blit

                    depthCopyCB.SetGlobalTexture(singularityFinalStackedBufferProperty, Singularity.Instance.screenBufferFlop.colorBuffer); //Expose result buffer to copy shader

                    ScaledCamera.Instance.cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, depthCopyCB);
                }

                //sort singularities by decreasing distance to camera and render them farthest to closest
                int cnt = 0;
                int rendererCount = renderersAdded.Count;

                foreach (var elt in renderersAdded.Reverse())
                {
                    CommandBuffer renderCB;
                    if (cnt < renderCBPool.Count)
                    {
                        renderCB = renderCBPool[cnt];
                        renderCB.Clear();
                    }
                    else
                    {
                        renderCB = new CommandBuffer();
                        renderCBPool.Add(renderCB);
                    }

                    renderCB.SetGlobalTexture(singularityScreenBufferProperty, Singularity.Instance.screenBufferFlip.colorBuffer);
                    renderCB.SetGlobalTexture(singularityDepthTextureProperty, Singularity.Instance.stackingDepthBuffer);

                    renderCB.SetRenderTarget(Singularity.Instance.screenBufferFlop, Singularity.Instance.screenBufferFlip.depthBuffer);

                    renderCB.DrawRenderer(elt.Value.Renderer, elt.Value.Material);    //Draw singularity with real shader

                    //If there are other singularities, copy back Color and depth to use as input for next singularity
                    if (cnt < rendererCount - 1)
                    {
                        renderCB.SetRenderTarget(Singularity.Instance.screenBufferFlip.colorBuffer, Singularity.Instance.stackingDepthBuffer);
                        renderCB.DrawRenderer(elt.Value.Renderer, lensingCopyMaterial);
                    }

                    ScaledCamera.Instance.cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, renderCB);

                    cnt++;
                }

                activeRenderCBCount = cnt;

                Singularity.Instance.SwitchSingularitiesToCopyMode();
            }
        }

        void OnPostRender()
        {
            if (renderingEnabled)
            {
                if (depthCopyCB != null)
                    ScaledCamera.Instance.cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, depthCopyCB);

                for (int i = 0; i < activeRenderCBCount; i++)
                {
                    ScaledCamera.Instance.cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, renderCBPool[i]);
                }
                activeRenderCBCount = 0;
                renderersAdded.Clear();

                renderingEnabled = false;

                Singularity.Instance.SwitchSingularitiesToNormalMode();
            }
        }

        public void OnDestroy()
        {
            OnPostRender();

            if (depthCopyCB != null)
            {
                depthCopyCB.Release();
                depthCopyCB = null;
            }
            for (int i = 0; i < renderCBPool.Count; i++)
            {
                renderCBPool[i].Release();
            }
            renderCBPool.Clear();
        }
    }
}

