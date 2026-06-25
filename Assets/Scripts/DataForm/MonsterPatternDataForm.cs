using System.Collections.Generic;
using static Commons;

public class MonsterPatternDataForm : IDataLoad
{
    public Dictionary<uint, MonsterPatternData> DB { get; private set; } = null;

    public void LoadData(string csvText)
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, MonsterPatternData>();
        }
        else
        {
            DB.Clear();
        }

        var dataList = Util.ParseFromCSV<MonsterPatternData>(csvText);

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