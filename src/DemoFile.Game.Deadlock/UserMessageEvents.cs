#pragma warning disable CS1591

namespace DemoFile.Game.Deadlock;

public struct UserMessageEvents
{
    public Action<CCitadelUserMessage_Damage>? Damage;
    public Action<CCitadelUserMsg_MapPing>? MapPing;
    public Action<CCitadelUserMsg_TeamRewards>? TeamRewards;
    public Action<CCitadelUserMsg_TriggerDamageFlash>? TriggerDamageFlash;
    public Action<CCitadelUserMsg_AbilitiesChanged>? AbilitiesChanged;
    public Action<CCitadelUserMsg_RecentDamageSummary>? RecentDamageSummary;
    public Action<CCitadelUserMsg_SpectatorTeamChanged>? SpectatorTeamChanged;
    public Action<CCitadelUserMsg_ChatWheel>? ChatWheel;
    public Action<CCitadelUserMsg_GoldHistory>? GoldHistory;
    public Action<CCitadelUserMsg_ChatMsg>? ChatMsg;
    public Action<CCitadelUserMsg_QuickResponse>? QuickResponse;
    public Action<CCitadelUserMsg_PostMatchDetails>? PostMatchDetails;
    public Action<CCitadelUserMsg_ChatEvent>? ChatEvent;
    public Action<CCitadelUserMsg_AbilityInterrupted>? AbilityInterrupted;
    public Action<CCitadelUserMsg_HeroKilled>? HeroKilled;
    public Action<CCitadelUserMsg_ReturnIdol>? ReturnIdol;
    public Action<CCitadelUserMsg_SetClientCameraAngles>? SetClientCameraAngles;
    public Action<CCitadelUserMsg_MapLine>? MapLine;
    public Action<CCitadelUserMessage_BulletHit>? BulletHit;
    public Action<CCitadelUserMessage_ObjectiveMask>? ObjectiveMask;
    public Action<CCitadelUserMessage_ModifierApplied>? ModifierApplied;
    public Action<CCitadelUserMsg_CameraController>? CameraController;
    public Action<CCitadelUserMessage_AuraModifierApplied>? AuraModifierApplied;
    public Action<CCitadelUserMsg_ObstructedShotFired>? ObstructedShotFired;
    public Action<CCitadelUserMsg_AbilityLateFailure>? AbilityLateFailure;
    public Action<CCitadelUserMsg_AbilityPing>? AbilityPing;
    public Action<CCitadelUserMsg_PostProcessingAnim>? PostProcessingAnim;
    public Action<CCitadelUserMsg_DeathReplayData>? DeathReplayData;
    public Action<CCitadelUserMsg_PlayerLifetimeStatInfo>? PlayerLifetimeStatInfo;
    public Action<CCitadelUserMsg_ForceShopClosed>? ForceShopClosed;
    public Action<CCitadelUserMsg_StaminaDrained>? StaminaDrained;
    public Action<CCitadelUserMessage_AbilityNotify>? AbilityNotify;
    public Action<CCitadelUserMsg_GetDamageStatsResponse>? GetDamageStatsResponse;
    public Action<CCitadelUserMsg_ParticipantStartSoundEvent>? ParticipantStartSoundEvent;
    public Action<CCitadelUserMsg_ParticipantStopSoundEvent>? ParticipantStopSoundEvent;
    public Action<CCitadelUserMsg_ParticipantStopSoundEventHash>? ParticipantStopSoundEventHash;
    public Action<CCitadelUserMsg_ParticipantSetSoundEventParams>? ParticipantSetSoundEventParams;
    public Action<CCitadelUserMsg_ParticipantSetLibraryStackFields>? ParticipantSetLibraryStackFields;
    public Action<CCitadelUserMessage_CurrencyChanged>? CurrencyChanged;
    public Action<CCitadelUserMessage_GameOver>? GameOver;
    public Action<CCitadelUserMsg_BossKilled>? BossKilled;

