using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private Keyboard qaKeys;
        private bool captureExpansion;

        private IEnumerator VerifyPlayerInput()
        {
            Directory.CreateDirectory(Argument("-ctcc-input-qa"));
            Require(Argument("-ctcc-save") != null && transaction.Inspect(SaveSlot.Auto) == SaveRead.Empty &&
                transaction.Inspect(SaveSlot.Manual) == SaveRead.Empty, "Input QA requires isolated, empty save slots");
            yield return Until(() => Application.isFocused, "Input QA needs a focused Windows player");
            var physicalKeyboards = InputSystem.devices.OfType<Keyboard>().Where(d => d.enabled).ToArray();
            foreach (var kb in physicalKeyboards) InputSystem.DisableDevice(kb);
            qaKeys = InputSystem.AddDevice<Keyboard>();
            qaKeys.MakeCurrent();
            var background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            try
            {
                yield return null;
                yield return null;
                if (Argument("-ctcc-input-s1-only") == "true")
                {
                    Require(scene.IsAdditiveMode, "targeted S1 QA requires additive campus scenes");
                    yield return InputVisit(FormStatus.Unverified, s1Only: true);
                    yield break;
                }
                string branchFilter = Argument("-ctcc-input-branch");
                var branches = new[] { FormStatus.Affirmed, FormStatus.Unverified, FormStatus.Refuted };
                bool Selected(FormStatus branch) =>
                    branchFilter == null || string.Equals(branch.ToString(), branchFilter, StringComparison.OrdinalIgnoreCase);
                Require(Array.Exists(branches, Selected), "input QA branch must be Affirmed, Unverified or Refuted");
                foreach (var choice in branches)
                    if (Selected(choice))
                        yield return InputVisit(choice);
                yield return InspectLifetimeStress();
            }
            finally
            {
                InputSystem.RemoveDevice(qaKeys);
                foreach (var kb in physicalKeyboards)
                {
                    if (kb.added) InputSystem.EnableDevice(kb);
                }
                InputSystem.settings.backgroundBehavior = background;
            }
        }

        private IEnumerator InputVisit(FormStatus branch, bool s1Only = false)
        {
            bool restart = transaction.Inspect(SaveSlot.Auto) is SaveRead.Valid or SaveRead.Backup;
            yield return Choose("Ca trực mới");
            if (restart) yield return Choose("Bắt đầu");
            yield return Until(() => !ui.IsModal && !busy, "free view at the gate");
            Require(State.evidence.Count == 0 && !State.Has("E10a") && !State.Happened(E10aParts.PostponementSms) && E10aParts.ReadCount(State) == 0,
                "B01 S0 starts with 0 evidence, unread SMS, and 0/4 E10a parts read");

            if (branch == FormStatus.Affirmed)
            {
                // ADVERSARIAL ROUTE (Core v3.6.2 / 4.2 / 4.6 Zero-Modal World-First Mailbox):
                // Skip phone & all documents -> [E] #1 opens 3D lid (zero modal) -> [E] #2 takes atomic package directly in-world ->
                // reject KEY_WING_C -> test Quick-Slot [1]/[H] -> unlock GATE-CONG while strictly keeping 0/4 parts read!
                yield return AimAndUseHere("MAILBOX");
                yield return Until(() => !busy && State.Happened(E10aParts.MailboxOpened) && !ui.IsModal,
                    "Adversarial 1st [E] swings 3D mailbox lid open with zero 2D modal");
                yield return AimAndUseHere("MAILBOX");
                yield return Until(() => !busy && !ui.IsModal && State.Happened(E10aParts.MailboxPickedUp) &&
                    InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.GateKey,
                    "Adversarial 2nd [E] takes atomic mailbox package directly in-world with zero 2D modal (KEY_GATE equipped)");
                Require(State.evidence.Count == 1 && State.Has("E10a") && E10aParts.ReadCount(State) == 0 && !State.Happened(E10aParts.PostponementSms),
                    "Adversarial pickup keeps strictly 0/4 E10a parts read");

                // Phase 2.5 Ch01JournalPolicy + Decoupled Sổ tay nhỏ ([N]) + Inventory 2.0 ([Tab])!
                yield return Tap(Key.N);
                yield return Until(() => IsPocketNoteOpen && ui.IsModal, "B01 [N] opens decoupled Sổ tay nhỏ");
                Require(ui.GetComponentsInChildren<Button>(false).All(b =>
                    !(b.GetComponentInChildren<Text>()?.text ?? "").Contains("Mở sổ trực đầy đủ")),
                    "Ch01JournalPolicy hides 5-tab Journal entry in B01 Sổ tay nhỏ");
                yield return Tap(Key.Escape);
                yield return Until(() => !ui.IsModal, "Sổ tay nhỏ closed cleanly");

                yield return Tap(Key.Tab);
                yield return Until(() => ui.IsInventory2Open && backpackStage.IsOpen, "B01 Inventory 2.0 opens with 3D preview");
                yield return Choose("Đồ vật");
                yield return Choose("Chìa khu C");
                yield return Choose("Cầm ra tay");
                yield return Until(() => !busy && !ui.IsModal &&
                    InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.WingCKey, "Wing-C key equipped from Inventory 2.0");

                yield return AimAndUseHere("GATE-CONG");
                yield return new WaitForSecondsRealtime(0.25f);
                Require(!State.Happened("CH01-T02") && InventoryService.HasPhysical(State, BackpackCatalog.WingCKey),
                    "wrong Wing-C key rejected at outer gate and retained in backpack");

                // Test real keyboard Quick-Slot [1] & [H] stow (Proposal V1.1 §3.1 / BP-02)
                Require(State.quickSlots.Count >= 2 && State.quickSlots[0] == BackpackCatalog.GateKey,
                    "GateKey auto-assigned to Quick-Slot #1");
                yield return Tap(Key.Digit1);
                yield return Until(() => !busy && InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.GateKey,
                    "Quick-Slot [1] equips GateKey directly without opening Backpack");
                yield return Tap(Key.H);
                yield return Until(() => !busy && InventoryService.GetEquipped(State) == null,
                    "[H] stows held GateKey back into Backpack");
                yield return Tap(Key.Digit1);
                yield return Until(() => !busy && InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.GateKey,
                    "Quick-Slot [1] re-equips GateKey for outer gate");

                Require(State.evidence.Count == 1 && State.Has("E10a") && E10aParts.ReadCount(State) == 0,
                    "Adversarial route reaches outer gate with strictly 0/4 E10a parts read");
                yield return AimAndUseHere("GATE-CONG");
                yield return Until(() => State.Happened("CH01-T02"), "Adversarial route unlocks gate with KEY_GATE at 0/4 parts");
                Require(E10aParts.ReadCount(State) == 0 && !State.Happened(E10aParts.PostponementSms),
                    "Adversarial gate unlock preserves 0/4 parts read without faking SMS");
            }
            else
            {
                // NARRATIVE ROUTE (Phase 2.5 Real Diegetic Phone 3D + Physical Envelope 3D — 0 Large-Print fallbacks!):
                yield return null; // allow TickCh01LitLobbySight() to observe lit lobby from AP00
                int nonVoBeforePhone = voice.NonVoAudioPlayCount;
                yield return Tap(Key.P);
                yield return Until(() => !busy && IsDiegeticPhoneActive && ui.IsStandaloneOverlay,
                    branch + " [P] opens standalone 0-sidebar DiegeticPhone3D");
                Require(!State.Happened(E10aParts.PostponementSms) && E10aParts.ReadCount(State) == 0,
                    "DiegeticPhone3D Inbox starts passive with 0 auto-read");
                yield return Tap(Key.Enter);
                yield return Until(() => !busy && State.Happened(E10aParts.PostponementSms),
                    branch + " physical OK/Enter reads 22:48 SMS on live 3D LCD");
                if (branch == FormStatus.Refuted)
                {
                    yield return new WaitForSecondsRealtime(0.22f);
                    CaptureInvestigation("walkthrough-b01-phone");
                    yield return null;
                }
                yield return Tap(Key.C);
                yield return Until(() => !busy && State.Happened(E10aParts.PhoneCalledBay), "Narrative route calls Bảy via 3D phone keypad");
                yield return Tap(Key.Escape);
                yield return Until(() => !busy && !ui.IsModal && !IsDiegeticPhoneActive, "DiegeticPhone3D closed cleanly");
                Require(voice.Transcript.Any(t => t.LineId == "CH01-D01") && !voice.IsSpeaking && voice.NonVoAudioPlayCount == nonVoBeforePhone,
                    "DEC-VO-01 CH01-D01 renders as silent thought after phone + lit lobby sight with zero VO playback");

                if (branch == FormStatus.Refuted)
                {
                    CaptureInvestigation("walkthrough-b01-mailbox-closed");
                    yield return null;
                }
                yield return AimAndUseHere("MAILBOX");
                yield return Until(() => !busy && State.Happened(E10aParts.MailboxOpened) && !ui.IsModal,
                    "Narrative 1st [E] swings 3D gate mailbox lid open with zero 2D modal");
                if (branch == FormStatus.Refuted)
                {
                    yield return new WaitForSecondsRealtime(0.20f);
                    CaptureInvestigation("walkthrough-b01-mailbox-open");
                    yield return null;
                }
                yield return AimAndUseHere("MAILBOX");
                yield return Until(() => !busy && !ui.IsModal && State.Happened(E10aParts.MailboxPickedUp) &&
                    InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.GateKey,
                    "Narrative 2nd [E] takes atomic mailbox package directly in-world with zero 2D modal (KEY_GATE equipped)");
                if (branch == FormStatus.Refuted)
                {
                    // Open real Physical Envelope 3D from Inventory 2.0 -> Giấy tờ -> Phong bì chú Bảy -> open flap -> pull Lan letter!
                    yield return Tap(Key.Tab);
                    yield return Until(() => ui.IsInventory2Open, "Inventory 2.0 opens for envelope");
                    yield return Choose("Giấy tờ");
                    yield return Choose("Phong bì chú Bảy");
                    yield return Choose("Cầm & mở phong bì");
                    yield return Until(() => !busy && IsDiegeticEnvelopeActive && ui.IsStandaloneOverlay, "Physical Envelope 3D opens standalone");
                    Require(!State.Happened(E10aParts.Letter), "Opening 3D envelope keeps Lan letter unread");
                    yield return Tap(Key.Space);
                    yield return Until(() => envelope3D.IsFlapOpen, "Space opens 3D kraft envelope flap");
                    Require(!State.Happened(E10aParts.Letter), "Opening 3D envelope flap never auto-reads Lan letter");
                    yield return Tap(Key.Digit2);
                    yield return Until(() => !busy && inspector.IsInspecting && inspector.HasBackSide, "Lan 2005 letter pulled out in 3D Physical Inspector 2.0");
                    yield return Tap(Key.Space);
                    yield return Until(() => inspector.ShowingBack, "Lan 2005 letter flips to back");
                    CaptureInvestigation("walkthrough-e10a-letter");
                    yield return null;
                    Require(State.Happened(E10aParts.Letter) && E10aParts.ReadCount(State) >= 2,
                        "Narrative route commits Lan letter read truthfully alongside SMS");
                    yield return Tap(Key.Escape);
                    yield return Until(() => !busy && IsDiegeticEnvelopeActive, "Returning Lan letter returns to open 3D Envelope");
                    yield return Tap(Key.Escape);
                    yield return Until(() => !busy && !IsDiegeticEnvelopeActive, "Envelope closes back to Inventory 2.0");
                    if (ui.IsModal) yield return Tap(Key.Escape);
                    yield return Until(() => !ui.IsModal, "Inventory 2.0 closed cleanly");
                }
                Require(State.evidence.Count == 1 && State.Has("E10a") && E10aParts.ReadCount(State) >= 1,
                    "Narrative route preserves single E10a evidence with >= 1 part read");
                yield return AimAndUseHere("GATE-CONG");
                yield return Until(() => State.Happened("CH01-T02"), "Narrative route unlocks outer gate with gate key");
            }
            yield return Until(() => Quaternion.Angle(scene.outerGate.pivot.localRotation, Quaternion.Euler(0, scene.outerGate.openAngle, 0)) < 3f,
                "outer gate swings smoothly open");
            if (branch == FormStatus.Affirmed)
            {
                yield return Until(() => Application.isFocused, "movement check requires a focused window");
                yield return Tap(Key.F);
                yield return Until(() => player.IsTorchOn && player.TorchIntensity >= 32f, "F toggles boosted flashlight ON");
                yield return null;
                CaptureInvestigation("proposal-flashlight-bright");
                yield return null;
                float z = player.Feet.z;
                yield return Hold(Key.W, 0.4f);
                Require(player.Feet.z > z + 0.35f, "W moves the real capsule");
                yield return Tap(Key.F);
                yield return Until(() => !player.IsTorchOn, "F toggles flashlight OFF cleanly");
            }
            if (branch == FormStatus.Refuted)
            {
                // FULL UNINTERRUPTED KEYBOARD+MOUSE CH01->CH02 ROUTE (ZERO TELEPORTATION INSIDE CH01!)
                string record = Argument("-ctcc-record");
                captureExpansion = !string.IsNullOrEmpty(record);
                if (captureExpansion) StartCoroutine(CaptureExpansionFrames(Path.Combine(record, "ch01r-full")));
                try
                {
                    yield return WalkWithInput(new Vector3(4f, 0f, -18.0f));
                    yield return WalkWithInput(new Vector3(4f, 0f, -10.5f));
                    yield return WalkWithInput(new Vector3(4f, 0f, -3.30f));
                    Require(State.layer == Reality.R && !ui.IsModal, "walking across AP02 leaves the view and layer free");
                    yield return WalkWithInput(new Vector3(3.28f, 0f, -3.54f));
                    yield return AimAndUseHere(Ch01FieldObjects.Handset);
                    yield return Until(() => !busy && inspector.IsInspecting, "handset lifts from the physical cradle");
                    var handset = FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).Single(t => t.id == Ch01FieldObjects.Handset).inspectModel;
                    Require(!handset.activeSelf, "the world handset leaves the cradle while held");
                    yield return new WaitForSecondsRealtime(.45f);
                    var held = player.Eye.transform.Find("Physical Inspector Stage/Inspect Holder").GetComponentInChildren<MeshRenderer>();
                    Vector3 screen = player.Eye.WorldToViewportPoint(held.bounds.center);
                    Require(held.isVisible && screen.x is > .10f and < .61f && screen.y is > .14f and < .86f && screen.z > .15f,
                        "the lifted handset stays visible on the inspection side of the screen");
                    CaptureInvestigation("handset-raised");
                    yield return new WaitForEndOfFrame();
                    yield return Tap(Key.Escape);
                    yield return Until(() => !busy && !inspector.IsInspecting && handset.activeSelf, "cancel returns the physical handset to the cradle");
                    Vector3 entryFeet = player.Feet;
                    yield return AimAndUseHere("AN01");
                    yield return Until(() => !busy && State.layer == Reality.B && State.Happened("CH01-T03"), "reading the ledger changes reception in place");
                    Require(Vector3.Distance(entryFeet, player.Feet) < .18f && !ui.IsModal &&
                        !State.Happened("CH01-T04") && !State.Happened("CH01-T05") && !State.Observed(ObservationRegistry.StoppedClock0317),
                        "Pha 1 reception flip preserves stance and leaves Lan cinematic player-initiated");

                    // Pha 3: Player-initiated active cinematic dialogue scene with Lan + safe skip verification
                    yield return WalkWithInput(new Vector3(3.35f, 0f, -3.40f));
                    yield return AimAndUseHere("LAN_TALK");
                    yield return Until(() => lanDirector.IsCinematicActive && player.ExternalCameraControl, "active Lan cinematic starts on player input");
                    yield return new WaitForSecondsRealtime(0.85f);
                    CaptureInvestigation("walkthrough-lan-cinematic");
                    yield return null;
                    yield return Tap(Key.Space);
                    yield return Until(() => !busy && !lanDirector.IsCinematicActive && !player.ExternalCameraControl && State.Happened("CH01-T04"),
                        "Lan cinematic skip restores FPS control and commits CH01-T04");
                    Require(voice.Transcript.Any(entry => entry.LineId == "CH01-D07b") &&
                        !State.Happened("CH01-T05") && !State.Observed(ObservationRegistry.StoppedClock0317),
                        "Lan cinematic skip keeps full canonical transcript without auto-crediting clock or BOARD");

                    // Optional in-world Lan reflection after cinematic
                    yield return Until(() => NextLanReflection() != null, "Lan can answer optional question at the desk");
                    yield return AimAndUseHere("LAN_TALK");
                    Require(!ui.IsModal, "optional words with Lan stay in the room");
                    yield return Until(() => voice.Transcript.Any(t => t.LineId == "CH01-D08"), "optional Lan dialogue recorded");
                    yield return Tap(Key.N);
                    yield return Until(() => IsPocketNoteOpen && ui.IsModal, "[N] opens decoupled Sổ tay nhỏ");
                    // Phase 2.5 Ch01JournalPolicy: 5-tab Journal is hidden in B03; player reads Lan transcript via Sổ tay nhỏ!
                    Require(ui.GetComponentsInChildren<Button>(false).All(b =>
                        !(b.GetComponentInChildren<Text>()?.text ?? "").Contains("Mở sổ trực đầy đủ")),
                        "Ch01JournalPolicy keeps 5-tab Journal hidden in B03 Sổ tay nhỏ");
                    yield return Choose("Xem lại lời thoại đã nghe");
                    Require(ActivePageContains("Lan") && ActivePageContains(Line("CH01-D04c")) &&
                            !ActivePageContains("không thấy bóng em"),
                        "Lan transcript keeps the clock line and omits the retired reflection beat");
                    yield return Choose("Quay lại sổ tay nhỏ");
                    yield return Until(() => IsPocketNoteOpen && ui.IsModal, "TranscriptPage returns cleanly to Sổ tay nhỏ in CH01");
                    yield return Tap(Key.Escape);
                    if (ui.IsModal) yield return Tap(Key.Escape);

                    // Pha 4: Manual 3D BOARD standalone 0-sidebar inspect and pickup via [G] -> unlocks DOOR-101 ajar
                    yield return WalkWithInput(new Vector3(2.95f, 0f, -2.75f));
                    yield return AimAndUseHere("BOARD");
                    yield return Until(() => !busy && inspector.IsInspecting && ui.IsStandaloneOverlay, "BOARD opens in standalone 0-sidebar Physical Inspector 2.0");
                    yield return Tap(Key.G);
                    yield return Until(() => !busy && !ui.IsModal && State.Happened("CH01-T05"), "board granted after manual standalone inspect");
                    yield return new WaitForSecondsRealtime(0.25f);
                    float doorAjar = scene.interactables.GetDragOpenness01("DOOR-101");
                    Require(doorAjar > 0.06f && doorAjar < 0.30f, $"DOOR-101 unlocks ajar for physical entry (got {doorAjar:F2})");

                    // Walk optional compact nonlinear exploration loop: Reception -> STAFF Nursing Alcove -> open DOOR-STAFF -> HALL -> Room 101!
                    yield return WalkWithInput(new Vector3(0.05f, 0f, -1.0f));
                    yield return AimAndUseHere("OBS_CLOCK_0317");
                    yield return Until(() => !busy && State.Observed(ObservationRegistry.StoppedClock0317),
                        "read the stopped clock in reception");
                    yield return WalkWithInput(new Vector3(-0.12f, 0f, 3.65f));
                    yield return AimAndUseHere("CHAIR-STAFF");
                    yield return Until(() => State.FindPropPose("CHAIR-STAFF", Reality.B)?.openness > .9f, "STAFF chair slides and saves");
                    yield return AimAndUseHere("CH01_NURSE_DESK");
                    yield return Until(() => State.Observed("CH01_NURSE_DESK"), "STAFF desk note is optional and has provenance");
                    yield return AimAndUseHere("OBS_CORRIDOR_NOTICE");
                    yield return Until(() => !busy && inspector.IsInspecting && ui.IsStandaloneOverlay, "STAFF overflow notice inspected in 0-sidebar 3D");
                    Require(StaffTranscriptCount("CH01-D17") == 0, "notice opens neutrally and without a reaction");
                    yield return Tap(Key.Escape);
                    yield return Until(() => StaffTranscriptCount("CH01-D17") == 1 && staffReactionCaption != null,
                        "notice reacts after ordinary view and close");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-notice-reaction.png"));
                    yield return WalkWithInput(new Vector3(-0.15f, 0f, 3.92f));
                    yield return AimAndUseHere("DRAWER-NURSE");
                    yield return Until(() => scene.interactables.IsDrawerOpen("DRAWER-NURSE"), "STAFF nurse drawer slides open");
                    yield return new WaitForSecondsRealtime(0.45f);
                    yield return AimAndUseHere("CH01_DRAWER_CLIP");
                    yield return Until(() => !busy && inspector.IsInspecting && ui.IsStandaloneOverlay, "a separate mesh inside the open STAFF drawer can be lifted");
                    yield return Tap(Key.Escape);
                    yield return AimAndUseHere("OBS_SHIFT_LOG");
                    yield return Until(() => !busy && inspector.IsInspecting && ui.IsStandaloneOverlay, "Shift guide inspected standalone");
                    yield return Tap(Key.Digit3);
                    Require(!inspector.ShowingBack && inspector.ActiveDetailIndex == -1,
                        "Shift guide does not reveal a back detail from the front");
                    yield return Tap(Key.Escape);
                    Require(staffReactions.Count == 0 && StaffTranscriptCount("CH01-D18") == 0,
                        "Shift guide opening alone does not earn the reaction");
                    yield return AimAndUseHere("OBS_SHIFT_LOG");
                    yield return Until(() => !busy && inspector.IsInspecting && ui.IsStandaloneOverlay, "Shift guide reopened for detail inspection");
                    yield return Tap(Key.Space); // Test live keyboard flip!
                    yield return Until(() => inspector.ShowingBack, "Shift guide flipped to back via Space");
                    yield return Tap(Key.Digit3);
                    yield return Until(() => !busy && inspector.ActiveDetailIndex == 2, "Shift guide side-specific detail focused");
                    Require(StaffTranscriptCount("CH01-D18") == 0,
                        "Shift guide reaction stays quiet while reading the discovered detail");
                    yield return new WaitForSecondsRealtime(0.35f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-shift-back.png"));
                    yield return Tap(Key.G);
                    yield return Until(() => !busy && InventoryService.HasPhysical(State, BackpackCatalog.PaperShiftLog),
                        "Shift guide temporarily stowed in Inventory via [G]");
                    yield return Tap(Key.Escape);
                    yield return Until(() => StaffTranscriptCount("CH01-D18") == 1 && staffReactionCaption != null,
                        "Shift guide reacts after inspection and close");
                    // Place stowed Shift guide onto 2-slot comparison desk and capture 1080p visual proof!
                    yield return WalkWithInput(new Vector3(-0.08f, 0f, 3.70f));
                    yield return AimAndUseHere("COMPARE_DESK_STAFF");
                    yield return Choose("Đặt Quy chế bàn giao");
                    yield return Until(() => !busy &&
                        InventoryService.GetPlacedAt(State, WorldAnchors.StaffDeskLeft)?.itemDefId == BackpackCatalog.PaperShiftLog,
                        "Shift guide placed visibly on 2-slot comparison desk");
                    if (ui.IsModal) yield return Tap(Key.Escape);
                    yield return Until(() => !ui.IsModal, "STAFF comparison modal closed");
                    player.SetViewRotation(Quaternion.LookRotation(new Vector3(0.88f, 0.93f, 3.70f) - player.Eye.transform.position));
                    yield return null;
                    CaptureInvestigation("proposal-staff-desk-2slot");
                    yield return null;
                    yield return WalkWithInput(new Vector3(0.15f, 0f, 7.50f));
                    yield return WalkWithInput(new Vector3(1.55f, 0f, 7.50f));
                    yield return AimAndUseHere("DOOR-STAFF");
                    yield return Until(() => scene.staffCrossDoor.isOpen &&
                        Quaternion.Angle(scene.staffCrossDoor.pivot.localRotation, Quaternion.Euler(0, scene.staffCrossDoor.openAngle, 0)) < 2f,
                        "STAFF cross-door swings fully open via keyboard use");
                    yield return WalkWithInput(new Vector3(4.00f, 0f, 7.50f));
                    yield return WalkWithInput(new Vector3(4.00f, 0f, 4.75f));
                    yield return AimAndUseHere("DOOR-101");
                    yield return Until(() => scene.roomDoor.isOpen &&
                        Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.Euler(0, scene.roomDoor.openAngle, 0)) < 2f,
                        "ajar DOOR-101 swings open via player interaction");
                    yield return WalkWithInput(new Vector3(6.52f, 0f, 4.93f));
                    yield return Until(() => !busy && State.Happened("CH01-T06"), "Room 101 threshold checkpoint via keyboard walk");

                    // Investigate Bed 2 comparison doc, Bed 3 drawer + medication log, Bed 4 traces, and Bed 4 card!
                    yield return WalkWithInput(new Vector3(10.15f, 0f, 5.10f));
                    yield return AimAndUseHere("OBS101C");
                    yield return Until(() => !busy && inspector.IsInspecting, "OBS101C Bed-2 transfer inspected");
                    yield return Tap(Key.Escape);

                    yield return WalkWithInput(new Vector3(8.64f, 0f, 3.65f));
                    yield return AimAndUseHere("DRAWER-BED3");
                    yield return Until(() => scene.interactables.IsDrawerOpen("DRAWER-BED3"), "Bed-3 drawer slides open");
                    yield return new WaitForSecondsRealtime(0.45f);
                    yield return AimAndUseHere("OBS101D");
                    yield return Until(() => !busy && inspector.IsInspecting, "OBS101D Bed-3 meds inspected");
                    yield return Tap(Key.Escape);

                    yield return WalkWithInput(new Vector3(12.05f, 0f, 5.05f));
                    yield return AimAndUseHere("CURTAIN-101");
                    yield return Until(() => scene.interactables.curtainPulled, "Bed 4 curtain opened");
                    yield return new WaitForSecondsRealtime(0.55f);
                    yield return WalkWithInput(new Vector3(11.35f, 0f, 5.25f));
                    yield return AimAndUseHere("OBS101B");
                    yield return Until(() => !busy && inspector.IsInspecting && ui.IsStandaloneOverlay, "OBS101B card inspected standalone 0-sidebar");
                    yield return Hold(Key.A, 2.1f);
                    yield return Tap(Key.Digit3);
                    Require(!inspector.ShowingBack && inspector.ActiveDetailIndex == -1 &&
                            !State.FindObservation(ObservationRegistry.Bed4Card).flippedBack,
                        "unseen Bed-4 back remains hidden after tilt and shortcut");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-card-front.png"));
                    yield return Tap(Key.L);
                    yield return Until(() => !busy && !inspector.IsInspecting && ui.IsModal && !ui.IsStandaloneOverlay, "Bed-4 opt-in Large Print ready after [L]");
                    Require(ActivePageContains("MẶT SAU") &&
                            ActivePageContains("Phần ghi nơi chuyển bị nhòe") &&
                            State.FindObservation(ObservationRegistry.Bed4Card).HasDetail("LARGE_PRINT") &&
                            State.FindObservation(ObservationRegistry.Bed4Card).flippedBack,
                        "Bed-4 Large Print grants both sides without explicit flip");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-card-large-print.png"));
                    Require(!ActivePageContains("03g17") && !CorroborationService.CanCorroborate0317(State),
                        "Large Print opening alone does not disclose the hidden time");
                    yield return Choose("Đọc vết hằn mặt sau");
                    yield return Until(() => !busy && CorroborationService.HasCardTimestamp(State) &&
                        ActivePageContains("03g17") && !ActivePageContains("C01"), "accessible reading finds a canon-safe card time");
                    yield return Choose("Người nhận bàn giao: ô ký nhận trống");
                    yield return Until(() => !busy && State.f101Annotations.MarkedHotspot(ObservationRegistry.Bed4Card) == F101Annotations.Receiver,
                        "AS-05B blank receiver box marked directly on card");
                    yield return Choose("Cất tạm vào ba lô (Lớp B)");
                    yield return Until(() => !busy && InventoryService.HasPhysical(State, BackpackCatalog.PaperObs101B),
                        "AS-05B temporarily stowed in Backpack (Layer B)");
                    yield return Tap(Key.Escape);

                    yield return WalkWithInput(new Vector3(10.75f, 0f, 4.35f));
                    yield return AimAndUseHere("OBS101A_BLANKET");
                    yield return Until(() => !busy && State.Observed("OBS101A"), "OBS101A Bed-4 trace observed");
                    Require(CanSpatialLinkBed4(), "Spatial Bed-4 cross-check eligible once OBS101A & marked AS-05B are verified in alcove");
                    player.CanPlay = false;
                    yield return Tap(Key.Q);
                    RefreshControls();
                    yield return Until(() => !busy && State.f101Annotations.HasLink &&
                        F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.SpatialFieldVerified,
                        "Spatial Bed-4 cross-check commits SpatialFieldVerified provenance in-world");

                    // Clip stowed AS-05B physically onto COMPARE_CLIP_AN02 beside F101 (standing clear of midCurtain)!
                    yield return WalkWithInput(new Vector3(6.42f, 0f, 4.92f));
                    yield return WalkWithInput(new Vector3(6.24f, 0f, 5.64f));
                    yield return AimAndUseHere("COMPARE_CLIP_AN02");
                    yield return Choose("Kẹp Thẻ AS-05B");
                    yield return Until(() => !busy && !ui.IsModal &&
                        InventoryService.GetPlacedAt(State, WorldAnchors.An02CompareClip)?.itemDefId == BackpackCatalog.PaperObs101B,
                        "AS-05B physically mounted on comparison clip beside F101");
                    player.Eye.transform.LookAt(new Vector3(5.73f, 1.36f, 5.64f));
                    yield return null;
                    CaptureInvestigation("walkthrough-an02-clip");
                    yield return null;

                    yield return Tap(Key.Tab);
                    yield return Until(() => ui.IsInventory2Open && backpackStage != null && backpackStage.IsOpen, "Inventory 2.0 + 3D preview opened via Tab");
                    yield return new WaitForSecondsRealtime(0.28f);
                    CaptureInvestigation("proposal-backpack-3d");
                    yield return null;
                    yield return Choose("Giấy tờ");
                    yield return new WaitForSecondsRealtime(0.16f);
                    CaptureInvestigation("walkthrough-backpack-papers");
                    yield return null;
                    Require(ui.GetComponentsInChildren<Button>(false).All(b =>
                        !(b.GetComponentInChildren<Text>()?.text ?? "").Contains("Mở sổ ghi chép") &&
                        !(b.GetComponentInChildren<Text>()?.text ?? "").Contains("Mở sổ trực đầy đủ")),
                        "Ch01JournalPolicy locks 100% of 5-tab Journal buttons in CH01 B04-B05 Inventory 2.0");
                    yield return Tap(Key.Escape);
                    yield return Until(() => !ui.IsModal, "Inventory 2.0 closed cleanly");

                    // Phase 2.5 Diegetic Direct-Surface 3D AN02 Clipboard (0 sidebar default!) + Free HH:MM EX01 + Spatial Provenance C!
                    yield return WalkWithInput(new Vector3(7.36f, 0f, 4.98f));
                    yield return AimAndUseHere("F101");
                    yield return Until(() => !busy && IsAn02DirectSurfaceActive && ui.IsStandaloneOverlay, "Diegetic Direct-Surface AN02 3D opens with 0 sidebar");
                    Require(!transaction.OnPad(player.Feet, "AP04"), "F101 is reachable from a free stance outside AP04");
                    yield return new WaitForSecondsRealtime(0.24f);
                    CaptureInvestigation("an02-3d-clipboard-100");
                    yield return null;

                    // Solve EX01 directly on the pinned 3D margin slip via keyboard 0-9 (03:16 rejected -> 03:17 committed!)
                    Require(CorroborationService.CanCorroborate0317(State) && !CorroborationService.IsCorroborated(State),
                        "EX01 stays unsolved until player explicitly enters 03:17 on 3D margin slip");
                    yield return Tap(Key.Digit0);
                    yield return Tap(Key.Digit3);
                    yield return Tap(Key.Digit1);
                    yield return Tap(Key.Digit6);
                    Require(an02Surface.TriggerZoneById("EX01_SUBMIT"), "Submit wrong EX01 time 03:16 on 3D slip");
                    yield return Until(() => !busy, "EX01 03:16 settle");
                    Require(!CorroborationService.IsCorroborated(State), "EX01 rejects wrong player-entered time 03:16");
                    Require(an02Surface.TriggerZoneById("EX01_RESET"), "Reset EX01 digits on 3D slip");
                    yield return Tap(Key.Digit0);
                    yield return Tap(Key.Digit3);
                    yield return Tap(Key.Digit1);
                    yield return Tap(Key.Digit7);
                    yield return null;
                    CaptureInvestigation("an02-3d-ex01-hhmm");
                    yield return null;
                    Require(an02Surface.TriggerZoneById("EX01_SUBMIT"), "Submit 03:17 on 3D slip");
                    yield return Until(() => !busy && CorroborationService.IsCorroborated(State),
                        "EX01 commits CH01-EX01 cleanly on player-entered 03:17 via 3D slip");
                    Require(State.exception0317Link.context == ObservationRegistry.ShiftGuide, "EX01 records shift context");

                    // Lift F101 sheet in 3D via [F] -> flip -> return seamlessly to AN02!
                    yield return Tap(Key.F);
                    yield return Until(() => !busy && inspector.IsInspecting, "F101 lifted from AN02 into 3D inspector via [F]");
                    yield return Tap(Key.Space);
                    yield return Until(() => inspector.ShowingBack, "F101 flipped to back");
                    yield return Tap(Key.Digit3);
                    yield return Until(() => !busy && inspector.ActiveDetailIndex == 2, "F101 back detail focused after explicit flip");
                    yield return Tap(Key.Escape);
                    yield return Until(() => !busy && !inspector.IsInspecting && IsAn02DirectSurfaceActive, "returned cleanly to Diegetic AN02 3D surface");

                    // Mark F101 zero line directly on 3D F101 sheet + verify Esc preserves F101Draft!
                    Require(an02Surface.TriggerZoneById("MARK_ZERO"), "Click F101_ZERO on 3D paper");
                    yield return Until(() => !busy && State.f101Annotations.MarkedHotspot(ObservationRegistry.F101Sheet) == F101Annotations.Zero,
                        "AN02 3D surface marks F101 zero line");
                    Require(an02Surface.TriggerZoneById("OPT_B"), "Click Option B on 3D paper");
                    yield return Tap(Key.Escape);
                    yield return Until(() => !IsAn02Clipboard3DActive && !ui.IsModal && reportDraft.Decision == FormStatus.Unverified,
                        "Esc exits 3D AN02 surface while preserving F101Draft");
                    yield return AimAndUseHere("F101");
                    yield return Until(() => !busy && IsAn02DirectSurfaceActive && ui.IsStandaloneOverlay, "3D AN02 surface reopens with draft preserved");

                    // Select Branch C directly on 3D paper -> test irrelevant source & overclaim rejected -> commit valid C!
                    Require(an02Surface.TriggerZoneById("OPT_C"), "Select Branch C on 3D paper");
                    Require(an02Surface.TriggerZoneById("SRC_" + ObservationRegistry.F101Sheet), "Attach F101 on 3D slip");
                    Require(an02Surface.TriggerZoneById("SRC_" + ObservationRegistry.Bed4Card), "Attach AS-05B on 3D slip");
                    Require(an02Surface.TriggerZoneById("SRC_" + ObservationRegistry.Bed4Traces), "Attach Bed4 traces on 3D slip");
                    Require(an02Surface.TriggerZoneById("SRC_" + ObservationRegistry.Bed3Meds), "Attach irrelevant Bed3 meds on 3D slip");
                    Require(an02Surface.TriggerZoneById("CLM_" + HypothesisService.ClaimUnreliableZero), "Select ClaimUnreliableZero on 3D slip");
                    Require(an02Surface.TriggerZoneById("SIGN_PREVIEW"), "Attempt preview with irrelevant Bed3 meds");
                    Require(an02View == An02View.Clipboard, "unrelated Bed-3 meds rejected on 3D AN02 surface");
                    Require(an02Surface.TriggerZoneById("SRC_" + ObservationRegistry.Bed3Meds), "Detach Bed3 meds on 3D slip");
                    Require(an02Surface.TriggerZoneById("CLM_" + HypothesisService.ClaimPatientPresent), "Select overclaim on 3D slip");
                    Require(an02Surface.TriggerZoneById("SIGN_PREVIEW"), "Attempt preview with overclaim");
                    Require(an02View == An02View.Clipboard, "unsupported overclaim rejected on 3D AN02 surface");
                    Require(an02Surface.TriggerZoneById("CLM_" + HypothesisService.ClaimUnreliableZero), "Restore valid claim on 3D slip");

                    var annotationSaves = new AtomicSaves(Argument("-ctcc-save"));
                    Require(annotationSaves.Read(SaveSlot.Auto, out var annotated) == SaveRead.Valid &&
                        annotated.f101Annotations.SupportsRefutation(annotated) &&
                        F101Annotations.ResolveLinkProvenance(annotated) == F101LinkProvenance.SpatialFieldVerified &&
                        annotated.f101History.Count == 0,
                        "draft marks and SpatialFieldVerified link round-trip before the first signature");

                    // 2-step Preview -> Sign N. H. Minh -> Red Stamp directly on the 3D F101 paper!
                    Require(an02Surface.TriggerZoneById("SIGN_PREVIEW") && an02View == An02View.Preview, "AN02 3D Preview armed");
                    Require(an02Surface.TriggerZoneById("SIGN_NAME") && an02View == An02View.Confirm, "AN02 3D Sign N. H. Minh armed");
                    Require(an02Surface.TriggerZoneById("SIGN_CANCEL") && an02View == An02View.Clipboard && State.f101.revision == 0 && !GateService.G01(State),
                        "cancel after signing leaves history and G01 untouched");
                    Require(an02Surface.TriggerZoneById("SIGN_PREVIEW") && an02Surface.TriggerZoneById("SIGN_NAME") && an02Surface.TriggerZoneById("SIGN_STAMP"),
                        "AN02 3D 2-step sign and stamp triggered");
                    yield return Until(() => !busy && !ui.IsModal && !IsAn02Clipboard3DActive && State.f101.revision == 1,
                        "F101 Refuted committed via Diegetic Direct-Surface AN02 3D with SpatialFieldVerified provenance");
                    Require(State.f101.firstDecision == FormStatus.Refuted && State.f101.revision == 1 &&
                        State.f101Argument.annotationVersion == F101Annotations.Version &&
                        State.f101Argument.selectedEvidenceIds.Count == 3 &&
                        State.f101History[0].annotations.SupportsRefutation(State) &&
                        F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.SpatialFieldVerified &&
                        scene.refutedBay != null && scene.refutedBay.activeInHierarchy, "Refuted visual, 3D AN02 & spatial provenance verified");
                    verification.Add("CH01 REBIRTH III PHASE 2.5 INPUT PASS: 0 Journal in CH01, Diegetic Phone 3D, Physical Envelope 3D, Inventory 2.0 + decoupled Sổ tay nhỏ, Standalone Physical Inspector 2.0, Diegetic Direct-Surface AN02 3D with EX01 & 2-step N. H. Minh confirmation.");
                    yield return LookForWardConsequence(branch);
                    if (captureExpansion) yield return new WaitForSecondsRealtime(0.75f);

                    // Walk back out of Room 101 to Reception (B->R collapse), outer gate seam, and Reception (CH02 handoff) — ZERO TELEPORTS!
                    yield return WalkWithInput(new Vector3(4.00f, 0f, 4.75f));
                    yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.F101RefutedCrack),
                        "Refuted reaction plays on the normal return route before collapse");
                    yield return Until(() => voice.Transcript.Any(entry => entry.LineId == "CH01-D19"), "03:17 monologue begins in the corridor",
                        timeoutSeconds: 15);
                    yield return Until(() => !voice.IsSpeaking, "03:17 monologue finishes before collapse", timeoutSeconds: 20);
                    verification.Add("CH01 INPUT PASS: full Lan scene, Large Print time reveal, wrong and right time, optional monologue, and Refuted signing reaction.");
                    player.Eye.transform.LookAt(new Vector3(5.46f, 1.66f, 3.78f));
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "f101-corridor-slip.png"));
                    yield return null;
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -3.30f));
                    yield return AimAndUseHere("AN01");
                    yield return Until(() => !busy && State.layer == Reality.R && State.Happened("CH01-T08"), "reception B→R collapse at the ledger");
                    Require(InventoryService.FindByDef(State, BackpackCatalog.PaperObs101B)?.status == BackpackItemStatus.MemoryRef &&
                        InventoryService.GetPlacedAt(State, WorldAnchors.An02CompareClip) == null && State.evidence.Count == 1,
                        "B->R collapse reconciles clipped AS-05B to MemoryRef with zero R export");
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -10.5f));
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -19.0f));
                    Require(scene.outerGate != null && scene.outerGate.isOpen,
                        "Phase 3 B06: outer gate remains physically open after T08 collapse");
                    yield return AimAndUseHere("SEAM");
                    yield return Until(() => !busy && State.Happened("CH01-T09") && State.HasDreadEvent(DreadCatalog.B07GateSlam),
                        "B07 intentional gate pull triggers Peak Horror #2 and continuity seam");
                    // Phase 3 B07 Live Keyboard WR05 Swept-Boundary Check: sprint straight into open gate -> 0 overshoot, 0 backward push!
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -19.55f));
                    player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    InputSystem.QueueStateEvent(qaKeys, new KeyboardState(Key.W, Key.LeftShift));
                    bool inputWr05Engaged = false;
                    float wr05StartAt = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - wr05StartAt < 0.70f)
                    {
                        qaKeys.MakeCurrent();
                        yield return null;
                        if (player.Wr05EngagedThisFrame) inputWr05Engaged = true;
                        Require(player.Feet.z >= Wr05ExitBoundaryFilter.ExitThresholdZ - 0.035f,
                            $"Phase 3 Input QA WR05 blocks sprint past gate threshold (z={player.Feet.z:F3})");
                    }
                    InputSystem.QueueStateEvent(qaKeys, new KeyboardState());
                    yield return Until(() => !qaKeys[Key.W].isPressed && !player.Wr05EngagedThisFrame,
                        "Phase 3 Input QA WR05 disengages cleanly on key release");
                    Require(inputWr05Engaged && (scene.seamBlocker == null || !scene.seamBlocker.activeSelf),
                        "Phase 3 Input QA WR05 engaged cleanly with zero seamBlocker collider");
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -10.5f));
                    yield return WalkWithInput(new Vector3(3.25f, 0f, -3.24f));
                    // Phase 3 B08 In-World Perceptual Echo check & capture at LOC02 R reception counter!
                    Require(scene.B08Echo != null && scene.B08Echo.IsManifesting && scene.B08Echo.ActiveBranch == FormStatus.Refuted &&
                            !scene.B08Echo.HasAnyCollider,
                        "Phase 3 Input QA: B08 in-world Perceptual Echo manifests Refuted branch at LOC02 R with 0 colliders");
                    player.SetViewRotation(Quaternion.LookRotation(new Vector3(2.02f, 1.32f, -3.24f) - player.Eye.transform.position));
                    yield return new WaitForSecondsRealtime(0.28f);
                    CaptureInvestigation("phase3-b08-inworld-echo-R");
                    yield return null;
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -3.30f));
                    yield return Tap(Key.N);
                    yield return Until(() => IsPocketNoteOpen && ui.IsModal, "B08 [N] opens decoupled Sổ tay nhỏ");
                    Require(ActivePageContains("chưa đủ căn cứ xác nhận"), "B08 Sổ tay nhỏ reflects Refuted choice");
                    yield return new WaitForSecondsRealtime(0.24f);
                    CaptureInvestigation("walkthrough-b08-pocket-note");
                    yield return null;
                    yield return Tap(Key.Escape);
                    yield return Until(() => !ui.IsModal, "B08 Sổ tay nhỏ closed");
                    yield return Tap(Key.Tab);
                    yield return Until(() => ui.IsInventory2Open, "B08 Inventory 2.0 opens");
                    yield return Choose("Giấy tờ");
                    yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.Bag08Echo), "B08 P_BAG08_ECHO triggers once when AS-05B was examined in B");
                    yield return new WaitForSecondsRealtime(0.24f);
                    CaptureInvestigation("walkthrough-b08-echo");
                    yield return null;
                    yield return Tap(Key.Escape);
                    yield return AimAndUseHere("AN01");
                    yield return Until(() => !busy && State.chapter == "CH02", "CH02 handoff via pure keyboard+mouse CH01 route");
                    verification.Add("INPUT CH01-R FULL ROUTE PASS: zero teleportation from AP00 through Diegetic Phone 3D, Physical Envelope 3D, Lan cinematic+skip, standalone BOARD inspect, ajar DOOR-101, STAFF loop, Room 101 stow+AN02 clip, Diegetic Direct-Surface AN02, B->R reconciliation, B07 gate pull+slam, B08 echo+Sổ tay nhỏ, and CH02 handoff.");
                }
                finally { captureExpansion = false; }
            }
            else
            {
                yield return WalkWithInput(new Vector3(4f, 0f, -18.0f));
                yield return WalkWithInput(new Vector3(4f, 0f, -10.5f));
                yield return WalkWithInput(new Vector3(4f, 0f, -3.30f));
                yield return AimAndUseHere("AN01");
                yield return Until(() => !busy && State.layer == Reality.B && State.Happened("CH01-T03"), "reception R→B");
                yield return WalkWithInput(new Vector3(3.35f, 0f, -3.40f));
                yield return AimAndUseHere("LAN_TALK");
                yield return Until(() => lanDirector.IsCinematicActive, "Lan cinematic started on fast route");
                yield return new WaitForSecondsRealtime(0.38f);
                yield return Tap(Key.Space);
                yield return Until(() => !busy && !lanDirector.IsCinematicActive && State.Happened("CH01-T04"), "Lan cinematic skipped cleanly");
                yield return WalkWithInput(new Vector3(2.98f, 0f, -2.76f));
                yield return AimAndUseHere("BOARD");
                yield return Until(() => !busy && inspector.IsInspecting && ui.IsStandaloneOverlay, "BOARD inspected standalone");
                yield return Tap(Key.G);
                yield return Until(() => !busy && !ui.IsModal && State.Happened("CH01-T05"), "board granted");

                yield return WalkWithInput(new Vector3(4f, 0f, -1f));
                yield return WalkWithInput(new Vector3(4f, 0f, 4.77f));
                yield return AimAndUseHere("DOOR-101");
                yield return Until(() => scene.roomDoor.isOpen &&
                    Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.Euler(0, scene.roomDoor.openAngle, 0)) < 2f,
                    "room door opens physically on player use");
                yield return WalkWithInput(new Vector3(6.52f, 0f, 4.90f));
                if (branch == FormStatus.Affirmed)
                {
                    yield return AimAndUseHere("DOOR-101");
                    yield return Until(() => !scene.roomDoor.isOpen && Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.identity) < 1,
                        "door closes through use input");
                    yield return AimAndUseHere("DOOR-101");
                    yield return Until(() => scene.roomDoor.isOpen && Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.Euler(0, scene.roomDoor.openAngle, 0)) < 1,
                        "door reopens through use input");
                    yield return WalkWithInput(new Vector3(10.15f, 0f, 5.10f));
                    yield return AimAndUseHere("OBS101C");
                    yield return Until(() => !busy && inspector.IsInspecting, "first look goes to a document");
                    Require(!State.Observed(ObservationRegistry.Bed4Traces), "the document can be read before examining bed 4");
                    yield return Tap(Key.Escape);
                }
                if (branch == FormStatus.Unverified)
                {
                    var cloth = scene.interactables.curtainCloth;
                    var scale = cloth.localScale;
                    yield return WalkWithInput(new Vector3(11.68f, 0.02f, 4.92f));
                    var curtain = FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).Single(t => t.id == "CURTAIN-101");
                    var sightSurface = curtain.visibilitySurface;
                    Require(sightSurface != null && sightSurface.isTrigger, "cloth carries a dedicated sight collider");
                    float closedWidth = sightSurface.bounds.size.x;
                    player.Eye.transform.LookAt(curtain.transform.position);
                    player.CanPlay = false;
                    yield return Tap(Key.Q);
                    RefreshControls();
                    yield return Until(() => !busy && Mathf.Abs((State.FindPropPose("CURTAIN-101", Reality.B)?.openness ?? -1f) - .5f) < .01f,
                        "keyboard half-open curtain pose is saved");
                    yield return AimAndUseHere("CURTAIN-101");
                    yield return Until(() => scene.interactables.curtainPulled && (cloth.localScale - scale).sqrMagnitude > 0.05f,
                        "curtain opens through use input");
                    Require(sightSurface.bounds.size.x < .88f * closedWidth,
                        "curtain shrinks its sight collider along the rail");
                    yield return AimAndUseHere("CURTAIN-101");
                    yield return Until(() => !scene.interactables.curtainPulled && (cloth.localScale - scale).sqrMagnitude < 0.001f,
                        "curtain closes through use input");
                    yield return AimAndUseHere("CURTAIN-101");
                    yield return Until(() => scene.interactables.curtainPulled, "curtain opens again for field inspection");
                    yield return new WaitForSecondsRealtime(.5f);
                    yield return WalkWithInput(new Vector3(10.75f, 0f, 4.56f));
                    yield return AimAndUseHere("OBS101A_BLANKET");
                    yield return Until(() => State.Observed(ObservationRegistry.Bed4Traces), "site-first route records physical traces");
                    Require(!State.Observed(ObservationRegistry.Bed2Transfer) && !State.Observed(ObservationRegistry.Bed4Card),
                        "the trace can be recorded without reading any bed document");
                }

                yield return WalkWithInput(new Vector3(7.24f, 0f, 4.93f));
                yield return OpenFormInput();
                Require(IsAn02DirectSurfaceActive && ui.IsStandaloneOverlay, $"Diegetic Direct-Surface AN02 opens for {branch}");
                Require(an02Surface.TriggerZoneById(branch == FormStatus.Affirmed ? "OPT_A" : "OPT_B"), $"Select {branch} on 3D paper");
                Require(an02Surface.TriggerZoneById("SIGN_PREVIEW") && an02View == An02View.Preview, $"Preview {branch} on 3D paper");
                Require(an02Surface.TriggerZoneById("SIGN_NAME") && an02View == An02View.Confirm, $"Sign N. H. Minh {branch} on 3D paper");
                if (branch == FormStatus.Affirmed)
                {
                    long sequence = State.sequence;
                    string autosave = saves.SlotPath(SaveSlot.Auto);
                    byte[] before = File.ReadAllBytes(autosave);
                    using (var slotLock = new FileStream(autosave + ".lock", FileMode.Open,
                        FileAccess.ReadWrite, FileShare.None))
                    {
                        Require(an02Surface.TriggerZoneById("SIGN_STAMP"), "Attempt stamp with locked autosave");
                        yield return Until(() => !busy && !string.IsNullOrEmpty(formFeedback) && formFeedback.Contains("Chưa ghi được bản lưu"), "UI retries a blocked atomic save");
                        Require(State.sequence == sequence && State.f101.revision == 0 && !GateService.G01(State) &&
                            an02View == An02View.Confirm,
                            "failed publication leaves the preview available, without signing or stamping");
                        Require(before.SequenceEqual(File.ReadAllBytes(autosave)) && !File.Exists(autosave + ".tmp"),
                            "the competing slot owner leaves the previous autosave byte-for-byte intact");
                        CaptureInvestigation("f101-save-blocked");
                        yield return null;
                    }
                    verification.Add("REBIRTH II SAVE FAILURE PASS: locked autosave leaves the same draft and revision, with the gate closed.");
                }
                Require(an02Surface.TriggerZoneById("SIGN_STAMP"), $"Stamp {branch} on 3D paper");
                yield return Until(() => !busy && !IsAn02Clipboard3DActive && GateService.G01(State), $"F101 {branch} confirmation");
                Require(State.f101.firstDecision == branch && State.f101.revision == 1 &&
                    FormService.Distortion(State) == (branch == FormStatus.Affirmed ? 1 : 0), $"F101 {branch} state");
                if (branch == FormStatus.Affirmed)
                {
                    Require(saves.Read(SaveSlot.Auto, out var recovered) == SaveRead.Valid && recovered.f101.revision == 1 &&
                        recovered.f101.firstDecision == branch && !File.Exists(saves.SlotPath(SaveSlot.Auto) + ".tmp"),
                        "retry after disposal durably publishes one F101 decision to the primary slot");
                    verification.Add("F101 SLOT RETRY PASS: lock disposal allows one durable publication without leaving a temp snapshot.");
                }
                yield return Until(() => !busy && State.HasDreadEvent(AmbientDreadDirector.F101ReactionId(branch)),
                    $"F101 {branch} cue follows the signature");
                if (branch == FormStatus.Unverified)
                {
                    var audio = scene.atmosphere;
                    bool SignatureCuePlaying() => audio.GetComponentsInChildren<AudioSource>().Any(source =>
                        source.isPlaying && (source.clip == audio.stamp || source.clip == audio.switchLayer));
                    yield return new WaitForSecondsRealtime(2.3f);
                    yield return Until(() => !voice.IsSpeaking && !SignatureCuePlaying(), "first signature cue finished");
                    int historyCount = State.f101History.Count;
                    yield return OpenFormInput();
                    Require(an02Surface.TriggerZoneById("OPT_B") && an02Surface.TriggerZoneById("SIGN_PREVIEW") && an02Surface.TriggerZoneById("SIGN_NAME"),
                        "Duplicate B sign armed on 3D paper");
                    long sequence = State.sequence;
                    Require(an02Surface.TriggerZoneById("SIGN_STAMP"), "Duplicate B stamp");
                    yield return Until(() => !busy && !ui.IsModal, "duplicate F101 submission closes");
                    Require(!voice.IsSpeaking && !SignatureCuePlaying() && State.sequence == sequence &&
                        State.f101.revision == 1 && State.f101History.Count == historyCount,
                        "Unchanged F101 UI submission issues no PA, stamp, transition cue, save or revision " +
                        $"[speaking={voice.IsSpeaking} cue={SignatureCuePlaying()} seq={State.sequence}/{sequence} rev={State.f101.revision} " +
                        $"hist={State.f101History.Count}/{historyCount} transcript={(voice.Transcript.Count > 0 ? voice.Transcript.Last().LineId : "none")}]");
                    verification.Add("TASK03 INPUT PASS: unchanged F101 submission through the 3D surface is silent and preserves the revision.");
                }
                yield return LookForWardConsequence(branch);
                yield return WalkWithInput(new Vector3(4f, 0f, 4.75f));
                yield return WalkWithInput(new Vector3(4f, 0f, -3.30f));
                yield return Until(() => !F101SignaturePlaying, "the signature response finishes before the return");
                yield return AimAndUseHere("AN01");
                yield return Until(() => !busy && State.layer == Reality.R, "reception B→R");
                yield return WalkWithInput(new Vector3(4.00f, 0f, -10.5f));
                yield return WalkWithInput(new Vector3(4.00f, 0f, -19.0f));
                yield return AimAndUseHere("SEAM");
                yield return Until(() => !busy && State.Happened("CH01-T09"), "exterior seam via gate pull");
                yield return WalkWithInput(new Vector3(4.00f, 0f, -10.5f));
                yield return WalkWithInput(new Vector3(4.00f, 0f, -3.30f));
                yield return Tap(Key.N);
                yield return Until(() => IsPocketNoteOpen && ui.IsModal, "B08 [N] Sổ tay nhỏ");
                Require(ActivePageContains(branch == FormStatus.Affirmed ? "đã ký xác nhận số 0" : "chưa xác minh"),
                    $"B08 Sổ tay nhỏ reflects {branch} choice");
                yield return Tap(Key.Escape);
                yield return Until(() => !ui.IsModal, "B08 Sổ tay nhỏ closed");
                yield return Tap(Key.Tab);
                yield return Until(() => ui.IsInventory2Open, "B08 Inventory 2.0");
                yield return Choose("Giấy tờ");
                yield return new WaitForSecondsRealtime(0.20f);
                Require(!State.HasDreadEvent(DreadCatalog.Bag08Echo),
                    $"P_BAG08_ECHO strictly does NOT fire on {branch} route where AS-05B was never examined");
                yield return Tap(Key.Escape);
                yield return AimAndUseHere("AN01");
                yield return Until(() => !busy && State.chapter == "CH02", "CH02 handoff");
            }

            if (branch == FormStatus.Unverified && scene.IsAdditiveMode &&
                (s1Only || !Environment.GetCommandLineArgs().Contains("-ctcc-ch01-ch02-only")))
                yield return InputS1BeforeG02();
            if (s1Only) yield break;
            if (branch != FormStatus.Refuted) yield return InputPhysicalCase(branch == FormStatus.Unverified);
            player.Teleport(scene.Pad("AP02"));
            yield return Tap(Key.Escape);
            if (branch == FormStatus.Refuted)
            {
                yield return Choose("Cài đặt & Trợ năng");
                foreach (string label in new[] { "Chuyển động", "Hỗ trợ lấy nét", "Chú thích âm thanh", "Âm lượng tổng", "Âm lượng tổng", "Âm nền môi trường", "Âm nền môi trường",
                    "Âm thanh ·", "Cỡ chữ", "Độ sáng", "Phím ·" }) yield return Choose(label);
                Require(State.settings.reduceMotion && State.settings.disableCameraAssist && !State.settings.captions && State.settings.volume == 0 &&
                    State.settings.ambienceVolume == 0 && State.settings.monoAudio && State.settings.textScale > 1.25f &&
                    State.settings.visibility > 0 && State.settings.alternateBindings && AudioListener.volume == 0,
                    "accessibility settings changed through actual controls");
                yield return Tap(Key.Escape);
            }
            yield return Choose("Lưu tay");
            yield return Tap(Key.Escape);
            yield return Choose("Mở bản lưu tay");
            yield return Until(() => !ui.IsModal && !busy, "manual reload finished");
            Require(!ui.IsModal && State.chapter == "CH02" && State.f101.firstDecision == branch &&
                transaction.OnPad(player.Feet, SafeCheckpoints.Pad(State.checkpoint)), "manual load restores state and checkpoint");
            Require(voice.Transcript.Any(entry => entry.LineId == "CH01-D07"), "Skipped Lan transcript survives actual manual load");
            Require(StaffTranscriptCount("CH01-D17") == (branch == FormStatus.Refuted ? 1 : 0) &&
                    StaffTranscriptCount("CH01-D18") == (branch == FormStatus.Refuted ? 1 : 0),
                "STAFF reactions survive reload once and remain absent on routes that skip STAFF");
            if (branch == FormStatus.Refuted)
            {
                Require(State.f101.current == FormStatus.Refuted && State.f101History.Count == 1 &&
                    State.f101History[0].claimId == HypothesisService.ClaimUnreliableZero &&
                    State.f101Argument.claimId == HypothesisService.ClaimUnreliableZero &&
                    State.f101Argument.selectedEvidenceIds.Count == 3 &&
                    State.f101Argument.selectedEvidenceIds.Contains(ObservationRegistry.F101Sheet) &&
                    State.f101Argument.selectedEvidenceIds.Contains(ObservationRegistry.Bed4Traces) &&
                    State.f101Argument.selectedEvidenceIds.Contains(ObservationRegistry.Bed4Card) &&
                    State.f101Argument.annotations.SupportsRefutation(State) &&
                    State.f101History[0].annotations.SupportsRefutation(State) &&
                    State.FindObservation(ObservationRegistry.Bed4Card).HasDetail("LARGE_PRINT") &&
                    State.FindObservation(ObservationRegistry.ShiftGuide).HasDetail("SHIFT_BACK") &&
                    State.FindObservation(ObservationRegistry.F101Sheet).HasDetail("F101_BACK"),
                    "Supported F101 argument and its single committed revision survive CH02 manual load");
                Require(State.settings.alternateBindings && State.settings.textScale > 1.25f && AudioListener.volume == 0,
                    "accessibility survives manual load");
                Require(State.FindPropPose("CHAIR-STAFF", Reality.B)?.openness > .9f &&
                    State.FindPropPose("DRAWER-NURSE", Reality.B)?.openness > .9f &&
                    State.journalNotes.Count == 0,
                    "independent physical poses survive handoff and CH01 stays 100% zero-Journal");
                yield return Tap(Key.J);
                yield return Until(() => ui.IsInventory2Open, "remapped J opens CH02 Inventory 2.0");
                yield return Choose("Sổ tay [N]");
                yield return Choose("Mở sổ trực đầy đủ");
                yield return VerifyNotebookInput();
                if (ui.IsModal) yield return Tap(Key.Escape);
                yield return NotebookLifetimeStress();
                Require(State.journalNotes.Count == 2 && State.journalNotes[0].paperMark.placed &&
                    State.journalNotes[1].source == ObservationRegistry.Bed4Card &&
                    string.IsNullOrEmpty(State.journalNotes[1].related),
                    "CH02 5-tab Journal authoring & 100-cycle lifetime stress verified post-handoff");
            }
            verification.Add($"INPUT PASS {branch}: Unity Input System, focus/interaction and UI, F101 branch, CH02 handoff, manual save/load.");
            if (branch == FormStatus.Unverified && !Environment.GetCommandLineArgs().Contains("-ctcc-ch01-ch02-only"))
            {
                try { yield return InputHospitalExpansion(); }
                finally { captureExpansion = false; }
            }
            yield return Tap(Key.Escape);
            yield return Choose("Về màn hình đầu");
        }

        private IEnumerator InputS1BeforeG02()
        {
            Require(State.chapter == "CH02" && State.layer == Reality.R && !GateService.G02(State),
                "S1 input starts in R before unlocking G02");
            var reception = scene.Pad("AP02").position;
            // Circle the south end of the solid duty desk before approaching the west stair.
            Vector3[] receptionApproach = { new(4f, 0f, -6.15f), new(-.10f, 0f, -6.15f), new(-.10f, 0f, -4f) };
            yield return WalkWithInput(reception);
            foreach (var point in receptionApproach) yield return WalkWithInput(point);
            yield return Until(() => scene.HasZone("LOC05") && !streaming, "S1 streams from the reception approach");
            var dischargeLanding = new Vector3(-2.05f, 0f, -4f);
            yield return WalkWithInput(dischargeLanding);
            yield return WalkWithInput(new Vector3(dischargeLanding.x, 0f, -7.35f));
            player.transform.rotation = Quaternion.LookRotation(Vector3.back);
            yield return Hold(State.settings.alternateBindings ? Key.UpArrow : Key.W, .8f);
            Require(player.Feet.z > -7.86f, "S1 discharge mesh stops the controller before the courtyard");
            yield return WalkWithInput(dischargeLanding);

            Vector3[] route = {
                new(-11.9f, 4f, -4f), new(-12.3f, 4f, 9.5f), new(-12.3f, 4f, 23f),
                new(-12.3f, 4f, 37.05f), new(-2.5f, 4f, 37.05f),
                new(-2.65f, 4f, 34.35f), new(-14.7f, 8f, 34.35f), new(-14.7f, 8f, 36.8f),
                new(-2.4f, 8f, 36.8f)
            };
            foreach (var point in route)
            {
                yield return WalkWithInput(point);
                Require(scene.HasZone("LOC05"), $"LOC05 stays resident during the S1 ascent at {point}");
            }
            Require(State.chapter == "CH02" && !GateService.G02(State) && !GateService.G03(State) &&
                !State.Happened("CH03-T01") && !State.Has(EvidenceRegistry.PatientRegister) && !scene.HasZone("LOC09"),
                "Walking into LOC07 does not start CH03 or load B1 before G02");

            for (int i = route.Length - 2; i >= 0; i--)
            {
                yield return WalkWithInput(route[i]);
                Require(scene.HasZone("LOC05"), $"LOC05 stays resident during the S1 descent at {route[i]}");
            }
            for (int i = receptionApproach.Length - 1; i >= 0; i--) yield return WalkWithInput(receptionApproach[i]);
            yield return WalkWithInput(reception);
            Require(State.chapter == "CH02" && !GateService.G02(State), "S1 returns to the CH02 reception route");
            verification.Add("V33-Q05 INPUT S1 PASS: walked LOC02–LOC05–LOC07 and back in R before G02.");
        }

        private IEnumerator InputHospitalExpansion()
        {
            yield return UseFrom("EXIT", scene.Pad("AP05").position);
            yield return Choose("Mở lối lên khu B");
            yield return Until(() => !busy && State.chapter == "CH03", "CH03 handoff");
            Require(player.Feet.y < .3f, "CH03 did not warp to the second floor");
            yield return WalkWithInput(new Vector3(4, 4, 33.73f));
            yield return WalkWithInput(new Vector3(-2.7f, 4, 34.35f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 34.35f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 36.8f));
            yield return WalkWithInput(new Vector3(-2.4f, 8, 36.8f));
            var an03 = scene.Pad("AN03");
            yield return UseFrom("E03", new Vector3(-.68f, 8.015f, 31.9f));
            yield return Choose("Chép sổ");
            yield return UseFrom("C02_NOTE", an03.position);
            yield return Until(() => !busy && State.Has(EvidenceRegistry.C02CareNote), "care note");
            yield return UseFrom("C02_BAG", new Vector3(-1.55f, 8.015f, 31.12f));
            yield return Choose("Ghi lại thẻ");
            yield return UseFrom("AN03", an03.position);
            yield return Choose("Chỉ chép mã C02");
            yield return Until(() => !busy && !GateService.G03(State), "unpaired PZ03 input rejected");
            yield return Choose("Ghi nhận C12");
            yield return Until(() => !busy && !GateService.G03(State), "wrong PZ03 input rejected");
            Require(PuzzleService.ShouldShowFieldHint(State, "CH03-PU01"), "PZ03 2-strike hint active");
            yield return Choose("Ghi nhận C02");
            yield return Until(() => !busy && GateService.G03(State) && State.checkpoint == "CH03-CP02", "PZ03 solved at CH03-CP02");
            yield return Choose("Xem ký ức");
            yield return Until(() => State.layer == Reality.M, "M via AN03");
            yield return Choose("Trở về kho");
            yield return Until(() => !busy && State.layer == Reality.R && !ui.IsModal, "Back to R");
            Require(InventoryService.FindByDef(State, BackpackCatalog.MemVignetteC02)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.ValidState(State), "CH03 MemoryOnly vignette recorded strictly as non-exportable MemoryRef");
            yield return UseFrom("C12_BOX", new Vector3(-1.55f, 8.015f, 33.45f));
            yield return Choose("Ghi nhận sự tồn tại C12");
            yield return Until(() => !busy && State.Happened("FS19"), "C12 row saved");
            yield return UseFrom("C12_BOX", new Vector3(-1.55f, 8.015f, 33.45f));
            yield return Choose("Đọc ghi chú của Lan");
            yield return Until(() => voice.Transcript.Any(e => e.LineId == "CH03-D07"), "CH03-D07 note recorded");
            yield return Choose("Quay lại hộp C12");
            yield return Choose("Đóng");
            Require(!State.PatientIdentityVerified("C02") && !State.PatientIdentityVerified("C12"), "No name required");
            yield return WalkWithInput(new Vector3(-.9f, 8f, 35.85f));
            yield return Until(() => !busy && State.Happened("CH03-T06") && State.checkpoint == "CH03-CP03", "CH03-T06 patrol beat");
            Require(orderlyAi.IsActive && orderlyAi.ActiveRole == AIRole.Hunter, "CH03 Floor-3 Hunter activates on CH03-T06");
            yield return AimAndUseHere("RADIO_LOC07");
            yield return Until(() => orderlyAi.CurrentState == OrderlyState.Investigate, "CH03-Q03 radio distraction pulls Hunter into Investigate");
            yield return WalkWithInput(new Vector3(-.8f, 8, 36.8f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 36.8f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 34.35f));
            yield return WalkWithInput(new Vector3(-2.65f, 4, 34.35f));
            yield return WalkWithInput(new Vector3(4, 4, 35.0f));
            yield return WalkWithInput(new Vector3(4, 4, 45.7f));
            yield return UseFrom("CH03_EXIT", scene.Pad("AP07").position);
            yield return Choose("Vào phòng trực");
            yield return Until(() => !busy && State.chapter == "CH04", "CH04 handoff");
            yield return UseFrom("AN04", scene.Pad("AP07").position);
            yield return Choose("Lấy bản lệnh giao ca");
            yield return Choose("Lưu vào sổ trực");
            yield return Choose("Chép sổ ca đêm");
            yield return Choose("Lưu vào sổ trực");
            yield return Choose("Đọc bản chép băng");
            yield return Choose("Ghi nhận lời Lan");
            yield return Choose("Rời phòng trực");
            Require(InventoryService.HasPhysical(State, BackpackCatalog.Ch04Orders) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.Ch04Log) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.Ch04Tape) &&
                    WorldUseResolver.Resolve(State, BackpackCatalog.Ch04Tape, "AN04").Outcome == WorldUseOutcome.PlayTapeAtDutyDesk,
                "CH04 duty items & tape synced to Backpack and WorldUseResolver");

            // CH04-Q01 PZ04 Route A (live Windows Player timing walk past 3-stop Orderly + cart without editing records!)
            yield return UseFrom("PZ04_SCHEDULE", new Vector3(4.35f, 4.015f, 44.85f));
            yield return Until(() => !busy && State.Happened("PZ04.ScheduleRead") && ui.IsModal, "PZ04 schedule read");
            yield return Choose("Đặt lịch xuống");
            yield return Until(() => !ui.IsModal && orderlyAi.IsActive && orderlyAi.ActiveRole == AIRole.Orderly &&
                orderlyAi.CurrentOrderlyStop == OrderlyStop.WingBDoor &&
                orderlyAi.ActiveActor.transform.position.x < 1.2f, "Orderly clears corridor toward WingBDoor for Route A", 32f);
            int resetsBeforeRouteA = orderlyAi.EncounterResetCount;
            yield return WalkWithInput(new Vector3(4f, 4f, 36.2f));
            yield return Until(() => !busy && State.Happened("CH04-T02") && State.Happened("PZ04.RouteA"), "PZ04 Route A passage recorded");
            Require(!State.Happened("PZ04.ScheduleUpdated") && orderlyAi.EncounterResetCount == resetsBeforeRouteA,
                "CH04-Q01 Route A passed cleanly past Orderly without record edits or reset");

            // CH04-Q01 PZ04 Route B (live Windows Player falsification rejection + factual room-status slip cross-check + rerouted corridor!)
            yield return Until(() => orderlyAi.ActiveActor.transform.position.z < 37.8f &&
                (orderlyAi.ActiveActor.transform.position.x < 1.5f || orderlyAi.ActiveActor.transform.position.x > 5.0f),
                "Wait for clear return to LOC06", 32f);
            yield return WalkWithInput(new Vector3(4f, 4f, 45.2f));
            yield return UseFrom("PZ04_ROOM_SLIP", new Vector3(4.35f, 4.015f, 46.05f));
            yield return Until(() => !busy && State.Happened("PZ04.SlipRead") && ui.IsModal, "PZ04 room-status slip read");
            yield return Choose("Mở bảng đối chiếu tuyến xe");
            yield return Choose("Sửa sai hồ sơ");
            yield return Until(() => !busy && !State.Happened("PZ04.ScheduleUpdated"), "PZ04 falsification rejected");
            yield return Choose("Đối chiếu đúng phiếu");
            yield return Until(() => !busy && State.Happened("PZ04.ScheduleUpdated") && orderlyAi.OrderlyRouteAdjusted,
                "PZ04 Route B factual cross-check reroutes Orderly");
            yield return Choose("Rời bảng");
            yield return WalkWithInput(new Vector3(4, 4, 33.6f));
            yield return Until(() => !busy && State.Happened("PZ04.RouteB"), "PZ04 Route B corridor passage recorded");
            yield return WalkWithInput(new Vector3(4, 0, 20.97f));
            yield return WalkWithInput(new Vector3(7, 0, 21.17f));
            yield return WalkWithInput(new Vector3(7, -3.4f, 32.4f));
            yield return UseFrom("CH04_POWER", new Vector3(8.67f, -3.385f, 34.5f));
            yield return Choose("Kho · 2");
            yield return Choose("Kỹ thuật · 1");
            yield return Choose("Rời bảng điện");
            yield return WalkWithInput(new Vector3(7, -3.4f, 38.98f));
            yield return WalkWithInput(new Vector3(1.5f, -3.4f, 39f));
            yield return UseFrom("E04_E07", scene.Pad("AP09").position);
            yield return Choose("Đọc hồ sơ tiếp nhận");
            yield return Choose("Chép tài liệu");
            yield return Choose("Đọc thẻ và phiếu");
            yield return Choose("Chép tài liệu");
            yield return Choose("Đối chiếu phiếu L-13");
            yield return Choose("Ghi nhận đối chiếu");
            yield return Choose("Nghe Lan");
            yield return Choose("Hỏi Lan");
            yield return Choose("Quay về cầu thang");
            yield return Tap(Key.Escape);
            yield return Choose("Tải điểm lưu tự động");
            yield return Until(() => !busy && !ui.IsModal && transaction.OnPad(player.Feet, "AP09"), "B1 reload with actual menu");
            Require(GateService.G04(State) && State.Happened("CH04-T06") && !State.Has("E08"), "B1 autosave state");
            // Return up B1 -> ER -> Floor 2 LOC06 to read AN05 direction note, then walk to canonical AN05 wheelchair in Khu B!
            yield return WalkWithInput(new Vector3(7, -3.4f, 38.98f));
            yield return WalkWithInput(new Vector3(7, -3.4f, 32.4f));
            yield return WalkWithInput(new Vector3(7, 0f, 21.17f));
            yield return WalkWithInput(new Vector3(4, 0f, 20.97f));
            yield return WalkWithInput(new Vector3(4, 4f, 33.73f));
            yield return WalkWithInput(new Vector3(4, 4f, 45.7f));
            yield return AimAndUseHere("AN05_HANDOFF_NOTE");
            yield return Until(() => !busy && State.Happened("CH04-AN05.reached") && ui.IsModal, "AN05 direction note reached");
            yield return Choose("Ra sảnh Khu B");
            yield return Until(() => !ui.IsModal, "closed AN05 direction note");
            verification.Add("INPUT CH03–04 PASS: W capsule on three flights, PZ03, C12, R/M, CH03 radio distraction, PZ04 Route A + Route B on live Orderly, duty papers, circuit controls, H05, dialogue guard, B1 autosave menu, and AN05 handoff.");

            // Start real 12fps video frame capture for the full CH05 B01-B08 gameplay sequence when -ctcc-record is active!
            string recordRoot = Argument("-ctcc-record");
            if (!string.IsNullOrEmpty(recordRoot))
            {
                captureExpansion = true;
                StartCoroutine(CaptureExpansionFrames(Path.Combine(recordRoot, "ch05-full")));
            }

            // ==================== CH05 FULL PLAYER INPUT WALKTHROUGH (B01–B08, ZERO NORMAL-ROUTE TELEPORT!) ====================
            yield return WalkWithInput(new Vector3(4f, 4f, 37.0f));
            yield return WalkWithInput(new Vector3(4f, 4f, 33.0f));
            yield return WalkWithInput(new Vector3(0.85f, 4f, 33.0f));
            yield return AimAndUseHere("AN05");
            yield return Until(() => !busy && State.chapter == "CH05" && State.checkpoint == ChapterFive.CheckpointEntry && ui.IsModal,
                "CH05-CP01 committed at AN05 wheelchair");
            yield return Choose("Bước vào ký ức sơ tán");
            yield return Until(() => !busy && !ui.IsModal && State.layer == Reality.M && State.Happened(ChapterFive.EvEntry),
                "CH05 entered Layer M at AN05");

            // Pair 1 (2/6) -> live Escape/Load mid-evacuation test (CH05-Q02!)
            yield return WalkWithInput(new Vector3(0.85f, 4f, 36.9f));
            yield return WalkWithInput(new Vector3(-1.4f, 4f, 36.9f));
            yield return AimAndUseHere("CH05_M_PAIR1");
            yield return Until(() => ui.IsModal, "CH05 Pair 1 modal");
            yield return Choose("Mở chốt cửa Khu B");
            yield return Until(() => !busy && !savingDialogue && !ui.IsModal &&
                ChapterFive.RescuedCount(State) == 2 && State.Happened(ChapterFive.EvPair1),
                "CH05 Pair 1 completed (2/6)");
            yield return null;
            yield return null;
            yield return Tap(Key.Escape);
            yield return Choose("Tải điểm lưu tự động");
            yield return Until(() => !busy && !ui.IsModal && State.layer == Reality.M &&
                ChapterFive.RescuedCount(State) == 2 && transaction.OnPad(player.Feet, "AP10"),
                "CH05-Q02 live mid-evacuation autosave reload restores Layer M at AP10 with 2/6");

            // Pair 2 (4/6): Walk S1 Ward bridge + S1 West gallery -> segment reset -> call colleague -> complete Pair 2
            yield return WalkWithInput(new Vector3(0.85f, 4f, 36.9f));
            yield return WalkWithInput(new Vector3(-12.2f, 4f, 37.0f));
            yield return WalkWithInput(new Vector3(-12.2f, 4f, 22.8f));
            yield return AimAndUseHere("CH05_M_PAIR2");
            yield return Until(() => ui.IsModal, "CH05 Pair 2 modal");
            yield return Choose("2. Dẫn Nhóm 2");
            yield return Until(() => ui.IsModal && !State.Happened(ChapterFive.EvPair2), "CH05 Pair 2 obstacle segment reset");
            yield return Choose("1. Gọi đồng nghiệp");
            yield return Choose("2. Dẫn Nhóm 2");
            yield return Until(() => !busy && !ui.IsModal && ChapterFive.RescuedCount(State) == 4 && State.Happened(ChapterFive.EvPair2),
                "CH05 Pair 2 completed (4/6)");

            // Pair 3 (6/6): Walk down S1 stairs -> straight through OPEN S1 courtyard discharge door -> Assembly -> B05 -> Return R!
            yield return WalkWithInput(new Vector3(-12.2f, 4f, -4.0f));
            Vector3 s1Threshold = scene.s1DischargeDoor.pivot.parent.position;
            yield return WalkWithInput(new Vector3(s1Threshold.x, 0f, -4.0f));
            yield return WalkWithInput(new Vector3(s1Threshold.x, 0f, s1Threshold.z - 0.35f));
            yield return AimAndUseHere("CH05_M_PAIR3_ASSEMBLY");
            yield return Until(() => ui.IsModal, "CH05 Pair 3 assembly modal");
            yield return Choose("Đưa Nhóm 3 ra điểm tập kết");
            yield return Until(() => !busy && ui.IsModal && ChapterFive.RescuedCount(State) == 6 && State.Happened(ChapterFive.EvPair3),
                "CH05 Pair 3 completed (6/6) & Vĩnh B05 praise shown");
            yield return Choose("Trở về xe bệnh nhân AN05");
            yield return Until(() => !busy && !ui.IsModal && State.layer == Reality.R &&
                State.Happened(ChapterFive.EvReturnR) && transaction.OnPad(player.Feet, "AP10"),
                "CH05 T05 returned to R at AP10 (CH05-CP02)");
            Require(scene.s1DischargeSalvage != null && scene.s1DischargeSalvage.activeInHierarchy,
                "CH05 return to R restored S1 courtyard salvage barrier");

            // B06–B07: Walk Floor 2 -> S3 -> LOC04 -> S2 -> LOC09 -> Optional Valve -> PZ07 (2 strikes + align) -> LOC11 -> E05b -> H03/G05
            yield return WalkWithInput(new Vector3(4f, 4f, 33.0f));
            yield return WalkWithInput(new Vector3(4f, 0f, 20.97f));
            yield return WalkWithInput(new Vector3(7f, 0f, 21.17f));
            yield return Until(() => scene.RegionReady("LOC09", RegionStreamer.Service), "LOC09 preloaded before S2 descent");
            yield return WalkWithInput(new Vector3(7f, -3.4f, 32.4f));
            yield return WalkWithInput(new Vector3(6.0f, -3.4f, 35.6f));
            yield return AimAndUseHere("LOC09_VALVE");
            yield return Until(() => !busy && ui.IsModal && State.Happened(ChapterFive.FlagValveSabotage), "CH05 optional valve observed");
            yield return Choose("Ghi nhận nghi vấn");
            yield return Until(() => !ui.IsModal, "closed LOC09 valve modal");

            // Step into LOC10 morgue refuge (x < 4.45) and wait until Hunter walks north inside LOC11 (~5.4s window) to solve PZ07 safely!
            yield return WalkWithInput(new Vector3(6.0f, -3.4f, 39.0f));
            yield return WalkWithInput(new Vector3(3.6f, -3.4f, 39.0f));
            yield return Until(() => !orderlyAi.IsActive ||
                (orderlyAi.CurrentState == OrderlyState.Patrol &&
                 orderlyAi.ActiveActor.transform.position.x > 11.4f &&
                 orderlyAi.ActiveActor.transform.forward.z > 0.3f),
                "CH05-B1 Hunter patrolling north inside LOC11 before PZ07", 35f);
            yield return WalkWithInput(new Vector3(6.0f, -3.4f, 39.0f));
            yield return WalkWithInput(new Vector3(6.0f, -3.4f, 43.3f));
            yield return WalkWithInput(new Vector3(7.4f, -3.4f, 43.3f));
            yield return AimAndUseHere("CH05_BLUEPRINTS");
            yield return Until(() => !busy && ui.IsModal && State.Has(EvidenceRegistry.AsBuilt2004), "CH05 E05 blueprints acquired");
            yield return Choose("Căn chồng hai bản vẽ");
            yield return Choose("Kiểm tra chồng bản vẽ");
            yield return Until(() => !busy && ui.IsModal && PuzzleService.FailedAttempts(State, ChapterFive.PuzzleOverlay) >= 1,
                "CH05 PZ07 strike 1 recorded");
            yield return Choose("Kiểm tra chồng bản vẽ");
            yield return Until(() => !busy && ui.IsModal && PuzzleService.ShouldShowFieldHint(State, ChapterFive.PuzzleOverlay),
                "CH05 PZ07 2-strike hint active");
            yield return Choose("Điều chỉnh Mốc 1");
            yield return Choose("Điều chỉnh Mốc 2");
            yield return Choose("Kiểm tra chồng bản vẽ");
            yield return Until(() => !busy && State.Happened(ChapterFive.FlagOverlayAligned), "CH05 PZ07 overlay aligned");
            Require(!GateService.H03(State) && !GateService.G05(State),
                "CH05-Q03 Input QA: aligned PZ07 overlay alone keeps H03 and G05 closed");
            yield return Choose("Đóng");
            yield return Until(() => !ui.IsModal, "closed PZ07 overlay modal");

            // Duck back into LOC10 refuge and wait for Hunter to sweep south past z=37.8 (facing south), then hug west wall x=5.3 (>3.6m clear of 3.0m walk hearing!) into LOC11 -> E05b gallery!
            yield return WalkWithInput(new Vector3(6.0f, -3.4f, 43.3f));
            yield return WalkWithInput(new Vector3(5.3f, -3.4f, 39.0f));
            yield return WalkWithInput(new Vector3(3.6f, -3.4f, 39.0f));
            yield return Until(() => !orderlyAi.IsActive ||
                (orderlyAi.CurrentState == OrderlyState.Patrol &&
                 orderlyAi.ActiveActor.transform.position.x < 9.6f &&
                 orderlyAi.ActiveActor.transform.position.z < 39.2f &&
                 orderlyAi.ActiveActor.transform.forward.z < -0.5f),
                "CH05-B1 Hunter sweeps south past z=39.2 leaving west wall & LOC11 silent and clear", 35f);
            yield return WalkWithInput(new Vector3(5.3f, -3.4f, 39.0f));
            yield return WalkWithInput(new Vector3(5.3f, -3.4f, 42.2f));
            yield return WalkWithInput(new Vector3(12.5f, -3.4f, 42.2f));
            yield return WalkWithInput(new Vector3(12.5f, -3.4f, 50.4f));
            yield return WalkWithInput(new Vector3(35.8f, -3.4f, 50.4f));
            yield return WalkWithInput(new Vector3(35.8f, -3.4f, 42.0f));
            yield return AimAndUseHere("E05B_SEALED_DOOR");
            yield return Until(() => !busy && ui.IsModal && State.Has(EvidenceRegistry.SealedWallPhoto), "CH05 physical E05b observed");
            yield return Choose("★ Xác minh H03");
            yield return Until(() => !busy && !ui.IsModal && GateService.H03(State) && GateService.G05(State),
                "CH05 H03 verified (E05 + E05b) and G05 opened");

            // B08: Return B1 -> S2 -> LOC04 -> S3 -> LOC05 -> S1 Archive stairs -> LOC07 (Hunter + Radio) -> LOC08 (AP11) -> CH05-CP03!
            yield return WalkWithInput(new Vector3(35.8f, -3.4f, 50.4f));
            yield return WalkWithInput(new Vector3(12.5f, -3.4f, 50.4f));
            yield return WalkWithInput(new Vector3(12.5f, -3.4f, 42.0f));
            yield return WalkWithInput(new Vector3(7f, -3.4f, 42.0f));
            yield return WalkWithInput(new Vector3(7f, -3.4f, 32.4f));
            yield return WalkWithInput(new Vector3(7f, 0f, 21.17f));
            yield return WalkWithInput(new Vector3(4f, 0f, 20.97f));
            yield return WalkWithInput(new Vector3(4f, 4f, 34.35f));
            yield return WalkWithInput(new Vector3(-2.65f, 4f, 34.35f));
            yield return WalkWithInput(new Vector3(-14.7f, 8f, 34.35f));
            yield return WalkWithInput(new Vector3(-14.7f, 8f, 36.8f));
            yield return WalkWithInput(new Vector3(-0.9f, 8f, 36.8f));
            yield return WalkWithInput(new Vector3(-0.9f, 8f, 35.85f));
            Require(orderlyAi.IsActive && orderlyAi.ActiveRole == AIRole.Hunter, "CH05-L3 Hunter active on Floor 3 post-G05");
            yield return AimAndUseHere("RADIO_LOC07");
            yield return Until(() => orderlyAi.CurrentState == OrderlyState.Investigate, "CH05-L3 radio distraction pulls Hunter");
            // Step south to AN03 behind the freestanding shelf (1.1, 34), let Hunter cross north toward the radio, then slip south to LOC08 cleanly!
            yield return WalkWithInput(new Vector3(-0.6f, 8f, 32.2f));
            yield return Until(() => !orderlyAi.IsActive ||
                (orderlyAi.ActiveActor.transform.position.z > 33.6f && orderlyAi.ActiveActor.transform.position.x < 0.4f),
                "CH05-L3 Hunter pulled to west radio table leaving south-east passage clear", 35f);
            yield return WalkWithInput(new Vector3(-0.6f, 8f, 31.1f));
            yield return WalkWithInput(new Vector3(2.4f, 8f, 31.1f));
            yield return WalkWithInput(new Vector3(2.4f, 8f, 32.3f));
            yield return WalkWithInput(new Vector3(5.2f, 8f, 32.3f));
            // Phase 3 Cross-Chapter H08 Recovery Live Check at LOC08 R Desk!
            yield return WalkWithInput(new Vector3(5.68f, 8f, 31.78f));
            yield return AimAndUseHere(WorldAnchors.SurfaceLoc08Recovery);
            yield return Until(() => !busy && ui.IsModal, "Phase 3 LOC08_RECOVERY_DESK opened");
            yield return Choose("Đặt phong bì lên mặt bàn");
            yield return Until(() => !busy && InventoryService.GetPlacedAt(State, WorldAnchors.Loc08DeskEnvelope)?.itemDefId == BackpackCatalog.E10aEnvelope,
                "Phase 3 E10a envelope physically placed on LOC08 recovery blotter");
            yield return Choose("Thu phong bì về ba lô");
            yield return Until(() => !busy && InventoryService.GetPlacedAt(State, WorldAnchors.Loc08DeskEnvelope) == null,
                "Phase 3 E10a envelope retrieved cleanly from LOC08 recovery blotter");
            yield return Choose("Đóng");
            yield return Until(() => !ui.IsModal, "Phase 3 LOC08 recovery desk closed");
            yield return WalkWithInput(new Vector3(5.2f, 8f, 32.3f));
            yield return AimAndUseHere("CH05_HANDOFF");
            yield return Until(() => !busy && ui.IsModal && State.Happened(ChapterFive.EvHandoff) &&
                State.checkpoint == ChapterFive.CheckpointHandoff, "CH05-CP03 handoff committed at LOC08");
            yield return new WaitForSecondsRealtime(0.45f);
            yield return Choose("Ở lại kiểm tra tầng 3");
            captureExpansion = false;
            Require(GateService.G05(State) && !State.Has("E01") && !State.Has("E06") && !State.Has("E10b") &&
                !State.Has("E08") && !State.Happened("G06.opened"),
                "CH05 full input walkthrough stops cleanly at CH05->CH06 boundary");
            verification.Add("INPUT CH05 PASS: zero-teleport W-walk B01-B08, AN05 3-pair 6/6 evacuation via open S1 courtyard discharge, live Escape mid-evac M reload, R S1 salvage restoration, DEC-V32-03 valve, PZ07 2-strike hint + dual E05/E05b H03/G05 guard, Floor 3 Hunter radio distraction, and unlocked LOC08 CH05-CP03 handoff.");
        }

        private IEnumerator WalkWithInput(Vector3 destination)
        {
            const float arrivalRadius = .20f;
            const float stuckTime = 2f;
            var start = player.Feet;
            float initialDistance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(destination.x, destination.z));
            // The controller clamps its movement delta on slow frames. Budget the same time for travel,
            // while retaining a separate real-time ceiling if input or frame updates stop progressing.
            float movementLimit = Mathf.Max(4f, initialDistance / (FirstPerson.WalkSpeed * .75f) + 2f);
            float wallLimit = Mathf.Clamp(movementLimit * 2.5f, 10f, 90f);
            double startedAt = Time.realtimeSinceStartupAsDouble;
            float movementTime = 0f, lastProgressAt = 0f, progressDistance = initialDistance;
            var capsule = player.GetComponent<CharacterController>();
            Key forward = State.settings.alternateBindings ? Key.UpArrow : Key.W;
            Vector3 direction = destination - start;
            direction.y = 0f;
            float remaining = initialDistance;
            bool arrived = remaining < arrivalRadius;
            string exitReason = "arrived";
            try
            {
                if (!arrived) InputSystem.QueueStateEvent(qaKeys, new KeyboardState(forward));
                while (!arrived)
                {
                    if (Time.realtimeSinceStartupAsDouble - startedAt >= wallLimit) { exitReason = "wall-time limit"; break; }
                    if (movementTime >= movementLimit) { exitReason = "movement-time limit"; break; }
                    if (movementTime - lastProgressAt >= stuckTime) { exitReason = "no forward progress"; break; }

                    player.transform.rotation = Quaternion.LookRotation(direction);
                    qaKeys.MakeCurrent();
                    yield return null;
                    movementTime += FirstPerson.MovementDeltaTime;
                    direction = destination - player.Feet;
                    direction.y = 0f;
                    remaining = direction.magnitude;
                    arrived = remaining < arrivalRadius;
                    if (progressDistance - remaining >= .12f)
                    {
                        progressDistance = remaining;
                        lastProgressAt = movementTime;
                    }
                }
            }
            finally { InputSystem.QueueStateEvent(qaKeys, new KeyboardState()); }

            Vector3 feet = player.Feet;
            Vector3 velocity = capsule.velocity;
            bool grounded = capsule.isGrounded;
            var collision = capsule.collisionFlags;
            bool pressed = qaKeys[forward].isPressed;
            float lastDelta = Time.deltaTime, lastMovementDelta = FirstPerson.MovementDeltaTime;
            double elapsed = Time.realtimeSinceStartupAsDouble - startedAt;
            yield return Until(() => !qaKeys[forward].isPressed, "walk release");
            if (arrived && Mathf.Abs(player.Feet.y - destination.y) < .32f) yield break;

            Vector3 final = player.Feet;
            string blocker = "none";
            if (remaining >= arrivalRadius &&
                Physics.CapsuleCast(feet + Vector3.up * .46f, feet + Vector3.up * 1.40f,
                    HospitalScene.Radius, direction.normalized, out var hit, .50f,
                    HospitalScene.SolidMask, QueryTriggerInteraction.Ignore))
                blocker = hit.collider.name;
            string floor = Physics.Raycast(feet + Vector3.up * .30f, Vector3.down, out var ground, .65f,
                HospitalScene.SolidMask, QueryTriggerInteraction.Ignore) ? ground.collider.name : "none";
            string zone = scene.ResolveZoneAt(feet)?.zoneId ?? "none";
            if (arrived) exitReason = "height outside tolerance";
            Require(false, $"Input route start={start}, destination={destination}, end={feet}, afterRelease={final}; " +
                $"reason={exitReason}, initial={initialDistance:F2}m, remaining={remaining:F2}m, wall={elapsed:F2}/{wallLimit:F2}s, " +
                $"movement={movementTime:F2}/{movementLimit:F2}s, delta={lastDelta:F3}s, movementDelta={lastMovementDelta:F3}s, timeScale={Time.timeScale:F2}, " +
                $"velocity={velocity}, grounded={grounded}, collision={collision}, blocker={blocker}, floor={floor}, " +
                $"zone={zone}, streaming={streaming}, LOC05={scene.HasZone("LOC05")}, LOC05Ready={scene.RegionReady("LOC05", RegionStreamer.Upper)}, " +
                $"controls={player.CanPlay}, modal={ui.IsModal}, lookSuppressed={player.LookSuppressed}, externalCamera={player.ExternalCameraControl}, " +
                $"key={forward}, pressed={pressed}, focused={Application.isFocused}");
        }

        private IEnumerator LookForWardConsequence(FormStatus branch)
        {
            yield return Until(() => !busy && !ui.IsModal, "the signed sheet returns before inspecting the ward");
            player.SetViewRotation(Quaternion.LookRotation(Vector3.left));
            yield return new WaitForSecondsRealtime(.28f);
            Require(!State.f101History[0].consequenceObserved, "filing alone does not mark the room as observed");
            yield return WalkWithInput(new Vector3(9.82f, 0f, 4.93f));
            Vector3 cue = branch switch
            {
                FormStatus.Affirmed => scene.deniedBay.transform.Find("Denied bay front").GetComponent<Collider>()
                    .ClosestPoint(player.Eye.transform.position),
                FormStatus.Unverified => scene.interactables.curtainCloth.GetComponent<Renderer>().bounds.center,
                _ => (scene.refutedBay.transform.Find("CH01_PR01_CornerTrace") ?? scene.refutedBay.transform.Find("RefutedBed4DampWall")).position
            };
            player.SetViewRotation(Quaternion.LookRotation(cue - player.Eye.transform.position));
            yield return Until(() => State.f101History[0].consequenceObserved && !busy, $"{branch} follows the player's renewed sightline");
            yield return null;
            CaptureInvestigation("room-101-after-" + branch);
            yield return new WaitForEndOfFrame();
            Require(saves.Read(SaveSlot.Auto, out var reviewed) == SaveRead.Valid && reviewed.f101History[0].consequenceObserved,
                "the room consequence stays observed in the save");
            verification.Add($"WARD AFTERMATH PASS {branch}: looking away defers the observation, returning to the ward records it durably.");
            yield return WalkWithInput(new Vector3(7.24f, 0f, 4.93f));
        }

        private IEnumerator CaptureExpansionFrames(string directory)
        {
            Directory.CreateDirectory(directory);
            var frameEnd = new WaitForEndOfFrame();
            float nextFrame = Time.unscaledTime;
            int index = 0;
            while (captureExpansion)
            {
                yield return frameEnd;
                if (Time.unscaledTime < nextFrame || !captureExpansion) continue;
                nextFrame += 1f / 12f;
                if (nextFrame < Time.unscaledTime - .24f) nextFrame = Time.unscaledTime;
                var frame = ScreenCapture.CaptureScreenshotAsTexture();
                if (frame == null) continue;
                try { File.WriteAllBytes(Path.Combine(directory, $"frame-{index++:00000}.jpg"), frame.EncodeToJPG(78)); }
                finally { Destroy(frame); }
            }
        }

        private IEnumerator InputPhysicalCase(bool sideDoor)
        {
            yield return UseFrom("LIST", new Vector3(6.69f, 0.02f, 3.35f));
            yield return Until(() => !busy && inspector.IsInspecting, "room list checkpoint");
            Require(inspector.IsInspecting, "R list opens physical paper");
            yield return Tap(Key.Escape);
            yield return UseFrom("SOCKETS", new Vector3(11.53f, 0.02f, 3.14f));
            yield return Until(() => !busy && State.Observed("SOCKETS"), "sockets checkpoint");
            yield return UseFrom("WHEELS", new Vector3(11.63f, 0.02f, 4.88f));
            yield return Until(() => !busy && State.Observed("WHEELS"), "wheel marks checkpoint");
            yield return UseFrom("PZ01", new Vector3(12.60f, 0.02f, 3.63f));
            yield return Choose("Có một chỗ kê");
            yield return Until(() => !busy && State.Has("E12"), "E12 checkpoint");
            Require(State.Has("E12") && (scene.cabinet.localPosition - scene.cabinetOpen).sqrMagnitude > 0.001f,
                "PZ01 opens cabinet with animation");
            yield return Until(() => (scene.cabinet.localPosition - scene.cabinetOpen).sqrMagnitude < 0.0004f,
                "cabinet settles at open pose");
            foreach (string id in new[] { "DRAW1", "DRAW2", "DRAW3", "WRIST", "TRANSFER" })
            {
                yield return UseFrom(id, new Vector3(15.0f, 0.02f, 3.64f));
                Require(inspector.IsInspecting, id + " inspect");
                yield return Choose("Thu thập");
                yield return Until(() => !busy && !ui.IsModal, id + " durable pickup completed");
            }
            Require(InventoryService.HasPhysical(State, BackpackCatalog.Ch02Draw1) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.Ch02Draw2) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.Ch02Draw3) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.Ch02Wristband) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.Ch02Transfer),
                "CH02 evidence auto-synced into physical Backpack catalog");
            if (!sideDoor)
            {
                // Affirmed branch tests physical Room 101 2-slot table: place wristband + assemble 3 drawing fragments on table!
                player.Teleport(new Vector3(7.08f, 0.02f, 3.18f), Quaternion.Euler(0f, 180f, 0f));
                // Phase 2.5 diegetic contract: with all 3 fragments carried, using the Room 101 table assembles the drawing directly (0 modal).
                yield return AimAndUseHere("COMPARE_DESK_101");
                yield return Until(() => !busy && !ui.IsModal && State.Has(EvidenceRegistry.Drawing),
                    "CH02 3 drawing fragments assembled directly on Room 101 table with zero modal");
                // Second use opens the 2-slot table: physically place the C01 wristband.
                yield return AimAndUseHere("COMPARE_DESK_101");
                yield return Choose("Đặt Vòng tay C01");
                yield return Until(() => !busy &&
                    (InventoryService.GetPlacedAt(State, WorldAnchors.Ward101TableLeft)?.itemDefId == BackpackCatalog.Ch02Wristband ||
                     InventoryService.GetPlacedAt(State, WorldAnchors.Ward101TableRight)?.itemDefId == BackpackCatalog.Ch02Wristband),
                    "CH02 wristband physically placed on Room 101 comparison table");
                player.SetTorchEnabled(true);
                player.SetViewRotation(Quaternion.LookRotation(new Vector3(7.08f, 0.80f, 2.26f) - player.Eye.transform.position));
                yield return null;
                CaptureInvestigation("proposal-ch02-table-drawing");
                yield return null;
                player.SetTorchEnabled(false);
            }
            player.Teleport(scene.Pad("AP04"));
            yield return AimAndUseHere("F101");
            yield return Choose("Ghép tranh và kiểm hồ sơ");
            if (!State.Has(EvidenceRegistry.Drawing))
            {
                yield return Choose("Ghép tranh theo đường mép");
                yield return Until(() => !busy && State.Has(EvidenceRegistry.Drawing), "assembled drawing was saved");
            }
            yield return Choose("Đối chiếu An");
            yield return Until(() => !busy && !savingDialogue && GateService.G02(State), "C01 checkpoint");
            Require(GateService.G02(State), "G02 opens through physical evidence and UI");
            float x = sideDoor ? 0.05f : 4f;
            player.Teleport(new Vector3(x, 0.02f, 10.4f), Quaternion.identity);
            yield return Until(() => gatekeeperCameo.IsActive, "Gatekeeper begins from the corridor trigger");
            yield return Hold(Key.W, 3.8f);
            yield return Until(() => !busy && !savingDialogue && State.Happened("CH02-T06") && !gatekeeperCameo.IsActive,
                sideDoor ? "walked through Door B" : "walked through Door A");
            yield return UseFrom("EXIT", scene.Pad("AP05").position);
            yield return Until(() => !busy && State.Happened("M1-complete") && ui.IsModal, "exit reached and recorded");
            yield return Choose("Ở lại khám phá");
            verification.Add($"INPUT PASS E11/E12/G02 & CH02-Q05 Door {(sideDoor ? "B" : "A")}: physical clues, cabinet, assembly, identification, walking escape and ER ledger.");
        }

        private IEnumerator OpenFormInput()
        {
            yield return Until(() => State.Happened("CH01-T06"), "room checkpoint");
            yield return AimAndUseHere("F101");
            if (State.chapter == "CH01")
                yield return Until(() => !busy && IsAn02Clipboard3DActive && ui.IsModal, "3D AN02 clipboard ready");
            else
                yield return Choose("Đặt giấy lên bảng kẹp");
        }

        private IEnumerator AimAndUseHere(string id)
        {
            var target = FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).Single(t => t.id == id);
            yield return Until(() => !busy && !savingDialogue, "ready to aim at " + id);
            Vector3 look = target.Contact - player.Eye.transform.position;
            Vector3 planar = new(look.x, 0f, look.z);
            if (planar.sqrMagnitude > 0.0001f)
                player.transform.rotation = Quaternion.LookRotation(planar);
            player.Eye.transform.LookAt(target.Contact);
            var focus = player.Focus(Available);
            if (focus != target)
            {
                Physics.Raycast(player.Eye.transform.position, look.normalized, out var occlusion, look.magnitude - .04f,
                    HospitalScene.SightMask, QueryTriggerInteraction.Collide);
                Require(false, $"No sightline to {id} from {player.Feet}; target={target.transform.position}, " +
                    $"available={Available(target)}, focus={focus?.id}, obstruction={occlusion.collider?.name}");
            }
            player.CanPlay = false;
            yield return Tap(State.settings.alternateBindings ? Key.K : Key.E);
            RefreshControls();
        }

        private IEnumerator UseFrom(string id, Vector3 origin)
        {
            var target = FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).Single(t => t.id == id);
            bool found = false;
            foreach (var offset in new[] { Vector3.zero, Vector3.right * 0.35f, Vector3.left * 0.35f, Vector3.forward * 0.35f, Vector3.back * 0.35f,
                Vector3.right * 0.7f, Vector3.left * 0.7f, Vector3.forward * 0.7f, Vector3.back * 0.7f })
            {
                var point = origin + offset;
                if (HospitalScene.Occupied(point)) continue;
                Vector3 look = target.transform.position - (point + Vector3.up * 1.62f);
                look.y = 0;
                if (look.sqrMagnitude < 0.001f) continue;
                player.Teleport(point, Quaternion.LookRotation(look));
                player.Eye.transform.LookAt(target.transform.position);
                if (player.Focus(Available) != target) continue;
                yield return Until(() => !busy, "ready for " + id + " interaction");
                // Keep the fixture aim while the real game Update reads focus and the Use action.
                player.CanPlay = false;
                yield return Tap(State.settings.alternateBindings ? Key.K : Key.E);
                RefreshControls();
                found = true;
                break;
            }
            Require(found, $"No interactable sightline to {id} from {origin} at {target.transform.position}");
        }

        private RectTransform ComparisonSlot(int side) =>
            ui.GetComponentsInChildren<RectTransform>().Where(rect => rect.name == "Document clip").ElementAt(side);

        private void CaptureInvestigation(string name) =>
            ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), name + ".png"));

        private void ClickUi(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            DispatchPaperClick(button.gameObject, pointer);
        }

        private void ClickPaperField(RectTransform clip, Vector2 uv)
        {
            Canvas.ForceUpdateCanvases();
            var viewport = clip.GetComponentInChildren<PaperPointer>();
            var paper = clip.GetComponentInChildren<RawImage>().rectTransform;
            var local = new Vector2(Mathf.Lerp(paper.rect.xMin, paper.rect.xMax, uv.x), Mathf.Lerp(paper.rect.yMin, paper.rect.yMax, uv.y));
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right,
                position = RectTransformUtility.WorldToScreenPoint(null, paper.TransformPoint(local))
            };
            DispatchPaperClick(viewport.gameObject, pointer);
        }

        private void DispatchPaperClick(GameObject expected, PointerEventData pointer)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count != 0 && hits[0].gameObject == expected, "raycast hits " + expected.name);
            ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private void DragSignature()
        {
            var target = (RectTransform)ButtonMatching("Kéo bút qua dòng ký").transform;
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(new Vector2(target.rect.xMin + 30, 0)));
            Vector2 end = start + new Vector2(Screen.width * .18f, 6);
            SendPaperDrag(target.gameObject, start, end);
        }

        private static void SendPaperDrag(GameObject source, Vector2 start, Vector2 end)
        {
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = start,
                pressPosition = start
            };
            ExecuteEvents.Execute(source, pointer, ExecuteEvents.beginDragHandler);
            pointer.position = end;
            ExecuteEvents.Execute(source, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(source, pointer, ExecuteEvents.endDragHandler);
        }

        private IEnumerator LinkFormInputs()
        {
            yield return Choose("Nối hai nguồn");
            yield return Choose("AS-05B · Thẻ giường 4");
            yield return Choose("Dấu sử dụng giường 4");
            yield return Until(() => !busy && State.f101Annotations.HasLink, "selected endpoints form a durable link");
        }

        private IEnumerator VerifyNotebookInput()
        {
            yield return Choose("Tài liệu");
            yield return Choose("Thẻ bàn giao giường số 4");
            Require(ActivePageContains("Phần ghi nơi chuyển bị nhòe") &&
                !ActivePageContains("Nhận định thận trọng"), "notebook preserves the inspected reverse side as an observation");
            yield return Choose("Kẹp sang trái");
            var source = ButtonMatching("Phiếu chuyển giường 02");
            var right = ComparisonSlot(1);
            Canvas.ForceUpdateCanvases();
            SendPaperDrag(source.gameObject, RectTransformUtility.WorldToScreenPoint(null, source.transform.position),
                RectTransformUtility.WorldToScreenPoint(null, right.TransformPoint(right.rect.center)));
            var leftSlot = ComparisonSlot(0);
            int rawCount = new[] { leftSlot, right }.SelectMany(slot => slot.GetComponentsInChildren<RawImage>()).Count(image => image.texture != null);
            Require(comparison[0]?.Id == ObservationRegistry.Bed4Card && comparison[1]?.Id == ObservationRegistry.Bed2Transfer &&
                rawCount == 2,
                $"two inspected sheets placed by keyboard and drag [c0={comparison[0]?.Id ?? "null"} c1={comparison[1]?.Id ?? "null"} raw={rawCount}]");
            Require(Screen.width == 1920 && Screen.height == 1080, $"comparison expected at 1080p, got {Screen.width}x{Screen.height}");
            CaptureInvestigation("notebook-comparison-100");
            yield return null;

            var left = ComparisonSlot(0);
            var leftPaper = comparison[0];
            var rightPaper = comparison[1];
            yield return Tap(Key.Digit1);
            yield return Hold(Key.Q, .6f);
            Require(leftPaper.Zoom > 1.5f && rightPaper.Zoom == 1, "zoom is independent");
            var viewport = left.GetComponentInChildren<PaperPointer>().gameObject;
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, viewport.transform.position);
            SendPaperDrag(viewport, center, center + new Vector2(114, 87));
            Require(leftPaper.Pan.sqrMagnitude > .005f && rightPaper.Pan == Vector2.zero, "drag pans only the selected scan");
            CaptureInvestigation("notebook-zoom-100");
            yield return null;
            yield return Choose("Lật giấy", left);
            Require(leftPaper.Back && !rightPaper.Back, "only the left sheet flips");
            float zoom = leftPaper.Zoom;
            Vector2 pan = leftPaper.Pan;
            yield return Choose("Soi 3D", left);
            yield return Until(() => inspector.IsInspecting, "opens registered inspector from the comparison");
            yield return Tap(Key.Escape);
            yield return Until(() => !inspector.IsInspecting && ui.IsModal && !ui.IsStandaloneOverlay,
                "standalone 3D inspector returns cleanly to 5-tab Journal comparison");
            Require(comparison[0] == leftPaper && comparison[1] == rightPaper && leftPaper.Back &&
                leftPaper.Zoom == zoom && (leftPaper.Pan - pan).sqrMagnitude < .00001f,
                "inspector returns to the same papers, side, zoom and pan");
            yield return Choose("Đổi hai tờ");
            Require(comparison[0] == rightPaper && comparison[1] == leftPaper && leftPaper.Back, "swap moves the entire paper pose");
            yield return Choose("Đổi hai tờ");
            yield return Choose("Đặt lại", left);
            yield return Choose("Lật giấy", left);

            var larger = new VisitSettings
            {
                textScale = 1.3f, alternateBindings = Options.alternateBindings, captions = Options.captions
            };
            ui.Apply(larger);
            Journal();
            yield return Choose("Chữ lớn", left);
            yield return Choose("Chữ lớn", right);
            Require(ui.GetComponentsInChildren<Text>().Count(label => label.name == "Large print on paper" && label.fontSize >= 29) == 2 &&
                ActivePageContains("CHƯA BÀN GIAO"), "both papers are simultaneously readable at 130 percent");
            CaptureInvestigation("notebook-comparison-130");
            yield return null;
            yield return Choose("Mặt giấy", left);
            yield return Choose("Mặt giấy", right);
            ui.Apply(Options);
            Journal();

            yield return Choose("Ghim vùng đang soi", right);
            var markedWriting = ui.GetComponentInChildren<InputField>();
            yield return Until(() => markedWriting != null && markedWriting.isFocused, "mark opens an attached note");
            markedWriting.text = "Đối chiếu vị trí ghi dấu của bản gốc.";
            yield return Tap(Key.Tab);
            yield return Choose("Lưu dòng này");
            yield return Until(() => !busy && State.journalNotes.Count == 1 && comparingDocuments &&
                journalTab == JournalTab.Documents, "saved mark returns to the two-paper comparison");
            var pin = State.journalNotes[0];
            Require(pin.paperMark.placed && !pin.paperMark.back && pin.source == ObservationRegistry.Bed2Transfer &&
                right.GetComponentsInChildren<RectTransform>().Any(rect => rect.name == "Noted region"),
                "note is pinned to the selected region and face of a previously read paper");

            yield return Choose("Rút tờ", right);
            Require(comparison[1] == null && comparison[0] == leftPaper, "removing the right sheet keeps the left sheet");
            yield return Choose("Phiếu chuyển giường 02");
            yield return Choose("Suy luận");
            yield return Choose("Viết giả thuyết");
            var writing = ui.GetComponentInChildren<InputField>();
            yield return Until(() => writing != null && writing.isFocused, "notebook takes keyboard focus for writing");
            writing.text = "Cần trở lại xem chỗ đặt tấm thẻ.";
            yield return Tap(Key.Tab);
            yield return Choose("Chọn nguồn");
            yield return Choose("Thẻ bàn giao giường số 4");
            yield return Choose("Nối nguồn thứ hai");
            yield return Choose("Dấu sử dụng tại vị trí giường 4");
            yield return Choose("Lưu dòng này");
            yield return Until(() => !busy && State.journalNotes.Count == 2, "authored note with two sources saved");
            var note = State.journalNotes[1];
            Require(note.kind == JournalNoteKind.Hypothesis && note.source == ObservationRegistry.Bed4Card &&
                note.related == ObservationRegistry.Bed4Traces && State.f101.revision == 1 && State.f101History.Count == 1 &&
                State.f101.current == FormStatus.Refuted,
                "a hypothesis link in CH02 Journal does not mutate committed F101 revision or history");
            yield return Choose("Cần trở lại");
            yield return Tap(Key.Tab);
            yield return Choose("Mở nguồn thứ hai");
            Require(ActivePageContains("chăn") || ActivePageContains("dép") || ActivePageContains("giường"),
                "the selected note opens its DEC-V361-CH01-01 field source");
            yield return Choose("Về ghi chú");
            yield return Choose("Đổi sang chưa xác thực");
            CaptureInvestigation("notebook-reasoning-100");
            yield return null;
            yield return Choose("Tháo liên kết");
            yield return Choose("Lưu dòng này");
            yield return Until(() => !busy && string.IsNullOrEmpty(State.journalNotes[1].related) &&
                State.journalNotes[1].kind == JournalNoteKind.Question, "unlink and status changes persist");
            yield return Tap(Key.Escape);
            verification.Add("REBIRTH II JOURNAL PASS: 1080p 100%/130%, keyboard, pointer drag, independent zoom/pan/flip, inspector return, swap/remove, positioned annotation, saved hypothesis, source reopen, status and unlink.");
        }

        private Button ButtonMatching(string label, Transform within = null)
        {
            var root = within == null ? ui.transform : within;
            var matches = root.GetComponentsInChildren<Button>().Where(b =>
                b.GetComponentsInChildren<Text>().Any(t => t.text.Contains(label))).ToArray();
            if (matches.Length != 1)
            {
                string visible = string.Join(" | ", root.GetComponentsInChildren<Button>()
                    .Select(b => b.GetComponentInChildren<Text>()?.text.Replace("\n", " ") ?? "?"));
                throw new InvalidOperationException(
                    $"ButtonMatching('{label}') found {matches.Length}; modal={ui.IsModal} busy={busy}; visible buttons: {visible}");
            }
            return matches[0];
        }

        private bool ActivePageContains(string copy) =>
            ui.GetComponentsInChildren<Text>().Any(label => label.text.Contains(copy, StringComparison.Ordinal));

        private int StaffTranscriptCount(string id) => voice.Transcript.Count(entry => entry.LineId == id);

        private IEnumerator Choose(string label, Transform within = null)
        {
            yield return Until(() => ui.IsModal && !busy && !savingDialogue, "modal for " + label);
            var button = ButtonMatching(label, within);
            Require(button.interactable, "enabled action " + label);
            for (int i = 0; i < 64 && EventSystem.current.currentSelectedGameObject != button.gameObject; i++)
                yield return Tap(Key.Tab);
            Require(EventSystem.current.currentSelectedGameObject == button.gameObject, "keyboard focused " + label);
            yield return Tap(Key.Enter);
        }

        private IEnumerator Tap(Key key)
        {
            yield return Until(() => !busy && !savingDialogue, "ready for tap " + key);
            yield return Hold(key, 0f);
        }

        private IEnumerator Hold(Key key, float duration)
        {
            qaKeys.MakeCurrent();
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState(key));
            yield return Until(() =>
            {
                qaKeys.MakeCurrent();
                return qaKeys[key].isPressed;
            }, "queued keyboard input " + key);
            // Let the normal Update handlers observe the pressed state across simulated time before releasing it.
            float elapsed = 0f;
            do
            {
                qaKeys.MakeCurrent();
                yield return null;
                elapsed += Mathf.Min(Time.deltaTime, 0.045f);
            } while (elapsed < duration);
            qaKeys.MakeCurrent();
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState());
            yield return Until(() =>
            {
                qaKeys.MakeCurrent();
                return !qaKeys[key].isPressed;
            }, "keyboard release " + key);
        }

        private static IEnumerator Until(Func<bool> ready, string step, float timeoutSeconds = 6)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (!ready() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Require(ready(), step);
        }

        private IEnumerator InspectLifetimeStress()
        {
            var document = Image("ObsCard");
            var backDoc = Image("ObsCard_Back");
            var indentNorm = Image("ObsCard_Back_Indent_Normal");
            var indentMask = Image("ObsCard_Back_Indent_Mask");
            Require(document != null && backDoc != null && indentNorm != null && indentMask != null,
                "dual-sided + raking indentation stress documents available");
            long before = Profiler.GetTotalAllocatedMemoryLong();
            for (int i = 0; i < 100; i++)
            {
                inspector.Open(null, document, (float)document.width / document.height, ActiveVolume(), backDoc,
                    backIndentNormal: indentNorm, backIndentMask: indentMask);
                inspector.Flip();
                if ((i & 1) == 0) inspector.TiltForRakingLight();
                yield return null;
                inspector.Close();
                yield return null;
            }
            int meshes = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name is "InspectPaperFront" or "InspectPaperBack");
            int materials = Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name is "InspectPaperFrontMat" or "InspectPaperBackMat");
            long after = Profiler.GetTotalAllocatedMemoryLong();
            Require(meshes == 0 && materials == 0, $"Inspector retains {meshes} meshes and {materials} materials");
            verification.Add($"INPUT STRESS PASS: 100 dual-sided + raking-light open/close cycles; {meshes} live paper meshes, {materials} live paper materials, Unity allocated bytes {before}→{after}.");
        }

        private IEnumerator NotebookLifetimeStress()
        {
            var lastTab = journalTab;
            for (int i = 0; i < 5; i++) DrawJournal((JournalTab)i);
            Form();
            CloseModal();
            yield return null;
            int objects = ui.GetComponentsInChildren<RectTransform>(true).Length;
            for (int cycle = 0; cycle < 100; cycle++)
            {
                DrawJournal((JournalTab)(cycle % 5));
                CloseModal();
                yield return null;
                Form();
                CloseModal();
                yield return null;
            }
            int after = ui.GetComponentsInChildren<RectTransform>(true).Length;
            Require(objects == after && !ui.IsModal && !inspector.IsInspecting,
                $"notebook and clipboard retain a bounded hierarchy ({objects} → {after} RectTransforms)");
            journalTab = lastTab;
            verification.Add($"REBIRTH II RETENTION PASS: 100 notebook and 100 clipboard open/close cycles; {objects} → {after} RectTransforms.");
        }
    }
}
