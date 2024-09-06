using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DemoFile.Extensions;
using DemoFile.Sdk;

namespace DemoFile;

public readonly record struct EntityContext(
    DemoParser Demo,
    CEntityIndex EntityIndex,
    uint SerialNumber,
    ServerClass ServerClass);

public partial class DemoParser
{
    private readonly record struct EntityBaseline(
        uint ServerClassId,
        ImmutableList<byte[]>? Baselines);

    // https://github.com/dotabuff/manta/blob/master/entity.go#L186-L190
    internal const int MaxEdictBits = 14;
    internal const int MaxEdicts = 1 << MaxEdictBits;
    internal const int NumEHandleSerialNumberBits = 17;

    private readonly CEntityInstance?[] _entities = new CEntityInstance?[MaxEdicts];
    private readonly EntityBaseline[][] _entityBaselines =
    {
        new EntityBaseline[MaxEdicts],
        new EntityBaseline[MaxEdicts],
    };

    private readonly CHandle<CCSTeam>[] _teamHandles = new CHandle<CCSTeam>[4];

    private CHandle<CCSGameRulesProxy> _gameRulesHandle;
    private CHandle<CCSPlayerResource> _playerResourceHandle;

    // todo(net8): use a frozen dictionary here
    private Dictionary<SerializerKey, Serializer> _serializers = new();

    private int _serverClassBits;
    private ServerClass?[] _serverClasses = Array.Empty<ServerClass>();

    /// <summary>
    /// Maximum number of players allowed on the server.
    /// </summary>
    public int MaxPlayers { get; private set; }

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
    /// All entities in the game at this point in time.
    /// </summary>
    public IEnumerable<CEntityInstance> Entities => _entities.WhereNotNull();

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

    internal IReadOnlyList<CMsgPlayerInfo?> PlayerInfos => _playerInfos;

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

    private void OnServerInfo(CSVCMsg_ServerInfo msg)
    {
        Debug.Assert(_playerInfos[msg.PlayerSlot] != null);
        IsGotv = _playerInfos[msg.PlayerSlot]?.Ishltv ?? false;

        MaxPlayers = msg.MaxClients;
        _serverClassBits = (int)Math.Log2(msg.MaxClasses) + 1;
    }

    private T GetCachedSingletonEntity<T>(ref CHandle<T> handle, Func<T, bool> predicate)
        where T: CEntityInstance
    {
        if (GetEntityByHandle(handle) is { } entity)
            return entity;

        entity = _entities.SingleOrDefault(x => x is T entity && predicate(entity)) as T;
        if (entity == null)
        {
            throw new InvalidOperationException($"Could not find singleton entity: {typeof(T)}");
        }

        Debug.Assert(entity.IsActive);
        handle = CHandle<T>.FromIndexSerialNum(entity.EntityIndex, entity.SerialNumber);
        return entity;
    }

    private T GetCachedSingletonEntity<T>(ref CHandle<T> handle) where T : CEntityInstance =>
        GetCachedSingletonEntity(ref handle, _ => true);

    public T? GetEntityByHandle<T>(CHandle<T> handle) where T : CEntityInstance
    {
        return _entities[(int) handle.Index.Value] is T entity && entity.SerialNumber == handle.SerialNum
            ? entity
            : null;
    }

    public T? GetEntityByIndex<T>(CEntityIndex index) where T : CEntityInstance
    {
        return index.IsValid ? _entities[(int)index.Value] as T : null;
    }

    public CCSPlayerController? GetPlayerByUserId(ushort userId)
    {
        for (var slot = 0; slot < _playerInfos.Length; slot++)
        {
            var playerInfo = _playerInfos[slot];
            if (playerInfo?.Userid == userId)
                return _entities[slot + 1] as CCSPlayerController;
        }

        return null;
    }

    public CCSPlayerController? GetPlayerBySteamId(ulong steamId64)
    {
        for (var slot = 0; slot < _playerInfos.Length; slot++)
        {
            var playerInfo = _playerInfos[slot];
            if (playerInfo?.Steamid == steamId64)
                return _entities[slot + 1] as CCSPlayerController;
        }

        return null;
    }

