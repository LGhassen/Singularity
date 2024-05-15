using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections;

namespace singularityShaders
{

	public class CreateAssetBundles
	{
		[MenuItem ("Assets/Build AssetBundles")]
		static void BuildAllAssetBundles ()
		{
			// Put the bundles in a folder called "AssetBundles"
			var outDir = "Assets/AssetBundles";
			//var outDir = "C:/Steam/steamapps/common/Kerbal Space Program/GameData/Singularity/shaders";
			var outDir2 = "C:/Steam/steamapps/common/Kerbal Space Program 1.8.1/GameData/Singularity/shaders";

			if (!Directory.Exists (outDir))
				Directory.CreateDirectory (outDir);

			if (!Directory.Exists (outDir2))
				Directory.CreateDirectory (outDir2);

			var opts = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle;

			BuildTarget[] platforms = { BuildTarget.StandaloneWindows, BuildTarget.StandaloneOSX, BuildTarget.StandaloneLinux64 };
			string[] platformExts = { "-windows", "-macosx", "-linux" };
			for (var i = 0; i < platforms.Length; ++i)
			{
				BuildPipeline.BuildAssetBundles(outDir, opts, platforms[i]);
				var outFile  = outDir  + "/singularityshaders" + platformExts[i];
				var outFile2 = outDir2 + "/singularityshaders" + platformExts[i];
				FileUtil.ReplaceFile(outDir  + "/singularityshaders", outFile);
				FileUtil.ReplaceFile(outDir  + "/singularityshaders", outFile2);
			}

			//cleanup
			foreach (string file in Directory.GetFiles(outDir, "*.*").Where(item => (item.EndsWith(".meta") || item.EndsWith(".manifest"))))
			{
				File.Delete(file);
			}
			File.Delete (outDir + "/CompiledAssetBundles");
			File.Delete(outDir+"/singularityshaders");
			File.Delete(outDir+"/shaders");
		}
	}

}