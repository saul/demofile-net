using DemoFile.Game.Deadlock;
using DemoFile.Sdk;

namespace DemoFile;

public sealed class DeadlockDemoParser : DemoParser<DeadlockDemoParser>
{
    private readonly CHandle<CCitadelTeam, DeadlockDemoParser>[] _teamHandles = new CHandle<CCitadelTeam, DeadlockDemoParser>[4];
    private EntityEvents _entityEvents;
    private GameEvents _gameEvents;
    private CHandle<CCitadelGameRulesProxy, DeadlockDemoParser> _gameRulesHandle;
    private UserMessageEvents _userMessageEvents;

    public DeadlockDemoParser()
    {
        Source1GameEvents = new Source1GameEvents(this);
        BaseGameEvents.Source1LegacyGameEventList += Source1GameEvents.ParseSource1GameEventList;
        BaseGameEvents.Source1LegacyGameEvent += @event => Source1GameEvents.ParseSource1GameEvent(this, @event);

        PacketEvents.SvcServerInfo += e =>
        {
            var gameDirParts = e.GameDir.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            if (gameDirParts[^1] != "citadel")
            {
                throw new InvalidDemoException($"Cannot use {nameof(DeadlockDemoParser)} to read a '{gameDirParts[^1]}' demo (expected 'citadel').");
            }
        };
    }

    public Source1GameEvents Source1GameEvents { get; }

    public ref UserMessageEvents UserMessageEvents => ref _userMessageEvents;
    public ref EntityEvents EntityEvents => ref _entityEvents;
    public ref GameEvents GameEvents => ref _gameEvents;

    /// <summary>
    /// The <see cref="CCitadelTeam"/> entity representing the Spectators.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCitadelTeam TeamSpectator => GetTeam(TeamNumber.Spectator);

    /// <summary>
    /// The <see cref="CCitadelTeam"/> entity representing the Amber Hand.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCitadelTeam TeamAmber => GetTeam(TeamNumber.Amber);

    /// <summary>
    /// The <see cref="CCitadelTeam"/> entity representing the Sapphire Flame.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCitadelTeam TeamSapphire => GetTeam(TeamNumber.Sapphire);

    /// <summary>
    /// The <see cref="CCitadelGameRules"/> entity representing the game rules
    /// (e.g. current game state)
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCitadelGameRules GameRules => GetCachedSingletonEntity(ref _gameRulesHandle).GameRules!;

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

    public static int TickRate => 60;

    protected override DecoderSet CreateDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers)
    {
        return new DeadlockDecoderSet(serializers);
    }

    /// <summary>
    /// Get the <see cref="CCitadelTeam"/> entity representing a given team.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCitadelTeam GetTeam(TeamNumber teamNumber) =>
        GetCachedSingletonEntity(
            ref _teamHandles[(int)teamNumber],
            team => team.CitadelTeamNum == teamNumber);

    public CCitadelPlayerController? GetPlayerByUserId(ushort userId)
    {
        for (var slot = 0; slot < PlayerInfos.Count; slot++)
        {
            var playerInfo = PlayerInfos[slot];
            if (playerInfo?.Userid == userId)
                return _entities[slot + 1] as CCitadelPlayerController;
        }

        return null;
    }

    public CCitadelPlayerController? GetPlayerBySteamId(ulong steamId64)
    {
        for (var slot = 0; slot < PlayerInfos.Count; slot++)
        {
            var playerInfo = PlayerInfos[slot];
            if (playerInfo?.Steamid == steamId64)
                return _entities[slot + 1] as CCitadelPlayerController;
        }

        return null;
    }

    protected override bool ParseNetMessage(int msgType, ReadOnlySpan<byte> msgBuf)
    {
        return _gameEvents.ParseNetMessage(msgType, msgBuf)
               || _userMessageEvents.ParseUserMessage(msgType, msgBuf);
    }
}
