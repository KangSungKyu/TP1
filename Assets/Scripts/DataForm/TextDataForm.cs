using UnityEngine;
using System.Collections;
using static Commons;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TextDataForm", menuName = "ScriptableObjects/TextDataForm", order = 1)]
public class TextDataForm : ScriptableObject, IDataLoad
{
    public Dictionary<uint, TextData> DB { get; private set; } = null;

    [SerializeField]
    public TextData[] TextData = null;


    public void LoadData()
    {
        if (DB == null)
        {
            DB = new Dictionary<uint, TextData>();
        }
        else
        {
            DB.Clear();
        }

        foreach (var data in TextData)
        {
            TextData newData = new TextData
            {
                Idx = data.Idx,
                Text = data.Text,
            };

            if (DB.ContainsKey(newData.Idx))
            {
                Debug.LogError($"Already contained data, text, idx:{newData.Idx}");
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