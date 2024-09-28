#pragma warning disable CS1591

namespace DemoFile.Game.Dota;

public struct UserMessageEvents
{
    //public Action<CDOTAUserMsg_AddUnitToSelection>? AddUnitToSelection;
    public Action<CDOTAUserMsg_AIDebugLine>? AIDebugLine;
    public Action<CDOTAUserMsg_ChatEvent>? ChatEvent;
    public Action<CDOTAUserMsg_CombatHeroPositions>? CombatHeroPositions;
    //public Action<CDOTAUserMsg_CombatLogData>? CombatLogData;
    public Action<CDOTAUserMsg_CombatLogBulkData>? CombatLogBulkData;
    public Action<CDOTAUserMsg_CreateLinearProjectile>? CreateLinearProjectile;
    public Action<CDOTAUserMsg_DestroyLinearProjectile>? DestroyLinearProjectile;
    public Action<CDOTAUserMsg_DodgeTrackingProjectiles>? DodgeTrackingProjectiles;
    public Action<CDOTAUserMsg_GlobalLightColor>? GlobalLightColor;
    public Action<CDOTAUserMsg_GlobalLightDirection>? GlobalLightDirection;
    public Action<CDOTAUserMsg_InvalidCommand>? InvalidCommand;
    public Action<CDOTAUserMsg_LocationPing>? LocationPing;
    public Action<CDOTAUserMsg_MapLine>? MapLine;
    public Action<CDOTAUserMsg_MiniKillCamInfo>? MiniKillCamInfo;
    public Action<CDOTAUserMsg_MinimapDebugPoint>? MinimapDebugPoint;
    public Action<CDOTAUserMsg_MinimapEvent>? MinimapEvent;
    public Action<CDOTAUserMsg_NevermoreRequiem>? NevermoreRequiem;
    public Action<CDOTAUserMsg_OverheadEvent>? OverheadEvent;
    public Action<CDOTAUserMsg_SetNextAutobuyItem>? SetNextAutobuyItem;
    public Action<CDOTAUserMsg_SharedCooldown>? SharedCooldown;
    public Action<CDOTAUserMsg_SpectatorPlayerClick>? SpectatorPlayerClick;
    public Action<CDOTAUserMsg_TutorialTipInfo>? TutorialTipInfo;
    public Action<CDOTAUserMsg_UnitEvent>? UnitEvent;
    //public Action<CDOTAUserMsg_ParticleManager>? ParticleManager;
    public Action<CDOTAUserMsg_BotChat>? BotChat;
    public Action<CDOTAUserMsg_HudError>? HudError;
    public Action<CDOTAUserMsg_ItemPurchased>? ItemPurchased;
    public Action<CDOTAUserMsg_Ping>? Ping;
    public Action<CDOTAUserMsg_ItemFound>? ItemFound;
    //public Action<CDOTAUserMsg_CharacterSpeakConcept>? CharacterSpeakConcept;
    public Action<CDOTAUserMsg_SwapVerify>? SwapVerify;
    public Action<CDOTAUserMsg_WorldLine>? WorldLine;
    //public Action<CDOTAUserMsg_TournamentDrop>? TournamentDrop;
    public Action<CDOTAUserMsg_ItemAlert>? ItemAlert;
    public Action<CDOTAUserMsg_HalloweenDrops>? HalloweenDrops;
    public Action<CDOTAUserMsg_ChatWheel>? ChatWheel;
    public Action<CDOTAUserMsg_ReceivedXmasGift>? ReceivedXmasGift;
    public Action<CDOTAUserMsg_UpdateSharedContent>? UpdateSharedContent;
    public Action<CDOTAUserMsg_TutorialRequestExp>? TutorialRequestExp;
    public Action<CDOTAUserMsg_TutorialPingMinimap>? TutorialPingMinimap;
    public Action<CDOTAUserMsg_GamerulesStateChanged>? GamerulesStateChanged;
    public Action<CDOTAUserMsg_ShowSurvey>? ShowSurvey;
    public Action<CDOTAUserMsg_TutorialFade>? TutorialFade;
    public Action<CDOTAUserMsg_AddQuestLogEntry>? AddQuestLogEntry;
    public Action<CDOTAUserMsg_SendStatPopup>? SendStatPopup;
    public Action<CDOTAUserMsg_TutorialFinish>? TutorialFinish;
    public Action<CDOTAUserMsg_SendRoshanPopup>? SendRoshanPopup;
    public Action<CDOTAUserMsg_SendGenericToolTip>? SendGenericToolTip;
    public Action<CDOTAUserMsg_SendFinalGold>? SendFinalGold;
    public Action<CDOTAUserMsg_CustomMsg>? CustomMsg;
    public Action<CDOTAUserMsg_CoachHUDPing>? CoachHUDPing;
    public Action<CDOTAUserMsg_ClientLoadGridNav>? ClientLoadGridNav;
    public Action<CDOTAUserMsg_TE_Projectile>? TE_Projectile;
    public Action<CDOTAUserMsg_TE_ProjectileLoc>? TE_ProjectileLoc;
    public Action<CDOTAUserMsg_TE_DotaBloodImpact>? TE_DotaBloodImpact;
    public Action<CDOTAUserMsg_TE_UnitAnimation>? TE_UnitAnimation;
    public Action<CDOTAUserMsg_TE_UnitAnimationEnd>? TE_UnitAnimationEnd;
    public Action<CDOTAUserMsg_AbilityPing>? AbilityPing;
    public Action<CDOTAUserMsg_ShowGenericPopup>? ShowGenericPopup;
    public Action<CDOTAUserMsg_VoteStart>? VoteStart;
    public Action<CDOTAUserMsg_VoteUpdate>? VoteUpdate;
    public Action<CDOTAUserMsg_VoteEnd>? VoteEnd;
    public Action<CDOTAUserMsg_BoosterState>? BoosterState;
    public Action<CDOTAUserMsg_WillPurchaseAlert>? WillPurchaseAlert;
    public Action<CDOTAUserMsg_TutorialMinimapPosition>? TutorialMinimapPosition;
    public Action<CDOTAUserMsg_AbilitySteal>? AbilitySteal;
    public Action<CDOTAUserMsg_CourierKilledAlert>? CourierKilledAlert;
    public Action<CDOTAUserMsg_EnemyItemAlert>? EnemyItemAlert;
    public Action<CDOTAUserMsg_StatsMatchDetails>? StatsMatchDetails;
    public Action<CDOTAUserMsg_MiniTaunt>? MiniTaunt;
    public Action<CDOTAUserMsg_BuyBackStateAlert>? BuyBackStateAlert;
    public Action<CDOTAUserMsg_SpeechBubble>? SpeechBubble;
    public Action<CDOTAUserMsg_CustomHeaderMessage>? CustomHeaderMessage;
    public Action<CDOTAUserMsg_QuickBuyAlert>? QuickBuyAlert;
    //public Action<CDOTAUserMsg_StatsHeroDetails>? StatsHeroDetails;
    //public Action<CDOTAUserMsg_PredictionResult>? PredictionResult;
    public Action<CDOTAUserMsg_ModifierAlert>? ModifierAlert;
    public Action<CDOTAUserMsg_HPManaAlert>? HPManaAlert;
    public Action<CDOTAUserMsg_GlyphAlert>? GlyphAlert;
    public Action<CDOTAUserMsg_BeastChat>? BeastChat;
    public Action<CDOTAUserMsg_SpectatorPlayerUnitOrders>? SpectatorPlayerUnitOrders;
    public Action<CDOTAUserMsg_CustomHudElement_Create>? CustomHudElement_Create;
    public Action<CDOTAUserMsg_CustomHudElement_Modify>? CustomHudElement_Modify;
    public Action<CDOTAUserMsg_CustomHudElement_Destroy>? CustomHudElement_Destroy;
    public Action<CDOTAUserMsg_CompendiumState>? CompendiumState;
    public Action<CDOTAUserMsg_ProjectionAbility>? ProjectionAbility;
    public Action<CDOTAUserMsg_ProjectionEvent>? ProjectionEvent;
    public Action<CMsgDOTACombatLogEntry>? CombatLogDataHLTV;
    public Action<CDOTAUserMsg_XPAlert>? XPAlert;
    public Action<CDOTAUserMsg_UpdateQuestProgress>? UpdateQuestProgress;
    public Action<CDOTAMatchMetadataFile>? MatchMetadata;
    public Action<CMsgDOTAMatch>? MatchDetails;
    public Action<CDOTAUserMsg_QuestStatus>? QuestStatus;
    public Action<CDOTAUserMsg_SuggestHeroPick>? SuggestHeroPick;
    public Action<CDOTAUserMsg_SuggestHeroRole>? SuggestHeroRole;
    public Action<CDOTAUserMsg_KillcamDamageTaken>? KillcamDamageTaken;
    public Action<CDOTAUserMsg_SelectPenaltyGold>? SelectPenaltyGold;
    public Action<CDOTAUserMsg_RollDiceResult>? RollDiceResult;
    public Action<CDOTAUserMsg_FlipCoinResult>? FlipCoinResult;
    public Action<CDOTAUserMessage_RequestItemSuggestions>? RequestItemSuggestions;
    public Action<CDOTAUserMessage_TeamCaptainChanged>? TeamCaptainChanged;
    public Action<CDOTAUserMsg_SendRoshanSpectatorPhase>? SendRoshanSpectatorPhase;
    public Action<CDOTAUserMsg_ChatWheelCooldown>? ChatWheelCooldown;
    public Action<CDOTAUserMsg_DismissAllStatPopups>? DismissAllStatPopups;
    public Action<CDOTAUserMsg_TE_DestroyProjectile>? TE_DestroyProjectile;
    public Action<CDOTAUserMsg_HeroRelicProgress>? HeroRelicProgress;
    public Action<CDOTAUserMsg_AbilityDraftRequestAbility>? AbilityDraftRequestAbility;
    public Action<CDOTAUserMsg_ItemSold>? ItemSold;
    public Action<CDOTAUserMsg_DamageReport>? DamageReport;
    public Action<CDOTAUserMsg_SalutePlayer>? SalutePlayer;
    public Action<CDOTAUserMsg_TipAlert>? TipAlert;
    public Action<CDOTAUserMsg_ReplaceQueryUnit>? ReplaceQueryUnit;
    public Action<CDOTAUserMsg_EmptyTeleportAlert>? EmptyTeleportAlert;
    public Action<CDOTAUserMsg_MarsArenaOfBloodAttack>? MarsArenaOfBloodAttack;
    public Action<CDOTAUserMsg_ESArcanaCombo>? ESArcanaCombo;
    public Action<CDOTAUserMsg_ESArcanaComboSummary>? ESArcanaComboSummary;
    public Action<CDOTAUserMsg_HighFiveLeftHanging>? HighFiveLeftHanging;
    public Action<CDOTAUserMsg_HighFiveCompleted>? HighFiveCompleted;
    public Action<CDOTAUserMsg_ShovelUnearth>? ShovelUnearth;
    public Action<CDOTAEntityMsg_InvokerSpellCast>? InvokerSpellCast;
    public Action<CDOTAUserMsg_RadarAlert>? RadarAlert;
    public Action<CDOTAUserMsg_AllStarEvent>? AllStarEvent;
    public Action<CDOTAUserMsg_TalentTreeAlert>? TalentTreeAlert;
    public Action<CDOTAUserMsg_QueuedOrderRemoved>? QueuedOrderRemoved;
    public Action<CDOTAUserMsg_DebugChallenge>? DebugChallenge;
    public Action<CDOTAUserMsg_OMArcanaCombo>? OMArcanaCombo;
    public Action<CDOTAUserMsg_FoundNeutralItem>? FoundNeutralItem;
    public Action<CDOTAUserMsg_OutpostCaptured>? OutpostCaptured;
    public Action<CDOTAUserMsg_OutpostGrantedXP>? OutpostGrantedXP;
    public Action<CDOTAUserMsg_MoveCameraToUnit>? MoveCameraToUnit;
    public Action<CDOTAUserMsg_PauseMinigameData>? PauseMinigameData;
    public Action<CDOTAUserMsg_VersusScene_PlayerBehavior>? VersusScene_PlayerBehavior;
    public Action<CDOTAUserMsg_QoP_ArcanaSummary>? QoP_ArcanaSummary;
    public Action<CDOTAUserMsg_HotPotato_Created>? HotPotato_Created;
    public Action<CDOTAUserMsg_HotPotato_Exploded>? HotPotato_Exploded;
    public Action<CDOTAUserMsg_WK_Arcana_Progress>? WK_Arcana_Progress;
    public Action<CDOTAUserMsg_GuildChallenge_Progress>? GuildChallenge_Progress;
    public Action<CDOTAUserMsg_WRArcanaProgress>? WRArcanaProgress;
    public Action<CDOTAUserMsg_WRArcanaSummary>? WRArcanaSummary;
    public Action<CDOTAUserMsg_EmptyItemSlotAlert>? EmptyItemSlotAlert;
    public Action<CDOTAUserMsg_AghsStatusAlert>? AghsStatusAlert;
    public Action<CDOTAUserMsg_PingConfirmation>? PingConfirmation;
    public Action<CDOTAUserMsg_MutedPlayers>? MutedPlayers;
    public Action<CDOTAUserMsg_ContextualTip>? ContextualTip;
    public Action<CDOTAUserMsg_ChatMessage>? ChatMessage;
    public Action<CDOTAUserMsg_NeutralCampAlert>? NeutralCampAlert;
    public Action<CDOTAUserMsg_RockPaperScissorsStarted>? RockPaperScissorsStarted;
    public Action<CDOTAUserMsg_RockPaperScissorsFinished>? RockPaperScissorsFinished;
    public Action<CDOTAUserMsg_DuelOpponentKilled>? DuelOpponentKilled;
    public Action<CDOTAUserMsg_DuelAccepted>? DuelAccepted;
    public Action<CDOTAUserMsg_DuelRequested>? DuelRequested;
    public Action<CDOTAUserMsg_MuertaReleaseEvent_AssignedTargetKilled>? MuertaReleaseEvent_AssignedTargetKilled;
    public Action<CDOTAUserMsg_PlayerDraftSuggestPick>? PlayerDraftSuggestPick;
    public Action<CDOTAUserMsg_PlayerDraftPick>? PlayerDraftPick;
    public Action<CDOTAUserMsg_UpdateLinearProjectileCPData>? UpdateLinearProjectileCPData;
    public Action<CDOTAUserMsg_GiftPlayer>? GiftPlayer;
    public Action<CDOTAUserMsg_FacetPing>? FacetPing;
    public Action<CDOTAUserMsg_InnatePing>? InnatePing;
    public Action<CDOTAUserMsg_RoshanTimer>? RoshanTimer;

