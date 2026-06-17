using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

[CreateAssetMenu(fileName = "StageDataForm", menuName = "ScriptableObjects/StageDataForm", order = 1)]
public class StageDataForm : ScriptableObject, IDataLoad
{
    public Dictionary<uint, StageData> DB { get; private set; } = null;

    [SerializeField]
    public StageData[] StageData = null;


    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, StageData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in StageData)
        {
            StageData newData = new StageData
            {
                Idx = data.Idx,
                Stage = data.Stage,
                SubStage = data.SubStage,
                MonsterIdx = data.MonsterIdx,
                MonsterCount = data.MonsterCount,
                RewardIdx = data.RewardIdx,
            };

            if (DB.ContainsKey(newData.Idx))
            {
                Debug.LogError($"Already contained data, stage, idx:{newData.Idx}");
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