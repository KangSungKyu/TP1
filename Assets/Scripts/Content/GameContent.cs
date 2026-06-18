using System.Collections;
using UnityEngine;

public abstract class GameContent : MonoBehaviour
{
    [SerializeField]
    protected GameObject contentRoot = null;

    public virtual async void Enter()
    {
        contentRoot?.SetActive(true);
    }

    public virtual async void Exit()
    {
        contentRoot?.SetActive(false);
    }
}