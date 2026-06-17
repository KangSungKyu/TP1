using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelBaseDataForm", menuName = "ScriptableObjects/LevelBaseDataForm", order = 1)]
public class LevelBaseDataForm : ScriptableObject, IDataLoad
{
    public Dictionary<uint, LevelBaseData> DB { get; private set; } = null;

    [SerializeField]
    public LevelBaseData[] LevelBaseData = null;

    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, LevelBaseData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in LevelBaseData)
        {
            LevelBaseData newData = new LevelBaseData
            {
                Idx = data.Idx,
                Level = data.Level,
                NeedExp = data.NeedExp,
                MaxHp = data.MaxHp,
                Atk = data.Atk,
                Def = data.Def,
                Dodge = data.Dodge,
                Spd = data.Spd,
            };

            if (DB.ContainsKey(newData.Idx))
            {
                Debug.LogError($"Already contained data, levelBase, idx:{newData.Idx}");
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