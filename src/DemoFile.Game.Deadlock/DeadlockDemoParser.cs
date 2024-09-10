using DemoFile.Sdk;

namespace DemoFile;

public sealed class DeadlockDemoParser : DemoParser<DeadlockDemoParser>
{
    private EntityEvents _entityEvents;

    public ref EntityEvents EntityEvents => ref _entityEvents;

    /// <summary>
    /// All connected players.
    /// </summary>
    public IEnumerable<CCitadelPlayerController> Players
    {
        get
        {
            for (var i = 1; i <= MaxPlayers; ++i)
            {
                if (_entities[i] is CCitadelPlayerController { Connected: PlayerConnectedState.PlayerConnected } controller)
                    yield return controller;
            }
        }
    }

    /// <summary>
    /// All players - including those that have disconnected or are reconnecting.
    /// </summary>
    public IEnumerable<CCitadelPlayerController> PlayersIncludingDisconnected
    {
        get
        {
            for (var i = 1; i <= MaxPlayers; ++i)
            {
                if (_entities[i] is CCitadelPlayerController controller)
                    yield return controller;
            }
        }
    }

    protected override IReadOnlyDictionary<string, EntityFactory<DeadlockDemoParser>> EntityFactories =>
        DeadlockEntityFactories.All;

    protected override ref EntityEvents<CEntityInstance<DeadlockDemoParser>, DeadlockDemoParser> EntityInstanceEvents => ref EntityEvents.CEntityInstance;

    protected override DecoderSet CreateDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers)
    {
        return new DeadlockDecoderSet(serializers);
    }

    protected override bool ParseNetMessage(int msgType, ReadOnlySpan<byte> msgBuf)
    {
        return false;
    }
}
