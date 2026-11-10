//pipelinedefine
#define H_URP

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using HTraceSSGI.Scripts.Infrastructure.URP;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace HTraceSSGI.Scripts.Globals
{
    public static class HRendererURP
    {
        public static bool RenderGraphEnabled
        {
            get
            {
#if UNITY_2023_3_OR_NEWER
                return GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode == false;
#endif
                return false;
            }
        }

        public static UniversalRenderPipelineAsset UrpAsset =>
            GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset ? urpAsset : null;

#if UNITY_EDITOR
        private static FieldInfo s_rendererDataListFieldInfo;
        private static FieldInfo s_defaultRendererIndexFieldInfo;

        private static ScriptableRendererData[] GetRendererDataList()
        {
            var urpAsset = UrpAsset;
            if (urpAsset == null)
                return null;

            try
            {
                if (s_rendererDataListFieldInfo == null)
                    s_rendererDataListFieldInfo = typeof(UniversalRenderPipelineAsset)
                        .GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);

                if (s_rendererDataListFieldInfo == null)
                    return null;

                return (ScriptableRendererData[])s_rendererDataListFieldInfo.GetValue(urpAsset);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get renderer data list: {e.Message}");
                return null;
            }
        }

        private static int GetDefaultRendererIndex()
        {
            var urpAsset = UrpAsset;
            if (urpAsset == null)
                return -1;

            try
            {
                if (s_defaultRendererIndexFieldInfo == null)
                    s_defaultRendererIndexFieldInfo = typeof(UniversalRenderPipelineAsset)
                        .GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);

                if (s_defaultRendererIndexFieldInfo == null)
                    return -1;

                return (int)s_defaultRendererIndexFieldInfo.GetValue(urpAsset);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get default renderer index: {e.Message}");
                return -1;
            }
        }

        public static UniversalRendererData UniversalRendererData => GetUniversalRendererData();

        /// <summary>
        /// Get UniversalRendererData by index or default
        /// </summary>
        /// <param name="rendererIndex">Renderer index. If -1 - than default</param>
        /// <returns></returns>
        private static UniversalRendererData GetUniversalRendererData(int rendererIndex = -1)
        {
            var rendererDataList = GetRendererDataList();
            if (rendererDataList == null || rendererDataList.Length == 0)
                return null;

            if (rendererIndex == -1) rendererIndex = GetDefaultRendererIndex();

            // Index validation
            if (rendererIndex < 0 || rendererIndex >= rendererDataList.Length)
            {
                Debug.LogWarning(
                    $"Invalid renderer index {rendererIndex}. Available renderers: {rendererDataList.Length}");
                return null;
            }

            return rendererDataList[rendererIndex] as UniversalRendererData;
        }

        public static bool IsSsaoNativeEnabled()
        {
            return HasRendererFeatureByTypeName("ScreenSpaceAmbientOcclusion");
        }

        private static bool HasRendererFeatureByTypeName(string typeName, int rendererIndex = -1)
        {
            return GetRendererFeatureByTypeName(typeName, rendererIndex) != null;
        }

        public static ScriptableRendererFeature GetRendererFeatureByTypeName(string typeName, int rendererIndex = -1)
        {
            var rendererDataList = GetRendererDataList();
            if (rendererDataList == null || rendererDataList.Length == 0)
                return null;

            var renderersToSearch = new List<ScriptableRendererData>();

            if (rendererIndex >= 0 && rendererIndex < rendererDataList.Length)
                renderersToSearch.Add(rendererDataList[rendererIndex]);
            else
                renderersToSearch.AddRange(rendererDataList);

            foreach (var rendererData in renderersToSearch)
            {
                if (rendererData?.rendererFeatures == null) continue;

                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature == null) continue;

                    if (feature.GetType().Name.Contains(typeName, StringComparison.OrdinalIgnoreCase)) return feature;
                }
            }

            return null;
        }

        public static T GetRendererFeature<T>(int rendererIndex = -1) where T : ScriptableRendererFeature
        {
            var rendererDataList = GetRendererDataList();
            if (rendererDataList == null || rendererDataList.Length == 0)
                return null;

            var renderersToSearch = new List<ScriptableRendererData>();

            if (rendererIndex >= 0 && rendererIndex < rendererDataList.Length)
                renderersToSearch.Add(rendererDataList[rendererIndex]);
            else
                renderersToSearch.AddRange(rendererDataList);

            foreach (var rendererData in renderersToSearch)
            {
                if (rendererData?.rendererFeatures == null) continue;

                foreach (var feature in rendererData.rendererFeatures)
                    if (feature is T typedFeature)
                        return typedFeature;
            }

            return null;
        }

        private static bool ContainsRenderFeature(List<ScriptableRendererFeature> features, string name)
        {
            if (features == null) return false;

            for (var i = 0; i < features.Count; i++)
                if (features[i]?.name == name)
                    return true;
            return false;
        }

        public static void AddHTraceRendererFeatureToUniversalRendererData()
        {
            var universalRendererData = UniversalRendererData;
            if (universalRendererData?.rendererFeatures == null)
            {
                Debug.LogWarning("Universal Renderer Data not found or has no features list");
                return;
            }

            var features = universalRendererData.rendererFeatures;

            CleanupRendererFeatures(features);

            if (!ContainsRenderFeature(features, nameof(HTraceSSGIRendererFeature)))
                AddHTraceRendererFeature(universalRendererData, features);

            universalRendererData.SetDirty();
        }

        private static void CleanupRendererFeatures(List<ScriptableRendererFeature> features)
        {
            for (var i = features.Count - 1; i >= 0; i--)
            {
                var feature = features[i];

                // Delete null elements
                if (feature == null)
                {
                    features.RemoveAt(i);
                    continue;
                }

                if (feature.GetType() == typeof(HTraceSSGIRendererFeature)) features.RemoveAt(i);
            }
        }

        private static void AddHTraceRendererFeature(UniversalRendererData universalRendererData, List<ScriptableRendererFeature> features)
        {
            try
            {
                var hTraceFeature = ScriptableObject.CreateInstance<HTraceSSGIRendererFeature>();
                AssetDatabase.AddObjectToAsset(hTraceFeature, universalRendererData);
                features.Add(hTraceFeature);

                Debug.Log($"{HNames.ASSET_NAME} Renderer Feature added successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to add {HNames.ASSET_NAME} Renderer Feature: {e.Message}");
            }
        }

        /// <summary>
        /// Get all renderer features by Type in all RendererDatas
        /// </summary>
        /// <typeparam name="T">Тип renderer feature</typeparam>
        /// <returns></returns>
        public static List<T> GetAllRendererFeatures<T>() where T : ScriptableRendererFeature
        {
            var result = new List<T>();
            var rendererDataList = GetRendererDataList();

            if (rendererDataList == null) return result;

            foreach (var rendererData in rendererDataList)
            {
                if (rendererData?.rendererFeatures == null) continue;

                foreach (var feature in rendererData.rendererFeatures)
                    if (feature is T typedFeature)
                        result.Add(typedFeature);
            }

            return result;
        }

        public static int GetRenderersCount()
        {
            return GetRendererDataList()?.Length ?? 0;
        }
#endif // UNITY_EDITOR
    }
}
