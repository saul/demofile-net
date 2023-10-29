#pragma warning disable CS1591

namespace DemoFile;

public struct UserMessageEvents
{
    public Action<CUserMessageAchievementEvent>? UserMessageAchievementEvent;
    public Action<CUserMessageCloseCaption>? UserMessageCloseCaption;
    public Action<CUserMessageCloseCaptionDirect>? UserMessageCloseCaptionDirect;
    public Action<CUserMessageCurrentTimescale>? UserMessageCurrentTimescale;
    public Action<CUserMessageDesiredTimescale>? UserMessageDesiredTimescale;
    public Action<CUserMessageFade>? UserMessageFade;
    public Action<CUserMessageGameTitle>? UserMessageGameTitle;
    public Action<CUserMessageHudMsg>? UserMessageHudMsg;
    public Action<CUserMessageHudText>? UserMessageHudText;
    public Action<CUserMessageColoredText>? UserMessageColoredText;
    public Action<CUserMessageRequestState>? UserMessageRequestState;
    public Action<CUserMessageResetHUD>? UserMessageResetHud;
    public Action<CUserMessageRumble>? UserMessageRumble;
    public Action<CUserMessageSayText>? UserMessageSayText;
    public Action<CUserMessageSayText2>? UserMessageSayText2;
    public Action<CUserMessageSayTextChannel>? UserMessageSayTextChannel;
    public Action<CUserMessageShake>? UserMessageShake;
    public Action<CUserMessageShakeDir>? UserMessageShakeDir;
    public Action<CUserMessageTextMsg>? UserMessageTextMsg;
    public Action<CUserMessageScreenTilt>? UserMessageScreenTilt;
    public Action<CUserMessageVoiceMask>? UserMessageVoiceMask;
    public Action<CUserMessageSendAudio>? UserMessageSendAudio;
    public Action<CUserMessageItemPickup>? UserMessageItemPickup;
    public Action<CUserMessageAmmoDenied>? UserMessageAmmoDenied;
    public Action<CUserMessageShowMenu>? UserMessageShowMenu;
    public Action<CUserMessageCreditsMsg>? UserMessageCreditsMsg;
    public Action<CUserMessageCloseCaptionPlaceholder>? UserMessageCloseCaptionPlaceholder;
    public Action<CUserMessageCameraTransition>? UserMessageCameraTransition;
    public Action<CUserMessageAudioParameter>? UserMessageAudioParameter;
    public Action<CUserMsg_ParticleManager>? UserMessageParticleManager;
    public Action<CUserMsg_HudError>? UserMessageHudError;
    public Action<CUserMsg_CustomGameEvent>? UserMessageCustomGameEvent;
    public Action<CUserMessageAnimStateGraphState>? UserMessageAnimGraphUpdate;
    public Action<CUserMessageHapticsManagerPulse>? UserMessageHapticsManagerPulse;
    public Action<CUserMessageHapticsManagerEffect>? UserMessageHapticsManagerEffect;
    public Action<CUserMessageCommandQueueState>? UserMessageCommandQueueState;
    public Action<CUserMessageUpdateCssClasses>? UserMessageUpdateCssClasses;
    public Action<CUserMessageServerFrameTime>? UserMessageServerFrameTime;
    public Action<CUserMessageLagCompensationError>? UserMessageLagCompensationError;
    public Action<CUserMessageRequestDllStatus>? UserMessageRequestDllStatus;
    public Action<CUserMessageRequestUtilAction>? UserMessageRequestUtilAction;
    public Action<CUserMessage_UtilMsg_Response>? UserMessageUtilActionResponse;
    public Action<CUserMessage_DllStatus>? UserMessageDllStatusResponse;
    public Action<CUserMessageRequestInventory>? UserMessageRequestInventory;
    public Action<CUserMessage_Inventory_Response>? UserMessageInventoryResponse;
    public Action<CUserMessageRequestDiagnostic>? UserMessageRequestDiagnostic;
    public Action<CUserMessage_ExtraUserData>? UserMessageExtraUserData;

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
