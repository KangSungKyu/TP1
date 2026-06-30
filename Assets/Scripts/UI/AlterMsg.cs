using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlterMsg : MonoBehaviour
{
    [SerializeField]
    private Image msgImg = null;
    [SerializeField]
    private TextMeshProUGUI msgText = null;

    public void SetText(string message)
    {
        msgText.text = message;
    }

    public void SetParent(RectTransform parent)
    {
        transform.SetParent(parent, false);
    }

    public void PlayFade(Action onComp)
    {
        Sequence seq = DOTween.Sequence();
        RectTransform rt = (RectTransform)transform;
        float duration = 2.0f;
    
        seq.Insert(0, msgImg.DOFade(0, duration));
        seq.Insert(0, msgText.DOFade(0, duration));
        seq.Insert(0, rt.DOAnchorPosY(50, duration));
        seq.OnComplete(()=>onComp?.Invoke());
        seq.Play();
    }
}
