namespace DemoFile;

public struct UserMessageEvents
{
    public Action<CUserMessageAchievementEvent>? UserMessageAchievementEvent { get; set; }
    public Action<CUserMessageCloseCaption>? UserMessageCloseCaption { get; set; }
    public Action<CUserMessageCloseCaptionDirect>? UserMessageCloseCaptionDirect { get; set; }
    public Action<CUserMessageCurrentTimescale>? UserMessageCurrentTimescale { get; set; }
    public Action<CUserMessageDesiredTimescale>? UserMessageDesiredTimescale { get; set; }
    public Action<CUserMessageFade>? UserMessageFade { get; set; }
    public Action<CUserMessageGameTitle>? UserMessageGameTitle { get; set; }
    public Action<CUserMessageHudMsg>? UserMessageHudMsg { get; set; }
    public Action<CUserMessageHudText>? UserMessageHudText { get; set; }
    public Action<CUserMessageColoredText>? UserMessageColoredText { get; set; }
    public Action<CUserMessageRequestState>? UserMessageRequestState { get; set; }
    public Action<CUserMessageResetHUD>? UserMessageResetHud { get; set; }
    public Action<CUserMessageRumble>? UserMessageRumble { get; set; }
    public Action<CUserMessageSayText>? UserMessageSayText { get; set; }
    public Action<CUserMessageSayText2>? UserMessageSayText2 { get; set; }
    public Action<CUserMessageSayTextChannel>? UserMessageSayTextChannel { get; set; }
    public Action<CUserMessageShake>? UserMessageShake { get; set; }
    public Action<CUserMessageShakeDir>? UserMessageShakeDir { get; set; }
    public Action<CUserMessageTextMsg>? UserMessageTextMsg { get; set; }
    public Action<CUserMessageScreenTilt>? UserMessageScreenTilt { get; set; }
    public Action<CUserMessageVoiceMask>? UserMessageVoiceMask { get; set; }
    public Action<CUserMessageSendAudio>? UserMessageSendAudio { get; set; }
    public Action<CUserMessageItemPickup>? UserMessageItemPickup { get; set; }
    public Action<CUserMessageAmmoDenied>? UserMessageAmmoDenied { get; set; }
    public Action<CUserMessageShowMenu>? UserMessageShowMenu { get; set; }
    public Action<CUserMessageCreditsMsg>? UserMessageCreditsMsg { get; set; }
    public Action<CUserMessageCloseCaptionPlaceholder>? UserMessageCloseCaptionPlaceholder { get; set; }
    public Action<CUserMessageCameraTransition>? UserMessageCameraTransition { get; set; }
    public Action<CUserMessageAudioParameter>? UserMessageAudioParameter { get; set; }
    public Action<CUserMsg_ParticleManager>? UserMessageParticleManager { get; set; }
    public Action<CUserMsg_HudError>? UserMessageHudError { get; set; }
    public Action<CUserMsg_CustomGameEvent>? UserMessageCustomGameEvent { get; set; }
    public Action<CUserMessageAnimStateGraphState>? UserMessageAnimGraphUpdate { get; set; }
    public Action<CUserMessageHapticsManagerPulse>? UserMessageHapticsManagerPulse { get; set; }
    public Action<CUserMessageHapticsManagerEffect>? UserMessageHapticsManagerEffect { get; set; }
    public Action<CUserMessageCommandQueueState>? UserMessageCommandQueueState { get; set; }
    public Action<CUserMessageUpdateCssClasses>? UserMessageUpdateCssClasses { get; set; }
    public Action<CUserMessageServerFrameTime>? UserMessageServerFrameTime { get; set; }
    public Action<CUserMessageLagCompensationError>? UserMessageLagCompensationError { get; set; }
    public Action<CUserMessageRequestDllStatus>? UserMessageRequestDllStatus { get; set; }
    public Action<CUserMessageRequestUtilAction>? UserMessageRequestUtilAction { get; set; }
    public Action<CUserMessage_UtilMsg_Response>? UserMessageUtilActionResponse { get; set; }
    public Action<CUserMessage_DllStatus>? UserMessageDllStatusResponse { get; set; }
    public Action<CUserMessageRequestInventory>? UserMessageRequestInventory { get; set; }
    public Action<CUserMessage_Inventory_Response>? UserMessageInventoryResponse { get; set; }
    public Action<CUserMessageRequestDiagnostic>? UserMessageRequestDiagnostic { get; set; }
    public Action<CUserMessage_ExtraUserData>? UserMessageExtraUserData { get; set; }

