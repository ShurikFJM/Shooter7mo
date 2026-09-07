using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace CoInspector
{
    internal class AssetTracker : AssetPostprocessor
    {/*
        private static Dictionary<int, float> lastImportsByAssetID = new Dictionary<int, float>();
        private static Dictionary<string, float> lastImportsByPath = new Dictionary<string, float>();
        private static float COOLDOWN = 2f;

        private static Func<string, int> getMainAssetInstanceID;
        private static bool reflectionInitialized = false; */

        public static Action OnAssetChangesDetected;
        /*
                static void InitializeReflection()
                {
                    if (reflectionInitialized) return;

                    try
                    {
                        var method = typeof(AssetDatabase).GetMethod(
                            "GetMainAssetInstanceID",
                            BindingFlags.Static | BindingFlags.NonPublic,
                            null,
                            new Type[] { typeof(string) },
                            null);

                        if (method != null)
                        {
                            getMainAssetInstanceID = (Func<string, int>)Delegate.CreateDelegate(
                                typeof(Func<string, int>), method);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[CoInspector] Failed to initialize reflection: {e.Message}");
                    }

                    reflectionInitialized = true;
                } */

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // InitializeReflection();

            if (deletedAssets.Length > 0 || movedAssets.Length > 0)
            {
                OnAssetChangesDetected?.Invoke();
                return;
            }
            /*
                        bool shouldRebuild = false;

                        foreach (string path in importedAssets)
                        {
                            if (path.EndsWith(".meta"))
                            {
                                continue;
                            }

                            if (getMainAssetInstanceID != null)
                            {
                                int trackingID = getMainAssetInstanceID(path);

                                if (IsAssetLooping(trackingID))
                                {
                                    Debug.LogWarning($"[CoInspector] Ignoring rapid reimport: {path}");
                                    continue;
                                }

                                shouldRebuild = true;
                                lastImportsByAssetID[trackingID] = Time.realtimeSinceStartup;
                            }
                            else
                            {
                                if (IsPathLooping(path))
                                {
                                    Debug.LogWarning($"[CoInspector] Ignoring rapid reimport: {path}");
                                    continue;
                                }

                                shouldRebuild = true;
                                lastImportsByPath[path] = Time.realtimeSinceStartup;
                            }
                        }
                        CleanupImports();
                        if (shouldRebuild)
                        {
                            Debug.Log("Changes detected");
                            OnAssetChangesDetected?.Invoke();
                        } */
        }
        /*
                static bool IsAssetLooping(int assetID)
                {
                    if (lastImportsByAssetID.TryGetValue(assetID, out float lastTime))
                    {
                        float timeSince = Time.realtimeSinceStartup - lastTime;
                        return timeSince < COOLDOWN;
                    }
                    return false;
                }

                static bool IsPathLooping(string path)
                {
                    if (lastImportsByPath.TryGetValue(path, out float lastTime))
                    {
                        float timeSince = Time.realtimeSinceStartup - lastTime;
                        return timeSince < COOLDOWN;
                    }
                    return false;
                }

                static void CleanupImports()
                {
                    var toRemove = new List<int>();
                    var pathsToRemove = new List<string>();
                    float currentTime = Time.realtimeSinceStartup;

                    foreach (var import in lastImportsByAssetID)
                    {
                        if (currentTime - import.Value > COOLDOWN * 2)
                        {
                            toRemove.Add(import.Key);
                        }
                    }
                    foreach (int id in toRemove)
                    {
                        lastImportsByAssetID.Remove(id);
                    }

                    foreach (var import in lastImportsByPath)
                    {
                        if (currentTime - import.Value > COOLDOWN * 2)
                        {
                            pathsToRemove.Add(import.Key);
                        }
                    }
                    foreach (string path in pathsToRemove)
                    {
                        lastImportsByPath.Remove(path);
                    }
                }*/
    }
}