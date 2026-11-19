using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public sealed partial class HospitalGame : MonoBehaviour
    {
        [Serializable] private sealed class DialogueEntry { public string id, chapter, speaker, context, copy; }
        [Serializable] private sealed class DialogueSheet { public DialogueEntry[] entries; }
        private readonly Dictionary<string,DialogueEntry> dialogue=new();
        private HospitalScene scene;
        private FirstPerson player;
        private HospitalUI ui;
        private RealityTransaction transaction;
        private Casework casework;
        private AtomicSaves saves;
        private bool live, busy, firstEscape;
        private string qaFolder;
        private Investigation State => casework.State;
        private VisitSettings Options => State.settings;
        private string UseKey => Options.alternateBindings ? "K" : "E";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            var world=FindFirstObjectByType<HospitalScene>();
            if (world!=null) world.gameObject.AddComponent<HospitalGame>();
        }

        private void Awake()
        {
            scene=GetComponent<HospitalScene>();
            var lineSheet=Resources.Load<TextAsset>("CTCC/Dialogue");
            foreach (var entry in JsonUtility.FromJson<DialogueSheet>(lineSheet.text).entries)
                dialogue.Add(entry.id,entry);
            var rig=new GameObject("FPS character",typeof(CharacterController),typeof(FirstPerson));
            player=rig.GetComponent<FirstPerson>();
            player.Initialize(Camera.main,scene.atmosphere);
            player.Teleport(scene.Pad("AP00"));
            ui=gameObject.AddComponent<HospitalUI>();
            ui.Initialize();
            ui.OpenChanged+=RefreshControls;
            qaFolder=Argument("-ctcc-qa");
            var saveDirectory=qaFolder==null ? Argument("-ctcc-save")??Path.Combine(Application.persistentDataPath,"Patient101")
                : Path.Combine(Path.GetFullPath(qaFolder),"isolated-save");
            saves=new AtomicSaves(saveDirectory);
            transaction=new RealityTransaction(scene,saves);
            transaction.ReadCharge=()=>player.BatterySeconds;
            casework=new Casework(transaction);
            transaction.Committed+=ApplyCommitted;
            transaction.Present(State);
            ApplyCommitted(State);
        }

        private void Start()
        {
            if (qaFolder!=null)
            {
                StartCoroutine(AutomatedVisit());
                return;
            }
            MainMenu();
        }

        private static string Argument(string label)
        {
            var args=Environment.GetCommandLineArgs();
            int i=Array.IndexOf(args,label);
            return i>=0 && i+1<args.Length ? args[i+1] : null;
        }

        private void ApplyCommitted(Investigation current)
        {
            player.Apply(current.settings);
            ui.Apply(current.settings);
            scene.atmosphere?.Apply(current.settings);
            if (scene.motion!=null) scene.motion.reducedMotion=current.settings.reduceMotion;
            if (scene.realGrade!=null && scene.realGrade.sharedProfile.TryGet<ColorAdjustments>(out var real))
                real.postExposure.value=.49f+current.settings.visibility*.62f;
            if (scene.reportGrade!=null && scene.reportGrade.sharedProfile.TryGet<ColorAdjustments>(out var report))
                report.postExposure.value=.08f+current.settings.visibility*.50f;
            RefreshObjective();
        }

        private void RefreshControls()
        {
            bool play=live && !ui.IsModal && !busy && qaFolder==null;
            player.CanPlay=play;
            if (qaFolder!=null) return;
            Cursor.lockState=play?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=!play;
        }

        private void Update()
        {
            if (!live || busy || ui.IsModal || qaFolder!=null)
                return;
            var keys=Keyboard.current;
            if(keys==null) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (firstEscape) firstEscape=false;
                else Pause();
                return;
            }
            firstEscape=false;
            if (keys[Options.alternateBindings?Key.J:Key.Tab].wasPressedThisFrame)
            {
                Journal();
                return;
            }
            Vector3 feet=player.Feet;
            if (State.chapter=="CH01" && State.Happened("CH01-T08") && !State.Happened("CH01-T09") &&
                feet.z<-20.26f && feet.z>-21.3f && Mathf.Abs(feet.x-4)<1.18f)
            {
                Transition(()=>casework.OutsideAttempt(player.Feet),true,()=>ui.Caption(Line("CH01-D13",2),4,true));
                return;
            }
            if (State.chapter=="CH01" && transaction.OnPad(feet,"AP02") && NeedsHub())
            {
                Hub();
                return;
            }
            if (State.chapter=="CH01" && State.Happened("CH01-T05") && !State.Happened("CH01-T06") &&
                feet.x>5.82f && feet.z>2.01f && feet.z<7.99f)
                casework.EnterRoom(feet);
            if(State.chapter=="CH02" && State.layer==Reality.R && !State.Happened("CH02-T02") &&
               feet.z>1.25f && feet.z<7.9f && feet.x>2.55f && feet.x<6f &&
               casework.Record("CH02-T02",feet)==CommitResult.Applied)
            {
                scene.atmosphere?.From("slippers",new Vector3(10.3f,.1f,4.74f));
                ui.Caption("[Tiếng dép · phía phòng 101]",4);
            }
            bool use=keys[Options.alternateBindings?Key.K:Key.E].wasPressedThisFrame;
            if (transaction.OnPad(feet,"AP04") && State.chapter=="CH02" && keys.qKey.wasPressedThisFrame)
            {
                Anchor();
                return;
            }
            var target=player.Focus(Available);
            ui.Prompt(target==null
                ? (transaction.OnPad(feet,"AP04") && State.chapter=="CH02" ? "Q   Điểm neo 101" : null)
                : UseKey+"   "+target.title);
            if (use && target!=null)
                Use(target.id);
            else if (use && transaction.OnPad(feet,"AP04") && casework.CanRevise)
                Anchor();
        }

        private bool NeedsHub() => !State.Happened("CH01-T03") && State.Happened("CH01-T02") ||
            GateService.G01(State) && !State.Happened("CH01-T08") ||
            State.Happened("CH01-T09") && !State.Happened("CH01-T10");

        private bool Available(SceneTarget target) => target.id switch
        {
            "GATE-CONG"=>!State.Happened("CH01-T02"),
            "SEAM"=>State.Happened("CH01-T08"),
            "BOARD"=>State.layer==Reality.B && State.chapter=="CH01" && !State.Happened("CH01-T05"),
            "F101" or "AN02"=>casework.CanRevise,
            "LIST" or "SOCKETS" or "WHEELS" or "PZ01"=>State.chapter=="CH02" && State.layer==Reality.R,
            "EXIT"=>GateService.G02(State),
            _=>true
        };

        private void Use(string id)
        {
            switch (id)
            {
                case "GATE-CONG":
                    if (!State.Happened("E10a.SMSRead")) Letter();
                    else if (Report(casework.OpenGate(player.Feet))) ui.Caption("Cổng phụ đã mở.");
                    break;
                case "SEAM":
                    if(!State.Happened("CH01-T09"))
                        Transition(()=>casework.OutsideAttempt(player.Feet),true,()=>ui.Caption("Minh · "+Line("CH01-D13",2),4,true));
                    else ui.Caption("Không tìm được đường ra. Trở lại sổ trực.",5,true);
                    break;
                case "E10a": Letter(); break;
                case "BOARD":
                    if(Report(casework.Board(player.Feet))) ui.Caption("Đã lấy phiếu bàn giao. Tới phòng 101.",5,true);
                    break;
                case "AN01":
                    if(NeedsHub() && State.chapter=="CH01") Hub();
                    else Journal();
                    break;
                case "AN02": Anchor(); break;
                case "F101": Form(); break;
                case "OBS101A":
                    Observation(id,"Dấu sử dụng giường thứ tư","Có vết bánh cạnh rèm và dấu đồ dùng đã được dời đi. Ổ cắm số 4 vẫn gắn trên tường.");
                    break;
                case "OBS101B":
                    Observation(id,"Thẻ bàn giao giường 4","18/11/2005 · Phòng 101\n\nGiường 4 · CHƯA BÀN GIAO\nKiểm tra lại.\n\nThẻ không ghi tên hoặc nơi chuyển.",Image("ObsCard"));
                    break;
                case "LIST":
                    Observation(id,"Bản kê phòng bệnh","Bản kê cũ có ba dòng, đánh số 1 đến 3. Tổng số ghi trên bản kê là 03.",Image("BedList"));
                    break;
                case "SOCKETS":
                    Observation(id,"Bốn ổ cắm","Ba giường ở chỗ kê. Ổ cắm số 4 vẫn có nhãn trên tường.");
                    break;
                case "WHEELS":
                    Observation(id,"Dấu bánh tại hốc 4","Bốn vết bánh tạo thành một hình chữ nhật. Một đường kéo mờ dừng trước tủ kho.");
                    break;
                case "PZ01": PhysicalContradiction(); break;
                case "DRAW1": Take(EvidenceRegistry.DrawingFragments[0],"Mảnh tranh 1","Nét mái ngói dừng ở mép giấy.",Image("DrawingPiece1")); break;
                case "DRAW2": Take(EvidenceRegistry.DrawingFragments[1],"Mảnh tranh 2","Nét cửa và khóa bằng chì sẫm màu.",Image("DrawingPiece2")); break;
                case "DRAW3": Take(EvidenceRegistry.DrawingFragments[2],"Mảnh tranh 3","Mặt trời và một nửa ô cửa sổ.",Image("DrawingPiece3")); break;
                case "WRIST": Take(EvidenceRegistry.Wristband,"Vòng cũ","Phiến giấy trong vòng ghi C01. Vòng được giữ trong túi đồ 101."); break;
                case "TRANSFER":
                    Take(EvidenceRegistry.Transfer,"Phiếu chuyển","17/11/2005 · An\nGiường 4 phòng 101, khám sốt tạm. Trở lại C01.\n\nBản lưu nội bộ từ bàn trực, trong tủ khô kho 101.",Image("Transfer"));
                    break;
                case "G02":
                    ui.Caption(GateService.G02(State) ? "Cửa cấp cứu đã mở." : "Cấp cứu tạm đóng. Hãy kiểm tra phòng 101.",5,true);
                    break;
                case "EXIT":
                    if (Report(casework.Complete(player.Feet))) EndOfVisit();
                    break;
            }
        }

        private void Hub()
        {
            if(!transaction.OnPad(player.Feet,"AP02"))
            {
                ui.Caption("Đứng sát vạch đồng trước bàn trực.",4,true);
                return;
            }
            bool entering=!State.Happened("CH01-T03");
            Transition(()=>casework.AtHub(player.Feet,out _),true,()=>
            {
                if(entering) LanTranscript();
                else if(State.chapter=="CH02") ui.Caption("Ca trực chưa kết thúc. Tìm lối ra.",5,true);
                else ui.Caption("Minh · "+Line("CH01-D13",1),4);
            });
        }

        private void Anchor()
        {
            if(State.chapter=="CH01") { Form(); return; }
            var choices=new List<ActionChoice>
            {
                new(State.layer==Reality.R?"Bước vào B · Bản báo cáo":"Trở về R · Hiện trường",()=>
                {
                    ui.Close();
                    Transition(()=>casework.SwitchIn101(player.Feet),true);
                }),
                new("Đối chiếu phiếu F101",Form),
                new("Ghép tranh và kiểm hồ sơ",Identify),
                new("Quay lại phòng",CloseModal)
            };
            ui.Show("Phòng 101","Điểm neo","Vạch đồng trước cửa giữ cùng vị trí trong cả hai lớp.\n\n"+
                "R giữ dấu vết vật lý của năm 2012. B trình bày căn phòng theo hồ sơ bàn giao.",
                choices.ToArray(),CloseModal);
        }

        private void Transition(Func<CommitResult> apply,bool warp,Action after=null)
        {
            if (busy) return;
            StartCoroutine(WhileTransition(apply,warp,after));
        }

        private IEnumerator WhileTransition(Func<CommitResult> apply,bool warp,Action after)
        {
            busy=true;
            ui.Close();
            RefreshControls();
            float seconds=Options.reduceMotion?.05f:.32f;
            for(float t=0;t<seconds;t+=Time.unscaledDeltaTime)
            {
                ui.Fade(Mathf.Clamp01(t/seconds));
                yield return null;
            }
            ui.Fade(1);
            var outcome=apply();
            if (outcome==CommitResult.Applied)
            {
                if(warp) player.Teleport(scene.Pad(SafeCheckpoints.Pad(State.checkpoint)));
                scene.atmosphere?.From("switch",player.Feet+Vector3.up);
            }
            for(float t=0;t<seconds;t+=Time.unscaledDeltaTime)
            {
                ui.Fade(1-Mathf.Clamp01(t/seconds));
                yield return null;
            }
            ui.Fade(0);
            busy=false;
            RefreshControls();
            if (Report(outcome)) after?.Invoke();
        }

        private bool Report(CommitResult result)
        {
            if (result==CommitResult.Applied) return true;
            string copy=result switch
            {
                CommitResult.PadRequired=>"Còn thiếu điều kiện. Kiểm tra vật chứng hoặc trở lại vạch neo.",
                CommitResult.Obstructed=>"Vị trí chuyển lớp đang bị chắn. Hồ sơ được giữ trước lần thử.",
                _=>"Chưa ghi được bản lưu. Ca trực vẫn ở mốc trước thao tác."
            };
            ui.Caption(copy,5,true);
            return false;
        }

        private string Line(string id,int ordinal=1)
        {
            if(!dialogue.TryGetValue(id,out var entry) || string.IsNullOrWhiteSpace(entry.copy)) return "";
            var quotes=System.Text.RegularExpressions.Regex.Matches(entry.copy,"“([^”]+)”");
            return ordinal<=quotes.Count ? quotes[ordinal-1].Groups[1].Value : entry.copy;
        }

        private Texture Image(string id)
        {
            foreach(var entry in scene.inspectImages)
                if(entry.id==id) return entry.image;
            return null;
        }

        private void RefreshObjective()
        {
            string task=State.chapter=="CH01"
                ? !State.Happened("E10a.SMSRead") ? "Đọc bì thư và tin nhắn của Bảy."
                : !State.Happened("CH01-T02") ? "Mở cổng phụ."
                : !State.Happened("CH01-T03") ? "Tới bàn trực phía trong sảnh."
                : !State.Happened("CH01-T05") ? "Lấy phiếu bàn giao tại quầy."
                : !GateService.G01(State) ? "Kiểm tra phòng 101 và xử lý phiếu."
                : !State.Happened("CH01-T08") ? "Mang phiếu về sổ trực."
                : !State.Happened("CH01-T09") ? "Thử ra cổng phụ."
                : "Trở lại sổ trực."
                : State.Happened("M1-complete") ? "Hồ sơ 101 đã lưu."
                : GateService.G02(State) ? "Đến sổ giao ca ở cấp cứu."
                : State.layer==Reality.B ? "Trở về hiện trường R tại điểm neo 101."
                : !State.Has("E12") ? "Đối chiếu bản kê, ổ cắm và dấu bánh phòng 101."
                : !EvidenceRegistry.BundleComplete(State) ? "Tìm và ghép các phần hồ sơ trong kho 101."
                : "Về điểm neo 101 để xác thực hồ sơ.";
            ui.Hud(State.layer==Reality.R ? "R  ·  HIỆN TRƯỜNG 2012" : "B  ·  BẢN BÁO CÁO",task);
        }
    }
}
