//pipelinedefine
#define H_URP

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HTraceSSGI.Scripts.PipelelinesConfigurator
{
	public static class ConfiguratorUtils
	{
		public static string GetFullHdrpPath()
		{
			string targetPathStandart = Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache");
			string hdrpPathStandart = Directory.GetDirectories(targetPathStandart)
				.FirstOrDefault(name => name.Contains("high-definition") && !name.Contains("config"));
			string targetPathCustom = Path.Combine(Directory.GetCurrentDirectory(), "Packages");
			string hdrpPathCustom = Directory.GetDirectories(targetPathCustom)
				.FirstOrDefault(name => name.Contains("high-definition") && !name.Contains("config"));

			if (string.IsNullOrEmpty(hdrpPathStandart) && string.IsNullOrEmpty(hdrpPathCustom))
			{
				Debug.LogError($"HDRP path was not found there: {hdrpPathStandart}\n and there:\n{hdrpPathCustom}");
			}
			
			return string.IsNullOrEmpty(hdrpPathStandart) ? hdrpPathCustom : hdrpPathStandart;
		}

		public static string GetHTraceFolderPath()
		{
			//string filePath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(this));
			string filePath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
			string htraceFolder = Directory.GetParent(Directory.GetParent(Path.GetDirectoryName(filePath)).FullName).FullName;
			return htraceFolder;
		}

		public static string GetUnityAndHdrpVersion()
		{
			return $"Unity {Application.unityVersion}, HDRP version {GetHdrpVersion()}";
		}

		public static string GetHdrpVersion()
		{
			string fullHdrpPath = GetFullHdrpPath();
			string pathPackageJson = Path.Combine(fullHdrpPath, "package.json");
			string[] packageJson = File.ReadAllLines(pathPackageJson);
			string hdrpVersion = string.Empty;
			foreach (string line in packageJson)
			{
				if (line.Contains("version"))
				{
					hdrpVersion = line.Replace("version", "").Replace(" ", "").Replace(":", "").Replace(",", "").Replace("\"", "");
					break;
				}
			}

			return hdrpVersion;
		}

		public static int GetMajorHdrpVersion()
		{
			string hdrpVersion = GetHdrpVersion();
			string[] split = hdrpVersion.Split('.');
			return Convert.ToInt32(split[0]);
		}

		public static int GetMinorHdrpVersion()
		{
			string hdrpVersion = GetHdrpVersion();
			string[] split = hdrpVersion.Split('.');
			return Convert.ToInt32(split[1]);
		}

		public static int GetPatchHdrpVersion()
		{
			string hdrpVersion = GetHdrpVersion();
			string[] split = hdrpVersion.Split('.');
			return Convert.ToInt32(split[2]);
		}
	} 
}
#endif
