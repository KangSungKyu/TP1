using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

public class UnitDataForm : IDataLoad
{
    public Dictionary<uint, UnitData> DB { get; private set; } = null;

    public void LoadData(string csvText)
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, UnitData>();
        }
        else
        {
            DB.Clear();
        }

        var dataList = Util.ParseFromCSV<UnitData>(csvText);

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
