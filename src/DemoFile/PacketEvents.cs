#pragma warning disable CS1591

namespace DemoFile;

public struct PacketEvents
{
    public Action<CNETMsg_SplitScreenUser>? NetSplitScreenUser;
    public Action<CNETMsg_Tick>? NetTick;
    public Action<CNETMsg_StringCmd>? NetStringCmd;
    public Action<CNETMsg_SetConVar>? NetSetConVar;
    public Action<CNETMsg_SignonState>? NetSignonState;
    public Action<CNETMsg_SpawnGroup_Load>? NetSpawnGroupLoad;
    public Action<CNETMsg_SpawnGroup_ManifestUpdate>? NetSpawnGroupManifestUpdate;
    public Action<CNETMsg_SpawnGroup_SetCreationTick>? NetSpawnGroupSetCreationTick;
    public Action<CNETMsg_SpawnGroup_Unload>? NetSpawnGroupUnload;
    public Action<CNETMsg_SpawnGroup_LoadCompleted>? NetSpawnGroupLoadCompleted;
    public Action<CNETMsg_DebugOverlay>? NetDebugOverlay;
    public Action<CSVCMsg_ServerInfo>? SvcServerInfo;
    public Action<CSVCMsg_FlattenedSerializer>? SvcFlattenedSerializer;
    public Action<CSVCMsg_ClassInfo>? SvcClassInfo;
    public Action<CSVCMsg_SetPause>? SvcSetPause;
    public Action<CSVCMsg_CreateStringTable>? SvcCreateStringTable;
    public Action<CSVCMsg_UpdateStringTable>? SvcUpdateStringTable;
    public Action<CSVCMsg_VoiceInit>? SvcVoiceInit;
    public Action<CSVCMsg_VoiceData>? SvcVoiceData;
    public Action<CSVCMsg_Print>? SvcPrint;
    public Action<CSVCMsg_Sounds>? SvcSounds;
    public Action<CSVCMsg_SetView>? SvcSetView;
    public Action<CSVCMsg_ClearAllStringTables>? SvcClearAllStringTables;
    public Action<CSVCMsg_CmdKeyValues>? SvcCmdKeyValues;
    public Action<CSVCMsg_BSPDecal>? SvcBspDecal;
    public Action<CSVCMsg_SplitScreen>? SvcSplitScreen;
    public Action<CSVCMsg_PacketEntities>? SvcPacketEntities;
    public Action<CSVCMsg_Prefetch>? SvcPrefetch;
    public Action<CSVCMsg_Menu>? SvcMenu;
    public Action<CSVCMsg_GetCvarValue>? SvcGetCvarValue;
    public Action<CSVCMsg_StopSound>? SvcStopSound;
    public Action<CSVCMsg_PeerList>? SvcPeerList;
    public Action<CSVCMsg_PacketReliable>? SvcPacketReliable;
    public Action<CSVCMsg_HLTVStatus>? SvcHltvStatus;
    public Action<CSVCMsg_ServerSteamID>? SvcServerSteamId;
    public Action<CSVCMsg_FullFrameSplit>? SvcFullFrameSplit;
    public Action<CSVCMsg_RconServerDetails>? SvcRconServerDetails;
    public Action<CSVCMsg_UserMessage>? SvcUserMessage;
    public Action<CSVCMsg_HltvReplay>? SvcHltvReplay;
    public Action<CSVCMsg_Broadcast_Command>? SvcBroadcastCommand;
    public Action<CSVCMsg_HltvFixupOperatorStatus>? SvcHltvFixupOperatorStatus;
    public Action<CSVCMsg_UserCommands>? SvcUserCmds;

    internal bool ParseNetMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
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
            /*
            case (int)SVC_Messages.SvcFlattenedSerializer:
                SvcFlattenedSerializer?.Invoke(CSVCMsg_FlattenedSerializer.Parser.ParseFrom(buf));
                return true;
            */
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
            /*
            case (int)SVC_Messages.SvcSounds:
                SvcSounds?.Invoke(CSVCMsg_Sounds.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcSetView:
                SvcSetView?.Invoke(CSVCMsg_SetView.Parser.ParseFrom(buf));
                return true;
            */
            case (int)SVC_Messages.SvcClearAllStringTables:
                SvcClearAllStringTables?.Invoke(CSVCMsg_ClearAllStringTables.Parser.ParseFrom(buf));
                return true;
            /*
            case (int)SVC_Messages.SvcCmdKeyValues:
                SvcCmdKeyValues?.Invoke(CSVCMsg_CmdKeyValues.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcBspdecal:
                SvcBspDecal?.Invoke(CSVCMsg_BSPDecal.Parser.ParseFrom(buf));
                return true;
            case (int)SVC_Messages.SvcSplitScreen:
                SvcSplitScreen?.Invoke(CSVCMsg_SplitScreen.Parser.ParseFrom(buf));
                return true;
            */
            case (int)SVC_Messages.SvcPacketEntities:
                SvcPacketEntities?.Invoke(CSVCMsg_PacketEntities.Parser.ParseFrom(buf));
                return true;
            /*
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
            */
            case (int)SVC_Messages.SvcHltvstatus:
                SvcHltvStatus?.Invoke(CSVCMsg_HLTVStatus.Parser.ParseFrom(buf));
                return true;
            /*
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
            */
            case (int)SVC_Messages.SvcUserCmds:
                SvcUserCmds?.Invoke(CSVCMsg_UserCommands.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