    internal bool ParseUserMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int)CitadelUserMessageIds.KEuserMsgDamage:
                Damage?.Invoke(CCitadelUserMessage_Damage.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgMapPing:
                MapPing?.Invoke(CCitadelUserMsg_MapPing.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgTeamRewards:
                TeamRewards?.Invoke(CCitadelUserMsg_TeamRewards.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgTriggerDamageFlash:
                TriggerDamageFlash?.Invoke(
                    CCitadelUserMsg_TriggerDamageFlash.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgAbilitiesChanged:
                AbilitiesChanged?.Invoke(CCitadelUserMsg_AbilitiesChanged.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgRecentDamageSummary:
                RecentDamageSummary?.Invoke(
                    CCitadelUserMsg_RecentDamageSummary.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgSpectatorTeamChanged:
                SpectatorTeamChanged?.Invoke(
                    CCitadelUserMsg_SpectatorTeamChanged.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgChatWheel:
                ChatWheel?.Invoke(CCitadelUserMsg_ChatWheel.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgGoldHistory:
                GoldHistory?.Invoke(CCitadelUserMsg_GoldHistory.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgChatMsg:
                ChatMsg?.Invoke(CCitadelUserMsg_ChatMsg.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgQuickResponse:
                QuickResponse?.Invoke(CCitadelUserMsg_QuickResponse.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgPostMatchDetails:
                PostMatchDetails?.Invoke(CCitadelUserMsg_PostMatchDetails.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgChatEvent:
                ChatEvent?.Invoke(CCitadelUserMsg_ChatEvent.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgAbilityInterrupted:
                AbilityInterrupted?.Invoke(
                    CCitadelUserMsg_AbilityInterrupted.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgHeroKilled:
                HeroKilled?.Invoke(CCitadelUserMsg_HeroKilled.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgReturnIdol:
                ReturnIdol?.Invoke(CCitadelUserMsg_ReturnIdol.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgSetClientCameraAngles:
                SetClientCameraAngles?.Invoke(
                    CCitadelUserMsg_SetClientCameraAngles.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgMapLine:
                MapLine?.Invoke(CCitadelUserMsg_MapLine.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgBulletHit:
                BulletHit?.Invoke(CCitadelUserMessage_BulletHit.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgObjectiveMask:
                ObjectiveMask?.Invoke(CCitadelUserMessage_ObjectiveMask.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgModifierApplied:
                ModifierApplied?.Invoke(CCitadelUserMessage_ModifierApplied.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgCameraController:
                CameraController?.Invoke(CCitadelUserMsg_CameraController.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgAuraModifierApplied:
                AuraModifierApplied?.Invoke(
                    CCitadelUserMessage_AuraModifierApplied.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgObstructedShotFired:
                ObstructedShotFired?.Invoke(
                    CCitadelUserMsg_ObstructedShotFired.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgAbilityLateFailure:
                AbilityLateFailure?.Invoke(
                    CCitadelUserMsg_AbilityLateFailure.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgAbilityPing:
                AbilityPing?.Invoke(CCitadelUserMsg_AbilityPing.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgPostProcessingAnim:
                PostProcessingAnim?.Invoke(
                    CCitadelUserMsg_PostProcessingAnim.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgDeathReplayData:
                DeathReplayData?.Invoke(CCitadelUserMsg_DeathReplayData.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgPlayerLifetimeStatInfo:
                PlayerLifetimeStatInfo?.Invoke(
                    CCitadelUserMsg_PlayerLifetimeStatInfo.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgForceShopClosed:
                ForceShopClosed?.Invoke(CCitadelUserMsg_ForceShopClosed.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgStaminaDrained:
                StaminaDrained?.Invoke(CCitadelUserMsg_StaminaDrained.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgAbilityNotify:
                AbilityNotify?.Invoke(CCitadelUserMessage_AbilityNotify.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgGetDamageStatsResponse:
                GetDamageStatsResponse?.Invoke(
                    CCitadelUserMsg_GetDamageStatsResponse.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgParticipantStartSoundEvent:
                ParticipantStartSoundEvent?.Invoke(
                    CCitadelUserMsg_ParticipantStartSoundEvent.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgParticipantStopSoundEvent:
                ParticipantStopSoundEvent?.Invoke(
                    CCitadelUserMsg_ParticipantStopSoundEvent.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgParticipantStopSoundEventHash:
                ParticipantStopSoundEventHash?.Invoke(
                    CCitadelUserMsg_ParticipantStopSoundEventHash.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgParticipantSetSoundEventParams:
                ParticipantSetSoundEventParams?.Invoke(
                    CCitadelUserMsg_ParticipantSetSoundEventParams.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgParticipantSetLibraryStackFields:
                ParticipantSetLibraryStackFields?.Invoke(
                    CCitadelUserMsg_ParticipantSetLibraryStackFields.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgCurrencyChanged:
                CurrencyChanged?.Invoke(CCitadelUserMessage_CurrencyChanged.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgGameOver:
                GameOver?.Invoke(CCitadelUserMessage_GameOver.Parser.ParseFrom(buf));
                return true;
            case (int)CitadelUserMessageIds.KEuserMsgBossKilled:
                BossKilled?.Invoke(CCitadelUserMsg_BossKilled.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
