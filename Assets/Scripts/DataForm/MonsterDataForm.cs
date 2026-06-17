using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

[CreateAssetMenu(fileName = "MonsterDataForm", menuName = "ScriptableObjects/MonsterDataForm", order = 1)]
public class MonsterDataForm: ScriptableObject, IDataLoad
{
    public Dictionary<uint, MonsterData> DB { get; private set; } = null;

    [SerializeField]
    public MonsterData[] MonsterData = null;


    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, MonsterData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in MonsterData)
        {
            MonsterData newData = new MonsterData
            {
                Idx = data.Idx,
                UnitIdx = data.UnitIdx,
                PatternIdx = data.PatternIdx,
                SizeScale = data.SizeScale,
                BoardDefaultWidth = data.BoardDefaultWidth,
                BoardDefaultHeight = data.BoardDefaultHeight,
            };

            if (DB.ContainsKey(newData.Idx))
            {
                Debug.LogError($"Already contained data, monster, idx:{newData.Idx}");
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