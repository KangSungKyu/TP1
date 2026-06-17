using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
public class BattleManager : Singleton<BattleManager>
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


    private ReactiveProperty<int> boardPageCursor = new ReactiveProperty<int>(0); //0 right, 1 left
    private ReactiveProperty<int>[] boardCursor = new ReactiveProperty<int>[] { new ReactiveProperty<int>(-1), new ReactiveProperty<int>(-1) };
    private ReactiveProperty<int> playerCount = new ReactiveProperty<int>(0);
    private ReactiveProperty<int> monsterCount = new ReactiveProperty<int>(0);
    private PlayerUnit player = null;
    private List<PlayerUnit> playerList = new List<PlayerUnit>();
    private List<MonsterUnit> monsterList = new List<MonsterUnit>();
    private List<BBoard>[] boardList = new List<BBoard>[] { new List<BBoard>(), new List<BBoard>() };

    private PlayerInput playerInput = null;
    private event System.Action onStageDefeat = null;
    private event System.Action onStageClear = null;
    

    public void InitStage(UserData userData, StageData stageData)
    {
        if (playerList.Count <= 0)
        {
            playerList.Add(Factory.Instance.GetPlayerUnit(transform, PlayerSpawnGO.transform.position) as PlayerUnit);
        }

        LevelBaseData lbd = SODataTable.Instance.GetLevelBaseData((uint)userData.Level);

        player = playerList[0];

        player.LoadFromSO(1); //test
        player.SetLevelBase(lbd);
        player.SetHPUI(Factory.Instance.GetHPUI(hpBarContainer));
        player.Subscribe_HP(OnPlayerDeath);
        // subscribe to ATB ready for player
        player.OnATBReady.Subscribe(_ => OnUnitATBReady(player)).AddTo(player);

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
                    spawnY = (monsterList.Count % spawnRow - (spawnRow / 2)) * 1.0f;
                    //test
                    monsterUnit.transform.position = new Vector3(monsterStartPos.x + prevX, monsterStartPos.y + spawnY, 0.0f);

                    monsterUnit.SetTarget(player);
                    monsterUnit.LoadFromSO(stageData.MonsterIdx[i]);
                    monsterUnit.SetHPUI(Factory.Instance.GetHPUI(hpBarContainer));
                    monsterUnit.Subscribe_HP(OnMonsterDeath);
                    monsterUnit.OnATBReady.Subscribe(_ => OnUnitATBReady(monsterUnit)).AddTo(monsterUnit);

                    BBoard board = Factory.Instance.GetBoard(boardContainer[0], (int)monsterUnit.MonsterData.BoardDefaultWidth, (int)monsterUnit.MonsterData.BoardDefaultHeight, 7.5f);

                    board.SetOwner(monsterUnit);
                    monsterUnit.AddBoard(board);

                    boardList[0].Add(board);
                    monsterList.Add(monsterUnit);

                    Image monsterPort = Factory.Instance.GetPortraitUI(uiPoolTempContainer);

                    monsterPort.rectTransform.SetParent(atbGaugeBG);
                    monsterUnit.SetPortraitUI(monsterPort);
                    monsterUnit.Subscribe_ATBGauge((v) => OnATBGauge(monsterPort, monsterUnit.ATBRatio));

                    Vector2 monsterSize = monsterUnit.transform.Find("Renderer/Sprite").GetComponent<SpriteRenderer>().bounds.size;

                    prevX += monsterSize.x * 0.5f;
                }
            }
        }

        playerCount.Value = playerList.Count;
        monsterCount.Value = monsterList.Count;

        playerCount.Subscribe(OnChangedPlayerCount).AddTo(this);
        monsterCount.Subscribe(OnChangedMonsterCount).AddTo(this);

        SortingBoard(0);
        DownToBoard(0);

        for (int i = 0; i < boardList[0].Count; i++)
        {
            boardList[0][i].InitBoard();
        }

        boardCursor[0].Value = 0;
        boardCursor[1].Value = 0;
        boardPageCursor.Value = 0;

        boardCursor[0].Subscribe(OnChangedBoardCursor).AddTo(this);
        boardCursor[1].Subscribe(OnChangedBoardCursor).AddTo(this);
        boardPageCursor.Subscribe(OnChangedBoardPageCursor).AddTo(this);
    }

    public void ReleaseStage()
    {
        player = null;

        for(int i = 0; i < playerList.Count; ++i)
        {
            playerList[i]?.Release();
        }

        playerList.Clear();

        for(int i = 0; i < monsterList.Count; ++i)
        {
            monsterList[i]?.Release();
        }

        monsterList.Clear();

        for (int i = 0; i < boardList.Length; ++i)
        {
            boardList[i].Clear();
        }

        playerInput.Battle.Disable();
        onStageDefeat = null;
        onStageClear = null;

        Factory.Instance.Release();
    }

    public void OnStageDefeat(System.Action act)
    {
        onStageDefeat += act;
    }

    public void OnStageClear(System.Action act)
    {
        onStageClear += act;
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();

        if(playerInput == null)
        {
            playerInput = new PlayerInput();
        }

        playerInput.Battle.PuzzleDrawUp.performed += ctx => OnPuzzleDrawUp();
        playerInput.Battle.PuzzleDrawDown.performed += ctx => OnPuzzleDrawDown();
        playerInput.Battle.PuzzleDrawLeft.performed += ctx => OnPuzzleDrawLeft();
        playerInput.Battle.PuzzleDrawRight.performed += ctx => OnPuzzleDrawRight();

        playerInput.Battle.PuzzleSelectL.performed += ctx => OnPuzzleSelectL();
        playerInput.Battle.PuzzleSelectR.performed += ctx => OnPuzzleSelectR();

        playerInput.Battle.PuzzlePageL.performed += ctx => OnPuzzlePageL();
        playerInput.Battle.PuzzlePageR.performed += ctx => OnPuzzlePageR();

        playerInput.Battle.PuzzleReset.performed += ctx => OnPuzzleReset();

        playerInput.Battle.Enable();
    }

    protected override void OnSingletonDestroyed()
    {
        base.OnSingletonDestroyed();

        if(playerInput != null)
        {
            playerInput.Battle.Disable();
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            if(monsterList.Count > 0)
            {
                monsterList[0].ApplyDamage(99999f);
            }
        }
        else if(Input.GetKeyDown(KeyCode.P))
        {
            player?.ApplyDamage(99999f);
        }
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

    private void OnPuzzleSelectR()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            currentBoardCursor += 1;
        }

        if (currentBoardCursor >= boardList[boardPageCursor.Value].Count)
        {
            currentBoardCursor = 0;
        }

        boardCursor[boardPageCursor.Value].Value = currentBoardCursor;
    }

    private void OnPuzzleSelectL()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (currentBoardCursor > 0)
        {
            currentBoardCursor -= 1;
        }

        if (currentBoardCursor < 0)
        {
            currentBoardCursor = boardList[boardPageCursor.Value].Count - 1;
        }

        boardCursor[boardPageCursor.Value].Value = currentBoardCursor;
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
        //*
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

    private void OnPlayerDeath(float hp)
    {
        if (hp <= 0)
        {
            System.Action act = () =>
            {
                player.Release();
                Factory.Instance.ReleasePortraitUI(player.PortraitUI);
                Factory.Instance.ReleasePlayerUnit(player);
                Factory.Instance.ReleaseHPUI(player.HpUI);

                playerList.Remove(player);

                playerCount.Value = playerList.Count;
            };

            player.PlayAction(UnitActionData.DefaultAction_None,
                new UnitActionData(UnitActionType.Death, null, act)
                );
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
                    monster.DelAttackBoardList();
                    Factory.Instance.ReleasePortraitUI(monster.PortraitUI);
                    currentBoard.ReleaseBoard();
                    boardList[boardPageCursor.Value].Remove(currentBoard);
                    Factory.Instance.ReleaseBoard(currentBoard);
                    Factory.Instance.ReleaseMonsterUnit(monster);
                    Factory.Instance.ReleaseHPUI(monster.HpUI);
                    monsterList.Remove(monster);

                    boardCursor[boardPageCursor.Value].SetValueAndForceNotify(boardList[boardPageCursor.Value].Count - 1);

                    monsterCount.Value = monsterList.Count;
                }
            };

            if (monster != null)
            {
                monster.PlayAction(UnitActionData.DefaultAction_None,
                    new UnitActionData(UnitActionType.Death, null, act)
                    );
            }
        }
    }

    // Example mediator: called when a unit's ATB is full
    private void OnUnitATBReady(UnitBase unit)
    {
        if (unit == null)
            return;

        // If player is ready, perform attack against current board owner (if any)
        if (unit is PlayerUnit)
        {
            int currentBoardCursor = boardCursor[0].Value;

            if (currentBoardCursor >= 0 && currentBoardCursor < boardList[0].Count)
            {
                BBoard current = boardList[0][currentBoardCursor];
                UnitBase defender = current.Owner;

                boardPageCursor.Value = 0;
                boardCursor[boardPageCursor.Value].SetValueAndForceNotify(0);

                SortingBoard(0);

                current.StartBoardTimer(() =>
                {
                    current.ClearBoard();
                    current.ClearDrawLine();
                    current.FillBoard();
                });
                current.SubscribeOnPathComplete(() =>
                {
                    if (defender != null)
                    {
                        TestPrintPointList();

                        //calc damage, def from path, and apply to player and monster (instance status)
                        ApplyStatusData applyAttackerStatus = current.GetApplyStatusFromPath();
                        ApplyStatusData applyDefenderStatus = default;

                        //player attack to target(board's owner, monster)

                        Debug.Log($"Player ATB ready -> attack {defender.name}");
                        unit.PlayAction(UnitActionData.DefaultAction_None,
                            //new UnitActionData(UnitActionType.Move),
                            new UnitActionData(UnitActionType.Attack, null, () =>
                            {
                                UnitCalculator.ApplyDamage(unit, defender, applyAttackerStatus, applyDefenderStatus);
                                unit.ClearApplyStatus();
                                defender.ClearApplyStatus();
                            }));

                        current.ForceStopBoardTimer();
                        current.ClearBoard();
                        current.ClearDrawLine();
                        current.FillBoard();
                    }
                });
            }
        }
        else if (unit is MonsterUnit)
        {
            // Monster attacks the player
            if (player != null)
            {
                MonsterUnit mu = (MonsterUnit)unit;
                int prevListCount = boardList[1].Count;

                Debug.Log($"Monster ATB ready -> spawn attack board for {mu.name}");

                if(mu.BoardCount > 1)
                {
                    BBoard prevAttack = mu.GetBoard(1);

                    if(prevAttack != null)
                    {
                        // cleanup board
                        //prevAttack.ForceBoardTimeOver();
                        prevAttack.ClearBoard();
                        prevAttack.ClearDrawLine();
                        prevAttack.ReleaseBoard();

                        int idx = boardList[1].IndexOf(prevAttack);

                        if (idx >= 0)
                        {
                            boardList[1].RemoveAt(idx);
                        }

                        mu.DelBoard(prevAttack);
                    }
                }


                // create a new temporary board for this monster's attack
                BBoard tempBoard = Factory.Instance.GetBoard(boardContainer[1], mu.GetBoardWidth(), mu.GetBoardHeight(), 7.0f);

                if (tempBoard != null)
                {
                    tempBoard.SetOwner(mu);
                    boardList[1].Add(tempBoard);
                    tempBoard.InitBoard();

                    boardPageCursor.Value = 1;
                    boardCursor[boardPageCursor.Value].SetValueAndForceNotify(0);

                    SortingBoard(1);

                    // start timer: when time over, execute attack using path-derived status and remove the board
                    tempBoard.StartBoardTimer(() =>
                    {
                        // gather statuses from path
                        ApplyStatusData applyAttackerStatus = player.ApplyStatusData;
                        ApplyStatusData applyDefenderStatus = default;

                        // perform the monster attack action
                        mu.PlayAction(UnitActionData.DefaultAction_None,
                            new UnitActionData(UnitActionType.Attack, null, () =>
                            {
                                UnitCalculator.ApplyDamage(mu, player, applyAttackerStatus, applyDefenderStatus);
                                mu.ClearApplyStatus();
                                player.ClearApplyStatus();
                            }));

                        // cleanup board
                        tempBoard.ClearBoard();
                        tempBoard.ClearDrawLine();
                        tempBoard.ReleaseBoard();

                        int idx = boardList[1].IndexOf(tempBoard);

                        if (idx >= 0)
                        {
                            boardList[1].RemoveAt(idx);
                        }

                        mu.DelBoard(tempBoard);

                        if (boardList[1].Count <= 0)
                        {
                            boardPageCursor.Value = 0;
                            boardCursor[boardPageCursor.Value].SetValueAndForceNotify(0);
                        }
                    });

                    tempBoard.SubscribeOnPathComplete(() =>
                    {
                        ApplyStatusData applyDefenderStatus = tempBoard.GetApplyStatusFromPath();

                        player.ApplyStatus(applyDefenderStatus);
                        tempBoard.ClearBoard();
                        tempBoard.ClearDrawLine();
                        tempBoard.ForceBoardTimeOver();
                    });
                }
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

    private void OnChangedBoardCursor(int cursor)
    {
        if (cursor > -1)
        {
            BBoard selectedBoard = boardList[boardPageCursor.Value][cursor];

            DownToBoard(boardPageCursor.Value);
            UpToBoard(boardPageCursor.Value, cursor);
            SortingBoard(boardPageCursor.Value);
            player.SetTarget(selectedBoard.Owner);

            Debug.Log(selectedBoard.ToString());
        }
    }

    private void UpToBoard(int page, int cursor)
    {
        BBoard selectedBoard = boardList[page][cursor];
        Vector3 startPos = OverlayToWorld(OverlayRectTransformCenter(boardContainer_U_RT[page]));

        selectedBoard.transform.SetParent(boardContainer_U[page]);

        for(int i = 0; i < boardContainer_U[page].childCount; ++i)
        {
            boardContainer_U[page].GetChild(i).position = startPos + new Vector3(0.25f * i, 0.0f, 0.0f);
        }
    }

    private void DownToBoard(int page)
    {
        Transform prev = boardContainer_U[page].childCount > 0 ? boardContainer_U[page].GetChild(0) : null;
        Vector3 startPos = OverlayToWorld(OverlayRectTransformCenter(boardContainer_RT[page]));

        prev?.SetParent(boardContainer[page]);

        for (int i = 0; i < boardContainer[page].childCount; ++i)
        {
            boardContainer[page].GetChild(i).position = startPos + new Vector3(0.25f * i, 0.0f, 0.0f);
        }
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
