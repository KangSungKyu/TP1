using UnityEngine;
using System.Collections;
using static Commons;

public static class UnitCalculator
{
    public static void ApplyDamage(UnitBase attacker, UnitBase defender, ApplyStatusData applyAttackerStatus, ApplyStatusData applyDefenderStatus, SkillData[] skillList = null)
    {
        attacker.ApplyStatus(applyAttackerStatus);
        defender.ApplyStatus(applyDefenderStatus);

        float damage = CalculateDamage(attacker, defender, skillList);

        defender.ApplyDamage(damage);
    }

    public static float CalculateDamage(UnitBase attacker, UnitBase defender, SkillData[] skillList = null)
    {
        if (attacker.ShieldCrushTime > 0f)
        {
            defender.ShieldCrush(attacker.ShieldCrushTime);
            Debug.Log($"attacker shieldCrush, time:{attacker.ShieldCrushTime} -> defender {defender.name}");
        }

        float attackerAtk = attacker.Atk;

        if (skillList != null)
        {
            for (int i = 0; i < skillList.Length; ++i)
            {
                float perValue = (float)(skillList[i].PerValue * 0.001f);
                attackerAtk *= perValue;
            }
        }

        float damage = Mathf.Max(0, attackerAtk - defender.Def);
        int rnd = Random.Range(0, 1000);

        if (defender.IsShield)
        {
            Debug.Log($"defender on shield, origin damage:{damage}");

            damage = Mathf.FloorToInt(damage * 0.01f);
        }

        if (defender.Dodge > rnd)
        {
            damage = 0;

            //dodge action
            //defender.PlayAction(UnitActionData.DefaultAction_None);
        }

        return damage;
    }

}