    internal bool ParseUserMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int)EBaseUserMessages.UmAchievementEvent:
                UserMessageAchievementEvent?.Invoke(CUserMessageAchievementEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCloseCaption:
                UserMessageCloseCaption?.Invoke(CUserMessageCloseCaption.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCloseCaptionDirect:
                UserMessageCloseCaptionDirect?.Invoke(CUserMessageCloseCaptionDirect.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCurrentTimescale:
                UserMessageCurrentTimescale?.Invoke(CUserMessageCurrentTimescale.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmDesiredTimescale:
                UserMessageDesiredTimescale?.Invoke(CUserMessageDesiredTimescale.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmFade:
                UserMessageFade?.Invoke(CUserMessageFade.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmGameTitle:
                UserMessageGameTitle?.Invoke(CUserMessageGameTitle.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmHudMsg:
                UserMessageHudMsg?.Invoke(CUserMessageHudMsg.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmHudText:
                UserMessageHudText?.Invoke(CUserMessageHudText.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmColoredText:
                UserMessageColoredText?.Invoke(CUserMessageColoredText.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmRequestState:
                UserMessageRequestState?.Invoke(CUserMessageRequestState.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmResetHud:
                UserMessageResetHud?.Invoke(CUserMessageResetHUD.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmRumble:
                UserMessageRumble?.Invoke(CUserMessageRumble.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmSayText:
                UserMessageSayText?.Invoke(CUserMessageSayText.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmSayText2:
                UserMessageSayText2?.Invoke(CUserMessageSayText2.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmSayTextChannel:
                UserMessageSayTextChannel?.Invoke(CUserMessageSayTextChannel.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmShake:
                UserMessageShake?.Invoke(CUserMessageShake.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmShakeDir:
                UserMessageShakeDir?.Invoke(CUserMessageShakeDir.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmTextMsg:
                UserMessageTextMsg?.Invoke(CUserMessageTextMsg.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmScreenTilt:
                UserMessageScreenTilt?.Invoke(CUserMessageScreenTilt.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmVoiceMask:
                UserMessageVoiceMask?.Invoke(CUserMessageVoiceMask.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmSendAudio:
                UserMessageSendAudio?.Invoke(CUserMessageSendAudio.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmItemPickup:
                UserMessageItemPickup?.Invoke(CUserMessageItemPickup.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmAmmoDenied:
                UserMessageAmmoDenied?.Invoke(CUserMessageAmmoDenied.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmShowMenu:
                UserMessageShowMenu?.Invoke(CUserMessageShowMenu.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCreditsMsg:
                UserMessageCreditsMsg?.Invoke(CUserMessageCreditsMsg.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCloseCaptionPlaceholder:
                UserMessageCloseCaptionPlaceholder?.Invoke(CUserMessageCloseCaptionPlaceholder.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCameraTransition:
                UserMessageCameraTransition?.Invoke(CUserMessageCameraTransition.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmAudioParameter:
                UserMessageAudioParameter?.Invoke(CUserMessageAudioParameter.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmParticleManager:
                UserMessageParticleManager?.Invoke(CUserMsg_ParticleManager.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmHudError:
                UserMessageHudError?.Invoke(CUserMsg_HudError.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCustomGameEvent:
                UserMessageCustomGameEvent?.Invoke(CUserMsg_CustomGameEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmAnimGraphUpdate:
                UserMessageAnimGraphUpdate?.Invoke(CUserMessageAnimStateGraphState.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmHapticsManagerPulse:
                UserMessageHapticsManagerPulse?.Invoke(CUserMessageHapticsManagerPulse.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmHapticsManagerEffect:
                UserMessageHapticsManagerEffect?.Invoke(CUserMessageHapticsManagerEffect.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmCommandQueueState:
                UserMessageCommandQueueState?.Invoke(CUserMessageCommandQueueState.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmUpdateCssClasses:
                UserMessageUpdateCssClasses?.Invoke(CUserMessageUpdateCssClasses.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmServerFrameTime:
                UserMessageServerFrameTime?.Invoke(CUserMessageServerFrameTime.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmLagCompensationError:
                UserMessageLagCompensationError?.Invoke(CUserMessageLagCompensationError.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmRequestDllStatus:
                UserMessageRequestDllStatus?.Invoke(CUserMessageRequestDllStatus.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmRequestUtilAction:
                UserMessageRequestUtilAction?.Invoke(CUserMessageRequestUtilAction.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmUtilActionResponse:
                UserMessageUtilActionResponse?.Invoke(CUserMessage_UtilMsg_Response.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmDllStatusResponse:
                UserMessageDllStatusResponse?.Invoke(CUserMessage_DllStatus.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmRequestInventory:
                UserMessageRequestInventory?.Invoke(CUserMessageRequestInventory.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmInventoryResponse:
                UserMessageInventoryResponse?.Invoke(CUserMessage_Inventory_Response.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmRequestDiagnostic:
                UserMessageRequestDiagnostic?.Invoke(CUserMessageRequestDiagnostic.Parser.ParseFrom(buf));
                return true;
            case (int)EBaseUserMessages.UmExtraUserData:
                UserMessageExtraUserData?.Invoke(CUserMessage_ExtraUserData.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
