using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UGUI 기반 윈도우 관리 매니저 - 해당 씬에서 상속받아 사용한다.
/// (기존 BaseUIManager의 UIPanel/UIRoot/UIAnchor → Canvas/RectTransform 으로 대체)
/// </summary>
public abstract class UGUI_BaseUIManager<T> : MonoBehaviour where T : UGUI_BaseUIManager<T>
{
    private static T _instance;

    public static T instance
    {
        get
        {
            if (_instance != null)
            {
                if (_instance._loadPrefabsList == null)
                    _instance._loadPrefabsList = new Dictionary<string, GameObject>();
                if (_instance._windowList == null)
                    _instance._windowList = new Dictionary<string, UGUI_WindowUIFormBase>();
                if (_instance._openWindowList == null)
                    _instance._openWindowList = new List<string>();
            }
            return _instance;
        }
        protected set
        {
            _instance = value;
        }
    }

    public static bool InstanceExists { get { return instance != null; } }

    public static event Action InstanceSet;

    // ──────────────────────────────────────────────────────────────────────────
    // Inspector Fields
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// UGUI Root Canvas (NGUI UIRoot 대응)
    /// </summary>
    [SerializeField]
    protected Canvas _uiRootCanvas = null;

    /// <summary>
    /// UI 카메라 배열 (UGUI에서는 Canvas의 worldCamera 설정에 사용)
    /// Screen Space - Camera 모드 Canvas일 경우 연결한다.
    /// Screen Space - Overlay 모드라면 사용하지 않아도 된다.
    /// </summary>
    [SerializeField]
    protected Camera[] _uiCameraArray = null;

    // ──────────────────────────────────────────────────────────────────────────
    // Runtime Collections
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>불러온 프리팹 캐시</summary>
    protected Dictionary<string, GameObject> _loadPrefabsList = new Dictionary<string, GameObject>();

    /// <summary>생성된 윈도우 목록</summary>
    protected Dictionary<string, UGUI_WindowUIFormBase> _windowList = new Dictionary<string, UGUI_WindowUIFormBase>();

    /// <summary>현재 열려 있는 윈도우 이름 스택</summary>
    protected List<string> _openWindowList = new List<string>();

    public Dictionary<string, UGUI_WindowUIFormBase> GetWindowList
    {
        get { return _windowList; }
    }

    public int OpenWindowListCount
    {
        get { return _openWindowList != null ? _openWindowList.Count : 0; }
    }

    /// <summary>
    /// 뎁스 Offset : Canvas sortingOrder 단위로 사용
    /// </summary>
    private const int DEPTH_OFFSET = 100;

    protected bool _isTutorial = false;

