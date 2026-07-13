using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static Commons;

//atb-based
//유저는 행동 가능 상태일때 퍼즐을 푸는 유예시간이 주어짐
//적은 행동 가능 상태일때 공격(+패턴)을 실행함
//적의 공격이 실제로 실행되는 시점은 연결된 퍼즐의 유예시간이 끝났을때
//퍼즐은 완성하거나 유예시간이 지났을때 갱신
//퍼즐이 생성되는기준
//유저가 공격하기 위한 고정 생성 (적 1개당 1퍼즐)
//적이 공격하기 전에 대처할 기회를 주는 퍼즐 1개 (휘발성, 적 공격 패턴 시전마다 추가 갱신)
//총 퍼즐의 양은 적 * 유저 공격 퍼즐 + a(적의 공격 빈도에 따라 유동적으로, 단 전체적인 atb 길이에 비해 방어 퍼즐의 유예시간은 짧게 유지)
public class BattleStage : MonoBehaviour
{

    [SerializeField]
    private RectTransform uiPoolTempContainer = null;
    [SerializeField]
    private RectTransform hpBarContainer = null;
    [SerializeField]
    private RectTransform[] boardContainer_RT = null;
    [SerializeField]
    private RectTransform[] boardContainer_U_RT = null;
    [SerializeField]
    private Transform[] boardContainer = null;
    [SerializeField]
    private Transform[] boardContainer_U = null;
    [SerializeField]
    private GameObject PlayerSpawnGO = null;
    [SerializeField]
    private GameObject MonsterSpawnGO = null;
    [SerializeField]
    private RectTransform atbGaugeBG = null;
    [SerializeField]
    private Image pageCursorUI = null;

    private bool isBattleActive = false;

    private ReactiveProperty<int> boardPageCursor = new ReactiveProperty<int>(0); //0 right, 1 left
    private ReactiveProperty<int>[] boardCursor = new ReactiveProperty<int>[] { new ReactiveProperty<int>(-1), new ReactiveProperty<int>(-1) };
    private ReactiveProperty<int> playerCount = new ReactiveProperty<int>(0);
    private ReactiveProperty<int> monsterCount = new ReactiveProperty<int>(0);
    private List<UnitBase> unitList = new List<UnitBase>();
    private List<BBoard>[] boardList = new List<BBoard>[] { new List<BBoard>(), new List<BBoard>() };
    private Queue<UnitBase> readyQueue = new Queue<UnitBase>();
    private Queue<PuzzleResult> resultQueue = new Queue<PuzzleResult>();
    private List<Coroutine> resultQueueList = new List<Coroutine>();

    private PlayerInput playerInput = null;

    private Coroutine coBattleLoop = null;
    private event System.Action onStageDefeat = null;
    private event System.Action onStageClear = null;

    private List<Coroutine> coGC = new List<Coroutine>();
    
