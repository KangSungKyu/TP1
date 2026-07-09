using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StageResultPanel : PanelBase
{
    [SerializeField]
    private Image bgImg = null;
    [SerializeField]
    private Image resultImg = null;
    [SerializeField]
    private AnimationCurve resultImgCurve = null;

    private bool isWin = false;
    private float startY = 0f;
    private float endY = 0f;

    public void Show(bool isWin, System.Action act)
    {
        this.isWin = isWin;

        SetOnExitEvent(() =>
        {
            act?.Invoke();
            //Hide();
        });
        Show();

        canvasGroup.interactable = false;

        float duration = 1.0f;
        Sequence seq = DOTween.Sequence();
        var moveTw = DOTween.To(() => 0f, x =>
        {
            float t = resultImgCurve.Evaluate(x);
            resultImg.rectTransform.anchoredPosition = new Vector2(0.0f, Mathf.LerpUnclamped(startY, endY, t));
        }, 1f, duration);

        seq.Insert(0, resultImg.DOFade(1.0f, duration));
        seq.Insert(0, moveTw);
        seq.OnComplete(() => canvasGroup.interactable = true).Play();
    }

    protected override bool OnPanelShow()
    {
        bool res = false;

        Sprite imgFont = ResourceManager.Instance.GetResource<Sprite>(isWin ? Commons.ResKey_WinText : Commons.ResKey_DefeatText);
        int screenHeight = Screen.currentResolution.height;

        bgImg.sprite = ResourceManager.Instance.GetResource<Sprite>(isWin ? Commons.ResKey_WinBG : Commons.ResKey_DefeatBG);
        resultImg.sprite = imgFont;
        resultImg.rectTransform.sizeDelta = imgFont.textureRect.size * 2.0f;

        float height = resultImg.rectTransform.sizeDelta.y;

        startY = (screenHeight + height) * 0.5f;
        resultImg.rectTransform.anchoredPosition = new Vector2(0, startY);

        res = true;

        return res;
    }

    protected override bool OnPanelHide()
    {
        bool res = false;

        res = true;

        return res;
    }
}