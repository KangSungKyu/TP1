using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


public class RestContent : GameContent
{
    [SerializeField]
    private TextMeshProUGUI stageUI = null;
    [SerializeField]
    private TextMeshProUGUI levelText = null;
    [SerializeField]
    private TextMeshProUGUI expText = null;
    [SerializeField]
    private Button lvupBtn = null;
    [SerializeField]
    private AssetReference selectStageScene = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    public override async Task Enter()
    {
        await base.Enter();

        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = DataTableManager.Instance.GetStageData((uint)userData.StageIdx);
        LevelBaseData lbd = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level + 1);
        int nextExp = 0;

        stageUI.SetText($"{sd.Stage} - {sd.SubStage}");

        if(lbd.Idx > 0)
        {
            nextExp = (int)lbd.NeedExp;
        }

        //test
        levelText.SetText($"Lv:{userData.Level}");
        expText.SetText($"Exp : {userData.Exp} / {nextExp}");
        
        lvupBtn.onClick.RemoveAllListeners();
        lvupBtn.onClick.AddListener(OnUserLevelUp);

        userData.SavedMapIdx = userData.MapIdx;
        userData.SavedStageIdx = userData.StageIdx;

        SaveLoadManager.Instance.SaveUserData((json) =>
        {
            Debug.Log($"save current stage");
        }, () => { });

        GameNetworkManager.Instance.SaveStageClearData((uint)userData.StageIdx, 2, (json) => { });

        await Task.CompletedTask;
    }

    private void OnUserLevelUp()
    {
        //test
        LevelBaseData lbd = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level + 1);
        int nextExp = 0;

        if (lbd.Idx > 0)
        {
            nextExp = (int)lbd.NeedExp;
        }

        if(nextExp > 0 && userData.Exp >= nextExp)
        {
            userData.Exp -= nextExp;
            userData.Level += 1;
        }

        GameNetworkManager.Instance.UpdateUserLevel((json) =>
        {
            APIResponseData<(int Level, int Exp)> res = GameNetworkManager.Instance.CreateAPIResponseDataFromJson<(int Level, int Exp)>(json);

            if(res.data != default)
            {

            }

            LevelBaseData lbd = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level + 1);
            int nextExp = 0;

            if (lbd.Idx > 0)
            {
                nextExp = (int)lbd.NeedExp;
            }

            levelText.SetText($"Lv:{userData.Level}");
            expText.SetText($"Exp : {userData.Exp} / {nextExp}");
        });
    }
}