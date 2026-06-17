using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

[CreateAssetMenu(fileName = "UnitDataForm", menuName = "ScriptableObjects/UnitDataForm", order = 1)]
public class UnitDataForm : ScriptableObject, IDataLoad
{
    public Dictionary<uint, UnitData> DB { get; private set; } = null;

    [SerializeField]
    public UnitData[] UnitData = null;

    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, UnitData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in UnitData)
        {
            UnitData newData = new UnitData
            {
                Idx = data.Idx,
                NameIdx = data.NameIdx,
                AnimGroupIdx = data.AnimGroupIdx,
                Hp = data.Hp,
                MaxHp = data.MaxHp,
                Atk = data.Atk,
                Def = data.Def,
                Dodge = data.Dodge,
                Spd = data.Spd,
                PortraitSpr = data.PortraitSpr,
            };

            if (DB.ContainsKey(newData.Idx))
            {
                Debug.LogError($"Already contained data, unit, idx:{newData.Idx}");
            }
            else
            {
                DB.Add(newData.Idx, newData);
            }
        }
    }
    public void Release()
    {
        DB?.Clear();
    }
}
