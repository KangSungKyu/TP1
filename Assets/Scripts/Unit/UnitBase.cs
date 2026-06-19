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
    public Subject<Unit> OnATBReady => atbReadySubject;
    public Image PortraitUI => portrait;

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
    protected UnitBase TargetUnit = null;
    protected HpBar hpUI = null;
    protected Image portrait = null;

    protected Queue<IEnumerator> actionQueue = new Queue<IEnumerator>();

    protected Subject<Unit> atbReadySubject = new Subject<Unit>();

    protected IDisposable atbTick = null;
    protected IDisposable dspShield = null;
    protected Coroutine procAction = null;


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
        // Start ATB ticking
        StartATBTick();
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
        TargetUnit = target;
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
        portrait.sprite = ResourceManager.Instance.GetResource<Sprite>($"Portraits[Portraits_{info.PortraitIdx}]");
    }

    public void PlayAction(UnitActionData defaultAction, params UnitActionData[] actionDatas)
    {
        foreach (var actionData in actionDatas)
        {
            DoAction(actionData);
        }

        if (defaultAction.Type != UnitActionType.None)
        {
            DoAction(defaultAction);
        }

        if (procAction == null)
        {
            procAction = StartCoroutine(IEPlayActionList());
        }
    }

    public void StopAction()
    {
        if(procAction != null)
        {
            while(actionQueue.Count > 0)
            {
                if(actionQueue.Peek() != null)
                {
                    StopCoroutine(actionQueue.Dequeue());
                }
            }

            actionQueue.Clear();

            StopCoroutine(procAction);
        }
    }

    public void ForceClearAction()
    {
        StopAllCoroutines();

        PlayAction(UnitActionData.DefaultAction_Idle);
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
        atbReadySubject?.Dispose();
        atbReadySubject = null;

        atbTick?.Dispose();
        atbTick = null;

        StopAction();
    }

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

        actionQueue.Clear();

        DoAction(new UnitActionData(UnitActionType.Idle));
    }

    protected void UpdateHpBarPosition()
    {
        Vector3 worldPos = transform.position + new Vector3(0, 2.0f, 0.0f);
        Vector2 uiPos = Util.WorldToCanvasPosition(hpUI.transform.parent.GetComponent<Canvas>(), Camera.main, worldPos);
        RectTransform uiRT = (RectTransform)hpUI.transform;

        uiRT.anchoredPosition = uiPos;
    }

    protected void DoAction(UnitActionData actionData)
    {
        this.actionType = actionData.Type;

        if (actionData.BeforeAction != null)
        {
            actionQueue.Enqueue(IESingleRoutine(actionData.BeforeAction));
        }

        switch (actionType)
        {
            case UnitActionType.Idle:
                Idle(actionData);
                break;
            case UnitActionType.Move:
                Run(actionData);
                break;
            case UnitActionType.Attack:
                Attack(actionData);
                break;
            case UnitActionType.Guard:
                Guard(actionData);
                break;
            case UnitActionType.Dodge:
                Dodged(actionData);
                break;
            case UnitActionType.KnockBack:
                KnockBack(actionData);
                break;
            case UnitActionType.Death:
                Death(actionData);
                break;
        }

        if (actionData.AfterAction != null)
        {
            actionQueue.Enqueue(IESingleRoutine(actionData.AfterAction));
        }
    }

    protected virtual void Idle(UnitActionData actionData)
    {
        animator.SetBool("Standing", true);
    }

    protected virtual void Run(UnitActionData actionData)
    {
        if(TargetUnit == null)
        {
            return;
        }

        actionQueue.Enqueue(IELerpMove(transform, TargetUnit.transform));
    }

    protected virtual void Attack(UnitActionData actionData)
    {
        actionQueue.Enqueue(IEAttack());
    }

    protected virtual void Guard(UnitActionData actionData)
    {
    }

    protected virtual void Dodged(UnitActionData actionData)
    {

    }

    protected virtual void KnockBack(UnitActionData actionData)
    {
        actionQueue.Enqueue(IEKnockBack());
    }

    protected virtual void Death(UnitActionData actionData)
    {
        actionQueue.Enqueue(IEDeath());
    }

    private void StartATBTick()
    {
        // Subscribe to EveryUpdate to increment ATB based on usageUnitData.Spd
        atbTick = Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                // If MaxATB is zero or negative, skip
                if (maxATB == null || maxATB.Value <= 0f)
                    return;

                float spd = Mathf.Max(1.0f, Spd);

                // ensure some minimal speed
                if (spd <= 0f)
                    spd = 1f;

                currentATB.Value += spd * Time.deltaTime;

                if (currentATB.Value >= maxATB.Value)
                {
                    // consume full gauge
                    currentATB.Value -= maxATB.Value;

                    // notify subscribers
                    atbReadySubject?.OnNext(Unit.Default);
                }
            })
            .AddTo(this);
    }

    private IEnumerator IEPlayActionList()
    {
        while (actionQueue.Count > 0)
        {
            Debug.Log($"play action, {name}, {actionQueue.Peek()}");
            yield return StartCoroutine(actionQueue.Dequeue());
        }

        procAction = null;
    }

    private IEnumerator IESingleRoutine(System.Action act)
    {
        act?.Invoke();

        yield return null;
    }

    private IEnumerator IELerpMove(Transform unitTransform, Transform targetTransform)
    {
        if (unitTransform == null || targetTransform == null)
        {
            yield break;
        }

        animator.SetBool("Standing", false);
        animator.SetFloat("MoveSpeed", Spd);

        while (Vector2.Distance((Vector2)unitTransform.position, (Vector2)targetTransform.position) > 1.5f)
        {
            yield return null;

            Vector2 dir = (targetTransform.position - unitTransform.position).normalized;

            unitTransform.position += (Vector3)dir * Spd * Time.deltaTime;

            UpdateHpBarPosition();
        }

        animator.SetFloat("MoveSpeed", 0.0f);

        yield return null;
    }

    private IEnumerator IEAttack()
    {
        animator.SetBool("Attack", true);

        float duration = animator.GetCurrentAnimatorClipInfo(0).Length;
        float calcDuration = duration * 1.0f;

        yield return new WaitForSeconds(calcDuration);

        animator.SetBool("Attack", false);
    }

    private IEnumerator IEKnockBack()
    {
        if (TargetUnit == null)
            yield break;

        float time = 0.0f;
        float duration = 0.5f;
        Vector3 dir = (transform.position - TargetUnit.transform.position).normalized;

        while(time < duration)
        {
            time += Time.deltaTime;
            yield return null;

            transform.position += dir * 5.0f * Time.deltaTime;

            UpdateHpBarPosition();
        }
    }

    private IEnumerator IEDeath()
    {
        float delay = 0.15f;

        animator.SetBool("Death", true);

        yield return new WaitForSeconds(animator.GetCurrentAnimatorClipInfo(0).Length + delay);
    }
}