    public void InitStage(UserData userData, StageData stageData)
    {
        isBattleActive = true;

        if (unitList.Count <= 0)
        {
            unitList.Add(Factory.Instance.GetPlayerUnit(transform, PlayerSpawnGO.transform.position) as PlayerUnit);
        }

        LevelBaseData lbd = DataTableManager.Instance.GetLevelBaseData((uint)userData.Level);

        PlayerUnit player = unitList[0] as PlayerUnit;

        player.LoadFromSO(Commons.Util.CreateDataIdx(DataTableType.UnitData, 1)); 
        player.SetLevelBase(lbd);
        player.SetHPUI(Factory.Instance.GetHPUI(hpBarContainer));
        player.Subscribe_HP((v) => OnPlayerDeath(player, v));
        player.OnATBReady.Subscribe((o) => readyQueue.Enqueue(o)).AddTo(player);

        Image playerPort = Factory.Instance.GetPortraitUI(uiPoolTempContainer);

        playerPort.rectTransform.SetParent(atbGaugeBG);
        player.SetPortraitUI(playerPort);
        player.Subscribe_ATBGauge((v) => OnATBGauge(playerPort, player.ATBRatio));

        //monster position -> start + up-down graph
        Vector3 monsterStartPos = MonsterSpawnGO.transform.position;
        float prevX = 0.0f;
        float spawnY = 0.0f;
        int spawnRow = 3;

        for (int i = 0; i < stageData.MonsterIdx.Length; ++i)
        {
            for (int j = 0; j < stageData.MonsterCount[i]; ++j)
            {
                MonsterUnit monsterUnit = Factory.Instance.GetMonsterUnit(transform, MonsterSpawnGO.transform.position) as MonsterUnit;

                if (monsterUnit != null)
                {
                    spawnY = ((unitList.Count - 1) % spawnRow - (spawnRow / 2)) * 1.0f;
                    //test
                    monsterUnit.transform.position = new Vector3(monsterStartPos.x + prevX, monsterStartPos.y + spawnY, 0.0f);

                    monsterUnit.SetTarget(player);
                    monsterUnit.LoadFromSO(stageData.MonsterIdx[i]);
                    monsterUnit.SetHPUI(Factory.Instance.GetHPUI(hpBarContainer));
                    monsterUnit.Subscribe_HP(OnMonsterDeath);
                    monsterUnit.OnATBReady.Subscribe((o) => readyQueue.Enqueue(o)).AddTo(monsterUnit);

                    BBoard board = Factory.Instance.GetBoard(BBoardType.Offensive, boardContainer[0], (int)monsterUnit.MonsterData.BoardDefaultWidth, (int)monsterUnit.MonsterData.BoardDefaultHeight, monsterUnit.Spd);

                    board.SetOwner(monsterUnit);
                    monsterUnit.AddBoard(board);

                    boardList[0].Add(board);
                    unitList.Add(monsterUnit);

                    Image monsterPort = Factory.Instance.GetPortraitUI(uiPoolTempContainer);

                    monsterPort.rectTransform.SetParent(atbGaugeBG);
                    monsterUnit.SetPortraitUI(monsterPort);
                    monsterUnit.Subscribe_ATBGauge((v) => OnATBGauge(monsterPort, monsterUnit.ATBRatio));

                    Vector2 monsterSize = monsterUnit.transform.Find("Renderer/Sprite").GetComponent<SpriteRenderer>().bounds.size;

                    prevX += monsterSize.x * 0.5f;
                }
            }
        }

        playerCount.Value = unitList.Count((o) => o is PlayerUnit);
        monsterCount.Value = unitList.Count((o) => o is MonsterUnit);

        playerCount.Subscribe(OnChangedPlayerCount).AddTo(this);
        monsterCount.Subscribe(OnChangedMonsterCount).AddTo(this);

        SortingBoard(0);
        DownToBoard(0);
        RepositionUpperBoardList(0);
        RepositionDownBoardList(0);

        for (int i = 0; i < boardList[0].Count; i++)
        {
            boardList[0][i].InitBoard(SaveLoadManager.Instance.UserSkillData.GetEquipedSkills());
        }

        boardCursor[0].Value = 0;
        boardCursor[1].Value = 0;
        boardPageCursor.Value = 0;

        boardCursor[0].Subscribe((v)=>{ OnChangedBoardCursor(boardPageCursor.Value, v); }).AddTo(this);
        boardCursor[1].Pairwise().Subscribe((pair)=> { OnPrevBoardCursor(boardPageCursor.Value, pair.Previous); OnChangedBoardCursor(boardPageCursor.Value, pair.Current); }).AddTo(this);
        boardPageCursor.Subscribe(OnChangedBoardPageCursor).AddTo(this);

        readyQueue.Clear();

        ClearCoroutineGC();

        if (coBattleLoop == null)
        {
            coBattleLoop = StartCoroutine(IEBattleLoop());
        }
    }

    public void ReleaseStage()
    {
        ClearCoroutineGC();

        if(coBattleLoop != null)
        {
            StopCoroutine(coBattleLoop);
            coBattleLoop = null;
        }

        isBattleActive = false;

        for(int i = 0; i < unitList.Count; ++i)
        {
            unitList[i]?.Release();

            if (unitList[i] is PlayerUnit)
            {
                Factory.Instance.ReleasePlayerUnit(unitList[i] as PlayerUnit);
            }
            else
            {
                Factory.Instance.ReleaseMonsterUnit(unitList[i] as MonsterUnit);
            }
        }

        unitList.Clear();
        readyQueue.Clear();

        for (int i = 0; i < resultQueueList.Count; ++i)
        {
            if (resultQueueList[i] != null)
            {
                StopCoroutine(resultQueueList[i]);
            }

            resultQueueList[i] = null;
        }

        resultQueueList.Clear();

        for (int i = 0; i < boardList.Length; ++i)
        {
            for(int j = 0; j < boardList[i].Count; ++j)
            {
                boardList[i][j].ReleaseBoard();
                Factory.Instance.ReleaseBoard(boardList[i][j]);
            }

            boardList[i].Clear();
        }

        for(int i = 0; i < hpBarContainer.childCount; ++i)
        {
            HpBar hpbar = hpBarContainer.GetChild(i)?.GetComponent<HpBar>();

            Factory.Instance.ReleaseHPUI(hpbar);
        }

        for(int i = 0; i < atbGaugeBG.childCount; ++i)
        {
            Image port = atbGaugeBG.GetChild(i)?.GetComponent<Image>();

            Factory.Instance.ReleasePortraitUI(port);
        }

        playerInput.Battle.Disable();
        onStageDefeat = null;
        onStageClear = null;

        Factory.Instance.Release();
    }

