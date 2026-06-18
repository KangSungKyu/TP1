using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using static Commons;

public class GameNetworkManager : Singleton<GameNetworkManager>
{
    private string server_url = "http://localhost:5000";


    public APIResponseData<T> CreateAPIResponseDataFromJson<T>(string json)
    {
        var res = JsonConvert.DeserializeObject<APIResponseData<T>>(json);

        return res;
    }

    public void SaveUserData(SavedUserData savedUserData, System.Action<string> onComplete = null, System.Action onFailed = null)
    {
        string json = JsonConvert.SerializeObject(savedUserData);

        StartCoroutine(IEPostRequest($"{server_url}/update_userdata", json, onComplete, onFailed));
    }

    public void LoadUserData(ClientData clientData, System.Action<string> onComplete = null, System.Action onFailed = null)
    {
        string json = JsonConvert.SerializeObject(clientData);

        StartCoroutine(IEPostRequest($"{server_url}/login_user", json, onComplete, onFailed));
    }

    public void SaveStageClearData(uint stageIdx, uint clearState, System.Action<string> onComplete = null, System.Action onFailed = null)
    {
        int userId = SaveLoadManager.Instance.UserData.UserId;
        var dto = new
        {
            UserId = userId,
            StageIdx = stageIdx,
            ClearState = clearState,
        };
        string json = JsonConvert.SerializeObject(dto);

        StartCoroutine(IEPostRequest($"{server_url}/update_stagecleardata", json, onComplete, onFailed));
    }

    public void LoadStageClearData(uint userId, System.Action<string> onComplete = null, System.Action onFailed = null)
    {
        StartCoroutine(IEGetRequest($"{server_url}/get_stagecleardata/{userId}", onComplete, onFailed));
    }


    private IEnumerator IEPostRequest(string url, string json, System.Action<string> onComplete = null, System.Action onFailed = null)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.Success)
            {
                string resultJson = www.downloadHandler.text;

                onComplete?.Invoke(resultJson);
            }
            else
            {
                onFailed?.Invoke();
                Debug.LogError(www.error);
            }
        }
    }

    private IEnumerator IEGetRequest(string url, System.Action<string> onComplete = null, System.Action onFailed = null) 
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "GET"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if(www.result == UnityWebRequest.Result.Success)
            {
                string resultJson = www.downloadHandler.text;

                onComplete?.Invoke(resultJson);
            }
            else
            {
                onFailed?.Invoke();
                Debug.LogError(www.error);
            }
        }
    }
}