using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UGUI 기반 윈도우 기본 클래스 - 콘텐츠 작업 시 직접 사용하지 않고 하위 클래스를 사용한다.
/// (기존 NGUI UIPanel → Canvas, UIEventListener → UGUI_EventListener 로 대체)
/// </summary>
public class UGUI_WindowUIFormBase : MonoBehaviour
{//UGUI_WindowUIFormBase

    public event Action<UGUI_WindowUIFormBase> OnClosed;

    /// <summary>
    /// UGUI 정렬/깊이 제어는 Canvas의 sortingOrder, sortingLayerName 사용
    /// </summary>
    protected Canvas _canvas = null;

    protected UGUI_WindowBaseLockBackground _baseLockBg = null;

    protected Parameter _openParameter = null;

    /// <summary>
    /// 오픈했는지 체크
    /// </summary>
    protected bool _checkOpen = false;

    /// <summary>
    /// 활성화 상태인지 체크
    /// </summary>
    protected bool _checkActive = false;

    // ──────────────────────────────────────────────────────────────────────────
    // Properties
    // ──────────────────────────────────────────────────────────────────────────

    public virtual Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.BASE; }
    }

    public GameObject GetGameObject
    {
        get { return this.gameObject; }
    }

    public Canvas UICanvas
    {
        get { return _canvas; }
    }

    public bool CheckOpen
    {
        get { return _checkOpen; }
    }

    public bool CheckActive
    {
        get { return _checkActive; }
    }

    /// <summary>
    /// NGUI UIPanel.depth 대응 : Canvas.sortingOrder 사용
    /// </summary>
    public virtual int Depth
    {
        get { return _canvas != null ? _canvas.sortingOrder : 0; }
        set { if (_canvas != null) _canvas.sortingOrder = value; }
    }

    public virtual string SortingLayerName
    {
        get { return _canvas != null ? _canvas.sortingLayerName : string.Empty; }
        set { if (_canvas != null) _canvas.sortingLayerName = value; }
    }

    public virtual int SortingOrder
    {
        get { return _canvas != null ? _canvas.sortingOrder : 0; }
        set { if (_canvas != null) _canvas.sortingOrder = value; }
    }

    /// <summary>
    /// NGUI startingRenderQueue 대응 : UGUI에서는 실질적으로 sortingOrder가 역할을 대신함.
    /// 필요 시 Canvas의 renderMode나 GraphicRaycaster 설정과 연계한다.
    /// </summary>
    public virtual int StartingRenderQueue
    {
        get { return _canvas != null ? _canvas.sortingOrder : 0; }
        set { if (_canvas != null) _canvas.sortingOrder = value; }
    }

    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    public bool CheckInitialized
    {
        get;
        protected set;
    }

    /// <summary>
    /// 뒤로가기(ESC) 버튼으로 윈도우 닫기 허용 여부
    /// </summary>
    public bool CheckEscCloseReturn
    {
        get;
        set;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Setup
    // ──────────────────────────────────────────────────────────────────────────

    protected virtual void Reset()
    {
        SetCanvas();
    }

    private void SetCanvas()
    {
        if (_canvas == null)
            _canvas = this.gameObject.GetComponent<Canvas>();
    }

    public void SetDepth(int depth, int sortingOrder)
    {
        Depth = depth;
        SortingOrder = sortingOrder;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 초기화 함수 : UI 초기 셋팅을 여기서 처리한다.
    /// NGUI UIAnchor는 UGUI RectTransform anchor로 대체되므로 별도 처리 불필요.
    /// </summary>
    public virtual void Initialze()
    {
        SetCanvas();

        if (this.gameObject != null)
        {
            CheckEscCloseReturn = true;
            CheckInitialized = true;
        }
        else
        {
            Debug.LogError("__ !!!! Err Null Window GameObject");
        }
    }

    public virtual void Open(Parameter par = null)
    {
        if (par != null)
        {
            if (_openParameter != null)
            {
                _openParameter.Clear();
                _openParameter = null;
            }
            _openParameter = par;
        }

        _checkOpen = true;
        _checkActive = true;

        this.gameObject.SetActive(true);
    }

    public void ClickClose(GameObject go)
    {
        Close();
    }

    public virtual void Close()
    {
        if (_openParameter != null)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;
        _checkActive = false;

        this.gameObject.SetActive(false);
        OnClosed?.Invoke(this);
    }

    /// <summary>
    /// 강제로 모든 윈도우가 닫힐 때 호출 : 초기화·삭제가 필요한 데이터만 처리한다.
    /// </summary>
    public virtual void ForciblyClose()
    {
        if (_openParameter != null)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;
        _checkActive = false;

        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// ESC(뒤로가기)로 UI를 닫을 때 Close 외에 별도 처리가 필요하면 오버라이드한다.
    /// </summary>
    public virtual void EscapeClose()
    {

    }

    public virtual void Release()
    {
        if (_canvas != null)
            _canvas = null;

        if (_openParameter != null)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        if (_baseLockBg != null)
        {
            _baseLockBg.Release();
            GameObject.Destroy(_baseLockBg.gameObject);
            _baseLockBg = null;
        }

        OnClosed = null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Activate
    // ──────────────────────────────────────────────────────────────────────────

    protected virtual void ActivateOn()  { _checkActive = true; }
    protected virtual void ActivateOff() { _checkActive = false; }

    public void Activate(bool isOn)
    {
        if (isOn)
            ActivateOn();
        else
            ActivateOff();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lock Background
    // ──────────────────────────────────────────────────────────────────────────

    protected void BaseLockBgSetActive(bool state)
    {
        if (_baseLockBg != null)
            _baseLockBg.Activate(state);
    }

    protected void CreateBaseLockBg(bool checkWindow)
    {
        if (_baseLockBg == null)
        {
            GameObject obj = Instantiate(Resources.Load<GameObject>(
                string.Format("{0}{1}", Constant.WINDOW_PREFAD_PATH, UICommon.OBJ_LOCK_BACK_GROUND)));
            obj.transform.SetParent(GetGameObject.transform, false);
            obj.name = string.Format("{0}_BaseLockBg", this.gameObject.name);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;

            _baseLockBg = obj.GetComponent<UGUI_WindowBaseLockBackground>();
            _baseLockBg.Initialze(checkWindow);
        }
    }

    protected void ActiveBaseLockBg_MoveBg(bool setBaseActive, bool moveBgActive = true)
    {
        if (_baseLockBg != null)
        {
            _baseLockBg.ActiveBg(setBaseActive);
            _baseLockBg.ActiveMoveBg(moveBgActive);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Destroy
    // ──────────────────────────────────────────────────────────────────────────

    public void DestroyGameObject()
    {
        if (Application.isPlaying)
            Destroy(this.gameObject);
        else
            DestroyImmediate(this.gameObject);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Parameter Helpers
    // ──────────────────────────────────────────────────────────────────────────

    public bool OpenParContainsKey(string key)
    {
        return _openParameter != null && _openParameter.ContainsKey(key);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UGUI Button / Event Listener Helpers
    // (기존 UIEventListener 대응 : UGUI_EventListener 컴포넌트를 이용)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// onClick 등록 (기본 버튼 사운드)
    /// </summary>
    protected void OnClickEventListener(GameObject obj, UGUI_EventListener.VoidDelegate click, object par = null)
    {
        var listener = UGUI_EventListener.Get(obj);
        listener.soundType = Constant.BUTTON_SOUND_TYPE.BASE;
        listener.onClick = click;
        listener.parameter = par;
    }

    protected void OnClickEventListener(UGUI_EventListener obj, UGUI_EventListener.VoidDelegate click, object par = null)
    {
        obj.onClick = click;
        obj.parameter = par;
    }

    /// <summary>
    /// onClick 등록 (사운드 타입 지정)
    /// </summary>
    protected void OnClickEventListener(Constant.BUTTON_SOUND_TYPE soundType, GameObject obj, UGUI_EventListener.VoidDelegate click, object par = null)
    {
        var listener = UGUI_EventListener.Get(obj);
        listener.soundType = soundType;
        listener.onClick = click;
        listener.parameter = par;
    }

    protected void OnClickEventListener(Constant.BUTTON_SOUND_TYPE soundType, UGUI_EventListener obj, UGUI_EventListener.VoidDelegate click, object par = null)
    {
        obj.soundType = soundType;
        obj.onClick = click;
        obj.parameter = par;
    }

    protected void OnDragEventListener(GameObject obj, UGUI_EventListener.VectorDelegate drag)
    {
        UGUI_EventListener.Get(obj).onDrag = drag;
    }

    protected void OnPressEventListener(GameObject obj, UGUI_EventListener.BoolDelegate press)
    {
        UGUI_EventListener.Get(obj).onPress = press;
    }

    protected void OnPressEventListener(UGUI_EventListener obj, UGUI_EventListener.BoolDelegate press)
    {
        obj.onPress = press;
    }

    protected void OnHoverEventListener(GameObject obj, UGUI_EventListener.BoolDelegate hover)
    {
        UGUI_EventListener.Get(obj).onHover = hover;
    }

    protected void OnHoverEventListener(UGUI_EventListener obj, UGUI_EventListener.BoolDelegate hover)
    {
        obj.onHover = hover;
    }

    protected void SetBtnParameter(GameObject obj, object setPar)
    {
        UGUI_EventListener.Get(obj).parameter = setPar;
    }

    protected object GetBtnParameter(GameObject obj)
    {
        return UGUI_EventListener.Get(obj).parameter;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Common Popup Helper
    // ──────────────────────────────────────────────────────────────────────────

    protected void UpdatePopupMessage()
    {
        Parameter openData = new Parameter();
        openData["PopUpType"] = Constant.MESSAGE_POPUP_TYPE.ONE_BTN;
        openData["YesButtonText"] = MessageDataManager.Instance.GetStringByIdx(300043);
        openData["MessageText"] = MessageDataManager.Instance.GetStringByIdx(10004);
        GameUIManager.instance.OpenWindow(UICommon.W_COMMON_MESSAGE, openData);
    }
}//UGUI_WindowUIFormBase


// ============================================================================
// UGUI EventListener
// NGUI UIEventListener 대응 컴포넌트
// IPointerClickHandler, IBeginDragHandler, IDragHandler,
// IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler 구현
// ============================================================================

/// <summary>
/// UGUI 이벤트 리스너 : NGUI UIEventListener 대응
/// </summary>
[DisallowMultipleComponent]
public class UGUI_EventListener : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public delegate void VoidDelegate(GameObject go);
    public delegate void BoolDelegate(GameObject go, bool state);
    public delegate void VectorDelegate(GameObject go, Vector2 delta);

    public VoidDelegate   onClick  = null;
    public VectorDelegate onDrag   = null;
    public BoolDelegate   onPress  = null;
    public BoolDelegate   onHover  = null;

    public Constant.BUTTON_SOUND_TYPE soundType = Constant.BUTTON_SOUND_TYPE.BASE;
    public object parameter = null;

    private bool _isDragging = false;

    /// <summary>
    /// 오브젝트에 UGUI_EventListener를 붙이거나 반환한다.
    /// </summary>
    public static UGUI_EventListener Get(GameObject go)
    {
        UGUI_EventListener listener = go.GetComponent<UGUI_EventListener>();
        if (listener == null)
            listener = go.AddComponent<UGUI_EventListener>();
        return listener;
    }

    // ── IPointerClickHandler ──────────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;

        // TODO: UGUI 사운드 재생 구현 필요
        // if (soundType != Constant.BUTTON_SOUND_TYPE.SOUND_OFF)
        //     CommonUtil.PlayBtnSound(soundType);

        onClick?.Invoke(gameObject);
    }

    // ── Drag ──────────────────────────────────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        onDrag?.Invoke(gameObject, eventData.delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    // ── Press ─────────────────────────────────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        onPress?.Invoke(gameObject, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onPress?.Invoke(gameObject, false);
    }

    // ── Hover ─────────────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke(gameObject, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHover?.Invoke(gameObject, false);
    }
}//UGUI_EventListener


// ============================================================================
// UGUI 잠금 배경 기본 클래스 (NGUI WindowBaseLockBackGroung 대응 스텁)
// 실제 구현은 이 클래스를 상속해서 작성한다.
// ============================================================================
public class UGUI_WindowBaseLockBackground : MonoBehaviour
{
    public virtual void Initialze(bool checkWindow) { }
    public virtual void Activate(bool state) { gameObject.SetActive(state); }
    public virtual void ActiveBg(bool state) { }
    public virtual void ActiveMoveBg(bool state) { }
    public virtual void Release() { }
}//UGUI_WindowBaseLockBackground


// ============================================================================
// 하위 윈도우 타입들
// ============================================================================

/// <summary>
/// 튜토리얼 윈도우
/// </summary>
public class UGUI_TutorialWindowUIForm : UGUI_WindowUIFormBase
{//UGUI_TutorialWindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.TUTORIAL_WINDOW; }
    }

    protected override void ActivateOn()
    {
        gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        gameObject.SetActive(false);
        base.ActivateOff();
    }

    public override void Close()
    {
        base.Close();
    }
}//UGUI_TutorialWindowUIForm


/// <summary>
/// 고정 윈도우 : 다른 윈도우가 열려도 닫히면 안 되는 UI
/// </summary>
public class UGUI_FittedWindowUIForm : UGUI_WindowUIFormBase
{//UGUI_FittedWindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.FITTED_WINDOW; }
    }

    protected override void ActivateOn()
    {
        this.gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        this.gameObject.SetActive(false);
        base.ActivateOff();
    }

    public virtual void UpdateWindow() { }
}//UGUI_FittedWindowUIForm


/// <summary>
/// 일반 윈도우
/// </summary>
public class UGUI_WindowUIForm : UGUI_WindowUIFormBase
{//UGUI_WindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.WINDOW; }
    }

    public override void Initialze()
    {
        CreateBaseLockBg(true);
        base.Initialze();
    }

    public override void Close()
    {
        base.Close();
    }

    protected override void ActivateOn()
    {
        this.gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        this.gameObject.SetActive(false);
        base.ActivateOff();
    }
}//UGUI_WindowUIForm


/// <summary>
/// 팝업
/// </summary>
public class UGUI_PopUpUIForm : UGUI_WindowUIFormBase
{//UGUI_PopUpUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.POPUP_WINDOW; }
    }

    public override void Initialze()
    {
        CreateBaseLockBg(false);
        base.Initialze();
    }

    public override void Close()
    {
        this.gameObject.SetActive(false);
        base.Close();
    }
}//UGUI_PopUpUIForm


/// <summary>
/// 다른 팝업이 열려도 오픈 상태를 유지하는 팝업
/// </summary>
public class UGUI_ActiveOnPopUpUIForm : UGUI_WindowUIFormBase
{//UGUI_ActiveOnPopUpUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW; }
    }

    public override void Initialze()
    {
        CreateBaseLockBg(false);
        base.Initialze();
    }

    public override void Close()
    {
        this.gameObject.SetActive(false);
        base.Close();
    }
}//UGUI_ActiveOnPopUpUIForm


