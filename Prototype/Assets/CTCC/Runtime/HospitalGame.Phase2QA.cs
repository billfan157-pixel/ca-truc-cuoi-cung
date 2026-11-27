using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        /// <summary>
        /// CH01 Rebirth III — Phase 2 Verification Suite (Core v3.6.2 / DEC-V36-CH01-01 / DEC-V361-CH01-01 / D1 / D5).
        /// Verifies:
        /// 1. DEC-V361-CH01-01 & D5 Bed 4 / Socket purity: 0 wheel marks in CH01 Layer B (FourthBedReportTrace_B &
        ///    RefutedBed4SharpWheels absent), Socket1..Socket4 hidden in B and active in R, OBS101A copy strictly
        ///    blanket+slippers, corridor slip B reads "CHỜ KIỂM TRA", and FS22 CH01_Clock0317_EEGHum loads cleanly.
        /// 2. V36-CH01-Q02 & V36-CH01-Q05: A/B commit at 0 OBS; A hides Bed4ChartHolder & blocks OBS101B behind deniedBay;
        ///    A->B restores Bed4ChartHolder & OBS101B without auto-granting OBS; backend F101LinkProvenance rejects
        ///    annotationVersion==0, SetF101LinkFromUiAsync in CH01, wrong-location spatial links, and unverified links
        ///    (with REAL fault-injection probe v36-f101-unverified-link); unmarking OBS101B clears SpatialLinkEvent;
        ///    valid spatial Bed-4 link commits C (first=A, rev=3, D=0, duplicate idempotent); legacy committed saves &
        ///    CH02 F101 revision remain 100% compatible.
        /// 3. V36-CH01-Q03: EX01 free HH:MM manual entry never auto-solves (with REAL fault-injection probe v36-ex01-autosolve),
        ///    rejects blank/wrong/future-timeline timecodes (02:50/03:05/07:40/etc.), commits cleanly on "03:17", and
        ///    persists across reload.
        /// 4. V36-CH01-Q06: 100% Zero-Journal lock across ALL of CH01 B01-B08 (DrawJournal redirects to Pocket Note),
        ///    3D AN02 clipboard opens with camera control & preserves draft on Esc, and CH02+ restores full 5-tab Journal.
        /// </summary>
        private IEnumerator VerifyCh01Rebirth3Phase2Suite()
        {
            string probe = Argument("-ctcc-qa-probe");
            var savedBefore = State.Copy();
            var savedPos = player.Feet;

            var settings = new VisitSettings
            {
                volume = 0f,
                reduceMotion = true,
                captions = true,
                textScale = 1.0f
            };
            Apply(casework.Begin(scene.Pad("AP00").position, settings), "P2 begin");
            scene.interactables?.ResetSessionProps();
            player.Teleport(scene.Pad("AP00"));

            // Reach CH01 B04 (Room 101 Layer B, post-T05/T06) with 0 observations!
            Apply(casework.TakeMailboxPackage(player.Feet, equipGateKey: true), "P2 pickup");
            Apply(casework.OpenGate(player.Feet, BackpackCatalog.GateKey), "P2 open gate");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P2 R->B");
            Apply(casework.Board(player.Feet), "P2 BOARD T05");
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.EnterRoom(player.Feet), "P2 enter Room 101 T06");

            // --- 1) DEC-V361-CH01-01 & D5: Bed 4 Layer B purity, Socket1..4 Layer R isolation, FS22 EEG hum ---
            var loc03Region = Array.Find(FindObjectsByType<ZoneRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                z => z.zoneId == "LOC03");
            Require(loc03Region != null, "P2 LOC03 ZoneRegion present");
            bool foundReportWheels = false;
            bool foundRefutedWheels = false;
            int socketsTotal = 0;
            int socketsActiveInB = 0;
            foreach (var t in loc03Region.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "FourthBedReportTrace_B") foundReportWheels = true;
                if (t.name == "RefutedBed4SharpWheels") foundRefutedWheels = true;
                if (t.name.StartsWith("Socket", StringComparison.Ordinal) && t.name.Length == 7 && char.IsDigit(t.name[6]))
                {
                    socketsTotal++;
                    if (t.gameObject.activeInHierarchy) socketsActiveInB++;
                }
            }
            Require(!foundReportWheels && !foundRefutedWheels,
                "DEC-V361-CH01-01: FourthBedReportTrace_B and RefutedBed4SharpWheels removed completely from LOC03 Layer B");
            Require(socketsTotal == 4 && socketsActiveInB == 0,
                $"DEC-V361-CH01-01: All 4 numbered bed outlet plaques Socket1..4 exist on Layer R and stay hidden in CH01 Layer B (total={socketsTotal}, activeInB={socketsActiveInB})");

            var obsASpec = ObservationRegistry.Get(ObservationRegistry.Bed4Traces);
            Require(obsASpec != null &&
                    !obsASpec.FactSummary.Contains("bánh", StringComparison.OrdinalIgnoreCase) &&
                    Array.TrueForAll(obsASpec.DetailLabels, l => !l.Contains("bánh", StringComparison.OrdinalIgnoreCase)),
                "DEC-V361-CH01-01: OBS101A copy describes strictly blanket & slippers with zero wheel mentions");
            Require(Resources.Load<AudioClip>("CTCC/CH01_Clock0317_EEGHum") != null,
                "FS22: CH01_Clock0317_EEGHum.wav loads cleanly from Resources/CTCC");
            Require(!Task02HardGateEnabled, "D1: Task 02 remains non-blocking by default");

            // --- 2) V36-CH01-Q05 & D5: A at 0 OBS hides Bed4ChartHolder & blocks OBS101B; A->B restores cleanly with 0 auto-OBS! ---
            Require(!State.Observed(ObservationRegistry.Bed4Traces) && !State.Observed(ObservationRegistry.Bed4Card),
                "P2 starts Room 101 with 0 Bed-4 observations");
            var obsBTarget = Array.Find(FindObjectsByType<SceneTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                t => t.id == "OBS101B");
            var obsATarget = Array.Find(FindObjectsByType<SceneTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                t => t.id == "OBS101A");
            Require(obsBTarget != null && obsATarget != null && scene.bed4ChartHolder != null && scene.bed4ChartHolder.activeInHierarchy,
                "D5: Bed4ChartHolder and OBS101A/B are active before F101 Affirmed");

            Apply(casework.SubmitF101(FormStatus.Affirmed, null, player.Feet, out var evalA), "P2 F101 A at 0 OBS");
            Require(evalA.Accepted && GateService.G01(State) && FormService.Distortion(State) == 1 &&
                    State.f101.firstDecision == FormStatus.Affirmed && State.f101.revision == 1 &&
                    scene.deniedBay.activeInHierarchy &&
                    !scene.bed4ChartHolder.activeInHierarchy &&
                    !Available(obsBTarget) && !Available(obsATarget),
                "D5 / V36-CH01-Q05: F101 Affirmed at 0 OBS seals alcove (deniedBay), hides Bed4ChartHolder, and blocks OBS101A/B");

            // Revise A -> B: MUST reopen alcove & restore Bed4ChartHolder/OBS101B with ZERO auto-granted observations!
            Apply(casework.SubmitF101(FormStatus.Unverified, null, player.Feet, out var evalB), "P2 F101 revision A->B");
            Require(evalB.Accepted && FormService.Distortion(State) == 0 &&
                    State.f101.firstDecision == FormStatus.Affirmed && State.f101.current == FormStatus.Unverified &&
                    State.f101.revision == 2 &&
                    !scene.deniedBay.activeInHierarchy && scene.bed4ChartHolder.activeInHierarchy &&
                    Available(obsBTarget) && Available(obsATarget) &&
                    !State.Observed(ObservationRegistry.Bed4Traces) && !State.Observed(ObservationRegistry.Bed4Card),
                "V36-CH01-Q05: A->B restores alcove, Bed4ChartHolder & OBS101B cleanly without auto-granting any observation");
            RequireF101Presentation(FormStatus.Unverified, true); // also asserts corridor slip == "CHỜ KIỂM TRA"!

            // --- 3) V36-CH01-Q02: Backend F101LinkProvenance (4 states) + REAL v36-f101-unverified-link fault probe ---
            Apply(casework.Observe(ObservationRegistry.F101Sheet, player.Feet, "LOC03"), "P2 examine F101");
            Apply(casework.Observe(ObservationRegistry.Bed4Traces, player.Feet, "LOC03"), "P2 examine OBS101A");
            Apply(casework.Observe(ObservationRegistry.Bed4Card, player.Feet, "LOC03"), "P2 examine OBS101B");
            yield return WaitForApplied(casework.ToggleF101MarkAsync(ObservationRegistry.F101Sheet, F101Annotations.Zero), "P2 mark F101_ZERO");
            yield return WaitForApplied(casework.ToggleF101MarkAsync(ObservationRegistry.Bed4Card, F101Annotations.Receiver), "P2 mark OBS101B_SIGN");

            // Negative 1: annotationVersion == 0 bypass in CH01 MUST be rejected by EvaluateF101LiveCommit!
            var zeroVerSub = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.F101Sheet, ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimUnreliableZero,
                annotationVersion = 0
            };
            Require(casework.SubmitF101(FormStatus.Refuted, zeroVerSub, player.Feet, out var zeroVerEval) == CommitResult.PadRequired &&
                    !zeroVerEval.Accepted,
                "V36-CH01-Q02: CH01 live commit blocks annotationVersion==0 bypass");

            // Negative 2: UI-only link call MUST be rejected in CH01!
            var linkFrom = new EvidenceAnnotation(ObservationRegistry.Bed4Card, F101Annotations.Receiver);
            var linkTo = new EvidenceAnnotation(ObservationRegistry.Bed4Traces, F101Annotations.TraceGroup);
            var uiLinkTask = casework.SetF101LinkFromUiAsync(linkFrom, linkTo);
            while (!uiLinkTask.IsCompleted) yield return null;
            Require(uiLinkTask.Result == CommitResult.PadRequired && !State.f101Annotations.HasLink,
                "V36-CH01-Q02: SetF101LinkFromUiAsync fails closed in CH01");

            // Negative 3: Spatial link from wrong location (doorway AP04 x=6.52, outside Bed-4 alcove) MUST be rejected!
            Require(casework.SetF101LinkSpatial(linkFrom, linkTo, scene.Pad("AP04").position) == CommitResult.PadRequired &&
                    !State.f101Annotations.HasLink,
                "V36-CH01-Q02: SetF101LinkSpatial rejects linking from AP04 doorway away from Bed 4 alcove");

            // Negative 4 + REAL FAULT PROBE (v36-f101-unverified-link):
            // Injected unverified link (no CH01-F101-LINK-SPATIAL event) MUST be rejected by EvaluateF101LiveCommit!
            var unverifiedState = State.Copy();
            unverifiedState.f101Annotations.linkFrom = linkFrom;
            unverifiedState.f101Annotations.linkTo = linkTo;
            var unverifiedSub = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.F101Sheet, ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimUnreliableZero,
                annotationVersion = F101Annotations.Version,
                annotations = unverifiedState.f101Annotations.Clone()
            };
            HypothesisEvaluation unverifiedEval;
            try
            {
                HypothesisService.QaInjectBypassLinkProvenanceForTesting = probe == "v36-f101-unverified-link";
                unverifiedEval = HypothesisService.EvaluateF101LiveCommit(unverifiedState, FormStatus.Refuted, unverifiedSub);
            }
            finally
            {
                HypothesisService.QaInjectBypassLinkProvenanceForTesting = false;
            }
            Require(!unverifiedEval.Accepted && unverifiedEval.Outcome == HypothesisOutcome.InvalidProvenance &&
                    F101Annotations.ResolveLinkProvenance(unverifiedState, unverifiedSub) == F101LinkProvenance.Unverified,
                "V36-CH01-Q02 backend rejects unverified F101 link without SpatialFieldVerified provenance");

            // Positive: Real spatial link at Bed 4 alcove succeeds, sets SpatialFieldVerified, and unmarking OBS101B clears it!
            Vector3 bed4Alcove = new(11.10f, 0.015f, 3.60f);
            Apply(casework.SetF101LinkSpatial(linkFrom, linkTo, bed4Alcove), "P2 spatial Bed-4 link");
            Require(State.f101Annotations.HasLink &&
                    F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.SpatialFieldVerified,
                "V36-CH01-Q02: SetF101LinkSpatial at Bed 4 commits SpatialFieldVerified provenance");

            // Unmark OBS101B -> MUST automatically strip SpatialLinkEvent!
            yield return WaitForApplied(casework.ToggleF101MarkAsync(ObservationRegistry.Bed4Card, F101Annotations.Receiver), "P2 unmark OBS101B");
            Require(!State.f101Annotations.HasLink && !State.Happened(F101Annotations.SpatialLinkEvent),
                "V36-CH01-Q02: unmarking OBS101B clears SpatialLinkEvent automatically");
            yield return WaitForApplied(casework.ToggleF101MarkAsync(ObservationRegistry.Bed4Card, F101Annotations.Receiver), "P2 re-mark OBS101B");
            Apply(casework.SetF101LinkSpatial(linkFrom, linkTo, bed4Alcove), "P2 re-link spatially");

            // Revise B -> C with valid spatial provenance + idempotent duplicate submit!
            var validCSub = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.F101Sheet, ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimUnreliableZero,
                annotationVersion = F101Annotations.Version,
                annotations = State.f101Annotations.Clone()
            };
            Apply(casework.SubmitF101(FormStatus.Refuted, validCSub, player.Feet, out var evalC), "P2 F101 revision B->C");
            Require(evalC.Accepted && State.f101.firstDecision == FormStatus.Affirmed &&
                    State.f101.current == FormStatus.Refuted && State.f101.revision == 3 &&
                    FormService.Distortion(State) == 0 && !State.Has("E12") && State.evidence.Count == 1,
                "V36-CH01-Q05: A->B->C commits cleanly (first=A, current=C, rev=3, D=0, no E11/E12)");
            long seqAtRev3 = State.sequence;
            Apply(casework.SubmitF101(FormStatus.Refuted, validCSub, player.Feet, out _), "P2 duplicate C submit");
            Require(State.f101.revision == 3 && State.sequence == seqAtRev3,
                "V36-CH01-Q05: duplicate C submission writes no revision or save");

            // Legacy committed save compatibility check (ValidPersistedF101 still accepts pre-Rebirth-III Refuted saves!)
            var legacyCommitted = State.Copy();
            legacyCommitted.events.Remove(F101Annotations.SpatialLinkEvent);
            legacyCommitted.f101Argument.annotationVersion = 0;
            legacyCommitted.f101Argument.annotations = null;
            foreach (var h in legacyCommitted.f101History)
            {
                h.annotationVersion = 0;
                h.annotations = null;
            }
            Require(HypothesisService.ValidPersistedF101(legacyCommitted) &&
                    F101Annotations.ResolveLinkProvenance(legacyCommitted) == F101LinkProvenance.LegacyCommittedSave,
                "V36-CH01-Q05: ValidPersistedF101 preserves 100% compatibility with legacy committed Refuted saves");

            // --- 4) V36-CH01-Q03: EX01 Free HH:MM Manual Entry + REAL v36-ex01-autosolve fault probe ---
            Apply(casework.Observe(ObservationRegistry.StoppedClock0317, player.Feet, "LOC02"), "P2 EX01 clock");
            Apply(casework.Observe(ObservationRegistry.ShiftGuide, player.Feet, "STAFF"), "P2 EX01 shift context");
            try
            {
                CorroborationService.QaInjectEx01AutoSolveForTesting = probe == "v36-ex01-autosolve";
                yield return WaitForApplied(casework.MarkObservationDetailAsync(ObservationRegistry.Bed4Card, CorroborationService.RakingDetailId, flippedBack: true),
                    "P2 EX01 raking indent");
            }
            finally
            {
                CorroborationService.QaInjectEx01AutoSolveForTesting = false;
            }
            Require(CorroborationService.CanCorroborate0317(State) && !CorroborationService.IsCorroborated(State),
                "V36-CH01-Q03 EX01 never auto-solves when all 3 sources are discovered");

            // All blank, wrong, and future-timeline timecodes MUST fail closed!
            string[] invalidTimes = { "", "--:--", "00:00", "02:50", "03:05", "03:16", "03:18", "07:40", "24:00", "99:99" };
            foreach (string bad in invalidTimes)
            {
                Require(casework.SubmitEx01Timecode(player.Feet, bad) == CommitResult.PadRequired &&
                        !CorroborationService.IsCorroborated(State),
                    $"V36-CH01-Q03: EX01 rejects invalid/non-matching timecode '{bad}'");
            }
            Apply(casework.SubmitEx01Timecode(player.Feet, "03:17"), "P2 EX01 submit 03:17");
            Require(CorroborationService.IsCorroborated(State) &&
                    State.exception0317Link.left == ObservationRegistry.StoppedClock0317 &&
                    State.exception0317Link.right == ObservationRegistry.Bed4Card &&
                    State.exception0317Link.context == ObservationRegistry.ShiftGuide,
                "V36-CH01-Q03: EX01 commits cleanly on player-entered '03:17' with full source link");

            // --- 5) V36-CH01-Q06: 100% Zero-Journal Lock in CH01 + 3D AN02 Clipboard Esc Draft Preservation + CH02 Restore ---
            Require(!IsJournalAllowedOnAn01 && !IsJournalAllowedInBackpack,
                "V36-CH01-Q06: Journal is 100% locked across CH01 B04-B05");
            OpenAn02Clipboard3D();
            Require(IsAn02Clipboard3DActive && ui.IsModal, "V36-CH01-Q06: 3D AN02 clipboard opens cleanly with camera control");
            reportDraft.Decision = FormStatus.Unverified;
            ExitAn02Clipboard3D(clearDraft: false);
            Require(!IsAn02Clipboard3DActive && !ui.IsModal && reportDraft.Decision == FormStatus.Unverified,
                "V36-CH01-Q06: Exiting 3D AN02 clipboard via Esc restores FPS camera while preserving F101Draft");
            reportDraft = new F101Draft();

            // Verify Socket1..4 become ACTIVE in Layer R + CH02 restores full 5-tab Journal & UI revision!
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P2 B->R T08");
            player.Teleport(scene.Pad("AP04"));
            yield return null;
            var loc03RegionR = Array.Find(FindObjectsByType<ZoneRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                z => z.zoneId == "LOC03");
            Require(loc03RegionR != null, "P2 LOC03 ZoneRegion present in Layer R");
            int socketsActiveInR = 0;
            foreach (var t in loc03RegionR.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Socket", StringComparison.Ordinal) && t.name.Length == 7 && char.IsDigit(t.name[6]) && t.gameObject.activeInHierarchy)
                    socketsActiveInR++;
            Require(socketsActiveInR == 4,
                $"DEC-V361-CH01-01: All 4 numbered bed outlet plaques Socket1..4 appear in Layer R for CH02 PZ01 (active={socketsActiveInR})");
            player.Teleport(scene.Pad("AP01"));
            Apply(casework.OutsideAttempt(player.Feet), "P2 T09");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P2 CH02 handoff");
            Require(State.chapter == "CH02" && IsJournalAllowedOnAn01 && IsJournalAllowedInBackpack,
                "V36-CH01-Q06: CH02 restores full 5-tab Journal");
            var ch02UiLink = casework.SetF101LinkFromUiAsync(linkFrom, linkTo);
            while (!ch02UiLink.IsCompleted) yield return null;
            Require(ch02UiLink.Result == CommitResult.Applied &&
                    (F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.SpatialFieldVerified ||
                     F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.LegacyUi),
                "V36-CH01-Q05: CH02 permits F101 UI revision link cleanly");

            // Restore pre-suite snapshot
            Apply(transaction.Commit(savedBefore, savedPos, warp: false, geometryChanges: true), "P2 restore pre-suite snapshot");
            player.Teleport(savedPos, player.transform.rotation);
            verification.Add("CH01 REBIRTH III PHASE 2 PASS: DEC-V361-CH01-01 (0 wheel marks in B / Socket1..4 R-only / FS22 EEG hum / CHỜ KIỂM TRA), D5 (OBS101B hidden behind deniedBay + A->B restore 0 auto-OBS), V36-CH01-Q02/Q05 (4-state F101LinkProvenance + real v36-f101-unverified-link probe + A->B->C + legacy save & CH02 compat), V36-CH01-Q03 (free HH:MM EX01 + real v36-ex01-autosolve probe), and V36-CH01-Q06 (3D AN02 clipboard + 100% CH01 zero-Journal lock).");
        }
    }
}
