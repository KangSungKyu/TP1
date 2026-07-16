using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

public class StageMapUI : MonoBehaviour
{
    [SerializeField]
    private int StageMapIdx = 0;
    [SerializeField]
    private StageUI[] stageUI = null;

    private MapData data = default;

    public void SetEnterStageEvent(UserData userData, System.Action sceneChange)
    {
        foreach (var ui in stageUI)
        {
            ui.Show();
            ui.SetClickEvent(async (stageIdx) =>
            {
                try
                {
                    string json = await GameNetworkManager.Instance.UpdateEnterUserStageAsync(StageMapIdx, (int)stageIdx, this.GetCancellationTokenOnDestroy());
                    APIResponseData<EnterUserStageData> res = GameNetworkManager.CreateAPIResponseDataFromJson<EnterUserStageData>(json);

                    if (res.data != null)
                    {
                        userData.MapIdx = res.data.MapIdx;
                        userData.StageIdx = res.data.StageIdx;

                        await SaveLoadManager.Instance.SaveUserDataAsync(this.GetCancellationTokenOnDestroy());
                        sceneChange?.Invoke();
                    }
                }
                catch(System.Exception ex)
                { 
                    APIResponseData<DumpData> dump = GameNetworkManager.CreateAPIResponseDataFromJson<DumpData>(ex.Message);

                    AlterMsgSystem.Instance.ShowMsg($"response : {dump.response}");
                }
            });
        }
    }

    private void Start()
    {
        data = DataTableManager.Instance.GetMapData((uint)StageMapIdx);

        if(data.Idx == 0)
        {
            Debug.LogError($"not found stageMap, {StageMapIdx}");
            return;
        }
    }
}