    // ──────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        try
        {
            if (instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = (T)this;
                InstanceSet?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    protected virtual void Start()
    {
        try
        {
            Resources.UnloadUnusedAssets();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    protected virtual void OnDestroy()
    {
        ClearWindow();
    }

    protected virtual void LateUpdate() { }

    // ──────────────────────────────────────────────────────────────────────────
    // Window Lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    public virtual void ClearWindow()
    {
        DestroyWindow();
        _loadPrefabsList?.Clear();
        _loadPrefabsList = null;
        _windowList = null;
        _openWindowList = null;
    }

    public virtual void DestroyWindow()
    {
        if (_windowList != null)
        {
            var enumerator = _windowList.GetEnumerator();
            while (enumerator.MoveNext())
            {
                UGUI_WindowUIFormBase uiForm = enumerator.Current.Value;
                if (uiForm.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
                    continue;
                uiForm.Release();
                uiForm.DestroyGameObject();
            }
            _windowList.Clear();
        }

        _openWindowList?.Clear();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FittedWindow Activate
    // ──────────────────────────────────────────────────────────────────────────

    public void ActiveFittedWindowUIForm(bool isActive)
    {
        for (int i = 0; i < _openWindowList.Count; ++i)
        {
            string name = _openWindowList[i];
            if (_windowList[name] is UGUI_FittedWindowUIForm)
                _windowList[name].Activate(isActive);
        }
        ActiveFittedWindowUIFormAdd(isActive);
    }

    /// <summary>고정 UI가 꺼질 때 함께 꺼야 할 UI를 처리한다.</summary>
    protected abstract void ActiveFittedWindowUIFormAdd(bool isActive);

    // ──────────────────────────────────────────────────────────────────────────
    // Prefab Load / Create
    // ──────────────────────────────────────────────────────────────────────────

    protected GameObject LoadPrefab(string prefabName)
    {
        if (!_loadPrefabsList.TryGetValue(prefabName, out GameObject loadPrefab))
        {
            string loadPath = string.Format("{0}{1}", Constant.WINDOW_PREFAD_PATH, prefabName);
            loadPrefab = Resources.Load<GameObject>(loadPath);

            if (loadPrefab == null)
                Debug.LogError("__ !!! Err Not Find Prefab Name = " + prefabName);
            else
                _loadPrefabsList.Add(prefabName, loadPrefab);
        }
        return loadPrefab;
    }

    /// <summary>
    /// 프리팹 생성 후 지정 Canvas의 자식으로 배치한다.
    /// (NGUI CreatePrefad의 UIAnchor 처리는 UGUI RectTransform anchor 로 대체되므로 제거됨)
    /// </summary>
    protected GameObject CreatePrefab(Canvas parentCanvas, string prefabName)
    {
        GameObject loadPrefab = LoadPrefab(prefabName);
        if (loadPrefab == null) return null;

        GameObject created = Instantiate(loadPrefab);
        created.name = prefabName;

        RectTransform rt = created.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.SetParent(parentCanvas.transform, false);
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            created.transform.SetParent(parentCanvas.transform, false);
            created.transform.localPosition = Vector3.zero;
            created.transform.localRotation = Quaternion.identity;
            created.transform.localScale = Vector3.one;
        }

        if (created == null)
            Debug.LogError("__ !!! Err Not Create Prefab Name = " + prefabName);

        return created;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Get Window
    // ──────────────────────────────────────────────────────────────────────────

    public UGUI_WindowUIFormBase GetWindow(string windowName)
    {
        if (_windowList != null && _windowList.ContainsKey(windowName))
            return _windowList[windowName];
        return null;
    }

    public C GetWindow<C>(string windowName) where C : UGUI_WindowUIFormBase
    {
        if (_windowList != null && _windowList.ContainsKey(windowName))
            return _windowList[windowName] as C;
        return null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Depth (UGUI Canvas sortingOrder)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// UGUI용 depth 조정 : 루트 Canvas의 sortingOrder를 기준값으로 설정하고,
    /// 하위 Canvas들은 순서대로 +1 씩 올린다.
    /// (NGUI CommonUtil.AdjustDepth2 대응)
    /// </summary>
    public static void AdjustCanvasDepth(GameObject go, int baseOrder)
    {
        if (go == null) return;

        Canvas rootCanvas = go.GetComponent<Canvas>();
        if (rootCanvas == null) return;

        Canvas[] canvases = go.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; ++i)
        {
            canvases[i].overrideSorting = true;
            canvases[i].sortingLayerName = CommonUtil.GetSortingLayerNameArry()[1];
            canvases[i].sortingOrder = baseOrder + i;
        }
    }

    /// <summary>
    /// 윈도우 뎁스 변경 (다른 윈도우 위로 올리거나 내릴 때)
    /// </summary>
    public void ChangeWindowDepth(UGUI_WindowUIFormBase window, int depth)
    {
        if (window == null) return;

        if (!window.CheckActive || !window.CheckOpen)
            window.Activate(true);

        AdjustCanvasDepth(window.gameObject, depth);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Open Window
    // ──────────────────────────────────────────────────────────────────────────

    public UGUI_WindowUIFormBase OpenWindow(string windowName, Parameter par = null,
        Constant.UI_CAMERA_NUMBER cameraNumber = Constant.UI_CAMERA_NUMBER.ONE)
    {
        Canvas targetCanvas = GetCanvasForCamera(cameraNumber);
        return OpenWindow(targetCanvas, windowName, par);
    }

    public UGUI_WindowUIFormBase OpenWindow(Canvas parentCanvas, string windowName, Parameter par)
    {//1
        UGUI_WindowUIFormBase openWindow = GetWindow(windowName);

        if (openWindow == null)
        {
            GameObject createObj = CreatePrefab(parentCanvas, windowName);

            if (createObj == null)
            {
                Debug.LogError("__ !!! Err Null createObj. windowName=" + windowName + " par=" + par);
                return null;
            }

            openWindow = createObj.GetComponent<UGUI_WindowUIFormBase>();

            if (openWindow == null)
            {
                Debug.LogError("__ !!! Err Null window. windowName=" + windowName + " par=" + par);
                Destroy(createObj);
                return null;
            }
        }

        switch (openWindow.WindowType)
        {
            case Constant.WINDOW_TYPE.NONE:
            case Constant.WINDOW_TYPE.BASE:
                Debug.LogError("__ !! Err 잘못된 타입의 윈도우가 요청됨. WindowName = " + windowName);
                return null;
        }

        UGUI_WindowUIFormBase tutorialWindow = null;
        int curTopDepth = 0;
        bool activeOnPopup = false;

        if (_openWindowList.Count > 0)
        {//2-6
            bool alreadyOpen = _openWindowList.Contains(windowName);
            if (alreadyOpen)
                _openWindowList.Remove(windowName);

            if (_openWindowList.Count > 0)
            {
                for (int i = _openWindowList.Count - 1; i >= 0; --i)
                {//2-6-1
                    string windName = _openWindowList[i];
                    UGUI_WindowUIFormBase checkWindow = _windowList[windName];

                    if (checkWindow.WindowType == Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW)
                        activeOnPopup = true;
                    else if (checkWindow.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
                        tutorialWindow = checkWindow;

                    if (checkWindow != null && openWindow.name != checkWindow.name)
                    {//2-6-2
                        switch (checkWindow.WindowType)
                        {
                            case Constant.WINDOW_TYPE.TUTORIAL_WINDOW:
                                // 튜토리얼 : 별도 처리 없음
                                break;

                            case Constant.WINDOW_TYPE.WINDOW:
                                // 새 창이 일반 창일 때만 이전 창 비활성화
                                if (openWindow.WindowType == Constant.WINDOW_TYPE.WINDOW)
                                {
                                    if (checkWindow.name.Equals(UICommon.W_LOBBY_MATCHING))
                                    {
                                        // 매칭 윈도우는 닫지 않음
                                    }
                                    else if (checkWindow.CheckOpen && checkWindow.CheckActive)
                                    {
                                        checkWindow.Activate(false);
                                    }
                                }
                                break;

                            case Constant.WINDOW_TYPE.POPUP_WINDOW:
                            case Constant.WINDOW_TYPE.TOOLTIP_WINDOW:
                                // 팝업/툴팁은 바로 닫는다
                                if (checkWindow.CheckOpen && checkWindow.CheckActive)
                                    checkWindow.Close();
                                break;

                            case Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW:
                                // 새 창이 일반 창일 때만 팝업 닫기
                                if (openWindow.WindowType == Constant.WINDOW_TYPE.WINDOW)
                                {
                                    if (checkWindow.CheckOpen && checkWindow.CheckActive)
                                        checkWindow.Close();
                                }
                                break;

                            default:
                                break;
                        }
                    }//2-6-2

                    if (curTopDepth < checkWindow.Depth)
                        curTopDepth = checkWindow.Depth;
                }//2-6-1
            }
        }//2-6

        // ── 뎁스 계산 & 적용 ──────────────────────────────────────────────────
        switch (openWindow.WindowType)
        {
            case Constant.WINDOW_TYPE.WINDOW:
            case Constant.WINDOW_TYPE.POPUP_WINDOW:
            case Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW:
            case Constant.WINDOW_TYPE.TOOLTIP_WINDOW:
            {
                int curDepth  = curTopDepth > 0 ? curTopDepth / DEPTH_OFFSET : 0;
                int nextDepth = curDepth > 0 ? (curDepth + 1) * DEPTH_OFFSET : DEPTH_OFFSET;

                if (activeOnPopup)
                    nextDepth += DEPTH_OFFSET;

                if (openWindow.CheckInitialized)
                    openWindow.Depth = 1;

                AdjustCanvasDepth(openWindow.GetGameObject, nextDepth);

                if (tutorialWindow != null)
                    AdjustCanvasDepth(tutorialWindow.gameObject, nextDepth + DEPTH_OFFSET);
            }
            break;

            case Constant.WINDOW_TYPE.TUTORIAL_WINDOW:
            {
                AdjustCanvasDepth(openWindow.GetGameObject, curTopDepth + DEPTH_OFFSET);
                _isTutorial = true;
            }
            break;
        }

        openWindow.GetGameObject.SetActive(true);

        if (!openWindow.CheckInitialized)
        {
            openWindow.Initialze();
            CloseEventWindowsAdd(openWindow);
        }

        // 윈도우 리스트 등록
        if (_windowList.ContainsKey(windowName))
            _windowList[windowName] = openWindow;
        else
            _windowList.Add(windowName, openWindow);

        _openWindowList.Add(windowName);

        openWindow.Open(par);

        return openWindow;
    }//1

    // ──────────────────────────────────────────────────────────────────────────
    // Close Window
    // ──────────────────────────────────────────────────────────────────────────

    public void CloseWindow(string windowName)
    {
        if (_openWindowList == null || _openWindowList.Count == 0) return;

        if (_openWindowList.Contains(windowName))
        {
            UGUI_WindowUIFormBase closeWindow = GetWindow(windowName);
            if (closeWindow != null)
            {
                closeWindow.ForciblyClose();
                _openWindowList.Remove(windowName);
            }
            else
            {
                Debug.LogError("__ !! Err 등록이 안된 윈도우 = " + windowName);
                _openWindowList.Remove(windowName);
            }
        }
    }

    /// <summary>고정형 · 튜토리얼 윈도우를 제외한 모든 창 닫기</summary>
    public void CloseWindows()
    {
        if (_openWindowList == null || _openWindowList.Count == 0) return;

        for (int i = _openWindowList.Count - 1; i > 0; --i)
        {
            string windowName = _openWindowList[i];
            UGUI_WindowUIFormBase closeWindow = GetWindow(windowName);
            if (closeWindow != null &&
                closeWindow.WindowType != Constant.WINDOW_TYPE.FITTED_WINDOW &&
                closeWindow.WindowType != Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
            {
                closeWindow.ForciblyClose();
                _openWindowList.Remove(windowName);
            }
        }
    }

    /// <summary>지정한 윈도우를 제외한 모든 창 닫기 (고정형 · 튜토리얼 제외)</summary>
    public void CloseWindows(string activeOnWindName)
    {
        if (_openWindowList == null || _openWindowList.Count == 0) return;

        for (int i = _openWindowList.Count - 1; i > 0; --i)
        {
            string windowName = _openWindowList[i];
            UGUI_WindowUIFormBase closeWindow = GetWindow(windowName);
            if (closeWindow != null &&
                !string.Equals(activeOnWindName, windowName) &&
                closeWindow.WindowType != Constant.WINDOW_TYPE.FITTED_WINDOW &&
                closeWindow.WindowType != Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
            {
                closeWindow.ForciblyClose();
                _openWindowList.Remove(windowName);
            }
        }
    }

    /// <summary>튜토리얼 윈도우를 제외한 모든 창 닫기</summary>
    public void AllCloseWindows()
    {
        if (_openWindowList == null || _openWindowList.Count == 0) return;

        for (int i = 0; i < _openWindowList.Count; i++)
        {
            string windowName = _openWindowList[i];
            UGUI_WindowUIFormBase closeWindow = GetWindow(windowName);
            if (closeWindow != null && closeWindow.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
                continue;
            closeWindow?.ForciblyClose();
        }
        _openWindowList.Clear();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Close Event
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 윈도우 초기화 시 OnClosed 이벤트에 PreWindowOpen을 등록한다.
    /// </summary>
    protected void CloseEventWindowsAdd(UGUI_WindowUIFormBase wind)
    {
        if (wind != null)
            wind.OnClosed += PreWindowOpen;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Open Window List Management
    // ──────────────────────────────────────────────────────────────────────────

    public void OpenWindowListAdd(string windowName, UGUI_WindowUIFormBase window)
    {
        if (_openWindowList != null && !_openWindowList.Contains(windowName))
            _openWindowList.Add(windowName);

        if (!_windowList.ContainsKey(windowName))
            _windowList.Add(windowName, window);
        else
            _windowList[windowName] = window;
    }

    public void OpenWindowListRemove(string windowName)
    {
        _openWindowList?.Remove(windowName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Pre-Window Open (이전 창 복원)
    // ──────────────────────────────────────────────────────────────────────────

    public void PreWindowOpen(string windowName, bool clickEsc = false)
    {
        UGUI_WindowUIFormBase closeWindow = GetWindow(windowName);

        if (closeWindow == null)
        {
            Debug.Log($"Close Window Is Null :: {windowName}");
            return;
        }

        if (closeWindow.WindowType == Constant.WINDOW_TYPE.FITTED_WINDOW)
            return;
        else if (closeWindow.WindowType == Constant.WINDOW_TYPE.TUTORIAL_WINDOW)
            _isTutorial = false;

        if (clickEsc)
            closeWindow.Close();

        for (int i = _openWindowList.Count - 1; i >= 0; --i)
        {
            if (string.Equals(windowName, _openWindowList[i]))
                _openWindowList.RemoveAt(i);
        }

        UpdatePreWindow();
    }

    /// <summary>
    /// OnClosed 이벤트 콜백 : 창이 닫힐 때 자동 호출된다.
    /// </summary>
    public void PreWindowOpen(UGUI_WindowUIFormBase closeWindow)
    {
        if (closeWindow == null) return;

        string windName = closeWindow.gameObject.name;
        Constant.WINDOW_TYPE windowType = closeWindow.WindowType;
        bool checkUpdatePreWindow = true;

        switch (windowType)
        {
            // 툴팁이 닫힐 때는 이전 윈도우 UI를 갱신하지 않는다.
            case Constant.WINDOW_TYPE.TOOLTIP_WINDOW:
                checkUpdatePreWindow = false;
                break;

            // 고정형 창은 처리하지 않는다.
            case Constant.WINDOW_TYPE.FITTED_WINDOW:
                return;

            case Constant.WINDOW_TYPE.TUTORIAL_WINDOW:
                _isTutorial = false;
                break;
        }

        for (int i = _openWindowList.Count - 1; i >= 0; --i)
        {
            if (string.Equals(windName, _openWindowList[i]))
                _openWindowList.RemoveAt(i);
        }

        if (checkUpdatePreWindow)
            UpdatePreWindow();
    }

    public void UpdatePreWindow()
    {
        if (_openWindowList == null || _openWindowList.Count == 0) return;

        string activateName = _openWindowList.Last();
        UGUI_WindowUIFormBase activateWindow = GetWindow(activateName);

        if (activateWindow != null)
        {
            switch (activateWindow.WindowType)
            {
                case Constant.WINDOW_TYPE.WINDOW:
                case Constant.WINDOW_TYPE.ACTIVEON_POPUP_WINDOW:
                    activateWindow.Activate(true);
                    break;

                case Constant.WINDOW_TYPE.FITTED_WINDOW:
                    (activateWindow as UGUI_FittedWindowUIForm)?.UpdateWindow();
                    break;

                default:
                    break;
            }
        }
        else
        {
            Debug.LogError("__ !! Err Null activateWindow Name = " + activateName);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// UI_CAMERA_NUMBER에 대응하는 Canvas를 반환한다.
    /// Screen Space - Overlay 구성이라면 _uiRootCanvas를 그대로 반환한다.
    /// </summary>
    protected virtual Canvas GetCanvasForCamera(Constant.UI_CAMERA_NUMBER cameraNumber)
    {
        return _uiRootCanvas;
    }

    /// <summary>게임 종료 확인 팝업</summary>
    protected void PopupAppQuit()
    {
        Parameter openData = new Parameter();
        openData["PopUpType"] = Constant.MESSAGE_POPUP_TYPE.TWO_BTN;
        openData["MessageText"] = MessageDataManager.Instance.GetStringByIdx(1700018);
        openData["NoButtonText"] = MessageDataManager.Instance.GetStringByIdx(300045);
        openData["YesButtonText"] = MessageDataManager.Instance.GetStringByIdx(300043);
        openData["RightButtonAction"] = new Action(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
        OpenWindow(UICommon.W_COMMON_MESSAGE, openData);
    }
}
