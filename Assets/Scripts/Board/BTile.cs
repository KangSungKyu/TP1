using System.Collections;
using UnityEngine;

public class BTile : MonoBehaviour
{
    public BTileAttribute attr = BTileAttribute.None;
    public int x = -1;
    public int y = -1;
    public BTileType type = BTileType.Empty;

    public BTile(int x, int y, BTileType type = BTileType.Empty)
    {
        this.attr = BTileAttribute.None;
        this.x = x;
        this.y = y;
        this.type = type;
    }

    public void SetAttribute(BTileAttribute attr)
    {
        this.attr = attr;
    }
}
