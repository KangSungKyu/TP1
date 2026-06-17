using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Commons;

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    public UserData UserData { get; set; } = null;
    public StageClearData StageClearData { get; set; } = null;

    private static string GetPath(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

    private SavedUserData savedUserData = null;
    private SavedStageClearData savedStageClearData = null;

    public void SaveUserData()
    {
        savedUserData = UserData.Convert();

        Save("userData.json", savedUserData);
    }

    public void SaveStageClearData()
    {
        savedStageClearData = StageClearData.Convert();

        Save("stageClearData.json", savedStageClearData);
    }

    public void LoadUserData()
    {
        savedUserData = Load<SavedUserData>("userData.json");

        if(savedUserData == default)
        {
            savedUserData = new SavedUserData()
            {
                Level = 1,
                Exp = 0,
                MapIdx = 1,
                StageIdx = 1,
                SavedMapIdx = 1,
                SavedStageIdx = 1,
            };
        }

        UserData = savedUserData.Convert();
    }

    public void LoadStageClearData()
    {
        savedStageClearData = Load<SavedStageClearData>("stageClearData.json");

        if(savedStageClearData == default)
        {
            savedStageClearData = new SavedStageClearData()
            {
                ClearDatas = new List<ClearData>(),
            };
        }

        StageClearData = savedStageClearData.Convert();
    }

    public void Save<T>(string fileName, T data) where T : class, new()
    {
        string json = JsonUtility.ToJson(data, true); // true: 가독성 좋게 들여쓰기

        File.WriteAllText(GetPath(fileName), json);
        Debug.Log("데이터 저장 완료: " + GetPath(fileName));
    }

    public T Load<T>(string fileName) where T : class, new()
    {
        string path = GetPath(fileName);

        if(!Find(fileName))
            return default; // 없으면 기본 객체 반환

        string json = File.ReadAllText(path);

        return JsonUtility.FromJson<T>(json);
    }

    public bool Find(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }
}