using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using static Commons;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class SaveLoadManager : Singleton<SaveLoadManager>
{
    public ClientData ClientData = null;
    public UserData UserData = null;
    public StageClearData StageClearData = null;
    public UserSkillData UserSkillData = null;

    private static string GetPath(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

    public void SaveClientData()
    {
        Save("clientData.json", ClientData);
    }

    public void LoadClientData()
    {
        ClientData = Load<ClientData>("clientData.json");

        if(ClientData == null)
        {
            ClientData = ClientData.CreateClientData();

            SaveClientData();
        }
    }

    //public void SaveUserData(System.Action<string> onComp, System.Action<string> onFail)
    //{
    //    ClientData.ClientId = UserData.ClientId;
    //
    //    GameNetworkManager.Instance.SaveUserData(UserData, onComp, onFail);
    //}

    public async UniTask SaveUserDataAsync(CancellationToken cancellationToken = default)
    {
        ClientData.ClientId = UserData.ClientId;
    
        try
        {
            string json = await GameNetworkManager.Instance.SaveUserDataAsync(UserData, cancellationToken);
        }
        catch(Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public async void LoadUserData(System.Action onComp, System.Action<string> onFail)
    {
        try
        {
            string json = await GameNetworkManager.Instance.LogInAsync(ClientData, this.GetCancellationTokenOnDestroy());
            APIResponseData<LoginData> res = GameNetworkManager.CreateAPIResponseDataFromJson<LoginData>(json);

            if (res.data.userData != null)
            {
                UserData = res.data.userData;
                ClientData.ClientId = UserData.ClientId;
            }

            if (res.data.stageClearData != null)
            {
                StageClearData.UserId = UserData.UserId;
                StageClearData.ClearDatas = res.data.stageClearData.ToDictionary((o) => o.StageIdx);
            }

            if (res.data.userSkillData != null)
            {
                UserSkillData = new UserSkillData() { SkillSlots = res.data.userSkillData };
            }

            onComp?.Invoke();
        }
        catch(System.Exception ex)
        {
            onFail?.Invoke(ex.Message);
        }
    }

    public async void SaveStageClearData(int stageIdx, int clearState, System.Action<string> onComp, System.Action<string> onFail)
    {
        try
        {
            string json = await GameNetworkManager.Instance.SaveStageClearDataAsync((uint)stageIdx, (uint)clearState, this.GetCancellationTokenOnDestroy());

            onComp?.Invoke(json);
        }
        catch (System.Exception ex)
        {
            onFail?.Invoke(ex.Message);
        }
    }

    public async void LoadStageClearData(System.Action onComp, System.Action<string> onFail)
    {
        try
        {
            string json = await GameNetworkManager.Instance.LoadStageClearDataAsync((uint)UserData.UserId, this.GetCancellationTokenOnDestroy());
            APIResponseData<List<ClearData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<ClearData>>(json);

            if (res.data != null)
            {
                StageClearData.UserId = UserData.UserId;
                StageClearData.ClearDatas = res.data.ToDictionary((o) => o.StageIdx);

                onComp?.Invoke();
            }
        }
        catch(System.Exception ex)
        {
            onFail?.Invoke(ex.Message);
        }
    }

    public async void LoadUserSkillData(System.Action onComp, System.Action<string> onFail)
    {
        try
        {
            string json = await GameNetworkManager.Instance.LoadUserSkillDataAsync((uint)UserData.UserId, this.GetCancellationTokenOnDestroy());
            APIResponseData<List<SkillSlotData>> res = GameNetworkManager.CreateAPIResponseDataFromJson<List<SkillSlotData>>(json);

            if (res.data != null)
            {
                UserSkillData.SkillSlots = res.data;

                onComp?.Invoke();
            }
        }
        catch (System.Exception ex)
        {
            onFail?.Invoke(ex.Message);
        }
    }

    public void Save<T>(string fileName, T data) where T : class, new()
    {
        File.WriteAllText(GetPath(fileName), Util.ToJson(data));
        Debug.Log("데이터 저장 완료: " + GetPath(fileName));
    }

    public T Load<T>(string fileName) where T : class, new()
    {
        string path = GetPath(fileName);

        if(!Find(fileName))
            return default; // 없으면 기본 객체 반환

        string json = File.ReadAllText(path);

        return Util.FromJson<T>(json);
    }

    public bool Find(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }

    protected override void OnSingletonAwake()
    {
        ClientData = null;
        UserData = new UserData();
        StageClearData = new StageClearData();
        UserSkillData = new UserSkillData();
    }
}