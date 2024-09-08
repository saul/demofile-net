#pragma warning disable CS1591

namespace DemoFile;

public struct CsUserMessageEvents
{
    public Action<CCSUsrMsg_VGUIMenu>? VguiMenu;
    public Action<CCSUsrMsg_Geiger>? Geiger;
    public Action<CCSUsrMsg_Train>? Train;
    public Action<CCSUsrMsg_HudText>? HudText;
    public Action<CCSUsrMsg_HudMsg>? HudMsg;
    public Action<CCSUsrMsg_ResetHud>? ResetHud;
    public Action<CCSUsrMsg_GameTitle>? GameTitle;
    public Action<CCSUsrMsg_Shake>? Shake;
    public Action<CCSUsrMsg_Fade>? Fade;
    public Action<CCSUsrMsg_Rumble>? Rumble;
    public Action<CCSUsrMsg_CloseCaption>? CloseCaption;
    public Action<CCSUsrMsg_CloseCaptionDirect>? CloseCaptionDirect;
    public Action<CCSUsrMsg_SendAudio>? SendAudio;
    public Action<CCSUsrMsg_RawAudio>? RawAudio;
    public Action<CCSUsrMsg_VoiceMask>? VoiceMask;
    public Action<CCSUsrMsg_RequestState>? RequestState;
    public Action<CCSUsrMsg_Damage>? Damage;
    public Action<CCSUsrMsg_RadioText>? RadioText;
    public Action<CCSUsrMsg_HintText>? HintText;
    public Action<CCSUsrMsg_KeyHintText>? KeyHintText;
    public Action<CCSUsrMsg_ProcessSpottedEntityUpdate>? ProcessSpottedEntityUpdate;
    public Action<CCSUsrMsg_ReloadEffect>? ReloadEffect;
    public Action<CCSUsrMsg_AdjustMoney>? AdjustMoney;
    public Action<CCSUsrMsg_StopSpectatorMode>? StopSpectatorMode;
    public Action<CCSUsrMsg_KillCam>? KillCam;
    public Action<CCSUsrMsg_DesiredTimescale>? DesiredTimescale;
    public Action<CCSUsrMsg_CurrentTimescale>? CurrentTimescale;
    public Action<CCSUsrMsg_AchievementEvent>? AchievementEvent;
    public Action<CCSUsrMsg_MatchEndConditions>? MatchEndConditions;
    public Action<CCSUsrMsg_DisconnectToLobby>? DisconnectToLobby;
    public Action<CCSUsrMsg_PlayerStatsUpdate>? PlayerStatsUpdate;
    public Action<CCSUsrMsg_WarmupHasEnded>? WarmupHasEnded;
    public Action<CCSUsrMsg_ClientInfo>? ClientInfo;
    public Action<CCSUsrMsg_XRankGet>? XRankGet;
    public Action<CCSUsrMsg_XRankUpd>? XRankUpd;
    public Action<CCSUsrMsg_CallVoteFailed>? CallVoteFailed;
    public Action<CCSUsrMsg_VoteStart>? VoteStart;
    public Action<CCSUsrMsg_VotePass>? VotePass;
    public Action<CCSUsrMsg_VoteFailed>? VoteFailed;
    public Action<CCSUsrMsg_VoteSetup>? VoteSetup;
    public Action<CCSUsrMsg_ServerRankRevealAll>? ServerRankRevealAll;
    public Action<CCSUsrMsg_SendLastKillerDamageToClient>? SendLastKillerDamageToClient;
    public Action<CCSUsrMsg_ServerRankUpdate>? ServerRankUpdate;
    public Action<CCSUsrMsg_ItemPickup>? ItemPickup;
    public Action<CCSUsrMsg_ShowMenu>? ShowMenu;
    public Action<CCSUsrMsg_BarTime>? BarTime;
    public Action<CCSUsrMsg_AmmoDenied>? AmmoDenied;
    public Action<CCSUsrMsg_MarkAchievement>? MarkAchievement;
    public Action<CCSUsrMsg_MatchStatsUpdate>? MatchStatsUpdate;
    public Action<CCSUsrMsg_ItemDrop>? ItemDrop;
    public Action<CCSUsrMsg_GlowPropTurnOff>? GlowPropTurnOff;
    public Action<CCSUsrMsg_SendPlayerItemDrops>? SendPlayerItemDrops;
    public Action<CCSUsrMsg_RoundBackupFilenames>? RoundBackupFilenames;
    public Action<CCSUsrMsg_SendPlayerItemFound>? SendPlayerItemFound;
    public Action<CCSUsrMsg_ReportHit>? ReportHit;
    public Action<CCSUsrMsg_XpUpdate>? XpUpdate;
    public Action<CCSUsrMsg_QuestProgress>? QuestProgress;
    public Action<CCSUsrMsg_ScoreLeaderboardData>? ScoreLeaderboardData;
    public Action<CCSUsrMsg_PlayerDecalDigitalSignature>? PlayerDecalDigitalSignature;
    public Action<CCSUsrMsg_WeaponSound>? WeaponSound;
    public Action<CCSUsrMsg_UpdateScreenHealthBar>? UpdateScreenHealthBar;
    public Action<CCSUsrMsg_EntityOutlineHighlight>? EntityOutlineHighlight;
    public Action<CCSUsrMsg_SSUI>? Ssui;
    public Action<CCSUsrMsg_SurvivalStats>? SurvivalStats;
    public Action<CCSUsrMsg_EndOfMatchAllPlayersData>? EndOfMatchAllPlayersData;
    public Action<CCSUsrMsg_PostRoundDamageReport>? PostRoundDamageReport;
    public Action<CCSUsrMsg_RoundEndReportData>? RoundEndReportData;
    public Action<CCSUsrMsg_CurrentRoundOdds>? CurrentRoundOdds;
    public Action<CCSUsrMsg_DeepStats>? DeepStats;
    public Action<CCSUsrMsg_ShootInfo>? ShootInfo;

