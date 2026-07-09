using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniRx;

public class StageUI : MonoBehaviour
{
    [SerializeField]
    private int stageIdx = 0;
    [SerializeField]
    private int[] connectStageIdx = null;
    [SerializeField]
    private Image stageBG = null;
    [SerializeField]
    private TextMeshProUGUI stageText = null;

    private StageData data = default;

    public void SetClickEvent(System.Action<uint> onClick)
    {
        GetComponent<Button>()?.onClick.AddListener(() => onClick?.Invoke((uint)stageIdx));
    }

    public void Show()
    {
        data = DataTableManager.Instance.GetStageData((uint)stageIdx);

        if (data.Idx == 0)
        {
            Debug.LogError($"not found stage, {stageIdx}");
            return;
        }

        int stageState = SaveLoadManager.Instance.StageClearData.GetState((int)data.Idx);// 0none, 1clear, 2rewarded
        int resIdx = 0; //0clear, 1avaliable, 2lock
        HorizontalAlignmentOptions haopt = HorizontalAlignmentOptions.Center;

        if (stageState <= 0)
        {
            StageData prevData = DataTableManager.Instance.GetStageData((uint)(stageIdx - 1));

            if (prevData != null && SaveLoadManager.Instance.StageClearData.GetState((int)prevData.Idx) <= 0)
            {
                resIdx = 2;
            }
            else
            {
                resIdx = 1;
            }
        }
        else
        {
            resIdx = 0;
            haopt = HorizontalAlignmentOptions.Right;
        }

        string resKey = $"{Commons.ResKey_StageUIs}[{Commons.ResKey_StageUIs}_{resIdx}]";

        stageText.SetText($"{data.Stage} - {data.SubStage}");
        stageText.horizontalAlignment = haopt;

        stageBG.sprite = ResourceManager.Instance.GetResource<Sprite>(resKey);

        GetComponent<Button>()?.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        Show();
    }
}