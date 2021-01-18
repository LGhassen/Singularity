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
		
		// Borrowed from smokeScreen
		public static string WriteRootNode(ConfigNode node)
		{
			StringBuilder builder = new StringBuilder();
			
			//print("node.values.Count " + node.values.Count + " node.nodes.Count " + node.nodes.Count);
			for (int i = 0; i < node.values.Count; i++)
			{
				ConfigNode.Value item = node.values[i];
				//builder.AppendLine(string.Concat(item.name, " = ", item.value));
				builder.Append(string.Concat(item.name, " = ", item.value, "\n")); //not sure why but the default newLine character messes up the parsing
			}
			for (int j = 0; j < node.nodes.Count; j++)
			{
				WriteNodeString(node.nodes[j], ref builder, string.Empty);
			}
			return builder.ToString();
		}
		
		public static void WriteNodeString(ConfigNode node, ref StringBuilder builder, string indent)
		{
			builder.AppendLine(string.Concat(indent, node.name));
			builder.AppendLine(string.Concat(indent, "{"));
			string str = string.Concat(indent, "  ");
			for (int i = 0; i < node.values.Count; i++)
			{
				ConfigNode.Value item = node.values[i];
				//builder.AppendLine(string.Concat(str, item.name, " = ", item.value));
				builder.Append(string.Concat(str, item.name, " = ", item.value, "\n"));
			}
			for (int j = 0; j < node.nodes.Count; j++)
			{
				WriteNodeString(node, ref builder, str);
			}
			//builder.AppendLine(string.Concat(indent, "}"));
			builder.AppendLine(string.Concat(indent, "}", "\n"));
		}
		
		public static char[] delimiters = new char[4]
		{
			' ',',',';','\t'
		};

		public static Texture2D LoadDDSTexture(byte[] data, string name)
		{
			Texture2D texture=null;

			byte ddsSizeCheck = data[4];
			if (ddsSizeCheck != 124)
			{
				LogError("This DDS texture is invalid - Unable to read the size check value from the header.");
				return texture;
			}
			
			
			int height = data[13] * 256 + data[12];
			int width = data[17] * 256 + data[16];
			
			int DDS_HEADER_SIZE = 128;
			byte[] dxtBytes = new byte[data.Length - DDS_HEADER_SIZE];
			Buffer.BlockCopy(data, DDS_HEADER_SIZE, dxtBytes, 0, data.Length - DDS_HEADER_SIZE);
			int mipMapCount = (data[28]) | (data[29] << 8) | (data[30] << 16) | (data[31] << 24);
			
			TextureFormat format = TextureFormat.YUY2; //just an invalid type
			if (data[84] == 'D')
			{
				if (data[87] == 49) //Also char '1'
				{
					format = TextureFormat.DXT1;
				}
				else if (data[87] == 53)    //Also char '5'
				{
					format = TextureFormat.DXT5;
				}
			}

			if (format == TextureFormat.YUY2)
			{
				LogError("Format of texture "+name+" unidentified");
				return texture;
			}

			if (mipMapCount == 1)
			{
				texture = new Texture2D(width, height, format, false);
			}
			else
			{
				texture = new Texture2D(width, height, format, true);
			}
			try
			{
				texture.LoadRawTextureData(dxtBytes);
			}
			catch
			{
				LogError("Texture "+name+" couldn't be loaded");
				return texture;
			}
			texture.Apply();

			LogInfo ("Loaded texture " + name + " " + width.ToString () + "x" + height.ToString () + " mip count: " + mipMapCount.ToString ());
			
			return texture;
		}
	}
}