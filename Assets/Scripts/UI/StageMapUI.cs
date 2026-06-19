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
            ui.SetClickEvent((stageIdx) =>
            {
                userData.MapIdx = StageMapIdx;
                userData.StageIdx = (int)stageIdx;

                SaveLoadManager.Instance.SaveUserData((t) => { Debug.Log("save"); }, () => { Debug.Log("save fail"); });
                sceneChange?.Invoke();
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