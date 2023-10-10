#pragma warning disable CS1591

namespace DemoFile;

public struct GameEvents
{
    public Action<CMsgVDebugGameSessionIDEvent>? VDebugGameSessionIdEvent { get; set; }
    public Action<CMsgPlaceDecalEvent>? PlaceDecalEvent { get; set; }
    public Action<CMsgClearWorldDecalsEvent>? ClearWorldDecalsEvent { get; set; }
    public Action<CMsgClearEntityDecalsEvent>? ClearEntityDecalsEvent { get; set; }
    public Action<CMsgClearDecalsForSkeletonInstanceEvent>? ClearDecalsForSkeletonInstanceEvent { get; set; }
    public Action<CMsgSource1LegacyGameEventList>? Source1LegacyGameEventList { get; set; }
    public Action<CMsgSource1LegacyListenEvents>? Source1LegacyListenEvents { get; set; }
    public Action<CMsgSource1LegacyGameEvent>? Source1LegacyGameEvent { get; set; }
    public Action<CMsgSosStartSoundEvent>? SosStartSoundEvent { get; set; }
    public Action<CMsgSosStopSoundEvent>? SosStopSoundEvent { get; set; }
    public Action<CMsgSosSetSoundEventParams>? SosSetSoundEventParams { get; set; }
    public Action<CMsgSosSetLibraryStackFields>? SosSetLibraryStackFields { get; set; }
    public Action<CMsgSosStopSoundEventHash>? SosStopSoundEventHash { get; set; }

    internal bool ParseGameEvent(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int)EBaseGameEvents.GeVdebugGameSessionIdevent:
                VDebugGameSessionIdEvent?.Invoke(CMsgVDebugGameSessionIDEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GePlaceDecalEvent:
                PlaceDecalEvent?.Invoke(CMsgPlaceDecalEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeClearWorldDecalsEvent:
                ClearWorldDecalsEvent?.Invoke(CMsgClearWorldDecalsEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeClearEntityDecalsEvent:
                ClearEntityDecalsEvent?.Invoke(CMsgClearEntityDecalsEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeClearDecalsForSkeletonInstanceEvent:
                ClearDecalsForSkeletonInstanceEvent?.Invoke(CMsgClearDecalsForSkeletonInstanceEvent.Parser
                    .ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSource1LegacyGameEventList:
                Source1LegacyGameEventList?.Invoke(CMsgSource1LegacyGameEventList.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSource1LegacyListenEvents:
                Source1LegacyListenEvents?.Invoke(CMsgSource1LegacyListenEvents.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSource1LegacyGameEvent:
                Source1LegacyGameEvent?.Invoke(CMsgSource1LegacyGameEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSosStartSoundEvent:
                SosStartSoundEvent?.Invoke(CMsgSosStartSoundEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSosStopSoundEvent:
                SosStopSoundEvent?.Invoke(CMsgSosStopSoundEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSosSetSoundEventParams:
                SosSetSoundEventParams?.Invoke(CMsgSosSetSoundEventParams.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSosSetLibraryStackFields:
                SosSetLibraryStackFields?.Invoke(CMsgSosSetLibraryStackFields.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseGameEvents.GeSosStopSoundEventHash:
                SosStopSoundEventHash?.Invoke(CMsgSosStopSoundEventHash.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
