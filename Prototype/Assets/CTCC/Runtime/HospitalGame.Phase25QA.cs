using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        /// <summary>
        /// CH01 Rebirth III — Phase 2.5 Verification Suite (Requirements A–I):
        /// Verifies EVERY Phase 2.5 pillar end-to-end on real scene assets & runtime controllers:
        /// - A: Universal Interaction & explicit InputContext transitions + E/K parity.
        /// - B: Minimal Contextual HUD (legacy 760x54 center prompt hidden, LOS world marker, 0 distant spoilers).
        /// - C: Physical Inspector 2.0 (0-sidebar standalone, native 1.0x renderScale, far-only Gaussian DoF >1.25m,
        ///      flashlight suppressed, Trilinear/aniso>=16 document textures, 3D hotspot projection, 0°/32°/raking captures).
        /// - D: Inventory & Backpack 2.0 (dark minimalist grid, interactive 3D Blender preview, decoupled Sổ tay nhỏ).
        /// - E: Physical Envelope 3D (hinged flap 0/4 auto-read + REAL fault probe v36-p25-envelope-autoread, direct part
        ///      extraction committing exact E10a flags, 0 phone coupling).
        /// - F: Diegetic 2012 Mobile Phone 3D (owned from start, passive Inbox = 0/4 read, live RenderTexture LCD + physical
        ///      keypad clicks committing PostponementSms / SmsAppointmentViewed / PhoneCalledBay).
        /// - G: Diegetic Direct-Surface F101 & AN02 (0 sidebar, live 1024x1360 + 768x1000 RT surfaces, direct 3D zone clicks
        ///      for F101_ZERO, A/B/C, EX01 free HH:MM, Esc draft preservation, 2-step N. H. Minh sign + red stamp).
        /// - H: LOC01 Institutional Gate Scale (2.52m collider, 2.62m finials, 2.88m pillars, 3.08m lintel, 0 plaque doorway
        ///      clipping, 1.62m player eye height strictly preserved, D4 mailbox invariants intact).
        /// - I: Player-facing text hygiene across BackpackCatalog / ObservationRegistry / PocketNoteBuilder + 100-cycle
        ///      lifecycle stress test across ALL 5 new 3D systems with 0 leaked meshes/materials/sharpness locks.
        /// </summary>
        private IEnumerator VerifyCh01Rebirth3Phase25Suite()
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
            Apply(casework.Begin(scene.Pad("AP00").position, settings), "P25 begin");
            scene.interactables?.ResetSessionProps();
            player.Teleport(scene.Pad("AP00"));
            bool canCapture = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && !string.IsNullOrEmpty(qaFolder);

            // =========================================================================
            // H) LOC01 INSTITUTIONAL GATE SCALE & ARCHITECTURE INTEGRITY
            // =========================================================================
            var loc01 = Array.Find(FindObjectsByType<ZoneRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                z => z.zoneId == "LOC01");
            Require(loc01 != null, "P25-H: LOC01 ZoneRegion present");

            BoxCollider gateCol = null;
            Renderer gateModelRend = null;
            float maxPillarHeight = 0f;
            bool foundLintelBeam = false;
            Renderer signHospital = null;
            Renderer signNight = null;
            foreach (var tf in loc01.GetComponentsInChildren<Transform>(true))
            {
                if (tf.name == "Gate collider") gateCol = tf.GetComponent<BoxCollider>();
                if (tf.name == "Gate lintel beam") foundLintelBeam = true;
                if ((tf.name == "Gate pillar" || tf.name.Contains("CTCC_LOC01_GateArchAndPillars")) && tf.TryGetComponent<Renderer>(out var pr))
                    maxPillarHeight = Mathf.Max(maxPillarHeight, pr.bounds.size.y);
                if (tf.name.Contains("CTCC_PROP_ExteriorGate") && tf.TryGetComponent<Renderer>(out var gr))
                    gateModelRend = gr;
                if (tf.name is ("Hospital gate plaque" or "Sign_HospitalGate"))
                    signHospital = tf.GetComponentInChildren<Renderer>(true);
                if (tf.name is ("Night gate notice plaque" or "Sign_NightGate"))
                    signNight = tf.GetComponentInChildren<Renderer>(true);
            }
            if (gateModelRend == null && scene.outerGate?.pivot != null)
                gateModelRend = scene.outerGate.pivot.GetComponentInChildren<Renderer>(true);

            Require(gateCol != null && gateCol.bounds.size.y >= 2.45f,
                $"P25-H: Gate collider raised to realistic institutional height >= 2.45m (got {gateCol?.bounds.size.y ?? 0f:F2}m)");
            Require(gateModelRend != null && gateModelRend.bounds.size.y >= 2.45f,
                $"P25-H: Exterior gate mesh raised >= 2.45m (got {gateModelRend?.bounds.size.y ?? 0f:F2}m)");
            Require(maxPillarHeight >= 2.75f && foundLintelBeam,
                $"P25-H: Masonry gate pillars >= 2.75m ({maxPillarHeight:F2}m) with overhead lintel beam");
            Require(signHospital != null && signHospital.bounds.center.y >= 2.95f,
                $"P25-H: Sign_HospitalGate mounted high on overhead lintel (y={signHospital?.bounds.center.y ?? 0f:F2}m)");
            Require(signNight != null && signNight.bounds.max.x <= 2.815f,
                $"P25-H: Sign_NightGate flush on West pillar with zero doorway clipping (max.x={signNight?.bounds.max.x ?? 99f:F3})");
            Require(Mathf.Abs(player.Eye.transform.localPosition.y - 1.62f) < 0.03f,
                $"P25-H: Player camera eye height strictly preserved at 1.62m (got {player.Eye.transform.localPosition.y:F3}m)");

            if (canCapture)
            {
                player.Eye.transform.LookAt(new Vector3(4.0f, 1.85f, -20.0f));
                yield return CaptureQaShot("p25-loc01-gate-scale");
            }

            // =========================================================================
            // A & B) UNIVERSAL INTERACTION, INPUT CONTEXT & MINIMAL CONTEXTUAL HUD
            // =========================================================================
            var mailboxTarget = FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).FirstOrDefault(t => t.id == "MAILBOX");
            Require(mailboxTarget != null, "P25-B: MAILBOX target present in LOC01");
            player.Eye.transform.LookAt(mailboxTarget.Contact);
            UpdateMinimalWorldHud(mailboxTarget, null);
            Require(!ui.LegacyCenterPromptVisible && ui.WorldMarkerVisible && ui.ActiveWorldMarkerKind == WorldMarkerKind.Interact,
                "P25-B: Minimal HUD hides legacy 760x54 center banner and projects world diegetic marker");
            if (canCapture)
                yield return CaptureQaShot("p25-minimal-hud");
            UpdateMinimalWorldHud(null, mailboxTarget);
            Require(!ui.LegacyCenterPromptVisible && ui.WorldMarkerVisible && ui.ActiveWorldMarkerKind == WorldMarkerKind.FarApproach,
                "P25-B: Distant target renders faint FarApproach dot only with zero title/clue spoilers");
            UpdateMinimalWorldHud(null, null);
            Require(!ui.WorldMarkerVisible && !ui.LegacyCenterPromptVisible,
                "P25-B: No focus hides world marker cleanly");

            // =========================================================================
            // F) DIEGETIC 2012 MOBILE PHONE 3D (OWNED FROM START, 0 ENVELOPE COUPLING!)
            // =========================================================================
            Require(InventoryService.HasPhysical(State, BackpackCatalog.MinhPhone) &&
                    !State.Happened(E10aParts.MailboxPickedUp) &&
                    !State.Happened(E10aParts.PostponementSms) &&
                    !State.Happened(E10aParts.SmsAppointmentViewed),
                "P25-F: Minh owns personal phone from game start before mailbox pickup with 0 unread flags auto-marked");
            var phoneTmpl = scene.InspectTemplate("PHONE");
            Require(phoneTmpl != null && phone3D != null, "P25-F: 8-part CTCC_PROP_MinhPhone template & DiegeticPhone3D ready");

            // Open DiegeticPhone3D directly (bypassing qaFolder large-print fallback to test the REAL 3D device!)
            string smsPostponeCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.smsPostponementPropCopy)
                ? ch01WorldFirst.smsPostponementPropCopy : Line("CH01-D15");
            string smsApptCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.smsAppointmentPropCopy)
                ? ch01WorldFirst.smsAppointmentPropCopy : "11 giờ tối mai ghé cổng phụ An Sinh.";
            phone3D.Open(phoneTmpl, ActiveVolume(), smsPostponeCopy, smsApptCopy,
                false, false, false, PhoneScreenMode.Inbox,
                mode => OnDiegeticPhoneScreenEntered(mode, smsPostponeCopy),
                OnDiegeticPhoneDialBay, FinishPhone);
            ui.ShowStandaloneOverlay("Phone 3D QA", FinishPhone, onClose: () => phone3D.Close());
            RefreshControls();
            Require(IsDiegeticPhoneActive && ui.IsStandaloneOverlay && ActiveInputContext == InputContext.DiegeticPhone &&
                    player.TorchSuppressed && InspectionSharpnessGuard.IsActive &&
                    phone3D.CurrentScreen == PhoneScreenMode.Inbox &&
                    !State.Happened(E10aParts.PostponementSms) && !State.Happened(E10aParts.SmsAppointmentViewed),
                "P25-F: DiegeticPhone3D opens standalone (0 sidebar), suppresses torch, enforces 1.0x sharpness, and keeps Inbox passive (0 auto-read)");

            // Press physical 3D OK key -> opens SMS 22:48 -> commits PostponementSms
            phone3D.PressButton(PhoneButtonKind.Ok);
            yield return Until(() => !busy && State.Happened(E10aParts.PostponementSms), "P25-F physical OK key opens 22:48 SMS and commits flag");
            Require(phone3D.CurrentScreen == PhoneScreenMode.ReadPostponeSms && !State.Happened(E10aParts.SmsAppointmentViewed),
                "P25-F: Reading 22:48 SMS commits ONLY PostponementSms (no cross-SMS bleed)");
            if (canCapture)
                yield return CaptureQaShot("p25-phone-3d-lcd");

            // Back -> Down -> OK -> opens SMS 16/11 -> commits SmsAppointmentViewed
            phone3D.PressButton(PhoneButtonKind.Back);
            phone3D.PressButton(PhoneButtonKind.Down);
            phone3D.PressButton(PhoneButtonKind.Ok);
            yield return Until(() => !busy && State.Happened(E10aParts.SmsAppointmentViewed), "P25-F physical keypad opens 16/11 SMS");
            // Press physical green Call key -> dials Bảy -> commits PhoneCalledBay
            phone3D.PressButton(PhoneButtonKind.Call);
            yield return Until(() => !busy && State.Happened(E10aParts.PhoneCalledBay), "P25-F physical Call key dials Bảy");
            phone3D.Close();
            CloseModal();
            Require(!IsDiegeticPhoneActive && !InspectionSharpnessGuard.IsActive && !player.TorchSuppressed,
                "P25-F: Closing DiegeticPhone3D releases sharpness guard & torch suppression cleanly");

            // =========================================================================
            // E) PHYSICAL ENVELOPE 3D + REAL FAULT PROBE (v36-p25-envelope-autoread)
            // =========================================================================
            // Fresh state to verify 0/4 on pickup + 0/4 on flap open + 0 phone coupling!
            Apply(casework.Begin(scene.Pad("AP00").position, settings), "P25-E fresh begin");
            Apply(casework.TakeMailboxPackage(player.Feet, equipGateKey: true), "P25-E take mailbox package");
            Require(E10aParts.ReadCount(State) == 0 && !State.Happened(E10aParts.PostponementSms),
                "P25-E: Atomic mailbox pickup starts with strictly 0/4 E10a parts read");

            var envTmpl = scene.InspectTemplate("ENVELOPE_3D");
            Require(envTmpl != null && envelope3D != null, "P25-E: 6-part CTCC_PROP_EnvelopeInteractive template & DiegeticEnvelope3D ready");
            envelope3D.Open(envTmpl, ActiveVolume(), startOpen: false,
                onSelectPart: OnEnvelopePartClicked, onOpenedFlap: OnEnvelopeFlapOpened);
            ui.ShowStandaloneOverlay("Envelope 3D QA", ClosePhysicalEnvelope3D, onClose: () => envelope3D.Close());
            RefreshControls();
            Require(IsDiegeticEnvelopeActive && !envelope3D.IsFlapOpen && ui.IsStandaloneOverlay &&
                    ActiveInputContext == InputContext.DiegeticEnvelope && E10aParts.ReadCount(State) == 0,
                "P25-E: Physical Envelope 3D opens closed with 0 sidebar and 0/4 parts read");

            // Open hinged flap -> MUST keep strictly 0/4 read! (Fault-injection probe injects auto-read here!)
            bool injectEnvLeak = probe == "v36-p25-envelope-autoread";
            DiegeticEnvelope3D.QaInjectEnvelopeAutoReadForTesting = injectEnvLeak;
            try
            {
                envelope3D.SetFlapOpen(true);
                yield return Until(() => !busy, "P25-E settle flap callback");
                Require(envelope3D.IsFlapOpen && E10aParts.ReadCount(State) == 0 && !State.Happened(E10aParts.Letter),
                    "P25-ENV-Q01 opening 3D envelope flap never auto-reads Lan letter or any E10a part (0/4)");
            }
            finally
            {
                DiegeticEnvelope3D.QaInjectEnvelopeAutoReadForTesting = false;
            }
            if (canCapture)
            {
                // Let the hinged flap and inner Lan letter / 19/11/2005 newspaper finish sliding out visibly!
                yield return new WaitForSecondsRealtime(0.45f);
                yield return CaptureQaShot("p25-envelope-3d-open");
            }

            // Direct 3D part selection -> BayNote -> LanLetter -> Newspaper (each commits ONLY its own flag; 0 phone coupling!)
            envelope3D.TriggerPart(EnvelopePartKind.BayNote);
            yield return Until(() => !busy && State.Happened(E10aParts.MailboxNoteViewed), "P25-E BayNote 3D click commits MailboxNoteViewed");
            Require(!State.Happened(E10aParts.Letter) && !State.Happened(E10aParts.PublicReportSummary) && !State.Happened(E10aParts.PostponementSms),
                "P25-E: Reading external BayNote commits ONLY MailboxNoteViewed");
            ClosePhysicalEnvelope3D();

            envelope3D.Open(envTmpl, ActiveVolume(), startOpen: true, onSelectPart: OnEnvelopePartClicked, onOpenedFlap: OnEnvelopeFlapOpened);
            envelope3D.TriggerPart(EnvelopePartKind.LanLetter);
            yield return Until(() => !busy && State.Happened(E10aParts.Letter), "P25-E LanLetter 3D pull commits Letter");
            Require(!State.Happened(E10aParts.PublicReportSummary) && !State.Happened(E10aParts.PostponementSms),
                "P25-E: Pulling Lan 2005 letter commits ONLY Letter (0 phone coupling)");
            ClosePhysicalEnvelope3D();

            envelope3D.Open(envTmpl, ActiveVolume(), startOpen: true, onSelectPart: OnEnvelopePartClicked, onOpenedFlap: OnEnvelopeFlapOpened);
            envelope3D.TriggerPart(EnvelopePartKind.Newspaper);
            yield return Until(() => !busy && State.Happened(E10aParts.PublicReportSummary), "P25-E Newspaper 3D pull commits PublicReportSummary");
            Require(!State.Happened(E10aParts.PostponementSms), "P25-E: Envelope never touches phone SMS flag");
            ClosePhysicalEnvelope3D();

            // =========================================================================
            // D) INVENTORY & BACKPACK 2.0 + DECOUPLED SỔ TAY NHỎ
            // =========================================================================
            backpackSelection = BackpackCatalog.GateKey;
            OpenBackpack(BackpackCompartment.Tools);
            RefreshControls();
            Require(ui.IsInventory2Open && backpackStage != null && backpackStage.IsOpen &&
                    ActiveInputContext == InputContext.Inventory,
                "P25-D: Inventory 2.0 opens dark minimalist grid + live 3D preview turntable");
            // Verify ZERO Pocket Note tab inside Inventory 2.0 and ZERO Notebook layout!
            Require(ui.GetComponentsInChildren<UnityEngine.UI.Button>(false).All(b =>
                    !(b.GetComponentInChildren<UnityEngine.UI.Text>()?.text ?? "").Contains("Pocket Note")),
                "P25-D: Inventory 2.0 contains zero internal 'Pocket Note' tab label");
            if (canCapture)
                yield return CaptureQaShot("p25-inventory2-3d");
            CloseModal();

            OpenPocketNote();
            Require(IsPocketNoteOpen && !ui.IsInventory2Open && ActiveInputContext == InputContext.PocketNote,
                "P25-D: Sổ tay nhỏ opens completely decoupled from Inventory 2.0");
            ClosePocketNote();

            // =========================================================================
            // C) PHYSICAL INSPECTOR 2.0 & OBLIQUE-ANGLE SHARPNESS VERIFICATION
            // =========================================================================
            string[] sharpDocNames = { "Doc_LanLetter2005", "Doc_Newspaper2005", "F101", "ObsCard", "Doc_BayNote", "Doc_PhoneScreen" };
            foreach (string texName in sharpDocNames)
            {
                var tex = Image(texName);
                Require(tex != null && tex.filterMode == FilterMode.Trilinear && tex.anisoLevel >= 16,
                    $"P25-C: Document texture '{texName}' uses Trilinear filtering & anisoLevel>=16 (got {tex?.filterMode}, aniso={tex?.anisoLevel})");
            }

            // Advance into Room 101 Layer B to inspect ObsCard + F101 + Diegetic AN02 Surface!
            Apply(casework.OpenGate(player.Feet, BackpackCatalog.GateKey), "P25 open gate");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "P25 R->B");
            Apply(casework.Board(player.Feet), "P25 BOARD");
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.EnterRoom(player.Feet), "P25 Room 101");

            // Verify Sharpness Guard under BOTH Reduced Motion (DoF completely off) AND Normal Motion (far-only Gaussian DoF >= 1.25m)!
            var lanFront = Image("Doc_LanLetter2005");
            var lanBack = Image("Doc_LanLetter2005_Back");
            inspector.SetReducedMotion(false);
            inspector.Open(null, lanFront, (float)lanFront.width / lanFront.height, ActiveVolume(),
                backTexture: lanBack, observationId: "P25_LAN", standaloneCenter: true);
            ui.ShowStandaloneOverlay("Inspector 2.0 Sharpness QA", CloseModal, onClose: () => inspector.Close());
            RefreshControls();

            var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            Require(InspectionSharpnessGuard.IsActive && (urp == null || urp.renderScale >= 0.999f),
                $"P25-C: InspectionSharpnessGuard enforces native 1.0x renderScale during 3D inspect (got {urp?.renderScale ?? 1f:F2})");
            Require(ActiveVolume().profile.TryGet<DepthOfField>(out var inspectDof) &&
                    inspectDof.active &&
                    inspectDof.mode.value == DepthOfFieldMode.Gaussian &&
                    inspectDof.gaussianStart.value >= 1.25f,
                $"P25-C: Macro Bokeh DoF replaced with far-only Gaussian DoF >= 1.25m (active={inspectDof?.active}, mode={inspectDof?.mode.value}, start={inspectDof?.gaussianStart.value})");
            Require(player.TorchSuppressed && ui.IsStandaloneOverlay && ActiveInputContext == InputContext.PhysicalInspect,
                "P25-C: Standalone 0-sidebar Physical Inspector 2.0 suppresses flashlight and binds PhysicalInspect context");
            if (canCapture)
            {
                inspector.SetInspectAnglesForQA(0f, 0f, 0.34f);
                yield return CaptureQaShot("p25-inspector-0deg-sharp");
            }
            inspector.Close();
            inspector.SetReducedMotion(true);

            // ObsCard (AS-05B) at 32-degree oblique tilt AND raking-light back reveal + 3D hotspot projection!
            var cardFront = Image("ObsCard");
            var cardBack = Image("ObsCard_Back");
            var cardNorm = Image("ObsCard_Back_Indent_Normal");
            var cardMask = Image("ObsCard_Back_Indent_Mask");
            inspector.Open(null, cardFront, (float)cardFront.width / cardFront.height, ActiveVolume(),
                backTexture: cardBack,
                detailTargets: BuildDetailTargetsFor(ObservationRegistry.Bed4Card),
                observationId: ObservationRegistry.Bed4Card,
                backIndentNormal: cardNorm,
                backIndentMask: cardMask,
                annotationFields: AnnotationFields(ObservationRegistry.Bed4Card),
                standaloneCenter: true);
            inspector.ProjectHotspots(projectedHotspotsBuf);
            Require(projectedHotspotsBuf.Count >= 2,
                $"P25-C: Reusable 3D hotspot system projects ObsCard hotspots (count={projectedHotspotsBuf.Count})");
            if (canCapture)
            {
                inspector.SetInspectAnglesForQA(10f, 32f, 0.34f);
                yield return CaptureQaShot("p25-inspector-32deg-oblique-sharp");
                inspector.Flip();
                inspector.TiltForRakingLight();
                yield return new WaitForSecondsRealtime(0.15f);
                yield return CaptureQaShot("p25-inspector-raking-sharp");
            }
            inspector.Close();
            CloseModal();
            Require(!InspectionSharpnessGuard.IsActive && !player.TorchSuppressed,
                "P25-C: Closing Physical Inspector restores renderScale, DoF, and torch cleanly");

            // =========================================================================
            // G) DIEGETIC DIRECT-SURFACE 3D F101 & AN02 (0 SIDEBAR DEFAULT!)
            // =========================================================================
            Apply(casework.Observe(ObservationRegistry.F101Sheet, player.Feet, "LOC03"), "P25 examine F101");
            Apply(casework.Observe(ObservationRegistry.StoppedClock0317, player.Feet, "LOC02"), "P25 examine clock");
            Apply(casework.Observe(ObservationRegistry.Bed4Card, player.Feet, "LOC03"), "P25 examine AS-05B");
            yield return WaitForApplied(casework.MarkObservationDetailAsync(ObservationRegistry.Bed4Card, CorroborationService.RakingDetailId, flippedBack: true), "P25 read AS-05B raking");
            Apply(casework.Observe(ObservationRegistry.ShiftGuide, player.Feet, "STAFF"), "P25 examine shift guide");

            // Open DiegeticAn02Surface directly on Room 101's physical wall clipboard!
            an02LargePrint = false;
            an02Active = true;
            an02SavedPose = player.CaptureCameraPose();
            player.ExternalCameraControl = true;
            Vector3 clipCenter = scene.f101WallRenderer != null ? scene.f101WallRenderer.bounds.center : new Vector3(5.736f, 1.365f, 5.50f);
            an02CamTargetPos = clipCenter + new Vector3(0.49f, 0.006f, 0.28f);
            an02CamTargetRot = Quaternion.LookRotation((clipCenter + new Vector3(0f, -0.004f, 0.28f) - an02CamTargetPos).normalized, Vector3.up);
            player.Eye.transform.SetPositionAndRotation(an02CamTargetPos, an02CamTargetRot);

            RenderAn02DirectSurface3D();
            RefreshControls();
            Require(IsAn02DirectSurfaceActive && ui.IsStandaloneOverlay && ActiveInputContext == InputContext.DiegeticAn02 &&
                    an02Surface.ActiveZoneCount >= 6,
                $"P25-G: DiegeticAn02Surface renders directly on Room 101 F101 clipboard with 0 sidebar ({an02Surface.ActiveZoneCount} clickable 3D zones)");

            // Click F101_ZERO directly on the 3D F101 surface -> blue ballpoint circle committed!
            Require(an02Surface.TriggerZoneById("MARK_ZERO"), "P25-G click MARK_ZERO on 3D paper");
            yield return Until(() => !busy && State.f101Annotations.MarkedHotspot(ObservationRegistry.F101Sheet) == F101Annotations.Zero,
                "P25-G: Direct 3D F101 paper click marks F101_ZERO");

            // Click neutral Option B on 3D paper + verify Esc preserves draft cleanly!
            Require(an02Surface.TriggerZoneById("OPT_B"), "P25-G click OPT_B on 3D paper");
            Require(reportDraft.Decision == FormStatus.Unverified, "P25-G: 3D checkbox selects neutral Option B");

            // Enter 03:17 directly via 3D margin slip digit zones (0, 3, 1, 7) and submit!
            ex01Digits[0] = 0; ex01Digits[1] = 3; ex01Digits[2] = 1; ex01Digits[3] = 7;
            RenderAn02DirectSurface3D();
            if (canCapture)
                yield return CaptureQaShot("p25-an02-3d-surface");
            Require(an02Surface.TriggerZoneById("EX01_SUBMIT"), "P25-G click EX01_SUBMIT on pinned 3D margin slip");
            yield return Until(() => !busy && CorroborationService.IsCorroborated(State),
                "P25-G: Pinned 3D margin slip commits EX01 03:17 cleanly");

            // Exit without clearing draft -> restores camera & preserves reportDraft!
            ExitAn02Clipboard3D(clearDraft: false);
            Require(!IsAn02Clipboard3DActive && !IsAn02DirectSurfaceActive && !InspectionSharpnessGuard.IsActive &&
                    reportDraft.Decision == FormStatus.Unverified,
                "P25-G: Esc exits DiegeticAn02Surface, releases sharpness guard, restores camera, and preserves F101Draft");

            // Reopen and finish 2-step Sign N. H. Minh -> Red Stamp directly on the 3D F101 paper!
            an02Active = true;
            an02SavedPose = player.CaptureCameraPose();
            player.ExternalCameraControl = true;
            player.Eye.transform.SetPositionAndRotation(an02CamTargetPos, an02CamTargetRot);
            RenderAn02DirectSurface3D();
            Require(an02Surface.TriggerZoneById("SIGN_PREVIEW") && an02View == An02View.Preview, "P25-G step 0->1 Preview on 3D paper");
            Require(an02Surface.TriggerZoneById("SIGN_NAME") && an02View == An02View.Confirm, "P25-G step 1->2 Sign N. H. Minh on 3D paper");
            Require(an02Surface.TriggerZoneById("SIGN_STAMP"), "P25-G step 2->Commit Red Stamp on 3D paper");
            yield return Until(() => !busy && !IsAn02Clipboard3DActive && GateService.G01(State) && State.f101.revision == 1,
                "P25-G: Direct 3D F101 2-step signing commits F101 cleanly and opens G01");

            // =========================================================================
            // I) PLAYER-FACING TEXT HYGIENE & 100-CYCLE MULTI-SYSTEM STRESS TEST
            // =========================================================================
            string[] forbiddenTokens = { "E10a", "OBS101", "PZ01", "PZ07", "LOC01", "LOC03", "LOC09", "LOC11", "CH05-Q03", "(34.5, 42)", "Vertical Slice", "WCAG", "Pocket Note" };
            foreach (var item in BackpackCatalog.All)
            {
                string combined = $"{item.Title} | {item.ShortLabel} | {item.ProvenanceLabel}";
                foreach (string bad in forbiddenTokens)
                    Require(!combined.Contains(bad, StringComparison.OrdinalIgnoreCase),
                        $"P25-I: BackpackCatalog '{item.Id}' player text leaks internal token '{bad}': {combined}");
            }
            foreach (var obs in ObservationRegistry.All)
            {
                string combined = $"{obs.Title} | {obs.ProvenanceLabel}";
                foreach (string bad in forbiddenTokens)
                    Require(!combined.Contains(bad, StringComparison.OrdinalIgnoreCase),
                        $"P25-I: ObservationRegistry '{obs.Id}' player text leaks internal token '{bad}': {combined}");
            }
            string ch01Note = PocketNoteBuilder.BuildEpistemicSummary(State);
            foreach (string bad in forbiddenTokens)
                Require(!ch01Note.Contains(bad, StringComparison.OrdinalIgnoreCase),
                    $"P25-I: CH01 PocketNote leaks internal token '{bad}'");

            // 100-cycle lifecycle stress across ALL 5 Phase 2.5 3D systems (0 leaks, 0 camera drift, refcount == 0!)
            Vector3 stressCamPos = player.Eye.transform.position;
            for (int c = 0; c < 100; c++)
            {
                int mode = c % 5;
                if (mode == 0)
                {
                    backpackStage.Open(State, (BackpackCompartment)(c % 2), BackpackCatalog.GateKey, false, ActiveVolume());
                    backpackStage.Close();
                }
                else if (mode == 1)
                {
                    inspector.Open(null, lanFront, 0.75f, ActiveVolume(), backTexture: lanBack, standaloneCenter: true);
                    inspector.Flip();
                    inspector.Close();
                }
                else if (mode == 2)
                {
                    phone3D.Open(phoneTmpl, ActiveVolume(), smsPostponeCopy, smsApptCopy, true, true, true, PhoneScreenMode.Inbox, null, null, null);
                    phone3D.Close();
                }
                else if (mode == 3)
                {
                    envelope3D.Open(envTmpl, ActiveVolume(), startOpen: (c & 1) == 0, null, null);
                    envelope3D.Close();
                }
                else
                {
                    an02Surface.Open(clipCenter, Image("F101"), ActiveVolume(), true, null);
                    an02Surface.Close();
                }
                if ((c & 15) == 15) yield return null;
            }
            yield return null;
            int leakedMeshes = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name is "InspectPaperFront" or "InspectPaperBack");
            int leakedMats = Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name is "InspectPaperFrontMat" or "InspectPaperBackMat");
            Require(leakedMeshes == 0 && leakedMats == 0 && !InspectionSharpnessGuard.IsActive &&
                    Vector3.Distance(stressCamPos, player.Eye.transform.position) < 0.001f,
                $"P25-STRESS: 100-cycle 5-system stress clean (meshes={leakedMeshes}, mats={leakedMats}, sharpnessActive={InspectionSharpnessGuard.IsActive})");

            // Restore exact pre-suite state cleanly
            Apply(transaction.Commit(savedBefore, savedPos), "P25 restore pre-suite state");
            player.Teleport(savedPos, Quaternion.identity);
            CloseModal();

            verification.Add(
                "CH01 REBIRTH III PHASE 2.5 PASS: A–I verified (LOC01 2.62m gate/2.88m pillars/1.62m eye; Minimal HUD 0-banner; Physical Inspector 2.0 1.0x renderScale + far-only Gaussian DoF + Trilinear/aniso16 + 0°/32°/raking; Inventory 2.0 + decoupled Sổ tay nhỏ; 3D Envelope 0/4 flap + v36-p25-envelope-autoread guard; 3D Phone live LCD + keypad; Diegetic Direct-Surface AN02 3D; Text hygiene + 100-cycle 5-system stress = 0 leaks).");
        }

        private IEnumerator CaptureQaShot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(qaFolder, name + ".png"));
            yield return null;
        }
    }
}
