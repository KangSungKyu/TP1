using System;
using System.Collections;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem parSys = null;

    private Action onComplete = null;

    public void PlayEffect(Vector2 pos, System.Action onComplete = null)
    {
        transform.position = pos + UnityEngine.Random.insideUnitCircle * 0.25f;
        this.onComplete = onComplete;

        if (parSys != null)
        {
            parSys.Play();
        }
    }

    private void Start()
    {
        if (parSys != null)
        {
            ParticleSystem.MainModule main = parSys.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    private void OnParticleSystemStopped()
    {
        onComplete?.Invoke();
    }
}