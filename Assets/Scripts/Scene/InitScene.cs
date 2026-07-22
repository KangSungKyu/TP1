
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using static UnityEngine.Analytics.IAnalytic;

public class InitScene : MonoBehaviour
{
    [SerializeField]
    private AssetReference selectSceneRef = null;

    private void Start()
    {
        Init();
    }

    private async void Init()
    {
        await ResourceManager.Instance.InitAsync(() =>
        {
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_WinBG, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_DefeatBG, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_WinText, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_DefeatText, (o) => Debug.Log($"resource loaded, {o.name}"));

            for (int i = 0; i < 3; ++i)
            {
                ResourceManager.Instance.LoadAssetAsync<Sprite>($"{Commons.ResKey_StageUIs}[{Commons.ResKey_StageUIs}_{i}]", (o) => Debug.Log($"resource loaded, {o.name}"));
            }

            ResourceManager.Instance.LoadAssetAsync<GameObject>(Commons.ResKey_Board, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>(Commons.ResKey_HPUI, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>(Commons.ResKey_MonsterUnit, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>(Commons.ResKey_PlayerUnit, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>(Commons.ResKey_PortraitUI, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>(Commons.ResKey_Tile, (o) => Debug.Log($"resource loaded, {o.name}"));

            ResourceManager.Instance.LoadAssetAsync<Sprite>($"{Commons.ResKey_PortraitUIs}[{Commons.ResKey_PortraitUIs}_1]", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<RuntimeAnimatorController>("player_anim", (o) => Debug.Log($"resource loaded, {o.name}"));

            SaveLoadManager.Instance.LoadClientData();

            SaveLoadManager.Instance.LoadUserData(
                () => 
                {
                    Debug.Log("load");
                    AfterLoadUserData(); 
                }, 
                (json) =>
                {
                    Debug.Log("load fail");
                    AfterLoadUserData();
                });

        }, this.GetCancellationTokenOnDestroy());
    }

    private async void AfterLoadUserData()
    {
        try
        {
            await GameSceneManager.Instance.LoadSceneAsync(selectSceneRef, CancellationToken.None);
        }
        catch (System.OperationCanceledException)
        {
            // Swallow cancellation when the InitScene object is destroyed during scene load.
        }
    }

}
