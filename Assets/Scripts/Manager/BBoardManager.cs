using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;

public class BBoardManager : MonoBehaviour
{
    [SerializeField]
    private RectTransform[] boardContainer_RT = null;
    [SerializeField]
    private RectTransform[] boardContainer_U_RT = null;
    [SerializeField]
    private Transform[] boardContainer = null;

    public int BoardPageCursor => boardPageCursor.Value;

    private List<BBoard>[] boardList = new List<BBoard>[] { new List<BBoard>(), new List<BBoard>() };
    private ReactiveProperty<int> boardPageCursor = new ReactiveProperty<int>(0); //0 right, 1 left
    private ReactiveProperty<int>[] boardCursor = new ReactiveProperty<int>[] { new ReactiveProperty<int>(-1), new ReactiveProperty<int>(-1) };

    public void SpawnOffensiveBoard(MonsterUnit monsterUnit)
    {
        BBoard board = Factory.Instance.GetBoard(BBoardType.Offensive, boardContainer[0], (int)monsterUnit.MonsterData.BoardDefaultWidth, (int)monsterUnit.MonsterData.BoardDefaultHeight, monsterUnit.Spd);

        if(board != null)
        {
            board.SetOwner(monsterUnit);
            monsterUnit.AddBoard(board);

            boardList[0].Add(board);
        }
    }

    public BBoard SpawnDefensiveBoard(MonsterUnit monsterUnit)
    {
        BBoard tempBoard = Factory.Instance.GetBoard(BBoardType.Defensive, boardContainer[1], monsterUnit.GetBoardWidth(), monsterUnit.GetBoardHeight(), monsterUnit.Spd * 0.85f);

        if(tempBoard != null)
        {
            tempBoard.SetOwner(monsterUnit);
            boardList[1].Add(tempBoard);
            tempBoard.InitBoard();
            tempBoard.PlayFadeCover();
            tempBoard.FillBoard(BBoardType.Defensive);
            monsterUnit.AddBoard(tempBoard);
        }

        return tempBoard;
    }

    public void InitBoardList()
    {
        for (int i = 0; i < boardList[0].Count; i++)
        {
            boardList[0][i].InitBoard(SaveLoadManager.Instance.UserSkillData.GetEquipedSkills());
        }

        boardCursor[0].Value = 0;
        boardCursor[1].Value = 0;
        boardPageCursor.Value = 0;

    }

    public void SetCursorEvent(System.Action<int, int> onChangedBoardCursor, System.Action<int, int> onPrevBoardCursor, System.Action<int> onChangedBoardPageCursor)
    {
        boardCursor[0].Subscribe((v) => { onChangedBoardCursor(boardPageCursor.Value, v); }).AddTo(this);
        boardCursor[1].Pairwise().Subscribe((pair) => { onPrevBoardCursor(boardPageCursor.Value, pair.Previous); onChangedBoardCursor(boardPageCursor.Value, pair.Current); }).AddTo(this);
        boardPageCursor.Subscribe(onChangedBoardPageCursor).AddTo(this);
    }

    public void ReleaseBoardList()
    {
        for (int i = 0; i < boardList.Length; ++i)
        {
            for (int j = 0; j < boardList[i].Count; ++j)
            {
                boardList[i][j].ReleaseBoard();
                Factory.Instance.ReleaseBoard(boardList[i][j]);
            }

            boardList[i].Clear();
        }
    }

