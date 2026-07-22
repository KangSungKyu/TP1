using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using UnityEngine.UI;


public class BattleContent : GameContent
{
    [SerializeField]
    private SpriteRenderer bgSprRenderer = null;
    [SerializeField]
    private TextMeshProUGUI stageUI = null;
    [SerializeField]
    private BattleStage battleStage = null;
    [SerializeField]
    private AssetReference selectStageScene = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    public override async Task Enter()
    {
        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = DataTableManager.Instance.GetStageData((uint)userData.StageIdx);
        Sprite bgSpr = await ResourceManager.Instance.LoadAssetAsyncTask<Sprite>(sd.BgSprite);

        bgSprRenderer.sprite = bgSpr;

        await base.Enter();

        stageUI.SetText($"{sd.Stage} - {sd.SubStage}");

        int monsterPoolCount = 0;
        int tilePoolCount = 0;
        int boardPoolCount = 0;

        HashSet<string> needAnim = new HashSet<string>();
        HashSet<string> needPort = new HashSet<string>();

        for (int i = 0; i < sd.MonsterIdx.Length; ++i)
        {
            MonsterData md = DataTableManager.Instance.GetMonsterData(sd.MonsterIdx[i]);

            if(md != null)
            {
                UnitData ud = DataTableManager.Instance.GetUnitData(md.UnitIdx);
                AnimationData ad = DataTableManager.Instance.GetAnimationData(ud.AnimGroupIdx);
                MonsterPatternData mpd = DataTableManager.Instance.GetMonsterPatternData(md.PatternIdx);

                int patternLen = mpd.SkillIdx.Count((o) => o > 0);

                needAnim.Add(ad.ControllerKey);
                needPort.Add($"{Commons.ResKey_PortraitUIs}[{Commons.ResKey_PortraitUIs}_{ud.PortraitIdx}]");

                tilePoolCount += (int)((md.BoardDefaultWidth * md.BoardDefaultHeight) * sd.MonsterCount[i]) * patternLen;
                monsterPoolCount += (int)sd.MonsterCount[i] * patternLen;
            }
        }

        tilePoolCount = tilePoolCount * 2; //buffer
        boardPoolCount = monsterPoolCount * 2; //buffer

        for (int i = 0; i < needAnim.Count; ++i)
        {
            await ResourceManager.Instance.LoadAssetAsyncTask<RuntimeAnimatorController>(needAnim.ElementAt(i));
        }

        for(int i = 0; i < needPort.Count; ++i)
        {
            await ResourceManager.Instance.LoadAssetAsyncTask<Sprite>(needPort.ElementAt(i));
        }

        await Factory.Instance.Init_UnitPoolAsync(monsterPoolCount);
        await Factory.Instance.Init_BoardPoolAsync(boardPoolCount, tilePoolCount);
        await Factory.Instance.Init_HitEffectPoolAsync(monsterPoolCount);
        await Factory.Instance.Init_TargetLinePoolAsync(1 + boardPoolCount);

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
        Debug.Log($"stage defeat");

        PanelManager.GetPanel<StageResultPanel>("StageResultPanel").Show(false, async () =>
        {
            battleStage.ReleaseStage();

            try
            {
                string json = await GameNetworkManager.Instance.UpdateDefeatStageAsync(this.GetCancellationTokenOnDestroy());
                APIResponseData<List<ClearData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<ClearData>>(json);

                if (res.data != null)
                {
                    stageClearData.ClearDatas = res.data.ToDictionary((o) => { return o.StageIdx; });

                    userData.Exp = 0;
                    userData.MapIdx = userData.SavedMapIdx;
                    userData.StageIdx = userData.SavedStageIdx;

                    await SaveLoadManager.Instance.SaveUserDataAsync(this.GetCancellationTokenOnDestroy());
                    await GameSceneManager.Instance.LoadSceneAsync(selectStageScene, CancellationToken.None);
                }
            }
            catch(System.Exception e)
            {
                Debug.LogError(e);
            }
        });
    }

    private void OnStageClear()
    {
        Debug.Log($"stage clear");

        PanelManager.GetPanel<StageResultPanel>("StageResultPanel").Show(true, async () =>
        {
            battleStage.ReleaseStage();

            StageData stageData = DataTableManager.Instance.GetStageData((uint)userData.StageIdx);
            uint totalExp = 0;

            for (int i = 0; i < stageData.MonsterIdx.Length; ++i)
            {
                if (stageData.MonsterIdx[i] > 0)
                {
                    MonsterData monsterData = DataTableManager.Instance.GetMonsterData(stageData.MonsterIdx[i]);
                    RewardData rewardData = DataTableManager.Instance.GetRewardData(stageData.RewardIdx[i]);

                    totalExp += (rewardData.Exp * stageData.MonsterCount[i]);
                }
            }

            userData.Exp += (int)totalExp;
            stageClearData.SetState(userData.StageIdx, 1);

            try
            {
                string json = await GameNetworkManager.Instance.UpdateClearStageAsync(userData.StageIdx, this.GetCancellationTokenOnDestroy());
                APIResponseData<List<ClearData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<ClearData>>(json);

                if (res.data != null)
                {
                    stageClearData.ClearDatas = res.data.ToDictionary((o) => { return o.StageIdx; });

                    await GameSceneManager.Instance.LoadSceneAsync(selectStageScene, CancellationToken.None);
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogError(ex);
            }
        });
    }
}