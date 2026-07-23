using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using static Commons;
using Cysharp.Threading.Tasks;

public class Factory : Singleton<Factory>
{
    [SerializeField]
    private Transform gobjContainer = null;
    [SerializeField]
    private RectTransform uiContainer = null;

    // Public InitAsync wrapper to orchestrate pool initialization. If containers were not provided, create fallbacks but log a warning.
    public async Task<bool> InitAsync()
    {
        bool c = await Init_UIPoolAsync();
        bool d = await Init_HPUIPoolAsync();
        bool e = await Init_DamageFontPoolAsync();

        return c && d && e;
    }

    public async Task<bool> Init_SystemResAsync()
    {
        return await SimplePoolManager.Instance.CreatePoolAsync<AlterMsg>(Commons.ResKey_AlterMsg, 10, 10, uiContainer, onGet_AlterMsg).AsTask();
    }

    public async Task<bool> Init_UnitPoolAsync(int monsterCount)
    {
        bool p = await SimplePoolManager.Instance.CreatePoolAsync<UnitBase>(Commons.ResKey_PlayerUnit, 1, 1, gobjContainer).AsTask();
        bool m = await SimplePoolManager.Instance.CreatePoolAsync<UnitBase>(Commons.ResKey_MonsterUnit, monsterCount, monsterCount, gobjContainer).AsTask();

        return p && m;
    }

    public async Task<bool> Init_HitEffectPoolAsync(int hitEffectCount)
    {
        return await SimplePoolManager.Instance.CreatePoolAsync<HitEffect>(Commons.ResKey_HitEffect, hitEffectCount, hitEffectCount, gobjContainer, onGet_HitEffect).AsTask();
    }

    public async Task<bool> Init_TargetLinePoolAsync(int count)
    {
        return await SimplePoolManager.Instance.CreatePoolAsync<QuadraticBezierRenderer>(Commons.ResKey_TargetLine, count, count, gobjContainer, onGet_TargetLine).AsTask();
    }

    public async Task<bool> Init_BoardPoolAsync(int boardCount, int tileCount)
    {
        bool t = await SimplePoolManager.Instance.CreatePoolAsync<BTile>(Commons.ResKey_Tile, tileCount, tileCount, gobjContainer, OnGet_Tile).AsTask();
        bool b = await SimplePoolManager.Instance.CreatePoolAsync<BBoard>(Commons.ResKey_Board, boardCount, boardCount, gobjContainer, OnGet_Board, OnRelease_Board).AsTask();

        return t && b;
    }

    public async Task<bool> Init_UIPoolAsync()
    {
        return await SimplePoolManager.Instance.CreatePoolAsync<Image>(Commons.ResKey_PortraitUI, 10, 10, uiContainer, OnGet_Portrait).AsTask();
    }

    public async Task<bool> Init_HPUIPoolAsync()
    {
        return await SimplePoolManager.Instance.CreatePoolAsync<HpBar>(Commons.ResKey_HPUI, 10, 10, uiContainer, OnGet_HPUI).AsTask();
    }

    public async Task<bool> Init_DamageFontPoolAsync()
    {
        return await SimplePoolManager.Instance.CreatePoolAsync<DamageFont>(Commons.ResKey_DamageFont, 10, 10, gobjContainer, onGet_DamageFont).AsTask();
    }

    public void Release()
    {
        SimplePoolManager.Instance?.ClearAll();
    }

    public UnitBase GetPlayerUnit(Transform container, Vector3? position = null, Quaternion? quaternion = null)
    {
        UnitBase unit = SimplePoolManager.Instance.Get<UnitBase>(Commons.ResKey_PlayerUnit);

        if (unit != null)
        {
            unit.transform.SetParent(container);
            unit.transform.SetPositionAndRotation(position ?? Vector3.zero, quaternion ?? Quaternion.identity);
        }

        return unit;
    }

    public UnitBase GetMonsterUnit(Transform container, Vector3? position = null, Quaternion? quaternion = null)
    {
        UnitBase unit = SimplePoolManager.Instance.Get<UnitBase>(Commons.ResKey_MonsterUnit);

        if(unit != null)
        {
            unit.transform.SetParent(container);
            unit.transform.SetPositionAndRotation(position ?? Vector3.zero, quaternion ?? Quaternion.identity);
        }

        return unit;
    }

    public HpBar GetHPUI(RectTransform container)
    {
        HpBar hpBar = SimplePoolManager.Instance.Get<HpBar>(Commons.ResKey_HPUI);

        if(hpBar != null)
        {
            hpBar.transform.SetParent(container, false);
        }

        return hpBar;
    }

    public Image GetPortraitUI(RectTransform container)
    {
        Image portrait = SimplePoolManager.Instance.Get<Image>(Commons.ResKey_PortraitUI);

        if(portrait != null)
        {
            portrait.transform.SetParent(container, false);
        }

        return portrait;
    }

