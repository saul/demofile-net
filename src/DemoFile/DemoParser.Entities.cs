using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DemoFile.Extensions;
using DemoFile.Sdk;

namespace DemoFile;

public partial class DemoParser<TGameParser>
{
    public readonly record struct EntityContext(
        TGameParser Demo,
        CEntityIndex EntityIndex,
        uint SerialNumber,
        ServerClass<TGameParser> ServerClass);

    private readonly record struct EntityBaseline(
        uint ServerClassId,
        ImmutableList<ReadOnlyMemory<byte>> Baselines);

    // https://github.com/dotabuff/manta/blob/master/entity.go#L186-L190
    internal const int MaxEdictBits = 14;
    internal const int MaxEdicts = 1 << MaxEdictBits;
    internal const int NumEHandleSerialNumberBits = 17;

    protected readonly CEntityInstance<TGameParser>?[] _entities = new CEntityInstance<TGameParser>?[MaxEdicts];
    private readonly EntityBaseline[][] _entityBaselines =
    {
        new EntityBaseline[MaxEdicts],
        new EntityBaseline[MaxEdicts],
    };

    // todo(net8): use a frozen dictionary here
    private Dictionary<SerializerKey, Serializer> _serializers = new();

    private int _serverClassBits;
    private ServerClass<TGameParser>?[] _serverClasses = Array.Empty<ServerClass<TGameParser>>();

    /// <summary>
    /// Maximum number of players allowed on the server.
    /// </summary>
    public int MaxPlayers { get; private set; }

    /// <summary>
    /// All entities in the game at this point in time.
    /// </summary>
    public IEnumerable<CEntityInstance<TGameParser>> Entities => _entities.WhereNotNull();

    public IReadOnlyList<CMsgPlayerInfo?> PlayerInfos => _playerInfos;

    private void OnServerInfo(CSVCMsg_ServerInfo msg)
    {
        Debug.Assert(_playerInfos[msg.PlayerSlot] != null);
        IsTvRecording = _playerInfos[msg.PlayerSlot]?.Ishltv ?? false;

        MaxPlayers = msg.MaxClients;
        _serverClassBits = (int)Math.Log2(msg.MaxClasses) + 1;
    }

    protected T GetCachedSingletonEntity<T>(ref CHandle<T, TGameParser> handle, Func<T, bool> predicate)
        where T: CEntityInstance<TGameParser>
    {
        if (GetEntityByHandle(handle) is { } entity)
            return entity;

        entity = _entities.SingleOrDefault(x => x is T entity && predicate(entity)) as T;
        if (entity == null)
        {
            throw new InvalidOperationException($"Could not find singleton entity: {typeof(T)}");
        }

        Debug.Assert(entity.IsActive);
        handle = CHandle<T, TGameParser>.FromIndexSerialNum(entity.EntityIndex, entity.SerialNumber);
        return entity;
    }

    protected T GetCachedSingletonEntity<T>(ref CHandle<T, TGameParser> handle) where T : CEntityInstance<TGameParser> =>
        GetCachedSingletonEntity(ref handle, _ => true);

    public T? GetEntityByHandle<T>(CHandle<T, TGameParser> handle) where T : CEntityInstance<TGameParser>
    {
        return _entities[(int) handle.Index.Value] is T entity && entity.SerialNumber == handle.SerialNum
            ? entity
            : null;
    }

