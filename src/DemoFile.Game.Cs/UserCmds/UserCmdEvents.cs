namespace DemoFile.Game.Cs;

/// <summary>
/// Player commands (<c>svc_UserCmds</c> in GOTV demos, <c>DEM_UserCmd</c> in POV demos), decoded into
/// <see cref="CSGOUserCmdPB"/>, including delta-encoded commands (CS2 patch 14197+).
/// </summary>
/// <remarks>
/// Nothing is decoded until <see cref="UserCmd"/> has a subscriber: the first subscriber attaches to the
/// underlying demo/packet events and the last one to unsubscribe detaches again.
/// </remarks>
public sealed class UserCmdEvents
{
    private readonly CsDemoParser _demo;
    private readonly UserCmdStream _stream = new();
    private readonly RawMessageHandler _onSvcUserCmds;
    private readonly Action<CDemoUserCmd> _onDemoUserCmd;
    private Action<UserCmd>? _userCmd;
    private bool _isListening;

    internal UserCmdEvents(CsDemoParser demo)
    {
        _demo = demo;
        _onSvcUserCmds = OnSvcUserCmds;
        _onDemoUserCmd = OnDemoUserCmd;
    }

    /// <summary>
    /// Fired for every player command, with delta-encoded commands already applied.
    /// </summary>
    /// <remarks>
    /// The <see cref="DemoFile.Game.Cs.UserCmd.Command"/> instance is reused for the player's next command: clone it to keep it.
    /// </remarks>
    public event Action<UserCmd>? UserCmd
    {
        add
        {
            _userCmd += value;
            if (_userCmd != null && !_isListening)
                StartListening();
        }
        remove
        {
            _userCmd -= value;
            if (_userCmd == null && _isListening)
                StopListening();
        }
    }

    /// <summary>
    /// Forget every player's previous command, e.g. after seeking.
    /// </summary>
    internal void Reset() => _stream.Reset();

    private void StartListening()
    {
        // Commands sent while nobody was listening weren't tracked, so deltas can't be applied
        // to what we have: start from scratch (commands are incomplete until the next full one).
        _stream.Reset();
        _demo.PacketEvents.SvcUserCmdsRaw += _onSvcUserCmds;
        _demo.DemoEvents.DemoUserCmd += _onDemoUserCmd;
        _isListening = true;
    }

    private void StopListening()
    {
        _demo.PacketEvents.SvcUserCmdsRaw -= _onSvcUserCmds;
        _demo.DemoEvents.DemoUserCmd -= _onDemoUserCmd;
        _isListening = false;
    }

    private void OnSvcUserCmds(ReadOnlySpan<byte> msgBuf)
    {
        if (_userCmd is { } callback)
            _stream.Read(msgBuf, callback);
    }

    private void OnDemoUserCmd(CDemoUserCmd msg)
    {
        if (_userCmd is { } callback)
            _stream.ReadDemoUserCmd(msg, _demo.LocalPlayerSlot, callback);
    }
}
