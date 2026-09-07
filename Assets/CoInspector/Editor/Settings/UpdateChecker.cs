using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace CoInspector
{

    [InitializeOnLoad]
    public static class UpdateChecker
    {
        internal static bool IsUpdateAvailable { get; private set; } = false;
        internal static string LatestVersion { get; private set; } = string.Empty;
        internal static readonly string currentVersion = "1.7.5";
        internal static readonly bool hasChangeLog = true;
        internal static readonly bool autoOpenChangeLog = false;
        private static readonly string updateUrl = "https://stuff-solutions.com/coinspector/latest-version.json";

        static UpdateChecker()
        {
            EditorApplication.update += RunUpdateCheck;
        }

        internal static bool HasValidChangelog()
        {
            return hasChangeLog && CustomGUIContents.ValidChangelog != null;
        }

        private static void RunUpdateCheck()
        {
            IsUpdateAvailable = false;
            LatestVersion = string.Empty;
            EditorApplication.update -= RunUpdateCheck;
            CheckForUpdates();
        }

        private static void CheckForUpdates()
        {
            EditorApplication.update += CheckForUpdatesCoroutine;
        }

        private static IEnumerator currentCoroutine = null;

        private static void CheckForUpdatesCoroutine()
        {
            if (currentCoroutine == null)
            {
                currentCoroutine = CheckForUpdatesRoutine();
            }

            if (!currentCoroutine.MoveNext())
            {
                EditorApplication.update -= CheckForUpdatesCoroutine;
                currentCoroutine = null;
            }
        }

        private static IEnumerator CheckForUpdatesRoutine()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(updateUrl))
            {
                var asyncOperation = request.SendWebRequest();
                while (!asyncOperation.isDone)
                {
                    yield return null;
                }

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    IsUpdateAvailable = false;
                    LatestVersion = string.Empty;
                }
                else
                {
                    ProcessUpdateInfo(request.downloadHandler.text);
                }
            }
        }

        private static void ProcessUpdateInfo(string json)
        {
            UpdateInfo updateInfo = JsonUtility.FromJson<UpdateInfo>(json);
            if (IsNewVersionAvailable(updateInfo.latestVersion))
            {
                OnNewVersionDetected(updateInfo.latestVersion);
            }
            else
            {
                IsUpdateAvailable = false;
                LatestVersion = string.Empty;
            }
        }

        private static bool IsNewVersionAvailable(string latestVersion)
        {
            return string.Compare(latestVersion, currentVersion) > 0;
        }

        private static void OnNewVersionDetected(string latestVersion)
        {
            Debug.Log($"A new version ({latestVersion}) of CoInspector is available! Current version: {currentVersion}");
            IsUpdateAvailable = true;
            LatestVersion = latestVersion;
        }

        [System.Serializable]
        private class UpdateInfo
        {
            public string latestVersion;
        }
    }
}