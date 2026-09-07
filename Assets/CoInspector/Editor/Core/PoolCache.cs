using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CoInspector
{
    internal static class PoolCache
    {
        public static Rect marginRect = new Rect(0, 1, 20, 22);
        public static Rect historyMarginRect = new Rect(40, 1, 20, 22);

        private static List<AssetInfo> _assetInfoCache = new List<AssetInfo>();
        private static int maxEntries = 100;

        public static bool IsAssetAFolder(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.isFolder ?? false;
        }

        public static bool IsAPrefabAsset(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.isPrefab ?? false;
        }

        public static bool IsBrokenPrefab(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.isBrokenPrefab ?? false;
        }

        public static bool IsAnImportedObject(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.isImportedObject ?? false;
        }

        public static bool IsMainAsset(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.isMainAsset ?? false;
        }

        public static string GetAssetPath(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.path ?? string.Empty;
        }

        public static string GetAssetExtension(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.extension ?? string.Empty;
        }

        public static string GetNiceType(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.niceType ?? string.Empty;
        }

        public static Texture GetIcon(UnityEngine.Object asset)
        {
            AssetInfo assetInfo = GetOrUpdateAssetInfo(asset);
            return assetInfo?.icon;
        }

        public static AssetInfo GetAssetInfo(UnityEngine.Object asset)
        {
            return GetOrUpdateAssetInfo(asset);
        }

        private static AssetInfo GetOrUpdateAssetInfo(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return null;
            }
            CleanNulls();
            string currentPath = AssetDatabase.GetAssetPath(asset);

            foreach (AssetInfo info in _assetInfoCache)
            {
                if (info.asset == asset)
                {
                    if (info.path != currentPath || info.asset == null)
                    {
                        //  Debug.Log("Rebuilding cache for " + asset.name);
                        // Asset path has changed or asset is null, update the cache
                        info.path = currentPath;
                        info.extension = System.IO.Path.GetExtension(currentPath).ToLower();
                        info.isFolder = CoInspectorWindow.IsAssetAFolder(currentPath);
                        info.isPrefab = info.extension == ".prefab";
                        info.isBrokenPrefab = info.isPrefab && !(asset is GameObject);
                        info.isImportedObject = EditorUtils.IsAnImportedObject(asset);
                        info.isMainAsset = EditorUtils.IsMainAsset(asset);
                        info.niceType = ObjectNames.NicifyVariableName(asset.GetType().Name);
                        info.icon = AssetPreview.GetMiniThumbnail(asset);
                    }

                    return info;
                }
            }

            AssetInfo newAssetInfo = new AssetInfo();
            //   Debug.Log("Adding new cache for " + asset.name);
            newAssetInfo.asset = asset;
            newAssetInfo.path = currentPath;
            newAssetInfo.extension = System.IO.Path.GetExtension(currentPath).ToLower();
            newAssetInfo.isFolder = CoInspectorWindow.IsAssetAFolder(currentPath);
            newAssetInfo.isPrefab = newAssetInfo.extension == ".prefab";
            newAssetInfo.isBrokenPrefab = newAssetInfo.isPrefab && !(asset is GameObject);
            newAssetInfo.isImportedObject = EditorUtils.IsAnImportedObject(asset);
            newAssetInfo.isMainAsset = EditorUtils.IsMainAsset(asset);
            newAssetInfo.niceType = ObjectNames.NicifyVariableName(asset.GetType().Name);
            newAssetInfo.icon = AssetPreview.GetMiniThumbnail(asset);
            _assetInfoCache.Add(newAssetInfo);
            FreeUpElements();
            return newAssetInfo;
        }

        static void CleanNulls()
        {
            _assetInfoCache.RemoveAll(info => info == null || info.asset == null);
        }

        private static void FreeUpElements()
        {
            if (_assetInfoCache.Count > maxEntries)
            {
                _assetInfoCache.RemoveAt(0);
            }
        }
    }

    internal class AssetInfo
    {
        public Object asset;
        public string path;
        public bool isFolder;
        public bool singleHeader;
        public bool isPrefab;
        public bool isBrokenPrefab;
        public bool isImportedObject;
        public bool isMainAsset;
        public string extension;
        public string niceType;
        public Texture icon;
    }
}