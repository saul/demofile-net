using DemoFile.Game.Dota;
using DemoFile.Sdk;

namespace DemoFile;

public sealed class DotaDemoParser : DemoParser<DotaDemoParser>
{
    private readonly CHandle<CDOTATeam, DotaDemoParser>[] _teamHandles = new CHandle<CDOTATeam, DotaDemoParser>[4];
    private EntityEvents _entityEvents;
    private CHandle<CDOTAGamerulesProxy, DotaDemoParser> _gameRulesHandle;
    private UserMessageEvents _userMessageEvents;

    public DotaDemoParser()
    {
        Source1GameEvents = new Source1GameEvents(this);
        BaseGameEvents.Source1LegacyGameEventList += Source1GameEvents.ParseSource1GameEventList;
        BaseGameEvents.Source1LegacyGameEvent += @event => Source1GameEvents.ParseSource1GameEvent(this, @event);

        PacketEvents.SvcServerInfo += e =>
        {
            var gameDirParts = e.GameDir.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            if (gameDirParts[^1] != "dota")
            {
                throw new InvalidDemoException($"Cannot use {nameof(DotaDemoParser)} to read a '{gameDirParts[^1]}' demo (expected 'dota').");
            }
        };
    }

    public Source1GameEvents Source1GameEvents { get; }

    public ref UserMessageEvents UserMessageEvents => ref _userMessageEvents;
    public ref EntityEvents EntityEvents => ref _entityEvents;

    /// <summary>
    /// The <see cref="CDOTATeam"/> entity representing the Spectators.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CDOTATeam TeamSpectator => GetTeam(TeamNumber.Spectator);

    /// <summary>
    /// The <see cref="CDOTATeam"/> entity representing the Dire.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CDOTATeam TeamDire => GetTeam(TeamNumber.Dire);

    /// <summary>
    /// The <see cref="CDOTATeam"/> entity representing the Radiant.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CDOTATeam TeamRadiant => GetTeam(TeamNumber.Radiant);

    /// <summary>
    /// The <see cref="CDOTAGameRules"/> entity representing the game rules
    /// (e.g. current game state)
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CDOTAGameRules GameRules => GetCachedSingletonEntity(ref _gameRulesHandle).GameRules!;

    /// <summary>
    /// All connected players.
    /// </summary>
    public IEnumerable<CDOTAPlayerController> Players
    {
        get
        {
            for (var i = 1; i <= MaxPlayers; ++i)
            {
                if (_entities[i] is CDOTAPlayerController { Connected: PlayerConnectedState.PlayerConnected } controller)
                    yield return controller;
            }
        }
    }

    /// <summary>
    /// All players - including those that have disconnected or are reconnecting.
    /// </summary>
    public IEnumerable<CDOTAPlayerController> PlayersIncludingDisconnected
    {
        get
        {
            for (var i = 1; i <= MaxPlayers; ++i)
            {
                if (_entities[i] is CDOTAPlayerController controller)
                    yield return controller;
            }
        }
    }

    protected override IReadOnlyDictionary<string, EntityFactory<DotaDemoParser>> EntityFactories =>
        DotaEntityFactories.All;

    protected override ref EntityEvents<CEntityInstance<DotaDemoParser>, DotaDemoParser> EntityInstanceEvents => ref EntityEvents.CEntityInstance;

    public static int TickRate => 30;

    protected override DecoderSet CreateDecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers)
    {
        return new DotaDecoderSet(serializers);
    }

    /// <summary>
    /// Get the <see cref="CDOTATeam"/> entity representing a given team.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Do not cache this value - it is unlikely to remain the same throughout the lifetime of the demo!
    /// </remarks>
    public CDOTATeam GetTeam(TeamNumber teamNumber) =>
        GetCachedSingletonEntity(
            ref _teamHandles[(int)teamNumber],
            team => team.DotaTeamNum == teamNumber);

    public CDOTAPlayerController? GetPlayerByUserId(ushort userId)
    {
        for (var slot = 0; slot < PlayerInfos.Count; slot++)
        {
            var playerInfo = PlayerInfos[slot];
            if (playerInfo?.Userid == userId)
                return _entities[slot + 1] as CDOTAPlayerController;
        }

        return null;
    }

    public CDOTAPlayerController? GetPlayerBySteamId(ulong steamId64)
    {
        for (var slot = 0; slot < PlayerInfos.Count; slot++)
        {
            var playerInfo = PlayerInfos[slot];
            if (playerInfo?.Steamid == steamId64)
                return _entities[slot + 1] as CDOTAPlayerController;
        }

        return null;
    }

    public CDOTAPlayerController? GetPlayerById(PlayerID id)
    {
        for (var slot = 1; slot <= MaxPlayers; slot++)
        {
            var controller = _entities[slot] as CDOTAPlayerController;
            if (controller?.PlayerID == id)
                return controller;
        }

        return null;
    }

    protected override bool ParseNetMessage(int msgType, ReadOnlySpan<byte> msgBuf)
    {
        return _userMessageEvents.ParseUserMessage(msgType, msgBuf);
    }
}
