using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using static Commons;

public class GameNetworkManager : Singleton<GameNetworkManager>
{
    private enum SendRequestMethodType
    {
        POST = 0,
        GET,

        SendRequestMethodType_End
    }

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
        var res = Util.FromJson<APIResponseData<T>>(json);

        return res;
    }

    public async UniTask<string> SaveUserDataAsync(UserData userData, CancellationToken cancellationToken = default)
    {
        string json = Util.ToJson(userData);

        return await SendWebRequestAsync($"{server_url}/update_userdata", SendRequestMethodType.POST, json, cancellationToken);
    }

    public async UniTask<string> LogInAsync(ClientData clientData, CancellationToken cancellationToken = default)
    {
        string json = Util.ToJson(clientData);

        return await SendWebRequestAsync($"{server_url}/login_user", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> LogOutAsync(CancellationToken cancellationToken = default)
    {
        if (SaveLoadManager.Instance != null)
        {
            int userId = SaveLoadManager.Instance.UserData.UserId;
            var dto = new
            {
                UserId = userId,
            };
            string json = Util.ToJson(dto);

            return await SendWebRequestAsync($"{server_url}/logout_user", SendRequestMethodType.POST, json);
        }

        return string.Empty;
    }

    public async UniTask<string> SaveStageClearDataAsync(uint stageIdx, uint clearState, CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            StageIdx = stageIdx,
            ClearState = clearState,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_stagecleardata", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> LoadStageClearDataAsync(uint userId, CancellationToken cancellationToken = default)
    {
        return await SendWebRequestAsync($"{server_url}/get_stagecleardata/{userId}", SendRequestMethodType.GET);
    }

    public async UniTask<string> UpdateUserLevelAsync(CancellationToken cancellationToken = default)
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
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_userlevel", SendRequestMethodType.POST, json);
    }
    
    public async UniTask<string> UpdateDefeatStageAsync(CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_defeatstage", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> UpdateClearStageAsync(int stageIdx, CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            StageIdx = stageIdx,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_clearstage", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> LoadUserSkillDataAsync(uint userId, CancellationToken cancellationToken = default)
    {
        return await SendWebRequestAsync($"{server_url}/get_userskilldata/{userId}", SendRequestMethodType.GET);
    }

    public async UniTask<string> UpdateBuyUserSkillAsync(int skillIdx, CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            SkillIdx = skillIdx,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_buyuserskill", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> UpdateEquipUserSkillAsync(int skillIdx, int slot, CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            SkillIdx = skillIdx,
            Slot = slot,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_equipuserskill", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> UpdateUnEquipUserSkillAsync(int slot, CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            Slot = slot,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_unequipuserskill", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> UpdateEnterUserStageAsync(int mapIdx, int stageIdx, CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            MapIdx = mapIdx,
            StageIdx = stageIdx,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_enteruserstage", SendRequestMethodType.POST, json);
    }

    public async UniTask<string> UpdateUsedUserSkillAsync(UsedUserSkillData[] usedSkillList, CancellationToken cancellationToken = default)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            UsedSkillList = usedSkillList,
        };
        string json = Util.ToJson(dto);

        return await SendWebRequestAsync($"{server_url}/update_useduserskill", SendRequestMethodType.POST, json);
    }

    private void OnOffProgressUI(bool onoff)
    {
        progressCanvas.enabled = onoff;
    }

    private async UniTask<string> SendWebRequestAsync(string url, SendRequestMethodType method, string json = null, CancellationToken cancellationToken = default)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, $"{method}"))
        {
            if(method == SendRequestMethodType.POST && !string.IsNullOrEmpty(json))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            OnOffProgressUI(true);

            try
            {
                await www.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                if (www.result == UnityWebRequest.Result.Success)
                {
                    return www.downloadHandler.text;
                }
                else
                {
                    throw new System.Exception(www.downloadHandler.text);
                }
            }
            finally
            {
                OnOffProgressUI(false);
            }
        }
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

    private async void OnApplicationQuit()
    {
        await LogOutAsync();
    }
}