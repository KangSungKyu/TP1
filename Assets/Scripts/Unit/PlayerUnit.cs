using UnityEngine;
using System.Collections;
using static Commons;
using System.Linq;

public class PlayerUnit : UnitBase
{
    public override void LoadFromSO(uint idx)
    {
        info = DataTableManager.Instance.GetUnitData(idx);
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

    public void SetLevelBase(LevelBaseData lbd)
    {
        info.MaxHp += lbd.MaxHp;
        info.Atk += lbd.Atk;
        info.Def += lbd.Def;
        info.Dodge += lbd.Dodge;
        info.Spd += lbd.Spd;

        info.Hp = info.MaxHp;

        usageUnitData.Hp.Value = info.Hp;
        usageUnitData.MaxHp.Value = info.MaxHp;
        usageUnitData.Atk = info.Atk;
        usageUnitData.Def = info.Def;
        usageUnitData.Dodge = info.Dodge;
        usageUnitData.Spd = info.Spd;
        usageUnitData.ShieldCrushTime = 0.0f;
    }

    public override void DrawTargetLine()
    {
        QuadraticBezierRenderer toTarget = null;

        if(targetLineList.Count > 0)
        {
            toTarget = targetLineList[0];
        }
        else
        {
            toTarget = Factory.Instance.GetTargetLine(transform.parent, Color.yellow);

            targetLineList.Add(toTarget);
        }

        if (toTarget != null && targetUnit != null)
        {
            Vector3 pA = GetUnitHeadPosition();
            Vector3 pB = targetUnit.GetUnitHeadPosition();
            Vector3 up = Vector2.Perpendicular((pB - pA).normalized);
            float dist = Vector2.Distance(pA, pB);
            Vector3 ctrlP = Vector3.Lerp(pA, pB, 0.35f) + up * dist * Random.Range(0.3f, 0.5f);

            toTarget.SetBezierPoints(pA, ctrlP, pB);
            toTarget.Render();
        }
    }

    protected override void Init()
    {
        SetShield(false);

        if (animator != null)
        {
            animator.runtimeAnimatorController = ResourceManager.Instance.GetResource<RuntimeAnimatorController>(DataTableManager.Instance.GetAnimationData(info.AnimGroupIdx).ControllerKey);
        }

        Subscribe_HP((hp) =>
        {
            Debug.Log($"Player, HP: {hp}, MaxHP: {MaxHp}");
        });

        Subscribe_MaxHP((maxhp) =>
        {
            Debug.Log($"Player, HP: {Hp}, MaxHP: {maxhp}");
        });
    }
}