    internal bool ParseUserMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int)ECstrike15UserMessages.CsUmVguimenu:
                VguiMenu?.Invoke(CCSUsrMsg_VGUIMenu.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmGeiger:
                Geiger?.Invoke(CCSUsrMsg_Geiger.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmTrain:
                Train?.Invoke(CCSUsrMsg_Train.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmHudText:
                HudText?.Invoke(CCSUsrMsg_HudText.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmHudMsg:
                HudMsg?.Invoke(CCSUsrMsg_HudMsg.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmResetHud:
                ResetHud?.Invoke(CCSUsrMsg_ResetHud.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmGameTitle:
                GameTitle?.Invoke(CCSUsrMsg_GameTitle.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmShake:
                Shake?.Invoke(CCSUsrMsg_Shake.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmFade:
                Fade?.Invoke(CCSUsrMsg_Fade.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmRumble:
                Rumble?.Invoke(CCSUsrMsg_Rumble.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmCloseCaption:
                CloseCaption?.Invoke(CCSUsrMsg_CloseCaption.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmCloseCaptionDirect:
                CloseCaptionDirect?.Invoke(CCSUsrMsg_CloseCaptionDirect.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmSendAudio:
                SendAudio?.Invoke(CCSUsrMsg_SendAudio.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmRawAudio:
                RawAudio?.Invoke(CCSUsrMsg_RawAudio.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmVoiceMask:
                VoiceMask?.Invoke(CCSUsrMsg_VoiceMask.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmRequestState:
                RequestState?.Invoke(CCSUsrMsg_RequestState.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmDamage:
                Damage?.Invoke(CCSUsrMsg_Damage.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmRadioText:
                RadioText?.Invoke(CCSUsrMsg_RadioText.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmHintText:
                HintText?.Invoke(CCSUsrMsg_HintText.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmKeyHintText:
                KeyHintText?.Invoke(CCSUsrMsg_KeyHintText.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmProcessSpottedEntityUpdate:
                ProcessSpottedEntityUpdate?.Invoke(CCSUsrMsg_ProcessSpottedEntityUpdate.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmReloadEffect:
                ReloadEffect?.Invoke(CCSUsrMsg_ReloadEffect.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmAdjustMoney:
                AdjustMoney?.Invoke(CCSUsrMsg_AdjustMoney.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmStopSpectatorMode:
                StopSpectatorMode?.Invoke(CCSUsrMsg_StopSpectatorMode.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmKillCam:
                KillCam?.Invoke(CCSUsrMsg_KillCam.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmDesiredTimescale:
                DesiredTimescale?.Invoke(CCSUsrMsg_DesiredTimescale.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmCurrentTimescale:
                CurrentTimescale?.Invoke(CCSUsrMsg_CurrentTimescale.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmAchievementEvent:
                AchievementEvent?.Invoke(CCSUsrMsg_AchievementEvent.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmMatchEndConditions:
                MatchEndConditions?.Invoke(CCSUsrMsg_MatchEndConditions.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmDisconnectToLobby:
                DisconnectToLobby?.Invoke(CCSUsrMsg_DisconnectToLobby.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmPlayerStatsUpdate:
                PlayerStatsUpdate?.Invoke(CCSUsrMsg_PlayerStatsUpdate.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmWarmupHasEnded:
                WarmupHasEnded?.Invoke(CCSUsrMsg_WarmupHasEnded.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmClientInfo:
                ClientInfo?.Invoke(CCSUsrMsg_ClientInfo.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmXrankGet:
                XRankGet?.Invoke(CCSUsrMsg_XRankGet.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmXrankUpd:
                XRankUpd?.Invoke(CCSUsrMsg_XRankUpd.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmCallVoteFailed:
                CallVoteFailed?.Invoke(CCSUsrMsg_CallVoteFailed.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmVoteStart:
                VoteStart?.Invoke(CCSUsrMsg_VoteStart.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmVotePass:
                VotePass?.Invoke(CCSUsrMsg_VotePass.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmVoteFailed:
                VoteFailed?.Invoke(CCSUsrMsg_VoteFailed.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmVoteSetup:
                VoteSetup?.Invoke(CCSUsrMsg_VoteSetup.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmServerRankRevealAll:
                ServerRankRevealAll?.Invoke(CCSUsrMsg_ServerRankRevealAll.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmSendLastKillerDamageToClient:
                SendLastKillerDamageToClient?.Invoke(CCSUsrMsg_SendLastKillerDamageToClient.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmServerRankUpdate:
                ServerRankUpdate?.Invoke(CCSUsrMsg_ServerRankUpdate.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmItemPickup:
                ItemPickup?.Invoke(CCSUsrMsg_ItemPickup.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmShowMenu:
                ShowMenu?.Invoke(CCSUsrMsg_ShowMenu.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmBarTime:
                BarTime?.Invoke(CCSUsrMsg_BarTime.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmAmmoDenied:
                AmmoDenied?.Invoke(CCSUsrMsg_AmmoDenied.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmMarkAchievement:
                MarkAchievement?.Invoke(CCSUsrMsg_MarkAchievement.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmMatchStatsUpdate:
                MatchStatsUpdate?.Invoke(CCSUsrMsg_MatchStatsUpdate.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmItemDrop:
                ItemDrop?.Invoke(CCSUsrMsg_ItemDrop.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmGlowPropTurnOff:
                GlowPropTurnOff?.Invoke(CCSUsrMsg_GlowPropTurnOff.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmSendPlayerItemDrops:
                SendPlayerItemDrops?.Invoke(CCSUsrMsg_SendPlayerItemDrops.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmRoundBackupFilenames:
                RoundBackupFilenames?.Invoke(CCSUsrMsg_RoundBackupFilenames.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmSendPlayerItemFound:
                SendPlayerItemFound?.Invoke(CCSUsrMsg_SendPlayerItemFound.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmReportHit:
                ReportHit?.Invoke(CCSUsrMsg_ReportHit.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmXpUpdate:
                XpUpdate?.Invoke(CCSUsrMsg_XpUpdate.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmQuestProgress:
                QuestProgress?.Invoke(CCSUsrMsg_QuestProgress.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmScoreLeaderboardData:
                ScoreLeaderboardData?.Invoke(CCSUsrMsg_ScoreLeaderboardData.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmPlayerDecalDigitalSignature:
                PlayerDecalDigitalSignature?.Invoke(
                    CCSUsrMsg_PlayerDecalDigitalSignature.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmWeaponSound:
                WeaponSound?.Invoke(CCSUsrMsg_WeaponSound.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmUpdateScreenHealthBar:
                UpdateScreenHealthBar?.Invoke(
                    CCSUsrMsg_UpdateScreenHealthBar.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmEntityOutlineHighlight:
                EntityOutlineHighlight?.Invoke(
                    CCSUsrMsg_EntityOutlineHighlight.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmSsui:
                Ssui?.Invoke(CCSUsrMsg_SSUI.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmSurvivalStats:
                SurvivalStats?.Invoke(CCSUsrMsg_SurvivalStats.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmEndOfMatchAllPlayersData:
                EndOfMatchAllPlayersData?.Invoke(CCSUsrMsg_EndOfMatchAllPlayersData.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmPostRoundDamageReport:
                PostRoundDamageReport?.Invoke(CCSUsrMsg_PostRoundDamageReport.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmRoundEndReportData:
                RoundEndReportData?.Invoke(CCSUsrMsg_RoundEndReportData.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmCurrentRoundOdds:
                CurrentRoundOdds?.Invoke(CCSUsrMsg_CurrentRoundOdds.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmDeepStats:
                DeepStats?.Invoke(CCSUsrMsg_DeepStats.Parser.ParseFrom(buf));
                return true;
            case (int)ECstrike15UserMessages.CsUmShootInfo:
                ShootInfo?.Invoke(CCSUsrMsg_ShootInfo.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
