using CsvHelper;
using CsvHelper.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static Commons;
using static UnityEngine.Rendering.STP;

public class AnimationDataForm : IDataLoad
{
    public Dictionary<uint, AnimationData> DB { get; private set; } = null;

    public void LoadData(string csvText)
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, AnimationData>();
        }
        else
        {
            DB.Clear();
        }

        var dataList = Util.ParseFromCSV<AnimationData>(csvText);

        for(int i = 0; i < dataList.Count; ++i)
        {
            var data = dataList[i];

            if(!DB.ContainsKey(data.Idx))
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