    public void ClearCoroutineGC()
    {
        for(int i = 0; i < coGC.Count; ++i)
        {
            if(coGC[i] != null)
            {
                StopCoroutine(coGC[i]);
                coGC[i] = null;
            }
        }

        coGC.Clear();
    }

    public void OnStageDefeat(System.Action act)
    {
        onStageDefeat += act;
    }

    public void OnStageClear(System.Action act)
    {
        onStageClear += act;
    }

    public void OnPuzzleCompleted(PuzzleResult resultData)
    {
        resultData.ResultType = PuzzleResultType.Completed;

        if(resultData.Attacker is PlayerUnit playerUnit)
        {
            List<(BTileType type, uint idx)> tileTypeList = resultData.CurrentBoard.GetTileTypeListInPath();
            bool findSkillType = tileTypeList.Any((o) => o.type == BTileType.Skill && o.idx != 0);
            bool findAttackType = tileTypeList.Any((o) => o.type == BTileType.Attack);

            //priority skill > attack
            if (findSkillType)
            {
                List<SkillData> sdList = tileTypeList
                    .Where((o) => o.type == BTileType.Skill && o.idx != 0)
                    .Select((s) => DataTableManager.Instance.GetSkillData(s.idx))
                    .Where((o) => o != null).ToList();

                if (sdList != null)
                {
                    resultData.SkillList.AddRange(sdList);

                    UsedUserSkillData[] usedSkillList = sdList.GroupBy((o) => o.Idx).Select((s) => new UsedUserSkillData { SkillIdx = (int)s.Key, Count = s.Count() }).ToArray();
                    
                    GameNetworkManager.Instance.UpdateUsedUserSkill(usedSkillList, (json) =>
                    {
                        APIResponseData<List<SkillSlotData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<SkillSlotData>>(json);

                        if(res.data != null)
                        {
                            SaveLoadManager.Instance.UserSkillData.SkillSlots = res.data;
                        }
                    },
                    (json) =>
                    {
                        APIResponseData<DumpData> dump = GameNetworkManager.CreateAPIResponseDataFromJson<DumpData>(json);

                        if(dump.response != ResponseType.Success)
                        {

                        }
                    });
                }
            }

            if (!findSkillType && findAttackType)
            {
                SkillData sdAtk = DataTableManager.Instance.GetSkillData((uint)playerUnit.Info.AttackIdx);

                if (sdAtk != null)
                {
                    resultData.SkillList.Add(sdAtk);
                }
            }

            if(resultData.CurrentBoard.Owner != null)
            {
                resultData.TargetList = new List<UnitBase>();

                resultData.TargetList.Add(resultData.CurrentBoard.Owner);
            }

            int maxTargetCount = Mathf.Min(monsterCount.Value, resultData.SkillList.Where((o) => o.Type == SkillType.Damaged && o.TargetType == SkillTargetType.Multiple).Select((o) => (int)o.TargetCount).DefaultIfEmpty(0).Max());

            if(maxTargetCount > 0)
            {
                resultData.TargetList.AddRange(unitList.Where((o) => { return o is MonsterUnit && o != playerUnit && o != resultData.CurrentBoard.Owner; }).Take(maxTargetCount).Select((s) => { return s; }).ToList());
            }
        }
        else if(resultData.Attacker is MonsterUnit monsterUnit)
        {

        }

        resultQueue.Enqueue(resultData);
    }

    public void OnPuzzleExpired(PuzzleResult resultData)
    {
        resultData.ResultType = PuzzleResultType.Expired;

        if(resultData.Attacker is PlayerUnit playerUnit)
        {

        }
        else if(resultData.Attacker is MonsterUnit monsterUnit)
        {
            uint patternSkillIdx = monsterUnit.GetCurrentPattern();

            if (patternSkillIdx != 0)
            {
                resultData.SkillList.Add(DataTableManager.Instance.GetSkillData(patternSkillIdx));
            }
            else
            {
                SkillData sdAtk = DataTableManager.Instance.GetSkillData((uint)monsterUnit.Info.AttackIdx);

                if (sdAtk != null)
                {
                    resultData.SkillList.Add(sdAtk);
                }
            }

            resultData.TargetList.Add(monsterUnit.TargetUnit);
        }

        resultQueue.Enqueue(resultData);
    }

