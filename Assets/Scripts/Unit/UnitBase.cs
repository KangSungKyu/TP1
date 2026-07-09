using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Commons;

public abstract class UnitBase : MonoBehaviour
{
    public UnitData Info => info;
    public UsageUnitData UsageUnitData => usageUnitData;
    public ApplyStatusData ApplyStatusData => applyStatusData;
    public bool IsShield => isShield;
    public int Hp => (int)usageUnitData.Hp.Value;
    public int MaxHp => (int)usageUnitData.MaxHp.Value;
    public int Atk => (int)usageUnitData.Atk;
    public int Def => (int)usageUnitData.Def;
    public int Dodge => (int)usageUnitData.Dodge;
    public int Spd => (int)usageUnitData.Spd;
    public float HpRatio => usageUnitData.Hp.Value / usageUnitData.MaxHp.Value;
    public float ATBRatio => currentATB.Value / maxATB.Value;
    public float ShieldCrushTime => usageUnitData.ShieldCrushTime;
    public string UnitName => unitName;
    public HpBar HpUI => hpUI;
    public Subject<UnitBase> OnATBReady => atbReadySubject;
    public Image PortraitUI => portrait;
    public UnitBase TargetUnit => targetUnit;

    [SerializeField]
    protected SpriteRenderer spriteRenderer = null;
    [SerializeField]
    protected Animator animator = null;
    [SerializeField]
    protected OutlineController olctrl = null;

    protected bool isShield = false;
    protected UnitActionType actionType = UnitActionType.Idle;
    protected ReactiveProperty<float> currentATB = new ReactiveProperty<float>(0.0f);
    protected ReactiveProperty<float> maxATB = new ReactiveProperty<float>(0.0f);
    protected UnitData info = default;
    protected ApplyStatusData applyStatusData = new ApplyStatusData();
    protected UsageUnitData usageUnitData = new UsageUnitData();
    protected string unitName = string.Empty;
    protected UnitBase targetUnit = null;
    protected HpBar hpUI = null;
    protected Image portrait = null;
    protected Subject<UnitBase> atbReadySubject = new Subject<UnitBase>();
    protected IDisposable dspShield = null;
    protected List<QuadraticBezierRenderer> targetLineList = new List<QuadraticBezierRenderer>();

    private TweenerCore<Color, Color, ColorOptions> fadeDo;

    public abstract void LoadFromSO(uint idx);
    public abstract void ApplyStatus(ApplyStatusData applyData);

    public void InitUnitData()
    {
        SetShield(false);

        usageUnitData.Hp = new ReactiveProperty<float>();
        usageUnitData.MaxHp = new ReactiveProperty<float>();
        usageUnitData.ShieldCrushedTime = new ReactiveProperty<float>();
        usageUnitData.ShieldCrushTime = 0.0f;

        currentATB.Value = 0.0f;
        maxATB.Value = 100.0f;

        ClearApplyStatus();

        usageUnitData.Hp.Value = usageUnitData.MaxHp.Value;
    }

    public void ClearApplyStatus()
    {
        applyStatusData.Clear();

        usageUnitData.MaxHp.Value = info.MaxHp;
        usageUnitData.Atk = info.Atk;
        usageUnitData.Def = info.Def;
        usageUnitData.Dodge = info.Dodge;
        usageUnitData.Spd = info.Spd;

        ClearApplyStatus_ShieldCrushTime();
    }

    public void ClearApplyStatus_ShieldCrushTime()
    {
        usageUnitData.ShieldCrushTime = 0.0f;
    }


    public void SetTarget(UnitBase target)
    {
        targetUnit = target;
    }

    public void SetShield(bool onoff)
    {
        isShield = onoff;

        olctrl.SetOutline(isShield);
        Debug.Log($"SetShield: {name} -> {(isShield ? "ON" : "OFF")}");
    }

    public void SetHPUI(HpBar hpUI)
    {
        if (hpUI != null)
        {
            this.hpUI = hpUI;

            UpdateHpBarPosition();
            Subscribe_HP(hp => { hpUI.SetRatio(HpRatio); hpUI.SetText($"{Hp} / {MaxHp}"); });
            Subscribe_MaxHP(maxHp => { hpUI.SetRatio(HpRatio); hpUI.SetText($"{Hp} / {MaxHp}"); });
        }
    }

    public void SetPortraitUI(Image port)
    {
        portrait = port;
        portrait.sprite = ResourceManager.Instance.GetResource<Sprite>($"{Commons.ResKey_PortraitUIs}[{Commons.ResKey_PortraitUIs}_{info.PortraitIdx}]");
    }

    public virtual void ApplyDamage(float damage)
    {
        usageUnitData.Hp.Value = Mathf.Max(0, usageUnitData.Hp.Value - damage);
    }

    public void Subscribe_HP(System.Action<float> act)
    {
        usageUnitData.Hp.Subscribe(act).AddTo(this);
    }

    public void Subscribe_MaxHP(System.Action<float> act)
    {
        usageUnitData.MaxHp.Subscribe(act).AddTo(this);
    }

    public void Subscribe_ATBGauge(System.Action<float> act)
    {
        currentATB.Subscribe(act).AddTo(this);
    }

