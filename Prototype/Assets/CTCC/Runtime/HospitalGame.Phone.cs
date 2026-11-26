using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Core v3.6.2 (DEC-V36-CH01-01 / DEC-V361-CH01-01) B01 Phone, Gate Mailbox & Ch01JournalPolicy controller.
    /// Enforces:
    /// - Epistemic SMS rule (4.2): passive arrival badge on spawn never auto-marks postponementSMS; actively viewing
    ///   the 22:48 SMS commits E10a.Part.postponementSMS (and alias E10a.SMSRead) truthfully.
    /// - Atomic mailbox pickup: grants E10a + envelope + keys (KEY_GATE equipped in hand) with 0/4 parts read.
    /// - Ch01JournalPolicy (Phase 1): locks unnecessary Journal 5-tab entry points in B01-B03 and keeps Pocket Note read-only.
    /// </summary>
    public sealed partial class HospitalGame
    {
        private bool pendingCh01D01AfterPhone;
        private int phoneActiveTab; // 0 = Inbox / SMS 22:48, 1 = SMS Appointment 16/11, 2 = Call Bảy

        /// <summary>
        /// Phase 1 Ch01JournalPolicy:
        /// - Completely disables full 5-tab Journal in CH01 during B01-B03 (and on AN01 / TranscriptPage everywhere in CH01).
        /// - Phase 2 will lock 100% of CH01 once 3D AN02 F101 & HH:MM EX01 replace the legacy B04-B05 notebook screens.
        /// - CH02-CH08 keep full 5-tab Journal intact.
        /// </summary>
        internal bool IsJournalAllowedOnAn01 => State != null && State.chapter != "CH01";
        internal bool IsJournalAllowedInBackpack =>
            State != null && (State.chapter != "CH01" || (State.Happened("CH01-T05") && !State.Happened("CH01-T08")));

        private void OpenAn01ReadOnlyInCh01()
        {
            var manual = transaction.Inspect(SaveSlot.Manual);
            var choices = new List<ActionChoice>
            {
                new("Lưu tay tại sổ trực", () =>
                {
                    if (transaction.KeepManual())
                    {
                        CloseModal();
                        ui.Caption("Đã lưu ca trực tại sổ trực lễ tân.", 4f, true);
                    }
                }, transaction.AtRequiredPlace(player.Feet, RealityTransaction.ReceptionDesk)),
                new("Xem Pocket Note (chỉ đọc lại)", () => OpenBackpack(BackpackCompartment.PocketNote)),
                new("Đóng", CloseModal)
            };
            ui.Show("SỔ TRỰC LỄ TÂN  ·  AN01", "Sổ trực quầy tiếp nhận",
                "Sổ trực giấy bìa cứng đặt trên mặt quầy tiếp nhận. Có thể ghi mốc lưu tay tại đây hoặc mở Pocket Note đọc lại những gì đã quan sát.",
                choices.ToArray(), CloseModal);
        }

        private void OpenPhone(int? tabOverride = null, bool returnToBackpack = false)
        {
            if (State == null || busy) return;
            if (tabOverride.HasValue)
                phoneActiveTab = Mathf.Clamp(tabOverride.Value, 0, 2);

            bool largePrint = qaFolder != null || inspector == null;
            bool openedAudio = false;

            string smsApptCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.smsAppointmentPropCopy)
                ? ch01WorldFirst.smsAppointmentPropCopy
                : "Minh à, dọn kho thanh lý tôi tìm thấy lá thư cũ cô Lan viết cho cậu hồi 2005. 11 giờ tối mai cậu ghé cổng phụ An Sinh lấy trước khi họ san phẳng. Nếu tôi chưa ra kịp thì chìa cổng và phong bì tôi để trong hộp thư cũ cạnh trụ cổng.";
            string smsPostponeCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.smsPostponementPropCopy)
                ? ch01WorldFirst.smsPostponementPropCopy
                : Line("CH01-D15");
            if (string.IsNullOrWhiteSpace(smsPostponeCopy))
                smsPostponeCopy = "Tôi đang ở viện. Hoãn nhé, cậu đừng vào trong.";

            void FinishPhone()
            {
                inspector?.Close();
                bool speakD01 = pendingCh01D01AfterPhone;
                pendingCh01D01AfterPhone = false;
                if (returnToBackpack) OpenBackpack(BackpackCompartment.Tools);
                else CloseModal();
                if (speakD01)
                    PresentLine("CH01-D01");
            }

            void ReadPostponementSms()
            {
                phoneActiveTab = 0;
                bool firstRead = !State.Happened(E10aParts.PostponementSms);
                SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.PostponementSms, "BAY_SMS"), () =>
                {
                    voice?.RecordTranscriptOnly("CH01-D15", "Bảy (SMS · 22:48)", smsPostponeCopy);
                    if (firstRead && State.chapter == "CH01")
                        pendingCh01D01AfterPhone = true;
                    RenderPhone(reopen3D: false);
                });
            }

            void ReadAppointmentSms()
            {
                phoneActiveTab = 1;
                SaveCheckpoint(() => casework.RecordSmsAppointmentViewedAsync(), () => RenderPhone(reopen3D: false));
            }

            void CallBay()
            {
                phoneActiveTab = 2;
                scene.atmosphere?.From("phoneRingNoAnswer", player.Eye.transform.position + player.Eye.transform.forward * 0.28f, 0.72f);
                SaveCheckpoint(() => casework.RecordPhoneCallBayAsync(), () =>
                {
                    ui.Caption("[Chuông đổ hai hồi dài rồi im lặng — chú Bảy không bắt máy]", 4.2f, true, aboveModal: true);
                    RenderPhone(reopen3D: false);
                });
            }

            void RenderPhone(bool reopen3D)
            {
                if (!largePrint && inspector != null && qaFolder == null && (reopen3D || !inspector.IsInspecting))
                {
                    if (!openedAudio)
                    {
                        scene.atmosphere?.From("latchClick", player.Eye.transform.position + player.Eye.transform.forward * 0.30f, 0.34f);
                        openedAudio = true;
                    }
                    var phoneModel = scene.InspectTemplate("PHONE");
                    var phoneTex = Image("Doc_PhoneScreen");
                    if (phoneModel != null)
                        inspector.Open(phoneModel, null, 1f, ActiveVolume(), observationId: "MINH_PHONE");
                    else if (phoneTex != null)
                        inspector.Open(null, phoneTex, (float)phoneTex.width / Mathf.Max(1, phoneTex.height), ActiveVolume(), observationId: "MINH_PHONE");
                }

                bool readPostpone = State.Happened(E10aParts.PostponementSms);
                bool viewedAppt = State.Happened(E10aParts.SmsAppointmentViewed);
                bool calledBay = State.Happened(E10aParts.PhoneCalledBay);
                bool gotMailbox = State.Happened(E10aParts.MailboxPickedUp);

                string section = phoneActiveTab switch
                {
                    0 => "ĐIỆN THOẠI MINH  ·  TIN NHẮN MỚI (22:48)",
                    1 => "ĐIỆN THOẠI MINH  ·  TIN NHẮN HẸN (16/11)",
                    _ => "ĐIỆN THOẠI MINH  ·  GỌI CHÚ BẢY"
                };
                string title = phoneActiveTab switch
                {
                    0 => "SMS · Chú Bảy (17/11/2012 · 22:48)",
                    1 => "SMS · Chú Bảy (16/11/2012 · 19:15)",
                    _ => "Gọi lại cho Chú Bảy"
                };

                var sb = new StringBuilder();
                if (phoneActiveTab == 0)
                {
                    sb.AppendLine("Người gửi: Chú Bảy  ·  Nhận lúc 22:48 (17/11/2012)");
                    sb.AppendLine();
                    sb.AppendLine("«" + smsPostponeCopy + "»");
                    sb.AppendLine();
                    sb.AppendLine(readPostpone
                        ? "✓ Đã đọc tin nhắn báo hoãn của chú Bảy."
                        : "• Nhấn xác nhận mở xem nội dung tin nhắn báo hoãn (không khóa ổ cổng phụ).");
                    if (!gotMailbox)
                        sb.AppendLine("• Theo tin nhắn hẹn hôm qua, chìa cổng và phong bì được gửi ở hộp thư sắt cũ gắn ngoài trụ cổng bên phải.");
                }
                else if (phoneActiveTab == 1)
                {
                    sb.AppendLine("Người gửi: Chú Bảy  ·  Nhận lúc 19:15 (16/11/2012)");
                    sb.AppendLine();
                    sb.AppendLine("«" + smsApptCopy + "»");
                    sb.AppendLine();
                    sb.AppendLine(viewedAppt ? "✓ Đã xem tin nhắn hẹn gốc." : "• Tin nhắn hẹn trước ca thanh lý ngày 18/11/2012.");
                    sb.AppendLine(State.Happened(E10aParts.SignedAppointment)
                        ? "✓ Đã đọc giấy nhắn ngoài bì thư của chú Bảy."
                        : "• Giấy nhắn tay của chú Bảy được kẹp ngoài phong bì trong hộp thư trụ cổng.");
                }
                else
                {
                    sb.AppendLine("Đang gọi: Chú Bảy (Tổ bảo vệ thanh lý An Sinh)");
                    sb.AppendLine();
                    sb.AppendLine(calledBay
                        ? "Kết quả: Chuông đổ kéo dài rồi tự ngắt — không ai bắt máy. Ánh đèn phía trong sảnh lễ tân vẫn sáng sau lớp kính mưa."
                        : "Có thể thử gọi lại cho chú Bảy để hỏi vì sao bên trong bệnh viện vẫn sáng đèn.");
                }

                var choices = new List<ActionChoice>();
                if (phoneActiveTab == 0 && !readPostpone)
                    choices.Add(new ActionChoice("Đọc kỹ tin nhắn báo hoãn (22:48)", ReadPostponementSms));
                if (phoneActiveTab != 0)
                    choices.Add(new ActionChoice(readPostpone ? "Xem SMS báo hoãn (22:48) ✓" : "Mở SMS báo hoãn (22:48)", ReadPostponementSms));
                if (phoneActiveTab != 1)
                    choices.Add(new ActionChoice(viewedAppt ? "Xem SMS hẹn (16/11) ✓" : "Xem SMS hẹn (16/11)", ReadAppointmentSms));
                choices.Add(new ActionChoice(calledBay ? "Gọi lại cho chú Bảy lần nữa" : "Gọi cho chú Bảy", CallBay));
                if (qaFolder == null && inspector != null)
                {
                    choices.Add(new ActionChoice(largePrint ? "Xem điện thoại 3D" : "Mở bản chữ lớn", () =>
                    {
                        largePrint = !largePrint;
                        if (largePrint) inspector.Close();
                        RenderPhone(reopen3D: !largePrint);
                    }));
                }
                choices.Add(new ActionChoice(returnToBackpack ? "Quay lại ba lô" : "Cất điện thoại", FinishPhone));

                if (!largePrint && inspector != null && qaFolder == null)
                {
                    ui.ShowInspectSidebar(section, title, sb.ToString().TrimEnd(), choices.ToArray(), FinishPhone,
                        () => inspector.Close(), "Chuột/WASD: Xoay điện thoại 3D · Chọn mục bên phải để đọc SMS hoặc gọi chú Bảy");
                }
                else
                {
                    ui.Show(section, title, sb.ToString().TrimEnd(), choices.ToArray(), FinishPhone, Image("Doc_PhoneScreen"), largePrint: largePrint);
                }
            }

            // Epistemic rule (4.2): Opening the phone directly on tab 0 commits reading the 22:48 postponement SMS ONLY
            // when player actually opens/reads it (explicit action in Narrative Route; never auto-marked at frame 0!).
            if (phoneActiveTab == 0 && !State.Happened(E10aParts.PostponementSms))
                ReadPostponementSms();
            else if (phoneActiveTab == 1 && !State.Happened(E10aParts.SmsAppointmentViewed))
                ReadAppointmentSms();
            else
                RenderPhone(reopen3D: true);
        }

        private void InteractGateMailbox(Transform source)
        {
            if (State == null || busy) return;
            Vector3 boxPos = source != null ? source.position : new Vector3(5.36f, 1.12f, -20.17f);

            bool pickedUp = State.Happened(E10aParts.MailboxPickedUp) &&
                            InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope) &&
                            InventoryService.HasPhysical(State, BackpackCatalog.GateKey) &&
                            InventoryService.HasPhysical(State, BackpackCatalog.WingCKey);
            if (pickedUp)
            {
                scene.atmosphere?.From("mailboxLatch", boxPos, 0.50f);
                ui.Caption("Hộp thư sắt trống. Chìa cổng và gói tài liệu E10a đang ở trong ba lô.", 3.6f, true);
                return;
            }

            bool wasOpened = State.Happened(E10aParts.MailboxOpened);
            if (!wasOpened)
            {
                scene.atmosphere?.From("mailboxLatch", boxPos, 0.78f);
                scene.atmosphere?.From("mailboxLidCreak", boxPos, 0.72f);
            }

            SaveCheckpoint(() => casework.OpenMailboxAsync(), () =>
            {
                scene.PresentCasework(State);
                bool hasPartial = InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope) ||
                                  InventoryService.HasPhysical(State, BackpackCatalog.GateKey) ||
                                  InventoryService.HasPhysical(State, BackpackCatalog.WingCKey);
                string pickupLabel = hasPartial
                    ? "Lấy nốt vật phẩm còn lại trong hộp thư (không trùng)"
                    : "Nhận gói (Chìa cổng cầm tay · Bì thư E10a & chìa khu C vào ba lô)";
                string notePreview = !string.IsNullOrWhiteSpace(ch01WorldFirst?.mailboxNotePropCopy)
                    ? ch01WorldFirst.mailboxNotePropCopy
                    : "Nếu chưa thấy tôi, vào quầy trực chờ. — Bảy";

                string body =
                    "Bên trong hộp thư sắt rỉ gắn ngoài trụ cổng Đông có:\n\n" +
                    "• Một chiếc chìa đồng cổng phụ treo sẵn bên ngoài (chưa bỏ vào bì).\n" +
                    "• Một phong bì giấy kraft chống ẩm (E10a) kèm mảnh giấy nhắn gấp ngoài và chìa khu C cũ có nhãn «đã thay khóa».\n\n" +
                    "Nhận cả gói sẽ đưa thẳng chìa cổng phụ lên tay và cất phong bì cùng giấy nhắn vào ba lô — có thể mở đọc bất cứ lúc nào trong ba lô (không tự đánh dấu đã đọc).";

                void TakePackage()
                {
                    SaveCheckpoint(() => casework.TakeMailboxPackageAsync(equipGateKey: true), () =>
                    {
                        scene.atmosphere?.From("mailboxPackagePickup", boxPos, 0.85f);
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                        CloseModal();
                        ui.Toast("ĐÃ NHẬN GÓI TỪ HỘP THƯ",
                            "Chìa cổng phụ đang cầm trên tay · Gói E10a và chìa khu C đã cất vào ba lô (0 tự động đọc).", 4.6f);
                    });
                }

                ui.Show("HỘP THƯ NGOÀI TRỤ CỔNG  ·  LOC01", "Gói chú Bảy gửi lại", body,
                    new[]
                    {
                        new ActionChoice(pickupLabel, TakePackage),
                        new ActionChoice("Đọc lướt mảnh giấy nhắn ngoài bì", () =>
                        {
                            SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.SignedAppointment, "SIGNED_APPT"), () =>
                            {
                                ui.Caption($"Giấy nhắn ngoài bì · «{notePreview}»", 4.5f, true, aboveModal: true);
                                InteractGateMailbox(source);
                            });
                        }),
                        new ActionChoice("Đóng nắp hộp thư", CloseModal)
                    }, CloseModal, Image("Doc_BayEnvelopeFront"));
            });
        }
    }
}
