
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class InitScene : MonoBehaviour
{
    [SerializeField]
    private AssetReference selectSceneRef = null;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        StartCoroutine(ResourceManager.Instance.Init(() =>
        {
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_BaseTile, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_BlockTile, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_AttackTile, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_GuardTile, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_ShieldTile, (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>(Commons.ResKey_SkillTile, (o) => Debug.Log($"resource loaded, {o.name}"));

            ResourceManager.Instance.LoadAssetAsync<GameObject>("Board", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("HPUI", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("MonsterUnit", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("PlayerUnit", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("PortraitUI", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("Tile", (o) => Debug.Log($"resource loaded, {o.name}"));

            ResourceManager.Instance.LoadAssetAsync<Sprite>("Portraits[Portraits_1]", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<RuntimeAnimatorController>("player_anim", (o) => Debug.Log($"resource loaded, {o.name}"));

            SaveLoadManager.Instance.LoadClientData();

            SaveLoadManager.Instance.LoadUserData(
                () => 
                {
                    Debug.Log("load");
                    AfterLoadUserData(); 
                }, 
                () =>
                {
                    Debug.Log("load fail");
                    AfterLoadUserData();
                });

        }));
    }

    private void AfterLoadUserData()
    {
        GameSceneManager.Instance.LoadScene(selectSceneRef);
    }
}
