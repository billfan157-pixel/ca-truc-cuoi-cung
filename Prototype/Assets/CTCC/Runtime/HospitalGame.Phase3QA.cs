using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        /// <summary>
        /// CH01 Rebirth III — Phase 3 Verification Suite (V36-CH01-Q04, Q06, Q07, Q08 + B08 Echo + v36-wr05-bypass):
        /// Verifies EVERY Phase 3 exit criterion on the live built scenes &amp; runtime systems:
        /// - V36-CH01-Q04 (B06): B->R atomic reconciliation across ALL 4 Layer-B item states (Equipped in hand,
        ///   InBag, Placed on AN02_CLIP, Placed on desk), save-lock rollback preserving all 4 states, outer gate
        ///   staying OPEN post-T08 (zero relock), >= 120s flashlight floor, and CP03 save/load parity.
        /// - V36-CH01-Q07 (B07 / WR05): One-time scripted T09 seam vs post-T09 WR05 swept-boundary projection
        ///   across ALL 5 mandatory anti-sequence-break scenarios (straight/diagonal sprint hugging both pillars,
        ///   low-FPS dt=0.045s 0.189m/frame step, save/load near threshold, both Reality R &amp; B layers, and
        ///   architectural detours S1/fences); zero seamBlocker collider, zero backward +Z push, free +/-X &amp; look,
        ///   CH01_ExitBoundaryHum + release decay, REAL fault probe 'v36-wr05-bypass', and pure unit test of
        ///   Wr05ExitBoundaryFilter.Evaluate(..., g08Open: true) (honestly separated from G08 Release E2E).
        /// - B08 Perceptual Echo: 3-branch F101 visual echo at LOC02 R (Affirmed/Unverified/Refuted), zero colliders,
        ///   zero evidence/observation grants, Reduced Motion static mode, CH01_B08EchoWhisper cue, clean hide on T10,
        ///   CP04 handoff (CH02, R, G01=true, G02=false), and V36-CH01-Q06 0-Journal CH01 -> restored CH02 Journal.
        /// - V36-CH01-Q08 (Cross-Chapter): CH05-D08 blocked without publicReportSummary -> H08 Recovery at LOC08 R
        ///   placing E10A_ENVELOPE &amp; MINH_PHONE on LOC08_RECOVERY_DESK (zero auto-read, strict 4-part independence,
        ///   rejected on CH01 surfaces) -> newspaper unlocks CH05-D08 WITHOUT E09 (INNER_SUB_SILENT, 0 VO) -> 4/4 read.
        /// </summary>
        private IEnumerator VerifyCh01Rebirth3Phase3Suite()
        {
            while (busy || savingDialogue) yield return null;
            string probe = Argument("-ctcc-qa-probe");
            var savedBefore = State.Copy();
            var savedPos = player.Feet;
            bool canCapture = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && !string.IsNullOrEmpty(qaFolder);

            var settings = new VisitSettings
            {
                volume = 0f,
                sfxVolume = 1f,
                ambienceVolume = 0.85f,
                reduceMotion = true,
                captions = true,
                textScale = 1.0f
            };

            // =========================================================================
            // 1) V36-CH01-Q04: B06 COLLAPSE & RETURN — ALL 4 ITEM STATES + ROLLBACK + OPEN GATE
            // =========================================================================
            Apply(casework.Begin(scene.Pad("AP00").position, settings), "P3 Q04 begin");
            scene.interactables?.ResetSessionProps();
            Apply(casework.TakeMailboxPackage(scene.Pad("AP00").position, equipGateKey: true), "P3 Q04 mailbox");
            Apply(casework.OpenGate(scene.Pad("AP00").position, BackpackCatalog.GateKey), "P3 Q04 T02");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P3 Q04 R->B T03");
            Apply(casework.Board(player.Feet), "P3 Q04 T05");
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.EnterRoom(player.Feet), "P3 Q04 T06");

            // Observe & place Layer-B papers across ALL 4 physical states simultaneously:
            // (1) Placed on AN02_CLIP, (2) Placed on WARD101_TABLE_L desk, (3) Equipped in hand, (4) InBag!
            Apply(casework.Observe(ObservationRegistry.Bed4Card, player.Feet, "LOC03"), "P3 Q04 obs 101B");
            Apply(casework.Observe(ObservationRegistry.Bed2Transfer, player.Feet, "LOC03"), "P3 Q04 obs 101C");
            Apply(casework.Observe(ObservationRegistry.Bed3Meds, player.Feet, "LOC03"), "P3 Q04 obs 101D");
            Apply(casework.Observe(ObservationRegistry.ShiftGuide, player.Feet, "STAFF"), "P3 Q04 obs shift");
            yield return WaitForApplied(casework.AcquireItemAsync(BackpackCatalog.PaperObs101B, "LOC03"), "P3 Q04 acq 101B");
            yield return WaitForApplied(casework.AcquireItemAsync(BackpackCatalog.PaperObs101C, "LOC03"), "P3 Q04 acq 101C");
            yield return WaitForApplied(casework.AcquireItemAsync(BackpackCatalog.PaperObs101D, "LOC03"), "P3 Q04 acq 101D");
            yield return WaitForApplied(casework.AcquireItemAsync(BackpackCatalog.PaperShiftLog, "STAFF"), "P3 Q04 acq shift");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.PaperObs101B, WorldAnchors.An02CompareClip), "P3 Q04 place AN02");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.PaperObs101C, WorldAnchors.Ward101TableLeft), "P3 Q04 place desk");
            yield return WaitForApplied(casework.EquipItemAsync(BackpackCatalog.PaperShiftLog), "P3 Q04 equip shift");
            // PaperObs101D & BoardAssignment remain InBag.
            handPresenter?.SyncState(State, scene, hideViewModel: false);

            Require(InventoryService.GetPlacedAt(State, WorldAnchors.An02CompareClip)?.itemDefId == BackpackCatalog.PaperObs101B &&
                    InventoryService.GetPlacedAt(State, WorldAnchors.Ward101TableLeft)?.itemDefId == BackpackCatalog.PaperObs101C &&
                    InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.PaperShiftLog &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperObs101D)?.status == BackpackItemStatus.InBag &&
                    InventoryService.ValidState(State),
                "V36-CH01-Q04 all 4 Layer-B paper states (Equipped/InBag/AN02_CLIP/Desk) staged cleanly before F101");

            // Commit F101 (Refuted with full spatial provenance) -> return to AP02 for B06 collapse
            var refSub = PrepareSpatialCh01RefutationSubmission();
            Apply(casework.SubmitF101(FormStatus.Refuted, refSub, player.Feet, out var evalC), "P3 Q04 F101 Refuted");
            Require(evalC.Accepted && GateService.G01(State), "V36-CH01-Q04 F101 C accepted opening G01");

            player.Teleport(scene.Pad("AP02"));
            // Adversarial save-lock rollback during B06 T08 collapse MUST preserve all 4 states & stay in B!
            long seqBeforeB06Fail = State.sequence;
            using (var lockFile = new FileStream(saves.SlotPath(SaveSlot.Auto) + ".lock",
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                Require(casework.AtHub(player.Feet, out _) == CommitResult.SaveUnavailable,
                    "V36-CH01-Q04 B06 T08 returns SaveUnavailable when Auto save is locked");
            }
            handPresenter?.SyncState(State, scene, hideViewModel: false);
            Require(State.layer == Reality.B && !State.Happened("CH01-T08") && State.sequence == seqBeforeB06Fail &&
                    InventoryService.GetPlacedAt(State, WorldAnchors.An02CompareClip)?.itemDefId == BackpackCatalog.PaperObs101B &&
                    InventoryService.GetPlacedAt(State, WorldAnchors.Ward101TableLeft)?.itemDefId == BackpackCatalog.PaperObs101C &&
                    InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.PaperShiftLog &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperObs101D)?.status == BackpackItemStatus.InBag &&
                    handPresenter.PlacedAn02ItemDefId == BackpackCatalog.PaperObs101B &&
                    handPresenter.EquippedItemDefId == BackpackCatalog.PaperShiftLog,
                "V36-CH01-Q04 save-failure rollback preserves Layer B, sequence, and all 4 physical item states + visuals");

            // Now commit B06 T08 cleanly!
            Apply(casework.AtHub(player.Feet, out _), "P3 Q04 B06 T08 collapse");
            player.RestoreBattery(State.torchSeconds);
            handPresenter?.SyncState(State, scene, hideViewModel: false);

            Require(State.layer == Reality.R && State.Happened("CH01-T08") && State.checkpoint == "CH01-CP03" &&
                    State.evidence.Count == 1 && State.Has("E10a") &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperObs101B)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperObs101C)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperObs101D)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperShiftLog)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.FindByDef(State, BackpackCatalog.BoardAssignment)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.GetEquipped(State) == null &&
                    InventoryService.GetAllPlaced(State).Count == 0 &&
                    string.IsNullOrEmpty(handPresenter.PlacedAn02ItemDefId) &&
                    string.IsNullOrEmpty(handPresenter.EquippedItemDefId) &&
                    InventoryService.ValidState(State),
                "V36-CH01-Q04 T08 atomically reconciles ALL 4 Layer-B states to MemoryRef with zero R export");

            // Outer gate MUST stay OPEN after T08 (Core 04 canon fix — zero b07GateLatched!)
            Require(scene.outerGate != null && scene.outerGate.isOpen,
                "V36-CH01-Q04 outer gate stays OPEN after T08 collapse (never relocks against canon)");
            Require(player.BatterySeconds >= 120, "V36-CH01-Q04 B06 guarantees >= 120s flashlight floor");

            // Save/Load CP03 round-trip preserves R, MemoryRef reconciliation, and OPEN outer gate!
            Require(transaction.Restore(SaveSlot.Auto, out var cp03Pad) == SaveRead.Valid && cp03Pad != null &&
                    State.layer == Reality.R && State.checkpoint == "CH01-CP03" &&
                    scene.outerGate != null && scene.outerGate.isOpen &&
                    InventoryService.GetAllPlaced(State).Count == 0 && InventoryService.ValidState(State),
                "V36-CH01-Q04 CH01-CP03 autosave reload restores Reality R, MemoryRefs, and OPEN outer gate");

            if (canCapture)
            {
                player.SetTorchEnabled(true);
                yield return CapturePhase3Frame("p3-b06-collapse-return-R",
                    new Vector3(3.65f, 0.015f, -3.24f), new Vector3(2.02f, 1.38f, -3.24f));
                player.SetTorchEnabled(false);
            }

            // =========================================================================
            // 2) V36-CH01-Q07: B07 T09 SEAM VS WR05 SWEPT-BOUNDARY + REAL PROBE v36-wr05-bypass
            // =========================================================================
            // Pre-T09: WR05 is NOT active yet, and stepping across the open gate triggers pre-T09 seam predicate!
            Require(!Wr05ExitBoundaryFilter.IsActive(State) &&
                    Wr05ExitBoundaryFilter.IsCrossingPreT09Seam(State, new Vector3(4.0f, 0.015f, -19.96f)),
                "V36-CH01-Q07 pre-T09 distinguishes scripted one-time gate seam from post-T09 WR05 boundary");

            // Commit one-time scripted T09 seam -> warps to AP01
            player.Teleport(new Vector3(4.0f, 0.015f, -19.2f), Quaternion.Euler(0f, 180f, 0f));
            Apply(casework.OutsideAttempt(player.Feet), "P3 Q07 scripted T09 seam");
            player.Teleport(scene.Pad("AP01"));

            Require(State.Happened("CH01-T09") &&
                    casework.OutsideAttempt(player.Feet) != CommitResult.Applied &&
                    scene.outerGate != null && scene.outerGate.isOpen &&
                    (scene.seamBlocker == null || !scene.seamBlocker.activeSelf) &&
                    !GateService.G08(State) && Wr05ExitBoundaryFilter.IsActive(State),
                "V36-CH01-Q07 T09 fires once, keeps outer gate physically open, disables seamBlocker, and activates WR05 via real GateService.G08");

            // Verify all 5 mandatory anti-sequence-break scenarios (with real fault-injection probe hook!)
            bool injectWr05Bypass = probe == "v36-wr05-bypass";
            Wr05ExitBoundaryFilter.QaInjectBypassForTesting = injectWr05Bypass;
            try
            {
                // Scenario 1: Straight & Diagonal Sprint hugging BOTH gate pillars inside clear doorway lane (x in [3.15..4.85])
                foreach (var (xStart, dir, label) in new (float, Vector3, string)[]
                {
                    (4.00f, new Vector3(0f, 0f, -4.2f), "center straight sprint"),
                    (3.40f, new Vector3(-0.95f, 0f, -4.09f), "west pillar diagonal sprint"),
                    (4.60f, new Vector3(0.95f, 0f, -4.09f), "east pillar diagonal sprint"),
                })
                {
                    player.Teleport(new Vector3(xStart, 0.015f, -19.35f), Quaternion.Euler(0f, 180f, 0f));
                    float prevZ = player.Feet.z;
                    bool everEngaged = false;
                    for (int step = 0; step < 18; step++)
                    {
                        player.Move(dir, 0.025f);
                        if (player.Wr05EngagedThisFrame) everEngaged = true;
                        // NEVER bounce player backward (+Z) by more than 0.01m (no NudgeWithinScene!)
                        Require(player.Feet.z <= prevZ + 0.01f,
                            $"V36-CH01-Q07 WR05 {label} never pushes player backward (+Z) (prev={prevZ:F3}, cur={player.Feet.z:F3})");
                        prevZ = player.Feet.z;
                    }
                    Require(player.Feet.z >= Wr05ExitBoundaryFilter.ExitThresholdZ - 0.03f && everEngaged,
                        $"V36-CH01-Q07 WR05 blocks {label} at z>=-19.95 without overshoot (z={player.Feet.z:F3}, engaged={everEngaged})");
                }

                // Verify free lateral +/-X slide while pressed against threshold!
                player.Teleport(new Vector3(4.00f, 0.015f, -19.94f), Quaternion.Euler(0f, 180f, 0f));
                float beforeSlideX = player.Feet.x;
                for (int i = 0; i < 6; i++)
                    player.Move(new Vector3(2.2f, 0f, -3.2f), 0.025f);
                Require(player.Feet.x > beforeSlideX + 0.20f && player.Feet.z >= Wr05ExitBoundaryFilter.ExitThresholdZ - 0.03f,
                    $"V36-CH01-Q07 WR05 allows free +/-X lateral slide along gate threshold (dx={player.Feet.x - beforeSlideX:F3}, z={player.Feet.z:F3})");

                // Scenario 2: Low-FPS worst-case frame step (dt = 0.045s -> 0.189m/frame sprint step across z=-19.95!)
                player.Teleport(new Vector3(4.00f, 0.015f, -19.85f), Quaternion.Euler(0f, 180f, 0f));
                player.Move(new Vector3(0f, 0f, -4.2f), 0.045f);
                Require(player.Feet.z >= Wr05ExitBoundaryFilter.ExitThresholdZ - 0.025f && player.Wr05EngagedThisFrame,
                    $"V36-CH01-Q07 WR05 low-FPS dt=0.045s sprint step (0.189m) has zero overshoot (z={player.Feet.z:F3})");

                // Scenario 3: Save/Load near threshold preserves WR05 lock!
                Require(transaction.Restore(SaveSlot.Auto, out _) == SaveRead.Valid && Wr05ExitBoundaryFilter.IsActive(State),
                    "V36-CH01-Q07 save/load preserves WR05 active lock");
                player.Teleport(new Vector3(4.00f, 0.015f, -19.86f), Quaternion.Euler(0f, 180f, 0f));
                player.Move(new Vector3(0f, 0f, -4.2f), 0.045f);
                Require(player.Feet.z >= Wr05ExitBoundaryFilter.ExitThresholdZ - 0.025f,
                    $"V36-CH01-Q07 post-reload WR05 blocks exit at threshold (z={player.Feet.z:F3})");

                // Scenario 4: Layer B cross-layer switch (CH02 R/B swap) MUST STILL enforce WR05!
                var layerBProbe = State.Copy();
                layerBProbe.chapter = "CH02";
                layerBProbe.layer = Reality.B;
                var bFilterRes = Wr05ExitBoundaryFilter.Filter(layerBProbe, new Vector3(4.0f, 0.015f, -19.85f), new Vector3(0f, 0f, -0.189f));
                Require(Wr05ExitBoundaryFilter.IsActive(layerBProbe) && bFilterRes.Blocked &&
                        (-19.85f + bFilterRes.AdjustedDelta.z) >= Wr05ExitBoundaryFilter.ExitThresholdZ - 0.001f,
                    "V36-CH01-Q07 WR05 blocks exit in BOTH Reality R and Reality B (closes CH02 Layer B bypass)");

                // Scenario 5: Architectural detours (Front fences west/east of gate & S1 discharge door in R remain solid!)
                bool westWallBlocked = Physics.CapsuleCast(new Vector3(1.5f, 0.46f, -19.2f), new Vector3(1.5f, 1.40f, -19.2f),
                    HospitalScene.Radius, Vector3.back, out _, 1.5f, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
                bool eastWallBlocked = Physics.CapsuleCast(new Vector3(6.5f, 0.46f, -19.2f), new Vector3(6.5f, 1.40f, -19.2f),
                    HospitalScene.Radius, Vector3.back, out _, 1.5f, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
                bool s1RBlocked = scene.s1DischargeSalvage == null || scene.s1DischargeSalvage.activeInHierarchy;
                Require(westWallBlocked && eastWallBlocked && s1RBlocked,
                    $"V36-CH01-Q07 architectural detours blocked (westWall={westWallBlocked}, eastWall={eastWallBlocked}, s1SalvageR={s1RBlocked})");
            }
            finally
            {
                Wr05ExitBoundaryFilter.QaInjectBypassForTesting = false;
            }

            // Input release immediately clears Wr05EngagedThisFrame & decays CH01_ExitBoundaryHum cleanly!
            var humClip = Resources.Load<AudioClip>("CTCC/CH01_ExitBoundaryHum");
            Require(humClip != null && humClip.samples > 0, "V36-CH01-Q07 CH01_ExitBoundaryHum.wav asset loaded cleanly");
            scene.atmosphere?.SetExitBoundaryHum(true, new Vector3(4f, 1.35f, -20.0f));
            yield return null;
            player.Move(Vector3.zero, 0.02f);
            scene.atmosphere?.SetExitBoundaryHum(false, new Vector3(4f, 1.35f, -20.0f));
            Require(!player.Wr05EngagedThisFrame, "V36-CH01-Q07 releasing outward input clears WR05 engagement immediately");

            // Pure unit test of Wr05ExitBoundaryFilter allow-path when g08Open==true (explicitly NOT a G08 E2E claim!).
            var pureAllow = Wr05ExitBoundaryFilter.Evaluate(new Vector3(4.0f, 0.015f, -19.85f), new Vector3(0f, 0f, -0.25f),
                t09Happened: true, g08Open: true);
            Require(!pureAllow.Blocked && Mathf.Approximately(pureAllow.AdjustedDelta.z, -0.25f) && !GateService.G08(State),
                "V36-CH01-Q07 pure unit allow-path passes when g08Open=true while real GateService.G08 stays false in CH01-CH05");

            if (canCapture)
            {
                player.SetTorchEnabled(true);
                yield return CapturePhase3Frame("p3-b07-wr05-gate-R",
                    new Vector3(4.0f, 0.015f, -16.85f), new Vector3(4.0f, 1.15f, -20.6f));
                player.SetTorchEnabled(false);
            }

            // =========================================================================
            // 3) B08 PERCEPTUAL ECHO (ALL 3 F101 BRANCHES + REDUCED MOTION + T10/CP04 + Q06)
            // =========================================================================
            Require(scene.B08Echo != null && !scene.B08Echo.HasAnyCollider,
                "B08 Echo controller present in LOC02 with strictly ZERO solid/trigger colliders");
            var echoWhisperClip = Resources.Load<AudioClip>("CTCC/CH01_B08EchoWhisper");
            Require(echoWhisperClip != null && echoWhisperClip.samples > 0, "B08 CH01_B08EchoWhisper.wav loaded cleanly");

            int evCountBeforeEcho = State.evidence.Count;
            int obsCountBeforeEcho = State.observed.Count;

            player.Teleport(new Vector3(3.16f, 0.015f, -3.24f),
                Quaternion.LookRotation(new Vector3(2.02f, 1.44f, -3.24f) - new Vector3(3.16f, 1.62f, -3.24f)));

            foreach (var (branch, shotName) in new (FormStatus, string)[]
            {
                (FormStatus.Affirmed, "p3-b08-echo-affirmed-R"),
                (FormStatus.Unverified, "p3-b08-echo-unverified-R"),
                (FormStatus.Refuted, "p3-b08-echo-refuted-R"),
            })
            {
                State.f101.current = branch;
                scene.B08Echo.Sync(State, player.Eye.transform, scene.atmosphere, reducedMotion: true, instantForQa: true);
                Require(scene.B08Echo.IsManifesting && scene.B08Echo.ActiveBranch == branch &&
                        scene.B08Echo.ReducedMotionApplied && !scene.B08Echo.HasAnyCollider,
                    $"B08 Perceptual Echo manifests branch {branch} in LOC02 R with Reduced Motion static support & 0 colliders");
                if (canCapture)
                {
                    yield return CapturePhase3Frame(shotName,
                        new Vector3(3.26f, 0.015f, -3.24f), new Vector3(2.02f, 1.48f, -3.24f));
                }
            }

            // Echo NEVER creates physical evidence or observation records!
            Require(State.evidence.Count == evCountBeforeEcho && State.observed.Count == obsCountBeforeEcho,
                "B08 Perceptual Echo is strictly perceptual (zero evidence & zero observation grants)");

            // V36-CH01-Q06: Journal strictly locked before T10 -> unlocked immediately upon T10/CP04 CH02 handoff!
            Require(!IsJournalAllowedInBackpack && !IsJournalAllowedOnAn01,
                "V36-CH01-Q06 5-tab Journal stays 100% locked throughout CH01 up to T10");

            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P3 B08 T10 CH02 handoff");
            scene.B08Echo.Sync(State, player.Eye.transform, scene.atmosphere, reducedMotion: true, instantForQa: true);
            Require(State.chapter == "CH02" && State.layer == Reality.R && State.checkpoint == "CH01-CP04" &&
                    State.Happened("CH01-T10") && GateService.G01(State) && !GateService.G02(State) &&
                    !scene.B08Echo.IsManifesting && !scene.B08Echo.gameObject.activeSelf &&
                    IsJournalAllowedInBackpack && IsJournalAllowedOnAn01 && casework.CanRevise,
                "B08 T10 commits CP04 (CH02, R, G01=true, G02=false), dismisses B08 Echo, and restores CH02 Journal & F101 revision");

            // =========================================================================
            // 4) V36-CH01-Q08: CROSS-CHAPTER CH05-D08 GUARD & LOC08 R H08 RECOVERY
            // =========================================================================
            // Ensure LOC05_ArchiveWing is loaded so LOC08 desk & blotter are live for 3D recovery capture!
            if (scene.IsAdditiveMode && !scene.HasZone("LOC05"))
                yield return scene.LoadZoneAsync(RegionStreamer.Upper);

            // Seed adversarial CH05 state from the REAL organically earned CH05-CP03 snapshot (savedBefore),
            // stripping ONLY E09 (DutyLog) and all 4 E10a read flags (0/4) while preserving G01..G05 & physical envelope/phone!
            var q08State = savedBefore.Copy();
            q08State.layer = Reality.R;
            q08State.evidence.Remove(EvidenceRegistry.DutyLog); // Explicitly NO E09!
            foreach (string flag in new[] { E10aParts.Letter, E10aParts.SignedAppointment, E10aParts.PostponementSms,
                E10aParts.PublicReportSummary, E10aParts.MailboxNoteViewed, E10aParts.SmsAppointmentViewed, "E10a.SMSRead" })
                q08State.events.Remove(flag);
            q08State.Remember(ChapterFive.FlagValveSabotage);
            InventoryService.EnsureDefaultTools(q08State);
            InventoryService.TryAcquire(q08State, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out _);
            Apply(transaction.Commit(q08State, scene.Pad("AP11").position, warp: false, geometryChanges: true), "P3 Q08 seed CH05 0/4 E10a");
            handPresenter?.SyncState(State, scene, hideViewModel: false);

            // 4.1) CH05-D08 MUST be blocked when ValveSabotageFound=true but publicReportSummary=false!
            Require(State.Happened(ChapterFive.FlagValveSabotage) && E10aParts.ReadCount(State) == 0 &&
                    !MayDeliver("CH05-D08", "B06"),
                "V36-CH01-Q08 CH05-D08 strictly blocked when E10a.Part.publicReportSummary is unread");

            // 4.2) Envelope & Phone CANNOT be placed on CH01 surfaces, ONLY on LOC08_RECOVERY_DESK in CH05 R!
            Require(!InventoryService.TryPlaceAtAnchor(State.Copy(), BackpackCatalog.E10aEnvelope, WorldAnchors.An02CompareClip) &&
                    !InventoryService.TryPlaceAtAnchor(State.Copy(), BackpackCatalog.MinhPhone, WorldAnchors.ReceptionDeskLeft),
                "V36-CH01-Q08 E10A_ENVELOPE and MINH_PHONE remain rejected on CH01/CH02 surfaces");

            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.E10aEnvelope, WorldAnchors.Loc08DeskEnvelope), "P3 Q08 place envelope on LOC08 desk");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.MinhPhone, WorldAnchors.Loc08DeskPhone), "P3 Q08 place phone on LOC08 desk");
            handPresenter?.SyncState(State, scene, hideViewModel: false);

            // Placing on LOC08 desk NEVER auto-reads any of the 4 E10a parts!
            Require(InventoryService.GetPlacedAt(State, WorldAnchors.Loc08DeskEnvelope)?.itemDefId == BackpackCatalog.E10aEnvelope &&
                    InventoryService.GetPlacedAt(State, WorldAnchors.Loc08DeskPhone)?.itemDefId == BackpackCatalog.MinhPhone &&
                    InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.MinhPhone) &&
                    InventoryService.ValidState(State) &&
                    E10aParts.ReadCount(State) == 0,
                "V36-CH01-Q08 placing E10A_ENVELOPE & MINH_PHONE on LOC08_RECOVERY_DESK passes ValidState with strictly 0/4 auto-read");

            if (canCapture)
            {
                player.SetTorchEnabled(true);
                yield return CapturePhase3Frame("p3-loc08-h08-recovery-R",
                    new Vector3(5.68f, 8.015f, 31.78f), new Vector3(6.81f, 8.92f, 31.78f));
                player.SetTorchEnabled(false);
            }

            // 4.3) Read each of the 4 E10a parts independently -> verify zero cross-bleed & CH05-D08 unlock WITHOUT E09!
            yield return WaitForApplied(casework.RecordE10aPartReadAsync(E10aParts.PublicReportSummary), "P3 Q08 read newspaper at LOC08");
            Require(E10aParts.ReadCount(State) == 1 && !State.Has(EvidenceRegistry.DutyLog) &&
                    MayDeliver("CH05-D08", "B06") && !MayDeliver("CH05-D08", "B07") && !voice.HasClip("CH05-D08"),
                "V36-CH01-Q08 reading publicReportSummary unlocks CH05-D08 at B06 WITHOUT E09 (INNER_SUB_SILENT, 0 VO) while keeping other 3 parts unread (1/4)");

            yield return WaitForApplied(casework.RecordE10aPartReadAsync(E10aParts.Letter), "P3 Q08 read Lan letter at LOC08");
            Require(E10aParts.ReadCount(State) == 2 && !State.Happened(E10aParts.SignedAppointment) && !State.Happened(E10aParts.PostponementSms),
                "V36-CH01-Q08 reading letter advances strictly to 2/4 with zero auto-complete");

            yield return WaitForApplied(casework.RecordE10aPartReadAsync(E10aParts.PostponementSms), "P3 Q08 read 22:48 SMS at LOC08");
            Require(E10aParts.ReadCount(State) == 3 && !State.Happened(E10aParts.SignedAppointment),
                "V36-CH01-Q08 reading postponementSMS advances strictly to 3/4 with signedAppointment still pending");

            yield return WaitForApplied(casework.RecordMailboxNoteViewedAsync(), "P3 Q08 read Bay note at LOC08");
            Require(!State.Happened(E10aParts.SignedAppointment) && E10aParts.ReadCount(State) == 3,
                "V36-CH01-Q08 Bay note alone keeps signedAppointment incomplete until 16/11 SMS is also read");

            yield return WaitForApplied(casework.RecordSmsAppointmentViewedAsync(), "P3 Q08 read 16/11 SMS at LOC08");
            Require(State.Happened(E10aParts.SignedAppointment) && E10aParts.ReadCount(State) == 4 && E10aParts.AllRead(State) &&
                    !State.Happened("G06.opened") && !State.Has("E08"),
                "V36-CH01-Q08 completing both appointment sources yields 4/4 independent E10a parts ready for H08 while G06/CH06 stay locked");

            // Retrieve cleanly back to bag and verify save/load persistence
            yield return WaitForApplied(casework.RetrieveItemFromAnchorAsync(WorldAnchors.Loc08DeskEnvelope), "P3 Q08 retrieve envelope");
            yield return WaitForApplied(casework.RetrieveItemFromAnchorAsync(WorldAnchors.Loc08DeskPhone), "P3 Q08 retrieve phone");
            Require(InventoryService.GetAllPlaced(State).Count == 0 && E10aParts.AllRead(State) && InventoryService.ValidState(State),
                "V36-CH01-Q08 retrieving envelope & phone from LOC08 desk preserves all 4/4 E10a parts & ValidState");

            // Restore pre-Phase3QA snapshot cleanly!
            Apply(transaction.Commit(savedBefore, savedPos, warp: false, geometryChanges: true), "P3 restore snapshot");
            player.Teleport(savedPos, Quaternion.identity);
            handPresenter?.SyncState(State, scene, hideViewModel: false);
            scene.B08Echo?.Sync(State, player.Eye.transform, scene.atmosphere, State.settings.reduceMotion, instantForQa: true);

            verification.Add("CH01 REBIRTH III PHASE 3 PASS: V36-CH01-Q04 (B06 all 4 Layer-B states + save-lock rollback + OPEN outer gate + >=120s battery), V36-CH01-Q07 (WR05 swept-boundary 5/5 anti-sequence-break scenarios in R&B + 0 seamBlocker + 0 backward push + real v36-wr05-bypass probe + pure G08 allow-path unit check; G08 E2E pending CH08), B08 Perceptual Echo (3 branches A/B/C + 0 colliders + 0 evidence + Reduced Motion + T10/CP04 + Q06 Journal restore), and V36-CH01-Q08 (CH05-D08 publicReportSummary guard w/o E09 + LOC08 R H08 Recovery 0/4->4/4 independent reads).");
        }

        private IEnumerator CapturePhase3Frame(string name, Vector3 feetPos, Vector3 lookTarget)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            var camera = player.Eye;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            bool previous = camera.enabled;
            camera.enabled = false;
            try
            {
                Vector3 planar = new(lookTarget.x - feetPos.x, 0f, lookTarget.z - feetPos.z);
                Quaternion bodyRot = planar.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(planar) : Quaternion.identity;
                player.Teleport(feetPos, bodyRot);
                camera.transform.LookAt(lookTarget);
                for (int i = 0; i < 4; i++) yield return null;
                SaveFrame(camera, rt, name);
            }
            finally
            {
                camera.enabled = previous;
                rt.Release();
                Destroy(rt);
            }
        }
    }
}
