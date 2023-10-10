#pragma warning disable CS1591

namespace DemoFile;

public struct PacketEvents
{
    public Action<CNETMsg_Disconnect>? NetDisconnect { get; set; }
    public Action<CNETMsg_SplitScreenUser>? NetSplitScreenUser { get; set; }
    public Action<CNETMsg_Tick>? NetTick { get; set; }
    public Action<CNETMsg_StringCmd>? NetStringCmd { get; set; }
    public Action<CNETMsg_SetConVar>? NetSetConVar { get; set; }
    public Action<CNETMsg_SignonState>? NetSignonState { get; set; }
    public Action<CNETMsg_SpawnGroup_Load>? NetSpawnGroupLoad { get; set; }
    public Action<CNETMsg_SpawnGroup_ManifestUpdate>? NetSpawnGroupManifestUpdate { get; set; }
    public Action<CNETMsg_SpawnGroup_SetCreationTick>? NetSpawnGroupSetCreationTick { get; set; }
    public Action<CNETMsg_SpawnGroup_Unload>? NetSpawnGroupUnload { get; set; }
    public Action<CNETMsg_SpawnGroup_LoadCompleted>? NetSpawnGroupLoadCompleted { get; set; }
    public Action<CNETMsg_DebugOverlay>? NetDebugOverlay { get; set; }
    public Action<CSVCMsg_ServerInfo>? SvcServerInfo { get; set; }
    public Action<CSVCMsg_FlattenedSerializer>? SvcFlattenedSerializer { get; set; }
    public Action<CSVCMsg_ClassInfo>? SvcClassInfo { get; set; }
    public Action<CSVCMsg_SetPause>? SvcSetPause { get; set; }
    public Action<CSVCMsg_CreateStringTable>? SvcCreateStringTable { get; set; }
    public Action<CSVCMsg_UpdateStringTable>? SvcUpdateStringTable { get; set; }
    public Action<CSVCMsg_VoiceInit>? SvcVoiceInit { get; set; }
    public Action<CSVCMsg_VoiceData>? SvcVoiceData { get; set; }
    public Action<CSVCMsg_Print>? SvcPrint { get; set; }
    public Action<CSVCMsg_Sounds>? SvcSounds { get; set; }
    public Action<CSVCMsg_SetView>? SvcSetView { get; set; }
    public Action<CSVCMsg_ClearAllStringTables>? SvcClearAllStringTables { get; set; }
    public Action<CSVCMsg_CmdKeyValues>? SvcCmdKeyValues { get; set; }
    public Action<CSVCMsg_BSPDecal>? SvcBspDecal { get; set; }
    public Action<CSVCMsg_SplitScreen>? SvcSplitScreen { get; set; }
    public Action<CSVCMsg_PacketEntities>? SvcPacketEntities { get; set; }
    public Action<CSVCMsg_Prefetch>? SvcPrefetch { get; set; }
    public Action<CSVCMsg_Menu>? SvcMenu { get; set; }
    public Action<CSVCMsg_GetCvarValue>? SvcGetCvarValue { get; set; }
    public Action<CSVCMsg_StopSound>? SvcStopSound { get; set; }
    public Action<CSVCMsg_PeerList>? SvcPeerList { get; set; }
    public Action<CSVCMsg_PacketReliable>? SvcPacketReliable { get; set; }
    public Action<CSVCMsg_HLTVStatus>? SvcHltvStatus { get; set; }
    public Action<CSVCMsg_ServerSteamID>? SvcServerSteamId { get; set; }
    public Action<CSVCMsg_FullFrameSplit>? SvcFullFrameSplit { get; set; }
    public Action<CSVCMsg_RconServerDetails>? SvcRconServerDetails { get; set; }
    public Action<CSVCMsg_UserMessage>? SvcUserMessage { get; set; }
    public Action<CSVCMsg_HltvReplay>? SvcHltvReplay { get; set; }
    public Action<CSVCMsg_Broadcast_Command>? SvcBroadcastCommand { get; set; }
    public Action<CSVCMsg_HltvFixupOperatorStatus>? SvcHltvFixupOperatorStatus { get; set; }

