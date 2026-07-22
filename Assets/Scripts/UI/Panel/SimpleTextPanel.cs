using System.Collections;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class SimpleTextPanel : PanelBase
{
    [SerializeField]
    private TextMeshProUGUI simpleText = null;

    private string text = string.Empty;

    public void Show(string text)
    {
        this.text = text;
        // Use async fire‑and‑forget version of Show from PanelBase
        ShowAsync().Forget();
    }

    protected override bool OnPanelShow()
    {
        bool res = false;

        simpleText.text = text;
        res = true;

        return res;
    }

    protected override bool OnPanelHide()
    {
        bool res = false;

        simpleText.text = string.Empty;
        res = true;

        return res;
    }
}