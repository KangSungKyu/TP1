using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DamageFont : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro tmp = null;

    public void SetText(string text, System.Action onComplete)
    {
        float jumpDuration = 1.0f;
        Vector3 fontJump = transform.position + new Vector3(0.0f, 0.25f, 0.0f);
        Sequence seq = DOTween.Sequence();

        tmp.alpha = 1.0f;

        transform.DOKill();
        tmp.SetText(text);

        seq.Join(tmp.DOFade(0.0f, jumpDuration));
        seq.Join(transform.DOJump(fontJump, 0.5f, 1, jumpDuration));
        seq.OnComplete(() => onComplete?.Invoke()).Play();
    }

    public void Reset()
    {
    }
}