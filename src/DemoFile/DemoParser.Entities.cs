﻿using System.Buffers;
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
    // https://github.com/dotabuff/manta/blob/master/entity.go#L186-L190
    public const int MaxEdictBits = 14;
    public const int MaxEdicts = 1 << MaxEdictBits;
    public const int NumEHandleSerialNumberBits = 17;

    private readonly CEntityInstance?[] _entities = new CEntityInstance?[MaxEdicts];
    private CHandle<CCSGameRulesProxy> _gameRulesHandle;
    private CHandle<CCSPlayerResource> _playerResourceHandle;

    // todo(net8): use a frozen dictionary here
    private Dictionary<SerializerKey, Serializer> _serializers = new();

    private int _serverClassBits;
    private ServerClass?[] _serverClasses = Array.Empty<ServerClass>();
    private readonly CHandle<CCSTeam>[] _teamHandles = new CHandle<CCSTeam>[4];

    public int MaxPlayers { get; private set; }

    public CCSTeam GetTeam(CSTeamNumber teamNumber) =>
        GetCachedSingletonEntity(
            ref _teamHandles[(int)teamNumber],
            team => team.CSTeamNum == teamNumber);

    public CCSTeam TeamSpectator => GetTeam(CSTeamNumber.Spectator);
    public CCSTeam TeamTerrorist => GetTeam(CSTeamNumber.Terrorist);
    public CCSTeam TeamCounterTerrorist => GetTeam(CSTeamNumber.CounterTerrorist);
    public CCSPlayerResource PlayerResource => GetCachedSingletonEntity(ref _playerResourceHandle);
    public CCSGameRules GameRules => GetCachedSingletonEntity(ref _gameRulesHandle).GameRules!;

    public IEnumerable<CEntityInstance> Entities => _entities.WhereNotNull();

    public IEnumerable<CCSPlayerController> Players
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

    private void OnServerInfo(CSVCMsg_ServerInfo msg)
    {
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

    internal void OnDemoSendTables(CDemoSendTables outerMsg)
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

    internal void OnDemoClassInfo(CDemoClassInfo msg)
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

    internal void OnPacketEntities(CSVCMsg_PacketEntities msg)
    {
        Debug.Assert(
            _serverClasses.Length > 0 && _serializers.Count > 0,
            $"{nameof(CSVCMsg_PacketEntities)} message before class/serializer info!");

        Debug.Assert(!msg.UpdateBaseline);
        Debug.Assert(msg.AlternateBaselines.Count == 0);

        var entitiesToDelete = ArrayPool<CEntityInstance>.Shared.Rent(_entities.Length);
        var entityDeleteIdx = 0;

        // Clear out all entities - this is a full update.
        if (!msg.IsDelta)
        {
            foreach (var entity in _entities)
            {
                if (entity == null)
                    continue;

                entity.IsActive = false;
                entity.FireDeleteEvent();
                entitiesToDelete[entityDeleteIdx++] = entity;
            }
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
                Debug.Assert(msg.IsDelta, "Deletion on full update");

                // FHDR_LEAVEPVS
                var entity = _entities[entityIndex] ?? throw new Exception($"LeavePvs on non-existent entity {entityIndex}");
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
            else if (updateType == 0b10)
            {
                // FHDR_ENTERPVS

                var classId = entityBitBuffer.ReadUBits(_serverClassBits);
                var serialNum = entityBitBuffer.ReadUBits(NumEHandleSerialNumberBits);

                // maybe spawngroup handle?
                var _unknown = entityBitBuffer.ReadUVarInt32();

                var serverClass = _serverClasses[classId];
                Debug.Assert(serverClass != null, $"Missing server class {classId}");

                var context = new EntityContext(this, new CEntityIndex((uint) entityIndex), serialNum, serverClass);
                var entity = serverClass.EntityFactory(context);

                // Grab the server class baseline and populate the entity with it
                if (_instanceBaselines.TryGetValue(classId, out var baseline))
                {
                    var baselineBuf = new BitBuffer(baseline);
                    ReadNewEntity(ref baselineBuf, entity);
                }

                ReadNewEntity(ref entityBitBuffer, entity);

                entity.IsActive = true;

                // If this entity already existed with the same serial number,
                // treat it as an entity update as well as entity creation.
                // This allows AddChangeCallback to track changes on snapshot.
                if (_entities[entityIndex] is { } previousEnt
                    && previousEnt.EntityHandle == entity.EntityHandle)
                {
                    previousEnt.FirePreUpdateEvent();
                    postUpdateEvents[postEventIdx++] = entity;
                }

                _entities[entityIndex] = entity;
                createEvents[createEventIdx++] = entity;
            }
            else
            {
                // DeltaEnt
                Debug.Assert(msg.IsDelta, "Delta entity on full update");

                var entity = _entities[entityIndex] ?? throw new Exception($"Delta on non-existent entity {entityIndex}");

                if (!entity.IsActive)
                {
                    entity.IsActive = true;
                    // todo: fire event: EntityEnterPvs
                }

                entity.FirePreUpdateEvent();
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
    private void ReadNewEntity(ref BitBuffer entityBitBuffer, CEntityInstance entity)
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
