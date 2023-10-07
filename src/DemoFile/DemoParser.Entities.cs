using System.Diagnostics;
using System.Runtime.CompilerServices;
using DemoFile.Sdk;

namespace DemoFile;

public readonly record struct EntityContext(
    DemoParser Demo,
    uint SerialNumber,
    ServerClass ServerClass);

public partial class DemoParser
{
    // https://github.com/dotabuff/manta/blob/master/entity.go#L186-L190
    private const int MAX_EDICT_BITS = 14;
    private const int MAX_EDICTS = 1 << MAX_EDICT_BITS;
    private const int NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS = 17;

    // todo(net8): use a frozen dictionary here
    private Dictionary<SerializerKey, Serializer> _serializers = new();

    private int _serverClassBits;
    private ServerClass?[] _serverClasses = Array.Empty<ServerClass>();

    public int MaxPlayers { get; private set; }

    private readonly CEntityInstance?[] _entities = new CEntityInstance?[MAX_EDICTS];

    private readonly Dictionary<Type, CEntityInstance> _singletons = new();

    private void OnServerInfo(CSVCMsg_ServerInfo msg)
    {
        MaxPlayers = msg.MaxClients;
        _serverClassBits = (int)Math.Log2(msg.MaxClasses) + 1;
    }

    public T GetSingletonEntity<T>(Func<T, bool> predicate)
        where T: CEntityInstance
    {
        if (_singletons.TryGetValue(typeof(T), out var entityInstance))
        {
            return (T)entityInstance;
        }

        entityInstance = _entities.SingleOrDefault(x => x is T entity && predicate(entity));
        if (entityInstance == null)
        {
            throw new InvalidOperationException($"Could not find singleton entity: {typeof(T)}");
        }

        _singletons[typeof(T)] = entityInstance;
        return (T)entityInstance;
    }

    public T GetSingletonEntity<T>() where T : CEntityInstance =>
        GetSingletonEntity<T>(_ => true);

    public CCSTeam TeamTerrorist => GetSingletonEntity<CCSTeam>(team => team.CSTeamNumber == CSTeamNumber.Terrorist);
    public CCSTeam TeamCounterTerrorist => GetSingletonEntity<CCSTeam>(team => team.CSTeamNumber == CSTeamNumber.CounterTerrorist);
    public CCSPlayerResource PlayerResource => GetSingletonEntity<CCSPlayerResource>();
    public CCSGameRules GameRules => GetSingletonEntity<CCSGameRulesProxy>().m_pGameRules!;

    public IEnumerable<CEntityInstance> Entities => _entities.Where(x => x != null)!;

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

    public T? GetEntityByHandle<T>(CHandle<T> handle) where T : CEntityInstance
    {
        var index = handle.Value & (MAX_EDICTS - 1);
        var serialNum = handle.Value >> MAX_EDICT_BITS;

        return _entities[index] is T entity && entity.SerialNumber == serialNum
            ? entity
            : null;
    }

    public T? GetEntityByIndex<T>(CEntityIndex index) where T : CEntityInstance
    {
        return _entities[(int)index.Value] as T;
    }

    internal void OnDemoSendTables(CDemoSendTables msg)
    {
        var byteBuffer = new ByteBuffer(msg.Data.Span);
        var size = byteBuffer.ReadUVarInt32();

        var flattenedSerializer = CSVCMsg_FlattenedSerializer.Parser.ParseFrom(byteBuffer.ReadBytes((int)size));

        // todo: abstract this into its own method
        var fieldFactories = flattenedSerializer.Fields.Select(
                Func<SerializerKey, SerializableField> (field) =>
                {
                    var varName = flattenedSerializer.Symbols[field.VarNameSym];
                    var varType = flattenedSerializer.Symbols[field.VarTypeSym];
                    var sendNode = flattenedSerializer.Symbols[field.SendNodeSym].Split('.', StringSplitOptions.RemoveEmptyEntries);
                    var varEncoder = field.HasVarEncoderSym
                        ? flattenedSerializer.Symbols[field.VarEncoderSym]
                        : null;
                    var fieldSerializerKey = field.HasFieldSerializerNameSym
                        ? new SerializerKey(
                            flattenedSerializer.Symbols[field.FieldSerializerNameSym],
                            field.FieldSerializerVersion)
                        : default(SerializerKey?);
                    var encodingInfo = new FieldEncodingInfo(
                        VarEncoder: varEncoder,
                        BitCount: field.BitCount,
                        EncodeFlags: field.EncodeFlags,
                        LowValue: field.HasLowValue ? field.LowValue : default(float?),
                        HighValue: field.HasHighValue ? field.HighValue : default(float?));
                    var fieldType = FieldType.Parse(varType);

                    return key => new SerializableField(
                        key,
                        varName,
                        fieldType,
                        sendNode,
                        encodingInfo,
                        fieldSerializerKey
                    );
                })
            .ToArray();

        _serializers =
            flattenedSerializer.Serializers.Select(
                    sz =>
                    {
                        var key = new SerializerKey(
                            flattenedSerializer.Symbols[sz.SerializerNameSym],
                            sz.SerializerVersion);

                        var serializer = new Serializer(
                            key,
                            sz.FieldsIndex.Select(i => fieldFactories[i](key)).ToArray());

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
                    context => throw new Exception($"Unknown server class in demo: {@class.NetworkName}"));

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

        Debug.Assert(msg.AlternateBaselines.Count == 0);

        var entityBitBuffer = new BitBuffer(msg.EntityData.Span);
        var entityIndex = -1;

        for (var i = 0; i < msg.UpdatedEntries; ++i)
        {
            entityIndex += 1 + (int)entityBitBuffer.ReadUBitVar();

            var updateType = entityBitBuffer.ReadUBits(2);
            if ((updateType & 0b01) != 0)
            {
                Debug.Assert(msg.IsDelta, "Deletion on full update");

                // entity leaving PVS

                if (updateType == 0b11)
                {
                    // FHDR_LEAVEPVS | FHDR_DELETE
                    _entities[entityIndex] = null;
                }
                else
                {
                    // FHDR_LEAVEPVS
                    var entity = _entities[entityIndex] ?? throw new Exception($"LeavePvs on non-existent entity {entityIndex}");
                    if (entity.IsActive)
                    {
                        entity.IsActive = false;
                        // todo: fire event: EntityLeavePvs
                    }
                }
            }
            else if (updateType == 0b10)
            {
                // FHDR_ENTERPVS

                var classId = entityBitBuffer.ReadUBits(_serverClassBits);
                var serialNum = entityBitBuffer.ReadUBits(NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS);

                var _unknown = entityBitBuffer.ReadUVarInt32();

                var serverClass = _serverClasses[classId];
                Debug.Assert(serverClass != null, $"Missing server class {classId}");

                var context = new EntityContext(this, serialNum, serverClass);
                var entity = serverClass.EntityFactory(context);

                // Grab the server class baseline and populate the entity with it
                if (_instanceBaselines.TryGetValue(classId, out var baseline))
                {
                    var baselineBuf = new BitBuffer(baseline);
                    ReadNewEntity(ref baselineBuf, entity);
                }

                ReadNewEntity(ref entityBitBuffer, entity);

                entity.IsActive = true;
                _entities[entityIndex] = entity;
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

                ReadNewEntity(ref entityBitBuffer, entity);
            }
        }
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

        foreach (var fieldPath in fieldPaths)
        {
            var pathSpan = fieldPath.AsSpan();
            entity.ReadField(pathSpan, ref entityBitBuffer);
        }
    }
}
