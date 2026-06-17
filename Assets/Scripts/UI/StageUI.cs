using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniRx;

public class StageUI : MonoBehaviour
{
    [SerializeField]
    private int StageIdx = 0;
    [SerializeField]
    private int[] ConnectStageIdx = null;
    [SerializeField]
    private TextMeshProUGUI stageText = null;
    [SerializeField]
    private TextMeshProUGUI stateText = null;

    private StageData data = default;

    public void SetClickEvent(System.Action<uint> onClick)
    {
        GetComponent<Button>()?.onClick.AddListener(() => onClick?.Invoke((uint)StageIdx));
    }

    private void Start()
    {
        data = SODataTable.Instance.GetStageData((uint)StageIdx);

        if(data.Idx == 0)
        {
            Debug.LogError($"not found stage, {StageIdx}");
            return;
        }

        stageText.SetText($"{data.Stage} - {data.SubStage}");
        stateText.SetText($"state : {SaveLoadManager.Instance.StageClearData.GetState((int)data.Idx)}");

        GetComponent<Button>()?.onClick.RemoveAllListeners();
    }
}