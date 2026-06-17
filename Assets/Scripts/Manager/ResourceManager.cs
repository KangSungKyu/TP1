using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

// Resource manager using Commons.Singleton<T>
public class ResourceManager : Commons.Singleton<ResourceManager>
{
    // Keep track of handles to allow safe release
    private readonly static string labelName = "Remote";
    private readonly Dictionary<string, AsyncOperationHandle> loadHandles = new Dictionary<string, AsyncOperationHandle>();
    private readonly List<AsyncOperationHandle> instantiateHandles = new List<AsyncOperationHandle>();

    public IEnumerator Init(Action onComplete = null)
    {
        // 1. 초기화
        yield return Addressables.InitializeAsync();

        // 2. [필수] 서버에서 카탈로그 정보를 새로 받아와야 합니다!
        var updateHandle = Addressables.CheckForCatalogUpdates(false);

        yield return updateHandle;

        if (updateHandle.Status == AsyncOperationStatus.Succeeded)
        {
            var catalogs = updateHandle.Result;

            if (catalogs.Count > 0)
            {
                yield return Addressables.UpdateCatalogs(catalogs);
            }
        }

        Addressables.Release(updateHandle);

        yield return StartCoroutine(IEStartDownload(onComplete));
    }

    public IEnumerator IEStartDownload(System.Action onResourceLoad)
    {
        var locationHandle = Addressables.LoadResourceLocationsAsync(labelName, typeof(object));

        yield return locationHandle;

        if (locationHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"찾은 결과 개수: {locationHandle.Result.Count}");
            // 2. 다운로드 시작
            AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(locationHandle.Result);

            // 3. 다운로드 완료까지 진행률 추적
            while (!handle.IsDone)
            {
                float progress = handle.PercentComplete;
                Debug.Log($"다운로드 중: {progress * 100}%");
                // UI에 업데이트 (예: slider.value = progress)
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("다운로드 완료!");
                // 이제 리소스를 로드해도 됩니다.
                onResourceLoad?.Invoke();
            }
            else
            {
                Debug.LogError("다운로드 실패: " + handle.OperationException);
            }

            Addressables.Release(handle);
        }
        else
        {
            Debug.LogError($"그룹을 찾을 수 없습니다. (상태: {locationHandle.Status})");
            // 발견된 모든 그룹을 출력해서 이름이 일치하는지 확인
            foreach (var location in locationHandle.Result)
            {
                Debug.Log($"발견된 키: {location.PrimaryKey}");
            }
        }

        Addressables.Release(locationHandle);
    }

    public void LoadAssetAsync<T>(string key, Action<T> onLoaded) where T : class
    {
        if(loadHandles.ContainsKey(key))
        {
            onLoaded?.Invoke((T)loadHandles[key].Result);
        }
        else
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);

                handle.Completed += (AsyncOperationHandle<T> op) =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        loadHandles.Add(key, handle);
                        onLoaded?.Invoke(op.Result);
                    }
                    else
                    {
                        Debug.LogError($"LoadAssetAsync<{typeof(T).Name}> failed for key={key}: {op.OperationException}");
                        onLoaded?.Invoke(null);
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoadAssetAsync<{typeof(T).Name}> threw for key={key}: {ex}");
                onLoaded?.Invoke(null);
            }
        }
    }

    public T GetResource<T>(string key) where T : class
    {
        T resource = null;

        if(loadHandles.ContainsKey(key))
        {
            resource = (T)loadHandles[key].Result;
        }

        return resource;
    }

    public Task<T> LoadAssetAsyncTask<T>(string key) where T : class
    {
        if(loadHandles.ContainsKey(key))
        {
            return Task.FromResult((T)loadHandles[key].Result);
        }
        else
        {
            var tcs = new TaskCompletionSource<T>();
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);

                handle.Completed += (AsyncOperationHandle<T> op) =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        loadHandles.Add(key, handle);
                        tcs.SetResult(op.Result);
                    }
                    else
                    {
                        tcs.SetException(op.OperationException ?? new Exception("Addressables load failed"));
                    }
                };
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }
    }


    public void LoadAssetsAsync<T>(IList<IResourceLocation> locList, Action<T> onComp)
    {
        StartCoroutine(IELoadAssetsAsync(locList, onComp));
    }

    public async Task<GameObject> InstantiateAsyncTask(string key, Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
    {
        try
        {
            AsyncOperationHandle<GameObject> handle;

            if (position.HasValue || rotation.HasValue)
            {
                handle = Addressables.InstantiateAsync(key, position ?? Vector3.zero, rotation ?? Quaternion.identity, parent);
            }
            else
            {
                handle = Addressables.InstantiateAsync(key, parent);
            }

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                instantiateHandles.Add(handle);

                return handle.Result;
            }
            else
            {
                throw handle.OperationException ?? new Exception("Addressables instantiate failed");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"InstantiateAsyncTask threw for key={key}: {ex}");
            return null;
        }
    }

    public void ReleaseInstance(GameObject go)
    {
        if (go == null) 
            return;

        try
        {
            int idx = instantiateHandles.FindIndex((o) => (GameObject)o.Result == go);

            Addressables.ReleaseInstance(go);

            if(idx > -1)
            {
                instantiateHandles.RemoveAt(idx);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ReleaseInstance threw: {ex}");
        }
    }

    public void Release(string key)
    {
        AsyncOperationHandle handle = default;

        if(loadHandles.ContainsKey(key))
        {
            handle = loadHandles[key];

            try
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                    loadHandles.Remove(key);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Release(handle) threw: {ex}");
            }
        }
    }

    public void ReleaseAll()
    {
        foreach(var pair in loadHandles)
        {
            var h = pair.Value;

            try
            {
                if (h.IsValid())
                { 
                    Addressables.Release(h);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ReleaseAll: failed to release handle: {ex}");
            }
        }

        loadHandles.Clear();

        try
        {
            foreach (var h in instantiateHandles)
            {
                ReleaseInstance(h.Result as GameObject);
            }
        }
        catch(Exception ex)
        {
            Debug.LogError($"ReleaseAll: failed to release inst handle:{ex}");
        }

        instantiateHandles.Clear();
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
        Debug.Log("ResourceMgr initialized");
    }

    protected override void OnSingletonDestroyed()
    {
        base.OnSingletonDestroyed();
        //ReleaseAll();
    }

    private IEnumerator IELoadAssetsAsync<T>(IList<IResourceLocation> locList, Action<T> onComp)
    {
        var loadHandle = Addressables.LoadAssetsAsync<T>(locList, onComp);

        yield return loadHandle;

        Addressables.Release(loadHandle);
    }
}