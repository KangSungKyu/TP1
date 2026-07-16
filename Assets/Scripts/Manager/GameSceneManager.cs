using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using static Commons;
using System.Threading;
using Cysharp.Threading.Tasks;

public class GameSceneManager : Singleton<GameSceneManager>
{
    private AsyncOperationHandle<SceneInstance> handle = default;

    public async UniTask LoadSceneAsync(AssetReference sceneRef, CancellationToken cancellationToken = default)
    {
        // 1. 로딩 씬으로 먼저 이동
        SceneManager.LoadScene(Commons.SceneName_Loading);

        await UniTask.Delay(System.TimeSpan.FromSeconds(0.5), cancellationToken: cancellationToken);

        try
        {
            var handle = Addressables.LoadSceneAsync(sceneRef, LoadSceneMode.Single);

            await handle.ToUniTask(
                progress: Progress.Create<float>(p =>
                {
                    LoadingScene.Instance.UpdateProgress(p);
                }),
                cancellationToken: cancellationToken);
        }
        catch(System.OperationCanceledException)
        {
            Debug.Log("scene loading is cancel from system");
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void LoadScene(AssetReference sceneRef)
    {
        StartCoroutine(LoadSceneRoutine(sceneRef));
    }

    private IEnumerator LoadSceneRoutine(AssetReference sceneRef)
    {
        // 1. 로딩 씬으로 먼저 이동
        SceneManager.LoadScene(Commons.SceneName_Loading);

        // 2. 잠시 대기 (로딩 씬 초기화 시간 확보)
        yield return new WaitForSeconds(0.5f);

        // 3. 실제 씬 로드
        handle = Addressables.LoadSceneAsync(sceneRef, LoadSceneMode.Single);

        while (!handle.IsDone)
        {
            float progress = handle.PercentComplete;
            // 로딩 씬의 UI에 진행률 전달 (예: 로딩 바 업데이트)
            LoadingScene.Instance.UpdateProgress(progress);
            yield return null;
        }

        if (handle.Status == AsyncOperationStatus.Failed)
            Debug.LogError("씬 로드 실패!");
    }
}