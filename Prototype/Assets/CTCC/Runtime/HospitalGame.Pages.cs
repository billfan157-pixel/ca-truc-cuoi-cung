using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private VisitSettings startOptions = new();

        private void MainMenu()
        {
            live=false;
            var choices=new List<ActionChoice>();
            SaveRead automatic=transaction.Inspect(SaveSlot.Auto);
            SaveRead manual=transaction.Inspect(SaveSlot.Manual);
            if(automatic==SaveRead.Valid || automatic==SaveRead.Backup)
                choices.Add(new ActionChoice("Tiếp tục từ tự lưu",()=>Load(SaveSlot.Auto)));
            if(manual==SaveRead.Valid || manual==SaveRead.Backup)
                choices.Add(new ActionChoice("Mở bản lưu tay",()=>Load(SaveSlot.Manual)));
            choices.Add(new ActionChoice("Ca trực mới",()=>
            {
                if (automatic==SaveRead.Empty) Begin();
                else ui.Show("Bắt đầu lại","Ca trực mới","Bắt đầu một lượt mới sẽ thay mốc tự lưu của lượt hiện tại.",
                    new[]{new ActionChoice("Quay lại",MainMenu),new ActionChoice("Bắt đầu",Begin)},MainMenu);
            }));
            choices.Add(new ActionChoice("Cài đặt",()=>Settings(MainMenu)));
            choices.Add(new ActionChoice("Thoát",()=>Application.Quit()));
            string warning=automatic==SaveRead.Unsupported ? "\n\nBản tự lưu được tạo bằng phiên bản khác."
                : automatic==SaveRead.Damaged ? "\n\nKhông đọc được bản tự lưu. Có thể mở bản lưu tay nếu còn." : "";
            ui.Show("BỆNH VIỆN AN SINH  /  17.11.2012","Ca trực cuối cùng",
                "Phòng 101\n\nMinh trở lại An Sinh trong ca trông coi cuối cùng trước ngày tháo dỡ."+warning,
                choices.ToArray(),null);
        }

        private void Begin()
        {
            if (!Report(casework.Begin(player.Feet,startOptions))) return;
            live=true;
            player.Teleport(scene.Pad("AP00"));
            player.RestoreBattery(State.torchSeconds);
            Letter();
        }

        private void Load(SaveSlot slot)
        {
            SaveRead result=transaction.Restore(slot,out var safe);
            if(safe==null)
            {
                ui.Show("Bản lưu","Không thể mở",result==SaveRead.Unsupported
                    ? "Bản lưu thuộc phiên bản khác." : "Không tìm thấy một mốc lưu có thể khôi phục.",
                    new[]{new ActionChoice("Quay lại",live?Pause:MainMenu)},live?Pause:MainMenu);
                return;
            }
            live=true;
            player.Teleport(safe);
            player.RestoreBattery(State.torchSeconds);
            CloseModal();
            if(result==SaveRead.Backup)
                ui.Caption("Đã mở bản lưu dự phòng.",5,true);
        }

        private void Pause()
        {
            var manual=transaction.Inspect(SaveSlot.Manual);
            var automatic=transaction.Inspect(SaveSlot.Auto);
            ui.Show(State.chapter=="CH01"?"NGƯỜI TRỞ VỀ":"PHÒNG BỆNH TRỐNG","Tạm dừng",
                transaction.OnPad(player.Feet,"AP02") ? "Có thể lưu tay tại sổ trực." : "Lưu tay khả dụng tại sổ trực ở lễ tân.",
                new[]
                {
                    new ActionChoice("Tiếp tục",CloseModal),
                    new ActionChoice("Lưu tay",()=>{ if(transaction.KeepManual()){ CloseModal(); ui.Caption("Đã lưu ca trực.",4,true); } },
                        transaction.OnPad(player.Feet,"AP02")),
                    new ActionChoice("Tải điểm lưu tự động",()=>Load(SaveSlot.Auto),automatic==SaveRead.Valid || automatic==SaveRead.Backup),
                    new ActionChoice("Mở bản lưu tay",()=>Load(SaveSlot.Manual),manual==SaveRead.Valid || manual==SaveRead.Backup),
                    new ActionChoice("Cài đặt",()=>Settings(Pause)),
                    new ActionChoice("Về màn hình đầu",MainMenu)
                },CloseModal);
        }

        private void Settings(Action back)
        {
            VisitSettings current=live?Options:startOptions;
            void Change(Action<VisitSettings> edit)
            {
                var next=JsonUtility.FromJson<VisitSettings>(JsonUtility.ToJson(current));
                edit(next);
                if(live)
                {
                    if (!Report(casework.UpdateSettings(next,player.Feet))) return;
                }
                else
                {
                    startOptions=next;
                    ui.Apply(next);
                    player.Apply(next);
                    scene.atmosphere?.Apply(next);
                }
                Settings(back);
            }
            ui.Show("TRỢ NĂNG","Cài đặt","Tab hoặc phím mũi tên để di chuyển giữa các nút. Enter để chọn. Esc để quay lại.",
                new[]
                {
                    new ActionChoice(current.reduceMotion?"Chuyển động · Giảm":"Chuyển động · Đầy đủ",()=>Change(x=>x.reduceMotion=!x.reduceMotion)),
                    new ActionChoice(current.captions?"Chú thích âm thanh · Bật":"Chú thích âm thanh · Tắt",()=>Change(x=>x.captions=!x.captions)),
                    new ActionChoice(current.volume>0?"Âm lượng · 72%":"Âm lượng · Tắt",()=>Change(x=>x.volume=x.volume>0?0:.72f)),
                    new ActionChoice(current.monoAudio?"Âm thanh · Mono":"Âm thanh · Không gian",()=>Change(x=>x.monoAudio=!x.monoAudio)),
                    new ActionChoice("Cỡ chữ · "+Mathf.RoundToInt(current.textScale*100)+"%",()=>Change(x=>x.textScale=x.textScale>=1.30f?1:1.30f)),
                    new ActionChoice(current.visibility>.1f?"Độ sáng · Tăng":"Độ sáng · Mặc định",()=>Change(x=>x.visibility=x.visibility>.1f?0:1)),
                    new ActionChoice(current.alternateBindings?"Phím · Mũi tên / K / J":"Phím · WASD / E / Tab",()=>Change(x=>x.alternateBindings=!x.alternateBindings)),
                    new ActionChoice("Góc nhìn · "+current.fov+"°",()=>Change(x=>x.fov=x.fov>=78?62:x.fov+8)),
                    new ActionChoice("Quay lại",back)
                },back);
        }

        private void Letter()
        {
            string body="Giấy hẹn có chữ ký Bảy\n23 giờ, 17/11/2012 · Chốt cổng phụ. Tôi trực ca cuối.\n\n"+
                "Tin nhắn mới\n"+Line("CH01-D15")+"\n\n"+
                "Thư cũ của Lan\nAnh Minh, em cần nói với anh một chuyện ở bệnh viện. Em không muốn để chuyện này qua thêm một ngày nữa.\n\n"+
                "Mẩu báo cũ\nVụ cháy 18/11/2005 ở An Sinh có 13 người tử vong. Minh đã đến đám tang Lan.\n\n"+
                "Trong bì thư có chìa cổng phụ và chìa khu C đã thay khóa.";
            ui.Show("THƯ · TỜ HẸN · TIN NHẮN","Bì thư của Bảy",body,
                new[]{new ActionChoice("Ghi tin nhắn vào sổ trực",()=>
                {
                    if (Report(casework.ReadLetter(player.Feet)))
                    {
                        CloseModal();
                        ui.Caption("Minh · "+Line("CH01-D01"),4,true);
                    }
                })},CloseModal);
        }

        private void LanTranscript()
        {
            ui.Show("BẢN GHI CUỘC GẶP","Ở bàn trực",
                "Lan  —  "+Line("CH01-D03")+"\n\nMinh  —  "+Line("CH01-D04")+
                "\n\nLan  —  "+Line("CH01-D05")+"\n\nLan  —  "+Line("CH01-D07")+
                "\n\nMinh vẫn nhớ lần anh đã từ chối mở cửa. Anh chưa biết chuyện gì xảy ra sau đó.",
                new[]{new ActionChoice("Xem bảng kẹp",CloseModal),
                      new ActionChoice("Đọc đối thoại về ca cũ",()=>ui.Show("BẢN GHI CUỘC GẶP","Về ca cũ",
                        "Lan  —  "+Line("CH01-D16"),new[]{new ActionChoice("Khép sổ",CloseModal)},CloseModal))},CloseModal);
        }

        private void Observation(string id,string title,string copy,Texture photo=null)
        {
            var outcome=casework.Observe(id,player.Feet);
            if (!Report(outcome)) return;
            scene.atmosphere?.From("paper",player.Feet+player.transform.forward*.6f+Vector3.up);
            ui.Show(State.layer==Reality.B?"QUAN SÁT TỪ B":"QUAN SÁT TẠI HIỆN TRƯỜNG",title,copy,
                new[]{new ActionChoice("Khép và trở lại",CloseModal)},CloseModal,photo);
        }

        private void Take(string id,string title,string copy,Texture photo=null)
        {
            ui.Show("KHO GIƯỜNG 101  /  HIỆN TRƯỜNG R",title,copy,
                new[]{new ActionChoice(State.Has(id)?"Đã ghi vào sổ":"Ghi vật chứng",()=>
                {
                    if(Report(casework.Take(id,player.Feet))) { CloseModal(); ui.Caption("Đã ghi vào sổ trực.",3,true); }
                }),new ActionChoice("Quay lại",CloseModal)},CloseModal,photo);
        }

        private void Form()
        {
            if(!casework.CanRevise) { ui.Caption("Phiếu có thể đối chiếu lại tại điểm neo phòng 101.",4,true); return; }
            var record=State.f101;
            string obs=FormService.RefutationAvailable(State)?"Hai quan sát đã có trong sổ.":"Cần xem cả dấu sử dụng và thẻ bàn giao nếu muốn phản chứng.";
            string pad=transaction.OnPad(player.Feet,"AP04")?"":"\n\nĐứng trên vạch đồng cạnh cửa trước khi xác nhận.";
            ui.Show("SỔ BÀN GIAO  /  18.11.2005","Phiếu phòng 101","Số chưa bàn giao theo bảng là 0.\n\n"+
                "Hiện tại · "+Status(record.current)+"\nLần ghi · "+record.revision+"\n\n"+obs+pad,
                new[]{
                    new ActionChoice("A    Xác nhận theo bảng",()=>PreviewForm(FormStatus.Affirmed,false)),
                    new ActionChoice("B    Chưa đủ thông tin",()=>PreviewForm(FormStatus.Unverified,false)),
                    new ActionChoice("C    Đối chiếu dữ liệu bất nhất",()=>MatchObservations(false,false),FormService.RefutationAvailable(State)),
                    new ActionChoice("Đóng phiếu",CloseModal)
                },CloseModal,Image("F101"));
        }

        private void MatchObservations(bool a,bool b)
        {
            ui.Show("PHẢN CHỨNG  /  F101","Chọn cặp quan sát",
                "Chọn hai quan sát liên hệ tới cùng chỗ kê giường. Các chi tiết cho biết còn mục chưa khép bàn giao.",
                new[]{
                    new ActionChoice((a?"✓    ":"□    ")+"Dấu sử dụng ở vị trí thứ tư",()=>MatchObservations(!a,b)),
                    new ActionChoice((b?"✓    ":"□    ")+"Thẻ giường 4 ghi chưa bàn giao",()=>MatchObservations(a,!b)),
                    new ActionChoice("Kiểm tra nội dung ký",()=>PreviewForm(FormStatus.Refuted,true),a&&b),
                    new ActionChoice("Quay lại phiếu",Form)
                },Form);
        }

        private void PreviewForm(FormStatus choice,bool paired)
        {
            ui.Show("F101  /  N. H. MINH","Kiểm tra trước khi ký",
                (choice==FormStatus.Affirmed ? "Xác nhận ca còn 0 bệnh nhân chưa bàn giao theo bản kê."
                    : choice==FormStatus.Unverified ? "Để ngỏ số bàn giao. Ghi rõ chưa đủ căn cứ."
                    : "Ghi nhận dữ liệu bất nhất. Giường 4 còn mục chưa bàn giao.")+
                "\n\nNgười ghi · N. H. Minh\nLựa chọn đầu tiên được lưu trong lịch sử sửa phiếu.",
                new[]{new ActionChoice("Quay lại",Form),new ActionChoice("Xác nhận lựa chọn",()=>
                {
                    bool revised=State.f101.current!=choice;
                    ui.Close();
                    Transition(()=>casework.Submit(choice,paired,player.Feet),false,()=>
                    {
                        if (!revised) return;
                        scene.atmosphere?.From("stamp",player.Feet+Vector3.up);
                        string cue=State.layer==Reality.B ?
                            "Loa hành lang · "+Line(choice==FormStatus.Affirmed?"CH01-PA01":choice==FormStatus.Unverified?"CH01-PA02":"CH01-PA03") :
                            "Đã lưu phiếu · "+Status(choice);
                        ui.Caption(cue,6,true);
                    });
                })},Form,Image("F101"));
        }

        private void PhysicalContradiction()
        {
            var missing=casework.MissingPhysicalObservations();
            if(State.Has("E12"))
            {
                ui.Caption("Tủ đã dịch. Kiểm tra hồ sơ trong kho.",4,true);
                return;
            }
            ui.Show("GIƯỜNG THỨ TƯ  /  E12","Đặt bản kê cạnh dấu vết",
                missing??"Bản kê có ba dòng. Bốn ổ cắm và dấu bánh có vị trí thứ tư. Chọn cách đọc và giữ lại dấu vết trong sổ.",
                new[]{
                    new ActionChoice("Có một chỗ kê chưa ghi trong bản kê",()=>
                    {
                        CloseModal();
                        if(Report(casework.SolvePz01(player.Feet))) ui.Caption("Đã ghi E12. Tủ đã được đẩy khỏi cửa kho.",5,true);
                    },missing==null),
                    new ActionChoice("Quay lại kiểm tra",CloseModal)
                },CloseModal,Image("BedList"));
        }

        private void Identify()
        {
            string missing=!State.Has("E12")?"Đối chiếu các dấu vật lý để tiếp cận kho 101."
                : !EvidenceRegistry.HaveFragments(State)?"Tìm đủ ba mảnh ở tủ khô trong kho."
                : !State.Has(EvidenceRegistry.Drawing)?"Ghép ba mép giấy trước khi đối chiếu."
                : !State.Has(EvidenceRegistry.Wristband)?"Còn thiếu vòng cũ có mã."
                : !State.Has(EvidenceRegistry.Transfer)?"Còn thiếu phiếu chuyển từ bàn trực."
                : State.layer!=Reality.R?"Trở về R để xác thực vật chứng."
                : null;
            var choices=new List<ActionChoice>();
            if (EvidenceRegistry.HaveFragments(State) && !State.Has(EvidenceRegistry.Drawing))
                choices.Add(new ActionChoice("Ghép tranh theo đường mép",()=>{
                    if(Report(casework.Take(EvidenceRegistry.Drawing,player.Feet))) Identify();
                },State.layer==Reality.R));
            if (missing==null && !GateService.G02(State))
                choices.Add(new ActionChoice("Đối chiếu An · 101/4 → C01",()=>
                {
                    CloseModal();
                    if(Report(casework.VerifyAn(player.Feet))) ui.Caption("Minh · "+Line("CH02-D04")+" Cửa cấp cứu đã mở.",6,true);
                }));
            choices.Add(new ActionChoice("Trở lại điểm neo",Anchor));
            ui.Show("HỒ SƠ TRONG KHO 101","Bức tranh của An",missing??
                "Tranh ghi lại một cánh cửa. Vòng cũ ghi C01. Phiếu chuyển ghi An, khám sốt tạm ở 101/4 ngày 17/11/2005, rồi trở lại C01.",
                choices.ToArray(),Anchor,State.Has(EvidenceRegistry.Drawing)?Image("Drawing"):null);
        }

        private void Journal()
        {
            var r=State.f101;
            int fragments=0;
            foreach(var id in EvidenceRegistry.DrawingFragments) if(State.Has(id)) fragments++;
            string status="F101 · "+Status(r.current)+"\nLần ghi "+r.revision+" · Lần đầu "+Status(r.firstDecision)+
                "\n\nDấu sử dụng · "+(State.Observed("OBS101A")?"Đã xem":"Chưa xem")+
                "    Thẻ 4 · "+(State.Observed("OBS101B")?"Đã xem":"Chưa xem");
            if(State.chapter=="CH02")
                status+="\n\nHiện trường · "+(State.Has("E12")?"Đã đối chiếu E12":"Chưa đối chiếu chỗ kê")+
                    "\nTranh "+fragments+"/3 · Vòng "+(State.Has(EvidenceRegistry.Wristband)?"đã ghi":"chưa có")+
                    " · Phiếu chuyển "+(State.Has(EvidenceRegistry.Transfer)?"đã ghi":"chưa có")+
                    "\nHồ sơ "+(GateService.G02(State)?"C01 đã xác thực":"còn mở");
            var choices=new List<ActionChoice>{new("Đọc thư và tin nhắn",Letter)};
            if(State.Happened("CH01-T04")) choices.Add(new ActionChoice("Bản ghi ở bàn trực",LanTranscript));
            if(casework.CanRevise) choices.Add(new ActionChoice("Xem phiếu 101",Form));
            if(State.chapter=="CH02") choices.Add(new ActionChoice("Xem hồ sơ trong kho",Identify));
            choices.Add(new ActionChoice("Khép sổ",CloseModal));
            ui.Show("NHẬT KÝ ĐIỀU TRA",State.chapter=="CH01"?"Người trở về":"Phòng bệnh trống",status,
                choices.ToArray(),CloseModal);
        }

        private void EndOfVisit()
        {
            ui.Show("PHÒNG 101  /  BẢN LƯU ĐÃ GHI","Hồ sơ C01",
                "Đã xác thực An từ bản lưu 101/4, vòng và phiếu chuyển. Vị trí tìm thấy An vẫn để ngỏ.\n\n"+
                "Phiếu 101 và lịch sử sửa phiếu được giữ trong sổ trực.",
                new[]{new ActionChoice("Ở lại khám phá",CloseModal),new ActionChoice("Về màn hình đầu",MainMenu)},CloseModal);
        }

        private void CloseModal()
        {
            firstEscape=true;
            ui.Close();
        }

        private static string Status(FormStatus status) => status switch
        {
            FormStatus.Affirmed=>"Đã xác nhận 0",
            FormStatus.Unverified=>"Chờ kiểm tra",
            FormStatus.Refuted=>"Có sai lệch",
            _=>"Chưa ghi"
        };
    }
}