    protected void Awake()
    { 
        if(playerInput == null)
        {
            playerInput = new PlayerInput();
        }

        playerInput.Battle.PuzzleDrawUp.performed += ctx => OnPuzzleDrawUp();
        playerInput.Battle.PuzzleDrawDown.performed += ctx => OnPuzzleDrawDown();
        playerInput.Battle.PuzzleDrawLeft.performed += ctx => OnPuzzleDrawLeft();
        playerInput.Battle.PuzzleDrawRight.performed += ctx => OnPuzzleDrawRight();

        playerInput.Battle.PuzzleSelectL.performed += ctx => OnPuzzleSelectL(boardPageCursor.Value);
        playerInput.Battle.PuzzleSelectR.performed += ctx => OnPuzzleSelectR(boardPageCursor.Value);

        playerInput.Battle.PuzzlePageL.performed += ctx => OnPuzzlePageL();
        playerInput.Battle.PuzzlePageR.performed += ctx => OnPuzzlePageR();

        playerInput.Battle.PuzzleReset.performed += ctx => OnPuzzleReset();

        playerInput.Battle.Enable();
    }

    protected void OnDestroy()
    {
        if(playerInput != null)
        {
            playerInput.Battle.Disable();
        }
    }

    private void Update()
    {
        if(resultQueue.Count > 0)
        {
            PuzzleResult result = resultQueue.Dequeue();
            Coroutine co = StartCoroutine(IEProcPuzzleResult(result));

            resultQueueList.Add(co);
        }


        //for(int i = 0; i < unitList.Count; ++i)
        //{
        //    Bounds bd = unitList[i].GetUnitBounds();
        //
        //    Debug.DrawLine(new Vector2(bd.min.x, bd.min.y), new Vector3(bd.max.x, bd.min.y), Color.red);
        //    Debug.DrawLine(new Vector2(bd.max.x, bd.min.y), new Vector3(bd.max.x, bd.max.y), Color.red);
        //    Debug.DrawLine(new Vector2(bd.max.x, bd.max.y), new Vector3(bd.min.x, bd.max.y), Color.red);
        //    Debug.DrawLine(new Vector2(bd.min.x, bd.max.y), new Vector3(bd.min.x, bd.min.y), Color.red);
        //}

        //test func
        //if(Input.GetKeyDown(KeyCode.K))
        //{
        //    if(unitList.Count > 1)
        //    {
        //        boardCursor[0].Value = 0;
        //        unitList.Skip(1).First()?.ApplyDamage(99999f);
        //    }
        //}
        //else if(Input.GetKeyDown(KeyCode.P))
        //{
        //    if(unitList.Count > 0)
        //    {
        //        unitList.FirstOrDefault()?.ApplyDamage(99999f);
        //    }
        //}
    }

    private IEnumerator IEBattleLoop()
    {
        while(isBattleActive)
        {
            foreach(var unit in unitList)
            {
                if(unit.ATBRatio < 1.0f)
                {
                    unit.AddATBTick(Time.deltaTime);
                }
            }

            while(readyQueue.Count > 0)
            {
                UnitBase readyUnit = readyQueue.Dequeue();

                Coroutine co = StartCoroutine(IERunUnitTurn(readyUnit));

                coGC.Add(co);

                yield return co;
            }

            yield return null;
        }
    }

