using System.Threading;
using Cysharp.Threading.Tasks;

public abstract class BaseSceneScripts
{
    protected string _sceneName = string.Empty;
    protected bool _checkInit = false;

    public string SceneName => _sceneName;
    public virtual bool TouchEffectEnabled => true;
    public virtual SceneLoadingType LoadingType => SceneLoadingType.Default;

    public virtual void Initialze(string sceneName)
    {
        _sceneName = sceneName;
        _checkInit = true;
    }

    public virtual void Release() { }

    public virtual async UniTask OnEntryAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }

    public virtual async UniTask OnEscapeAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }

    public virtual async UniTask UpdateAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }
}

public enum SceneLoadingType
{
    Default,
    None,
    MatchLoading,
    NormalLoading,
}
