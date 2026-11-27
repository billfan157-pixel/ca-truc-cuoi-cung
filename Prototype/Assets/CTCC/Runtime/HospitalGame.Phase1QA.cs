using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        /// <summary>
        /// CH01 Rebirth III — Phase 1 Hardened Verification Suite (Core v3.6.2 / DEC-V36-CH01-01 / DEC-V361-CH01-01 / DEC-VO-01).
        /// Verifies:
        /// 1. VAL-V3-01 full Dialogue.json runtime contract (143 rows, 14 barks, 12/12 CUT_TO_JOURNAL pocketNoteText,
        ///    0 stageDirection leaks, 0 forbidden non-VO WAVs loaded, all 17 valid CH01 VO clips loaded).
        /// 2. VO-SILENT-Q01 WCAG AA silent thought styling (#F4F1E8, scrim #101414 0.56/0.80, duration scales 1.0x/1.35x/1.6x,
        ///    0 AudioBus touches) + REAL fault-injection probe (v36-vo-leak bypasses INNER_SUB_SILENT guard into SpeakInternal).
        /// 3. V36-CH01-Q01 two independent mandatory B01 routes + Hardening 4.1/4.2/4.3/4.4/4.5:
        ///    - 4.2 Negative bypass tests: InspectE10aLayerAsync & AcquireItemAsync reject E10a/keys pre-pickup in CH01.
        ///    - 4.3 Service-level Casework.OpenGate validation (5 cases) + REAL fault-injection probe (v36-t02-wrong-key).
        ///    - 4.4 Two-way CH01-D01 lit-lobby sight guard (look-first vs read-first-facing-away; never gates T02).
        ///    - 4.5 Two-source signedAppointment epistemic pair (mailbox note + 16/11 SMS, in both orders + save/reload).
        /// 4. D3 Rev 1.1: 6 synthetic + 3 REAL historical prior-build Save v7 files (CH01-CP01, CH04-CP04, CH05-CP03).
        /// 5. Ch01JournalPolicy Phase 1 boundaries across B01-B03, B06-B08, AN01, and CH02+ preservation.
        /// 6. D4: Exterior East Gate Pillar mailbox clearance, reach, East-hinged outward pivot, and frontOnly facing.
        /// 7. Real GateService.G08 predicate (false in CH01-CH05, true only with full CH08 finalization state).
        /// </summary>
        private IEnumerator VerifyCh01Rebirth3Phase1Suite()
        {
            string probe = Argument("-ctcc-qa-probe");
            var savedBefore = State.Copy();
            var savedPos = player.Feet;

            // --- 1) VAL-V3-01: Full Dialogue.json v3.6.2 runtime contract & DEC-VO-01 clip hygiene ---
            bool contractOk = ValidateDialogueRuntimeContract(throwOnError: true, out string contractReport);
            Require(contractOk, "VAL-V3-01 dialogue contract must pass: " + contractReport);

            // Explicitly assert ALL forbidden non-VO WAVs are excluded from build voiceClips
            string[] forbiddenNonVoIds =
            {
                "CH01-D01", "CH01-D02", "CH01-D03b", "CH01-D07b", "CH01-D10", "CH01-D11", "CH01-D12",
                "CH01-D14", "CH01-D17", "CH01-D18", "CH01-D19", "CH02-D02", "CH02-D08", "CH03-D01", "CH03-D02", "CH03-D04"
            };
            foreach (string fid in forbiddenNonVoIds)
                Require(!voice.HasLoadedClipRaw(fid), "DEC-VO-01 forbidden non-VO clip must not be loaded in VoiceDirector: " + fid);

            // Explicitly assert all 17 valid CH01 VO clips (Lan, Minh dialogue with Lan, PA, Bảy/C01) ARE loaded
            string[] validCh01VoIds =
            {
                "CH01-D03", "CH01-D03c", "CH01-D04", "CH01-D04c", "CH01-D05", "CH01-D05b", "CH01-D05c",
                "CH01-D06", "CH01-D07", "CH01-D08", "CH01-D09", "CH01-D13", "CH01-D16",
                "CH01-PA01", "CH01-PA02", "CH01-PA03", "CH01-PA04"
            };
            foreach (string vid in validCh01VoIds)
                Require(voice.HasLoadedClipRaw(vid), "DEC-VO-01 valid CH01 VO clip must be loaded: " + vid);

            // --- 2) VO-SILENT-Q01: WCAG AA silent thought styling, contrast toggle, duration scale & REAL v36-vo-leak probe (4.1) ---
            var voSettings = new VisitSettings
            {
                volume = 0f,
                reduceMotion = true,
                captions = true,
                textScale = 1.0f,
                highContrastThoughtBox = false,
                thoughtReadDurationScale = 1.0f
            };
            Apply(casework.Begin(scene.Pad("AP00").position, voSettings), "P1 VO-SILENT begin");
            player.Teleport(scene.Pad("AP00"));

            int nonVoCountBefore = voice.NonVoAudioPlayCount;
            try
            {
                // 4.1 REAL fault injection: when -ctcc-qa-probe v36-vo-leak is set, VoiceDirector.Present bypasses the
                // INNER_SUB_SILENT early return into SpeakInternal(), triggering NonVoAudioPlayCount++ & Standard subtitle style,
                // which MUST be caught by the unmodified production assertion immediately below!
                voice.QaInjectVoLeakForTesting = probe == "v36-vo-leak";
                PresentLine("CH01-D01");
            }
            finally
            {
                voice.QaInjectVoLeakForTesting = false;
            }
            Require(ui.ActiveCueStyle == SubtitleStyle.SilentThought && ui.ActiveCueBackingEnabled &&
                    !voice.IsSpeaking && voice.NonVoAudioPlayCount == nonVoCountBefore,
                "VO-SILENT-Q01 CH01-D01 renders as SilentThought with WCAG AA scrim and zero AudioBus usage");
            Color expectedText = new(0.957f, 0.945f, 0.910f, 1f); // #F4F1E8
            Require(Vector4.Distance(ui.ActiveCueTextColor, expectedText) < 0.02f,
                $"VO-SILENT-Q01 text color matches #F4F1E8 (got {ui.ActiveCueTextColor})");
            Require(Mathf.Abs(ui.ActiveCueBackingColor.a - 0.56f) < 0.03f,
                $"VO-SILENT-Q01 default scrim alpha == 0.56 (got {ui.ActiveCueBackingColor.a:F2})");
            Require(Mathf.Abs(ui.EffectiveThoughtDurationScale - 1.0f) < 0.01f,
                $"VO-SILENT-Q01 default duration scale == 1.0x (got {ui.EffectiveThoughtDurationScale:F2})");

            // High-contrast scrim (0.80) + auto 1.35x when textScale >= 1.25 + explicit 1.6x
            var hcSettings = new VisitSettings
            {
                volume = 0f,
                reduceMotion = true,
                captions = true,
                textScale = 1.30f,
                highContrastThoughtBox = true,
                thoughtReadDurationScale = 1.0f
            };
            ui.Apply(hcSettings);
            PresentLine("CH01-D10");
            Require(Mathf.Abs(ui.ActiveCueBackingColor.a - 0.80f) < 0.03f &&
                    Mathf.Abs(ui.EffectiveThoughtDurationScale - 1.35f) < 0.02f &&
                    voice.NonVoAudioPlayCount == nonVoCountBefore,
                $"VO-SILENT-Q01 high-contrast scrim=0.80 (got {ui.ActiveCueBackingColor.a:F2}) and auto 1.35x scale at 130% font (got {ui.EffectiveThoughtDurationScale:F2})");

            hcSettings.thoughtReadDurationScale = 1.6f;
            ui.Apply(hcSettings);
            Require(Mathf.Abs(ui.EffectiveThoughtDurationScale - 1.6f) < 0.02f,
                $"VO-SILENT-Q01 explicit 1.6x duration scale applied (got {ui.EffectiveThoughtDurationScale:F2})");

            // CUT_TO_JOURNAL (CH01-D02): never touches AudioBus, routes canonical pocketNoteText into Transcript & Pocket Note!
            PresentLine("CH01-D02");
            string pocketAfterD02 = BuildPocketNoteWithCutToJournalEntries(State);
            Require(dialogue.TryGetValue("CH01-D02", out var d02Row) &&
                    !string.IsNullOrWhiteSpace(d02Row.pocketNoteText) &&
                    pocketAfterD02.Contains(d02Row.pocketNoteText) &&
                    !voice.IsSpeaking && voice.NonVoAudioPlayCount == nonVoCountBefore,
                "VO-SILENT-Q01 CUT_TO_JOURNAL CH01-D02 routes canonical pocketNoteText into Pocket Note with zero VO");

            // --- 3) V36-CH01-Q01 Route A: ADVERSARIAL ROUTE (strictly 0/4 E10a parts) + 4.1/4.2/4.3 Hardening ---
            Apply(casework.Begin(scene.Pad("AP00").position, voSettings), "P1 Adversarial begin");
            scene.interactables?.ResetSessionProps();
            player.Teleport(scene.Pad("AP00"));

            Require(State.evidence.Count == 0 && !State.Has("E10a") &&
                    InventoryService.HasPhysical(State, BackpackCatalog.Flashlight) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.MinhPhone) &&
                    E10aParts.ReadCount(State) == 0 && !State.Happened(E10aParts.PostponementSms),
                "V36-CH01-Q01 S0 starts with 0 evidence, Flashlight + MinhPhone, and 0/4 parts read (passive SMS badge does not auto-mark read)");

            // 4.2 Negative legacy-bypass tests: neither InspectE10aLayerAsync nor AcquireItemAsync may grant E10a or keys in CH01!
            var bypassInspect = casework.InspectE10aLayerAsync(0, "SIGNED_APPT");
            while (!bypassInspect.IsCompleted) yield return null;
            var bypassEnv = casework.AcquireItemAsync(BackpackCatalog.E10aEnvelope, "LOC01");
            while (!bypassEnv.IsCompleted) yield return null;
            var bypassGateKey = casework.AcquireItemAsync(BackpackCatalog.GateKey, "LOC01");
            while (!bypassGateKey.IsCompleted) yield return null;
            var bypassWingKey = casework.AcquireItemAsync(BackpackCatalog.WingCKey, "LOC01");
            while (!bypassWingKey.IsCompleted) yield return null;
            Require(bypassInspect.Result == CommitResult.PadRequired &&
                    bypassEnv.Result == CommitResult.PadRequired &&
                    bypassGateKey.Result == CommitResult.PadRequired &&
                    bypassWingKey.Result == CommitResult.PadRequired &&
                    State.evidence.Count == 0 && !State.Has("E10a") &&
                    !InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope) &&
                    !InventoryService.HasPhysical(State, BackpackCatalog.GateKey) &&
                    !InventoryService.HasPhysical(State, BackpackCatalog.WingCKey),
                "4.2 Legacy E10a/key bypass routes fail closed in CH01 (TakeMailboxPackage is the sole acquisition source)");

            // 4.3 Service test 1: OpenGate fails closed without KEY_GATE
            Require(casework.OpenGate(player.Feet) == CommitResult.PadRequired &&
                    casework.OpenGate(player.Feet, BackpackCatalog.GateKey) == CommitResult.PadRequired,
                "4.3 Casework.OpenGate fails closed before acquiring KEY_GATE");

            yield return WaitForApplied(casework.OpenMailboxAsync(), "P1 Adversarial open mailbox");
            yield return WaitForApplied(casework.TakeMailboxPackageAsync(equipGateKey: true), "P1 Adversarial atomic pickup");
            Require(State.evidence.Count == 1 && State.Has("E10a") &&
                    InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.GateKey) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.WingCKey) &&
                    InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.GateKey &&
                    E10aParts.ReadCount(State) == 0 && !State.Happened(E10aParts.PostponementSms),
                "V36-CH01-Q01 atomic mailbox pickup grants E10a + keys (KEY_GATE equipped) while keeping 0/4 parts read");

            // 4.3 Service test 2 + 4.1 REAL fault injection (v36-t02-wrong-key):
            // Equip KEY_WING_C while KEY_GATE sits in bag -> BOTH WorldUseResolver AND direct Casework.OpenGate MUST reject!
            yield return WaitForApplied(casework.EquipItemAsync(BackpackCatalog.WingCKey), "P1 equip WingCKey");
            WorldUseResolution wingKeyCheck;
            CommitResult wingGateCommitImplicit;
            CommitResult wingGateCommitExplicit;
            try
            {
                WorldUseResolver.QaInjectAllowWingKeyForTesting = probe == "v36-t02-wrong-key";
                wingKeyCheck = WorldUseResolver.Resolve(State, BackpackCatalog.WingCKey, "GATE-CONG");
                wingGateCommitImplicit = casework.OpenGate(player.Feet);
                wingGateCommitExplicit = casework.OpenGate(player.Feet, BackpackCatalog.WingCKey);
            }
            finally
            {
                WorldUseResolver.QaInjectAllowWingKeyForTesting = false;
            }
            Require(wingKeyCheck.Outcome == WorldUseOutcome.WrongKeyForOuterGate &&
                    wingGateCommitImplicit == CommitResult.PadRequired &&
                    wingGateCommitExplicit == CommitResult.PadRequired &&
                    InventoryService.HasPhysical(State, BackpackCatalog.WingCKey) &&
                    !State.Happened("CH01-T02"),
                "V36-CH01-Q01 KEY_WING_C rejected at outer gate (both resolver and Casework service) and retained");

            // 4.3 Service test 3: Unrelated item (MINH_PHONE) presented to Casework.OpenGate MUST fail closed even with KEY_GATE in bag!
            Require(casework.OpenGate(player.Feet, BackpackCatalog.MinhPhone) == CommitResult.PadRequired && !State.Happened("CH01-T02"),
                "4.3 Casework.OpenGate rejects unrelated MINH_PHONE even when KEY_GATE is in backpack");

            // 4.3 Service test 4: Valid KEY_GATE unlocks T02 at strictly 0/4 parts read
            yield return WaitForApplied(casework.EquipItemAsync(BackpackCatalog.GateKey), "P1 equip GateKey");
            Apply(casework.OpenGate(player.Feet, BackpackCatalog.GateKey), "P1 Adversarial T02 open gate at 0/4");
            Require(State.Happened("CH01-T02") && E10aParts.ReadCount(State) == 0 && !State.Happened(E10aParts.PostponementSms),
                "V36-CH01-Q01 Adversarial Route unlocks CH01-T02 cleanly with strictly 0/4 parts read");

            // 4.3 Service test 5: Repeated OpenGate call after T02 is idempotent with ZERO sequence increment
            long seqAfterT02 = State.sequence;
            Require(casework.OpenGate(player.Feet, BackpackCatalog.GateKey) == CommitResult.Applied &&
                    State.sequence == seqAfterT02,
                "4.3 Casework.OpenGate is strictly idempotent after CH01-T02 with zero sequence increment");

            // --- 4) V36-CH01-Q01 Route B: NARRATIVE ROUTE + 4.4 CH01-D01 Sight Guard + 4.5 Two-Source signedAppointment ---
            // 4.4 Order A: Look at lit lobby FIRST (facing North at AP00) -> read SMS 22:48 -> D01 fires once with 0 VO!
            Apply(casework.Begin(scene.Pad("AP00").position, voSettings), "P1 Narrative begin (Order A)");
            player.Teleport(scene.Pad("AP00"));
            observedLitLobbyInCh01 = false;
            deliveredCh01D01 = false;
            voice.RestoreTranscript(Array.Empty<TranscriptEntry>(), _ => true);
            int nonVoBeforeD01 = voice.NonVoAudioPlayCount;
            TickCh01LitLobbySight();
            Require(observedLitLobbyInCh01 && !deliveredCh01D01 && !State.Happened(E10aParts.PostponementSms) && E10aParts.ReadCount(State) == 0,
                "4.4 Order A: looking at lit lobby sets sight flag without firing CH01-D01 before SMS 22:48");

            yield return WaitForApplied(casework.RecordE10aPartReadAsync(E10aParts.PostponementSms, "BAY_SMS"), "P1 Narrative read SMS 22:48");
            Require(State.Happened(E10aParts.PostponementSms) && State.Happened("E10a.SMSRead") && E10aParts.ReadCount(State) == 1,
                "V36-CH01-Q01 reading SMS 22:48 commits postponementSMS truthfully (1/4 parts)");
            TickCh01LitLobbySight();
            Require(deliveredCh01D01 && !TryPresentCh01D01() &&
                    voice.Transcript.Any(t => t.LineId == "CH01-D01") &&
                    voice.NonVoAudioPlayCount == nonVoBeforeD01,
                "4.4 Order A: CH01-D01 fires exactly once (0 VO) once BOTH lit lobby & SMS 22:48 are satisfied");

            // 4.5 Two-source signedAppointment Order 1: SMS 16/11 alone does NOT commit signedAppointment; adding mailbox note DOES!
            yield return WaitForApplied(casework.RecordSmsAppointmentViewedAsync(), "P1 Narrative read SMS 16/11");
            yield return WaitForApplied(casework.RecordPhoneCallBayAsync(), "P1 Narrative call Bảy");
            Require(State.Happened(E10aParts.SmsAppointmentViewed) && State.Happened(E10aParts.PhoneCalledBay) &&
                    !State.Happened(E10aParts.SignedAppointment) && E10aParts.ReadCount(State) == 1,
                "4.5 SMS 16/11 alone sets SmsAppointmentViewed without prematurely committing signedAppointment (still 1/4)");

            yield return WaitForApplied(casework.OpenMailboxAsync(), "P1 Narrative open mailbox");
            yield return WaitForApplied(casework.RecordMailboxNoteViewedAsync(), "P1 Narrative view mailbox note");
            Require(State.Happened(E10aParts.MailboxNoteViewed) && State.Happened(E10aParts.SignedAppointment) && E10aParts.ReadCount(State) == 2,
                "4.5 Viewing BOTH SMS 16/11 + mailbox external note commits signedAppointment truthfully (2/4)");

            yield return WaitForApplied(casework.TakeMailboxPackageAsync(equipGateKey: true), "P1 Narrative atomic pickup");
            yield return WaitForApplied(casework.RecordE10aPartReadAsync(E10aParts.Letter, "LAN_LETTER"), "P1 Narrative read Lan 2005 letter");
            Require(State.Happened(E10aParts.Letter) && E10aParts.ReadCount(State) == 3 &&
                    !State.Happened(E10aParts.PublicReportSummary),
                "V36-CH01-Q01 Narrative Route tracks each E10a part independently (3/4 read, newspaper still unread)");
            Apply(casework.OpenGate(player.Feet, BackpackCatalog.GateKey), "P1 Narrative T02 open gate");
            Require(State.Happened("CH01-T02") && State.evidence.Count == 1 && State.Has("E10a"),
                "V36-CH01-Q01 Narrative Route commits T02 cleanly with single E10a evidence");

            // 4.4 Order B & 4.5 Order 2: Face AWAY toward road (yaw=180) -> read SMS 22:48 -> D01 stays withheld ->
            // T02 STILL unlocks (D01 never gates T02!) -> turn back toward lit lobby -> D01 fires cleanly!
            // Also verify mailbox note alone != signedAppointment until SMS 16/11 is viewed, including across Save v7 reload!
            Apply(casework.Begin(scene.Pad("AP00").position, voSettings), "P1 D01 Order B + SignedAppt Order 2");
            player.Teleport(scene.Pad("AP00").position, Quaternion.Euler(0f, 180f, 0f)); // facing South away from lit lobby!
            observedLitLobbyInCh01 = false;
            deliveredCh01D01 = false;
            voice.RestoreTranscript(Array.Empty<TranscriptEntry>(), _ => true);
            TickCh01LitLobbySight();
            Require(!IsLookingTowardLitLobby() && !observedLitLobbyInCh01,
                "4.4 Order B: facing away toward road (South) does NOT count as observing lit lobby");
            yield return WaitForApplied(casework.RecordE10aPartReadAsync(E10aParts.PostponementSms, "BAY_SMS"), "P1 Order B SMS 22:48");
            TickCh01LitLobbySight();
            Require(!deliveredCh01D01 && !TryPresentCh01D01() && !voice.Transcript.Any(t => t.LineId == "CH01-D01"),
                "4.4 Order B: reading SMS 22:48 while facing away does NOT prematurely fire CH01-D01");

            // 4.5 Order 2: Mailbox note alone must NOT commit signedAppointment (even after Save v7 reconciliation!)
            yield return WaitForApplied(casework.OpenMailboxAsync(), "P1 Order B open mailbox");
            yield return WaitForApplied(casework.TakeMailboxPackageAsync(equipGateKey: true), "P1 Order B pickup");
            yield return WaitForApplied(casework.InspectE10aLayerAsync(0, "SIGNED_APPT"), "P1 Order B inspect mailbox note only");
            Require(State.Happened(E10aParts.MailboxNoteViewed) && !State.Happened(E10aParts.SignedAppointment) && E10aParts.ReadCount(State) == 1,
                "4.5 Order 2: viewing mailbox note alone does NOT prematurely commit signedAppointment");
            var noteOnlyCopy = State.Copy();
            InventoryService.ReconcileSaveV7ForRebirthIII(noteOnlyCopy);
            Require(noteOnlyCopy.Happened(E10aParts.MailboxNoteViewed) && !noteOnlyCopy.Happened(E10aParts.SignedAppointment),
                "4.5 Order 2: Save v7 reconciliation preserves partial MailboxNoteViewed without fabricating signedAppointment");
            yield return WaitForApplied(casework.RecordSmsAppointmentViewedAsync(), "P1 Order B view SMS 16/11");
            Require(State.Happened(E10aParts.SignedAppointment) && E10aParts.ReadCount(State) == 2,
                "4.5 Order 2: adding SMS 16/11 after mailbox note completes signedAppointment (2/4)");

            // Verify T02 unlocks BEFORE CH01-D01 has ever fired (Core 04 P78: D01 never gates T02!)
            Apply(casework.OpenGate(player.Feet, BackpackCatalog.GateKey), "P1 Order B T02 without D01");
            Require(State.Happened("CH01-T02") && !deliveredCh01D01,
                "4.4 Order B: CH01-T02 unlocks freely even while CH01-D01 is still pending sight of lit lobby");

            // Now rotate player North toward the lit reception lobby -> TickCh01LitLobbySight() fires CH01-D01 cleanly!
            player.Teleport(scene.Pad("AP00").position, Quaternion.identity);
            TickCh01LitLobbySight();
            Require(observedLitLobbyInCh01 && deliveredCh01D01 &&
                    voice.Transcript.Any(t => t.LineId == "CH01-D01") &&
                    voice.NonVoAudioPlayCount == nonVoBeforeD01,
                "4.4 Order B: turning toward lit lobby after SMS fires CH01-D01 once cleanly with zero VO");

            // --- 5) D3 Rev 1.1: 6 Synthetic + 3 REAL Historical Prior-Build Save v7 Migration Suite (4.7) ---
            VerifyLegacySaveV7Rebirth3Migrations(savedBefore);

            // --- 6) Ch01JournalPolicy Phase 1 Boundaries ---
            Apply(casework.Begin(scene.Pad("AP00").position, voSettings), "P1 JournalPolicy B01");
            Require(!IsJournalAllowedOnAn01 && !IsJournalAllowedInBackpack,
                "Ch01JournalPolicy Phase 1: Journal blocked on AN01 and Backpack in B01");
            Apply(casework.TakeMailboxPackage(player.Feet, equipGateKey: true), "P1 JP pickup");
            Apply(casework.OpenGate(player.Feet, BackpackCatalog.GateKey), "P1 JP gate");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P1 JP enter B (B03)");
            Require(!IsJournalAllowedOnAn01 && !IsJournalAllowedInBackpack,
                "Ch01JournalPolicy Phase 1: Journal remains blocked on AN01 and Backpack across B01-B03 (pre-T05)");
            Apply(casework.Board(player.Feet), "P1 JP accept BOARD (T05)");
            Require(!IsJournalAllowedOnAn01 && !IsJournalAllowedInBackpack,
                "Ch01JournalPolicy Phase 2: Journal is 100% locked across ALL of CH01 including B04-B05 (3D AN02 replaces notebook)");
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.EnterRoom(player.Feet), "P1 JP enter 101");
            Apply(casework.SubmitF101(FormStatus.Unverified, null, player.Feet, out _), "P1 JP F101 B");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P1 JP B->R (T08)");
            Require(!IsJournalAllowedOnAn01 && !IsJournalAllowedInBackpack,
                "Ch01JournalPolicy Phase 1: Journal locked again in B06-B08 (post-T08)");
            player.Teleport(scene.Pad("AP01"));
            Apply(casework.OutsideAttempt(player.Feet), "P1 JP T09");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P1 JP handoff CH02");
            Require(State.chapter == "CH02" && IsJournalAllowedOnAn01 && IsJournalAllowedInBackpack,
                "Ch01JournalPolicy Phase 1: CH02+ restores full AN01 & Backpack Journal compatibility");

            // --- 7) D4: Exterior East Gate Pillar Mailbox Geometry, Reach, Pivot & frontOnly Facing ---
            var mailboxTarget = Array.Find(FindObjectsByType<SceneTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                t => t.id == "MAILBOX");
            Require(mailboxTarget != null, "D4 MAILBOX SceneTarget exists in LOC01");
            Vector3 mbPos = mailboxTarget.transform.position;
            Vector3 ap00 = scene.Pad("AP00").position;
            float distFromAp00 = Vector2.Distance(new Vector2(mbPos.x, mbPos.z), new Vector2(ap00.x, ap00.z));
            Require(mbPos.x >= 5.20f && mbPos.x <= 5.52f && distFromAp00 <= 2.60f,
                $"D4 MAILBOX mounted on East Gate Pillar [5.20..5.52] within 2.60m of AP00 (x={mbPos.x:F2}, dist={distFromAp00:F2}m)");
            Require(mailboxTarget.frontOnly && mailboxTarget.handhold != null &&
                    Vector3.Dot(mailboxTarget.handhold.forward, Vector3.back) > 0.95f,
                "D4 MAILBOX configures frontOnly=true with South-facing (-Z) handhold");
            Require(mailboxTarget.VisibleFrom(new Vector3(4.80f, 1.50f, -22.20f), mailboxTarget.Contact) &&
                    !mailboxTarget.VisibleFrom(new Vector3(4.80f, 1.50f, -18.50f), mailboxTarget.Contact),
                "D4 MAILBOX is reachable from outside road (-Z) and blocked from inside courtyard (+Z)");
            var mbColGo = GameObject.Find("Gate mailbox collider");
            Require(mbColGo != null && mbColGo.GetComponent<Collider>() is Collider mbCol &&
                    mbCol.bounds.min.x >= 5.19f && mbCol.bounds.max.x <= 5.53f,
                "D4 Gate mailbox solid collider stays strictly clear of outer gate doorway [2.80..5.20]");
            // Verify East-hinged pivot swings outward South-East (-104 deg)
            var loc01Region = Array.Find(FindObjectsByType<ZoneRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                z => z.zoneId == "LOC01");
            Require(loc01Region != null && loc01Region.mailboxLidPivot != null &&
                    loc01Region.mailboxLidPivot.childCount > 0 && loc01Region.mailboxPackageVisual != null,
                "D4 LOC01 ZoneRegion binds unpacked MailboxLidPivot (with MailboxLid mesh child) and MailboxPackageVisual");
            var closedProbe = Investigation.New();
            loc01Region.PresentCasework(closedProbe);
            var lidRenderer = loc01Region.mailboxLidPivot.GetComponentInChildren<Renderer>(true);
            Require(lidRenderer != null, "D4 MailboxLid renderer present under MailboxLidPivot");
            Vector3 lidClosedCenter = lidRenderer.bounds.center;
            var openProbe = Investigation.New();
            openProbe.Remember(E10aParts.MailboxOpened);
            loc01Region.PresentCasework(openProbe);
            Vector3 lidOpenCenter = lidRenderer.bounds.center;
            Require(Quaternion.Angle(loc01Region.mailboxLidPivot.localRotation, Quaternion.Euler(0f, -104f, 0f)) < 1f &&
                    Vector3.Distance(lidClosedCenter, lidOpenCenter) > 0.12f &&
                    lidOpenCenter.x >= 5.25f && lidOpenCenter.z < lidClosedCenter.z &&
                    loc01Region.mailboxPackageVisual.activeSelf,
                $"D4 opening mailbox physically swings lid outward South-East away from gate ({lidClosedCenter} -> {lidOpenCenter})");
            InventoryService.EnsureDefaultTools(openProbe);
            InventoryService.TryAcquire(openProbe, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out _);
            InventoryService.TryAcquire(openProbe, BackpackCatalog.GateKey, Reality.R, "LOC01", out _);
            InventoryService.TryAcquire(openProbe, BackpackCatalog.WingCKey, Reality.R, "LOC01", out _);
            openProbe.Remember(E10aParts.MailboxPickedUp);
            loc01Region.PresentCasework(openProbe);
            Require(!loc01Region.mailboxPackageVisual.activeSelf,
                "D4 atomic pickup hides MailboxPackageVisual cleanly");

            // --- 8) Real GateService.G08 Predicate (not a stub; CH08 E2E remains Out of Scope until TASK016) ---
            Require(!GateService.G08(State), "GateService.G08 must be false in CH02");
            var g08Probe = Investigation.New();
            g08Probe.chapter = "CH02";
            g08Probe.Remember("G08.opened");
            Require(!GateService.G08(g08Probe), "GateService.G08 must reject G08.opened outside CH08");
            g08Probe.chapter = "CH08";
            g08Probe.events.Remove("G08.opened");
            Require(!GateService.G08(g08Probe), "GateService.G08 requires G08.opened in CH08");
            g08Probe.Remember("G08.opened");
            Require(GateService.G08(g08Probe), "GateService.G08 evaluates true only in CH08 once G08.opened is committed");

            // Restore pre-suite snapshot so subsequent QA suites continue cleanly
            Apply(transaction.Commit(savedBefore, savedPos, warp: false, geometryChanges: true), "P1 restore pre-suite snapshot");
            player.Teleport(savedPos, player.transform.rotation);
            verification.Add("CH01 REBIRTH III PHASE 1 HARDENED PASS: VAL-V3-01 (143 rows / 0 forbidden WAVs / 17 CH01 VO), VO-SILENT-Q01 (WCAG AA scrim + scale + CUT_TO_JOURNAL + real v36-vo-leak probe), V36-CH01-Q01 (4.2 bypass block + 4.3 Casework.OpenGate 5-case service guard + real v36-t02-wrong-key probe + 4.4 two-way D01 sight guard + 4.5 two-source signedAppointment), D3 (6 synthetic + real historical CH01/CH04/CH05 Save v7 round-trips), Ch01JournalPolicy, D4 East-pillar mailbox, and real G08 predicate.");
        }

        private void VerifyLegacySaveV7Rebirth3Migrations(Investigation realPostCh01Snapshot)
        {
            var dir = Path.Combine(qaFolder, "rebirth3-savev7-migration");
            var isolated = new AtomicSaves(dir);

            Investigation LoadAndAssertIdempotent(string caseLabel, Investigation rawLegacyV7)
            {
                rawLegacyV7.schemaVersion = Investigation.Schema;
                string payload = JsonUtility.ToJson(rawLegacyV7);
                string checksum;
                using (var sha = SHA256.Create())
                    checksum = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
                Directory.CreateDirectory(Path.GetDirectoryName(isolated.SlotPath(SaveSlot.Auto)));
                File.WriteAllText(isolated.SlotPath(SaveSlot.Auto),
                    $"{{\"schemaVersion\":{Investigation.Schema},\"payload\":{JsonEscape(payload)},\"checksum\":\"{checksum}\"}}");

                Require(isolated.Read(SaveSlot.Auto, out var migrated) == SaveRead.Valid && migrated != null,
                    $"D3 Migration [{caseLabel}] failed to read valid Save v7");
                string afterFirst = JsonUtility.ToJson(migrated);
                InventoryService.ReconcileSaveV7ForRebirthIII(migrated);
                string afterSecond = JsonUtility.ToJson(migrated);
                Require(string.Equals(afterFirst, afterSecond, StringComparison.Ordinal),
                    $"D3 Migration [{caseLabel}] is NOT strictly idempotent on second pass!");
                return migrated;
            }

            // Case 1: Pre-Rebirth-III New Game save (frame-0 seeded "E10a" + envelope, NO keys, NO T02) -> MUST NOT SOFTLOCK!
            var c1 = new Investigation { chapter = "CH01", layer = Reality.R, checkpoint = "CH01-CP00" };
            c1.evidence.Add("E10a");
            InventoryService.EnsureDefaultTools(c1);
            InventoryService.TryAcquire(c1, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out _);
            var m1 = LoadAndAssertIdempotent("Case1-SeedOnlyNewGame", c1);
            Require(!m1.Has("E10a") && m1.evidence.Count == 0 &&
                    !m1.Happened(E10aParts.MailboxPickedUp) &&
                    !InventoryService.HasPhysical(m1, BackpackCatalog.E10aEnvelope) &&
                    !InventoryService.HasPhysical(m1, BackpackCatalog.GateKey) &&
                    InventoryService.HasPhysical(m1, BackpackCatalog.MinhPhone),
                "D3 Case 1: seed-only New Game strips legacy E10a seed so mailbox stays interactable (zero softlock)");

            // Case 2: Partially inspected (read Lan letter on envelope, missing physical keys) -> keeps read flag & allows completing pickup without duplicates!
            var c2 = new Investigation { chapter = "CH01", layer = Reality.R, checkpoint = "CH01-CP00" };
            c2.evidence.Add("E10a");
            InventoryService.EnsureDefaultTools(c2);
            InventoryService.TryAcquire(c2, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out var c2Env);
            c2Env.MarkViewed("LAN_LETTER");
            var m2 = LoadAndAssertIdempotent("Case2-PartialInspect", c2);
            Require(m2.Has("E10a") && m2.Happened(E10aParts.Letter) && !m2.Happened(E10aParts.MailboxPickedUp) &&
                    InventoryService.HasPhysical(m2, BackpackCatalog.E10aEnvelope) &&
                    !InventoryService.HasPhysical(m2, BackpackCatalog.GateKey),
                "D3 Case 2: partial inspect preserves envelope & Lan read flag while leaving mailbox pickup available for missing keys");

            // Case 3: Complete envelope + both keys before T02 -> preserves items, sets MailboxPickedUp, zero duplicates!
            var c3 = new Investigation { chapter = "CH01", layer = Reality.R, checkpoint = "CH01-CP00" };
            c3.evidence.Add("E10a");
            InventoryService.EnsureDefaultTools(c3);
            InventoryService.TryAcquire(c3, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out _);
            InventoryService.TryAcquire(c3, BackpackCatalog.GateKey, Reality.R, "LOC01", out _);
            InventoryService.TryAcquire(c3, BackpackCatalog.WingCKey, Reality.R, "LOC01", out _);
            var m3 = LoadAndAssertIdempotent("Case3-CompletePreT02", c3);
            Require(m3.Has("E10a") && m3.Happened(E10aParts.MailboxPickedUp) &&
                    InventoryService.HasPhysical(m3, BackpackCatalog.E10aEnvelope) &&
                    InventoryService.HasPhysical(m3, BackpackCatalog.GateKey) &&
                    InventoryService.HasPhysical(m3, BackpackCatalog.WingCKey) &&
                    E10aParts.ReadCount(m3) == 0,
                "D3 Case 3: complete physical package sets MailboxPickedUp without duplicating items or faking reads");

            // Case 4: Past T02 in CH01 -> keeps gate & progress committed, marks MailboxPickedUp & ensures keys/envelope
            var c4 = new Investigation { chapter = "CH01", layer = Reality.R, checkpoint = "CH01-CP01" };
            c4.evidence.Add("E10a");
            c4.Remember("CH01-T02");
            InventoryService.EnsureDefaultTools(c4);
            var m4 = LoadAndAssertIdempotent("Case4-PastT02", c4);
            Require(m4.Happened("CH01-T02") && m4.Happened(E10aParts.MailboxPickedUp) &&
                    m4.Has("E10a") && InventoryService.HasPhysical(m4, BackpackCatalog.GateKey),
                "D3 Case 4: post-T02 save retains gate progress and reconciles physical package cleanly");

            // Case 5: Partial reads (LAN_LETTER + legacy FLAP) -> maps ONLY Letter; NEVER guesses SignedAppointment from FLAP!
            var c5 = new Investigation { chapter = "CH01", layer = Reality.R, checkpoint = "CH01-CP01" };
            c5.evidence.Add("E10a");
            c5.Remember("CH01-T02");
            InventoryService.EnsureDefaultTools(c5);
            InventoryService.TryAcquire(c5, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out var c5Env);
            c5Env.MarkViewed("LAN_LETTER");
            c5Env.MarkViewed("FLAP");
            var m5 = LoadAndAssertIdempotent("Case5-PartialReadsNoFlapGuess", c5);
            Require(m5.Happened(E10aParts.Letter) &&
                    !m5.Happened(E10aParts.SignedAppointment) &&
                    !m5.Happened(E10aParts.PostponementSms) &&
                    !m5.Happened(E10aParts.PublicReportSummary) &&
                    E10aParts.ReadCount(m5) == 1,
                "D3 Case 5: migrates evidenced LAN_LETTER only; never guesses SignedAppointment from legacy FLAP");

            // Case 6: Real post-CH01 save (CH02 snapshot stripped of Rebirth III flags/items) -> never resets chapter/checkpoint/layer to LOC01!
            var c6 = realPostCh01Snapshot.Copy();
            string expectedChapter = c6.chapter;
            Reality expectedLayer = c6.layer;
            string expectedCheckpoint = c6.checkpoint;
            c6.events.RemoveAll(e => e.StartsWith("E10a.Part.", StringComparison.Ordinal) ||
                e == E10aParts.MailboxOpened || e == E10aParts.MailboxPickedUp || e == "E10a.SMSRead");
            c6.backpackItems.RemoveAll(b => b != null && (b.itemDefId == BackpackCatalog.E10aEnvelope ||
                b.itemDefId == BackpackCatalog.MinhPhone || b.itemDefId == BackpackCatalog.GateKey || b.itemDefId == BackpackCatalog.WingCKey));
            var m6 = LoadAndAssertIdempotent("Case6-PostCh01RealSnapshot", c6);
            Require(m6.chapter == expectedChapter && m6.layer == expectedLayer && m6.checkpoint == expectedCheckpoint &&
                    m6.Happened(E10aParts.MailboxPickedUp) &&
                    InventoryService.HasPhysical(m6, BackpackCatalog.E10aEnvelope) &&
                    InventoryService.HasPhysical(m6, BackpackCatalog.MinhPhone) &&
                    E10aParts.ReadCount(m6) == 0,
                "D3 Case 6: post-CH01 save preserves chapter/layer/checkpoint without resetting to LOC01 or faking reads");

            // 4.7 Historical Real Prior-Build Save v7 Round-Trip Verification (when repo output/ch01-art-production exists on disk)
            string repoRoot = FindRepoRootForHistoricalSaves();
            if (!string.IsNullOrEmpty(repoRoot))
            {
                var historicalCases = new (string RelPath, string Chapter, string Checkpoint, Reality Layer, int ExpectedReadCount, bool ExpectLetter)[]
                {
                    ("output/ch01-art-production/input-delivery/save/auto.json", "CH01", "CH01-CP01", Reality.B, 2, true),
                    ("output/ch01-art-production/module-final/ch05-legacy-ch04/auto.json", "CH04", "CH04-CP04", Reality.R, 1, false),
                    ("output/ch01-art-production/module-final/isolated-save/auto.json", "CH05", "CH05-CP03", Reality.R, 1, false)
                };
                int verifiedRealFiles = 0;
                foreach (var hc in historicalCases)
                {
                    string src = Path.Combine(repoRoot, hc.RelPath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(src)) continue;
                    string histDir = Path.Combine(dir, "historical-" + hc.Chapter.ToLowerInvariant());
                    Directory.CreateDirectory(histDir);
                    var histStore = new AtomicSaves(histDir);
                    File.Copy(src, histStore.SlotPath(SaveSlot.Auto), overwrite: true);
                    Require(histStore.Read(SaveSlot.Auto, out var loaded) == SaveRead.Valid && loaded != null,
                        $"4.7 Historical Save v7 [{hc.RelPath}] failed to read");
                    Require(loaded.chapter == hc.Chapter && loaded.checkpoint == hc.Checkpoint && loaded.layer == hc.Layer &&
                            loaded.Happened(E10aParts.MailboxPickedUp) &&
                            InventoryService.HasPhysical(loaded, BackpackCatalog.MinhPhone) &&
                            InventoryService.HasPhysical(loaded, BackpackCatalog.E10aEnvelope) &&
                            loaded.Happened(E10aParts.PostponementSms) &&
                            loaded.Happened(E10aParts.Letter) == hc.ExpectLetter &&
                            !loaded.Happened(E10aParts.SignedAppointment) &&
                            !loaded.Happened(E10aParts.PublicReportSummary) &&
                            E10aParts.ReadCount(loaded) == hc.ExpectedReadCount &&
                            loaded.settings != null && Mathf.Approximately(loaded.settings.thoughtReadDurationScale, 1f) &&
                            InventoryService.ValidState(loaded),
                        $"4.7 Historical Save v7 [{hc.RelPath}] migrated cleanly (chapter={loaded.chapter}, cp={loaded.checkpoint}, readCount={E10aParts.ReadCount(loaded)}, zero fake FLAP->signedAppointment)");

                    // Round-trip write & reload must be strictly byte-identical on payload!
                    string beforeWrite = JsonUtility.ToJson(loaded);
                    histStore.Write(SaveSlot.Auto, loaded);
                    Require(histStore.Read(SaveSlot.Auto, out var reloaded) == SaveRead.Valid && reloaded != null,
                        $"4.7 Historical Save v7 [{hc.RelPath}] round-trip reload failed");
                    Require(string.Equals(beforeWrite, JsonUtility.ToJson(reloaded), StringComparison.Ordinal),
                        $"4.7 Historical Save v7 [{hc.RelPath}] round-trip reload is NOT strictly idempotent!");
                    verifiedRealFiles++;
                }
                Require(verifiedRealFiles == 3, $"4.7 Expected all 3 historical Save v7 files to verify on disk (verified {verifiedRealFiles})");
                verification.Add($"D3 HISTORICAL SAVE V7 PASS: Verified {verifiedRealFiles}/3 real prior-build saves (CH01-CP01/B, CH04-CP04/R, CH05-CP03/R) with strict round-trip idempotency & zero legacy FLAP fabrication.");
            }
        }

        private string FindRepoRootForHistoricalSaves()
        {
            string[] seeds = { qaFolder, Application.dataPath, Directory.GetCurrentDirectory() };
            foreach (string seed in seeds)
            {
                if (string.IsNullOrEmpty(seed)) continue;
                var cur = new DirectoryInfo(seed);
                for (int depth = 0; depth < 8 && cur != null; depth++, cur = cur.Parent)
                {
                    if (Directory.Exists(Path.Combine(cur.FullName, "output", "ch01-art-production")))
                        return cur.FullName;
                }
            }
            return null;
        }
    }
}
