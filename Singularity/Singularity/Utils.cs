using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Reflection;

namespace Singularity
{
	public static class Utils
	{
		public static BindingFlags reflectionFlags =  BindingFlags.FlattenHierarchy |  BindingFlags.NonPublic | BindingFlags.Public | 
			BindingFlags.Instance | BindingFlags.Static;

		public static void LogDebug(string log)
		{
			Debug.Log ("[Singularity][Debug] " + log);
		}
		
		public static void LogInfo(string log)
		{
			Debug.Log ("[Singularity][Info] " + log);
		}
		
		public static void LogError(string log)
		{
			Debug.LogError("[Singularity][Error] " + log);
		}
		
		public static Dictionary<string, Shader> LoadAssetBundle(string path)
		{
			string shaderspath;
			Dictionary<string, Shader> LoadedShaders = new Dictionary<string, Shader>();

			if (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith ("OpenGL"))
				shaderspath = path+"/shaders/singularityshaders-linux";   //fixes openGL on windows
			else
				if (Application.platform == RuntimePlatform.WindowsPlayer)
					shaderspath = path + "/shaders/singularityshaders-windows";
			else if (Application.platform == RuntimePlatform.LinuxPlayer)
				shaderspath = path+"/shaders/singularityshaders-linux";
			else
				shaderspath = path+"/shaders/singularityshaders-macosx";
			
			LoadedShaders.Clear ();
			
			using (WWW www = new WWW("file://"+shaderspath))
			{
				AssetBundle bundle = www.assetBundle;
				Shader[] shaders = bundle.LoadAllAssets<Shader>();
				
				foreach (Shader shader in shaders)
				{
					LogDebug (shader.name+" loaded. Supported?"+shader.isSupported.ToString());
					LoadedShaders.Add(shader.name, shader);
				}
				
				bundle.Unload(false); // unload the raw asset bundle
				www.Dispose();
			}
			
			return LoadedShaders;
		}
		
//		// Borrowed from smokeScreen
//		public static string WriteRootNode(ConfigNode node)
//		{
//			StringBuilder builder = new StringBuilder();
//			
//			//print("node.values.Count " + node.values.Count + " node.nodes.Count " + node.nodes.Count);
//			for (int i = 0; i < node.values.Count; i++)
//			{
//				ConfigNode.Value item = node.values[i];
//				builder.AppendLine(string.Concat(item.name, " = ", item.value));
//			}
//			for (int j = 0; j < node.nodes.Count; j++)
//			{
//				WriteNodeString(node.nodes[j], ref builder, string.Empty);
//			}
//			return builder.ToString();
//		}
//		
//		public static void WriteNodeString(ConfigNode node, ref StringBuilder builder, string indent)
//		{
//			builder.AppendLine(string.Concat(indent, node.name));
//			builder.AppendLine(string.Concat(indent, "{"));
//			string str = string.Concat(indent, "  ");
//			for (int i = 0; i < node.values.Count; i++)
//			{
//				ConfigNode.Value item = node.values[i];
//				builder.AppendLine(string.Concat(str, item.name, " = ", item.value));
//			}
//			for (int j = 0; j < node.nodes.Count; j++)
//			{
//				WriteNodeString(node, ref builder, str);
//			}
//			builder.AppendLine(string.Concat(indent, "}"));
//		}
//		
//		public static char[] delimiters = new char[4]
//		{
//			' ',',',';','\t'
//		};
	}
}