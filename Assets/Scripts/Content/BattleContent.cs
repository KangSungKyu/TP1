using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class BattleContent : GameContent
{
    [SerializeField]
    private TextMeshProUGUI stageUI = null;
    [SerializeField]
    private BattleStage battleStage = null;
    [SerializeField]
    private AssetReference selectStageScene = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    public override async void Enter()
    {
        base.Enter();

        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = SODataTable.Instance.GetStageData((uint)userData.StageIdx);

        stageUI.SetText($"{sd.Stage} - {sd.SubStage}");

        int monsterPoolCount = 0;
        int tilePoolCount = 0;
        int boardPoolCount = 0;

        for (int i = 0; i < sd.MonsterIdx.Length; ++i)
        {
            MonsterData md = SODataTable.Instance.GetMonsterData(sd.MonsterIdx[i]);

            tilePoolCount += (int)((md.BoardDefaultWidth * md.BoardDefaultHeight) * sd.MonsterCount[i]);
            monsterPoolCount += (int)sd.MonsterCount[i];
        }

        tilePoolCount = tilePoolCount * 2; //buffer
        boardPoolCount = monsterPoolCount * 2; //buffer

        await Factory.Instance.Init_UnitPoolAsync(monsterPoolCount);
        await Factory.Instance.Init_BoardPoolAsync(boardPoolCount, tilePoolCount);

        await Factory.Instance.InitAsync().ContinueWith(t => {
            if (t.IsFaulted)
                Debug.LogError($"Factory.InitAsync failed: {t.Exception}");
            else if (!t.Result)
                Debug.LogWarning("Factory.InitAsync completed with failure");
            else
                Debug.Log("Factory.InitAsync completed successfully");
        }, TaskScheduler.FromCurrentSynchronizationContext());

        battleStage.OnStageClear(OnStageClear);
        battleStage.OnStageDefeat(OnStageDefeat);
        battleStage.InitStage(userData, sd);
    }



    private void OnStageDefeat()
    {
        //player stage progress to last save point(stage)
        //anim defeat ui
        //branch stage select ui and title scene by player selection

        battleStage.ReleaseStage();

        Debug.Log($"stage defeat");

        userData.Exp = 0;
        userData.MapIdx = userData.SavedMapIdx;
        userData.StageIdx = userData.SavedStageIdx;

        SaveLoadManager.Instance.SaveUserData((t) => { Debug.Log("save"); }, () => { Debug.Log("save fail"); });

        GameSceneManager.Instance.LoadScene(selectStageScene);
    }

    private void OnStageClear()
    {
        //clear ui
        //calc reward
        //input map change (battle -> ui)
        //goto stage select ui(or scene)

        battleStage.ReleaseStage();

        Debug.Log($"stage clear");

        StageData stageData = SODataTable.Instance.GetStageData((uint)userData.StageIdx);
        uint totalExp = 0;

        for (int i = 0; i < stageData.MonsterIdx.Length; ++i)
        {
            if (stageData.MonsterIdx[i] > 0)
            {
                MonsterData monsterData = SODataTable.Instance.GetMonsterData(stageData.MonsterIdx[i]);
                RewardData rewardData = SODataTable.Instance.GetRewardData(stageData.RewardIdx[i]);

                totalExp += (rewardData.Exp * stageData.MonsterCount[i]);
            }
        }

        userData.Exp += (int)totalExp;
        stageClearData.SetState(userData.StageIdx, 1);

        if (userData.SavedMapIdx <= 0)
        {
            userData.SavedMapIdx = 1;
        }

        if (userData.SavedStageIdx <= 0)
        {
            userData.SavedStageIdx = 1;
        }

        SaveLoadManager.Instance.SaveUserData((t) => { Debug.Log("save"); }, () => { Debug.Log("save fail"); });
        SaveLoadManager.Instance.SaveStageClearData(userData.StageIdx, 1, (t) => { Debug.Log("save"); }, () => { Debug.Log("save fail"); });
        GameSceneManager.Instance.LoadScene(selectStageScene);
    }
}