using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public sealed partial class HospitalGame : MonoBehaviour
    {
        [Serializable] private sealed class DialogueVariant
        {
            public string variantId, branch, beat, presentationMode, displaySpeaker, runtimeCopy, uiAudioRule;
            public bool needsVO, showSpeakerPrefix, isItalicSilentThought;
        }
        [Serializable] private sealed class DialogueEntry
        {
            public string id, chapter, speaker, displaySpeaker, context, presentationMode;
            public bool needsVO, showSpeakerPrefix, isItalicSilentThought;
            public string runtimeCopy, legacyCopy, pocketNoteText, stageDirection, timecodeCard, uiAudioRule;
            public string copy;
            public DialogueVariant[] variants;
        }
        [Serializable] private sealed class AmbientBarkEntry
        {
            public string id, channel, speaker, trigger, copy;
        }
        [Serializable] private sealed class SilentThoughtUiSpec
        {
            public bool playVO, showSpeakerPrefix, textShadow, respectsFontScaling, routeToTranscriptHistory;
            public string fontStyle, colorHex, scrimColorHex, accessibilitySettingToggle, position, durationScaleSettingKey;
            public float scrimAlpha, highContrastBoxAlpha, minContrastRatioWCAG, fadeInSec, holdSecMin, holdSecMax, fadeOutSec;
            public float autoScaleWhenLargePrint, holdSecMaxWithExtendedScale;
            public float[] durationScaleOptions;
        }
        [Serializable] private sealed class UiPresentationSpecDto
        {
            public SilentThoughtUiSpec INNER_SUB_SILENT;
        }
        [Serializable] private sealed class Ch01WorldFirstDto
        {
            public string scope, source, smsAppointmentPropCopy, smsPostponementPropCopy, mailboxNotePropCopy;
            public string gateInContract, mailboxPickupContract, e10aRecoveryContract, exitBoundaryPrototype, obs101AContract;
            public bool pocketNoteReadOnly, mandatoryJournalGameplay, legacyDialogueIdsPreserved, propCopyNotDialogue;
            public bool requiresRuntimeIntegration, task02HardGate, schemaCompatibilityUnverified;
        }
        [Serializable] private sealed class DialogueSheet
        {
            public string schemaVersion;
            public string designRevision;
            public string decisionRef;
            public UiPresentationSpecDto uiPresentationSpec;
            public AmbientBarkEntry[] ambientBarks;
            public Ch01WorldFirstDto ch01WorldFirst;
            public string sourceRevision;
            public string sourceFile;
            public string sourceSha256;
            public DialogueEntry[] entries;
            public DialogueCondition[] conditions;
        }
        private string dialogueSchemaVersion = "";
        private string dialogueDesignRevision = "";
        private string dialogueDecisionRef = "";
        private string dialogueSourceRevision = "";
        private string dialogueSourceSha256 = "";
        private UiPresentationSpecDto dialogueUiSpec;
        private AmbientBarkEntry[] ambientBarks = Array.Empty<AmbientBarkEntry>();
        private Ch01WorldFirstDto ch01WorldFirst;
        private int canonicalDialogueCount;
        private readonly Dictionary<string, DialogueEntry> dialogue = new();
        private readonly Dictionary<string, DialogueCondition> revealConditions = new();
        private readonly Queue<string> staffReactions = new();
        private string staffReactionCaption;
        private HospitalScene scene;
        private FirstPerson player;
        private HospitalUI ui;
        private PhysicalInspector inspector;
        private VoiceDirector voice;
        private LanDirector lanDirector;
        private GatekeeperCameo gatekeeperCameo;
        private HunterOrderlyAI orderlyAi;
        private AmbientDreadDirector dreadDirector;
        private RegionStreamer streamer;
        private bool streaming;
        private float reconcileRegionsAt;
        private float retryEmergencySaveAt;
        private float retryAutomaticCheckpointAt;
        private float dialogueSaveAt;
        private AnApparitionDirector anDirector;
        private RealityTransaction transaction;
        private Casework casework;
        private AtomicSaves saves;
        private bool live, busy, savingDialogue, firstEscape, bootReady;
        private string qaFolder;

        public enum InputContext
        {
            Exploration = 0,
            WorldDrag = 1,
            PhysicalInspect = 2,
            DiegeticPhone = 3,
            DiegeticEnvelope = 4,
            DiegeticAn02 = 5,
            Inventory = 6,
            PocketNote = 7,
            Modal = 8,
            Cinematic = 9,
            Pause = 10
        }

        // Scripted Horror Events (once-per-visit runtime guards) & Progressive Hints (CH01-T11)
        private bool heardLobbySilhouette;
        private bool playedHe04CorridorCut;
        private bool playedCh01Ex01Monologue;
        private bool pendingCh01D01AfterLetter;
        private bool draggingSeamGate;
        private float b01SignKnockAt;
        private float ch01StartUnscaledTime;
        private int f101SignatureRevision;
        private bool F101SignaturePlaying => f101SignatureRevision != 0;
        private string lastObjectiveKey = "";
        private float objectiveStagnantSeconds;
        private int hintTierShown;
        private FirstPerson.FuncTarget availableTarget;

        // Phase 2.5 Universal Left-Click Interaction & Zero-Bleed Input Guards
        private bool cursorWasLocked;
        private bool waitForLmbRelease;
        private SceneTarget lmbPendingDragTarget;
        private float lmbHoldStartTime;
        private float lmbHoldDeltaPx;
        private const float LmbDragHoldSeconds = 0.16f;
        private const float LmbDragMovePixels = 10f;

        internal InputContext ActiveInputContext
        {
            get
            {
                if (!live || busy) return InputContext.Pause;
                if (lanDirector != null && lanDirector.IsCinematicActive) return InputContext.Cinematic;
                if (IsDiegeticPhoneActive) return InputContext.DiegeticPhone;
                if (IsDiegeticEnvelopeActive) return InputContext.DiegeticEnvelope;
                if (IsAn02Clipboard3DActive) return InputContext.DiegeticAn02;
                if (inspector != null && inspector.IsInspecting) return InputContext.PhysicalInspect;
                if (ui != null && ui.IsInventory2Open) return InputContext.Inventory;
                if (IsPocketNoteOpen) return InputContext.PocketNote;
                if (ui != null && ui.IsModal) return InputContext.Modal;
                if (scene?.interactables != null && scene.interactables.IsDragging) return InputContext.WorldDrag;
                return InputContext.Exploration;
            }
        }

        private Investigation State => casework.State;
        private VisitSettings Options => State.settings;
        private string UseKey => Options.alternateBindings ? "K" : "E";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            var world = FindFirstObjectByType<HospitalScene>();
            if (world != null && world.GetComponent<HospitalGame>() == null)
                world.gameObject.AddComponent<HospitalGame>();
        }

        private void Awake()
        {
            availableTarget = Available;
            string requestedQuality = Argument("-ctcc-quality");
            if (requestedQuality != null)
            {
                int preset = Array.IndexOf(QualitySettings.names, requestedQuality);
                if (preset >= 0) QualitySettings.SetQualityLevel(preset, true);
            }
            else if (Argument("-ctcc-qa") == null && Argument("-ctcc-input-qa") == null)
            {
                int preset = Array.IndexOf(QualitySettings.names, PlayerPrefs.GetString("CTCC.GraphicsQuality"));
                if (preset >= 0) QualitySettings.SetQualityLevel(preset, true);
            }

            scene = GetComponent<HospitalScene>();
            streamer = new RegionStreamer(scene);
            string requestedGi = Argument("-ctcc-gi");
            if (!string.IsNullOrEmpty(requestedGi))
                scene.giMode = requestedGi.ToLowerInvariant();

            var lineSheet = Resources.Load<TextAsset>("CTCC/Dialogue");
            var sheet = JsonUtility.FromJson<DialogueSheet>(lineSheet.text);
            dialogueSchemaVersion = sheet.schemaVersion ?? "";
            dialogueDesignRevision = sheet.designRevision ?? "";
            dialogueDecisionRef = sheet.decisionRef ?? "";
            dialogueUiSpec = sheet.uiPresentationSpec;
            ambientBarks = sheet.ambientBarks ?? Array.Empty<AmbientBarkEntry>();
            ch01WorldFirst = sheet.ch01WorldFirst;
            dialogueSourceRevision = sheet.sourceRevision ?? "";
            dialogueSourceSha256 = sheet.sourceSha256 ?? "";
            canonicalDialogueCount = sheet.entries != null ? sheet.entries.Length : 0;
            foreach (var entry in sheet.entries)
            {
                if (string.IsNullOrEmpty(entry.copy))
                    entry.copy = !string.IsNullOrWhiteSpace(entry.runtimeCopy) ? entry.runtimeCopy
                        : (!string.IsNullOrWhiteSpace(entry.pocketNoteText) ? entry.pocketNoteText : (entry.legacyCopy ?? ""));
                dialogue.Add(entry.id, entry);
            }
            // DEC-VO-01 groups D10/D11/D12 and D13/D14 into one source row with explicit variants.
            // Expand the variants into stable IDs so existing beat/transcript references keep working.
            foreach (var entry in sheet.entries)
                ExpandDialogueVariants(entry);
            AddDialogueAlias("CH01-D11", "CH01-D10", 2);
            AddDialogueAlias("CH01-D12", "CH01-D10", 3);
            AddDialogueAlias("CH01-D14", "CH01-D13", 2);
            foreach (var condition in sheet.conditions)
                revealConditions.Add(condition.id, condition);
            ValidateDialogueRuntimeContract(throwOnError: false);

            qaFolder = Argument("-ctcc-qa") ?? Argument("-ctcc-loc02-shots");
            if (Argument("-ctcc-benchmark") != null) benchmarkAwakeQpc = PerformanceCounter();
        }

        private void Start() => StartCoroutine(BootAndRun());

        private IEnumerator BootAndRun()
        {
            // Load all additive zone scenes (LOC01..LOC04) and wait until every ZoneRegion is live
            yield return scene.EnsureAllZonesLoadedAsync();
            Debug.Log($"CTCC_BOOT_ZONES loaded={scene.LoadedZoneCount} additive={scene.IsAdditiveMode} gi={scene.giMode}");

            var rig = new GameObject("FPS character", typeof(CharacterController), typeof(FirstPerson));
            player = rig.GetComponent<FirstPerson>();
            player.Initialize(Camera.main, scene.atmosphere);
            ApplyGraphicsTierCameraSettings();
            player.Teleport(scene.Pad("AP00"));

            inspector = gameObject.AddComponent<PhysicalInspector>();
            inspector.Initialize(player.Eye, scene.atmosphere, scene.rakingPaperShader, scene.inspectPaperTemplate);
            inspector.InspectOpened += id =>
            {
                dreadDirector?.NotifyInspectorOpened(id);
                if (State != null) handPresenter?.SyncState(State, scene, hideViewModel: true);
            };
            inspector.InspectClosed += id =>
            {
                dreadDirector?.NotifyInspectorClosed(id);
                if (State != null) handPresenter?.SyncState(State, scene, hideViewModel: ui.IsModal || (lanDirector != null && lanDirector.IsCinematicActive));
            };

            handPresenter = gameObject.AddComponent<HandItemPresenter>();
            handPresenter.Initialize(player.Eye, Image);
            handPresenter.SetBlenderKeyTemplates(scene.InspectTemplate("GATE_KEY"), scene.InspectTemplate("WINGC_KEY"));

            backpackStage = gameObject.AddComponent<BackpackStage3D>();
            backpackStage.Initialize(player.Eye, Image, scene.InspectTemplate);

            envelope3D = gameObject.AddComponent<DiegeticEnvelope3D>();
            envelope3D.Initialize(player.Eye, scene.atmosphere, Image);

            phone3D = gameObject.AddComponent<DiegeticPhone3D>();
            phone3D.Initialize(player.Eye, scene.atmosphere);

            an02Surface = gameObject.AddComponent<DiegeticAn02Surface>();
            an02Surface.Initialize(player.Eye, scene.atmosphere);

            ui = gameObject.AddComponent<HospitalUI>();
            ui.Initialize();
            ui.OpenChanged += RefreshControls;

            voice = gameObject.AddComponent<VoiceDirector>();
            voice.Initialize(scene.atmosphere?.voiceClips, scene.atmosphere?.lanMotif, scene.atmosphere, ui, MayDeliver);

            lanDirector = gameObject.AddComponent<LanDirector>();
            lanDirector.Initialize(
                scene,
                voice,
                player,
                ui,
                () => State,
                () => !live || busy || ui.IsModal || (inspector != null && inspector.IsInspecting),
                () => SaveCheckpoint(() => casework.RecordDreadEventAsync(DreadCatalog.B02GlassStudied)),
                () => SaveCheckpoint(() => casework.RecordLanNoticedAsync()),
                LanEncounterLine);

            gatekeeperCameo = gameObject.AddComponent<GatekeeperCameo>();
            gatekeeperCameo.Initialize(scene, voice, player, ui);

            anDirector = gameObject.AddComponent<AnApparitionDirector>();
            anDirector.Initialize(scene, voice, player);

            orderlyAi = gameObject.AddComponent<HunterOrderlyAI>();
            orderlyAi.Initialize(scene, voice, player, ui, inspector);

            dreadDirector = gameObject.AddComponent<AmbientDreadDirector>();

            var saveDirectory = qaFolder == null ? Argument("-ctcc-save") ?? Path.Combine(Application.persistentDataPath, "Patient101")
                : Path.Combine(Path.GetFullPath(qaFolder), "isolated-save");
            saves = new AtomicSaves(saveDirectory);
            transaction = new RealityTransaction(scene, saves);
            transaction.ReadCharge = () => player.BatterySeconds;
            voice.Recorded += entry =>
            {
                transaction.RecordDialogue(entry);
                dialogueSaveAt = Time.unscaledTime + 2f;
            };
            casework = new Casework(transaction);
            transaction.Committed += ApplyCommitted;

            int dreadSeed = State.dreadSeed;
            string seedArg = Argument("-ctcc-dread-seed");
            if (!string.IsNullOrEmpty(seedArg) && int.TryParse(seedArg, out int parsedSeed))
                dreadSeed = parsedSeed;
            dreadDirector.Initialize(
                scene,
                scene.atmosphere,
                player,
                () => State,
                () => !live || busy || ui.IsModal || (inspector != null && inspector.IsInspecting) ||
                      (voice != null && voice.IsSpeaking) ||
                      (lanDirector != null && (lanDirector.IsEncounterRunning || lanDirector.IsCinematicActive)),
                id => SaveCheckpoint(() => casework.RecordDreadEventAsync(id)),
                cap => ui.Caption(cap, 4.4f),
                dreadSeed);
            scene.ZoneUnregistering += zone =>
            {
                if (zone.lanActor != null)
                {
                    voice.StopFor(zone.lanActor);
                    lanDirector.StopEncounter(false);
                }
                if (zone.gatekeeperActor != null) gatekeeperCameo.StopAndHide();
                if (zone.anActor != null) anDirector.HideImmediately();
                orderlyAi?.OnZoneUnregistering(zone);
                if (zone.zoneId == "LOC03") handPresenter?.ResetWorldBindings();
            };
            scene.ZoneReloaded += () => ApplyCommitted(State);

            // Pre-warm BOTH R and B regional NavMesh caches behind startup so the very first R->B swap never hitches!
            scene.smoothPresentation = false;
            scene.WarmupNavigationAndShaders();
            transaction.Present(State);
            scene.smoothPresentation = qaFolder == null;
            ApplyCommitted(State);
            bootReady = true;

            string benchmark = Argument("-ctcc-benchmark");
            if (benchmark != null)
            {
                benchmarkReadyQpc = PerformanceCounter();
                StartCoroutine(BenchmarkVisit(Path.GetFullPath(benchmark)));
                yield break;
            }
            string loc02Shots = Argument("-ctcc-loc02-shots");
            if (loc02Shots != null)
            {
                StartCoroutine(Loc02ShotRun(Path.GetFullPath(loc02Shots)));
                yield break;
            }
            if (qaFolder != null)
            {
                StartCoroutine(AutomatedVisit());
                yield break;
            }
            MainMenu();
            string inputQa = Argument("-ctcc-input-qa");
            if (inputQa != null) StartCoroutine(FinishVerification(VerifyPlayerInput(), Path.GetFullPath(inputQa)));
        }

        private static string Argument(string label)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, label);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private void ApplyCommitted(Investigation current)
        {
            scene.PresentCasework(current);
            player.Apply(current.settings);
            player.ReportLayer = current.layer == Reality.B;
            ui.Apply(current.settings);
            scene.atmosphere?.Apply(current.settings);
            voice?.Apply(current.settings);
            inspector?.SetReducedMotion(current.settings.reduceMotion);
            inspector?.SetCameraAssist(!current.settings.disableCameraAssist);
            scene.interactables?.SetReducedMotion(current.settings.reduceMotion);
            if (scene.PedalBin != null) scene.PedalBin.ReducedMotion = current.settings.reduceMotion;
            scene.ApplyAtmosphereSettings(current.settings.reduceMotion);
            if (scene.motion != null)
            {
                scene.motion.reducedMotion = current.settings.reduceMotion;
                if (current.layer != Reality.B)
                {
                    scene.motion.ClearFanSlowdown();
                    scene.motion.ClearTubeBlackout();
                }
            }
            if (scene.realGrade != null && scene.realGrade.sharedProfile.TryGet<ColorAdjustments>(out var real))
                real.postExposure.value = 0.42f + current.settings.visibility * 0.58f;
            if (scene.reportGrade != null && scene.reportGrade.sharedProfile.TryGet<ColorAdjustments>(out var report))
                report.postExposure.value = -0.02f + current.settings.visibility * 0.45f;

            lanDirector?.SyncState(current);
            if (current.chapter is ("CH01" or "CH02") || gatekeeperCameo.IsActive)
                gatekeeperCameo.SyncState(current);
            if (current.chapter == "CH01" || current.layer == Reality.B)
                anDirector?.HideImmediately();
            orderlyAi?.SyncState(current);

            bool hideHeld = ui.IsModal || (inspector != null && inspector.IsInspecting) ||
                            IsDiegeticPhoneActive || IsDiegeticEnvelopeActive || IsAn02Clipboard3DActive ||
                            (lanDirector != null && lanDirector.IsCinematicActive);
            handPresenter?.SetReducedMotion(current.settings.reduceMotion);
            handPresenter?.SyncState(current, scene, hideViewModel: hideHeld);
            backpackStage?.SetReducedMotion(current.settings.reduceMotion);
            envelope3D?.SetReducedMotion(current.settings.reduceMotion);
            phone3D?.SetReducedMotion(current.settings.reduceMotion);
            var equippedItem = InventoryService.GetEquipped(current);
            ui.SetHeldItemBadge(equippedItem != null ? BackpackCatalog.Get(equippedItem.itemDefId)?.ShortLabel : null);
            RefreshQuickSlotHud(current);

            bool b08ReturnWalk = current.chapter == "CH01" && current.Happened("CH01-T09") && !current.Happened("CH01-T10");
            scene.atmosphere?.SetReturnWalkRainScale(b08ReturnWalk ? 0.62f : 1f);

            RefreshObjective();
        }

        private void OnDestroy()
        {
            InspectionSharpnessGuard.ResetAll();
        }

        private void RefreshControls()
        {
            bool cinematic = lanDirector != null && lanDirector.IsCinematicActive;
            bool play = live && !ui.IsModal && !busy && !cinematic && qaFolder == null;
            bool inspectOrDevice = (inspector != null && inspector.IsInspecting) ||
                                   IsDiegeticPhoneActive || IsDiegeticEnvelopeActive ||
                                   IsAn02Clipboard3DActive || (backpackStage != null && backpackStage.IsOpen);
            if (inspector != null) inspector.InputBlocked = busy || ui.ModalInputBlocked;
            if (phone3D != null) phone3D.InputBlocked = busy || ui.ModalInputBlocked;
            if (envelope3D != null) envelope3D.InputBlocked = busy || ui.ModalInputBlocked;
            if (an02Surface != null) an02Surface.InputBlocked = busy || ui.ModalInputBlocked;
            if (!play)
            {
                lmbPendingDragTarget = null;
                waitForLmbRelease = true;
                if (scene?.interactables != null && scene.interactables.IsDragging)
                    CancelWorldDrag();
                player.ClearFocus();
                ui.UpdateWorldMarker(player.Eye, null, null, WorldMarkerKind.None, null);
            }
            player.LookSuppressed = false;
            player.TorchSuppressed = inspectOrDevice;
            player.CanPlay = play;
            if (casework != null && State != null)
                handPresenter?.SyncState(State, scene, hideViewModel: !play || inspectOrDevice);
            if (qaFolder != null) return;
            Cursor.lockState = play ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !play;
            if (!play) cursorWasLocked = false;
        }

        private void TickPhysicalInspectHotspots()
        {
            if (inspector == null || !inspector.IsInspecting || !ui.IsStandaloneOverlay) return;
            string obsId = inspector.CurrentObservationId;
            inspector.ProjectHotspots(projectedHotspotsBuf, id =>
                State != null && !string.IsNullOrEmpty(obsId) &&
                State.f101Annotations.MarkedHotspot(obsId) == id);
            ui.Update3DHotspotMarkers(projectedHotspotsBuf);
        }

        private void Update()
        {
            if (bootReady)
            {
                TickAn02Clipboard3D();
                TickDiegeticPhone3D();
                TickDiegeticEnvelope3D();
                TickPhysicalInspectHotspots();
            }
            if (bootReady && (staffReactionCaption != null || staffReactions.Count > 0))
            {
                if (staffReactionCaption != null && !ui.IsShowingCaption(staffReactionCaption))
                    staffReactionCaption = null;
                bool inContext = StaffReactionContext();
                if (!inContext) staffReactions.Clear();
                if (!inContext || ui.IsModal) CancelStaffReaction();
            }
            if (!bootReady || !live || busy || ui.IsModal || qaFolder != null ||
                (lanDirector != null && lanDirector.IsCinematicActive))
                return;
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                lmbPendingDragTarget = null;
                if (scene.interactables?.IsDragging == true)
                {
                    draggingSeamGate = false;
                    CancelWorldDrag();
                    return;
                }
                if (scene.PedalBin?.HeldOpen == true)
                {
                    SetPedalBin(false);
                    return;
                }
                if (firstEscape) firstEscape = false;
                else Pause();
                return;
            }
            firstEscape = false;
            if (keys[Options.alternateBindings ? Key.J : Key.Tab].wasPressedThisFrame)
            {
                OpenBackpack();
                return;
            }
            if (keys.nKey.wasPressedThisFrame)
            {
                OpenPocketNote();
                return;
            }
            if (keys.pKey.wasPressedThisFrame)
            {
                OpenPhone();
                return;
            }
            if (keys.hKey.wasPressedThisFrame && InventoryService.GetEquipped(State) != null)
            {
                SaveCheckpoint(() => casework.EquipItemAsync(""), () =>
                {
                    scene.atmosphere?.From("bagZip", player.Eye.transform.position + player.Eye.transform.forward * 0.3f, 0.36f);
                });
                return;
            }
            if (keys.digit1Key.wasPressedThisFrame) { ActivateQuickSlot(0); return; }
            if (keys.digit2Key.wasPressedThisFrame) { ActivateQuickSlot(1); return; }
            if (keys.digit3Key.wasPressedThisFrame) { ActivateQuickSlot(2); return; }
            if (keys.digit4Key.wasPressedThisFrame) { ActivateQuickSlot(3); return; }
            if (b01SignKnockAt > 0f && Time.unscaledTime >= b01SignKnockAt)
            {
                b01SignKnockAt = 0f;
                dreadDirector?.TryTriggerB01SignKnock();
            }

            Vector3 feet = player.Feet;
            if (TryDepartureThreshold(feet)) return;
            if (scene.PedalBin?.HeldOpen == true && (scene.PedalBin.transform.position - feet).sqrMagnitude > 7f)
                SetPedalBin(false);
            if (!streaming && Time.unscaledTime >= reconcileRegionsAt)
                StartCoroutine(UpdateRegions());
            if (State.chapter == "CH02" && GateService.G02(State) && transaction.OnPad(feet, "AP06"))
            {
                StartCoroutine(EnterCh03ArchiveWingAsync());
                return;
            }
            if (State.chapter == "CH03" && !State.Happened("CH03-T01") && transaction.OnPad(feet, "AP06"))
            {
                SaveCheckpoint(() => casework.RecordAsync("CH03-T01", feet));
                return;
            }
            if (State.chapter == "CH03" && GateService.G03(State) && !State.Happened("CH03-T06") &&
                Mathf.Abs(feet.y - 8f) < .25f && feet.z > 34.3f)
            {
                SaveCheckpoint(() => casework.RecordAsync("CH03-T06", feet, "CH03-CP03"));
                return;
            }
            if (State.chapter == "CH04" &&
                Mathf.Abs(feet.y - 4f) < .25f && feet.z > 33f && feet.z < 37f && feet.x > -3.4f && feet.x < 6f)
            {
                string expectedRoute = State.Happened("PZ04.ScheduleUpdated") ? "PZ04.RouteB" : "PZ04.RouteA";
                if (!State.Happened("CH04-T02") || !State.Happened(expectedRoute))
                {
                    SaveCheckpoint(() => ChapterFour.CompletePz04Passage(transaction), () =>
                    {
                        bool viaB = State.Happened("PZ04.ScheduleUpdated");
                        ui.Toast("ĐÃ VƯỢT TUYẾN XE THUỐC",
                            viaB ? "Đã đi qua hành lang trống sau khi cập nhật đúng phiếu buồng."
                                 : "Đã vượt qua nhịp 3 điểm của xe thuốc mà không cần sửa hồ sơ.", 4.5f);
                    });
                    return;
                }
            }
            bool use = keys[Options.alternateBindings ? Key.K : Key.E].wasPressedThisFrame;
            bool anchorPressed = keys.qKey.wasPressedThisFrame;

            // Core 04 [P78] / 4.4: Track whether Minh has visually observed the lit An Sinh reception lobby (for CH01-D01 two-way guard).
            TickCh01LitLobbySight();

            // CH01 B02: Spotting Lan's warm backlit silhouette behind reception glass pulls Minh voluntarily inside!
            if (!heardLobbySilhouette && State.chapter == "CH01" && State.Happened("CH01-T02") && !State.Happened("CH01-T03") &&
                feet.z > -10.5f && feet.z < -6.2f)
            {
                heardLobbySilhouette = true;
                // DEC-VO-01 CUT_TO_JOURNAL: the courtyard stays silent; rain and the glass silhouette carry the beat.
                PresentLine("CH01-D02");
            }

            // Upgrade #5 (CH01-EX01): Corroborating 03:17 wall clock + AS-05B 03g17 timestamp unlocks Minh's chilling pre-collapse monologue!
            if (!playedCh01Ex01Monologue && State.chapter == "CH01" && State.layer == Reality.B && GateService.G01(State) &&
                !State.Happened("CH01-T08") && CorroborationService.IsCorroborated(State) &&
                !F101SignaturePlaying && voice?.IsSpeaking != true &&
                feet.x < 5.5f && feet.z < 6.0f && feet.z > -0.5f)
            {
                playedCh01Ex01Monologue = true;
                var clockAnchor = new Vector3(-0.99f, 2.74f, -3.26f);
                scene.atmosphere?.From("clockTick", clockAnchor, 0.70f);
                scene.atmosphere?.From("clockEegHum", clockAnchor, 0.50f);
                PresentLine("CH01-D19", 1, "B05");
            }

            // HE04: After handling F101 in B, walking back down corridor slows ceiling fans & cuts PA04 mid-sentence!
            if (!playedHe04CorridorCut && State.chapter == "CH01" && State.layer == Reality.B && GateService.G01(State) &&
                !State.Happened("CH01-T08") && !F101SignaturePlaying && voice?.IsSpeaking != true &&
                feet.x < 5.2f && feet.z < 3.6f && feet.z > -0.8f)
            {
                playedHe04CorridorCut = true;
                if (State.f101.current == FormStatus.Unverified)
                    scene.motion?.SlowFans(3.5f);
                scene.atmosphere?.From("paStatic", scene.PaSpeakerAnchor != null ? scene.PaSpeakerAnchor.transform.position : new Vector3(5.5f, 2.6f, 3.35f));
                voice?.Speak("CH01-PA04", "Loa hành lang", Line("CH01-PA04"), scene.PaSpeakerAnchor,
                    directionalPrefix: "[Loa hành lang · rè và ngắt đột ngột]");
            }

            // CH01 Room 101 threshold checkpoint
            if (State.chapter == "CH01" && State.Happened("CH01-T05") && !State.Happened("CH01-T06") &&
                feet.x > 5.82f && feet.z > 2.01f && feet.z < 7.99f &&
                !use && !anchorPressed && Time.unscaledTime >= retryAutomaticCheckpointAt)
            {
                retryAutomaticCheckpointAt = Time.unscaledTime + 2f;
                SaveCheckpoint(() => casework.EnterRoomAsync(feet));
                return;
            }

            // CH02 B02: Slipper cue + gentle Bé An glimpse near Room 101 doorway
            if (State.chapter == "CH02" && State.layer == Reality.R && !State.Happened("CH02-T02") &&
               feet.z > 1.25f && feet.z < 7.9f && feet.x > 2.55f && feet.x < 6f &&
               !use && !anchorPressed && Time.unscaledTime >= retryAutomaticCheckpointAt)
            {
                retryAutomaticCheckpointAt = Time.unscaledTime + 2f;
                SaveCheckpoint(() => casework.RecordAsync("CH02-T02", feet), () =>
                {
                    var slipperPosition = new Vector3(10.3f, 0.1f, 4.74f);
                    scene.atmosphere?.From("slippers", slipperPosition);
                    string direction = scene.atmosphere?.DirectionTo(slipperPosition) ?? "phía phòng 101";
                    anDirector?.PlaySlipperGlimpse(Options.reduceMotion);
                    PresentLine("CH02-D02", directionalPrefix: $"[Tiếng dép tổ ong · {direction} · phòng 101]");
                });
                return;
            }

            // CH02 B06: Gatekeeper scripted cameo (strictly after G02, once-only, non-lethal, escapable via Door A or Door B!)
            if (gatekeeperCameo != null)
            {
                if (gatekeeperCameo.ShouldTrigger(State, feet))
                {
                    gatekeeperCameo.BeginCameo(State, Line);
                }
                else if (!use && gatekeeperCameo.CheckEscapedToEmergency(feet) && Time.unscaledTime >= retryEmergencySaveAt)
                {
                    StartCoroutine(SaveEmergency(false));
                    return;
                }
            }

            // CH01-T11: Progressive gentle inactivity hints (60s / 120s / 180s, toggleable in Settings)
            UpdateProgressiveHints();

            // Regional Patrol/Investigate/Chase/Search/Return AI & Orderly/Unrecorded sync (TASK011-A..E, TASK013-C)
            if (State.chapter is ("CH03" or "CH04" or "CH05"))
            {
                orderlyAi?.SyncState(State);
                if (orderlyAi != null && (orderlyAi.IsActive || orderlyAi.UnrecordedActive))
                    orderlyAi.Tick(State, FirstPerson.MovementDeltaTime, player.IsRunning, player.IsCrouching, player.IsMoving, Line);
            }

            if (transaction.OnPad(feet, "AN03") && State.chapter == "CH03" && anchorPressed)
            {
                Ch03ArchiveAnchor();
                return;
            }
            var target = player.Focus(availableTarget);
            var mouse = Mouse.current;
            if (target?.id == "MAILBOX" && anchorPressed && State.Happened(E10aParts.MailboxOpened) &&
                !State.Happened(E10aParts.MailboxPickedUp))
            {
                InspectMailboxExternalNote(target.transform);
                return;
            }
            if (target?.id == "CURTAIN-101" && anchorPressed && scene.interactables?.IsDragging != true)
            {
                float before = scene.interactables.IntendedOpenness(target.id);
                scene.interactables.SetCurtainAmount(before < .28f || before > .78f ? .5f : 1f, scene.atmosphere);
                SaveWorldPose(target.id, before);
                return;
            }
            if (target?.id == "F101" && State.chapter == "CH02" && anchorPressed)
            {
                SwitchAtClipboard();
                return;
            }
            if (target?.id == "DRAWER-NURSE" && anchorPressed && scene.interactables?.IsDragging != true &&
                scene.interactables?.IntendedOpenness("DRAWER-NURSE") <= 0.5f)
            {
                ToggleWorldProp("DRAWER-NURSE", quietNurseDrawer: true);
                return;
            }
            if (target != null && target.id is ("OBS101A" or "OBS101A_BLANKET" or "OBS101A_SLIPPERS") &&
                anchorPressed && CanSpatialLinkBed4())
            {
                TriggerSpatialBed4Link();
                return;
            }
            if (anchorPressed && State.quickSlots != null && State.quickSlots.Count > 0)
            {
                CycleQuickSlot();
                return;
            }

            // Phase 2.5 Universal Left-Click Interaction System (Requirement A):
            // - Zero-bleed guard across cursor lock transitions (ignores held/stale LMB from closing a modal/device)
            // - Short LMB click = primary interact (full parity with E/K); hold/drag on doors/drawers/curtains/B07 gate = physical drag!
            bool lmbClickInteract = false;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                if (!cursorWasLocked)
                {
                    cursorWasLocked = true;
                    waitForLmbRelease = true;
                    lmbPendingDragTarget = null;
                }
                if (waitForLmbRelease)
                {
                    if (!mouse.leftButton.isPressed)
                        waitForLmbRelease = false;
                }
                else if (scene.interactables != null && scene.interactables.IsDragging)
                {
                    string dragId = scene.interactables.DraggingId;
                    if (!Application.isFocused &&
                        InputSystem.settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)
                    {
                        draggingSeamGate = false;
                        CancelWorldDrag();
                        return;
                    }
                    if (mouse.leftButton.isPressed)
                    {
                        float before = scene.interactables.GetDragOpenness01(dragId);
                        scene.interactables.UpdateDrag(mouse.delta.ReadValue(), scene.atmosphere);
                        float after = scene.interactables.GetDragOpenness01(dragId);
                        if (draggingSeamGate)
                        {
                            if (after >= 0.45f)
                            {
                                draggingSeamGate = false;
                                scene.interactables.EndDrag(null);
                                player.LookSuppressed = false;
                                ExecuteB07GateAttempt();
                                return;
                            }
                            ui.UpdateWorldMarker(player.Eye, target, null, WorldMarkerKind.Drag, "Kéo mạnh cánh cổng ra · Thả để buông tay");
                            return;
                        }
                        if (HospitalScene.Occupied(feet) || dragId == "CHAIR-STAFF" && !scene.interactables.StaffChairClear())
                            scene.interactables.RestorePose(dragId, before);
                        ui.UpdateWorldMarker(player.Eye, target, null, WorldMarkerKind.Drag, "Thả để giữ vị trí · Esc để hủy");
                        return;
                    }
                    if (draggingSeamGate)
                    {
                        draggingSeamGate = false;
                        scene.interactables.EndDrag(null);
                        player.LookSuppressed = false;
                        if (scene.outerGate?.pivot != null)
                            scene.interactables.MoveDoor("GATE-CONG", scene.outerGate.pivot, 0f, Options.reduceMotion, scene.atmosphere);
                        return;
                    }
                    float finalOpen = scene.interactables.GetDragOpenness01(dragId);
                    scene.interactables.EndDrag(scene.atmosphere);
                    player.LookSuppressed = false;
                    SaveWorldPose(dragId, dragStartPose);
                    if (dragId == "DRAWER-DESK" && finalOpen > 0.54f && dragStartPose <= 0.5f)
                        TryTriggerLanDrawerLook();
                    else if (dragId == "DRAWER-NURSE" && finalOpen > 0.54f && dragStartPose <= 0.5f)
                        dreadDirector?.NotifyStaffDrawerForcePull();
                    return;
                }
                else if (lmbPendingDragTarget != null)
                {
                    if (!mouse.leftButton.isPressed)
                    {
                        // Released cleanly under threshold -> execute crisp single-click toggle/interact!
                        SceneTarget clickTarget = lmbPendingDragTarget;
                        lmbPendingDragTarget = null;
                        if (target != null && target == clickTarget)
                            lmbClickInteract = true;
                    }
                    else
                    {
                        lmbHoldDeltaPx += mouse.delta.ReadValue().magnitude;
                        bool promoteToDrag = (Time.unscaledTime - lmbHoldStartTime) >= LmbDragHoldSeconds ||
                                             lmbHoldDeltaPx >= LmbDragMovePixels;
                        if (promoteToDrag && scene.interactables != null)
                        {
                            SceneTarget dragTarget = lmbPendingDragTarget;
                            lmbPendingDragTarget = null;
                            if (dragTarget.id == "SEAM" && State.chapter == "CH01" && State.Happened("CH01-T08") && !State.Happened("CH01-T09"))
                            {
                                if (scene.interactables.BeginDrag("GATE-CONG"))
                                {
                                    draggingSeamGate = true;
                                    dragStartPose = scene.interactables.GetDragOpenness01("GATE-CONG");
                                    player.LookSuppressed = true;
                                    dreadDirector?.NotifyInteraction(0.28f);
                                    return;
                                }
                            }
                            else if (IsDraggablePhysicalRig(dragTarget.id) && scene.interactables.BeginDrag(dragTarget.id))
                            {
                                dragStartPose = scene.interactables.GetDragOpenness01(dragTarget.id);
                                player.LookSuppressed = true;
                                dreadDirector?.NotifyInteraction(0.28f);
                                return;
                            }
                        }
                    }
                }
                else if (target != null && mouse.leftButton.wasPressedThisFrame)
                {
                    bool draggableNow = IsDraggablePhysicalRig(target.id) ||
                                        (target.id == "SEAM" && State.chapter == "CH01" && State.Happened("CH01-T08") && !State.Happened("CH01-T09"));
                    if (draggableNow && scene.interactables != null)
                    {
                        lmbPendingDragTarget = target;
                        lmbHoldStartTime = Time.unscaledTime;
                        lmbHoldDeltaPx = 0f;
                    }
                    else
                    {
                        lmbClickInteract = true;
                    }
                }
            }
            else
            {
                cursorWasLocked = false;
                lmbPendingDragTarget = null;
            }

            UpdateMinimalWorldHud(target, player.BeyondReach);

            if ((use || lmbClickInteract) && target != null)
            {
                lmbPendingDragTarget = null;
                dreadDirector?.NotifyInteraction(0.32f);
                if (target.fieldObject != null) ExamineFieldObject(target);
                else Use(target.id, target.transform);
            }
            else if (TryRecordWardChange()) return;
            else if (!TryPlayStaffReaction() && !busy && !savingDialogue && !ui.IsModal &&
                (gatekeeperCameo == null || !gatekeeperCameo.IsActive) &&
                transaction.HasPendingDialogue && Time.unscaledTime >= dialogueSaveAt)
                StartCoroutine(SaveDialogue());
        }

        private void UpdateMinimalWorldHud(SceneTarget target, SceneTarget beyondReach)
        {
            if (target == null)
            {
                // Subtle distant dot ONLY — never reveal clue titles or answers from afar!
                ui.UpdateWorldMarker(player.Eye, null, beyondReach,
                    beyondReach != null ? WorldMarkerKind.FarApproach : WorldMarkerKind.None, null);
                return;
            }

            WorldMarkerKind kind = ClassifyWorldMarker(target);
            string whisper = ResolveMinimalBottomWhisper(target);
            ui.UpdateWorldMarker(player.Eye, target, null, kind, whisper);
        }

        private WorldMarkerKind ClassifyWorldMarker(SceneTarget target)
        {
            if (target == null) return WorldMarkerKind.None;
            if (IsDraggablePhysicalRig(target.id) ||
                (target.id == "SEAM" && State.chapter == "CH01" && State.Happened("CH01-T08") && !State.Happened("CH01-T09")))
                return WorldMarkerKind.Drag;
            if (target.fieldObject != null) return WorldMarkerKind.Clue;
            switch (target.id)
            {
                case "F101":
                case "BOARD":
                case "OBS101A":
                case "OBS101A_BLANKET":
                case "OBS101A_SLIPPERS":
                case "OBS101B":
                case "OBS101C":
                case "OBS101D":
                case "OBS_SHIFT_LOG":
                case "OBS_CORRIDOR_NOTICE":
                case "OBS_RECEPTION_DRAWER":
                case "OBS_CLOCK_0317":
                case "LIST":
                case "SOCKETS":
                case "WHEELS":
                case "DRAW1":
                case "DRAW2":
                case "DRAW3":
                case "WRIST":
                case "TRANSFER":
                case "E03":
                case "C02_NOTE":
                case "C02_BAG":
                case "C02_LETTER":
                case "C12_BOX":
                case "FS10_LOCK":
                case "CH05_BLUEPRINTS":
                case "E05B_SEALED_DOOR":
                case "LOC09_VALVE":
                    return WorldMarkerKind.Clue;
                default:
                    return WorldMarkerKind.Interact;
            }
        }

        private string ResolveMinimalBottomWhisper(SceneTarget target)
        {
            if (target == null) return null;
            // Secondary [Q] interactions — only show terse contextual whisper when actually available
            if (target.id == "MAILBOX" && State.Happened(E10aParts.MailboxOpened) && !State.Happened(E10aParts.MailboxPickedUp))
                return "Lấy chìa & phong bì   ·   [Q] Xem giấy nhắn";
            if (target.id is ("OBS101A" or "OBS101A_BLANKET" or "OBS101A_SLIPPERS") && CanSpatialLinkBed4())
                return "[Q] Đối chứng thẻ AS-05B với dấu hiện trường";
            if (target.id == "DRAWER-NURSE" && scene.interactables?.IntendedOpenness("DRAWER-NURSE") <= 0.5f)
                return "[Q] Mở êm chốt";
            if (target.id == "CURTAIN-101")
                return "[Q] Hé rèm";
            if (target.id == "F101" && State.chapter == "CH02")
                return "[Q] Chuyển giữa 2005 và 2012";

            // Contextual item-in-hand feedback when aiming at a compatible world target
            var eq = InventoryService.GetEquipped(State);
            if (eq != null)
            {
                if (target.id == "GATE-CONG" && eq.itemDefId == BackpackCatalog.GateKey)
                    return "Dùng chìa cổng phụ";
                if (target.id == "GATE-CONG" && eq.itemDefId == BackpackCatalog.WingCKey)
                    return "Thử chìa khóa cũ";
                if (target.id is ("COMPARE_CLIP_AN02" or "COMPARE_DESK_STAFF" or "COMPARE_DESK_RECEPTION" or "COMPARE_DESK_101"))
                {
                    var eqDef = BackpackCatalog.Get(eq.itemDefId);
                    if (eqDef != null && WorldUseResolver.Resolve(State, eqDef.Id, target.id).Outcome == WorldUseOutcome.PlacePaperOnAnchor)
                        return target.id == "COMPARE_CLIP_AN02"
                            ? $"Kẹp {eqDef.ShortLabel} cạnh phiếu F101"
                            : $"Đặt {eqDef.ShortLabel} lên mặt bàn";
                }
                if (target.id == "AN04" && State.chapter == "CH04" && eq.itemDefId == BackpackCatalog.Ch04Tape)
                    return "Đặt cuộn băng vào máy phát";
            }

            // Large-Print accessibility users get a clean terse bottom action label without restoring the giant center box
            if (Options.textScale >= 1.25f)
                return WorldPrompt(target);
            return null;
        }

        private bool StaffReactionContext() =>
            live && State.chapter == "CH01" && State.layer == Reality.B && !State.Happened("CH01-T08") &&
            scene.staffCrossDoor?.pivot != null && Mathf.Abs(player.Feet.y) < 0.25f &&
            AmbientDreadDirector.ClassifyZone(player.Feet) == "STAFF";

        private void QueueStaffReaction(string observationId)
        {
            string lineId = observationId switch
            {
                ObservationRegistry.WardOverflowNotice => "CH01-D17",
                ObservationRegistry.ShiftGuide => "CH01-D18",
                _ => null
            };
            if (lineId != null && StaffReactionContext() && !staffReactions.Contains(lineId) &&
                !State.transcript.Exists(entry => entry.LineId == lineId))
                staffReactions.Enqueue(lineId);
        }

        private bool TryPlayStaffReaction()
        {
            if (staffReactions.Count == 0 || busy || savingDialogue || ui.IsModal || !StaffReactionContext() ||
                voice.IsPaused || voice.IsSpeaking || lanDirector.IsEncounterRunning || inspector.IsInspecting)
                return false;

            string lineId = staffReactions.Dequeue();
            string copy = LineAtBeat(lineId, 1, "STAFF");
            if (string.IsNullOrEmpty(copy) || State.transcript.Exists(entry => entry.LineId == lineId))
                return false;

            // Save the transcript as the once-per-discovery record before offering the caption.
            // Restoring it into the director also keeps Speak from recording a duplicate.
            var next = State.Copy();
            string speaker = dialogue[lineId].speaker;
            next.transcript.Add(new TranscriptEntry(lineId, speaker, copy));
            SaveCheckpoint(() => transaction.CommitEvidenceAsync(next, publishGameplay: false), () =>
            {
                RestoreJournal(State);
                if (!StaffReactionContext() || ui.IsModal || voice.IsSpeaking || lanDirector.IsEncounterRunning) return;
                bool isSilent = dialogue.TryGetValue(lineId, out var staffEntry) &&
                    ParsePresentationMode(staffEntry.presentationMode) == DialoguePresentationMode.INNER_SUB_SILENT;
                staffReactionCaption = copy;
                PresentLine(lineId, 1, "STAFF", onComplete: isSilent ? null : (_ => staffReactionCaption = null));
            });
            return true;
        }

        private void CancelStaffReaction()
        {
            if (staffReactionCaption == null) return;
            ui.ClearCaption(staffReactionCaption);
            staffReactionCaption = null;
            voice.StopCurrent();
        }

        private static bool IsDraggablePhysicalRig(string id) => ScenePropPose.Known(id);

        private IEnumerator SaveDialogue()
        {
            savingDialogue = true;
            var task = transaction.SaveDialogueAsync();
            while (!task.IsCompleted) yield return null;
            savingDialogue = false;
            if (task.GetAwaiter().GetResult() != CommitResult.Applied) dialogueSaveAt = Time.unscaledTime + 10f;
        }

        private void UpdateProgressiveHints()
        {
            if (!Options.hintsEnabled || State.Happened("M1-complete")) return;
            objectiveStagnantSeconds += Time.unscaledDeltaTime;
            int tier = objectiveStagnantSeconds >= 180f ? 3 : objectiveStagnantSeconds >= 120f ? 2 : objectiveStagnantSeconds >= 60f ? 1 : 0;
            if (tier <= hintTierShown) return;
            hintTierShown = tier;
            string hint = BuildProgressiveHint(tier);
            if (!string.IsNullOrEmpty(hint))
                ui.Toast("GỢI Ý NHẸ · SỔ TRỰC", hint, 6.5f);
        }

        private string BuildProgressiveHint(int tier)
        {
            if (State.chapter == "CH01")
            {
                if (!State.Happened(E10aParts.MailboxPickedUp))
                    return "Kiểm tra hộp thư sắt gắn ở trụ cổng bên phải để lấy chìa cổng phụ và phong bì chú Bảy gửi lại (hoặc nhấn [P] mở điện thoại).";
                if (!State.Happened("CH01-T02")) return "Dùng chìa cổng phụ đang cầm trên tay để mở khóa cổng sắt phía trước.";
                if (!State.Happened("CH01-T03")) return "Qua sân vào sảnh và giở sổ trực trên quầy tiếp nhận.";
                if (!State.Happened("CH01-T04")) return "Lại gần quầy và trò chuyện với Lan.";
                if (!State.Happened("CH01-T05")) return "Xem và nhận bảng kẹp phân công Phòng 101 đặt trên mặt quầy lễ tân.";
                if (!GateService.G01(State))
                    return tier == 1 ? "Đi dọc hành lang, mở cửa Phòng 101 bên phải để kiểm tra phiếu bàn giao F101."
                        : "Kiểm tra các dấu vết quanh buồng trước khi ký trên bảng kẹp.";
                if (!State.Happened("CH01-T08")) return "Quay lại sổ trực ở quầy tiếp nhận để bàn giao.";
                if (!State.Happened("CH01-T09")) return "Quay ra cánh cổng sắt ngoài sân và kéo thử để rời khỏi bệnh viện.";
                return "Trở lại sổ trực ở bàn lễ tân để ghi nhận bất thường và mở bước điều tra hiện trường.";
            }
            if (!State.Has("E12"))
                return tier == 1 ? "Trong Phòng 101 (hiện trường 2012), kiểm tra bản kê 3 giường, 4 ổ cắm trên tường và dấu bánh xe trên sàn."
                    : "Sau khi ghi đủ 3 dấu vết vật lý, lại gần tủ sắt kính chắn cửa kho để đối chiếu chỗ kê giường thứ 4.";
            if (!EvidenceRegistry.BundleComplete(State))
                return "Vào kho giường sau tủ sắt. Tìm mảnh tranh, vòng tay và phiếu chuyển rồi đối chiếu tại bảng kẹp.";
            if (!GateService.G02(State))
                return "Mở hồ sơ trên bảng kẹp Phòng 101 để ghép tranh và đối chiếu.";
            return "Cửa Cấp cứu ở cuối hành lang chính (hoặc hành lang nhân viên bên trái) đã mở. Tới sổ giao ca Cấp cứu.";
        }

        private bool NeedsHub() => !State.Happened("CH01-T03") && State.Happened("CH01-T02") ||
            GateService.G01(State) && !State.Happened("CH01-T08") ||
            State.Happened("CH01-T09") && !State.Happened("CH01-T10");

        private bool Available(SceneTarget target) => target.id switch
        {
            "CHAIR-STAFF" => State.chapter == "CH01",
            "GATE-CONG" => !State.Happened("CH01-T02"),
            "MAILBOX" => State.chapter == "CH01" && State.layer == Reality.R,
            "SEAM" => State.Happened("CH01-T08"),
            "AN01" => State.Happened("CH01-T02") && !(F101SignaturePlaying && NeedsHub()),
            "BOARD" => State.layer == Reality.B && State.chapter == "CH01" && State.Happened("CH01-T03") && !State.Happened("CH01-T05"),
            "LAN_TALK" => State.layer == Reality.B && State.chapter == "CH01" && State.Happened("CH01-T03") &&
                !State.Happened("CH01-T08") && !GateService.G01(State) &&
                scene.LanActor != null && scene.LanActor.gameObject.activeInHierarchy &&
                (!State.Happened("CH01-T04") || NextLanReflection() != null),
            "LAN_EMPTY_DESK" => State.layer == Reality.B && State.chapter == "CH01" && GateService.G01(State) && !State.Happened("CH01-T08"),
            "COMPARE_CLIP_AN02" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02") && casework.CanRevise,
            "COMPARE_DESK_STAFF" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02"),
            "COMPARE_DESK_RECEPTION" => State.Happened("CH01-T02") && State.chapter is ("CH01" or "CH02"),
            "COMPARE_DESK_101" => State.chapter is ("CH01" or "CH02") && (State.layer == Reality.B || State.layer == Reality.R),
            "BATTERY" => !State.Happened("CH01-BATTERY") && State.Happened("CH01-T02"),
            "F101" => casework.CanRevise && !F101SignaturePlaying && gatekeeperCameo?.IsActive != true,
            "AN02" => false,
            "OBS101A" or "OBS101A_BLANKET" or "OBS101A_SLIPPERS" or "OBS101B" =>
                State.layer == Reality.B && State.chapter is ("CH01" or "CH02") && State.f101.current != FormStatus.Affirmed,
            "OBS101C" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02"),
            "OBS101D" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02") && (scene.interactables == null || scene.interactables.IsDrawerOpen("DRAWER-BED3")),
            "OBS_SHIFT_LOG" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02") && (scene.interactables == null || scene.interactables.IsDrawerOpen("DRAWER-NURSE")),
            "OBS_CORRIDOR_NOTICE" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02"),
            "OBS_RECEPTION_DRAWER" => State.Happened("CH01-T03") && State.chapter is ("CH01" or "CH02") && (scene.interactables == null || scene.interactables.IsDrawerOpen("DRAWER-DESK")),
            "OBS_CLOCK_0317" => State.Happened("CH01-T02") && State.chapter is ("CH01" or "CH02"),
            "DRAWER-BED3" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02"),
            "DRAWER-NURSE" => State.layer == Reality.B && State.chapter is ("CH01" or "CH02"),
            "DRAWER-DESK" => State.Happened("CH01-T03") && State.chapter is ("CH01" or "CH02"),
            "DOOR-STAFF" => State.Happened("CH01-T03"),
            "LIST" or "SOCKETS" or "WHEELS" or "PZ01" => State.chapter == "CH02" && State.layer == Reality.R,
            "EXIT" => State.chapter == "CH02" && GateService.G02(State),
            "CH03_DOOR" => false,
            "DOOR-101" => State.Happened("CH01-T05") || State.layer == Reality.R,
            "CURTAIN-101" => scene.interactables != null && scene.interactables.curtainCloth != null && scene.interactables.curtainCloth.gameObject.activeInHierarchy,
            "AN03" => State.chapter is ("CH03" or "CH04" or "CH05") && !(State.chapter == "CH05" && State.layer == Reality.M),
            "E03" or "C02_NOTE" or "C02_BAG" or "C02_LETTER" or "C12_BOX" or "FS10_LOCK" or "RADIO_LOC07" or "G03" =>
                State.chapter is ("CH03" or "CH04" or "CH05") && State.layer == Reality.R,
            "CH03_EXIT" => State.chapter == "CH03" && GateService.G03(State),
            "AN04" or "CH04_POWER" or "E04_E07" => State.chapter is ("CH04" or "CH05") && State.layer == Reality.R,
            "PZ04_SCHEDULE" or "PZ04_ROOM_SLIP" or "PZ04_STATUS_BOARD" => State.chapter == "CH04" && State.layer == Reality.R,
            "AN05" or "AN05_HANDOFF_NOTE" => State.chapter is ("CH04" or "CH05") && State.layer == Reality.R && GateService.G04(State) && State.Happened("CH04-T06"),
            "CH05_M_PAIR1" or "CH05_M_PAIR2" or "CH05_M_PAIR3_ASSEMBLY" or "CH05_M_EXIT" => State.chapter == "CH05" && State.layer == Reality.M,
            "CH05_BLUEPRINTS" or "LOC09_VALVE" or "E05B_SEALED_DOOR" => State.chapter == "CH05" && State.layer == Reality.R,
            "LOC08_GATE" => State.chapter is ("CH03" or "CH04" or "CH05") && State.layer == Reality.R && !GateService.G05(State),
            "CH05_HANDOFF" => State.chapter == "CH05" && State.layer == Reality.R && GateService.G05(State),
            Ch01FieldObjects.BinInterior => State.chapter == "CH01" && scene.PedalBin?.CanSeeInside == true,
            _ when target.fieldObject != null => target.fieldObject.Observation.AllowsChapter(State.chapter) &&
                target.fieldObject.Observation.AllowsLayer(State.layer) &&
                (target.fieldObject.Drawer == null || scene.interactables?.IsDrawerOpen(target.fieldObject.Drawer) == true),
            _ => true
        };

        private void Use(string id, Transform source = null)
        {
            switch (id)
            {
                case "GATE-CONG":
                    TryUnlockOuterGate();
                    break;
                case "BATTERY":
                    if (Report(casework.TakeSpareBattery(player.Feet)))
                    {
                        player.RestoreBattery(360);
                        scene.atmosphere?.From("doorLatch", player.Feet + Vector3.up);
                        ui.Toast("PIN ĐÈN PIN DỰ PHÒNG", "Đã thay đôi pin khô đặt trên quầy lễ tân. Pin đèn hồi đầy 100%.", 4.5f);
                    }
                    break;
                case "STAIRWELL":
                    ui.Caption("Cầu thang lên Khu B đã bị rào chắn và khóa xích trước ngày tháo dỡ.", 4.5f, true);
                    break;
                case "DOOR-101":
                case "DOOR-STAFF":
                case "DRAWER-BED3":
                case "DRAWER-DESK":
                case "DRAWER-NURSE":
                case "CURTAIN-101":
                case "CHAIR-STAFF":
                    ToggleWorldProp(id);
                    break;
                case "SEAM":
                    ExecuteB07GateAttempt();
                    break;
                case "MAILBOX":
                    InteractGateMailbox(source);
                    break;
                case "E10a": Letter(source); break;
                case "LAN_TALK":
                    SpeakAtReception();
                    break;
                case "LAN_EMPTY_DESK":
                    ui.Caption("Ghế trực trống, cây bút đặt ngang mép sổ. Không còn tiếng viết hay bóng dáng Lan.", 4.6f, true);
                    break;
                case "COMPARE_CLIP_AN02":
                case "COMPARE_DESK_STAFF":
                case "COMPARE_DESK_RECEPTION":
                case "COMPARE_DESK_101":
                    InteractComparisonSurface(id);
                    break;
                case "BOARD":
                    if (!State.Happened("CH01-T04"))
                        ui.Caption("Lan đang ở ngay quầy — nên nói chuyện với cô ấy trước khi cầm bảng kẹp.", 3.6f, true);
                    else
                        InspectBoardBeforeAccept(source);
                    break;
                case "AN01":
                    if (NeedsHub() && State.chapter == "CH01") Hub(source);
                    else if (IsJournalAllowedOnAn01) Journal();
                    else OpenAn01ReadOnlyInCh01();
                    break;
                case "F101":
                    if (State.layer == Reality.B) WorldF101(source);
                    else Anchor();
                    break;
                case "OBS101A":
                    if (InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.PaperObs101B && CanSpatialLinkBed4())
                        TriggerSpatialBed4Link();
                    else
                        ObserveBed4Trace(BlanketTrace, "Dấu sử dụng tại vị trí giường 4");
                    break;
                case "OBS101A_BLANKET":
                    if (InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.PaperObs101B && CanSpatialLinkBed4())
                        TriggerSpatialBed4Link();
                    else
                        ObserveBed4Trace(BlanketTrace, "Chăn bông gấp vội");
                    break;
                case "OBS101A_SLIPPERS":
                    if (InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.PaperObs101B && CanSpatialLinkBed4())
                        TriggerSpatialBed4Link();
                    else
                        ObserveBed4Trace(SlipperTrace, "Đôi dép lệch");
                    break;
                case "OBS101B":
                    InspectRegisteredObservation(ObservationRegistry.Bed4Card, "LOC03", source: source);
                    break;
                case "OBS101C":
                    InspectRegisteredObservation(ObservationRegistry.Bed2Transfer, "LOC03", source: source);
                    break;
                case "OBS101D":
                    InspectRegisteredObservation(ObservationRegistry.Bed3Meds, "LOC03", source: source);
                    break;
                case "OBS_SHIFT_LOG":
                    InspectRegisteredObservation(ObservationRegistry.ShiftGuide, "STAFF", source: source);
                    break;
                case "OBS_CORRIDOR_NOTICE":
                    InspectRegisteredObservation(ObservationRegistry.WardOverflowNotice, "STAFF", source: source);
                    break;
                case "OBS_RECEPTION_DRAWER":
                    InspectRegisteredObservation(ObservationRegistry.ReceptionDrawerLog, "LOC02", source: source);
                    break;
                case "OBS_CLOCK_0317":
                    if (State.layer == Reality.B)
                        DiegeticObservation(ObservationRegistry.StoppedClock0317, "Đồng hồ dừng ở 03:17",
                            "Kim đồng hồ treo sau quầy đứng yên ở 03:17. Kèm một nhịp ù rất khẽ của thiết bị đo như vọng từ xa.",
                            "LOC02", () =>
                            {
                                var clockPos = new Vector3(-0.99f, 2.74f, -3.26f);
                                scene.atmosphere?.From("clockTick", clockPos, .45f);
                                scene.atmosphere?.From("clockEegHum", clockPos, .58f);
                            });
                    else ui.Caption("Kim đồng hồ ngoài quầy chỉ 11:52.", 3f, true);
                    break;
                case "LIST":
                    InspectDocumentObservation(id, "Bản kê phòng bệnh",
                        "Bản kê cũ có ba dòng, đánh số 1 đến 3. Tổng số ghi trên bản kê là 03.", "BedList");
                    break;
                case "SOCKETS":
                    DiegeticObservation(id, "Bốn ổ cắm đầu giường",
                        "Phòng kê ba giường, nhưng trên tường vẫn còn đủ bốn ổ cắm đánh số 1 đến 4.");
                    break;
                case "WHEELS":
                    DiegeticObservation(id, "Dấu bánh xe tại hốc 4",
                        "Bốn vết bánh tạo thành hình chữ nhật một khung giường. Đường kéo cao su dừng ngay trước tủ sắt kho.");
                    break;
                case "PZ01": PhysicalContradiction(); break;
                case "DRAW1": Take3D(EvidenceRegistry.DrawingFragments[0], "Mảnh tranh 1", "Nét mái ngói vẽ bằng sáp màu dừng ở mép giấy rách.", "DrawingPiece1", null); break;
                case "DRAW2": Take3D(EvidenceRegistry.DrawingFragments[1], "Mảnh tranh 2", "Nét cánh cửa màu xanh và ổ khóa bằng chì sẫm.", "DrawingPiece2", null); break;
                case "DRAW3": Take3D(EvidenceRegistry.DrawingFragments[2], "Mảnh tranh 3", "Ông mặt trời và một nửa ô cửa sổ nhỏ.", "DrawingPiece3", null); break;
                case "WRIST": Take3D(EvidenceRegistry.Wristband, "Vòng tay bệnh nhi C01", "Phiến giấy trong vòng nhựa mờ ghi mã C01. Vòng được giữ trong túi đồ vải ở kho 101.", null, "WRIST"); break;
                case "TRANSFER":
                    if (State.chapter == "CH02" && State.layer == Reality.R && State.Has("E12"))
                        Take3D(EvidenceRegistry.Transfer, "Phiếu chuyển 101/4",
                            "17/11/2005 · Bệnh nhi An (C01)\nKhám sốt tạm tại giường 4, Phòng 101. Theo lệnh phòng Giám đốc, chuyển lại Khu C trong ngày.\n\nPhiếu viết tay của Lan, chưa nhập sổ chính.",
                            "Transfer", null, new ActionChoice("Đọc dòng gạch xóa", Ch02ReadTransferNote));
                    break;
                case "G02":
                    ui.Caption(GateService.G02(State)
                        ? "Cửa cấp cứu đã mở."
                        : State.chapter == "CH01"
                            ? "Khu Cấp cứu tạm khóa trong ca này. Quay lại xử lý phiếu Phòng 101."
                            : "Cấp cứu tạm đóng. Hãy đối chiếu hồ sơ phòng 101.", 5, true);
                    break;
                case "EXIT":
                    StartCoroutine(SaveEmergency(true));
                    break;
                case "E03":
                    Ch03ReadRegister();
                    break;
                case "C02_NOTE":
                    SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.C02CareNote, "LOC07"), () =>
                    {
                        ui.Toast("GHI CHÚ KẸP SỢI LEN", "B/2. Giữ khăn đan dở trong túi có thẻ K-02.", 5.5f);
                    });
                    break;
                case "AN03":
                    Ch03ArchiveAnchor();
                    break;
                case "C02_BAG":
                    Ch03InspectKnittingBag();
                    break;
                case "C02_LETTER":
                    Ch03InspectOptionalIdentityLetter();
                    break;
                case "C12_BOX":
                    Ch03InspectAnonymousBoxC12();
                    break;
                case "FS10_LOCK":
                    SaveCheckpoint(() => casework.RecordAsync("FS10", player.Feet), () =>
                    {
                        ui.Toast("DẤU NIÊM PHONG CŨ", "Ổ khóa tủ lưu trữ mang vết kìm cắt cũ và nhãn niêm phong nội bộ đêm 18/11/2005.", 5.0f);
                    });
                    break;
                case "RADIO_LOC07":
                    TriggerLoc07RadioDistraction();
                    break;
                case "G03":
                    ui.Caption(GateService.G03(State)
                        ? "Cửa khu lưu trữ đã mở."
                        : "Cửa phòng trực đang khóa. Đối chiếu sổ tên và túi len tại bàn đọc tầng 3.", 5f, true);
                    break;
                case "CH03_EXIT":
                    SaveCheckpoint(() => casework.CompleteCh03Async(player.Feet), Ch03EndOfChapter);
                    break;
                case "AN04":
                    if (!TryUseHeldTapeOnAn04()) Ch04DutyRoom();
                    break;
                case "PZ04_SCHEDULE":
                    Ch04OrderlySchedule();
                    break;
                case "PZ04_ROOM_SLIP":
                    Ch04RoomStatusSlip();
                    break;
                case "PZ04_STATUS_BOARD":
                    Ch04WardStatusBoard();
                    break;
                case "AN05":
                    Ch05An05Anchor();
                    break;
                case "AN05_HANDOFF_NOTE":
                    Ch05An05DirectionNote();
                    break;
                case "CH05_M_PAIR1":
                    Ch05MemoryPair1();
                    break;
                case "CH05_M_PAIR2":
                    Ch05MemoryPair2();
                    break;
                case "CH05_M_PAIR3_ASSEMBLY":
                    Ch05MemoryPair3Assembly();
                    break;
                case "CH05_M_EXIT":
                    Ch05MemoryControlModal();
                    break;
                case "CH05_BLUEPRINTS":
                    Ch05BlueprintsLoc09();
                    break;
                case "LOC09_VALVE":
                    Ch05OptionalValveLoc09();
                    break;
                case "E05B_SEALED_DOOR":
                    Ch05SealedWallE05b();
                    break;
                case "LOC08_GATE":
                    Ch05Loc08GatePrompt();
                    break;
                case "CH05_HANDOFF":
                    Ch05HandoffLoc08();
                    break;
                case "CH04_POWER":
                    Ch04PowerBoard();
                    break;
                case "E04_E07":
                    Ch04Morgue();
                    break;
            }
        }

        private IEnumerator UpdateRegions()
        {
            streaming = true;
            try { yield return streamer.Reconcile(State, player.Feet); }
            finally { streaming = false; reconcileRegionsAt = Time.unscaledTime + 0.4f; }
        }

        private IEnumerator SaveEmergency(bool finish)
        {
            while (savingDialogue) yield return null;
            busy = true;
            RefreshControls();
            bool firstEscape = !State.Happened("CH02-T06");
            var save = casework.SaveEmergencyAsync(player.Feet, finish, gatekeeperCameo.IsActive);
            while (!save.IsCompleted) yield return null;
            busy = false;
            RefreshControls();
            if (!Report(save.GetAwaiter().GetResult()))
            {
                retryEmergencySaveAt = Time.unscaledTime + 5f;
                yield break;
            }
            if (firstEscape)
                ui.Caption("[Tiếng chùm chìa khóa xa dần sau cánh cửa kính mờ Cấp cứu]", 4.5f, true);
            if (finish) EndOfVisit();
        }

        private void Hub(Transform source)
        {
            bool enteringB = !State.Happened("CH01-T03");
            bool collapsingR = GateService.G01(State) && !State.Happened("CH01-T08");
            bool startingCh02 = State.Happened("CH01-T09") && !State.Happened("CH01-T10");

            if (collapsingR && F101SignaturePlaying) return;
            if (!transaction.AtRequiredPlace(player.Feet, RealityTransaction.ReceptionDesk))
            {
                ui.Caption("Lại gần sổ trực trên quầy.", 2.7f, true);
                return;
            }
            if (source != null) scene.atmosphere?.From("paperFlip", source.position, 0.6f);
            if (collapsingR)
                scene.atmosphere?.From("breakerCut", new Vector3(1.5f, 2.2f, -3.2f));

            TransitionAsync(() => casework.AtHubAsync(player.Feet), false, () =>
            {
                dreadDirector?.NotifyMajorBeatQuietWindow(12f);
                if (enteringB)
                {
                    // Pha 1 complete: Lan sits quietly at the reception desk writing (CH01_PenWrite loop).
                    // Pha 2 (scripted recognition) & Pha 3 (player-initiated cinematic via LAN_TALK) unfold naturally!
                    ui.Caption("[Ánh đèn hành lang sáng ấm lên; phía trong quầy vang nhẹ tiếng ngòi bút lướt trên giấy.]", 4.2f);
                }
                else if (collapsingR)
                {
                    lanDirector?.StopEncounter(false);
                    scene.atmosphere?.SetPenWriting(false, Vector3.zero);
                    PresentLine("CH01-D13", 1, "B06");
                }
                else if (startingCh02)
                {
                    scene.atmosphere?.SetReturnWalkRainScale(1f);
                    WritePlaytestTelemetryIfEnabled();
                    ui.ShowChapterCard("CHƯƠNG 02  ·  17.11.2012", "PHÒNG BỆNH TRỐNG", 4.6f);
                    scene.atmosphere?.From("phoneBuzz", scene.PhoneAnchor != null ? scene.PhoneAnchor.transform.position : new Vector3(1.8f, 1.2f, -3.2f));
                    PresentLine("CH02-D01");
                }
            });
        }

        [Serializable]
        private sealed class Ch01PlaytestTelemetry
        {
            public string timestampUtc;
            public float durationSeconds;
            public string firstF101Decision;
            public string currentF101Decision;
            public int f101Revisions;
            public int f101FailedAttempts;
            public bool visitedStaffAlcove;
            public int observedCount;
            public bool corroborated0317;
            public bool rakingIndentRevealed;
            public int dreadEventsCount;
            public bool hintsEnabled;
            public bool storyMode;
            public bool reduceMotion;
        }

        private void WritePlaytestTelemetryIfEnabled()
        {
            string logPath = Argument("-ctcc-playtest-log");
            if (string.IsNullOrWhiteSpace(logPath) || State == null) return;
            try
            {
                string full = Path.GetFullPath(logPath);
                string dir = Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var payload = new Ch01PlaytestTelemetry
                {
                    timestampUtc = DateTime.UtcNow.ToString("O"),
                    durationSeconds = Mathf.Max(0f, Time.unscaledTime - ch01StartUnscaledTime),
                    firstF101Decision = State.f101.firstDecision.ToString(),
                    currentF101Decision = State.f101.current.ToString(),
                    f101Revisions = State.f101.revision,
                    f101FailedAttempts = PuzzleService.FailedAttempts(State, HypothesisService.F101PuzzleId),
                    visitedStaffAlcove = dreadDirector != null && dreadDirector.VisitedStaffAlcove,
                    observedCount = State.observed != null ? State.observed.Count : 0,
                    corroborated0317 = CorroborationService.IsCorroborated(State),
                    rakingIndentRevealed = CorroborationService.HasRakingIndent(State),
                    dreadEventsCount = State.dreadEventsSeen != null ? State.dreadEventsSeen.Count : 0,
                    hintsEnabled = Options.hintsEnabled,
                    storyMode = Options.storyMode,
                    reduceMotion = Options.reduceMotion
                };
                File.WriteAllText(full, JsonUtility.ToJson(payload, true), System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("CTCC playtest telemetry write skipped: " + ex.Message);
            }
        }

        private void SwitchAtClipboard()
        {
            if (State.chapter != "CH02" ||
                !transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard))
            {
                CloseModal();
                ui.Caption("Đối chiếu tại bảng kẹp bên trong phòng 101.", 4f, true);
                return;
            }
            CloseModal();
            TransitionAsync(() => casework.SwitchIn101Async(player.Feet), false);
        }

        private void Anchor()
        {
            if (State.chapter != "CH02" ||
                !transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard))
            {
                CloseModal();
                return;
            }
            var choices = new List<ActionChoice>
            {
                new(State.layer == Reality.R ? "Bước vào B · Bản báo cáo 2005" : "Trở về R · Hiện trường 2012", SwitchAtClipboard),
                new("Đối chiếu phiếu F101", Form),
                new("Ghép tranh và kiểm hồ sơ", Identify),
                new("Quay lại phòng", CloseModal)
            };
            ui.Show("Phòng 101", "Bảng kẹp bàn giao",
                State.layer == Reality.B ? "Bản bàn giao năm 2005" : "Hiện trường năm 2012",
                choices.ToArray(), CloseModal);
        }

        private void Transition(Func<CommitResult> apply, bool warp, Action after = null)
        {
            TransitionAsync(() => Task.FromResult(apply()), warp, after);
        }

        private void TransitionAsync(Func<Task<CommitResult>> applyAsync, bool warp, Action after = null)
        {
            if (busy) return;
            StartCoroutine(WhileTransitionAsync(applyAsync, warp, after));
        }

        private IEnumerator WhileTransitionAsync(Func<Task<CommitResult>> applyAsync, bool warp, Action after)
        {
            busy = true;
            ui.Close();
            RefreshControls();
            while (savingDialogue) yield return null;

            Reality beforeLayer = State.layer;
            bool reduced = Options.reduceMotion;
            bool keepView = Options.disableCameraAssist || !warp && State.chapter is ("CH01" or "CH02");
            float baseFov = player.Eye != null ? player.Eye.fieldOfView : Options.fov;
            Volume activeVol = beforeLayer == Reality.B ? scene.reportGrade : scene.realGrade;
            ChromaticAberration ca = null;
            LensDistortion lens = null;
            if (!reduced && !keepView && activeVol != null && activeVol.sharedProfile != null)
            {
                activeVol.sharedProfile.TryGet(out ca);
                activeVol.sharedProfile.TryGet(out lens);
            }

            float seconds = reduced ? 0.07f : keepView ? 0.21f : 0.42f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / seconds);
                float ease = keepView ? Mathf.SmoothStep(0f, 1f, k) : k * k;
                ui.Fade(ease);
                if (!reduced && !keepView)
                {
                    if (ca != null) ca.intensity.value = Mathf.Lerp(0.06f, 0.48f, ease);
                    if (lens != null) lens.intensity.value = Mathf.Lerp(0f, -0.22f, ease);
                    if (player.Eye != null) player.Eye.fieldOfView = baseFov + ease * 3.8f;
                }
                yield return null;
            }
            ui.Fade(1f);

            // Atomic CommitAsync runs durable NTFS Flush on background thread during peak dark fade (ZERO main-thread spike!)
            scene.smoothPresentation = false;
            var task = applyAsync();
            while (!task.IsCompleted) yield return null;
            var outcome = task.IsFaulted ? CommitResult.SaveUnavailable : task.GetAwaiter().GetResult();
            if (task.IsFaulted) Debug.LogException(task.Exception);
            scene.smoothPresentation = qaFolder == null;

            bool switchedLayer = State.layer != beforeLayer;
            if (outcome == CommitResult.Applied)
            {
                if (warp) player.Teleport(scene.Pad(SafeCheckpoints.Pad(State.checkpoint)));
                string cueName = switchedLayer ? (State.layer == Reality.B ? "switchToB" : "switchToR") : "switch";
                scene.atmosphere?.From(cueName, player.Feet + Vector3.up);
            }

            if (ca != null) ca.intensity.value = 0.06f;
            if (lens != null) lens.intensity.value = 0f;

            Volume nextVol = State.layer == Reality.B ? scene.reportGrade : scene.realGrade;
            ChromaticAberration nextCa = null;
            if (!reduced && !keepView && nextVol != null && nextVol.sharedProfile != null)
                nextVol.sharedProfile.TryGet(out nextCa);

            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Clamp01(t / seconds);
                float ease = keepView ? Mathf.SmoothStep(0f, 1f, k) : k * k;
                ui.Fade(ease);
                if (!reduced && !keepView)
                {
                    if (nextCa != null) nextCa.intensity.value = Mathf.Lerp(0.04f, 0.36f, ease);
                    if (player.Eye != null) player.Eye.fieldOfView = baseFov + ease * 2.4f;
                }
                yield return null;
            }

            if (nextCa != null) nextCa.intensity.value = State.layer == Reality.R ? 0.06f : 0.03f;
            if (player.Eye != null) player.Eye.fieldOfView = Mathf.Clamp(Options.fov, 55f, 82f);
            ui.Fade(0f);
            busy = false;
            RefreshControls();
            if (Report(outcome))
            {
                if (switchedLayer && reduced)
                    ui.Caption(State.layer == Reality.M ? "[Ký ức đêm 18/11/2005]"
                        : State.layer == Reality.B ? "[Bản bàn giao đêm 18/11/2005]"
                        : "[Hiện trường đêm 17/11/2012]", 3.5f, true);
                after?.Invoke();
            }
        }

        private bool Report(CommitResult result)
        {
            if (result == CommitResult.Applied) return true;
            string copy = result switch
            {
                CommitResult.PadRequired => "Còn thiếu điều kiện. Kiểm tra lại vị trí thao tác và các nguồn đã xem.",
                CommitResult.Obstructed => "Vị trí chuyển lớp đang bị chắn. Hồ sơ được giữ trước lần thử.",
                _ => "Chưa ghi được bản lưu. Ca trực vẫn ở mốc trước thao tác."
            };
            ui.Caption(copy, 5, true);
            return false;
        }

        private bool MayDeliver(string id, string beat)
        {
            if (string.IsNullOrEmpty(id) || !dialogue.TryGetValue(id, out var dEntry)) return false;
            if (dEntry.chapter is ("CH06" or "CH07" or "CH08") &&
                !string.Equals(State?.chapter, dEntry.chapter, StringComparison.Ordinal))
                return false;
            if (beat == "STAFF" && !StaffReactionContext()) return false;
            return !revealConditions.TryGetValue(id, out var condition) || condition.Allows(State, beat);
        }

        private void RestoreJournal(Investigation state) =>
            voice.RestoreTranscript(state.transcript.Select(entry => entry.LineId == "CH01-D04c"
                ? new TranscriptEntry(entry.LineId, dialogue[entry.LineId].speaker, Line(entry.LineId))
                : entry), id => dialogue.ContainsKey(id) &&
                (!revealConditions.TryGetValue(id, out var condition) || condition.HasPrerequisites(state)));

        private (string speaker, string text) LanEncounterLine(string id)
        {
            if (!dialogue.TryGetValue(id, out var entry)) return (null, null);
            string speaker = string.IsNullOrEmpty(entry.displaySpeaker) ? entry.speaker : entry.displaySpeaker;
            string text = Line(id);
            if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(entry.pocketNoteText))
                text = entry.pocketNoteText;
            return (speaker, text);
        }

        private string Line(string id, int ordinal = 1) => LineAtBeat(id, ordinal, null);

        private static DialoguePresentationMode ParsePresentationMode(string mode) => mode switch
        {
            "VO_REACTION_SFX" => DialoguePresentationMode.VO_REACTION_SFX,
            "INNER_SUB_SILENT" => DialoguePresentationMode.INNER_SUB_SILENT,
            "CUT_TO_JOURNAL" => DialoguePresentationMode.CUT_TO_JOURNAL,
            "UI_NON_VO" => DialoguePresentationMode.UI_NON_VO,
            _ => DialoguePresentationMode.VO_DIALOGUE,
        };

        private void ExpandDialogueVariants(DialogueEntry entry)
        {
            if (entry?.variants == null || entry.variants.Length == 0) return;
            DialogueVariant baseVariant = null;
            foreach (var variant in entry.variants)
            {
                if (variant == null || string.IsNullOrEmpty(variant.variantId)) continue;
                if (variant.variantId == entry.id)
                {
                    baseVariant = variant;
                    continue;
                }
                if (dialogue.ContainsKey(variant.variantId)) continue;
                dialogue.Add(variant.variantId, new DialogueEntry
                {
                    id = variant.variantId,
                    chapter = entry.chapter,
                    speaker = entry.speaker,
                    displaySpeaker = string.IsNullOrEmpty(variant.displaySpeaker) ? entry.displaySpeaker : variant.displaySpeaker,
                    context = entry.context,
                    presentationMode = variant.presentationMode,
                    needsVO = variant.needsVO,
                    showSpeakerPrefix = variant.showSpeakerPrefix,
                    isItalicSilentThought = variant.isItalicSilentThought,
                    runtimeCopy = variant.runtimeCopy,
                    legacyCopy = entry.legacyCopy,
                    uiAudioRule = variant.uiAudioRule,
                    copy = !string.IsNullOrWhiteSpace(variant.runtimeCopy) ? variant.runtimeCopy : entry.copy,
                    variants = Array.Empty<DialogueVariant>(),
                });
            }
            if (baseVariant == null) return;
            entry.displaySpeaker = string.IsNullOrEmpty(baseVariant.displaySpeaker) ? entry.displaySpeaker : baseVariant.displaySpeaker;
            entry.presentationMode = baseVariant.presentationMode;
            entry.needsVO = baseVariant.needsVO;
            entry.showSpeakerPrefix = baseVariant.showSpeakerPrefix;
            entry.isItalicSilentThought = baseVariant.isItalicSilentThought;
            entry.runtimeCopy = baseVariant.runtimeCopy;
            entry.uiAudioRule = baseVariant.uiAudioRule;
        }

        private DialogueVariant SelectVariant(DialogueEntry entry, int ordinal, string beat)
        {
            if (entry?.variants == null || entry.variants.Length == 0) return null;
            if (!string.IsNullOrEmpty(beat))
            {
                foreach (var variant in entry.variants)
                    if (variant != null && variant.beat == beat) return variant;
            }
            if (ordinal > 0 && ordinal <= entry.variants.Length)
            {
                var candidate = entry.variants[ordinal - 1];
                if (candidate != null) return candidate;
            }
            return null;
        }

        private string RuntimeLine(DialogueEntry entry, int ordinal, string beat)
        {
            var variant = SelectVariant(entry, ordinal, beat);
            string text = variant != null ? variant.runtimeCopy : entry.runtimeCopy;
            if (!string.IsNullOrWhiteSpace(text))
                return text.StartsWith("[ẨN", StringComparison.Ordinal) ? "" : text;
            if (string.IsNullOrWhiteSpace(entry.copy)) return "";
            var quotes = System.Text.RegularExpressions.Regex.Matches(entry.copy, "“([^”]+)”");
            return ordinal <= quotes.Count ? quotes[ordinal - 1].Groups[1].Value : entry.copy;
        }

        /// <summary>Core v3.3.3 DEC-VO-01 presentation-aware line delivery.</summary>
        private void PresentLine(string id, int ordinal = 1, string beat = null, AudioSource spatialAnchor = null,
            CharacterActor speakerActor = null, string directionalPrefix = null, Action<bool> onComplete = null,
            bool essential = true)
        {
            if (!MayDeliver(id, beat) || !dialogue.TryGetValue(id, out var entry))
            {
                onComplete?.Invoke(false);
                return;
            }
            var variant = SelectVariant(entry, ordinal, beat);
            string text = RuntimeLine(entry, ordinal, beat);
            string speaker = variant != null && !string.IsNullOrEmpty(variant.displaySpeaker)
                ? variant.displaySpeaker
                : (string.IsNullOrEmpty(entry.displaySpeaker) ? entry.speaker : entry.displaySpeaker);
            var mode = ParsePresentationMode(variant?.presentationMode ?? entry.presentationMode);
            bool prefix = variant?.showSpeakerPrefix ?? entry.showSpeakerPrefix;
            bool italic = variant?.isItalicSilentThought ?? entry.isItalicSilentThought;
            voice?.Present(id, speaker, text, mode, spatialAnchor, speakerActor, directionalPrefix,
                onComplete, essential, beat, prefix, italic, pocketNoteText: entry.pocketNoteText);
        }

        internal bool ValidateDialogueRuntimeContract(bool throwOnError, out string report)
        {
            var issues = new List<string>();
            if (!string.Equals(dialogueDesignRevision, "3.6.2-documentation-consolidation", StringComparison.Ordinal))
                issues.Add($"designRevision='{dialogueDesignRevision}' (expected 3.6.2-documentation-consolidation)");
            if (canonicalDialogueCount != 143)
                issues.Add($"canonicalDialogueCount={canonicalDialogueCount} (expected 143)");
            if (ambientBarks == null || ambientBarks.Length != 14)
                issues.Add($"ambientBarks={ambientBarks?.Length ?? 0} (expected 14)");
            if (ch01WorldFirst == null || ch01WorldFirst.mandatoryJournalGameplay || !ch01WorldFirst.pocketNoteReadOnly || ch01WorldFirst.task02HardGate)
                issues.Add("ch01WorldFirst metadata missing or mismatched");
            if (dialogueUiSpec?.INNER_SUB_SILENT == null ||
                !string.Equals(dialogueUiSpec.INNER_SUB_SILENT.colorHex, "#F4F1E8", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(dialogueUiSpec.INNER_SUB_SILENT.scrimColorHex, "#101414", StringComparison.OrdinalIgnoreCase) ||
                Mathf.Abs(dialogueUiSpec.INNER_SUB_SILENT.scrimAlpha - 0.56f) > 0.02f)
                issues.Add("uiPresentationSpec.INNER_SUB_SILENT WCAG AA spec missing or mismatched");

            int cutToJournalCount = 0;
            int cutWithNoteCount = 0;
            foreach (var kv in dialogue)
            {
                var e = kv.Value;
                if (e == null) continue;
                if (!string.IsNullOrWhiteSpace(e.stageDirection) && !string.IsNullOrWhiteSpace(e.runtimeCopy) &&
                    e.runtimeCopy.Contains(e.stageDirection, StringComparison.Ordinal))
                    issues.Add($"stageDirection leaked into runtimeCopy for {e.id}");
                if (e.presentationMode == "CUT_TO_JOURNAL")
                {
                    cutToJournalCount++;
                    if (!string.IsNullOrWhiteSpace(e.pocketNoteText)) cutWithNoteCount++;
                }
                if (voice != null && !e.needsVO && voice.HasLoadedClipRaw(e.id))
                    issues.Add($"Forbidden non-VO WAV loaded into build for {e.id} ({e.presentationMode})");
            }
            if (cutToJournalCount < 12 || cutWithNoteCount < 12)
                issues.Add($"CUT_TO_JOURNAL pocketNoteText incomplete ({cutWithNoteCount}/{cutToJournalCount})");

            report = issues.Count == 0
                ? $"VAL-V3-01 PASS: rev={dialogueDesignRevision}, rows={canonicalDialogueCount}, barks={ambientBarks.Length}, cutNotes={cutWithNoteCount}/{cutToJournalCount}"
                : "VAL-V3-01 FAIL: " + string.Join("; ", issues);
            if (issues.Count > 0)
            {
                if (throwOnError) throw new InvalidOperationException(report);
                Debug.LogWarning("CTCC " + report);
            }
            return issues.Count == 0;
        }

        private void ValidateDialogueRuntimeContract(bool throwOnError) => ValidateDialogueRuntimeContract(throwOnError, out _);

        private string BuildPocketNoteWithCutToJournalEntries(Investigation state)
        {
            string baseNote = PocketNoteBuilder.BuildEpistemicSummary(state);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var notes = new List<string>();
            void Collect(IEnumerable<TranscriptEntry> source)
            {
                if (source == null) return;
                foreach (var t in source)
                {
                    if (string.IsNullOrEmpty(t.LineId) || !seen.Add(t.LineId)) continue;
                    if (dialogue.TryGetValue(t.LineId, out var entry) && entry.presentationMode == "CUT_TO_JOURNAL" && !string.IsNullOrWhiteSpace(t.Text))
                        notes.Add("• " + t.Text);
                }
            }
            Collect(state?.transcript);
            if (ReferenceEquals(state, State)) Collect(voice?.Transcript);
            if (notes.Count == 0) return baseNote;
            return baseNote + "\n\nGhi nhận trong không gian:\n" + string.Join("\n", notes);
        }

        private string ResolveCh01ObjectiveHud()
        {
            // Core v3.6.2: drive CH01 HUD objective from canonical Dialogue.json CH01-OBJ01–05 & CH01-OBJ06 entries.
            string obj06 = dialogue.TryGetValue("CH01-OBJ06", out var o6) && !string.IsNullOrWhiteSpace(o6.runtimeCopy)
                ? o6.runtimeCopy : "Tìm lối ra.";
            if (State.Happened("CH01-T09"))
                return obj06;

            string[] steps = null;
            if (dialogue.TryGetValue("CH01-OBJ01–05", out var o15) && !string.IsNullOrWhiteSpace(o15.runtimeCopy))
                steps = o15.runtimeCopy.Split(new[] { '→' }, StringSplitOptions.RemoveEmptyEntries);

            string StepAt(int idx, string fallback) =>
                steps != null && idx >= 0 && idx < steps.Length ? steps[idx].Trim().TrimEnd('.') + "." : fallback;

            if (!State.Happened("CH01-T02")) return StepAt(0, "Đến An Sinh.");
            if (!State.Happened("CH01-T05")) return StepAt(1, "Tìm người đã hẹn.");
            if (!GateService.G01(State)) return StepAt(2, "Kiểm tra phòng 101.");
            if (!State.Happened("CH01-T08")) return StepAt(3, "Mang phiếu về bàn trực.");
            return StepAt(4, "Thử ra cổng.");
        }

        internal DialoguePresentationMode PresentationModeOf(string id, string beat = null)
        {
            if (!dialogue.TryGetValue(id, out var entry)) return DialoguePresentationMode.VO_DIALOGUE;
            var variant = SelectVariant(entry, 1, beat);
            return ParsePresentationMode(variant?.presentationMode ?? entry.presentationMode);
        }

        private void AddDialogueAlias(string id, string groupId, int ordinal)
        {
            if (dialogue.ContainsKey(id) || !dialogue.TryGetValue(groupId, out var group) || string.IsNullOrEmpty(group.copy)) return;
            var quotes = System.Text.RegularExpressions.Regex.Matches(group.copy, "“([^”]+)”");
            if (ordinal <= quotes.Count)
                dialogue.Add(id, new DialogueEntry { id = id, chapter = group.chapter, speaker = "Minh", copy = quotes[ordinal - 1].Groups[1].Value });
        }

        private string LineAtBeat(string id, int ordinal, string beat)
        {
            if (!MayDeliver(id, beat) || !dialogue.TryGetValue(id, out var entry)) return "";
            return RuntimeLine(entry, ordinal, beat);
        }

        private Texture Image(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == "Calendar2005_Previous") return Resources.Load<Texture2D>("CTCC/Calendar2005_Previous");
            foreach (var entry in scene.inspectImages)
                if (entry.id == id) return entry.image;
            return null;
        }

        private Volume ActiveVolume() => State.layer == Reality.B ? scene.reportGrade : scene.realGrade;

        private void RefreshObjective()
        {
            string task = State.chapter == "CH01"
                ? !State.Happened(E10aParts.MailboxPickedUp) ? "Kiểm tra hộp thư sắt ngoài trụ cổng."
                : !State.Happened("CH01-T02") ? "Mở cổng phụ."
                : !State.Happened("CH01-T03") ? "Tới bàn trực phía trong sảnh."
                : !State.Happened("CH01-T04") ? "Nói chuyện với Lan tại quầy tiếp nhận."
                : !State.Happened("CH01-T05") ? "Nhận bảng kẹp phân công tại quầy."
                : !GateService.G01(State) ? "Kiểm tra Phòng 101 và xử lý phiếu."
                : !State.Happened("CH01-T08") ? "Mang phiếu về sổ trực."
                : !State.Happened("CH01-T09") ? "Thử ra cổng phụ."
                : "Tìm lối ra."
                : State.chapter == "CH03"
                ? !State.Has("E03") ? "Đi qua trạm Khu B lên kho hồ sơ tầng 3. Tìm sổ trên bàn đọc."
                : !GateService.G03(State) ? "Đối chiếu sổ bàn giao, ghi chú và túi len tại bàn đọc."
                : !State.Happened("FS19") ? "Đọc dòng C/12 và kiểm tra hộp giữ đồ."
                : "Trở xuống tầng 2, theo hành lang đến phòng trực."
                : State.chapter == "CH04"
                ? !State.Happened("PT01.presented") ? "Đọc sổ trực, bản nhận và bản chép băng họp."
                : !State.Happened("CH04-T02") ? "Quan sát lịch 3 điểm của xe thuốc (hoặc đối chiếu phiếu buồng) để qua Khu B xuống B1."
                : !ChapterFour.MorguePower(State.servicePower) && !GateService.H05(State) ? "Qua Cấp cứu xuống B1. Tìm bảng tải ở khu kỹ thuật."
                : !GateService.H05(State) ? "Đối chiếu hồ sơ tiếp nhận và phiếu thu hồi ở nhà xác."
                : !State.Happened("CH04-T06") ? "Nói chuyện với Lan ở cạnh tủ hồ sơ."
                : "Quay lại tầng 2 tới xe bệnh nhân tại sảnh Khu B."
                : State.chapter == "CH05"
                ? State.layer == Reality.M
                    ? !State.Happened(ChapterFive.EvPair1) ? "Sơ tán (0/6) · Mở lối cửa Khu B và đưa nhóm đầu tiên ra hành lang S1."
                    : !State.Happened(ChapterFive.EvPair2) ? "Sơ tán (2/6) · Dọc hành lang Tây S1, gọi đồng nghiệp tại điểm hẹn A vượt vật cản."
                    : !State.Happened(ChapterFive.EvPair3) ? "Sơ tán (4/6) · Xuống cầu thang S1 qua cửa xả sân ngoài tới điểm tập kết."
                    : "Sơ tán (6/6) · Quay lại xe bệnh nhân tại sảnh Khu B."
                : !State.Happened(ChapterFive.EvReturnR) ? "Tới xe bệnh nhân ở sảnh Khu B."
                : !State.Happened(ChapterFive.FlagOverlayAligned) ? "Xuống khu kỹ thuật tầng hầm B1 mở tủ bản vẽ và đối chiếu sơ đồ mặt bằng."
                : !State.Has(EvidenceRegistry.SealedWallPhoto) ? "Theo lối Đông tầng hầm B1 kiểm chứng mảng tường gạch xây bít năm 2004."
                : !GateService.G05(State) ? "Đối chiếu bản vẽ và ảnh chụp tường bít để mở khóa Phòng Hành chính."
                : !State.Happened(ChapterFive.EvHandoff) ? "Lên tầng 3 vào Phòng Hành chính."
                : "Đã tới ngưỡng Phòng Hành chính tầng 3."
                : State.Happened("M1-complete") ? "Hồ sơ Phòng 101 đã lưu."
                : GateService.G02(State) ? "Đến sổ giao ca ở Cấp cứu."
                : State.layer == Reality.B ? "Trở về hiện trường qua bảng kẹp Phòng 101."
                : !State.Has("E12") ? "Đối chiếu bản kê, ổ cắm và dấu bánh Phòng 101."
                : !EvidenceRegistry.BundleComplete(State) ? "Tìm và ghép các phần hồ sơ trong kho 101."
                : "Đối chiếu hồ sơ tại bảng kẹp Phòng 101.";

            if (task != lastObjectiveKey)
            {
                lastObjectiveKey = task;
                objectiveStagnantSeconds = 0f;
                hintTierShown = 0;
            }
            string layerLabel = State.layer == Reality.M ? "ĐÊM 18.11.2005  ·  KÝ ỨC"
                : State.layer == Reality.B ? "ĐÊM 18.11.2005  ·  BÀN GIAO" : "17.11.2012  ·  AN SINH";
            string hudLine = State.chapter == "CH01" ? ResolveCh01ObjectiveHud() : task;
            ui.Hud(layerLabel, hudLine);
        }
    }
}
