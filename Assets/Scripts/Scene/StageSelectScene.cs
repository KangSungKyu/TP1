using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

//todo
//mapui도 풀링 (mapdata만큼)
public class StageSelectScene : MonoBehaviour
{
    [SerializeField]
    private RectTransform canvasRT = null;
    [SerializeField]
    private AssetReference mainScene = null;
    [SerializeField]
    private Image bgImg = null;
    [SerializeField]
    private Button prevBtn = null;
    [SerializeField]
    private Button nextBtn = null;
    [SerializeField]
    private Button exitBtn = null;
    [SerializeField]
    private Button tutoBtn = null;

    private int stageMapIdx = 0;
    private List<StageMapUI> stageMapUIList = new List<StageMapUI>();

    private UserData userData = null;
    private StageClearData stageClearData = null;

    private async void Start()
    {
        await Factory.Instance.Init_SystemResAsync();

        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        int mapCount = DataTableManager.Instance.GetDataCount<MapDataForm>(DataTableType.MapData);

        for(int i = 0; i < mapCount; ++i)
        {
            uint idx = Commons.Util.CreateDataIdx(DataTableType.MapData, (uint)(i + 1));
            MapData mapdata = DataTableManager.Instance.GetMapData(idx);

            if (mapdata.Idx == 0)
            {
                Debug.LogError($"not found mapData, {idx}");
                return;
            }

            string resName = $"StageMapUI_{mapdata.Idx}";

            await ResourceManager.Instance.LoadAssetAsyncTask<GameObject>(resName);

            StageMapUI stageMapUI = (await ResourceManager.Instance.InstantiateAsyncTask(resName, canvasRT)).GetComponent<StageMapUI>();

            if (stageMapUI != null)
            {
                stageMapUI.SetEnterStageEvent(userData, LoadMainScene);
            }

            stageMapUI.gameObject.SetActive(false);
            stageMapUIList.Add(stageMapUI);
        }

        //prevBtn.transform.SetAsLastSibling();
        //nextBtn.transform.SetAsLastSibling();
        //exitBtn.transform.SetAsLastSibling();
        //tutoBtn.transform.SetAsLastSibling();

        prevBtn.onClick.AddListener(() =>
        {
            int idx = Mathf.Max(0, stageMapIdx - 1);
            LoadMap(idx);
        });

        nextBtn.onClick.AddListener(() =>
        {
            int idx = Mathf.Min(stageMapUIList.Count - 1, stageMapIdx + 1);
            LoadMap(idx);
        });

        exitBtn.onClick.AddListener(() =>
        {
            //okcancel msgbox
            Application.Quit();
        });

        tutoBtn.onClick.AddListener(() =>
        {
            string text = string.Empty;
            text = DataTableManager.Instance.GetText(1011);

            PanelManager.GetPanel<SimpleTextPanel>("TutorialPanel")?.Show(text);
        });

        LoadMap((int)Commons.Util.GetDataInnerId((uint)userData.MapIdx) - 1);
    }

    private void LoadMainScene()
    {
        GameSceneManager.Instance.LoadScene(mainScene);
    }

    private async void LoadMap(int mapIdx)
    {
        int prevIdx = stageMapIdx;
        stageMapIdx = mapIdx;

        if(stageMapUIList != null && stageMapUIList.Count > stageMapIdx)
        {
            stageMapUIList[prevIdx].gameObject.SetActive(false);
            stageMapUIList[stageMapIdx].gameObject.SetActive(true);

            uint idx = Commons.Util.CreateDataIdx(DataTableType.MapData, (uint)(stageMapIdx + 1));
            MapData mapdata = DataTableManager.Instance.GetMapData(idx);

            if (mapdata.Idx == 0)
            {
                Debug.LogError($"not found mapData, {idx}");
                return;
            }

            Sprite bgSpr = await ResourceManager.Instance.LoadAssetAsyncTask<Sprite>(mapdata.BgSprite);

            bgImg.sprite = bgSpr;
        }
    }
}