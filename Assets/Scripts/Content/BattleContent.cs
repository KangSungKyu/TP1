using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        //player stage progress to last save point(stage)
        //anim defeat ui
        //branch stage select ui and title scene by player selection

        Debug.Log($"stage defeat");

        PanelManager.GetPanel<StageResultPanel>("StageResultPanel").Show(false, () =>
        {
            battleStage.ReleaseStage();

            GameNetworkManager.Instance.UpdateDefeatStage((json) =>
            {
                APIResponseData<List<ClearData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<ClearData>>(json);

                if (res.data != null)
                {
                    stageClearData.ClearDatas = res.data.ToDictionary((o) => { return o.StageIdx; });

                    userData.Exp = 0;
                    userData.MapIdx = userData.SavedMapIdx;
                    userData.StageIdx = userData.SavedStageIdx;

                    SaveLoadManager.Instance.SaveUserData((t) => { Debug.Log("save"); }, (json) => { Debug.Log("save fail"); });

                    GameSceneManager.Instance.LoadScene(selectStageScene);
                }
            });
        });
    }

    private void OnStageClear()
    {
        //clear ui
        //calc reward
        //input map change (battle -> ui)
        //goto stage select ui(or scene)

        Debug.Log($"stage clear");

        PanelManager.GetPanel<StageResultPanel>("StageResultPanel").Show(true, () =>
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

            GameNetworkManager.Instance.UpdateClearStage(userData.StageIdx, (json) =>
            {
                APIResponseData<List<ClearData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<ClearData>>(json);

                if (res.data != null)
                {
                    stageClearData.ClearDatas = res.data.ToDictionary((o) => { return o.StageIdx; });

                    GameSceneManager.Instance.LoadScene(selectStageScene);
                }
            });
        });
    }
}