    private IEnumerator IERunUnitTurn(UnitBase unit)
    {
        if (unit == null)
            yield break;

        if (unit is PlayerUnit playerUnit)
        {
            for(int i = 0; i < boardList[0].Count; ++i)
            {
                BBoard board = boardList[0][i];

                if(board != null)
                {
                    if(board.Width <= 0 || board.Height <= 0)
                    {
                        Factory.Instance.ReleaseBoard(board);
                        Debug.LogError($"invalid board, {i}");
                        yield break;
                    }

                    board.ForceStopBoardTimer();
                    ClearBoard(board);
                    board.FillBoard(BBoardType.Offensive, SaveLoadManager.Instance.UserSkillData.GetEquipedSkills());
                    board.PlayFadeCover();

                    UnitBase defender = board.Owner;
                    PuzzleResult resultData = new PuzzleResult()
                    {
                        ResultType = PuzzleResultType.NotYet,
                        CurrentBoard = board,
                        Attacker = playerUnit,
                        TargetList = null,
                        SkillList = new List<SkillData>(),
                    };

                    board.StartBoardTimer(() => OnPuzzleExpired(resultData));
                    board.SubscribeOnPathComplete(() => OnPuzzleCompleted(resultData));
                }
            }

            SortingBoard(0);
            RepositionUpperBoardList(0);
            RepositionDownBoardList(0);
        }
        else if (unit is MonsterUnit monsterUnit)
        {
            PlayerUnit player = monsterUnit.TargetUnit as PlayerUnit;

            if (player != null)
            {
                int prevListCount = boardList[1].Count;
                BBoard tempBoard = Factory.Instance.GetBoard(BBoardType.Defensive, boardContainer[1], monsterUnit.GetBoardWidth(), monsterUnit.GetBoardHeight(), monsterUnit.Spd * 0.85f);

                if (tempBoard != null)
                {
                    tempBoard.SetOwner(monsterUnit);
                    boardList[1].Add(tempBoard);
                    tempBoard.InitBoard();
                    tempBoard.PlayFadeCover();
                    tempBoard.FillBoard(BBoardType.Defensive);
                    monsterUnit.AddBoard(tempBoard);

                    //boardPageCursor.Value = 1;
                    //boardCursor[1].SetValueAndForceNotify(boardList[1].Count - 1);

                    SortingBoard(1);
                    RepositionUpperBoardList(1);
                    RepositionDownBoardList(1);

                    PuzzleResult resultData = new PuzzleResult()
                    {
                        ResultType = PuzzleResultType.NotYet,
                        CurrentBoard = tempBoard,
                        Attacker = monsterUnit,
                        TargetList = new List<UnitBase>(),
                        SkillList = new List<SkillData>(),
                    };

                    tempBoard.StartBoardTimer(() => OnPuzzleExpired(resultData));
                    tempBoard.SubscribeOnPathComplete(() => OnPuzzleCompleted(resultData));
                }
            }
        }

        unit.ResetATB();
    }