    internal bool ParseUserMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int)EDotaUserMessages.DotaUmAddUnitToSelection:
                //AddUnitToSelection?.Invoke(CDOTAUserMsg_AddUnitToSelection.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmAidebugLine:
                AIDebugLine?.Invoke(CDOTAUserMsg_AIDebugLine.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmChatEvent:
                ChatEvent?.Invoke(CDOTAUserMsg_ChatEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCombatHeroPositions:
                CombatHeroPositions?.Invoke(CDOTAUserMsg_CombatHeroPositions.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCombatLogData:
                //CombatLogData?.Invoke(CDOTAUserMsg_CombatLogData.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCombatLogBulkData:
                CombatLogBulkData?.Invoke(CDOTAUserMsg_CombatLogBulkData.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCreateLinearProjectile:
                CreateLinearProjectile?.Invoke(CDOTAUserMsg_CreateLinearProjectile.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDestroyLinearProjectile:
                DestroyLinearProjectile?.Invoke(CDOTAUserMsg_DestroyLinearProjectile.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDodgeTrackingProjectiles:
                DodgeTrackingProjectiles?.Invoke(CDOTAUserMsg_DodgeTrackingProjectiles.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmGlobalLightColor:
                GlobalLightColor?.Invoke(CDOTAUserMsg_GlobalLightColor.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmGlobalLightDirection:
                GlobalLightDirection?.Invoke(CDOTAUserMsg_GlobalLightDirection.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmInvalidCommand:
                InvalidCommand?.Invoke(CDOTAUserMsg_InvalidCommand.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmLocationPing:
                LocationPing?.Invoke(CDOTAUserMsg_LocationPing.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMapLine:
                MapLine?.Invoke(CDOTAUserMsg_MapLine.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMiniKillCamInfo:
                MiniKillCamInfo?.Invoke(CDOTAUserMsg_MiniKillCamInfo.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMinimapDebugPoint:
                MinimapDebugPoint?.Invoke(CDOTAUserMsg_MinimapDebugPoint.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMinimapEvent:
                MinimapEvent?.Invoke(CDOTAUserMsg_MinimapEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmNevermoreRequiem:
                NevermoreRequiem?.Invoke(CDOTAUserMsg_NevermoreRequiem.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmOverheadEvent:
                OverheadEvent?.Invoke(CDOTAUserMsg_OverheadEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSetNextAutobuyItem:
                SetNextAutobuyItem?.Invoke(CDOTAUserMsg_SetNextAutobuyItem.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSharedCooldown:
                SharedCooldown?.Invoke(CDOTAUserMsg_SharedCooldown.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSpectatorPlayerClick:
                SpectatorPlayerClick?.Invoke(CDOTAUserMsg_SpectatorPlayerClick.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTutorialTipInfo:
                TutorialTipInfo?.Invoke(CDOTAUserMsg_TutorialTipInfo.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmUnitEvent:
                UnitEvent?.Invoke(CDOTAUserMsg_UnitEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmParticleManager:
                //ParticleManager?.Invoke(CDOTAUserMsg_ParticleManager.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmBotChat:
                BotChat?.Invoke(CDOTAUserMsg_BotChat.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHudError:
                HudError?.Invoke(CDOTAUserMsg_HudError.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmItemPurchased:
                ItemPurchased?.Invoke(CDOTAUserMsg_ItemPurchased.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmPing:
                Ping?.Invoke(CDOTAUserMsg_Ping.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmItemFound:
                ItemFound?.Invoke(CDOTAUserMsg_ItemFound.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCharacterSpeakConcept:
                //CharacterSpeakConcept?.Invoke(CDOTAUserMsg_CharacterSpeakConcept.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSwapVerify:
                SwapVerify?.Invoke(CDOTAUserMsg_SwapVerify.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmWorldLine:
                WorldLine?.Invoke(CDOTAUserMsg_WorldLine.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTournamentDrop:
                //TournamentDrop?.Invoke(CDOTAUserMsg_TournamentDrop.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmItemAlert:
                ItemAlert?.Invoke(CDOTAUserMsg_ItemAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHalloweenDrops:
                HalloweenDrops?.Invoke(CDOTAUserMsg_HalloweenDrops.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmChatWheel:
                ChatWheel?.Invoke(CDOTAUserMsg_ChatWheel.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmReceivedXmasGift:
                ReceivedXmasGift?.Invoke(CDOTAUserMsg_ReceivedXmasGift.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmUpdateSharedContent:
                UpdateSharedContent?.Invoke(CDOTAUserMsg_UpdateSharedContent.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTutorialRequestExp:
                TutorialRequestExp?.Invoke(CDOTAUserMsg_TutorialRequestExp.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTutorialPingMinimap:
                TutorialPingMinimap?.Invoke(CDOTAUserMsg_TutorialPingMinimap.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmGamerulesStateChanged:
                GamerulesStateChanged?.Invoke(CDOTAUserMsg_GamerulesStateChanged.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmShowSurvey:
                ShowSurvey?.Invoke(CDOTAUserMsg_ShowSurvey.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTutorialFade:
                TutorialFade?.Invoke(CDOTAUserMsg_TutorialFade.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmAddQuestLogEntry:
                AddQuestLogEntry?.Invoke(CDOTAUserMsg_AddQuestLogEntry.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSendStatPopup:
                SendStatPopup?.Invoke(CDOTAUserMsg_SendStatPopup.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTutorialFinish:
                TutorialFinish?.Invoke(CDOTAUserMsg_TutorialFinish.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSendRoshanPopup:
                SendRoshanPopup?.Invoke(CDOTAUserMsg_SendRoshanPopup.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSendGenericToolTip:
                SendGenericToolTip?.Invoke(CDOTAUserMsg_SendGenericToolTip.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSendFinalGold:
                SendFinalGold?.Invoke(CDOTAUserMsg_SendFinalGold.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCustomMsg:
                CustomMsg?.Invoke(CDOTAUserMsg_CustomMsg.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCoachHudping:
                CoachHUDPing?.Invoke(CDOTAUserMsg_CoachHUDPing.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmClientLoadGridNav:
                ClientLoadGridNav?.Invoke(CDOTAUserMsg_ClientLoadGridNav.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTeProjectile:
                TE_Projectile?.Invoke(CDOTAUserMsg_TE_Projectile.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTeProjectileLoc:
                TE_ProjectileLoc?.Invoke(CDOTAUserMsg_TE_ProjectileLoc.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTeDotaBloodImpact:
                TE_DotaBloodImpact?.Invoke(CDOTAUserMsg_TE_DotaBloodImpact.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTeUnitAnimation:
                TE_UnitAnimation?.Invoke(CDOTAUserMsg_TE_UnitAnimation.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTeUnitAnimationEnd:
                TE_UnitAnimationEnd?.Invoke(CDOTAUserMsg_TE_UnitAnimationEnd.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmAbilityPing:
                AbilityPing?.Invoke(CDOTAUserMsg_AbilityPing.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmShowGenericPopup:
                ShowGenericPopup?.Invoke(CDOTAUserMsg_ShowGenericPopup.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmVoteStart:
                VoteStart?.Invoke(CDOTAUserMsg_VoteStart.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmVoteUpdate:
                VoteUpdate?.Invoke(CDOTAUserMsg_VoteUpdate.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmVoteEnd:
                VoteEnd?.Invoke(CDOTAUserMsg_VoteEnd.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmBoosterState:
                BoosterState?.Invoke(CDOTAUserMsg_BoosterState.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmWillPurchaseAlert:
                WillPurchaseAlert?.Invoke(CDOTAUserMsg_WillPurchaseAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTutorialMinimapPosition:
                TutorialMinimapPosition?.Invoke(CDOTAUserMsg_TutorialMinimapPosition.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmAbilitySteal:
                AbilitySteal?.Invoke(CDOTAUserMsg_AbilitySteal.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCourierKilledAlert:
                CourierKilledAlert?.Invoke(CDOTAUserMsg_CourierKilledAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmEnemyItemAlert:
                EnemyItemAlert?.Invoke(CDOTAUserMsg_EnemyItemAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmStatsMatchDetails:
                StatsMatchDetails?.Invoke(CDOTAUserMsg_StatsMatchDetails.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMiniTaunt:
                MiniTaunt?.Invoke(CDOTAUserMsg_MiniTaunt.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmBuyBackStateAlert:
                BuyBackStateAlert?.Invoke(CDOTAUserMsg_BuyBackStateAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSpeechBubble:
                SpeechBubble?.Invoke(CDOTAUserMsg_SpeechBubble.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCustomHeaderMessage:
                CustomHeaderMessage?.Invoke(CDOTAUserMsg_CustomHeaderMessage.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmQuickBuyAlert:
                QuickBuyAlert?.Invoke(CDOTAUserMsg_QuickBuyAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmStatsHeroDetails:
                //StatsHeroDetails?.Invoke(CDOTAUserMsg_StatsHeroDetails.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmPredictionResult:
                //PredictionResult?.Invoke(CDOTAUserMsg_PredictionResult.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmModifierAlert:
                ModifierAlert?.Invoke(CDOTAUserMsg_ModifierAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHpmanaAlert:
                HPManaAlert?.Invoke(CDOTAUserMsg_HPManaAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmGlyphAlert:
                GlyphAlert?.Invoke(CDOTAUserMsg_GlyphAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmBeastChat:
                BeastChat?.Invoke(CDOTAUserMsg_BeastChat.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSpectatorPlayerUnitOrders:
                SpectatorPlayerUnitOrders?.Invoke(CDOTAUserMsg_SpectatorPlayerUnitOrders.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCustomHudElementCreate:
                CustomHudElement_Create?.Invoke(CDOTAUserMsg_CustomHudElement_Create.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCustomHudElementModify:
                CustomHudElement_Modify?.Invoke(CDOTAUserMsg_CustomHudElement_Modify.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCustomHudElementDestroy:
                CustomHudElement_Destroy?.Invoke(CDOTAUserMsg_CustomHudElement_Destroy.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCompendiumState:
                CompendiumState?.Invoke(CDOTAUserMsg_CompendiumState.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmProjectionAbility:
                ProjectionAbility?.Invoke(CDOTAUserMsg_ProjectionAbility.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmProjectionEvent:
                ProjectionEvent?.Invoke(CDOTAUserMsg_ProjectionEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmCombatLogDataHltv:
                CombatLogDataHLTV?.Invoke(CMsgDOTACombatLogEntry.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmXpalert:
                XPAlert?.Invoke(CDOTAUserMsg_XPAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmUpdateQuestProgress:
                UpdateQuestProgress?.Invoke(CDOTAUserMsg_UpdateQuestProgress.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMatchMetadata:
                MatchMetadata?.Invoke(CDOTAMatchMetadataFile.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMatchDetails:
                MatchDetails?.Invoke(CMsgDOTAMatch.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmQuestStatus:
                QuestStatus?.Invoke(CDOTAUserMsg_QuestStatus.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSuggestHeroPick:
                SuggestHeroPick?.Invoke(CDOTAUserMsg_SuggestHeroPick.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSuggestHeroRole:
                SuggestHeroRole?.Invoke(CDOTAUserMsg_SuggestHeroRole.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmKillcamDamageTaken:
                KillcamDamageTaken?.Invoke(CDOTAUserMsg_KillcamDamageTaken.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSelectPenaltyGold:
                SelectPenaltyGold?.Invoke(CDOTAUserMsg_SelectPenaltyGold.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmRollDiceResult:
                RollDiceResult?.Invoke(CDOTAUserMsg_RollDiceResult.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmFlipCoinResult:
                FlipCoinResult?.Invoke(CDOTAUserMsg_FlipCoinResult.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmRequestItemSuggestions:
                RequestItemSuggestions?.Invoke(CDOTAUserMessage_RequestItemSuggestions.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTeamCaptainChanged:
                TeamCaptainChanged?.Invoke(CDOTAUserMessage_TeamCaptainChanged.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSendRoshanSpectatorPhase:
                SendRoshanSpectatorPhase?.Invoke(CDOTAUserMsg_SendRoshanSpectatorPhase.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmChatWheelCooldown:
                ChatWheelCooldown?.Invoke(CDOTAUserMsg_ChatWheelCooldown.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDismissAllStatPopups:
                DismissAllStatPopups?.Invoke(CDOTAUserMsg_DismissAllStatPopups.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTeDestroyProjectile:
                TE_DestroyProjectile?.Invoke(CDOTAUserMsg_TE_DestroyProjectile.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHeroRelicProgress:
                HeroRelicProgress?.Invoke(CDOTAUserMsg_HeroRelicProgress.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmAbilityDraftRequestAbility:
                AbilityDraftRequestAbility?.Invoke(CDOTAUserMsg_AbilityDraftRequestAbility.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmItemSold:
                ItemSold?.Invoke(CDOTAUserMsg_ItemSold.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDamageReport:
                DamageReport?.Invoke(CDOTAUserMsg_DamageReport.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmSalutePlayer:
                SalutePlayer?.Invoke(CDOTAUserMsg_SalutePlayer.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTipAlert:
                TipAlert?.Invoke(CDOTAUserMsg_TipAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmReplaceQueryUnit:
                ReplaceQueryUnit?.Invoke(CDOTAUserMsg_ReplaceQueryUnit.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmEmptyTeleportAlert:
                EmptyTeleportAlert?.Invoke(CDOTAUserMsg_EmptyTeleportAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMarsArenaOfBloodAttack:
                MarsArenaOfBloodAttack?.Invoke(CDOTAUserMsg_MarsArenaOfBloodAttack.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmEsarcanaCombo:
                ESArcanaCombo?.Invoke(CDOTAUserMsg_ESArcanaCombo.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmEsarcanaComboSummary:
                ESArcanaComboSummary?.Invoke(CDOTAUserMsg_ESArcanaComboSummary.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHighFiveLeftHanging:
                HighFiveLeftHanging?.Invoke(CDOTAUserMsg_HighFiveLeftHanging.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHighFiveCompleted:
                HighFiveCompleted?.Invoke(CDOTAUserMsg_HighFiveCompleted.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmShovelUnearth:
                ShovelUnearth?.Invoke(CDOTAUserMsg_ShovelUnearth.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaEmInvokerSpellCast:
                InvokerSpellCast?.Invoke(CDOTAEntityMsg_InvokerSpellCast.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmRadarAlert:
                RadarAlert?.Invoke(CDOTAUserMsg_RadarAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmAllStarEvent:
                AllStarEvent?.Invoke(CDOTAUserMsg_AllStarEvent.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmTalentTreeAlert:
                TalentTreeAlert?.Invoke(CDOTAUserMsg_TalentTreeAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmQueuedOrderRemoved:
                QueuedOrderRemoved?.Invoke(CDOTAUserMsg_QueuedOrderRemoved.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDebugChallenge:
                DebugChallenge?.Invoke(CDOTAUserMsg_DebugChallenge.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmOmarcanaCombo:
                OMArcanaCombo?.Invoke(CDOTAUserMsg_OMArcanaCombo.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmFoundNeutralItem:
                FoundNeutralItem?.Invoke(CDOTAUserMsg_FoundNeutralItem.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmOutpostCaptured:
                OutpostCaptured?.Invoke(CDOTAUserMsg_OutpostCaptured.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmOutpostGrantedXp:
                OutpostGrantedXP?.Invoke(CDOTAUserMsg_OutpostGrantedXP.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMoveCameraToUnit:
                MoveCameraToUnit?.Invoke(CDOTAUserMsg_MoveCameraToUnit.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmPauseMinigameData:
                PauseMinigameData?.Invoke(CDOTAUserMsg_PauseMinigameData.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmVersusScenePlayerBehavior:
                VersusScene_PlayerBehavior?.Invoke(CDOTAUserMsg_VersusScene_PlayerBehavior.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmQoPArcanaSummary:
                QoP_ArcanaSummary?.Invoke(CDOTAUserMsg_QoP_ArcanaSummary.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHotPotatoCreated:
                HotPotato_Created?.Invoke(CDOTAUserMsg_HotPotato_Created.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmHotPotatoExploded:
                HotPotato_Exploded?.Invoke(CDOTAUserMsg_HotPotato_Exploded.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmWkArcanaProgress:
                WK_Arcana_Progress?.Invoke(CDOTAUserMsg_WK_Arcana_Progress.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmGuildChallengeProgress:
                GuildChallenge_Progress?.Invoke(CDOTAUserMsg_GuildChallenge_Progress.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmWrarcanaProgress:
                WRArcanaProgress?.Invoke(CDOTAUserMsg_WRArcanaProgress.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmWrarcanaSummary:
                WRArcanaSummary?.Invoke(CDOTAUserMsg_WRArcanaSummary.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmEmptyItemSlotAlert:
                EmptyItemSlotAlert?.Invoke(CDOTAUserMsg_EmptyItemSlotAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmAghsStatusAlert:
                AghsStatusAlert?.Invoke(CDOTAUserMsg_AghsStatusAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmPingConfirmation:
                PingConfirmation?.Invoke(CDOTAUserMsg_PingConfirmation.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMutedPlayers:
                MutedPlayers?.Invoke(CDOTAUserMsg_MutedPlayers.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmContextualTip:
                ContextualTip?.Invoke(CDOTAUserMsg_ContextualTip.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmChatMessage:
                ChatMessage?.Invoke(CDOTAUserMsg_ChatMessage.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmNeutralCampAlert:
                NeutralCampAlert?.Invoke(CDOTAUserMsg_NeutralCampAlert.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmRockPaperScissorsStarted:
                RockPaperScissorsStarted?.Invoke(CDOTAUserMsg_RockPaperScissorsStarted.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmRockPaperScissorsFinished:
                RockPaperScissorsFinished?.Invoke(CDOTAUserMsg_RockPaperScissorsFinished.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDuelOpponentKilled:
                DuelOpponentKilled?.Invoke(CDOTAUserMsg_DuelOpponentKilled.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDuelAccepted:
                DuelAccepted?.Invoke(CDOTAUserMsg_DuelAccepted.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmDuelRequested:
                DuelRequested?.Invoke(CDOTAUserMsg_DuelRequested.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmMuertaReleaseEventAssignedTargetKilled:
                MuertaReleaseEvent_AssignedTargetKilled?.Invoke(CDOTAUserMsg_MuertaReleaseEvent_AssignedTargetKilled.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmPlayerDraftSuggestPick:
                PlayerDraftSuggestPick?.Invoke(CDOTAUserMsg_PlayerDraftSuggestPick.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmPlayerDraftPick:
                PlayerDraftPick?.Invoke(CDOTAUserMsg_PlayerDraftPick.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmUpdateLinearProjectileCpdata:
                UpdateLinearProjectileCPData?.Invoke(CDOTAUserMsg_UpdateLinearProjectileCPData.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmGiftPlayer:
                GiftPlayer?.Invoke(CDOTAUserMsg_GiftPlayer.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmFacetPing:
                FacetPing?.Invoke(CDOTAUserMsg_FacetPing.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmInnatePing:
                InnatePing?.Invoke(CDOTAUserMsg_InnatePing.Parser.ParseFrom(buf));
                return true;
            case (int)EDotaUserMessages.DotaUmRoshanTimer:
                RoshanTimer?.Invoke(CDOTAUserMsg_RoshanTimer.Parser.ParseFrom(buf));
                return true;
        }

        return false;
    }
}
