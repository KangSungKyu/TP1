using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardDataForm", menuName = "ScriptableObjects/RewardDataForm", order = 1)]
public class RewardDataForm : ScriptableObject, IDataLoad
{
    public Dictionary<uint, RewardData> DB { get; private set; } = null;

    [SerializeField]
    public RewardData[] RewardData = null;

    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, RewardData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in RewardData)
        {
            RewardData newData = new RewardData
            {
                Idx = data.Idx,
                Exp = data.Exp,
            };

            if (DB.ContainsKey(newData.Idx))
            {
                Debug.LogError($"Already contained data, reward, idx:{newData.Idx}");
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