    private void OnDemoSendTables(CDemoSendTables outerMsg)
    {
        var byteBuffer = new ByteBuffer(outerMsg.Data.Span);
        var size = byteBuffer.ReadUVarInt32();

        var msg = CSVCMsg_FlattenedSerializer.Parser.ParseFrom(byteBuffer.ReadBytes((int)size));

        var fields = msg.Fields
            .Select(field => SerializableField.FromProto(field, msg.Symbols))
            .ToArray();

        _serializers = msg.Serializers
            .Select(sz =>
            {
                var key = new SerializerKey(msg.Symbols[sz.SerializerNameSym], sz.SerializerVersion);

                var serializer = new Serializer(
                    key,
                    sz.FieldsIndex.Select(i => fields[i]).ToArray());

                return KeyValuePair.Create(key, serializer);
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private void OnDemoClassInfo(CDemoClassInfo msg)
    {
        var maxClassIds = msg.Classes.Max(x => x.ClassId) + 1;
        _serverClasses = new ServerClass[maxClassIds];

        var decoderSet = new DecoderSet(_serializers);

        foreach (var @class in msg.Classes)
        {
            if (!EntityFactories.All.TryGetValue(@class.NetworkName, out var entityFactory))
            {
                _serverClasses[@class.ClassId] = new ServerClass(
                    @class.NetworkName,
                    @class.ClassId,
                    context => throw new Exception($"Attempted to create unknown entity: {@class.NetworkName}"));

                continue;
            }

            var decoder = decoderSet.GetDecoder(@class.NetworkName);

            _serverClasses[@class.ClassId] = new ServerClass(
                @class.NetworkName,
                @class.ClassId,
                context => entityFactory(context, decoder));
        }

        Debug.Assert(_serverClasses.All(x => x != null), "Missing server classes");
    }

    private void OnPacketEntities(CSVCMsg_PacketEntities msg)
    {
        Debug.Assert(
            _serverClasses.Length > 0 && _serializers.Count > 0,
            $"{nameof(CSVCMsg_PacketEntities)} message before class/serializer info!");

        IReadOnlyDictionary<int, uint> alternateBaselines = msg.AlternateBaselines.Count == 0
            ? ImmutableDictionary<int, uint>.Empty
            : msg.AlternateBaselines.ToDictionary(x => x.EntityIndex, x => (uint)x.BaselineIndex);

        var entitiesToDelete = ArrayPool<CEntityInstance>.Shared.Rent(_entities.Length);
        var entityDeleteIdx = 0;

        // Clear out all entities - this is a full update.
        if (!msg.LegacyIsDelta)
        {
            foreach (var entity in _entities)
            {
                if (entity == null)
                    continue;

                entity.IsActive = false;
                entity.FireDeleteEvent();
                entitiesToDelete[entityDeleteIdx++] = entity;
            }

            ((Span<EntityBaseline>)_entityBaselines[0]).Clear();
            ((Span<EntityBaseline>)_entityBaselines[1]).Clear();
        }

        var entityBitBuffer = new BitBuffer(msg.EntityData.Span);
        var entityIndex = -1;

        // Fire create/post update events after all entities have been read.
        // Otherwise things like handle props may refer to entities that
        // haven't been created yet.
        var createEvents = ArrayPool<CEntityInstance>.Shared.Rent(msg.UpdatedEntries);
        var createEventIdx = 0;

        var postUpdateEvents = ArrayPool<CEntityInstance>.Shared.Rent(msg.UpdatedEntries);
        var postEventIdx = 0;

        for (var i = 0; i < msg.UpdatedEntries; ++i)
        {
            entityIndex += 1 + (int)entityBitBuffer.ReadUBitVar();

            var updateType = entityBitBuffer.ReadUBits(2);
            if ((updateType & 0b01) != 0)
            {
                Debug.Assert(msg.LegacyIsDelta, "Deletion on full update");

                // FHDR_LEAVEPVS

                // In POV demos, we can see an entity be deleted on consecutive ticks,
                // so check the entity exists first.
                if (_entities[entityIndex] is {} entity)
                {
                    if (entity.IsActive)
                    {
                        entity.IsActive = false;
                        // todo: fire event: EntityLeavePvs
                    }

                    if (updateType == 0b11)
                    {
                        // FHDR_LEAVEPVS | FHDR_DELETE
                        entity.IsActive = false;
                        entity.FireDeleteEvent();
                        entitiesToDelete[entityDeleteIdx++] = entity;
                    }
                }
            }
            else if (updateType == 0b10)
            {
                // FHDR_ENTERPVS
                Debug.Assert(!alternateBaselines.ContainsKey(entityIndex));

                var classId = entityBitBuffer.ReadUBits(_serverClassBits);
                var serialNum = entityBitBuffer.ReadUBits(NumEHandleSerialNumberBits);

                // maybe spawngroup handle?
                var _unknown = entityBitBuffer.ReadUVarInt32();

                var serverClass = _serverClasses[classId];
                Debug.Assert(serverClass != null, $"Missing server class {classId}");

                var context = new EntityContext(this, new CEntityIndex((uint) entityIndex), serialNum, serverClass);
                var entity = serverClass.EntityFactory(context);

                EntityBaseline existingEntityBaseline = default;
                byte[]? instanceBaselineBytes = null;

                // Does this entity have an alternate baseline?
                if (alternateBaselines.TryGetValue(entityIndex, out var alternateBaseline))
                {
                    instanceBaselineBytes = _instanceBaselines[_instanceBaselineLookup[new BaselineKey(classId, alternateBaseline)]].Value;
                }

                else if (msg.LegacyIsDelta
                     && _entityBaselines[msg.Baseline][entityIndex] is { ServerClassId: var baselineClassId, Baselines: not null } entityBaseline
                     && baselineClassId == classId)
                {
                    Debug.Assert(entityBaseline.Baselines != null);

                    foreach (var baseline in entityBaseline.Baselines)
                    {
                        var baselineBuf = new BitBuffer(baseline);
                        ReadNewEntity(ref baselineBuf, entity);
                    }

                    existingEntityBaseline = entityBaseline;
                }

                // Grab the server class baseline and populate the entity with it
                else if (_instanceBaselineLookup.TryGetValue(new BaselineKey(classId, 0), out var baselineIdx))
                {
                    instanceBaselineBytes = _instanceBaselines[baselineIdx].Value;
                }

                if (instanceBaselineBytes != null)
                {
                    var baselineBuf = new BitBuffer(instanceBaselineBytes);
                    ReadNewEntity(ref baselineBuf, entity);
                }

                if (msg.UpdateBaseline)
                {
                    var cloned = entityBitBuffer.Clone();
                    var prevBitsRead = entityBitBuffer.TellBits;
                    ReadNewEntity(ref entityBitBuffer, entity);
                    var bitsRead = entityBitBuffer.TellBits - prevBitsRead;

                    var newBaseline = new byte[(bitsRead + 7) / 8];
                    cloned.ReadBitsAsBytes(newBaseline, bitsRead);

                    if (instanceBaselineBytes != null)
                    {
                        existingEntityBaseline = new EntityBaseline(classId, ImmutableList.Create<byte[]>(instanceBaselineBytes));
                    }

                    _entityBaselines[msg.Baseline == 0 ? 1 : 0][entityIndex] = existingEntityBaseline == default
                        ? new EntityBaseline(classId, ImmutableList.Create<byte[]>(newBaseline))
                        : existingEntityBaseline with {Baselines = existingEntityBaseline.Baselines!.Add(newBaseline)};
                }
                else
                {
                    ReadNewEntity(ref entityBitBuffer, entity);
                }

                entity.IsActive = true;

                // If this entity already exists with the same serial number,
                // treat it as an entity update as well as entity creation.
                // This allows AddChangeCallback to track changes on snapshot.
                if (_entities[entityIndex] is { } previousEnt
                    && previousEnt.SerialNumber == entity.SerialNumber
                    && previousEnt.ServerClass.ServerClassId == entity.ServerClass.ServerClassId)
                {
                    previousEnt.FirePreUpdateEvent();
                    postUpdateEvents[postEventIdx++] = entity;
                }

                _entities[entityIndex] = entity;
                createEvents[createEventIdx++] = entity;
            }
            else
            {
                if (msg.HasPvsVisBits > 0)
                {
                    var deltaCmd = entityBitBuffer.ReadUBits(2);
                    if ((deltaCmd & 0x1) == 1)
                    {
                        continue;
                    }
                }

                // DeltaEnt
                Debug.Assert(msg.LegacyIsDelta, "Delta entity on full update");

                var entity = _entities[entityIndex] ?? throw new Exception($"Delta on non-existent entity {entityIndex}");

                if (!entity.IsActive)
                {
                    entity.IsActive = true;
                    // todo: fire event: EntityEnterPvs
                }

                entity.FirePreUpdateEvent();

                if (alternateBaselines.TryGetValue(entityIndex, out var alternateBaseline))
                {
                    var (savedBaseline, baselineBytes) = _instanceBaselines[alternateBaseline];
                    Debug.Assert(savedBaseline.ServerClassId == entity.ServerClass.ServerClassId);

                    var baselineBuf = new BitBuffer(baselineBytes);
                    ReadNewEntity(ref baselineBuf, entity);
                }

                ReadNewEntity(ref entityBitBuffer, entity);

                postUpdateEvents[postEventIdx++] = entity;
            }
        }

        for (var idx = 0; idx < entityDeleteIdx; ++idx)
        {
            var entity = entitiesToDelete[idx];
            var entityIndexToDelete = entity.EntityIndex.Value;
            if (ReferenceEquals(_entities[entityIndexToDelete], entity))
            {
                _entities[entityIndexToDelete] = null;
            }

            entitiesToDelete[idx] = null!;
        }

        for (var idx = 0; idx < createEventIdx; ++idx)
        {
            createEvents[idx].FireCreateEvent();
            createEvents[idx] = null!;
        }

        for (var idx = 0; idx < postEventIdx; ++idx)
        {
            postUpdateEvents[idx].FirePostUpdateEvent();
            postUpdateEvents[idx] = null!;
        }

        ArrayPool<CEntityInstance>.Shared.Return(entitiesToDelete);
        ArrayPool<CEntityInstance>.Shared.Return(createEvents);
        ArrayPool<CEntityInstance>.Shared.Return(postUpdateEvents);
    }

    [SkipLocalsInit]
    private static void ReadNewEntity(ref BitBuffer entityBitBuffer, CEntityInstance entity)
    {
        Span<FieldPath> fieldPaths = stackalloc FieldPath[512];

        var fp = FieldPath.Default;

        // Keep reading field paths until we reach an op with a null reader.
        // The null reader signifies `FieldPathEncodeFinish`.
        var index = 0;
        while (FieldPathEncoding.ReadFieldPathOp(ref entityBitBuffer) is { Reader: { } reader })
        {
            if (index == fieldPaths.Length)
            {
                var newArray = new FieldPath[fieldPaths.Length * 2];
                fieldPaths.CopyTo(newArray);
                fieldPaths = newArray;
            }

            reader.Invoke(ref entityBitBuffer, ref fp);
            fieldPaths[index++] = fp;
        }

        fieldPaths = fieldPaths[..index];

        for (var idx = 0; idx < fieldPaths.Length; idx++)
        {
            var fieldPath = fieldPaths[idx];
            var pathSpan = fieldPath.AsSpan();
            entity.ReadField(pathSpan, ref entityBitBuffer);
        }
    }

    private void OnNetTick(CNETMsg_Tick msg)
    {
        if (!msg.HasTick)
            return;

        var tick = msg.Tick;
        CurrentGameTick = new GameTick(tick);

        while (_serverTickTimers.TryPeek(out var timer, out var timerTick) && timerTick <= tick)
        {
            _serverTickTimers.Dequeue();
            timer.Invoke();
        }
    }
}
