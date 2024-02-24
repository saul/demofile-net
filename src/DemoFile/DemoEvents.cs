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
                DemoFullPacket?.Invoke(CDemoFullPacket.Parser.ParseFrom(buffer));
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
}
