using System;
using UnityEditor;
using UnityEngine;

namespace CoInspector
{
    [Serializable]
    public class MissingScriptManager : ScriptableObject
    {
        private static MissingScriptManager instance;
        private static string STORAGE_PATH
        {
            get
            {
                string path = CoInspectorWindow._GetRootPath() + "/Core/Helpers/MissingScriptManager.asset";
                return path;
            }
        }
#if UNITY_6000_4_OR_NEWER
        [HideInInspector] public EntityId entityID = EntityId.None;

#else
        [HideInInspector] public int instanceID = -1;

#endif
        [HideInInspector] public int localIdentifierInFile = -1;
        [HideInInspector] public int count = 0;
        [HideInInspector] public string path = "";
        [HideInInspector] public string assetName = "";
        [HideInInspector] public MonoScript script = null;

        private void CleanExtension()
        {
            if (assetName.Contains("."))
            {
                assetName = assetName.Substring(0, assetName.LastIndexOf('.'));
            }
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;

            instance = AssetDatabase.LoadAssetAtPath<MissingScriptManager>(STORAGE_PATH);

            if (instance == null)
            {
                instance = CreateInstance<MissingScriptManager>();
                AssetDatabase.CreateAsset(instance, STORAGE_PATH);
            }
        }

#if UNITY_6000_4_OR_NEWER

        public static MissingScriptManager WriteData()
        {
            EnsureInstance();
            instance.path = AssetDatabase.GetAssetPath(instance.GetEntityId());
            string guid;
            long _localIdentifierInFile;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance.GetEntityId(), out guid, out _localIdentifierInFile);
            instance.localIdentifierInFile = (int)_localIdentifierInFile;
            instance.assetName = instance.path[(instance.path.LastIndexOf('/') + 1)..];
            instance.count = 1;
            instance.CleanExtension();
            return instance;
        }


        public static MissingScriptManager WriteData(EntityId entityID)
        {
            EnsureInstance();
            if (entityID.IsValid())
            {
                instance.entityID = entityID;
            }
            return WriteData();
        }
#else
#pragma warning disable CS0618

        public static MissingScriptManager WriteData(int instanceID = -1)
        {
            EnsureInstance();
            if (instanceID != -1)
            {
                instance.instanceID = instanceID;
            }
            instance.path = AssetDatabase.GetAssetPath(instance.instanceID);
            string guid;
            long _localIdentifierInFile;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance.instanceID, out guid, out _localIdentifierInFile);
            instance.localIdentifierInFile = (int)_localIdentifierInFile;
            instance.assetName = instance.path[(instance.path.LastIndexOf('/') + 1)..];
            instance.count = 1;
            instance.CleanExtension();
            return instance;
        }
#pragma warning restore CS0618
#endif
        public static MissingScriptManager WriteData(string _path)
        {
            EnsureInstance();
#if UNITY_6000_4_OR_NEWER
            instance.entityID = EntityId.None;

#else
            instance.instanceID = 0;

#endif
            instance.path = _path;
            instance.assetName = instance.path[(instance.path.LastIndexOf('/') + 1)..];
            instance.count = 1;
            instance.CleanExtension();
            return instance;
        }
#if UNITY_6000_4_OR_NEWER
        public static int CountMissingScripts()
        {
            int count = 0;
            foreach (var entityID in Selection.entityIds)
            {
                var asset = EditorUtils.IdToObject(entityID);
                if (!asset)
                {
                    if (count == 0)
                    {
                        EnsureInstance();
                        instance.entityID = entityID;
                    }
                    count++;
                }
            }
            return count;
        }
#else
#pragma warning disable CS0618

        public static int CountMissingScripts()
        {
            int count = 0;
            foreach (var instanceID in Selection.instanceIDs)
            {
                var asset = EditorUtils.IdToObject(instanceID);
                if (!asset)
                {
                    if (count == 0)
                    {
                        EnsureInstance();
                        instance.instanceID = instanceID;
                    }
                    count++;
                }
            }
            return count;
        }
#pragma warning restore CS0618
#endif
        public static MissingScriptManager WriteMultiData(int count)
        {
            EnsureInstance();
#if UNITY_6000_4_OR_NEWER
            instance.path = AssetDatabase.GetAssetPath(instance.GetEntityId());

#else
#pragma warning disable CS0618
            instance.path = AssetDatabase.GetAssetPath(instance.instanceID);
#pragma warning restore CS0618
#endif
            instance.assetName = count + " Missing Scripts";
            instance.count = count;
            return instance;
        }
        public static void SetInactive()
        {
            EnsureInstance();
            instance.count = 0;
        }
        public static int Count()
        {
            EnsureInstance();
            return instance.count;
        }


        public static void ClearData()
        {
            EnsureInstance();
#if UNITY_6000_4_OR_NEWER
            instance.entityID = EntityId.None;

#else
            instance.instanceID = 0;

#endif
            instance.localIdentifierInFile = -1;
            instance.count = 0;
            instance.path = "";
            instance.assetName = "";
            instance.script = null;
        }


        public static bool IsActive()
        {
            EnsureInstance();
            return instance.count > 0;
        }

        public static bool IsMulti => instance != null && instance.count > 1;
    }
    [Serializable]
    internal class MissingComponent : MonoBehaviour
    {
        public int instanceID = -1;
        public int index = -1;
        public GameObject owner = null;

        internal MissingComponent(int instanceID, int index, GameObject owner)
        {
            this.instanceID = instanceID;
            this.index = index;
            this.owner = owner;
        }
    }
}
