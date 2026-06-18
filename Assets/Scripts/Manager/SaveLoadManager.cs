using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using static Commons;

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    public ClientData ClientData { get; private set; } = null;
    public UserData UserData { get; set; } = null;
    public StageClearData StageClearData { get; set; } = null;

    private static string GetPath(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

    private ClientData clientData = null;
    private SavedUserData savedUserData = null;
    private SavedStageClearData savedStageClearData = null;

    public void SaveClientData()
    {
        Save("clientData.json", clientData);
    }

    public void LoadClientData()
    {
        clientData = Load<ClientData>("clientData.json");

        if(clientData == null)
        {
            clientData = ClientData.CreateClientData();

            SaveClientData();
        }
    }

    public void SaveUserData(System.Action<string> onComp, System.Action onFail)
    {
        savedUserData = UserData.Convert();
        clientData.ClientId = savedUserData.ClientId;

        //Save("userData.json", savedUserData);
        GameNetworkManager.Instance.SaveUserData(savedUserData, onComp, onFail);
    }

    public void LoadUserData(System.Action onComp, System.Action onFail)
    {
        GameNetworkManager.Instance.LoadUserData(clientData, (json) =>
        {
            //savedUserData = Load<SavedUserData>("userData.json");
            APIResponseData<SavedUserData> res = GameNetworkManager.Instance.CreateAPIResponseDataFromJson<SavedUserData>(json);

            if(res.data != null)
            {
                savedUserData = res.data;
                UserData = savedUserData.Convert();
                clientData.ClientId = UserData.ClientId;

                onComp?.Invoke();
            }
        }, onFail);
    }

    public void SaveStageClearData(int stageIdx, int clearState, System.Action<string> onComp, System.Action onFail)
    {
        savedStageClearData = StageClearData.Convert();

        //Save("stageClearData.json", savedStageClearData);
        GameNetworkManager.Instance.SaveStageClearData((uint)stageIdx, (uint)clearState, onComp, onFail);
    }

    public void LoadStageClearData(System.Action onComp, System.Action onFail)
    {
        GameNetworkManager.Instance.LoadStageClearData((uint)UserData.UserId, (json) =>
        {
            //savedStageClearData = Load<SavedStageClearData>("stageClearData.json");
            APIResponseData<List<ClearData>> res = GameNetworkManager.Instance.CreateAPIResponseDataFromJson<List<ClearData>>(json);

            if(res.data != null)
            {
                savedStageClearData = new SavedStageClearData() { UserId = UserData.UserId, ClearDatas = res.data };
                StageClearData = savedStageClearData.Convert();

                onComp?.Invoke();
            }
        }, onFail);
    }

    public string ToJson<T>(T data) where T : class, new()
    {
        string json = JsonConvert.SerializeObject(data); // true: 가독성 좋게 들여쓰기

        return json;
    }

    public T FromJson<T>(string json) where T : class, new()
    {
        T data = default;

        if (json != string.Empty)
        {
            data = JsonConvert.DeserializeObject<T>(json);
        }

        return data;
    }

    public void Save<T>(string fileName, T data) where T : class, new()
    {
        File.WriteAllText(GetPath(fileName), ToJson(data));
        Debug.Log("데이터 저장 완료: " + GetPath(fileName));
    }

    public T Load<T>(string fileName) where T : class, new()
    {
        string path = GetPath(fileName);

        if(!Find(fileName))
            return default; // 없으면 기본 객체 반환

        string json = File.ReadAllText(path);

        return FromJson<T>(json);
    }

    public bool Find(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }
}