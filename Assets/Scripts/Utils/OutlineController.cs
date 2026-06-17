using UnityEngine;

public class OutlineController : MonoBehaviour
{
    private SpriteRenderer sr;
    private MaterialPropertyBlock propBlock;
    private static readonly int OutlineEnabledID = Shader.PropertyToID("_OutlineEnabled");

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    public void SetOutline(bool active)
    {
        sr.GetPropertyBlock(propBlock);
        propBlock.SetFloat(OutlineEnabledID, active ? 1.0f : 0.0f);
        sr.SetPropertyBlock(propBlock);
    }
}