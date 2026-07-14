using System.Collections;
using UnityEngine;

public class BTile : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer icon = null;
    [SerializeField]
    private SpriteRenderer border = null;

    public BTileType type = BTileType.Empty;
    public BTileAttribute attr = BTileAttribute.None;
    public int x = -1;
    public int y = -1;
    public uint skillIdx = 0;

    public void SetTile(Sprite spr)
    {
        if(spr == null)
        {
            icon.gameObject.SetActive(false);
        }
        else
        {
            icon.gameObject.SetActive(true);

            icon.sprite = spr;
        }
    }

    public void SetColor(Color color)
    {
        border.color = color;
    }
}
