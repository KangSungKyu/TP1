using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class UnitSpawnerService
{
    public static PlayerUnit SpawnPlayer(Transform transform, Vector2 spawnPos, uint level, RectTransform hpUIContainter, System.Action<PlayerUnit, float> onChangeHP, System.Action<UnitBase> onATBReady, RectTransform atbContainer, System.Action<Image, float> onATBRatio)
    {
        UnitBase unit = Factory.Instance.GetPlayerUnit(transform, spawnPos);
        PlayerUnit player = null;

        if (unit != null)
        {
            player = unit as PlayerUnit;
            LevelBaseData lbd = DataTableManager.Instance.GetLevelBaseData(level);

            player.LoadFromSO(Util.CreateDataIdx(DataTableType.UnitData, 1));
            player.SetLevelBase(lbd);
            player.SetHPUI(Factory.Instance.GetHPUI(hpUIContainter));
            player.Subscribe_HP((v) => onChangeHP(player, v));
            player.OnATBReady.Subscribe(onATBReady).AddTo(player);

            Image playerPort = Factory.Instance.GetPortraitUI(atbContainer);

            player.SetPortraitUI(playerPort);
            player.Subscribe_ATBGauge((v) => onATBRatio(playerPort, player.ATBRatio));
        }

        return player;
    }

    public static MonsterUnit SpawnMonster(Transform transform, Vector2 spawnPos, UnitBase targetUnit, uint idx, RectTransform hpUIContainer, System.Action<float> onChangeHP, System.Action<UnitBase> onATBReady, RectTransform atbContainer, System.Action<Image, float> onATBRatio)
    {
        UnitBase unit = Factory.Instance.GetMonsterUnit(transform, spawnPos);
        MonsterUnit monsterUnit = null;

        if(unit != null)
        {
            monsterUnit = unit as MonsterUnit;

            monsterUnit.SetTarget(targetUnit);
            monsterUnit.LoadFromSO(idx);
            monsterUnit.SetHPUI(Factory.Instance.GetHPUI(hpUIContainer));
            monsterUnit.Subscribe_HP(onChangeHP);
            monsterUnit.OnATBReady.Subscribe(onATBReady).AddTo(monsterUnit);

            Image monsterPort = Factory.Instance.GetPortraitUI(atbContainer);

            monsterUnit.SetPortraitUI(monsterPort);
            monsterUnit.Subscribe_ATBGauge((v) => onATBRatio(monsterPort, monsterUnit.ATBRatio));
        }

        return monsterUnit;
    }

    public static void DespawnUnitList(List<UnitBase> unitList)
    {
        for (int i = 0; i < unitList.Count; ++i)
        {
            unitList[i]?.Release();

            if (unitList[i] is PlayerUnit)
            {
                Factory.Instance.ReleasePlayerUnit(unitList[i] as PlayerUnit);
            }
            else
            {
                Factory.Instance.ReleaseMonsterUnit(unitList[i] as MonsterUnit);
            }
        }
    }
}
