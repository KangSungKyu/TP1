using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFadeUI : MonoBehaviour
{
    [SerializeField]
    private Image fadeUI = null;

    public void Init()
    {
        fadeUI.enabled = true;
    }

    public void Play()
    {
        fadeUI.DOFade(0.0f, 1.0f)
            .OnComplete(() => { fadeUI.enabled = false; })
            .Play();
    }
}