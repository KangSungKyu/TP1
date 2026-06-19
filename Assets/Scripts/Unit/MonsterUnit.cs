using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using static Commons;

public class MonsterUnit : UnitBase
{
    public MonsterData MonsterData => monsterData;
    public int BoardCount => boardList.Count;

    private List<BBoard> boardList = null;
    private MonsterData monsterData = default(MonsterData);

    public override void LoadFromSO(uint idx)
    {
        boardList = new List<BBoard>();
        monsterData = DataTableManager.Instance.GetMonsterData(idx);
        info = DataTableManager.Instance.GetUnitData(monsterData.UnitIdx);
        unitName = DataTableManager.Instance.GetText(info.NameIdx);

        InitUnitData();
        Init();
    }

    public override void ApplyStatus(ApplyStatusData applyData)
    {
        float defaultShieldCrushTime = 0.0f;
        
        applyStatusData = applyData;

        usageUnitData.MaxHp.Value = Mathf.Max(0, info.MaxHp + applyData.MaxHp);
        usageUnitData.Atk = Mathf.Max(0, info.Atk + applyData.Atk);
        usageUnitData.Def = Mathf.Max(0, info.Def + applyData.Def);
        usageUnitData.Dodge = Mathf.Clamp(info.Dodge + applyData.Dodge, 0, 1000);
        usageUnitData.Spd = Mathf.Max(0, info.Spd + applyData.Spd);
        usageUnitData.ShieldCrushTime = Mathf.Max(0, defaultShieldCrushTime + applyData.ShieldCrushTime);

        Debug.Log($"{name} apply status, apply:{applyData}, usage:{usageUnitData}");
    }

    public void AddBoard(BBoard board)
    {
        this.boardList.Add(board);
    }

    public void DelBoard(BBoard board)
    {
        board.ReleaseBoard();
        Factory.Instance.ReleaseBoard(board);
        this.boardList.Remove(board);
    }

    public BBoard GetBoard(int i)
    {
        if(boardList.Count > 0)
        {
            return this.boardList[i];
        }

        return null;
    }

    public void DelAttackBoardList()
    {
        if(boardList.Count > 1)
        {
            for (int i = 1; i < this.boardList.Count; i++)
            {
                DelBoard(boardList[i]);
            }
        }
    }

    protected override void Init()
    {
        SetShield(true);

        AnimationData animData = DataTableManager.Instance.GetAnimationData(info.AnimGroupIdx);

        spriteRenderer.color = Color.white;

        if (animData.Idx > 0 && animator != null)
        {
            animator.runtimeAnimatorController = ResourceManager.Instance.GetResource<RuntimeAnimatorController>(animData.ControllerKey);
        }
        else
        {
            spriteRenderer.color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
        }

        spriteRenderer.transform.localScale = Vector3.one * monsterData.SizeScale;

        Subscribe_HP((hp) =>
        {
            Debug.Log($"Monster {info.Idx}, HP: {hp}, MaxHP: {MaxHp}");
        });

        Subscribe_MaxHP((maxhp) =>
        {
            Debug.Log($"Monster {info.Idx}, HP: {Hp}, MaxHP: {maxhp}");
        });

        currentATB.Value += UnityEngine.Random.Range(-8, 2); // simple jitter

        usageUnitData.ShieldCrushedTime
            .Select(x => Observable.Timer(System.TimeSpan.FromSeconds(x)))
            .Switch()
            .Subscribe(_ =>
            {
                SetShield(true);
                if (usageUnitData.ShieldCrushedTime != null)
                    usageUnitData.ShieldCrushedTime.Value = 0f;
            })
            .AddTo(this);
    }

    public int GetBoardWidth()
    {
        if(boardList.Count > 0)
        {
            return boardList[0].Width;
        }

        return (int)monsterData.BoardDefaultWidth;
    }

    public int GetBoardHeight()
    {
        if (boardList.Count > 0)
        {
            return boardList[0].Height;
        }

        return (int)monsterData.BoardDefaultHeight;
    }
}