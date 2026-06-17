using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

[CreateAssetMenu(fileName = "MapData", menuName = "ScriptableObjects/MapData", order = 1)]
public class MapDataForm : ScriptableObject, IDataLoad
{
    public Dictionary<uint, MapData> DB { get; private set; } = null;

    [SerializeField]
    public MapData[] MapData;


    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, MapData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in MapData)
        {
            MapData newData = new MapData
            {
                Idx = data.Idx,
                StageIdx = data.StageIdx,
            };

            if (DB.ContainsKey(newData.Idx))
            {
                Debug.LogError($"Already contained data, anim, idx:{newData.Idx}");
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