using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class RestContent : GameContent
{
    [SerializeField]
    private TextMeshProUGUI stageUI = null;
    [SerializeField]
    private AssetReference selectStageScene = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;

    public override async void Enter()
    {
        base.Enter();

        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = SODataTable.Instance.GetStageData((uint)userData.StageIdx);

        stageUI.SetText($"{sd.Stage} - {sd.SubStage}");

        //test
    }

}