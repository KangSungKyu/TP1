using System.Collections;
using UnityEngine;

public class BTile : MonoBehaviour
{
    public BTileType type = BTileType.Empty;
    public BTileAttribute attr = BTileAttribute.None;
    public int x = -1;
    public int y = -1;
    public uint skillIdx = 0;

    public BTile(int x, int y, BTileType type = BTileType.Empty)
    {
        this.type = type;
        this.attr = BTileAttribute.None;
        this.x = x;
        this.y = y;
        this.skillIdx = 0;
    }
}
