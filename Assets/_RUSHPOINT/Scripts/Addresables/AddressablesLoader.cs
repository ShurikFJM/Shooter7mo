using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesLoader : MonoBehaviour
{
    private static AddressablesLoader _instance;
    public static AddressablesLoader Instance => _instance;

    private readonly Dictionary<string, AsyncOperationHandle> _loadedAssetHandles = new Dictionary<string, AsyncOperationHandle>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadAsset<T>(string addressKey, Action<T> onCompleteCallback) where T : UnityEngine.Object
    {
        if (_loadedAssetHandles.TryGetValue(addressKey, out AsyncOperationHandle existingHandle))
        {
            if (existingHandle.IsDone)
            {
                onCompleteCallback?.Invoke(existingHandle.Result as T);
                return;
            }
        }

        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(addressKey);
        _loadedAssetHandles[addressKey] = handle;

        handle.Completed += operation =>
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                onCompleteCallback?.Invoke(operation.Result);
            }
            else
            {
                Debug.LogError($"[Addressables] Failed to load asset with key: {addressKey}");
                onCompleteCallback?.Invoke(null);
            }
        };
    }

    public void InstantiatePrefab(string addressKey, Vector3 position, Quaternion rotation, Action<GameObject> onCompleteCallback)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(addressKey, position, rotation);

        handle.Completed += operation =>
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                onCompleteCallback?.Invoke(operation.Result);
            }
            else
            {
                Debug.LogError($"[Addressables] Failed to instantiate prefab with key: {addressKey}");
                onCompleteCallback?.Invoke(null);
            }
        };
    }

    public void ReleaseAsset(string addressKey)
    {
        if (_loadedAssetHandles.TryGetValue(addressKey, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            _loadedAssetHandles.Remove(addressKey);
        }
    }

    public void ReleaseInstance(GameObject instanceToRelease)
    {
        if (instanceToRelease != null)
        {
            Addressables.ReleaseInstance(instanceToRelease);
        }
    }
}