    public BBoard GetBoard(BBoardType type, Transform container, int boardWidth, int boardHeight, float maxTimer)
    {
        BBoard board = BBoard.CreateEmptyBoard(type, container, boardWidth, boardHeight, maxTimer);

        return board;
    }

    public DamageFont GetDamageFont(Transform container, float damage)
    {
        DamageFont damageFont = SimplePoolManager.Instance.Get<DamageFont>(Commons.ResKey_DamageFont);

        if(damageFont != null)
        {
            damageFont.transform.SetParent(container, false);
        }

        return damageFont;
    }

    public HitEffect GetHitEffect(Transform container)
    {
        HitEffect hitEffect = SimplePoolManager.Instance.Get<HitEffect>(Commons.ResKey_HitEffect);

        if (hitEffect != null)
        {
            hitEffect.transform.SetParent(container, false);
        }
        
        return hitEffect;
    }

    public AlterMsg GetAlterMsg()
    {
        AlterMsg alter = SimplePoolManager.Instance.Get<AlterMsg>(Commons.ResKey_AlterMsg);

        return alter;
    }

    public QuadraticBezierRenderer GetTargetLine(Transform container, Color color)
    {
        QuadraticBezierRenderer targetLine = SimplePoolManager.Instance.Get<QuadraticBezierRenderer>(Commons.ResKey_TargetLine);

        if(targetLine != null)
        {
            targetLine.transform.SetParent(container);
            targetLine.SetColor(color);
        }

        return targetLine;
    }

    public void ReleasePlayerUnit(PlayerUnit player)
    {
        if (player != null)
        {
            player.transform.SetParent(gobjContainer);
        }

        SimplePoolManager.Instance.Release<UnitBase>(Commons.ResKey_PlayerUnit, player);
    }

    public void ReleaseHPUI(HpBar hpUI)
    {
        if(hpUI != null)
        {
            hpUI.transform.SetParent(uiContainer);
        }
        
        SimplePoolManager.Instance.Release<HpBar>(Commons.ResKey_HPUI, hpUI);
    }

    public void ReleaseBoard(BBoard currentBoard)
    {
        if(currentBoard != null)
        {
            currentBoard.transform.SetParent(gobjContainer);
        }

        SimplePoolManager.Instance.Release<BBoard>(Commons.ResKey_Board, currentBoard);
    }

    public void ReleaseMonsterUnit(MonsterUnit monster)
    {
        if(monster != null)
        {
            monster.transform.SetParent(gobjContainer);
        }

        SimplePoolManager.Instance.Release<UnitBase>(Commons.ResKey_MonsterUnit, monster);
    }

    public void ReleasePortraitUI(Image portraitUI)
    {
        if(portraitUI != null)
        {
            portraitUI.transform.SetParent(uiContainer);
        }

        SimplePoolManager.Instance.Release<Image>(Commons.ResKey_PortraitUI, portraitUI);
    }

    public void ReleaseDamageFont(DamageFont damageFont)
    {
        if(damageFont != null)
        {
            damageFont.transform.SetParent(gobjContainer);
        }

        SimplePoolManager.Instance.Release<DamageFont>(Commons.ResKey_DamageFont, damageFont);
    }

    public void ReleaseAlterMsg(AlterMsg alterMsg)
    {
        if(alterMsg != null)
        {
            alterMsg.SetParent(uiContainer);
        }

        SimplePoolManager.Instance.Release<AlterMsg>(Commons.ResKey_AlterMsg, alterMsg);
    }

    public void ReleaseHitEffect(HitEffect hitEffect)
    {
        if (hitEffect != null)
        {
            hitEffect.transform.SetParent(gobjContainer);
        }

        SimplePoolManager.Instance.Release<HitEffect>(Commons.ResKey_HitEffect, hitEffect);
    }

    public void ReleaseTargetLine(QuadraticBezierRenderer targetLine)
    {
        if(targetLine != null)
        {
            targetLine.transform.SetParent(gobjContainer);
        }

        SimplePoolManager.Instance.Release<QuadraticBezierRenderer>(Commons.ResKey_TargetLine, targetLine);
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

    private void onGet_DamageFont(DamageFont font)
    {
        font.Reset();
    }

    private void onGet_AlterMsg(AlterMsg msg)
    {
        RectTransform rt = (RectTransform)msg.transform;

        rt.localScale = Vector3.one;
        rt.anchoredPosition3D = new Vector3(0, 0, 0);
    }

    private void onGet_HitEffect(HitEffect effect)
    {
        effect.transform.localScale = Vector3.one * 1.0f;
    }

    private void onGet_TargetLine(QuadraticBezierRenderer targetLine)
    {
        targetLine.transform.position = Vector3.zero;
        targetLine.transform.localScale = Vector3.one;
    }

}