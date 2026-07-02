using System.Collections.Generic;
using static Commons;

public class SkillDataForm : IDataLoad
{
    public Dictionary<uint, SkillData> DB { get; private set; } = null;

    public int GetDataCount()
    {
        return DB != null ? DB.Count : 0;
    }

    public void LoadData(string csvText)
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, SkillData>();
        }
        else
        {
            DB.Clear();
        }

        var dataList = Util.ParseFromCSV<SkillData>(csvText);

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
