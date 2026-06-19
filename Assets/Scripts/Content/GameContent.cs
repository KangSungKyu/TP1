using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public abstract class GameContent : MonoBehaviour
{
    [SerializeField]
    protected GameObject contentRoot = null;

    public virtual async Task Enter()
    {
        contentRoot?.SetActive(true);

        await Task.CompletedTask;
    }

    public virtual async Task Exit()
    {
        contentRoot?.SetActive(false);

        await Task.CompletedTask;
    }
}