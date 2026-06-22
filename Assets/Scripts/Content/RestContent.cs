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
    private Button stageBtn = null;
    [SerializeField]
    private AssetReference selectStageScene = null;

    [SerializeField]
    private TextMeshProUGUI[] hpText = null;
    [SerializeField]
    private TextMeshProUGUI[] atkText = null;
    [SerializeField]
    private TextMeshProUGUI[] defText = null;
    [SerializeField]
    private TextMeshProUGUI[] dodgeText = null;
    [SerializeField]
    private TextMeshProUGUI[] spdText = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    public override async Task Enter()
    {
        await base.Enter();

        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = DataTableManager.Instance.GetStageData((uint)userData.StageIdx);
        LevelBaseData currLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level);
        LevelBaseData nextLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level + 1);
        int nextExp = 0;

        stageUI.SetText($"{sd.Stage} - {sd.SubStage}");

        if(nextLBD.Idx > 0)
        {
            nextExp = (int)nextLBD.NeedExp;
        }

        //test
        levelText.SetText($"Lv:{userData.Level}");
        expText.SetText($"Exp : {userData.Exp} / {nextExp}");
        
        lvupBtn.onClick.RemoveAllListeners();
        lvupBtn.onClick.AddListener(OnUserLevelUp);
        stageBtn.onClick.RemoveAllListeners();
        stageBtn.onClick.AddListener(OnGoStage);

        userData.SavedMapIdx = userData.MapIdx;
        userData.SavedStageIdx = userData.StageIdx;

        UpdateStatus(currLBD, nextLBD);

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

            LevelBaseData currLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level);
            LevelBaseData nextLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level + 1);
            int nextExp = 0;

            if (nextLBD != null)
            {
                nextExp = (int)nextLBD.NeedExp;
            }

            levelText.SetText($"Lv:{userData.Level}");
            expText.SetText($"Exp : {userData.Exp} / {nextExp}");

            UpdateStatus(currLBD, nextLBD);
        });
    }

    private void OnGoStage()
    {
        GameSceneManager.Instance.LoadScene(selectStageScene);
    }

    private void UpdateStatus(LevelBaseData currentLBD, LevelBaseData nextLBD)
    {
        UnitData playerData = DataTableManager.Instance.GetUnitData(Commons.Util.CreateDataIdx(DataTableType.UnitData, 1));

        if(currentLBD != null)
        {
            hpText[0].SetText($"maxHP : {playerData.MaxHp + currentLBD.MaxHp}");
            atkText[0].SetText($"atk : {playerData.Atk + currentLBD.Atk}");
            defText[0].SetText($"def : {playerData.Def + currentLBD.Def}");
            dodgeText[0].SetText($"dodge : {playerData.Dodge + currentLBD.Dodge}");
            spdText[0].SetText($"spd : {playerData.Spd + currentLBD.Spd}");
        }
        else
        {
            hpText[0].SetText($"maxHP : {playerData.MaxHp}");
            atkText[0].SetText($"atk : {playerData.Atk}");
            defText[0].SetText($"def : {playerData.Def}");
            dodgeText[0].SetText($"dodge : {playerData.Dodge}");
            spdText[0].SetText($"spd : {playerData.Spd}");
        }

        if(nextLBD != null)
        {
            hpText[1].SetText($"maxHP : {playerData.MaxHp + nextLBD.MaxHp}");
            atkText[1].SetText($"atk : {playerData.Atk + nextLBD.Atk}");
            defText[1].SetText($"def : {playerData.Def + nextLBD.Def}");
            dodgeText[1].SetText($"dodge : {playerData.Dodge + nextLBD.Dodge}");
            spdText[1].SetText($"spd : {playerData.Spd + nextLBD.Spd}");
        }
        else
        {
            hpText[1].SetText($"maxHP : {playerData.MaxHp}");
            atkText[1].SetText($"atk : {playerData.Atk}");
            defText[1].SetText($"def : {playerData.Def}");
            dodgeText[1].SetText($"dodge : {playerData.Dodge}");
            spdText[1].SetText($"spd : {playerData.Spd}");
        }
    }
}