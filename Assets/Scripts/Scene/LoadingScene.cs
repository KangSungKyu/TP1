using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    [SerializeField]
    private Image progressBar = null;

    public static LoadingScene Instance { get; private set; }

    private void Awake() => Instance = this;

    public void UpdateProgress(float value)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = value;
        }
    }
}