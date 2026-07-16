using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;


public class RestContent : GameContent
{
    [SerializeField]
    private Image bgImg = null;
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

    [SerializeField]
    private GameObject[] skillUI = null;

    [SerializeField]
    private GameObject[] skillSlotUI = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    private int[] selectedSkillDropdown = new int[] { 0, 0, 0 };

    public override async Task Enter()
    {
        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = DataTableManager.Instance.GetStageData((uint)userData.StageIdx);
        Sprite bgSpr = await ResourceManager.Instance.LoadAssetAsyncTask<Sprite>(sd.BgSprite);

        bgImg.sprite = bgSpr;

        await base.Enter();

        LevelBaseData currLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level);
        LevelBaseData nextLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level + 1);
        int nextExp = 0;

        stageUI.SetText($"{sd.Stage} - {sd.SubStage}");

        if(nextLBD != null)
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

        for(int i = 0; i < skillSlotUI.Length; ++i)
        {
            int id = i;
            skillSlotUI[i].transform.GetComponentInChildren<TMP_Dropdown>().onValueChanged.RemoveAllListeners();
            skillSlotUI[i].transform.GetComponentInChildren<TMP_Dropdown>().onValueChanged.AddListener((n) => selectedSkillDropdown[id] = n);
            skillSlotUI[i].transform.Find("UnEquipBtn").GetComponent<Button>()?.onClick.RemoveAllListeners();
            skillSlotUI[i].transform.Find("UnEquipBtn").GetComponent<Button>()?.onClick.AddListener(() => OnUnEquipSkill(id + 1));
        }

        UpdateStatus(currLBD, nextLBD);
        UpdateSkillPanel();
        UpdateSkillSlots();

        try
        {
            await SaveLoadManager.Instance.SaveUserDataAsync(this.GetCancellationTokenOnDestroy());
        }
        catch (Exception ex)
        {
            APIResponseData<DumpData> dump = GameNetworkManager.CreateAPIResponseDataFromJson<DumpData>(ex.Message);

            AlterMsgSystem.Instance.ShowMsg($"response : {dump.response}");

            Debug.LogError($"server log, {dump.response}");
        }

        try
        {
            string json = await GameNetworkManager.Instance.SaveStageClearDataAsync((uint)userData.StageIdx, 2, this.GetCancellationTokenOnDestroy());

            APIResponseData<ClearData> res = GameNetworkManager.CreateAPIResponseDataFromJson<ClearData>(json);

            if (res.data != null)
            {
                if (!stageClearData.ClearDatas.ContainsKey(res.data.StageIdx))
                {
                    stageClearData.ClearDatas.Add(res.data.StageIdx, res.data);
                }
                else
                {
                    stageClearData.ClearDatas[res.data.StageIdx] = res.data;
                }
            }
        }
        catch(Exception ex)
        {
            APIResponseData<DumpData> dump = GameNetworkManager.CreateAPIResponseDataFromJson<DumpData>(ex.Message);

            if(dump != null)
            {
                AlterMsgSystem.Instance.ShowMsg($"error : {dump.response}");
            }
        }

        await Task.CompletedTask;
    }

    private async void OnUserLevelUp()
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

        try
        {
            string json = await GameNetworkManager.Instance.UpdateUserLevelAsync(this.GetCancellationTokenOnDestroy());
            APIResponseData<(int Level, int Exp)> res = GameNetworkManager.CreateAPIResponseDataFromJson<(int Level, int Exp)>(json);

            if (res.data != default)
            {
                LevelBaseData currLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level);
                LevelBaseData nextLBD = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level + 1);

                nextExp = 0;

                if (nextLBD != null)
                {
                    nextExp = (int)nextLBD.NeedExp;
                }

                levelText.SetText($"Lv:{userData.Level}");
                expText.SetText($"Exp : {userData.Exp} / {nextExp}");

                UpdateStatus(currLBD, nextLBD);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
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

    private void UpdateSkillPanel()
    {
        List<int> haveList = SaveLoadManager.Instance.UserSkillData.SkillSlots.Select((s) => s.SkillIdx).ToList();
        List<int> allList = DataTableManager.Instance.GetDB<SkillDataForm>(DataTableType.SkillData).DB
            .Where((s)=>s.Key != Commons.Util.CreateDataIdx(DataTableType.SkillData, 1) && s.Value.RequireLevel <= userData.Level)
            .Select((s) => (int)s.Key)
            .ToList();
        List<int> rndList = allList.Except(haveList).Shuffle().ToList();
        
        for(int i = 0; i < skillUI.Length; ++i)
        {
            if(i < rndList.Count)
            {
                int skillIdx = rndList[i];
                SkillData sd = DataTableManager.Instance.GetSkillData((uint)skillIdx);
                int quota = sd != null ? sd.Quota : 0;

                skillUI[i].gameObject.SetActive(true);
                skillUI[i].transform.Find("NameText").GetComponent<TextMeshProUGUI>()?.SetText($"skill:{skillIdx}");
                skillUI[i].transform.Find("QuotaText").GetComponent<TextMeshProUGUI>()?.SetText($"qt:{quota}");
                skillUI[i].transform.GetComponentInChildren<Button>()?.onClick.RemoveAllListeners();
                skillUI[i].transform.GetComponentInChildren<Button>()?.onClick.AddListener(() => OnBuySkill(skillIdx));
            }
            else
            {
                skillUI[i].gameObject.SetActive(false);
            }
        }
    }

    private async void OnBuySkill(int skillIdx)
    {
        try
        {
            string json = await GameNetworkManager.Instance.UpdateBuyUserSkillAsync(skillIdx, this.GetCancellationTokenOnDestroy());
            APIResponseData<List<SkillSlotData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<SkillSlotData>>(json);

            if (res.data != null)
            {
                SaveLoadManager.Instance.UserSkillData.SkillSlots = res.data;
            }

            UpdateSkillPanel();
            UpdateSkillSlots();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private void UpdateSkillSlots()
    {
        for (int i = 0; i < skillSlotUI.Length; ++i)
        {
            TextMeshProUGUI skillText = skillSlotUI[i].transform.Find("EquipedText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI quotaText = skillSlotUI[i].transform.Find("QuotaText").GetComponent<TextMeshProUGUI>();
            Button equipBtn = skillSlotUI[i].transform.Find("EquipBtn").GetComponent<Button>();
            TMP_Dropdown skillDd = skillSlotUI[i].transform.GetComponentInChildren<TMP_Dropdown>();

            skillDd.ClearOptions();
            equipBtn.onClick.RemoveAllListeners();

            List<int> ides = SaveLoadManager.Instance.UserSkillData.SkillSlots.Where((o) => o.Slot <= 0).Select((s) => s.SkillIdx).ToList();
            List<string> addDdOd = SaveLoadManager.Instance.UserSkillData.SkillSlots.Where((o) => o.Slot <= 0).Select((s) => $"{s.SkillIdx}").ToList();
            int slot = i + 1;
            int idx = i;
            int curIdx = SaveLoadManager.Instance.UserSkillData.GetSkillIdx(slot);
            SkillData sd = DataTableManager.Instance.GetSkillData((uint)curIdx);
            int quota = sd != null ? sd.Quota : 0;

            skillDd.AddOptions(addDdOd);
            skillText.SetText($"skill : {curIdx}");
            quotaText.SetText($"qt:{quota}");
            equipBtn.onClick.AddListener(() => OnEqiupSkill(ides[selectedSkillDropdown[idx]], slot)); //equip selected dropdown
        }
    }

    private async void OnEqiupSkill(int skillIdx, int slot)
    {
        try
        {
            string json = await GameNetworkManager.Instance.UpdateEquipUserSkillAsync(skillIdx, slot, this.GetCancellationTokenOnDestroy());
            APIResponseData<List<SkillSlotData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<SkillSlotData>>(json);

            if (res.data != null)
            {
                SaveLoadManager.Instance.UserSkillData.SkillSlots = res.data;
            }

            UpdateSkillSlots();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private async void OnUnEquipSkill(int slot)
    {
        try
        {
            string json = await GameNetworkManager.Instance.UpdateUnEquipUserSkillAsync(slot, this.GetCancellationTokenOnDestroy());
            APIResponseData<List<SkillSlotData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<SkillSlotData>>(json);

            if (res.data != null)
            {
                SaveLoadManager.Instance.UserSkillData.SkillSlots = res.data;
            }

            UpdateSkillSlots();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }
}