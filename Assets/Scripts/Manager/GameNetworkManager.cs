using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static Commons;

public class GameNetworkManager : Singleton<GameNetworkManager>
{
    [SerializeField]
    private bool isDebugMode = true;
    [SerializeField]
    private Canvas progressCanvas = null;

    private static string[] server_list =
    {
        "http://localhost:80",
        "http://3.34.190.7:80",
    };

    private string server_url = string.Empty;


    public static APIResponseData<T> CreateAPIResponseDataFromJson<T>(string json)
    {
        var res = FromJson<APIResponseData<T>>(json);

        return res;
    }

    public void SaveUserData(UserData userData, System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        string json = ToJson(userData);

        StartCoroutine(IEPostRequest($"{server_url}/update_userdata", json, onComplete, onFailed));
    }

    public void LogIn(ClientData clientData, System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        string json = ToJson(clientData);

        StartCoroutine(IEPostRequest($"{server_url}/login_user", json, onComplete, onFailed));
    }

    public void LogOut(System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        if(SaveLoadManager.Instance != null)
        {
            int userId = SaveLoadManager.Instance.UserData.UserId;
            var dto = new
            {
                UserId = userId,
            };
            string json = ToJson(dto);

            StartCoroutine(IEPostRequest($"{server_url}/logout_user", json, onComplete, onFailed));
        }
    }

    public void SaveStageClearData(uint stageIdx, uint clearState, System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            StageIdx = stageIdx,
            ClearState = clearState,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_stagecleardata", json, onComplete, onFailed));
    }

    public void LoadStageClearData(uint userId, System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        StartCoroutine(IEGetRequest($"{server_url}/get_stagecleardata/{userId}", onComplete, onFailed));
    }

    public void UpdateUserLevel(System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        int level = SaveLoadManager.Instance.UserData.Level;
        int exp = SaveLoadManager.Instance.UserData.Exp;
        var dto = new
        {
            UserId = userId,
            Level = level,
            Exp = exp,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_userlevel", json, onComplete, onFailed));
    }

    public void UpdateDefeatStage(System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_defeatstage", json, onComplete, onFailed));
    }

    public void UpdateClearStage(int stageIdx, Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            StageIdx = stageIdx,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_clearstage", json, onComplete, onFailed));
    }
    
    public void LoadUserSkillData(uint userId, Action<string> onComplete = null, Action<string> onFailed = null)
    {
        StartCoroutine(IEGetRequest($"{server_url}/get_userskilldata/{userId}", onComplete, onFailed));
    }

    public void UpdateBuyUserSkill(int skillIdx, Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            SkillIdx = skillIdx,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_buyuserskill", json, onComplete, onFailed));
    }

    public void UpdateEquipUserSkill(int skillIdx, int slot, Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            SkillIdx = skillIdx,
            Slot = slot,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_equipuserskill", json, onComplete, onFailed));
    }

    public void UpdateUnEquipUserSkill(int slot, Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            Slot = slot,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_unequipuserskill", json, onComplete, onFailed));
    }

    public void UpdateEnterUserStage(int mapIdx, int stageIdx, Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            MapIdx = mapIdx,
            StageIdx = stageIdx,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_enteruserstage", json, onComplete, onFailed));
    }

    public void UpdateUsedUserSkill(UsedUserSkillData[] usedSkillList, Action<string> onComplete = null, Action<string> onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            UsedSkillList = usedSkillList,
        };
        string json = ToJson(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_useduserskill", json, onComplete, onFailed));
    }

    private void OnOffProgressUI(bool onoff)
    {
        progressCanvas.enabled = onoff;
    }

    private IEnumerator IEPostRequest(string url, string json, System.Action<string> onComplete = null, System.Action<string> onFailed = null)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            OnOffProgressUI(true);

            yield return www.SendWebRequest();

            OnOffProgressUI(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                string resultJson = www.downloadHandler.text;

                onComplete?.Invoke(resultJson);
            }
            else
            {
                string resultJson = www.downloadHandler.text;

                onFailed?.Invoke(resultJson);
                Debug.LogError(www.error);
            }
        }
    }

    private IEnumerator IEGetRequest(string url, System.Action<string> onComplete = null, System.Action<string> onFailed = null) 
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "GET"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            OnOffProgressUI(true);

            yield return www.SendWebRequest();

            OnOffProgressUI(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                string resultJson = www.downloadHandler.text;

                onComplete?.Invoke(resultJson);
            }
            else
            {
                string resultJson = www.downloadHandler.text;

                onFailed?.Invoke(resultJson);
                Debug.LogError(www.error);
            }
        }
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();

        server_url = isDebugMode ? server_list[0] : server_list[1];

        OnOffProgressUI(false);
    }

    private void OnApplicationQuit()
    {
        LogOut();
    }
}