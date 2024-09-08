using DemoFile.Sdk;

namespace DemoFile;

public sealed class CsDemoParser : DemoParser<CsDemoParser>
{
    private readonly CHandle<CCSTeam, CsDemoParser>[] _teamHandles = new CHandle<CCSTeam, CsDemoParser>[4];
    private CsEntityEvents _csEntityEvents;
    private GameEvents _gameEvents;
    private UserMessageEvents _userMessageEvents;
    private CHandle<CCSGameRulesProxy, CsDemoParser> _gameRulesHandle;
    private CHandle<CCSPlayerResource, CsDemoParser> _playerResourceHandle;

    public CsDemoParser()
    {
        Source1GameEvents = new Source1GameEvents(this);
        BaseGameEvents.Source1LegacyGameEventList += Source1GameEvents.ParseSource1GameEventList;
        BaseGameEvents.Source1LegacyGameEvent += @event => Source1GameEvents.ParseSource1GameEvent(this, @event);
    }

    public Source1GameEvents Source1GameEvents { get; }

    public ref UserMessageEvents UserMessageEvents => ref _userMessageEvents;
    public ref CsEntityEvents EntityEvents => ref _csEntityEvents;
    public ref GameEvents GameEvents => ref _gameEvents;

    /// <summary>
    /// The <see cref="CCSTeam"/> entity representing the Spectators.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCSTeam TeamSpectator => GetTeam(CSTeamNumber.Spectator);

    /// <summary>
    /// The <see cref="CCSTeam"/> entity representing the Terrorists.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCSTeam TeamTerrorist => GetTeam(CSTeamNumber.Terrorist);

    /// <summary>
    /// The <see cref="CCSTeam"/> entity representing the Counter-Terrorists.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCSTeam TeamCounterTerrorist => GetTeam(CSTeamNumber.CounterTerrorist);

    public CCSPlayerResource PlayerResource => GetCachedSingletonEntity(ref _playerResourceHandle);

    /// <summary>
    /// The <see cref="CCSGameRules"/> entity representing the game rules
    /// (e.g. freeze time, current game phase)
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCSGameRules GameRules => GetCachedSingletonEntity(ref _gameRulesHandle).GameRules!;

    /// <summary>
    /// All connected players.
    /// </summary>
    public IEnumerable<CCSPlayerController> Players
    {
        get
        {
            for (var i = 1; i <= MaxPlayers; ++i)
            {
                if (_entities[i] is CCSPlayerController { Connected: PlayerConnectedState.PlayerConnected } controller)
                    yield return controller;
            }
        }
    }

    /// <summary>
    /// All players - including those that have disconnected or are reconnecting.
    /// </summary>
    public IEnumerable<CCSPlayerController> PlayersIncludingDisconnected
    {
        get
        {
            for (var i = 1; i <= MaxPlayers; ++i)
            {
                if (_entities[i] is CCSPlayerController controller)
                    yield return controller;
            }
        }
    }

    protected override IReadOnlyDictionary<string, EntityFactory<CsDemoParser>> EntityFactories =>
        CsEntityFactories.All;

    protected override ref EntityEvents<CEntityInstance<CsDemoParser>, CsDemoParser> EntityInstanceEvents => ref EntityEvents.CEntityInstance;

    protected override DecoderSet CreateDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers)
    {
        return new CsDecoderSet(serializers);
    }

    /// <summary>
    /// Get the <see cref="CCSTeam"/> entity representing a given team.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CCSTeam GetTeam(CSTeamNumber teamNumber) =>
        GetCachedSingletonEntity(
            ref _teamHandles[(int)teamNumber],
            team => team.CSTeamNum == teamNumber);

    public CCSPlayerController? GetPlayerByUserId(ushort userId)
    {
        for (var slot = 0; slot < PlayerInfos.Count; slot++)
        {
            var playerInfo = PlayerInfos[slot];
            if (playerInfo?.Userid == userId)
                return _entities[slot + 1] as CCSPlayerController;
        }

        return null;
    }

    public CCSPlayerController? GetPlayerBySteamId(ulong steamId64)
    {
        for (var slot = 0; slot < PlayerInfos.Count; slot++)
        {
            var playerInfo = PlayerInfos[slot];
            if (playerInfo?.Steamid == steamId64)
                return _entities[slot + 1] as CCSPlayerController;
        }

        return null;
    }

    protected override bool ParseNetMessage(int msgType, ReadOnlySpan<byte> msgBuf)
    {
        return _gameEvents.ParseNetMessage(msgType, msgBuf)
               || _userMessageEvents.ParseUserMessage(msgType, msgBuf);
    }
}
