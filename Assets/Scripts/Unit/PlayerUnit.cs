using UnityEngine;
using System.Collections;
using static Commons;
using System;

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