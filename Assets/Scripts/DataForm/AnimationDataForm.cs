using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Commons;

[CreateAssetMenu(fileName = "AnimationData", menuName = "ScriptableObjects/AnimationData", order = 1)]
public class AnimationDataForm : ScriptableObject, IDataLoad
{
    public Dictionary<uint, AnimationData> DB { get; private set; } = null;

    [SerializeField]
    public AnimationData[] AnimationData;


    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, AnimationData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in AnimationData)
        {
            AnimationData newData = new AnimationData
            {
                Idx = data.Idx,
                ControllerKey = data.ControllerKey,
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