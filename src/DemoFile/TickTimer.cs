namespace DemoFile;

internal interface ITickTimer
{
    public void Invoke();
}

public sealed class TickTimer<T> : ITickTimer, IDisposable
{
    private T _state;
    private Action<T>? _callback;

    public TickTimer(T state, Action<T> callback)
    {
        _state = state;
        _callback = callback;
    }

    public void Invoke()
    {
        // Callback will be null if the timer was disposed
        _callback?.Invoke(_state);
    }

    public void Dispose()
    {
        _callback = null;
        _state = default!;
    }
}
