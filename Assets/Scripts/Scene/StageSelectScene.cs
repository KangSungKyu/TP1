using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class StageSelectScene : MonoBehaviour
{
    [SerializeField]
    private RectTransform canvasRT = null;
    [SerializeField]
    private AssetReference mainScene = null;
    [SerializeField]
    private Image bgImg = null;

    private int stageMapIdx = 0;
    private StageMapUI stageMapUI = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    private async void Start()
    {
        await Factory.Instance.Init_SystemResAsync();

        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        stageMapIdx = userData.MapIdx;

        MapData mapdata = DataTableManager.Instance.GetMapData((uint)stageMapIdx);

        if(mapdata.Idx == 0)
        {
            Debug.LogError($"not found mapData, {stageMapIdx}");
            return;
        }

        Sprite bgSpr = await ResourceManager.Instance.LoadAssetAsyncTask<Sprite>(mapdata.BgSprite);

        bgImg.sprite = bgSpr;

        string resName = $"StageMapUI_{stageMapIdx}";

        await ResourceManager.Instance.LoadAssetAsyncTask<GameObject>(resName);

        stageMapUI = (await ResourceManager.Instance.InstantiateAsyncTask(resName, canvasRT)).GetComponent<StageMapUI>();

        if (stageMapUI != null)
        {
            stageMapUI.SetEnterStageEvent(userData, LoadMainScene);
        }
    }

    private void LoadMainScene()
    {
        GameSceneManager.Instance.LoadScene(mainScene);
    }

}