/// <summary>
/// 툴팁 : 터치/클릭 발생 시 자동으로 닫힌다.
/// NGUI UICamera.onClick 대응으로 UGUI_Camera.onClick 이벤트를 사용한다.
/// </summary>
public class UGUI_ToolTipUIForm : UGUI_WindowUIFormBase
{//UGUI_ToolTipUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.TOOLTIP_WINDOW; }
    }

    /// <summary>특정 상황에서 터치로 닫기를 막는 변수</summary>
    protected bool _checkTouchClose = true;

    /// <summary>툴팁을 닫을 때 실행할 이벤트</summary>
    protected Action _closeAction = null;

    public override void Open(Parameter par = null)
    {
        _checkTouchClose = true;
        base.Open(par);

        if (_openParameter != null)
        {
            if (_openParameter.ContainsKey("CloseAction"))
                _closeAction = _openParameter["CloseAction"] as Action;
        }

        UGUI_Camera.onClick += OnClickClose;
    }

    public override void Close()
    {
        UGUI_Camera.onClick -= OnClickClose;

        if (_closeAction != null)
        {
            _closeAction();
            _closeAction = null;
        }

        base.Close();
    }

    protected void OnClickClose(GameObject go)
    {
        if (_checkOpen && _checkActive && _checkTouchClose)
            Close();
    }
}//UGUI_ToolTipUIForm


