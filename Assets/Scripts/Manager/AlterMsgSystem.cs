using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Commons;

public class AlterMsgSystem : Singleton<AlterMsgSystem>
{
    [SerializeField]
    private Canvas canvas = null;

    public void ShowMsg(string msg)
    {
        RectTransform rt = (RectTransform)canvas.transform;
        AlterMsg alter = Factory.Instance.GetAlterMsg();

        if(alter != null)
        {
            alter.SetText(msg);
            alter.SetParent(rt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            alter.PlayFade(() => Factory.Instance.ReleaseAlterMsg(alter));
        }
    }
}