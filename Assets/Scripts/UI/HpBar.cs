using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class HpBar: MonoBehaviour
{
    [SerializeField]
    private Image hpBarImage = null;
    [SerializeField]
    private TextMeshProUGUI tmp = null;

    public void SetRatio(float ratio)
    {
        hpBarImage.fillAmount = ratio;
    }

    public void SetText(string text)
    {
        tmp.text = text;
    }
}