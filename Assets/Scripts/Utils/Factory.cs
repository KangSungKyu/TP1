using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using static Commons;

public class Factory : Singleton<Factory>
{
    [SerializeField]
    private Transform gobjContainer = null;
    [SerializeField]
    private RectTransform uiContainer = null;

    private SimplePool<UnitBase> playerPool = null;
    private SimplePool<UnitBase> monsterPool = null;
    private SimplePool<BBoard> boardPool = null;
    private SimplePool<BTile> tileUIPool = null;
    private SimplePool<HpBar> hpBarPool = null;
    private SimplePool<Image> portraitPool = null;


    // Public InitAsync wrapper to orchestrate pool initialization. If containers were not provided, create fallbacks but log a warning.
    public async Task<bool> InitAsync()
    {
        bool c = await Init_UIPoolAsync();
        bool d = await Init_HPUIPoolAsync();

        return c && d;
    }

    public async Task<bool> Init_UnitPoolAsync(int monsterCount)
    {
        // Use addressable-backed pools. Prewarm a few instances.
        playerPool = new SimplePool<UnitBase>(1, Commons.ResKey_PlayerUnit, gobjContainer);
        monsterPool = new SimplePool<UnitBase>(monsterCount, Commons.ResKey_MonsterUnit, gobjContainer);

        // Prewarm minimal instances
        try
        {
            await playerPool.PrewarmAsync(1);
            await monsterPool.PrewarmAsync(monsterCount);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Init_UnitPoolAsync: PrewarmAsync failed: {ex}");
        }

        bool res = playerPool != null && monsterPool != null;

        return res;
    }

    public async Task<bool> Init_BoardPoolAsync(int boardCount, int tileCount)
    {
        tileUIPool = new SimplePool<BTile>(tileCount, Commons.ResKey_Tile, gobjContainer, OnGet_Tile);
        boardPool = new SimplePool<BBoard>(boardCount, Commons.ResKey_Board, gobjContainer, OnGet_Board, OnRelease_Board);

        try
        {
            await tileUIPool.PrewarmAsync(tileCount);
            await boardPool.PrewarmAsync(boardCount);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Init_BoardPoolAsync: PrewarmAsync failed: {ex}");
        }

        bool res = tileUIPool != null && boardPool != null;

        return res;
    }

    public async Task<bool> Init_UIPoolAsync()
    {
        portraitPool = new SimplePool<Image>(100, Commons.ResKey_PortraitUI, uiContainer, OnGet_Portrait);

        try
        {
            await portraitPool.PrewarmAsync(10);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Init_UIPoolAsync: PrewarmAsync failed: {ex}");
        }

        bool res = portraitPool != null;

        return res;
    }

    public async Task<bool> Init_HPUIPoolAsync()
    {
        hpBarPool = new SimplePool<HpBar>(100, Commons.ResKey_HPUI, uiContainer, OnGet_HPUI);

        try
        {
            await hpBarPool.PrewarmAsync(10);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Init_UIPoolAsync: PrewarmAsync failed: {ex}");
        }

        bool res = hpBarPool != null;

        return res;
    }

    public void Release()
    {
        playerPool?.Clear();
        monsterPool?.Clear();
        boardPool?.Clear();
        tileUIPool?.Clear();
        hpBarPool?.Clear();
        portraitPool?.Clear();
    }

    public UnitBase GetPlayerUnit(Transform container, Vector3? position = null, Quaternion? quaternion = null)
    {
        UnitBase unit = playerPool.Get();

        if (unit != null)
        {
            unit.transform.SetParent(container);
            unit.transform.SetPositionAndRotation(position ?? Vector3.zero, quaternion ?? Quaternion.identity);
        }

        return unit;
    }

    public UnitBase GetMonsterUnit(Transform container, Vector3? position = null, Quaternion? quaternion = null)
    {
        UnitBase unit = monsterPool.Get();

        if(unit != null)
        {
            unit.transform.SetParent(container);
            unit.transform.SetPositionAndRotation(position ?? Vector3.zero, quaternion ?? Quaternion.identity);
        }

        return unit;
    }

    public HpBar GetHPUI(RectTransform container)
    {
        HpBar hpBar = hpBarPool.Get();

        hpBar.transform.SetParent(container, false);

        return hpBar;
    }

    public Image GetPortraitUI(RectTransform container)
    {
        Image portrait = portraitPool.Get();

        portrait.transform.SetParent(container, false);

        return portrait;
    }

    public BBoard GetBoard(Transform container, int boardWidth, int boardHeight, float maxTimer)
    {
        BBoard board = BBoard.CreateEmptyBoard(boardPool, tileUIPool, container, boardWidth, boardHeight, maxTimer);

        return board;
    }

    public void ReleasePlayerUnit(PlayerUnit player)
    {
        if (player != null)
        {
            player.transform.SetParent(gobjContainer);
        }

        playerPool.Release(player);
    }

    public void ReleaseHPUI(HpBar hpUI)
    {
        if(hpUI != null)
        {
            hpUI.transform.SetParent(uiContainer);
        }
        
        hpBarPool.Release(hpUI);
    }
    public void ReleaseBoard(BBoard currentBoard)
    {
        if(currentBoard != null)
        {
            currentBoard.transform.SetParent(gobjContainer);
        }

        boardPool.Release(currentBoard);
    }

    public void ReleaseMonsterUnit(MonsterUnit monster)
    {
        if(monster != null)
        {
            monster.transform.SetParent(gobjContainer);
        }

        monsterPool.Release(monster);
    }

    public void ReleasePortraitUI(Image portraitUI)
    {
        if(portraitUI != null)
        {
            portraitUI.transform.SetParent(uiContainer);
        }

        portraitPool.Release(portraitUI);
    }

    protected override void OnSingletonDestroyed()
    {
        Release();
        base.OnSingletonDestroyed();
    }


    private void OnGet_Board(BBoard board)
    {
        board.transform.localScale = Vector3.one;
    }
    private void OnGet_Tile(BTile tile)
    {
        tile.transform.localScale = Vector3.one;
    }

    private void OnGet_Portrait(Image image)
    {
        image.transform.localScale = Vector3.one;
    }

    private void OnGet_HPUI(HpBar bar)
    {
        bar.transform.localScale = Vector3.one;
    }

    private void OnRelease_Board(BBoard board)
    {
        board.ReleaseBoard();
    }
}