    public void ShieldCrush(float shieldCrushTime)
    {
        // immediate disable shield and publish crushed time via reactive property
        SetShield(false);

        if (usageUnitData.ShieldCrushedTime == null)
            usageUnitData.ShieldCrushedTime = new ReactiveProperty<float>(shieldCrushTime);
        else
            usageUnitData.ShieldCrushedTime.Value = shieldCrushTime;
    }

    public virtual void Release()
    {
        for(int i = 0; i < targetLineList.Count; ++i)
        {
            Factory.Instance.ReleaseTargetLine(targetLineList[i]);
        }

        targetLineList.Clear();

        fadeDo?.Kill();
        fadeDo = null;

        ClearApplyStatus();
        ClearApplyStatus_ShieldCrushTime();

        atbReadySubject?.Dispose();
        atbReadySubject = null;
    }

    public void FadeOut(float duration)
    {
        if(spriteRenderer != null)
        {
            fadeDo = spriteRenderer.DOFade(0.0f, duration);

            fadeDo.Play();
        }
    }

    public void FadeIn(float duration)
    {
        if (spriteRenderer != null)
        {
            fadeDo = spriteRenderer.DOFade(1.0f, duration);

            fadeDo.Play();
        }
    }

    public IEnumerator IEPlayAction(UnitActionData actionData)
    {
        IEnumerator proc = actionData.Type switch 
        { 
            UnitActionType.Move => IELerpMove(transform, TargetUnit != null ? TargetUnit.transform : null),
            _ => null
        };

        return IEPlayAction(actionData, proc);
    }

    public void AddATBTick(float dt)
    {
        if (maxATB == null || maxATB.Value <= 0f)
            return;

        float spd = Mathf.Max(1.0f, Spd);

        if (spd <= 0f)
            spd = 1f;

        currentATB.Value += spd * dt;

        while (currentATB.Value >= maxATB.Value)
        {
            currentATB.Value -= maxATB.Value;

            atbReadySubject?.OnNext(this);
        }
    }

    public void ResetATB()
    {
        currentATB.Value = 0.0f;
    }

    public Bounds GetUnitBounds()
    {
        return spriteRenderer.bounds;
    }

    //todo : 최대한 근사값을 찾아야함
    public Vector3 GetUnitHeadPosition()
    {
        float size = transform.localScale.magnitude;
        Vector3 pos = transform.position + Vector3.up * size;

        return pos;
    }

    public void DelLastTargetLine()
    {
        //todo : 이왕이면 생성될때와 지워질때 짝 맞추기
        if(targetLineList != null && targetLineList.Count > 0)
        {
            Factory.Instance.ReleaseTargetLine(targetLineList.Last());
        }
    }

    public void DelTargetLine(QuadraticBezierRenderer targetLine)
    {
        Factory.Instance.ReleaseTargetLine(targetLine);
    }

    public abstract void DrawTargetLine();

    protected abstract void Init();

    protected virtual void Awake()
    {
        actionType = UnitActionType.Idle;

        if (spriteRenderer == null)
        {
            spriteRenderer = transform.Find("Renderer/Sprite").GetComponent<SpriteRenderer>();
        }
        
        if(animator == null)
        {
            animator = transform.Find("Renderer/Sprite").GetComponent<Animator>();
        }

        if(olctrl == null)
        {
            spriteRenderer.GetComponent<OutlineController>();
        }

        if(spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    protected void UpdateHpBarPosition()
    {
        Vector3 worldPos = GetUnitHeadPosition();
        Vector2 uiPos = Util.WorldToCanvasPosition(hpUI.transform.parent.GetComponent<Canvas>(), Camera.main, worldPos);
        RectTransform uiRT = (RectTransform)hpUI.transform;

        uiRT.anchoredPosition = uiPos;
    }

    private IEnumerator IEPlayAction(UnitActionData actionData, IEnumerator procCustomFunc = null)
    {
        actionData.BeforeAction?.Invoke();

        string anim = actionData.Type.ToString(); // Enum 이름을 트리거로 사용
        bool isLoopAnimType = actionData.Type == UnitActionType.Idle || actionData.Type == UnitActionType.Move;

        if (isLoopAnimType)
        {
            animator.SetBool(anim, true);
        }
        else
        {
            animator.SetTrigger(anim);

            yield return null;

            var clipList = animator.GetCurrentAnimatorClipInfo(0);

            float duration = (clipList != null && clipList.Length > 0 ? clipList[0].clip.length : 0f);

            if(actionData.Type == UnitActionType.Death)
            {
                duration += 1.0f;

                FadeOut(duration);
            }

            yield return new WaitForSeconds(duration);
        }

        if (procCustomFunc != null)
        {
            yield return StartCoroutine(procCustomFunc);
        }

        if (procCustomFunc != null && isLoopAnimType)
        {
            animator.SetBool(anim, false);
        }

        actionData.AfterAction?.Invoke();
    }

    private IEnumerator IELerpMove(Transform unitTransform, Transform targetTransform)
    {
        if (unitTransform == null || targetTransform == null)
        {
            yield break;
        }

        while (Vector2.Distance((Vector2)unitTransform.position, (Vector2)targetTransform.position) > 1.5f)
        {
            yield return null;

            Vector2 dir = (targetTransform.position - unitTransform.position).normalized;

            unitTransform.position += (Vector3)dir * Spd * Time.deltaTime;

            UpdateHpBarPosition();
        }

        yield return null;
    }

}
