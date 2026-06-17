using System.Collections;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererFillAmount : MonoBehaviour
{
    public bool Horizontal => bHorizontal.Value;
    public float FillAmount => fillAmount.Value;

    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int HorizontalID = Shader.PropertyToID("_Horizontal");
    private static readonly bool DefaultHorizontal = true;
    private static readonly float DefaultFillAmount = 1.0f;

    private SpriteRenderer spriteRenderer = null;
    private MaterialPropertyBlock mpb = null;

    private ReactiveProperty<bool> bHorizontal = new ReactiveProperty<bool>(DefaultHorizontal);
    private ReactiveProperty<float> fillAmount = new ReactiveProperty<float>(DefaultFillAmount);

    public void SetHorizontal(bool horizontal)
    {
        bHorizontal.Value = horizontal;
    }

    public void SetFillAmount(float amount)
    {
        fillAmount.Value = Mathf.Clamp01(amount);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();

        bHorizontal.Subscribe((b) =>
        {
            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(HorizontalID, b ? 1.0f : 0.0f);
            spriteRenderer.SetPropertyBlock(mpb);
        }).AddTo(this);

        fillAmount.Subscribe((v) =>
        {
            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(FillAmountID, v);
            spriteRenderer.SetPropertyBlock(mpb);
        }).AddTo(this);
    }
}