using UnityEngine;
using System.Collections;
using static Commons;

public static class UnitCalculator
{
    public static void ApplyDamage(UnitBase attacker, UnitBase defender, ApplyStatusData applyAttackerStatus, ApplyStatusData applyDefenderStatus)
    {
        attacker.ApplyStatus(applyAttackerStatus);
        defender.ApplyStatus(applyDefenderStatus);

        float damage = CalculateDamage(attacker, defender);

        defender.ApplyDamage(damage);
    }

    public static float CalculateDamage(UnitBase attacker, UnitBase defender)
    {
        if (attacker.ShieldCrushTime > 0f)
        {
            defender.ShieldCrush(attacker.ShieldCrushTime);
            Debug.Log($"attacker shieldCrush, time:{attacker.ShieldCrushTime} -> defender {defender.name}");
        }

        float damage = Mathf.Max(0, attacker.Atk - defender.Def);
        int rnd = Random.Range(0, 1000);

        if (defender.IsShield)
        {
            Debug.Log($"defender on shield, origin damage:{damage}");

            damage = Mathf.FloorToInt(damage * 0.01f);
        }

        if (defender.Dodge > rnd)
        {
            damage = 0;
        }

        return damage;
    }

}