using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using static Commons;

public class MonsterUnit : UnitBase
{
    public MonsterData MonsterData => monsterData;
    public int BoardCount => (ofsBoard != null ? 1 : 0) + dfsBoardList.Count;

    private BBoard ofsBoard = null;
    private List<BBoard> dfsBoardList = null;
    private MonsterData monsterData = default(MonsterData);

    public override void LoadFromSO(uint idx)
    {
        ofsBoard = null;
        dfsBoardList = new List<BBoard>();
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
        if(board != null)
        {
            if(board.Type == BBoardType.Offensive)
            {
                ofsBoard = board;
            }
            else if(board.Type == BBoardType.Defensive)
            {
                this.dfsBoardList.Add(board);
            }
        }
    }

    public void DelBoard(BBoard board)
    {
        if (board != null)
        {
            board.ReleaseBoard();
            Factory.Instance.ReleaseBoard(board);

            if(board.Type == BBoardType.Offensive)
            {
                ofsBoard = null;
            }
            else if(board.Type == BBoardType.Defensive)
            {
                this.dfsBoardList.Remove(board);
            }
        }
    }

    public BBoard GetOffensiveBoard()
    {
        return ofsBoard;
    }

    public BBoard GetDefensiveBoard(int i)
    {
        if(dfsBoardList.Count > 0)
        {
            return this.dfsBoardList[i];
        }

        return null;
    }

    public void DelAttackBoardList()
    {
        for (int i = 1; i < this.dfsBoardList.Count; i++)
        {
            DelBoard(dfsBoardList[i]);
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
        if(ofsBoard != null)
        {
            return ofsBoard.Width;
        }
        else if(dfsBoardList.Count > 0)
        {
            return dfsBoardList[0].Width;
        }

        return (int)monsterData.BoardDefaultWidth;
    }

    public int GetBoardHeight()
    {
        if (ofsBoard != null)
        {
            return ofsBoard.Height;
        }
        else if (dfsBoardList.Count > 0)
        {
            return dfsBoardList[0].Height;
        }

        return (int)monsterData.BoardDefaultHeight;
    }
}