    private IEnumerator IEProcPuzzleResult(PuzzleResult result)
    {
        if(result.ResultType == PuzzleResultType.Completed)
        {
            if(result.Attacker is PlayerUnit playerUnit)
            {
                if (playerUnit.TargetUnit != null)
                {
                    TestPrintPointList();

                    ApplyStatusData applyAttackerStatus = result.CurrentBoard.GetApplyStatusFromPath();

                    for(int i = 0; i < result.TargetList.Count; ++i)
                    {
                        ApplyStatusData applyDefenderStatus = default;
                        UnitActionData uad = new UnitActionData(UnitActionType.Attack, null, () =>
                        {
                            UnitCalculator.DamageResult damageResult = UnitCalculator.ApplyDamage(result.Attacker, result.TargetList[i], applyAttackerStatus, applyDefenderStatus, result.SkillList.ToArray());

                            result.TargetList[i].ClearApplyStatus();

                            DamageFont df = Factory.Instance.GetDamageFont(transform, damageResult.Damage);
                            float hheight = result.TargetList[i].GetUnitBounds().size.y * 0.5f;
                            Vector3 tarPos = result.TargetList[i].transform.position;

                            if(df != null)
                            {
                                df.transform.position = tarPos + new Vector3(0.0f, hheight, 0.0f);

                                if (damageResult.Type == DamageResultType.Damaged)
                                {
                                    df.SetText($"{damageResult.Damage:0}", () => Factory.Instance.ReleaseDamageFont(df));
                                }
                                else if (damageResult.Type == DamageResultType.Dodge)
                                {
                                    //dodge action
                                    df.SetText($"Dodge!", () => Factory.Instance.ReleaseDamageFont(df));
                                }
                            }

                            HitEffect hitEffect = Factory.Instance.GetHitEffect(transform);

                            if(hitEffect != null)
                            {
                                hitEffect.PlayEffect(tarPos, () => Factory.Instance.ReleaseHitEffect(hitEffect));
                            }
                        });

                        Coroutine co = StartCoroutine(result.Attacker.IEPlayAction(uad));

                        coGC.Add(co);

                        yield return co;
                    }

                    playerUnit.ClearApplyStatus();

                    result.CurrentBoard.ForceStopBoardTimer();
                    ClearBoard(result.CurrentBoard);
                    result.CurrentBoard.FillBoard(BBoardType.Offensive, SaveLoadManager.Instance.UserSkillData.GetEquipedSkills());
                    result.CurrentBoard.PlayFadeCover();
                }
            }
            else if(result.Attacker is MonsterUnit monsterUnit)
            {
                PlayerUnit player = result.Attacker.TargetUnit as PlayerUnit;

                if(player != null)
                {
                    ApplyStatusData applyDefenderStatus = result.CurrentBoard.GetApplyStatusFromPath();

                    player.ApplyStatus(applyDefenderStatus);
                    result.CurrentBoard.ForceBoardTimeOver();
                    ClearBoard(result.CurrentBoard);

                    if (boardList[1].Count <= 0)
                    {
                        boardPageCursor.Value = 0;
                    }
                    else
                    {
                        OnPuzzleSelectR(1);
                        OnChangedBoardCursor(1, boardCursor[1].Value);
                    }
                }
            }
        }
        else if(result.ResultType == PuzzleResultType.Expired)
        {
            if(result.Attacker is PlayerUnit playerUnit)
            {
                ClearBoard(result.CurrentBoard);
                result.CurrentBoard.FillBoard(BBoardType.Offensive, SaveLoadManager.Instance.UserSkillData.GetEquipedSkills());
                result.CurrentBoard.PlayFadeCover();
            }
            else if(result.Attacker is MonsterUnit monsterUnit)
            {
                // gather statuses from path
                ApplyStatusData applyAttackerStatus = default;
                ApplyStatusData applyDefenderStatus = result.CurrentBoard.GetApplyStatusFromPath();

                for(int i = 0; i < result.TargetList.Count; ++i)
                {
                    UnitActionData uad = new UnitActionData(UnitActionType.Attack, null, () =>
                    {
                        UnitCalculator.DamageResult damageResult = UnitCalculator.ApplyDamage(result.Attacker, result.TargetList[i], applyAttackerStatus, applyDefenderStatus, result.SkillList.ToArray());

                        result.TargetList[i].ClearApplyStatus();

                        DamageFont df = Factory.Instance.GetDamageFont(transform, damageResult.Damage);
                        Vector3 tarPos = result.TargetList[i].transform.position;

                        if (df != null)
                        {
                            float hwidth = result.TargetList[i].GetUnitBounds().size.x * 0.5f;
                            float hheight = result.TargetList[i].GetUnitBounds().size.y * 0.5f;
                            df.transform.position = tarPos + new Vector3(UnityEngine.Random.Range(-hwidth, hwidth), hheight, 0.0f);

                            if (damageResult.Type == DamageResultType.Damaged)
                            {
                                df.SetText($"{damageResult.Damage:0}", () => Factory.Instance.ReleaseDamageFont(df));
                            }
                            else if (damageResult.Type == DamageResultType.Dodge)
                            {
                                //dodge action
                                df.SetText($"Dodge!", () => Factory.Instance.ReleaseDamageFont(df));
                            }
                        }

                        HitEffect hitEffect = Factory.Instance.GetHitEffect(transform);

                        if (hitEffect != null)
                        {
                            hitEffect.PlayEffect(tarPos, () => Factory.Instance.ReleaseHitEffect(hitEffect));
                        }
                    });

                    Coroutine co = StartCoroutine(result.Attacker.IEPlayAction(uad));

                    coGC.Add(co);

                    yield return co;
                }

                monsterUnit.ClearApplyStatus();
                ClearBoard(result.CurrentBoard);
                result.CurrentBoard.ReleaseBoard();

                int idx = boardList[1].IndexOf(result.CurrentBoard);

                if (idx >= 0)
                {
                    boardList[1].RemoveAt(idx);
                }

                monsterUnit.DelBoard(result.CurrentBoard);

                if (boardList[1].Count <= 0)
                {
                    boardPageCursor.Value = 0;
                }
                else
                {
                    OnPuzzleSelectR(1);
                    OnChangedBoardCursor(1, boardCursor[1].Value);
                }
            }
        }
    }

    private void ClearBoard(BBoard board)
    {
        board.ClearBoard();
        board.ClearDrawLine();
    }

