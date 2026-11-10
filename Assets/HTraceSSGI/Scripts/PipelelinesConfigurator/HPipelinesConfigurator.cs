#if UNITY_EDITOR
using System.IO;
using System.Linq;
using HTraceSSGI.Scripts.Globals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTraceSSGI.Scripts.PipelelinesConfigurator
{
	internal class HPipelinesConfigurator
	{
		public static void UpdateDefines(HRenderPipeline hRenderPipeline)
		{
			string         firstLine      = "//pipelinedefine";

			var    hTraceFolderPath    = ConfiguratorUtils.GetHTraceFolderPath();
			string supportedExtensions = "*.cs,*.hlsl,*.compute,*.glsl,*.shader";
			var    filesPaths          = Directory.GetFiles(hTraceFolderPath, "*.*", SearchOption.AllDirectories).Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));
			
			foreach (string filePath in filesPaths)
			{
				string[] allLinesFile = File.ReadAllLines(filePath);

				if (allLinesFile[0] == firstLine)
				{
					switch (hRenderPipeline)
					{
						case HRenderPipeline.None:
							allLinesFile[1] = "#define NONE";
							break;
						case HRenderPipeline.BIRP:
							if (allLinesFile[1] == "#define H_BIRP")
								continue;
							allLinesFile[1] = "#define H_BIRP";
							break;
						case HRenderPipeline.URP:
							if (allLinesFile[1] == "#define H_URP")
								continue;
							allLinesFile[1] = "#define H_URP";
							break;
						case HRenderPipeline.HDRP:
							if (allLinesFile[1] == "#define H_HDRP")
								continue;
							allLinesFile[1] = "#define H_HDRP";
							break;
					}
				}
				else
				{
					continue;
				}

				File.WriteAllLines(filePath, allLinesFile);
			}
			
			Debug.Log($"Defines updated successfully {hRenderPipeline}!");
		}
		
		public static void AlwaysIncludedShaders()
		{
			AddShaderToGraphicsSettings("Hidden/HTraceSSGI/MotionVectorsURP");
			AddShaderToGraphicsSettings("Hidden/HTraceSSGI/ColorComposeURP");
		}
		
		public static void AddShaderToGraphicsSettings(string shaderName)
		{
			var shader = Shader.Find(shaderName);
			if (shader == null)
				return;

			var  graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
			var  serializedObject = new SerializedObject(graphicsSettings);
			var  arrayProp        = serializedObject.FindProperty("m_AlwaysIncludedShaders");
			bool hasShader        = false;
			for (int i = 0; i < arrayProp.arraySize; ++i)
			{
				var arrayElem = arrayProp.GetArrayElementAtIndex(i);
				if (shader == arrayElem.objectReferenceValue)
				{
					hasShader = true;
					break;
				}
			}

			if (!hasShader)
			{
				int arrayIndex = arrayProp.arraySize;
				arrayProp.InsertArrayElementAtIndex(arrayIndex);
				var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
				arrayElem.objectReferenceValue = shader;

				serializedObject.ApplyModifiedProperties();

				AssetDatabase.SaveAssets();
			}
		}
		
	}
}
#endif
