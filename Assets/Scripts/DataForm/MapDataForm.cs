using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

public class MapDataForm : IDataLoad
{
    public Dictionary<uint, MapData> DB { get; private set; } = null;

    public void LoadData(string csvText)
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, MapData>();
        }
        else
        {
            DB.Clear();
        }

        var dataList = Util.ParseFromCSV<MapData>(csvText);

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