    internal bool ParseNetMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int)NET_Messages.NetDisconnect:
                NetDisconnect?.Invoke(CNETMsg_Disconnect.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSplitScreenUser:
                NetSplitScreenUser?.Invoke(CNETMsg_SplitScreenUser.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetTick:
                NetTick?.Invoke(CNETMsg_Tick.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetStringCmd:
                NetStringCmd?.Invoke(CNETMsg_StringCmd.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSetConVar:
                NetSetConVar?.Invoke(CNETMsg_SetConVar.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSignonState:
                NetSignonState?.Invoke(CNETMsg_SignonState.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSpawnGroupLoad:
                NetSpawnGroupLoad?.Invoke(CNETMsg_SpawnGroup_Load.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSpawnGroupManifestUpdate:
                NetSpawnGroupManifestUpdate?.Invoke(CNETMsg_SpawnGroup_ManifestUpdate.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSpawnGroupSetCreationTick:
                NetSpawnGroupSetCreationTick?.Invoke(CNETMsg_SpawnGroup_SetCreationTick.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSpawnGroupUnload:
                NetSpawnGroupUnload?.Invoke(CNETMsg_SpawnGroup_Unload.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetSpawnGroupLoadCompleted:
                NetSpawnGroupLoadCompleted?.Invoke(CNETMsg_SpawnGroup_LoadCompleted.Parser.ParseFrom(buf));
                return true;
            case (int)NET_Messages.NetDebugOverlay:
                NetDebugOverlay?.Invoke(CNETMsg_DebugOverlay.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcServerInfo:
                SvcServerInfo?.Invoke(CSVCMsg_ServerInfo.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcFlattenedSerializer:
                SvcFlattenedSerializer?.Invoke(CSVCMsg_FlattenedSerializer.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcClassInfo:
                SvcClassInfo?.Invoke(CSVCMsg_ClassInfo.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcSetPause:
                SvcSetPause?.Invoke(CSVCMsg_SetPause.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcCreateStringTable:
                SvcCreateStringTable?.Invoke(CSVCMsg_CreateStringTable.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcUpdateStringTable:
                SvcUpdateStringTable?.Invoke(CSVCMsg_UpdateStringTable.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcVoiceInit:
                SvcVoiceInit?.Invoke(CSVCMsg_VoiceInit.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcVoiceData:
                SvcVoiceData?.Invoke(CSVCMsg_VoiceData.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcPrint:
                SvcPrint?.Invoke(CSVCMsg_Print.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcSounds:
                SvcSounds?.Invoke(CSVCMsg_Sounds.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcSetView:
                SvcSetView?.Invoke(CSVCMsg_SetView.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcClearAllStringTables:
                SvcClearAllStringTables?.Invoke(CSVCMsg_ClearAllStringTables.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcCmdKeyValues:
                SvcCmdKeyValues?.Invoke(CSVCMsg_CmdKeyValues.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcBspdecal:
                SvcBspDecal?.Invoke(CSVCMsg_BSPDecal.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcSplitScreen:
                SvcSplitScreen?.Invoke(CSVCMsg_SplitScreen.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcPacketEntities:
                SvcPacketEntities?.Invoke(CSVCMsg_PacketEntities.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcPrefetch:
                SvcPrefetch?.Invoke(CSVCMsg_Prefetch.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcMenu:
                SvcMenu?.Invoke(CSVCMsg_Menu.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcGetCvarValue:
                SvcGetCvarValue?.Invoke(CSVCMsg_GetCvarValue.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcStopSound:
                SvcStopSound?.Invoke(CSVCMsg_StopSound.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcPeerList:
                SvcPeerList?.Invoke(CSVCMsg_PeerList.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcPacketReliable:
                SvcPacketReliable?.Invoke(CSVCMsg_PacketReliable.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcHltvstatus:
                SvcHltvStatus?.Invoke(CSVCMsg_HLTVStatus.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcServerSteamId:
                SvcServerSteamId?.Invoke(CSVCMsg_ServerSteamID.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcFullFrameSplit:
                SvcFullFrameSplit?.Invoke(CSVCMsg_FullFrameSplit.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcRconServerDetails:
                SvcRconServerDetails?.Invoke(CSVCMsg_RconServerDetails.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcUserMessage:
                SvcUserMessage?.Invoke(CSVCMsg_UserMessage.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcHltvReplay:
                SvcHltvReplay?.Invoke(CSVCMsg_HltvReplay.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcBroadcastCommand:
                SvcBroadcastCommand?.Invoke(CSVCMsg_Broadcast_Command.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcHltvFixupOperatorStatus:
                SvcHltvFixupOperatorStatus?.Invoke(CSVCMsg_HltvFixupOperatorStatus.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