    private void OnPuzzleReset()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (currentBoardCursor > -1)
        {
            boardList[boardPageCursor.Value][currentBoardCursor].ResetDrawLine();
        }
    }

    private void OnPuzzlePageR()
    {
        if (boardList[0].Count > 0)
        {
            boardPageCursor.Value = 0;
        }
    }

    private void OnPuzzlePageL()
    {
        if (boardList[1].Count > 0)
        {
            boardPageCursor.Value = 1;
        }
    }

    private void OnPuzzleSelectR(int pageCursor)
    {
        int currentBoardCursor = boardCursor[pageCursor].Value;

        if (currentBoardCursor < boardList[pageCursor].Count)
        {
            currentBoardCursor += 1;
        }

        if (currentBoardCursor >= boardList[pageCursor].Count)
        {
            currentBoardCursor = 0;
        }

        boardCursor[pageCursor].Value = currentBoardCursor;
    }

    private void OnPuzzleSelectL(int pageCursor)
    {
        int currentBoardCursor = boardCursor[pageCursor].Value;

        if (currentBoardCursor > 0)
        {
            currentBoardCursor -= 1;
        }

        if (currentBoardCursor < 0)
        {
            currentBoardCursor = boardList[pageCursor].Count - 1;
        }

        boardCursor[pageCursor].Value = currentBoardCursor;
    }

    private void OnPuzzleDrawRight()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            boardList[boardPageCursor.Value][currentBoardCursor].SetDirection(1, 0);
        }
    }

    private void OnPuzzleDrawLeft()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            boardList[boardPageCursor.Value][currentBoardCursor].SetDirection(-1, 0);
        }
    }

    private void OnPuzzleDrawDown()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            boardList[boardPageCursor.Value][currentBoardCursor].SetDirection(0, -1);
        }
    }

    private void OnPuzzleDrawUp()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            boardList[boardPageCursor.Value][currentBoardCursor].SetDirection(0, 1);
        }
    }

    private void TestPrintPointList()
    {
        /*
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;
        BBoard selectedBoard = boardList[boardPageCursor.Value][currentBoardCursor];

        if (selectedBoard.DrawState == BBoardDrawState.Finished)
        {
            List<Vector2Int> pointList = selectedBoard.PointList;
            StringBuilder sb = new StringBuilder();

            sb.Append("Point List: ");

            for (int i = 0; i < pointList.Count; i++)
            {
                BTile btile = null;

                if (selectedBoard.TryGetTile(pointList[i].x, pointList[i].y, out btile))
                {
                    sb.Append($"[{btile.type}] ");
                }
            }

            Debug.Log(sb.ToString());
        }
        //*/
    }

    private void OnPlayerDeath(PlayerUnit player, float hp)
    {
        if (hp <= 0)
        {
            System.Action act = () =>
            {
                player.Release();
                Factory.Instance.ReleasePortraitUI(player.PortraitUI);
                Factory.Instance.ReleasePlayerUnit(player);
                Factory.Instance.ReleaseHPUI(player.HpUI);

                unitList.Remove(player);

                playerCount.Value = unitList.Count((o) => o is PlayerUnit);
            };

            if(player != null)
            {
                Coroutine co = StartCoroutine(player.IEPlayAction(new UnitActionData(UnitActionType.Death, null, act)));

                coGC.Add(co);
            }
        }
    }

    private void OnMonsterDeath(float hp)
    {
        if (hp <= 0)
        {
            int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;
            BBoard currentBoard = boardList[boardPageCursor.Value][currentBoardCursor];
            MonsterUnit monster = (MonsterUnit)currentBoard.Owner;

            System.Action act = () =>
            {
                if (monster != null)
                {
                    monster.Release();
                    monster.DelDefensivekBoardList();
                    Factory.Instance.ReleasePortraitUI(monster.PortraitUI);
                    currentBoard.ReleaseBoard();
                    boardList[boardPageCursor.Value].Remove(currentBoard);
                    Factory.Instance.ReleaseBoard(currentBoard);
                    Factory.Instance.ReleaseMonsterUnit(monster);
                    Factory.Instance.ReleaseHPUI(monster.HpUI);
                    unitList.Remove(monster);

                    boardCursor[boardPageCursor.Value].SetValueAndForceNotify(boardList[boardPageCursor.Value].Count - 1);

                    monsterCount.Value = unitList.Count((o) => o is MonsterUnit);
                }
            };

            if (monster != null)
            {
                Coroutine co = StartCoroutine(monster.IEPlayAction(new UnitActionData(UnitActionType.Death, null, act)));

                coGC.Add(co);
            }
        }
    }

    private void OnATBGauge(Image port, float ratio)
    {
        float bgHWidth = (atbGaugeBG.rect.xMax - atbGaugeBG.rect.xMin) * 0.5f;
        float portHWidth = port.rectTransform.sizeDelta.x * 0.5f;
        float minX = -bgHWidth;
        float maxX = bgHWidth;
        float posX = Mathf.Lerp(minX, maxX, ratio);

        port.rectTransform.anchoredPosition = new Vector2(posX, 0.0f);
    }

    private void OnChangedBoardPageCursor(int cursor)
    {
        pageCursorUI.rectTransform.localScale = new Vector3(cursor == 0 ? 1.0f : -1.0f, 1.0f, 1.0f);
    }

    private void OnChangedBoardCursor(int page, int cursor)
    {
        if (cursor > -1 && cursor < boardList[page].Count)
        {
            PlayerUnit player = unitList.FirstOrDefault((o) => o is PlayerUnit) as PlayerUnit;
            BBoard selectedBoard = boardList[page][cursor];

            SelectBoard(page, cursor);

            if(page == 0)
            {
                player?.SetTarget(selectedBoard.Owner);
                player?.DrawTargetLine();
            }
            else if(page == 1)
            {
                MonsterUnit monsterUnit = selectedBoard.Owner as MonsterUnit;

                monsterUnit?.PlaySelectedTargetLine(selectedBoard);
            }

            //Debug.Log(selectedBoard.ToString());
        }
    }

    private void OnPrevBoardCursor(int page, int cursor)
    {
        PlayerUnit player = unitList.FirstOrDefault((o) => o is PlayerUnit) as PlayerUnit;

        if(page == 1)
        {
            for(int i = 0; i < boardList[page].Count; ++i)
            {
                BBoard board = boardList[page][i];
                MonsterUnit monsterUnit = board.Owner as MonsterUnit;

                monsterUnit?.StopSelectedTargetLine(board);
            }
        }
    }

    private void SelectBoard(int page, int cursor)
    {
        DownToBoard(page);
        UpToBoard(page, cursor);
        SortingBoard(page);
        RepositionUpperBoardList(page);
        RepositionDownBoardList(page);
    }

    private void UpToBoard(int page, int cursor)
    {
        BBoard selectedBoard = boardList[page][cursor];

        selectedBoard.transform.SetParent(boardContainer_U[page]);
    }

    private void RepositionUpperBoardList(int page)
    {
        Vector3 startPos = OverlayToWorld(OverlayRectTransformCenter(boardContainer_U_RT[page]));

        for (int i = 0; i < boardContainer_U[page].childCount; ++i)
        {
            boardContainer_U[page].GetChild(i).position = startPos + new Vector3(0.25f * i, 0.0f, 0.0f);
        }
    }

    private void RepositionDownBoardList(int page)
    {
        Vector3 startPos = OverlayToWorld(OverlayRectTransformCenter(boardContainer_RT[page]));

        for (int i = 0; i < boardContainer[page].childCount; ++i)
        {
            boardContainer[page].GetChild(i).position = startPos + new Vector3(0.25f * i, 0.0f, 0.0f);
        }
    }

    private void DownToBoard(int page)
    {
        Transform prev = boardContainer_U[page].childCount > 0 ? boardContainer_U[page].GetChild(0) : null;

        prev?.SetParent(boardContainer[page]);

    }

    private Vector3 OverlayToWorld(RectTransform rectTransform)
    {
        return OverlayToWorld(rectTransform.position);
    }

    private Vector3 OverlayToWorld(Vector3 screen)
    {
        Vector3 screenPos = new Vector3(screen.x, screen.y, 10.0f);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPos);

        return worldPosition;
    }

    private Vector3 OverlayRectTransformCenter(RectTransform rectTransform)
    {
        Bounds bound = RectTransformUtility.CalculateRelativeRectTransformBounds(rectTransform);

        return rectTransform.TransformPoint(bound.center);
    }

    private void SortingBoard(int page)
    {
        for(int i = 0; i < boardContainer_U[page].childCount; ++i)
        {
            BBoard board = boardContainer_U[page].GetChild(i).GetComponent<BBoard>();
            board.GetComponent<SortingGroup>().sortingOrder = 1000 - (i * 10);
        }

        for (int i = 0; i < boardContainer[page].childCount; ++i)
        {
            BBoard board = boardContainer[page].GetChild(i).GetComponent<BBoard>();
            board.GetComponent<SortingGroup>().sortingOrder = 100 - (i * 10);
        }
    }

    private void OnChangedPlayerCount(int count)
    {
        if(count <= 0)
        {
            onStageDefeat?.Invoke();
        }
        else
        {

        }
    }

    private void OnChangedMonsterCount(int count)
    {
        if(count <= 0)
        {
            onStageClear?.Invoke();
        }
        else
        {

        }
    }
}