    public void ResetCurrentPuzzle()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (currentBoardCursor > -1)
        {
            BBoard board = GetBoard(boardPageCursor.Value, currentBoardCursor);

            board?.ResetDrawLine();
        }
    }

    public void SelectPuzzlePage_R()
    {
        SelectPuzzlePage(0);
    }

    public void SelectPuzzlePage_L()
    {
        SelectPuzzlePage(1);
    }

    public void SelectPuzzlePage(int page)
    {
        if (boardList[page].Count > 0)
        {
            boardPageCursor.Value = page;
        }
    }

    public void SelectPuzzle_R(int pageCursor)
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

    public void SelectPuzzle_L(int pageCursor)
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

    public void DrawCurrentPuzzle_ToRight()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            BBoard board = GetBoard(boardPageCursor.Value, currentBoardCursor);

            board?.SetDirection(1, 0);
        }
    }

    public void DrawCurrentPuzzle_ToLeft()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            BBoard board = GetBoard(boardPageCursor.Value, currentBoardCursor);

            board?.SetDirection(-1, 0);
        }
    }

    public void DrawCurrentPuzzle_ToDown()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            BBoard board = GetBoard(boardPageCursor.Value, currentBoardCursor);

            board?.SetDirection(0, -1);
        }
    }

    public void DrawCurrentPuzzle_ToUp()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        if (-1 < currentBoardCursor && currentBoardCursor < boardList[boardPageCursor.Value].Count)
        {
            BBoard board = GetBoard(boardPageCursor.Value, currentBoardCursor);

            board?.SetDirection(0, 1);
        }
    }

    public void SelectLastPuzzle()
    {
        boardCursor[boardPageCursor.Value].SetValueAndForceNotify(boardList[boardPageCursor.Value].Count - 1);
    }

    public void RemoveBoard(BBoard currentBoard)
    {
        currentBoard.ReleaseBoard();
        boardList[boardPageCursor.Value].Remove(currentBoard);
    }

    public BBoard GetCurrentSelectedBoard()
    {
        int currentBoardCursor = boardCursor[boardPageCursor.Value].Value;

        // guard: ensure cursor is within range
        if (currentBoardCursor < 0 || currentBoardCursor >= boardList[boardPageCursor.Value].Count)
        {
            return null;
        }

        BBoard currentBoard = GetBoard(boardPageCursor.Value, currentBoardCursor);

        return currentBoard;
    }

    public void ChangedBoardCursor(int page, int cursor)
    {
        if (cursor > -1 && cursor < boardList[page].Count)
        {
            SelectBoard(page, cursor);

            //Debug.Log(selectedBoard.ToString());
        }
    }

    public BBoard GetBoard(int page, int cursor)
    {
        if (0 <= cursor && cursor < boardList[page].Count)
        {
            return boardList[page][cursor];
        }

        return null;
    }

    public IEnumerable<BBoard> GetBoardList(int page)
    {
        return boardList[page];
    }

    public void AutoSelectBoard_L(BBoard board, System.Action<int, int> onChangedBoardCursor)
    {
        int idx = boardList[1].IndexOf(board);

        if (idx >= 0)
        {
            boardList[1].RemoveAt(idx);
        }

        if (boardList[1].Count <= 0)
        {
            boardPageCursor.Value = 0;
        }
        else
        {
            SelectPuzzle_R(1);
            onChangedBoardCursor(1, boardCursor[1].Value);
        }
    }

    public int GetBoardCount(int page)
    {
        return boardList[page].Count;
    }
    public void SortingBoardZOrder(int page)
    {
        for (int i = 0; i < boardList[page].Count; ++i)
        {
            BBoard board = GetBoard(page, i);

            if (i == boardCursor[page].Value)
            {
                board.GetComponent<SortingGroup>().sortingOrder = 1000 - (i * 10);
            }
            else
            {
                board.GetComponent<SortingGroup>().sortingOrder = 100 - (i * 10);
            }
        }
    }

    public void RepositionBoardList(int page)
    {
        for (int i = 0; i < boardList[page].Count; ++i)
        {
            Vector3 startPos = Vector3.zero;

            if (i == boardCursor[page].Value)
            {
                startPos = OverlayToWorld(OverlayRectTransformCenter(boardContainer_U_RT[page]));
            }
            else
            {
                startPos = OverlayToWorld(OverlayRectTransformCenter(boardContainer_RT[page]));
            }

            Transform tr = boardContainer[page].GetChild(i);

            if (tr != null)
            {
                tr.position = startPos + new Vector3(0.25f * i, 0.0f, 0.0f);
            }
        }
    }


    private void SelectBoard(int page, int cursor)
    {
        SortingBoardZOrder(page);
        RepositionBoardList(page);
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

}