using System.Collections;
using TMPro;
using UnityEngine;

public class SimpleTextPanel : PanelBase
{
    [SerializeField]
    private TextMeshProUGUI simpleText = null;

    private string text = string.Empty;

    public void Show(string text)
    {
        this.text = text;

        Show();
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