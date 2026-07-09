using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public abstract class PanelBase : MonoBehaviour
{
    [SerializeField]
    protected bool isRegisterByName = true;
    [SerializeField]
    protected string registedName = string.Empty;
    [SerializeField]
    protected CanvasGroup canvasGroup = null;
    [SerializeField]
    protected Button exitBtn = null;

    protected bool isShow = false;
    protected Coroutine coPanel = null;

    public void Show()
    {
        OnPanel();
    }

    public void Hide()
    {
        OffPanel();
    }

    public void ForceHide()
    {
        StopCoroutine();

        if (OnPanelHide())
        {
            this.gameObject.SetActive(false);
        }
    }

    public void SetOnExitEvent(System.Action act)
    {
        if(exitBtn != null)
        {
            exitBtn.onClick.RemoveAllListeners();
            exitBtn.onClick.AddListener(() =>
            {
                act?.Invoke();
            });
        }
    }

    protected abstract bool OnPanelShow();
    protected abstract bool OnPanelHide();

    private void Start()
    {
        if(isRegisterByName)
        {
            PanelManager.RegisterPanel(registedName, this);
        }
        else
        {
            registedName = this.name;

            PanelManager.RegisterPanel(this);
        }

        SetOnExitEvent(Hide);

        if(canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        StopCoroutine();
        ForceHide();
    }

    private void OnDestroy()
    {
        if(isRegisterByName)
        {
            PanelManager.UnregisterPanel(registedName);
        }
        else
        {
            PanelManager.UnregisterPanel(this);
        }
    }

    private void StopCoroutine()
    {
        if (coPanel != null)
        {
            StopCoroutine(coPanel);

            coPanel = null;
        }
    }

    private void OnPanel()
    {
        if(coPanel == null)
        {
            this.gameObject.SetActive(true);

            coPanel = StartCoroutine(IEOnPanel());
        }
    }

    private void OffPanel()
    {
        if(coPanel == null)
        {
            coPanel = StartCoroutine(IEOffPanel());
        }
    }

    private void OnOffCanvasGroup(bool onoff)
    {
        canvasGroup.alpha = onoff ? 1f : 0f;
        canvasGroup.blocksRaycasts = onoff;
        canvasGroup.interactable = onoff;
    }

    private IEnumerator IEOnPanel()
    {
        OnOffCanvasGroup(false);
        
        yield return new WaitUntil(() => OnPanelShow());
        yield return null;
        
        OnOffCanvasGroup(true);

        coPanel = null;
    }

    private IEnumerator IEOffPanel()
    {
        yield return new WaitUntil(() => OnPanelHide());

        OnOffCanvasGroup(false);

        coPanel = null;

        Invoke("DeactivePanel", 0.5f);
    }

    private void DeactivePanel()
    {
        this.gameObject.SetActive(false);
    }
}