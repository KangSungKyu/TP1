using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StageSelectScene : MonoBehaviour
{
    [SerializeField]
    private RectTransform canvasRT = null;
    [SerializeField]
    private AssetReference mainScene = null;

    private int stageMapIdx = 0;
    private StageMapUI stageMapUI = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    private async void Start()
    {
        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        stageMapIdx = userData.MapIdx;

        MapData mapdata = DataTableManager.Instance.GetMapData((uint)stageMapIdx);

        if(mapdata.Idx == 0)
        {
            Debug.LogError($"not found mapData, {stageMapIdx}");
            return;
        }

        string resName = $"StageMapUI_{stageMapIdx}";

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