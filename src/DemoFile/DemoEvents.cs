using System.Buffers;
using Google.Protobuf;
using Snappier;

#pragma warning disable CS1591

namespace DemoFile;

public struct DemoEvents
{
    internal bool ReadDemoCommand(EDemoCommands msgType, ReadOnlySpan<byte> buffer, bool isCompressed)
    {
        switch (msgType)
        {
            case EDemoCommands.DemStop:
                return false;
            case EDemoCommands.DemFileHeader:
                ReadDemoCommandCore(DemoFileHeader, CDemoFileHeader.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemFileInfo:
                ReadDemoCommandCore(DemoFileInfo, CDemoFileInfo.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemSyncTick:
                ReadDemoCommandCore(DemoSyncTick, CDemoSyncTick.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemSendTables:
                ReadDemoCommandCore(DemoSendTables, CDemoSendTables.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemClassInfo:
                ReadDemoCommandCore(DemoClassInfo, CDemoClassInfo.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemStringTables:
                ReadDemoCommandCore(DemoStringTables, CDemoStringTables.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemSignonPacket:
            case EDemoCommands.DemPacket:
                ReadDemoCommandCore(DemoPacket, CDemoPacket.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemConsoleCmd:
                ReadDemoCommandCore(DemoConsoleCmd, CDemoConsoleCmd.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemCustomData:
                ReadDemoCommandCore(DemoCustomData, CDemoCustomData.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemCustomDataCallbacks:
                ReadDemoCommandCore(DemoCustomDataCallbacks, CDemoCustomDataCallbacks.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemUserCmd:
                ReadDemoCommandCore(DemoUserCmd, CDemoUserCmd.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemFullPacket:
                ReadDemoCommandCore(DemoFullPacket, CDemoFullPacket.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemSaveGame:
                ReadDemoCommandCore(DemoSaveGame, CDemoSaveGame.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemSpawnGroups:
                ReadDemoCommandCore(DemoSpawnGroups, CDemoSpawnGroups.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemAnimationData:
                ReadDemoCommandCore(DemoAnimationData, CDemoAnimationData.Parser, buffer, isCompressed);
                return true;
            case EDemoCommands.DemAnimationHeader:
                DemoAnimationHeader?.Invoke(CDemoAnimationHeader.Parser.ParseFrom(buffer));
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(msgType), msgType, null);
        }
    }

    private static void ReadDemoCommandCore<T>(Action<T>? callback, MessageParser<T> parser, ReadOnlySpan<byte> buffer, bool isCompressed)
        where T : IMessage<T>
    {
        if (callback == null)
            return;

        if (isCompressed)
        {
            var uncompressedSize = Snappy.GetUncompressedLength(buffer);
            var rented = ArrayPool<byte>.Shared.Rent(uncompressedSize);
            Snappy.Decompress(buffer, rented);
            callback(parser.ParseFrom(rented[..uncompressedSize]));
            ArrayPool<byte>.Shared.Return(rented);
        }
        else
        {
            callback(parser.ParseFrom(buffer));
        }
    }

    public Action<CDemoFileHeader>? DemoFileHeader;
    public Action<CDemoFileInfo>? DemoFileInfo;
    public Action<CDemoSyncTick>? DemoSyncTick;
    public Action<CDemoSendTables>? DemoSendTables;
    public Action<CDemoClassInfo>? DemoClassInfo;
    public Action<CDemoStringTables>? DemoStringTables;
    public Action<CDemoFullPacket>? DemoFullPacket;
    public Action<CDemoPacket>? DemoPacket;
    public Action<CDemoConsoleCmd>? DemoConsoleCmd;
    public Action<CDemoCustomData>? DemoCustomData;
    public Action<CDemoCustomDataCallbacks>? DemoCustomDataCallbacks;
    public Action<CDemoUserCmd>? DemoUserCmd;
    public Action<CDemoSaveGame>? DemoSaveGame;
    public Action<CDemoSpawnGroups>? DemoSpawnGroups;
    public Action<CDemoAnimationData>? DemoAnimationData;
    public Action<CDemoAnimationHeader>? DemoAnimationHeader;
}