/// <summary>
/// 윈도우 내부 서브 UI 오브젝트에 UGUI_WindowUIFormBase 기능을 부여하기 위한 클래스
/// 타입 지정 없음 (WINDOW_TYPE.NONE)
/// </summary>
public class UGUI_SubWindowUIForm : UGUI_WindowUIFormBase
{//UGUI_SubWindowUIForm
    public override Constant.WINDOW_TYPE WindowType
    {
        get { return Constant.WINDOW_TYPE.NONE; }
    }

    // Canvas가 없을 경우를 위한 폴백 필드
    protected int    _depth              = 0;
    protected string _sortingLayerName   = string.Empty;
    protected int    _sortingOrder       = 0;

    public override int Depth
    {
        get { return _canvas != null ? _canvas.sortingOrder : _depth; }
        set { if (_canvas != null) _canvas.sortingOrder = value; else _depth = value; }
    }

    public override string SortingLayerName
    {
        get { return _canvas != null ? _canvas.sortingLayerName : _sortingLayerName; }
        set { if (_canvas != null) _canvas.sortingLayerName = value; else _sortingLayerName = value; }
    }

    public override int SortingOrder
    {
        get { return _canvas != null ? _canvas.sortingOrder : _sortingOrder; }
        set { if (_canvas != null) _canvas.sortingOrder = value; else _sortingOrder = value; }
    }

