namespace FancadeLoaderLib.Runtime.Compiled.Utils;

internal sealed class Disposable : IDisposable
{
    private Action? _onDispose;

    public Disposable(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        _onDispose?.Invoke();
        _onDispose = null;
    }
}
