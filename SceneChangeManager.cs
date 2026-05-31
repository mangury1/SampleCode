using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CHANGE_GAME_SCENE_TYPE
{
    WAITING_ROOM, // 대기방으로 이동
    PLAY_GAME     // 게임으로 바로 이동
}

public class SceneChangeManager : MonoBehaviour
{
    private AsyncOperation _async = null;
    private AsyncOperation _loadingSceneAsync = null;
    private string _nextSceneName = string.Empty;

    private CancellationTokenSource _processCts = null;
    private CancellationTokenSource _checkLoadCts = null;

    /// <summary>현재 씬 이름 (직접 추적)</summary>
    public string CurrentSceneName { get; private set; }

    /// <summary>Unity가 인식하는 현재 씬 이름</summary>
    public string UnityActiveSceneName => SceneManager.GetActiveScene().name;

    public AsyncOperation GetLoadingSceneAsync => _loadingSceneAsync;

    // ── 싱글톤 ────────────────────────────────────────────────────────────
    private static SceneChangeManager _instance;
    public static SceneChangeManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            GameObject obj = GameObject.Find(typeof(SceneChangeManager).Name);
            if (obj == null)
            {
                obj = new GameObject(typeof(SceneChangeManager).Name);
                _instance = obj.AddComponent<SceneChangeManager>();
            }
            else
            {
                _instance = obj.GetComponent<SceneChangeManager>();
            }
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }

    private Dictionary<string, BaseSceneScripts> _sceneScriptsList = null;

    /// <summary>진입할 게임 씬 타입</summary>
    public CHANGE_GAME_SCENE_TYPE ChangeGameSceneType { get; private set; }

    // ── Unity 생명주기 ────────────────────────────────────────────────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitSceneScriptsList();
    }

    private void OnDestroy()
    {
        DestroySceneScriptsList();
        CancelAndDispose(ref _processCts);
        CancelAndDispose(ref _checkLoadCts);
    }

    // ── 씬 스크립트 관리 ─────────────────────────────────────────────────
    private void InitSceneScriptsList()
    {
        if (_sceneScriptsList != null)
            DestroySceneScriptsList();
        _sceneScriptsList = new Dictionary<string, BaseSceneScripts>();
    }

    private void DestroySceneScriptsList()
    {
        if (_sceneScriptsList == null) return;
        foreach (var pair in _sceneScriptsList)
            pair.Value.Release();
        _sceneScriptsList.Clear();
        _sceneScriptsList = null;
    }

    public void RegisterSceneScript<T>(string sceneName) where T : BaseSceneScripts, new()
    {
        if (_sceneScriptsList.ContainsKey(sceneName)) return;
        T script = new T();
        script.Initialze(sceneName);
        _sceneScriptsList.Add(sceneName, script);
    }

    public void RegisterSceneScript(string sceneName, BaseSceneScripts script)
    {
        if (_sceneScriptsList.ContainsKey(sceneName)) return;
        script.Initialze(sceneName);
        _sceneScriptsList.Add(sceneName, script);
    }

    public S GetSceneScripts<S>(string sceneName) where S : BaseSceneScripts
    {
        if (_sceneScriptsList != null && _sceneScriptsList.ContainsKey(sceneName))
            return _sceneScriptsList[sceneName] as S;
        return null;
    }

    // ── 씬 상태 조회 ──────────────────────────────────────────────────────
    public void SetChangeGameSceneType(CHANGE_GAME_SCENE_TYPE type) => ChangeGameSceneType = type;
    public bool CheckMatchGame() => ChangeGameSceneType == CHANGE_GAME_SCENE_TYPE.PLAY_GAME;
    public bool CheckScene(string sceneName) => string.Equals(CurrentSceneName, sceneName);

    public bool CheckOutGameScene()
        => CheckScene(Constant.S_MAIN_ROOM_NAME)
        || CheckScene(Constant.S_CLIENT_ASSET_NAME)
        || CheckScene(Constant.S_CLIENT_NAME);

    // ── 공개 씬 전환 API ──────────────────────────────────────────────────

    /// <summary>일반 씬 전환 (EmptyScene 경유 GC 패턴)</summary>
    public void SceneChange(string sceneName, string scriptsName = null)
    {
        if (!string.IsNullOrEmpty(scriptsName))
            Debug.LogWarning("__ !! SceneChange ScriptsName = " + scriptsName);

        if (string.IsNullOrEmpty(sceneName)) return;

        _nextSceneName = sceneName;

        GameUIManager.instance.GetWindowSceneLoading.StartLoading(true);
        GameUIManager.instance.DestroyWindow();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        if (_processCts == null)
        {
            _processCts = new CancellationTokenSource();
            LoadEmptySceneAsync(_processCts.Token).Forget();
        }
    }

    /// <summary>에셋 씬 전환 (EmptyScene 없이 바로 로드)</summary>
    public void ClientAssetSceneChange(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        _nextSceneName = sceneName;
        GameUIManager.instance.DestroyWindow();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        LoadNextScene();
    }

    public void SceneChangeAdditive(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName) == null)
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    public void SceneChangeUnload(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName) != null)
            SceneManager.UnloadSceneAsync(sceneName);
    }

    // ── 내부 씬 전환 흐름  ──────────────────────────────────

    private async UniTask LoadEmptySceneAsync(CancellationToken ct)
    {
        try
        {
            string nowSceneName = SceneManager.GetActiveScene().name;

            // 현재 씬 탈출 훅 대기
            if (_sceneScriptsList.ContainsKey(nowSceneName))
                await _sceneScriptsList[nowSceneName].OnEscapeAsync(ct);
                
            _async = SceneManager.LoadSceneAsync(Constant.S_EMPTY_SCENE_NAME);
            await _async.ToUniTask(cancellationToken: ct);

            LoadNextScene();
        }
        finally
        {
            // 완료·취소 모두 CTS 정리
            CancelAndDispose(ref _processCts);
        }
    }

    private void LoadNextScene()
    {
        _async = null;

        if (string.IsNullOrEmpty(_nextSceneName)) return;

        CancelAndDispose(ref _checkLoadCts);
        _checkLoadCts = new CancellationTokenSource();
        CheckLoadSceneAsync(_nextSceneName, _checkLoadCts.Token).Forget();
    }

    private async UniTask CheckLoadSceneAsync(string sceneName, CancellationToken ct)
    {
        try
        {
            // 로딩 전용 씬은 동기 전환 후 취소될 때까지 대기
            if (sceneName == Constant.S_FIELD_LOADING_SCENE_NAME
             || sceneName == Constant.S_MAIN_ROOM_LOADING_NAME
             || sceneName == Constant.S_EMPTY_SCENE_NAME)
            {
                SceneManager.LoadScene(_nextSceneName);
                // 기존: while (true) yield return null
                // 변경: 취소 신호가 올 때까지 1프레임씩 대기
                await UniTask.WaitUntilCanceled(ct);
                return;
            }

            // 씬 비동기 로드
            _loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName);
            await _loadingSceneAsync.ToUniTask(cancellationToken: ct);

            // 하위 호환용 자동 등록
            TryAutoRegisterScript(sceneName);
            
            await UniTask.Delay(1000, cancellationToken: ct);

            // 씬 진입 훅 대기
            if (_sceneScriptsList.ContainsKey(sceneName))
                await _sceneScriptsList[sceneName].OnEntryAsync(ct);

            ApplySceneSettings(sceneName);
            CurrentSceneName = sceneName;
            ApplyLoadingUI(sceneName);
        }
        finally
        {
            _loadingSceneAsync = null;
            CancelAndDispose(ref _checkLoadCts);
        }
    }

    // ── 씬 설정 적용 ──────────────────────────────────────────────────────
    private void ApplySceneSettings(string sceneName)
    {
        bool touchEffectEnabled = true;
        if (_sceneScriptsList.TryGetValue(sceneName, out BaseSceneScripts script))
            touchEffectEnabled = script.TouchEffectEnabled;
        MouseTouchManager.Instance.SetActiveEffect(touchEffectEnabled);
    }

    private void ApplyLoadingUI(string sceneName)
    {
        SceneLoadingType loadingType = SceneLoadingType.Default;
        if (_sceneScriptsList.TryGetValue(sceneName, out BaseSceneScripts script))
            loadingType = script.LoadingType;

        switch (loadingType)
        {
            case SceneLoadingType.None:
                break;
            case SceneLoadingType.MatchLoading:
                GameUIManager.instance.GetWindowSceneLoading.StartMatchLoading();
                break;
            case SceneLoadingType.NormalLoading:
                GameUIManager.instance.GetWindowSceneLoading.StartLoading();
                break;
            case SceneLoadingType.Default:
            default:
                GameUIManager.instance.GetWindowSceneLoading.EndLoading(true);
                break;
        }
    }

    private void TryAutoRegisterScript(string sceneName)
    {
        if (_sceneScriptsList.ContainsKey(sceneName)) return;
        switch (sceneName)
        {
            case Constant.S_CLIENT_NAME:
                RegisterSceneScript<ClientSceneScripts>(sceneName); break;
            case Constant.S_MAIN_ROOM_NAME:
                RegisterSceneScript<MainRoomSceneScripts>(sceneName); break;
            case Constant.S_FISHING_GROUND_NAME:
                RegisterSceneScript<FishingGroundSceneScripts>(sceneName); break;
            default:
                RegisterSceneScript<NewSceneScripts>(Constant.S_NEW_SCENE_NAME); break;
        }
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────
    /// <summary>CTS를 안전하게 취소·해제하고 null로 초기화</summary>
    private static void CancelAndDispose(ref CancellationTokenSource cts)
    {
        if (cts == null) return;
        cts.Cancel();
        cts.Dispose();
        cts = null;
    }
}
