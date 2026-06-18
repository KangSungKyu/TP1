using DG.Tweening;
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

        StageData sd = SODataTable.Instance.GetStageData((uint)userData.StageIdx);

        currentGameContent?.Exit();
        currentGameContent = gameContents[(int)sd.Type];
        currentGameContent?.Enter();

        fadeUI.enabled = true;
        fadeUI.DOFade(0.0f, 1.0f).OnComplete(() => { fadeUI.enabled = false; }).Play();
    }

}
