using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Core v3.6.2 (DEC-V36-CH01-01 / DEC-V361-CH01-01) B01 Phone, Zero-Modal World-First Gate Mailbox,
    /// CH01-D01 Lit-Lobby Sight Guard, and Ch01JournalPolicy controller.
    /// Enforces:
    /// - Epistemic SMS rule (4.2 / 4.5): passive arrival badge never auto-marks postponementSMS; actively opening a tab
    ///   commits reading that exact source; signedAppointment requires BOTH mailbox note + 16/11 appointment SMS.
    /// - CH01-D01 guard (4.4 / Core 04 P78): fires ONLY after BOTH postponementSMS read AND lit lobby visually observed
    ///   (works in either order without ever losing D01; never gates T02).
    /// - Zero-modal World-first Mailbox (4.6): [E] #1 opens the 3D iron lid (-104 deg) revealing the envelope & hanging brass key;
    ///   [E] #2 takes the atomic package directly into hand/bag with zero 2D modal blocking the world; [Q] inspects the folded note.
    /// - Ch01JournalPolicy (Phase 1): locks 5-tab Journal in B01-B03, B06-B08, AN01, and TranscriptPage in CH01.
    /// </summary>
    public sealed partial class HospitalGame
    {
        private bool observedLitLobbyInCh01;
        private bool deliveredCh01D01;
        private int phoneActiveTab; // 0 = Inbox / SMS 22:48, 1 = SMS Appointment 16/11, 2 = Call Bảy
        private static readonly Vector3 LitLobbyLookAnchor = new(4.0f, 1.55f, -6.5f);

        /// <summary>
        /// Phase 2 Ch01JournalPolicy (100% locked across ALL of CH01 B01-B08):
        /// - Completely disables full 5-tab Journal anywhere in CH01 (AN01, Backpack, F101, EX01, COMPARE_*).
        /// - Pocket Note & Transcript remain read-only optional reference in CH01.
        /// - CH02-CH08 keep full 5-tab Journal, document comparison, and F101 revision intact.
        /// </summary>
        internal bool IsJournalAllowedOnAn01 => State != null && State.chapter != "CH01";
        internal bool IsJournalAllowedInBackpack => State != null && State.chapter != "CH01";

        /// <summary>
        /// Core 04 [P78] / 4.4: Checks whether the player camera is actively facing the lit An Sinh reception lobby
        /// from the exterior approach or courtyard (returns false when facing away toward the road/motorbike).
        /// </summary>
        internal bool IsLookingTowardLitLobby()
        {
            if (State == null || State.chapter != "CH01" || State.layer != Reality.R || State.Happened("CH01-T03"))
                return false;
            if (player?.Eye == null) return false;
            Vector3 feet = player.Feet;
            if (feet.x < -2.5f || feet.x > 10.5f || feet.z < -26.5f || feet.z > -4.0f)
                return false;
            Vector3 eyePos = player.Eye.transform.position;
            Vector3 toLobby = LitLobbyLookAnchor - eyePos;
            float dist = toLobby.magnitude;
            if (dist > 26.0f || dist < 0.5f) return false;
            return Vector3.Dot(player.Eye.transform.forward, toLobby / dist) >= 0.62f;
        }

        internal void TickCh01LitLobbySight()
        {
            if (State == null || State.chapter != "CH01" || State.layer != Reality.R || State.Happened("CH01-T03"))
                return;
            if (ui != null && ui.IsModal) return;
            if (inspector != null && inspector.IsInspecting) return;

            if (!observedLitLobbyInCh01 && IsLookingTowardLitLobby())
                observedLitLobbyInCh01 = true;

            if (!deliveredCh01D01 && observedLitLobbyInCh01 && State.Happened(E10aParts.PostponementSms))
                TryPresentCh01D01();
        }

        internal bool TryPresentCh01D01()
        {
            if (State == null || State.chapter != "CH01" || State.layer != Reality.R || State.Happened("CH01-T03"))
                return false;
            if (deliveredCh01D01 || !State.Happened(E10aParts.PostponementSms) || !observedLitLobbyInCh01)
                return false;
            if (ui != null && ui.IsModal) return false;
            if (inspector != null && inspector.IsInspecting) return false;

            if (State.transcript != null && State.transcript.Exists(t => t.LineId == "CH01-D01"))
            {
                deliveredCh01D01 = true;
                return false;
            }
            if (voice?.Transcript != null)
            {
                for (int i = 0; i < voice.Transcript.Count; i++)
                {
                    if (voice.Transcript[i].LineId == "CH01-D01")
                    {
                        deliveredCh01D01 = true;
                        return false;
                    }
                }
            }

            deliveredCh01D01 = true;
            PresentLine("CH01-D01");
            return true;
        }

        private void OpenAn01ReadOnlyInCh01()
        {
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
                if (returnToBackpack) OpenBackpack(BackpackCompartment.Tools);
                else CloseModal();
                // Core 04 [P78] / 4.4: Deliver D01 only if lit lobby has ALSO been observed; otherwise stays armed until player turns to look!
                if (!returnToBackpack)
                    TryPresentCh01D01();
            }

            void ReadPostponementSms()
            {
                phoneActiveTab = 0;
                SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.PostponementSms, "BAY_SMS"), () =>
                {
                    voice?.RecordTranscriptOnly("CH01-D15", "Bảy (SMS · 22:48)", smsPostponeCopy);
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
                bool viewedNote = State.Happened(E10aParts.MailboxNoteViewed);
                bool fullAppt = State.Happened(E10aParts.SignedAppointment);
                bool calledBay = State.Happened(E10aParts.PhoneCalledBay);
                bool gotMailbox = State.Happened(E10aParts.MailboxPickedUp);

                string section = phoneActiveTab switch
                {
                    0 => "ĐIỆN THOẠI MINH  ·  SMS 22:48",
                    1 => "ĐIỆN THOẠI MINH  ·  SMS HẸN 16/11",
                    _ => "ĐIỆN THOẠI MINH  ·  GỌI CHÚ BẢY"
                };
                string title = phoneActiveTab switch
                {
                    0 => "Chú Bảy · 17/11/2012 (22:48)",
                    1 => "Chú Bảy · 16/11/2012 (19:15)",
                    _ => "Gọi lại cho Chú Bảy"
                };

                var sb = new StringBuilder();
                if (phoneActiveTab == 0)
                {
                    sb.AppendLine("«" + smsPostponeCopy + "»");
                    sb.AppendLine();
                    sb.AppendLine("✓ Đã đọc tin nhắn báo hoãn (22:48).");
                    if (!gotMailbox)
                        sb.AppendLine("• Tin nhắn hẹn hôm 16/11 có ghi chỗ gửi chìa và phong bì ở hộp thư cũ cạnh trụ cổng.");
                }
                else if (phoneActiveTab == 1)
                {
                    sb.AppendLine("«" + smsApptCopy + "»");
                    sb.AppendLine();
                    sb.AppendLine("✓ Đã xem SMS hẹn gốc (16/11).");
                    sb.AppendLine(fullAppt
                        ? "✓ Đã đối chiếu đủ lời hẹn (SMS 16/11 + giấy nhắn ngoài bì)."
                        : viewedNote
                            ? "✓ Đã xem cả giấy nhắn ngoài bì."
                            : "• Còn mảnh giấy nhắn tay của chú Bảy kẹp ngoài phong bì trong hộp thư trụ cổng.");
                }
                else
                {
                    sb.AppendLine("Liên hệ: Chú Bảy (Tổ bảo vệ thanh lý An Sinh)");
                    sb.AppendLine();
                    sb.AppendLine(calledBay
                        ? "Chuông đổ kéo dài rồi tự ngắt — không ai bắt máy."
                        : "Thử gọi lại cho chú Bảy.");
                }

                // Streamlined 3-action phone sidebar (4.5 / 4.6): zero redundant confirm buttons.
                var choices = new List<ActionChoice>();
                if (phoneActiveTab != 0)
                    choices.Add(new ActionChoice(readPostpone ? "SMS báo hoãn (22:48) ✓" : "Mở SMS báo hoãn (22:48)", ReadPostponementSms));
                if (phoneActiveTab != 1)
                    choices.Add(new ActionChoice(viewedAppt ? "SMS hẹn (16/11) ✓" : "Xem SMS hẹn (16/11)", ReadAppointmentSms));
                choices.Add(new ActionChoice(calledBay ? "Gọi lại chú Bảy" : "Gọi cho chú Bảy", CallBay));
                if (qaFolder == null && inspector != null)
                {
                    choices.Add(new ActionChoice(largePrint ? "Xem điện thoại 3D" : "Bản chữ lớn", () =>
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
                        () => inspector.Close(), "Chuột/WASD: Xoay điện thoại 3D · Chọn mục bên phải");
                }
                else
                {
                    ui.Show(section, title, sb.ToString().TrimEnd(), choices.ToArray(), FinishPhone, Image("Doc_PhoneScreen"), largePrint: largePrint);
                }
            }

            // Epistemic rule (4.5): Opening an SMS tab directly commits reading that source — never displays body with flag=false!
            if (phoneActiveTab == 0 && !State.Happened(E10aParts.PostponementSms))
                ReadPostponementSms();
            else if (phoneActiveTab == 1 && !State.Happened(E10aParts.SmsAppointmentViewed))
                ReadAppointmentSms();
            else
                RenderPhone(reopen3D: true);
        }

        /// <summary>
        /// Zero-modal World-first Gate Mailbox interaction (4.6 / Core 04 B01):
        /// - First [E] on closed mailbox: unlatches & swings the 3D iron lid (-104 deg) outward in-world, exposing the
        ///   kraft envelope, folded note, and brass gate key hanging on the wire hook. Zero 2D modal!
        /// - Second [E] on open mailbox: atomically takes the package directly in-world (equips KEY_GATE into Minh's 3D hand,
        ///   stows E10a envelope + KEY_WING_C into bag, hides mailbox contents, keeps 0/4 parts read). Zero 2D modal!
        /// </summary>
        private void InteractGateMailbox(Transform source)
        {
            if (State == null || busy) return;
            Vector3 boxPos = source != null ? source.position : new Vector3(5.36f, 1.12f, -20.24f);

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

            // Step 1 (World-first): Unlatch & swing the 3D iron mailbox lid open so the player sees the contents inside!
            if (!State.Happened(E10aParts.MailboxOpened))
            {
                scene.atmosphere?.From("mailboxLatch", boxPos, 0.78f);
                scene.atmosphere?.From("mailboxLidCreak", boxPos, 0.72f);
                SaveCheckpoint(() => casework.OpenMailboxAsync(), () =>
                {
                    scene.PresentCasework(State);
                    ui.Caption("Nắp hộp thư sắt bật mở — thấy chìa cổng đồng treo ngoài bì thư E10a và mảnh giấy nhắn gấp.", 4.2f, true);
                });
                return;
            }

            // Step 2 (World-first): Atomic pickup directly in-world — zero 2D menu blocking the opened mailbox!
            SaveCheckpoint(() => casework.TakeMailboxPackageAsync(equipGateKey: true), () =>
            {
                scene.atmosphere?.From("mailboxPackagePickup", boxPos, 0.85f);
                scene.PresentCasework(State);
                handPresenter?.SyncState(State, scene, hideViewModel: false);
                ui.Toast("ĐÃ NHẬN GÓI TỪ HỘP THƯ",
                    "Chìa cổng phụ đang cầm trên tay · Gói E10a và chìa khu C đã cất vào ba lô.", 4.4f);
            });
        }

        /// <summary>
        /// Optional [Q] world inspect on the open mailbox: lifts & reads the folded external note from Bảy
        /// using the 3D Physical Inspector (records MailboxNoteViewed and completes SignedAppointment if 16/11 SMS was also viewed).
        /// </summary>
        private void InspectMailboxExternalNote(Transform source)
        {
            if (State == null || busy || !State.Happened(E10aParts.MailboxOpened)) return;
            string noteCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.mailboxNotePropCopy)
                ? ch01WorldFirst.mailboxNotePropCopy
                : "Nếu chưa thấy tôi, vào quầy trực chờ. — Bảy";

            SaveCheckpoint(() => casework.RecordMailboxNoteViewedAsync(), () =>
            {
                var noteTex = Image("Doc_BayNote") ?? Image("Doc_BayEnvelopeFront");
                bool use3D = qaFolder == null && inspector != null && noteTex != null;
                if (use3D)
                {
                    scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.55f);
                    inspector.Open(null, noteTex, (float)noteTex.width / Mathf.Max(1, noteTex.height), ActiveVolume(),
                        observationId: "E10a_MAILBOX_NOTE", source: source != null ? source.position : (Vector3?)null);
                }
                void CloseNote()
                {
                    inspector?.Close();
                    CloseModal();
                }
                string statusLine = State.Happened(E10aParts.SignedAppointment)
                    ? "✓ Đã kiểm tra đủ cặp lời hẹn (giấy nhắn ngoài bì + SMS 16/11)."
                    : "✓ Đã đọc giấy nhắn ngoài bì · Xem thêm SMS hẹn 16/11 trên điện thoại [P] để đủ cặp lời hẹn.";
                string body = $"Giấy nhắn tay kẹp bên ngoài phong bì chống ẩm trong hộp thư:\n\n«{noteCopy}»\n\n{statusLine}";
                var choices = new[]
                {
                    new ActionChoice("Đặt lại & tiếp tục", CloseNote)
                };
                if (use3D)
                    ui.ShowInspectSidebar("GIẤY NHẮN NGOÀI BÌ  ·  HỘP THƯ LOC01", "Giấy nhắn của chú Bảy", body,
                        choices, CloseNote, () => inspector.Close(), "Chuột/WASD: Xoay giấy nhắn · Esc để đặt lại");
                else
                    ui.Show("GIẤY NHẮN NGOÀI BÌ  ·  HỘP THƯ LOC01", "Giấy nhắn của chú Bảy", body, choices, CloseNote, noteTex);
            });
        }
    }
}
