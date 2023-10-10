#pragma warning disable CS1591

namespace DemoFile;

public struct DemoEvents
{
    internal bool ReadDemoCommand(EDemoCommands msgType, ReadOnlySpan<byte> buffer)
    {
        switch (msgType)
        {
            case EDemoCommands.DemStop:
                return false;
            case EDemoCommands.DemFileHeader:
                DemoFileHeader?.Invoke(CDemoFileHeader.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemFileInfo:
                DemoFileInfo?.Invoke(CDemoFileInfo.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemSyncTick:
                DemoSyncTick?.Invoke(CDemoSyncTick.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemSendTables:
                DemoSendTables?.Invoke(CDemoSendTables.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemClassInfo:
                DemoClassInfo?.Invoke(CDemoClassInfo.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemStringTables:
                DemoStringTables?.Invoke(CDemoStringTables.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemSignonPacket:
            case EDemoCommands.DemPacket:
                DemoPacket?.Invoke(CDemoPacket.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemConsoleCmd:
                DemoConsoleCmd?.Invoke(CDemoConsoleCmd.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemCustomData:
                DemoCustomData?.Invoke(CDemoCustomData.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemCustomDataCallbacks:
                DemoCustomDataCallbacks?.Invoke(CDemoCustomDataCallbacks.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemUserCmd:
                DemoUserCmd?.Invoke(CDemoUserCmd.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemFullPacket:
                var fullPacket = CDemoFullPacket.Parser.ParseFrom(buffer);
                DemoStringTables?.Invoke(fullPacket.StringTable);
                DemoPacket?.Invoke(fullPacket.Packet);
                return true;
            case EDemoCommands.DemSaveGame:
                DemoSaveGame?.Invoke(CDemoSaveGame.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemSpawnGroups:
                DemoSpawnGroups?.Invoke(CDemoSpawnGroups.Parser.ParseFrom(buffer));
                return true;
            case EDemoCommands.DemAnimationData:
                DemoAnimationData?.Invoke(CDemoAnimationData.Parser.ParseFrom(buffer));
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(msgType), msgType, null);
        }
    }

    public Action<CDemoFileHeader>? DemoFileHeader { get; set; }
    public Action<CDemoFileInfo>? DemoFileInfo { get; set; }
    public Action<CDemoSyncTick>? DemoSyncTick { get; set; }
    public Action<CDemoSendTables>? DemoSendTables { get; set; }
    public Action<CDemoClassInfo>? DemoClassInfo { get; set; }
    public Action<CDemoStringTables>? DemoStringTables { get; set; }
    public Action<CDemoPacket>? DemoPacket { get; set; }
    public Action<CDemoConsoleCmd>? DemoConsoleCmd { get; set; }
    public Action<CDemoCustomData>? DemoCustomData { get; set; }
    public Action<CDemoCustomDataCallbacks>? DemoCustomDataCallbacks { get; set; }
    public Action<CDemoUserCmd>? DemoUserCmd { get; set; }
    public Action<CDemoSaveGame>? DemoSaveGame { get; set; }
    public Action<CDemoSpawnGroups>? DemoSpawnGroups { get; set; }
    public Action<CDemoAnimationData>? DemoAnimationData { get; set; }
}
