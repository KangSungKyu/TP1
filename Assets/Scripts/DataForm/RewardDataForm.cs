using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

public class RewardDataForm : IDataLoad
{
    public Dictionary<uint, RewardData> DB { get; private set; } = null;

    public void LoadData(string csvText)
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, RewardData>();
        }
        else
        {
            DB.Clear();
        }

        var dataList = Util.ParseFromCSV<RewardData>(csvText);

        for (int i = 0; i < dataList.Count; ++i)
        {
            var data = dataList[i];

            if (!DB.ContainsKey(data.Idx))
            {
                DB.Add(data.Idx, data);
            }
        }
    }
    public void Release()
    {
        DB?.Clear();
    }
}