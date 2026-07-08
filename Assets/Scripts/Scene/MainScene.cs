using DG.Tweening;
using JetBrains.Annotations;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class MainScene : MonoBehaviour
{
    [SerializeField]
    private SceneFadeUI fadeUI = null;

    [SerializeField]
    private GameContent[] gameContents = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;
    private GameContent currentGameContent = null;

    private void Awake()
    {
        fadeUI.Init();
    }

    private void Start()
    {
        for(int i = 0; i < gameContents.Length; ++i)
        {
            gameContents[i]?.Exit();
        }   

        Init();
    }

    private async void Init()
    {
        await Factory.Instance.Init_SystemResAsync();

        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = DataTableManager.Instance.GetStageData((uint)userData.StageIdx);

        if(currentGameContent != null)
        {
            await currentGameContent.Exit();
        }

        currentGameContent = gameContents[(int)sd.Type];
        
        await currentGameContent?.Enter();

        fadeUI.Play();
    }

}
