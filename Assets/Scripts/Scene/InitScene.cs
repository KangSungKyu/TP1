
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
            ResourceManager.Instance.LoadAssetAsync<Sprite>("BaseTile", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>("BlockTile", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>("AttackTile", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>("GuardTile", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<Sprite>("ShieldTile", (o) => Debug.Log($"resource loaded, {o.name}"));

            ResourceManager.Instance.LoadAssetAsync<GameObject>("Board", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("HPUI", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("MonsterUnit", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("PlayerUnit", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("PortraitUI", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<GameObject>("Tile", (o) => Debug.Log($"resource loaded, {o.name}"));

            ResourceManager.Instance.LoadAssetAsync<RuntimeAnimatorController>("player_anim", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<RuntimeAnimatorController>("monster1_anim", (o) => Debug.Log($"resource loaded, {o.name}"));
            ResourceManager.Instance.LoadAssetAsync<RuntimeAnimatorController>("monster2_anim", (o) => Debug.Log($"resource loaded, {o.name}"));

            ResourceManager.Instance.LoadAssetAsync<GameObject>("StageMapUI_1", (o) => Debug.Log($"resource loaded, {o.name}"));

            SaveLoadManager.Instance.LoadUserData();
            SaveLoadManager.Instance.LoadStageClearData();

            GameSceneManager.Instance.LoadScene(selectSceneRef);
        }));
    }

}