    public T? GetEntityByIndex<T>(CEntityIndex index) where T : CEntityInstance<TGameParser>
    {
        return index.IsValid ? _entities[(int)index.Value] as T : null;
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
        _serverClasses = new ServerClass<TGameParser>[maxClassIds];

        var decoderSet = CreateDecoderSet(_serializers);

        foreach (var @class in msg.Classes)
        {
            if (!EntityFactories.TryGetValue(@class.NetworkName, out var entityFactory))
            {
                _serverClasses[@class.ClassId] = new ServerClass<TGameParser>(
                    @class.NetworkName,
                    @class.ClassId,
                    context => throw new Exception($"Attempted to create unknown entity: {@class.NetworkName}"));

                continue;
            }

            var decoder = decoderSet.GetDecoder(@class.NetworkName);

            _serverClasses[@class.ClassId] = new ServerClass<TGameParser>(
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

        var otherBaselineIdx = msg.Baseline == 0 ? 1 : 0;

        IReadOnlyDictionary<int, int> alternateBaselines = msg.AlternateBaselines.Count == 0
            ? ImmutableDictionary<int, int>.Empty
            : msg.AlternateBaselines.ToDictionary(x => x.EntityIndex, x => x.BaselineIndex);

        var entitiesToDelete = ArrayPool<CEntityInstance<TGameParser>>.Shared.Rent(_entities.Length);
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
        var createEvents = ArrayPool<CEntityInstance<TGameParser>>.Shared.Rent(msg.UpdatedEntries);
        var createEventIdx = 0;

        var postUpdateEvents = ArrayPool<CEntityInstance<TGameParser>>.Shared.Rent(msg.UpdatedEntries);
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

                var context = new EntityContext((TGameParser) this, new CEntityIndex((uint) entityIndex), serialNum, serverClass);
                var entity = serverClass.EntityFactory(context);

                EntityBaseline existingEntityBaseline = default;
                var instanceBaselineBytes = ReadOnlyMemory<byte>.Empty;

                // Entity baselines are preferred over instance baselines
                if (msg.LegacyIsDelta
                    && _entityBaselines[msg.Baseline][entityIndex] is { ServerClassId: var baselineClassId } entityBaseline
                    && baselineClassId == classId
                    && classId > 0)
                {
                    Debug.Assert(entityBaseline.Baselines != null);

                    foreach (var baseline in entityBaseline.Baselines)
                    {
                        var baselineBuf = new BitBuffer(baseline.Span);
                        ReadNewEntity(ref baselineBuf, entity);
                    }

                    existingEntityBaseline = entityBaseline;
                }

                // Grab the server class baseline and populate the entity with it
                else if (_instanceBaselineLookup.TryGetValue(new BaselineKey(classId, 0), out var baselineIndex))
                {
                    instanceBaselineBytes = _instanceBaselines[baselineIndex].Value;
                    var baselineBuf = new BitBuffer(instanceBaselineBytes.Span);
                    ReadNewEntity(ref baselineBuf, entity);
                }

                if (msg.UpdateBaseline)
                {
                    // Take a copy of just the bits that represent the delta on this entity
                    var cloned = entityBitBuffer.Clone();
                    var prevBitsRead = entityBitBuffer.TellBits;
                    ReadNewEntity(ref entityBitBuffer, entity);
                    var bitsRead = entityBitBuffer.TellBits - prevBitsRead;

                    var newBaseline = new byte[(bitsRead + 7) / 8];
                    cloned.ReadBitsAsBytes(newBaseline, bitsRead);

                    Debug.Assert(existingEntityBaseline != default || !instanceBaselineBytes.IsEmpty);

                    // Over the course of a sample 35 min POV demo, the histogram of `Baselines.Count` is:
                    // [2] = {int} 7365
                    // [3] = {int} 2177
                    // [4] = {int} 1053
                    // [5] = {int} 853
                    // [6] = {int} 394
                    // [7] = {int} 262
                    // [8] = {int} 37
                    // [9] = {int} 13
                    // [10] = {int} 15
                    // [11] = {int} 5
                    // [12] = {int} 8

                    _entityBaselines[otherBaselineIdx][entityIndex] = instanceBaselineBytes.IsEmpty
                        ? existingEntityBaseline with {Baselines = existingEntityBaseline.Baselines.Add(newBaseline)}
                        : new EntityBaseline(classId, ImmutableList.Create(instanceBaselineBytes, newBaseline));
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

                    var baselineBuf = new BitBuffer(baselineBytes.Span);
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

        ArrayPool<CEntityInstance<TGameParser>>.Shared.Return(entitiesToDelete);
        ArrayPool<CEntityInstance<TGameParser>>.Shared.Return(createEvents);
        ArrayPool<CEntityInstance<TGameParser>>.Shared.Return(postUpdateEvents);
    }

    [SkipLocalsInit]
    private static void ReadNewEntity(ref BitBuffer entityBitBuffer, CEntityInstance<TGameParser> entity)
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
