using System.Collections.Generic;
using static Commons;

public class MonsterDataForm: IDataLoad
{
    public Dictionary<uint, MonsterData> DB { get; private set; } = null;

    public int GetDataCount()
    {
        return DB != null ? DB.Count : 0;
    }

    public void LoadData(string csvText)
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, MonsterData>();
        }
        else
        {
            DB.Clear();
        }

        var dataList = Util.ParseFromCSV<MonsterData>(csvText);

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