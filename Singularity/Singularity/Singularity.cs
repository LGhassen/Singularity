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


namespace Singularity
{
	[KSPAddon(KSPAddon.Startup.AllGameScenes, true)]
//	[KSPAddon(KSPAddon.Startup.MainMenu, true)] //make it reload every scene for the main menu shit?
//	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class Singularity : MonoBehaviour
	{
		private static Singularity instance;
		public static Singularity Instance {get {return instance;}}
		
		private static Dictionary<string, Shader> LoadedShadersDictionary = new Dictionary<string, Shader>();				
		public static Dictionary<string, Shader> LoadedShaders {get{return LoadedShadersDictionary;}}
		
		private string path,gameDataPath;		
		public string GameDataPath {get{return gameDataPath;}}
		
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

			LoadConfigs ();
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
	}
}


