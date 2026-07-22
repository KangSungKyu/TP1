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

}