    public override void Close()
    {
        if (_openParameter != null)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;
        _checkActive = false;

        this.gameObject.SetActive(false);
    }

    protected override void ActivateOn()
    {
        this.gameObject.SetActive(true);
        base.ActivateOn();
    }

    protected override void ActivateOff()
    {
        this.gameObject.SetActive(false);
        base.ActivateOff();
    }

    public override void Open(Parameter par = null)
    {
        if (par != null)
        {
            if (_openParameter != null)
            {
                _openParameter.Clear();
                _openParameter = null;
            }
            _openParameter = par;
        }

        _checkOpen = true;
        _checkActive = true;

        this.gameObject.SetActive(true);
    }

    public virtual void SubWindClose()
    {
        if (_openParameter != null)
        {
            _openParameter.Clear();
            _openParameter = null;
        }

        _checkOpen = false;
        this.gameObject.SetActive(false);
    }

    public virtual void UpdateWindow() { }
}//UGUI_SubWindowUIForm


// ============================================================================
// UGUI_Camera — NGUI UICamera.onClick 대응 정적 이벤트 헬퍼
// 씬에 UGUI_CameraEventDriver MonoBehaviour를 하나 배치해서 이벤트를 구동한다.
// ============================================================================

/// <summary>
/// NGUI UICamera.onClick 대응 글로벌 클릭 이벤트.
/// 씬의 UGUI_CameraEventDriver가 EventSystem 입력을 감지해서 InvokeClick을 호출한다.
/// </summary>
public static class UGUI_Camera
{
    public static event Action<GameObject> onClick;

    internal static void InvokeClick(GameObject go) => onClick?.Invoke(go);
}

/// <summary>
/// UGUI_Camera.onClick을 구동하는 MonoBehaviour.
/// 씬에 하나 배치하거나 GameUIManager 초기화 시 생성한다.
/// </summary>
public class UGUI_CameraEventDriver : MonoBehaviour
{
    private void Update()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            var raycast = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.mousePosition
            };
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, raycast);
            GameObject hit = raycast.Count > 0 ? raycast[0].gameObject : null;
            UGUI_Camera.InvokeClick(hit);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            var raycast = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.GetTouch(0).position
            };
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, raycast);
            GameObject hit = raycast.Count > 0 ? raycast[0].gameObject : null;
            UGUI_Camera.InvokeClick(hit);
        }
#endif
    }
}//UGUI_CameraEventDriver
