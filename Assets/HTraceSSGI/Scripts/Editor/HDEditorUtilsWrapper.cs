#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HTraceSSGI.Scripts.Editor
{
	public class HDEditorUtilsWrapper
	{
        #region Public Methods

        public static bool EnsureFrameSetting(int frameSettingsField)
        {
            try
            {
                Type hdEditorUtilsType = GetHDEditorUtilsType();
                if (hdEditorUtilsType == null)
                {
                    return false;
                }

                Type frameSettingsFieldType = GetFrameSettingsFieldType();
                if (frameSettingsFieldType == null)
                {
                    return false;
                }

                object frameSettingsFieldEnum = Enum.ToObject(frameSettingsFieldType, frameSettingsField);

                MethodInfo ensureFrameSettingMethod = hdEditorUtilsType.GetMethod("EnsureFrameSetting",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new Type[] { frameSettingsFieldType },
                    null);

                if (ensureFrameSettingMethod == null)
                {
                    return false;
                }

                object result = ensureFrameSettingMethod.Invoke(null, new object[] { frameSettingsFieldEnum });
                return result != null && (bool)result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static void QualitySettingsHelpBox(string message, MessageType messageType, int expandableGroup, string propertyPath)
        {
            try
            {
                Type hdEditorUtilsType = GetHDEditorUtilsType();
                if (hdEditorUtilsType == null)
                {
                    return;
                }

                Type messageTypeEnum = typeof(MessageType);
                Type expandableGroupType = GetExpandableGroupType();

                if (expandableGroupType == null)
                {
                    return;
                }

                object messageTypeValue = Enum.ToObject(messageTypeEnum, messageType);
                object expandableGroupValue = Enum.ToObject(expandableGroupType, expandableGroup);

                MethodInfo qualitySettingsHelpBoxMethod = hdEditorUtilsType.GetMethod("QualitySettingsHelpBox",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new Type[] { typeof(string), messageTypeEnum, expandableGroupType, typeof(string) },
                    null);

                if (qualitySettingsHelpBoxMethod == null)
                {
                    return;
                }

                qualitySettingsHelpBoxMethod.Invoke(null, new object[] { message, messageTypeValue, expandableGroupValue, propertyPath });
            }
            catch (Exception ex)
            {
                //Debug.LogError($"Error calling HDEditorUtils.QualitySettingsHelpBox: {ex.Message}");
            }
        }

        public static void QualitySettingsHelpBoxWithSection(string message, MessageType messageType, int expandableGroup, int expandableSection, string propertyPath)
        {
            try
            {
                Type hdEditorUtilsType = GetHDEditorUtilsType();
                if (hdEditorUtilsType == null)
                {
                    Debug.LogError("Failed to find HDEditorUtils type");
                    return;
                }

                Type messageTypeEnum = typeof(MessageType);
                Type expandableGroupType = GetExpandableGroupType();

                if (expandableGroupType == null)
                {
                    Debug.LogError("Failed to find ExpandableGroup type");
                    return;
                }

                object messageTypeValue = Enum.ToObject(messageTypeEnum, messageType);
                object expandableGroupValue = Enum.ToObject(expandableGroupType, expandableGroup);

                MethodInfo[] methods = hdEditorUtilsType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo genericMethod = null;

                foreach (var method in methods)
                {
                    if (method.Name == "QualitySettingsHelpBox" &&
                        method.IsGenericMethodDefinition &&
                        method.GetParameters().Length == 5)
                    {
                        genericMethod = method;
                        break;
                    }
                }

                if (genericMethod == null)
                {
                    Debug.LogError("Failed to find generic QualitySettingsHelpBox method");
                    return;
                }

                Type expandableLightingType = GetExpandableLightingType();
                if (expandableLightingType == null)
                {
                    expandableLightingType = expandableGroupType;
                }

                MethodInfo concreteMethod = genericMethod.MakeGenericMethod(expandableLightingType);

                object expandableSectionValue = Enum.ToObject(expandableLightingType, expandableSection);

                concreteMethod.Invoke(null, new object[] { message, messageTypeValue, expandableGroupValue, expandableSectionValue, propertyPath });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calling HDEditorUtils.QualitySettingsHelpBox with section: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private static Type GetHDEditorUtilsType()
        {
            Type hdEditorUtilsType = Type.GetType("UnityEditor.Rendering.HighDefinition.HDEditorUtils, Unity.RenderPipelines.HighDefinition.Editor");

            if (hdEditorUtilsType == null)
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    hdEditorUtilsType = assembly.GetType("UnityEditor.Rendering.HighDefinition.HDEditorUtils");
                    if (hdEditorUtilsType != null) break;
                }
            }

            return hdEditorUtilsType;
        }

        private static Type GetFrameSettingsFieldType()
        {
            Type frameSettingsFieldType = Type.GetType("UnityEngine.Rendering.HighDefinition.FrameSettingsField, Unity.RenderPipelines.HighDefinition.Runtime");

            if (frameSettingsFieldType == null)
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    frameSettingsFieldType = assembly.GetType("UnityEngine.Rendering.HighDefinition.FrameSettingsField");
                    if (frameSettingsFieldType != null) break;
                }
            }

            return frameSettingsFieldType;
        }

        private static Type GetExpandableGroupType()
        {
            Type hdrpUIType = GetHDRPUIType();
            if (hdrpUIType == null) return null;

            return hdrpUIType.GetNestedType("ExpandableGroup", BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static Type GetExpandableLightingType()
        {
            Type hdrpUIType = GetHDRPUIType();
            if (hdrpUIType == null) return null;

            return hdrpUIType.GetNestedType("ExpandableLighting", BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static Type GetHDRPUIType()
        {
            Type hdrpUIType = Type.GetType("UnityEditor.Rendering.HighDefinition.HDRenderPipelineUI, Unity.RenderPipelines.HighDefinition.Editor");

            if (hdrpUIType == null)
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    hdrpUIType = assembly.GetType("UnityEditor.Rendering.HighDefinition.HDRenderPipelineUI");
                    if (hdrpUIType != null) break;
                }
            }

            return hdrpUIType;
        }

        public static int GetFrameSettingsFieldValue(string fieldName)
        {
            try
            {
                Type frameSettingsFieldType = GetFrameSettingsFieldType();
                if (frameSettingsFieldType == null)
                    return -1;

                object enumValue = Enum.Parse(frameSettingsFieldType, fieldName);
                return (int)enumValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when get FrameSettingsField.{fieldName}: {ex.Message}");
                return -1;
            }
        }

        // internal enum ExpandableGroup
        // {
        //     Rendering = 1 << 4,
        //     Lighting = 1 << 5,
        //     LightingTiers = 1 << 6,
        //     Material = 1 << 7,
        //     PostProcess = 1 << 8,
        //     PostProcessTiers = 1 << 9,
        //     XR = 1 << 10,
        //     VirtualTexturing = 1 << 11,
        //     Volumes = 1 << 12
        // }

        public static int GetExpandableGroupValue(string groupName)
        {
            try
            {
                Type expandableGroupType = GetExpandableGroupType();
                if (expandableGroupType == null)
                    return -1;

                object enumValue = Enum.Parse(expandableGroupType, groupName);
                return (int)enumValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when get ExpandableGroup.{groupName}: {ex.Message}");
                return -1;
            }
        }

        // internal enum ExpandableLighting
        // {
        //     Volumetric = 1 << 0,
        //     ProbeVolume = 1 << 1,
        //     Cookie = 1 << 2,
        //     Reflection = 1 << 3,
        //     Sky = 1 << 4,
        //     // Illegal index 1 << 5 since parent Lighting section index is using it
        //     LightLoop = 1 << 6,
        //     Shadow = 1 << 7
        // }

        public static int GetExpandableLightingValue(string lightingName)
        {
            try
            {
                Type expandableLightingType = GetExpandableLightingType();
                if (expandableLightingType == null)
                    return -1;

                object enumValue = Enum.Parse(expandableLightingType, lightingName);
                return (int)enumValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when get ExpandableLighting.{lightingName}: {ex.Message}");
                return -1;
            }
        }

        #endregion
	}
}
#endif
