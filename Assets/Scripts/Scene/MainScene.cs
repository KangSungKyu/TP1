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
    private Image fadeUI = null;

    [SerializeField]
    private GameContent[] gameContents = null;

    private UserData userData = null;
    private StageClearData stageClearData = null;
    private GameContent currentGameContent = null;

    private void Awake()
    {
        fadeUI.enabled = true;
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
        userData = SaveLoadManager.Instance.UserData;
        stageClearData = SaveLoadManager.Instance.StageClearData;

        StageData sd = DataTableManager.Instance.GetStageData((uint)userData.StageIdx);

        if(currentGameContent != null)
        {
            await currentGameContent.Exit();
        }

        currentGameContent = gameContents[(int)sd.Type];
        
        await currentGameContent?.Enter();

        fadeUI.DOFade(0.0f, 1.0f)
            .OnComplete(() => { fadeUI.enabled = false; })
            .Play();
    }

}
