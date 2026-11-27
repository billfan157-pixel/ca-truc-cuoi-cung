using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Core v3.6.2 + Phase 2.5 Diegetic 2012 Mobile Phone, Zero-Modal Gate Mailbox,
    /// CH01-D01 Lit-Lobby Two-Way Sight Guard, and Ch01JournalPolicy controller.
    /// - Mobile Phone is Minh's independent personal device owned from game start (0 coupling to Envelope).
    /// - Uses DiegeticPhone3D (live RenderTexture LCD + physically clickable 3D keypad, 0 sidebar by default).
    /// - Passive inbox NEVER auto-marks SMS read; opening each SMS on the physical LCD commits its exact flag.
    /// </summary>
    public sealed partial class HospitalGame
    {
        private DiegeticPhone3D phone3D;
        private bool observedLitLobbyInCh01;
        private bool deliveredCh01D01;
        private bool phoneReturnToBackpack;
        private static readonly Vector3 LitLobbyLookAnchor = new(4.0f, 1.55f, -6.5f);

        internal bool IsJournalAllowedOnAn01 => State != null && State.chapter != "CH01";
        internal bool IsJournalAllowedInBackpack => State != null && State.chapter != "CH01";
        internal bool IsDiegeticPhoneActive => phone3D != null && phone3D.IsOpen;

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
                new("Lưu ca trực tại sổ quầy", () =>
                {
                    if (transaction.KeepManual())
                    {
                        CloseModal();
                        ui.Caption("Đã ghi mốc lưu tay tại sổ trực.", 3.8f, true);
                    }
                }, transaction.AtRequiredPlace(player.Feet, RealityTransaction.ReceptionDesk)),
                new("Mở sổ tay nhỏ (đọc lại ghi nhớ)", () => OpenPocketNote(false)),
                new("Đóng", CloseModal)
            };
            ui.Show("SỔ TRỰC QUẦY TIẾP NHẬN", "Sổ trực lễ tân",
                "Quyển sổ trực bìa cứng đặt trên mặt quầy tiếp nhận. Có thể ghi mốc lưu tay tại đây.",
                choices.ToArray(), CloseModal);
        }

        private void OpenPhone(int? tabOverride = null, bool returnToBackpack = false)
        {
            if (State == null || busy) return;
            phoneReturnToBackpack = returnToBackpack;
            backpackStage?.Close();
            inspector?.Close();

            string smsApptCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.smsAppointmentPropCopy)
                ? ch01WorldFirst.smsAppointmentPropCopy
                : "Minh à, dọn kho thanh lý tôi tìm thấy lá thư cũ cô Lan viết cho cậu hồi 2005. 11 giờ tối mai cậu ghé cổng phụ An Sinh lấy trước khi họ san phẳng. Nếu tôi chưa ra kịp thì chìa cổng và phong bì tôi để trong hộp thư cũ cạnh trụ cổng.";
            string smsPostponeCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.smsPostponementPropCopy)
                ? ch01WorldFirst.smsPostponementPropCopy
                : Line("CH01-D15");
            if (string.IsNullOrWhiteSpace(smsPostponeCopy))
                smsPostponeCopy = "Tôi đang ở viện. Hoãn nhé, cậu đừng vào trong.";

            var phoneModel = scene.InspectTemplate("PHONE");
            if (qaFolder != null || phone3D == null || phoneModel == null)
            {
                OpenPhoneLargePrintAccessible(smsPostponeCopy, smsApptCopy);
                return;
            }

            // Start on Inbox by default so passive opening NEVER auto-marks SMS read!
            PhoneScreenMode startMode = PhoneScreenMode.Inbox;
            if (tabOverride == 1) startMode = PhoneScreenMode.ReadAppointmentSms;
            else if (tabOverride == 2) startMode = PhoneScreenMode.CallingBay;

            scene.atmosphere?.From("latchClick", player.Eye.transform.position + player.Eye.transform.forward * 0.28f, 0.34f);

            phone3D.Open(
                phoneModel,
                ActiveVolume(),
                smsPostponeCopy,
                smsApptCopy,
                State.Happened(E10aParts.PostponementSms),
                State.Happened(E10aParts.SmsAppointmentViewed),
                State.Happened(E10aParts.PhoneCalledBay),
                startMode,
                onEnterScreen: mode => OnDiegeticPhoneScreenEntered(mode, smsPostponeCopy),
                onDial: OnDiegeticPhoneDialBay,
                onClose: FinishPhone);

            // If explicitly opened directly to a specific SMS screen (tabOverride == 1), commit that exact read
            if (startMode == PhoneScreenMode.ReadAppointmentSms && !State.Happened(E10aParts.SmsAppointmentViewed))
                OnDiegeticPhoneScreenEntered(startMode, smsPostponeCopy);

            var chips = new[]
            {
                new ActionChoice("Chữ lớn [L]", () => OpenPhoneLargePrintAccessible(smsPostponeCopy, smsApptCopy)),
                new ActionChoice("Cất máy [Esc]", FinishPhone)
            };
            ui.ShowStandaloneOverlay(
                "Bấm trực tiếp phím trên điện thoại (hoặc ↑↓ / Enter đọc · C gọi · Backspace quay lại · L chữ lớn · Esc cất máy)",
                FinishPhone,
                onClose: () => phone3D?.Close(),
                microChips: chips,
                onLargePrintKey: () => OpenPhoneLargePrintAccessible(smsPostponeCopy, smsApptCopy));
        }

        private void OnDiegeticPhoneScreenEntered(PhoneScreenMode mode, string smsPostponeCopy)
        {
            if (State == null) return;
            if (mode == PhoneScreenMode.ReadPostponeSms)
            {
                SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.PostponementSms, "BAY_SMS"), () =>
                {
                    voice?.RecordTranscriptOnly("CH01-D15", "Bảy (SMS · 22:48)", smsPostponeCopy);
                    phone3D?.SyncFlags(
                        State.Happened(E10aParts.PostponementSms),
                        State.Happened(E10aParts.SmsAppointmentViewed),
                        State.Happened(E10aParts.PhoneCalledBay));
                });
            }
            else if (mode == PhoneScreenMode.ReadAppointmentSms)
            {
                SaveCheckpoint(() => casework.RecordSmsAppointmentViewedAsync(), () =>
                {
                    phone3D?.SyncFlags(
                        State.Happened(E10aParts.PostponementSms),
                        State.Happened(E10aParts.SmsAppointmentViewed),
                        State.Happened(E10aParts.PhoneCalledBay));
                });
            }
        }

        private void OnDiegeticPhoneDialBay()
        {
            if (State == null) return;
            scene.atmosphere?.From("phoneRingNoAnswer", player.Eye.transform.position + player.Eye.transform.forward * 0.26f, 0.72f);
            SaveCheckpoint(() => casework.RecordPhoneCallBayAsync(), () =>
            {
                ui.Caption("[Chuông đổ hai hồi dài rồi im lặng — không ai bắt máy]", 4.0f, true, aboveModal: true);
                phone3D?.SyncFlags(
                    State.Happened(E10aParts.PostponementSms),
                    State.Happened(E10aParts.SmsAppointmentViewed),
                    State.Happened(E10aParts.PhoneCalledBay));
            });
        }

        private void OpenPhoneLargePrintAccessible(string smsPostponeCopy, string smsApptCopy)
        {
            phone3D?.Close();
            var choices = new List<ActionChoice>
            {
                new("Đọc tin nhắn báo hoãn (22:48)", () =>
                    SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.PostponementSms, "BAY_SMS"), () =>
                    {
                        voice?.RecordTranscriptOnly("CH01-D15", "Bảy (SMS · 22:48)", smsPostponeCopy);
                        ui.Show("TIN NHẮN ĐẾN · 22:48", "Chú Bảy (17/11/2012 · 22:48)",
                            "«" + smsPostponeCopy + "»",
                            new[]
                            {
                                new ActionChoice("Gọi chú Bảy", OnDiegeticPhoneDialBay),
                                new ActionChoice("Quay lại hộp thư", () => OpenPhoneLargePrintAccessible(smsPostponeCopy, smsApptCopy)),
                                new ActionChoice("Cất điện thoại", FinishPhone)
                            }, FinishPhone, largePrint: true);
                    })),
                new("Đọc tin nhắn hẹn (16/11)", () =>
                    SaveCheckpoint(() => casework.RecordSmsAppointmentViewedAsync(), () =>
                    {
                        ui.Show("TIN NHẮN ĐẾN · 16/11", "Chú Bảy (16/11/2012 · 19:15)",
                            "«" + smsApptCopy + "»",
                            new[]
                            {
                                new ActionChoice("Gọi chú Bảy", OnDiegeticPhoneDialBay),
                                new ActionChoice("Quay lại hộp thư", () => OpenPhoneLargePrintAccessible(smsPostponeCopy, smsApptCopy)),
                                new ActionChoice("Cất điện thoại", FinishPhone)
                            }, FinishPhone, largePrint: true);
                    })),
                new("Gọi chú Bảy", OnDiegeticPhoneDialBay),
            };
            if (qaFolder == null && phone3D != null)
                choices.Add(new ActionChoice("Quay lại điện thoại 3D", () => OpenPhone(null, phoneReturnToBackpack)));
            choices.Add(new ActionChoice("Cất điện thoại", FinishPhone));

            ui.Show("ĐIỆN THOẠI DI ĐỘNG", "Hộp thư đến (2 tin nhắn)",
                "• Chú Bảy · 22:48 — Tôi đang ở viện. Hoãn nhé...\n• Chú Bảy · 16/11 — Minh à, dọn kho thanh lý...",
                choices.ToArray(), FinishPhone, Image("Doc_PhoneScreen"), largePrint: true);
        }

        private void FinishPhone()
        {
            phone3D?.Close();
            inspector?.Close();
            bool backToBag = phoneReturnToBackpack;
            phoneReturnToBackpack = false;
            if (backToBag) OpenBackpack(BackpackCompartment.Tools);
            else CloseModal();
            if (!backToBag)
                TryPresentCh01D01();
        }

        private void TickDiegeticPhone3D()
        {
            if (phone3D == null || !phone3D.IsOpen || !ui.IsStandaloneOverlay) return;
            phone3D.ProjectHotspots(projectedHotspotsBuf);
            ui.Update3DHotspotMarkers(projectedHotspotsBuf);
        }

        private void InteractGateMailbox(Transform source)
        {
            if (State == null || busy) return;
            Vector3 boxPos = source != null ? source.position : new Vector3(5.36f, 1.18f, -20.29f);

            bool pickedUp = State.Happened(E10aParts.MailboxPickedUp) &&
                            InventoryService.HasPhysical(State, BackpackCatalog.GateKey) &&
                            InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope) &&
                            InventoryService.HasPhysical(State, BackpackCatalog.WingCKey);
            if (pickedUp)
            {
                scene.atmosphere?.From("mailboxLatch", boxPos, 0.50f);
                ui.Caption("Hộp thư sắt đã trống.", 3.0f, true);
                return;
            }

            if (!State.Happened(E10aParts.MailboxOpened))
            {
                scene.atmosphere?.From("mailboxLatch", boxPos, 0.78f);
                scene.atmosphere?.From("mailboxLidCreak", boxPos, 0.72f);
                SaveCheckpoint(() => casework.OpenMailboxAsync(), () =>
                {
                    scene.PresentCasework(State);
                    ui.Caption("Nắp hộp thư sắt bật mở — bên trong có chìa cổng đồng treo ngoài phong bì và mảnh giấy nhắn gấp.", 4.0f, true);
                });
                return;
            }

            SaveCheckpoint(() => casework.TakeMailboxPackageAsync(equipGateKey: true), () =>
            {
                scene.atmosphere?.From("mailboxPackagePickup", boxPos, 0.85f);
                scene.PresentCasework(State);
                handPresenter?.SyncState(State, scene, hideViewModel: false);
                ui.Caption("Đã lấy chìa cổng phụ trên tay và cất phong bì cùng chìa cũ vào ba lô.", 4.2f, true);
            });
        }

        private void InspectMailboxExternalNote(Transform source)
        {
            if (State == null || busy || !State.Happened(E10aParts.MailboxOpened)) return;
            string noteCopy = !string.IsNullOrWhiteSpace(ch01WorldFirst?.mailboxNotePropCopy)
                ? ch01WorldFirst.mailboxNotePropCopy
                : "Nếu chưa thấy tôi, vào quầy trực chờ. — Bảy";

            SaveCheckpoint(() => casework.RecordMailboxNoteViewedAsync(), () =>
            {
                var noteTex = Image("Doc_BayNote") ?? Image("Doc_BayEnvelopeFront");
                void CloseNote()
                {
                    inspector?.Close();
                    CloseModal();
                }
                void ShowLarge()
                {
                    inspector?.Close();
                    ui.Show("GIẤY NHẮN NGOÀI BÌ", "Giấy nhắn của chú Bảy", $"«{noteCopy}»",
                        new[] { new ActionChoice("Đặt lại", CloseNote) }, CloseNote, noteTex, largePrint: true);
                }

                if (qaFolder == null && inspector != null && noteTex != null)
                {
                    scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.55f);
                    inspector.Open(null, noteTex, (float)noteTex.width / Mathf.Max(1, noteTex.height), ActiveVolume(),
                        observationId: "E10a_MAILBOX_NOTE", source: source != null ? source.position : (Vector3?)null,
                        standaloneCenter: true, animateUnfold: true);
                    ui.ShowStandaloneOverlay("Kéo xoay · Cuộn thu phóng · L chữ lớn · Esc đặt lại", CloseNote,
                        onClose: () => inspector?.Close(),
                        microChips: new[] { new ActionChoice("Chữ lớn [L]", ShowLarge), new ActionChoice("Đặt lại [Esc]", CloseNote) },
                        onLargePrintKey: ShowLarge);
                }
                else
                {
                    ShowLarge();
                }
            });
        }
    }
}
