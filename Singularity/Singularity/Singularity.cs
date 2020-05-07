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
	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
	public class Singularity : MonoBehaviour
	{
		private static Singularity instance;
		public static Singularity Instance {get {return instance;}}
		
		private static Dictionary<string, Shader> LoadedShadersDictionary = new Dictionary<string, Shader>();				
		public static Dictionary<string, Shader> LoadedShaders {get{return LoadedShadersDictionary;}}
		
		private string path,gameDataPath;		
		public string GameDataPath {get{return gameDataPath;}}

		public Cubemap galaxyCubemap;
		public MaterialPropertyBlock galaxyCubeControlMPB;

		public List<SingularityObject> loadedObjects = new List<SingularityObject>();
		
		public RenderTexture screenBuffer;

		public ScaledSceneBufferRenderer scaledSceneBufferRenderer;

		double initialUniversalTime;

		public Singularity ()
		{
			if (instance == null)
			{
				instance = this;
				Utils.LogInfo("Instance created");
			}
			else
			{
				//destroy any duplicate instances that may be created by a duplicate install
				Utils.LogError("Destroying duplicate instance, check your install for duplicate mod folders");
				UnityEngine.Object.Destroy (this);
			}
		}

		void Awake()
		{
			string codeBase = Assembly.GetExecutingAssembly ().CodeBase;
			UriBuilder uri = new UriBuilder (codeBase);
			
			path = Uri.UnescapeDataString (uri.Path);
			path = Path.GetDirectoryName (path);
			
			gameDataPath = KSPUtil.ApplicationRootPath + "GameData/";	
			
			LoadedShadersDictionary = Utils.LoadAssetBundle (path);
			StartCoroutine (DelayedInit ());
		}

		// Delay for the galaxy cubemap to be set correctly
		IEnumerator DelayedInit()
		{
			for (int i=0; i<5; i++)
			{
				yield return new WaitForFixedUpdate ();
			}
			
			Init();
		}

		void Init()
		{
			SetupCubemap ();

			screenBuffer = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, 9);
			screenBuffer.filterMode = FilterMode.Bilinear;
			screenBuffer.Create ();

			scaledSceneBufferRenderer = new ScaledSceneBufferRenderer ();
			scaledSceneBufferRenderer.Init ();

			initialUniversalTime = Planetarium.GetUniversalTime ();

			LoadConfigs ();
		}

		void SetupCubemap()
		{
			try
			{
				galaxyCubeControlMPB = typeof(GalaxyCubeControl).GetField ("mpb", Utils.reflectionFlags).GetValue (GalaxyCubeControl.Instance) as MaterialPropertyBlock;
				UnityEngine.Renderer[] cubeRenderers = typeof(GalaxyCubeControl).GetField ("cubeRenderers", Utils.reflectionFlags).GetValue (GalaxyCubeControl.Instance) as UnityEngine.Renderer[];				
				Component galaxyCubeControlComponent = (Component) GalaxyCubeControl.Instance;
				
				if (!ReferenceEquals (galaxyCubeControlMPB, null) && !ReferenceEquals(cubeRenderers,null))
				{
					// Disable cubemap dimming before we capture it
					galaxyCubeControlMPB.SetColor(PropertyIDs._Color, Color.white);					
					for (int i = 0; i < cubeRenderers.Length; i++)
					{
						cubeRenderers[i].SetPropertyBlock(galaxyCubeControlMPB);	
					}					
					// De-rotate galaxy cubemap before we capture it, later adjust in shader for additional planetarium rotations
					GalaxyCubeControl.Instance.transform.rotation = GalaxyCubeControl.Instance.initRot;
				}
			}
			catch (Exception E)
			{
				Utils.LogError("Couldn't setup galaxy cubeMap correctly, Exception thrown: "+E.ToString());
			}
			
			galaxyCubemap = new Cubemap (2048, TextureFormat.ARGB32, 9); //add switch for controllable RES?
			ScaledCamera.Instance.galaxyCamera.RenderToCubemap (galaxyCubemap);
			Utils.LogInfo ("GalaxyCubemap initialized");
		}

		void LoadConfigs()
		{
			UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs ("Singularity");

			foreach (UrlDir.UrlConfig _url in configs)
			{
				ConfigNode[] configNodeArray = _url.config.GetNodes("Singularity_object");

				foreach(ConfigNode _cn in configNodeArray)
				{
					AddSingularityObject (_cn);
				}
			}
		}

		void AddSingularityObject (ConfigNode _cn)
		{
			if (_cn.HasValue ("name") && _cn.HasValue ("gravity"))
			{
				Transform scaledBodyTransform = ScaledSpace.Instance.transform.FindChild (_cn.GetValue ("name"));
				if (!ReferenceEquals (scaledBodyTransform, null))
				{
					try
					{
						SingularityObject singularityObject = scaledBodyTransform.gameObject.AddComponent<SingularityObject> ();
						loadedObjects.Add(singularityObject);
						singularityObject.Init (_cn);
					}
					catch (Exception e)
					{
						Utils.LogError ("Couldn't add singularity object to " + _cn.GetValue ("name") + ", Exception thrown: " + e.ToString ());
					}
				}
				else
				{
					Utils.LogError ("Unable to find " + _cn.GetValue ("name") + ", skipping ...");
				}
			}
		}

		void OnDestroy()
		{
			foreach (SingularityObject singularityObject in loadedObjects)
			{
				singularityObject.OnDestroy();
				UnityEngine.Object.Destroy(singularityObject);
			}

			screenBuffer.Release ();

			if (!ReferenceEquals(scaledSceneBufferRenderer,null))
			{
				scaledSceneBufferRenderer.Cleanup();
			}
		}

		public void DisableSingularitiesForSceneBuffer()
		{
			foreach (SingularityObject singularityObject in loadedObjects)
			{
				singularityObject.DisableForSceneOrCubemap();
			}
		}

		public void ReEnableSingularities()
		{
			foreach (SingularityObject singularityObject in loadedObjects)
			{
				singularityObject.ReEnable();
			}
		}

		// universal time is a large double, if we pass it to the shader directly as a float we lose enough precision that things look like they run at lower fps
		// pass the offset from an initial value as float
		public float getTime()
		{
			return (float)(Planetarium.GetUniversalTime () - initialUniversalTime);
		}
	}
}


