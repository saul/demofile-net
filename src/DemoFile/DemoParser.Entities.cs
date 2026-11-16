using System.Buffers;
using System.Collections;
using System.Collections.Frozen;
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
        ServerClass<TGameParser> ServerClass,
        uint SpawnGroupHandle);

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

    // Entities that still exist on the client but aren't currently being transmitted (e.g. outside the PVS).
    // Maintained from CSVCMsg_PacketEntities.non_transmitted_entities (CS2 ~14030+), reset on full updates.
    private readonly BitArray _nonTransmittedEntities = new(MaxEdicts);

    // Indices of entities that have left the PVS without being deleted (may be stale; checked lazily).
    private readonly HashSet<int> _dormantEntityIndices = new();
    private readonly List<int> _dormantEntityScratch = new();

    internal FrozenDictionary<SerializerKey, Serializer> Serializers = FrozenDictionary<SerializerKey, Serializer>.Empty;

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

        // Coord/normal/angle bit widths are sent by the server since CS2 patch 14197.
        // Older demos fall back to the legacy values, as the engine does.
        var coordSizeParams = CoordSizeParams.FromProto(msg.CoordSizeParams);

        var fields = msg.Fields
            .Select(field => SerializableField.FromProto(field, msg.Symbols, coordSizeParams))
            .ToArray();

        Serializers = msg.Serializers
            .Select(sz =>
            {
                var key = new SerializerKey(msg.Symbols[sz.SerializerNameSym], sz.SerializerVersion);

                var serializer = new Serializer(
                    key,
                    sz.FieldsIndex.Select(i => fields[i]).ToArray());

                return KeyValuePair.Create(key, serializer);
            })
            .ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private void OnDemoClassInfo(CDemoClassInfo msg)
    {
        var maxClassIds = msg.Classes.Max(x => x.ClassId) + 1;
        _serverClasses = new ServerClass<TGameParser>[maxClassIds];

        var decoderSet = CreateDecoderSet(Serializers);

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
            _serverClasses.Length > 0 && Serializers.Count > 0,
            $"{nameof(CSVCMsg_PacketEntities)} message before class/serializer info!");

        var otherBaselineIdx = msg.Baseline == 0 ? 1 : 0;

        IReadOnlyDictionary<int, int> alternateBaselines = msg.AlternateBaselines.Count == 0
            ? FrozenDictionary<int, int>.Empty
            : msg.AlternateBaselines.ToDictionary(x => x.EntityIndex, x => x.BaselineIndex);

        var entitiesToDelete = ArrayPool<CEntityInstance<TGameParser>>.Shared.Rent(_entities.Length);
        var entityDeleteIdx = 0;

        if (!msg.LegacyIsDelta)
        {
            ((Span<EntityBaseline>)_entityBaselines[0]).Clear();
            ((Span<EntityBaseline>)_entityBaselines[1]).Clear();

            // The engine starts a fresh non-transmitted set when there's no frame to delta from
            _nonTransmittedEntities.SetAll(false);
            _dormantEntityIndices.Clear();
        }

        var entityBitBuffer = new BitBuffer(msg.EntityData.Span);
        var entityIndex = -1;

        // Fire create/post update events after all entities have been read.
        // Otherwise things like handle props may refer to entities that
        // haven't been created yet.
        var maxEntityEvents = msg.UpdatedEntries + (msg.OutofpvsEntityUpdates?.Count ?? 0);
        var createEvents = ArrayPool<CEntityInstance<TGameParser>>.Shared.Rent(maxEntityEvents);
        var createEventIdx = 0;

        var postUpdateEvents = ArrayPool<CEntityInstance<TGameParser>>.Shared.Rent(maxEntityEvents);
        var postEventIdx = 0;

        for (var i = 0; i < msg.UpdatedEntries; ++i)
        {
            var lastEntityIndex = entityIndex;
            entityIndex += 1 + (int)entityBitBuffer.ReadUBitVar();

            // Clear out all intermediate entities on a full update
            if (entityIndex > lastEntityIndex + 1)
            {
                DeleteEntityFullUpdateRange(_entities.AsSpan(lastEntityIndex + 1, entityIndex - lastEntityIndex - 1));
            }

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
                    else
                    {
                        // Left the PVS but still exists. Newer demos delete it at the end of this
                        // packet unless it's listed as non-transmitted (see DeleteUnreferencedDormantEntities).
                        _dormantEntityIndices.Add(entityIndex);
                    }
                }
            }
            else if (updateType == 0b10)
            {
                // FHDR_ENTERPVS
                ReadEnterPvsEntity(ref entityBitBuffer, entityIndex, outOfPvs: false);
            }
            else
            {
                // Field 16 was renamed has_pvs_vis_bits -> has_pvs_vis_bits_deprecated upstream.
                // Still set by older demos, so keep honouring it.
                if (msg.HasPvsVisBitsDeprecated > 0)
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

        DeleteEntityFullUpdateRange(_entities.AsSpan(entityIndex + 1));

        // Out-of-PVS entity updates (POV demos from servers with sv_outofpvsentityupdates, see ENGINE_PARITY.md §7.3):
        // a second stream of creates/deltas for entities outside the recipient's PVS, read after the main records.
        if (msg.OutofpvsEntityUpdates is { } outOfPvsUpdates)
        {
            var outOfPvsBuffer = new BitBuffer(outOfPvsUpdates.Data.Span);
            ReadOutOfPvsEntities(ref outOfPvsBuffer, outOfPvsUpdates.Count);

            // The engine re-points its entity reader at this stream, so anything read afterwards
            // (the trailing deletions list below) comes from the end of the out-of-PVS data.
            entityBitBuffer = outOfPvsBuffer;
        }

        // Deletions that aren't part of the entity records (engine2 ReadPacketEntities, see ENGINE_PARITY.md §7.2):
        //  - Newer demos (CS2 ~14030+) send non_transmitted_entities: entities that still exist on the client but
        //    aren't being transmitted. Any entity that has left the PVS and isn't in that set is deleted.
        //  - Older demos instead append a list of deleted entity indices to the entity data of delta packets.
        if (msg.NonTransmittedEntities is { } nonTransmitted)
        {
            ReadNonTransmittedEntities(nonTransmitted);
            DeleteUnreferencedDormantEntities();
        }
        else if (msg.LegacyIsDelta)
        {
            ReadTrailingEntityDeletions(ref entityBitBuffer);
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

        return;

        void DeleteEntityFullUpdateRange(ReadOnlySpan<CEntityInstance<TGameParser>?> entities)
        {
            // Clear out all intermediate entities on a full update
            if (msg.LegacyIsDelta)
                return;

            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;

                entity.IsActive = false;
                entity.FireDeleteEvent();
                entitiesToDelete[entityDeleteIdx++] = entity;
            }
        }

        // FHDR_ENTERPVS / out-of-PVS create (both are CopyNewEntity in the engine): class id, serial,
        // a varint (opaque; passed to entity creation), then the fields as a delta from a baseline.
        void ReadEnterPvsEntity(ref BitBuffer buffer, int entityIndex, bool outOfPvs)
        {
            Debug.Assert(!alternateBaselines.ContainsKey(entityIndex));

            var classId = buffer.ReadUBits(_serverClassBits);
            var serialNum = buffer.ReadUBits(NumEHandleSerialNumberBits);

            // The spawn group the entity is created in (0 = none)
            var spawnGroupHandle = buffer.ReadUVarInt32();

            var serverClass = _serverClasses[classId];
            Debug.Assert(serverClass != null, $"Missing server class {classId}");

            var context = new EntityContext((TGameParser) this, new CEntityIndex((uint) entityIndex), serialNum, serverClass, spawnGroupHandle);
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
                var cloned = buffer.Clone();
                var prevBitsRead = buffer.TellBits;
                ReadNewEntity(ref buffer, entity);
                var bitsRead = buffer.TellBits - prevBitsRead;

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
                ReadNewEntity(ref buffer, entity);
            }

            // Entities created via outofpvs_entity_updates exist but aren't in the PVS
            entity.IsActive = !outOfPvs;

            // If this entity already exists with the same serial number,
            // treat it as an entity update. This allows AddChangeCallback
            // to track changes on snapshot.
            var previousEnt = _entities[entityIndex];
            if (previousEnt?.SerialNumber == entity.SerialNumber
                && previousEnt.ServerClass.ServerClassId == entity.ServerClass.ServerClassId)
            {
                previousEnt.IsActive = false;
                previousEnt.FirePreUpdateEvent();
                postUpdateEvents[postEventIdx++] = entity;
            }
            else
            {
                // Delete any existing entity - the slot is being reused, but it's a new entity
                if (previousEnt != null)
                {
                    previousEnt.IsActive = false;
                    previousEnt.FireDeleteEvent();
                    entitiesToDelete[entityDeleteIdx++] = previousEnt;
                }

                createEvents[createEventIdx++] = entity;
            }

            _entities[entityIndex] = entity;

            if (outOfPvs)
                _dormantEntityIndices.Add(entityIndex);
        }

        // Each record: `UBitVar delta` (index = previous + 1 + delta, starting at -1), then 1 bit:
        // 1 = create (as FHDR_ENTERPVS), 0 = delta on an existing entity. Either way the entity is outside the
        // PVS: it isn't part of the frame, so it only survives the non-transmitted reconcile if it's listed there.
        void ReadOutOfPvsEntities(ref BitBuffer buffer, int count)
        {
            var index = -1;
            for (var j = 0; j < count; ++j)
            {
                index += 1 + (int)buffer.ReadUBitVar();
                if (index >= MaxEdicts)
                {
                    Debug.Fail($"Invalid out-of-PVS entity index {index}");
                    return;
                }

                if (buffer.ReadOneBit())
                {
                    ReadEnterPvsEntity(ref buffer, index, outOfPvs: true);
                    continue;
                }

                var entity = _entities[index] ?? throw new Exception($"Out-of-PVS delta on non-existent entity {index}");
                entity.IsActive = false;
                entity.FirePreUpdateEvent();

                if (alternateBaselines.TryGetValue(index, out var alternateBaseline))
                {
                    var (savedBaseline, baselineBytes) = _instanceBaselines[alternateBaseline];
                    Debug.Assert(savedBaseline.ServerClassId == entity.ServerClass.ServerClassId);

                    var baselineBuf = new BitBuffer(baselineBytes.Span);
                    ReadNewEntity(ref baselineBuf, entity);
                }

                ReadNewEntity(ref buffer, entity);

                postUpdateEvents[postEventIdx++] = entity;
                _dormantEntityIndices.Add(index);
            }
        }

        void DeleteEntityAtIndex(int index)
        {
            if (_entities[index] is not { } entity)
                return;

            // Already being deleted by this packet (e.g. FHDR_LEAVEPVS | FHDR_DELETE)
            for (var idx = 0; idx < entityDeleteIdx; ++idx)
            {
                if (ReferenceEquals(entitiesToDelete[idx], entity))
                    return;
            }

            entity.IsActive = false;
            entity.FireDeleteEvent();
            entitiesToDelete[entityDeleteIdx++] = entity;
            _dormantEntityIndices.Remove(index);
        }

        // Older demos: `UBitVar count`, then `count` x `UBitVar delta` (index starts at -1, each delta >= 1),
        // written by the server for entities deleted since the delta frame that weren't reported with FHDR_DELETE.
        void ReadTrailingEntityDeletions(ref BitBuffer buffer)
        {
            var count = buffer.ReadUBitVar();
            var index = -1;
            for (var j = 0; j < count; ++j)
            {
                var delta = (int)buffer.ReadUBitVar();
                if (delta == 0 || index + delta >= MaxEdicts)
                {
                    Debug.Fail($"Invalid entity deletion delta {delta} after index {index}");
                    return;
                }

                index += delta;
                DeleteEntityAtIndex(index);
            }
        }

        // Each record: `UBitVar delta` (index = previous + 1 + delta, starting at -1), then 1 bit:
        // 1 = add to the non-transmitted set, 0 = remove from it. The set persists between delta packets.
        void ReadNonTransmittedEntities(CSVCMsg_PacketEntities.Types.non_transmitted_entities_t nonTransmitted)
        {
            var buffer = new BitBuffer(nonTransmitted.Data.Span);
            var index = -1;
            for (var j = 0; j < nonTransmitted.HeaderCount; ++j)
            {
                index += 1 + (int)buffer.ReadUBitVar();
                if (index >= MaxEdicts)
                {
                    Debug.Fail($"Invalid non-transmitted entity index {index}");
                    return;
                }

                _nonTransmittedEntities[index] = buffer.ReadOneBit();
            }
        }

        // The engine deletes every client entity that is neither in this frame, being deleted, nor non-transmitted
        // ("CL:  deleting entity %d %s due to reconcile issue!"). For us, entities not in the frame are the dormant
        // ones (left the PVS without being deleted).
        void DeleteUnreferencedDormantEntities()
        {
            if (_dormantEntityIndices.Count == 0)
                return;

            _dormantEntityScratch.Clear();
            _dormantEntityScratch.AddRange(_dormantEntityIndices);

            foreach (var index in _dormantEntityScratch)
            {
                if (_entities[index] is not { IsActive: false })
                {
                    // Stale: re-entered the PVS, or the slot was emptied/reused
                    _dormantEntityIndices.Remove(index);
                    continue;
                }

                if (!_nonTransmittedEntities[index])
                {
                    DeleteEntityAtIndex(index);
                }
            }
        }
    }

    [SkipLocalsInit]
    private static void ReadNewEntity(ref BitBuffer entityBitBuffer, CEntityInstance<TGameParser> entity)
    {
        // perf: 512 covers ~100% of cases
        // benchmarking 16/32/48/64/128/512 buffer sizes has no impact on runtime, but smaller buffer size increases allocations/GC pressure significantly
        Span<FieldPath> fieldPaths = stackalloc FieldPath[512];

        var fp = FieldPath.Default;

        // perf: implementing peek on BitBuffer and build a lookup table of symbols
        // was noticeably slower than reading one bit at a time
        // | Method    | Job        | Arguments        | Mean    | Error    | StdDev   | Ratio | RatioSD | Gen0       | Gen1       | Gen2      | Allocated | Alloc Ratio |
        // |---------- |----------- |----------------- |--------:|---------:|---------:|------:|--------:|-----------:|-----------:|----------:|----------:|------------:|
        // | ParseDemo | Job-KLQGNY | /p:Baseline=true | 2.256 s | 0.0336 s | 0.0314 s |  1.00 |    0.00 | 73000.0000 | 17000.0000 | 2000.0000 | 671.65 MB |        1.00 |
        // | ParseDemo | Job-LTNMPJ | Default          | 2.387 s | 0.0267 s | 0.0236 s |  1.06 |    0.02 | 75000.0000 | 17000.0000 | 3000.0000 | 683.36 MB |        1.02 |
        var written = 0;
        while (FieldPathEncoding.ReadFieldPathOp(ref entityBitBuffer, ref fp))
        {
            if (written == fieldPaths.Length)
            {
                var newArray = new FieldPath[fieldPaths.Length * 2];
                fieldPaths.CopyTo(newArray);
                fieldPaths = newArray;
            }

            fieldPaths[written++] = fp;
        }

        fieldPaths = fieldPaths[..written];

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
