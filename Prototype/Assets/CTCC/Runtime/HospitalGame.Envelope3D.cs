using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Phase 2.5 Physical Envelope & Documents controller (Requirement E):
    /// Replaces tab-based / ActionChoice E10a inspection with direct 3D manipulation of the kraft envelope:
    /// hold -> inspect tucked note -> open flap -> pull out & unfold Lan's 2005 letter, 19/11/2005 clipping, or Wing-C key!
    /// Completely decoupled from Minh's mobile phone; preserves 100% of E10a epistemic contracts.
    /// </summary>
    public sealed partial class HospitalGame
    {
        private DiegeticEnvelope3D envelope3D;
        private bool envelopeReturnToBag;
        private Action envelopeOnClose;
        private bool envelopeFlapKeptOpen;
        private readonly List<ProjectedHotspot3D> projectedHotspotsBuf = new(16);

        internal bool IsDiegeticEnvelopeActive => envelope3D != null && envelope3D.IsOpen;

        private void OpenPhysicalEnvelope3D(Action onClose = null, bool returnToBackpack = false, bool startFlapOpen = false)
        {
            if (State == null || busy) return;
            if (!InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope))
            {
                ui.Caption("Chưa lấy phong bì ngoài hộp thư trụ cổng.", 3.2f);
                return;
            }
            envelopeReturnToBag = returnToBackpack;
            envelopeOnClose = onClose;
            envelopeFlapKeptOpen = startFlapOpen;

            var envTmpl = scene.InspectTemplate("ENVELOPE_3D") ?? scene.InspectTemplate("E10a");
            if (qaFolder != null || envelope3D == null || envTmpl == null)
            {
                ShowEnvelopeLargePrintAccessible();
                return;
            }

            inspector?.Close();
            backpackStage?.Close();
            scene.atmosphere?.From("paper", player.Eye.transform.position + player.Eye.transform.forward * 0.34f, 0.56f);

            envelope3D.Open(envTmpl, ActiveVolume(), startOpen: envelopeFlapKeptOpen,
                onSelectPart: OnEnvelopePartClicked,
                onOpenedFlap: OnEnvelopeFlapOpened);

            ShowEnvelopeStandaloneOverlay();
        }

        private void OnEnvelopeFlapOpened()
        {
            envelopeFlapKeptOpen = true;
            // Real fault-injection probe hook (v36-p25-envelope-autoread): NEVER runs in production!
            if (DiegeticEnvelope3D.QaInjectEnvelopeAutoReadForTesting)
                SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.Letter, "LAN_LETTER"));
            ShowEnvelopeStandaloneOverlay();
        }

        private void ShowEnvelopeStandaloneOverlay()
        {
            if (envelope3D == null || !envelope3D.IsOpen) return;
            bool open = envelope3D.IsFlapOpen;
            var chips = new List<ActionChoice>
            {
                new(open ? "Gấp nắp [Space]" : "Mở nắp [Space]", () => envelope3D.ToggleFlap()),
                new("Chữ lớn [L]", ShowEnvelopeLargePrintAccessible),
                new("Cất phong bì [Esc]", ClosePhysicalEnvelope3D)
            };
            string whisper = open
                ? "Bấm trực tiếp vào thư, mẩu báo, giấy nhắn hoặc chìa để lấy ra xem · Kéo xoay · L chữ lớn · Esc đóng"
                : "Bấm nắp phong bì [Space] để mở · Hoặc bấm giấy nhắn kẹp bên ngoài · Kéo xoay · Esc đóng";

            ui.ShowStandaloneOverlay(
                whisper,
                ClosePhysicalEnvelope3D,
                onClose: () => envelope3D?.Close(),
                microChips: chips.ToArray(),
                onLargePrintKey: ShowEnvelopeLargePrintAccessible);
        }

        private void OnEnvelopePartClicked(EnvelopePartKind kind)
        {
            if (State == null || busy) return;
            envelope3D?.Close();

            void ReturnToOpenEnvelope()
            {
                inspector?.Close();
                OpenPhysicalEnvelope3D(envelopeOnClose, envelopeReturnToBag, startFlapOpen: envelopeFlapKeptOpen);
            }

            switch (kind)
            {
                case EnvelopePartKind.BayNote:
                    SaveCheckpoint(() => casework.RecordMailboxNoteViewedAsync(), () =>
                    {
                        var tex = Image("Doc_BayNote") ?? Image("Doc_BayEnvelopeFront");
                        OpenPulledEnvelopeDocument("Giấy nhắn của chú Bảy", tex, null, "E10A_NOTE",
                            "«Nếu chưa thấy tôi, vào quầy trực chờ. Chìa cổng phụ tôi buộc sẵn bên ngoài bì chống ẩm. — Bảy»",
                            ReturnToOpenEnvelope);
                    });
                    break;

                case EnvelopePartKind.LanLetter:
                    SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.Letter, "LAN_LETTER"), () =>
                    {
                        var front = Image("Doc_LanLetter2005") ?? Image("Doc_BayLetter");
                        var back = Image("Doc_LanLetter2005_Back");
                        OpenPulledEnvelopeDocument("Thư cũ của Lan (2005)", front, back, "E10A_LETTER",
                            "«Anh Minh,\n\nEm cần nói với anh một chuyện ở bệnh viện.\nAnh có thể đến gặp em sau ca trực không?\nEm không muốn để chuyện này qua thêm một ngày nữa.\n\n— Lan»\n\nMặt sau (bút chì):\n«Tìm thấy kẹp sau ngăn kéo tủ lưu trữ khi dọn kho. 16/11/2012 — Bảy»",
                            ReturnToOpenEnvelope);
                    });
                    break;

                case EnvelopePartKind.Newspaper:
                    SaveCheckpoint(() => casework.RecordE10aPartReadAsync(E10aParts.PublicReportSummary, "NEWSPAPER"), () =>
                    {
                        var front = Image("Doc_Newspaper2005");
                        OpenPulledEnvelopeDocument("Mẩu báo địa phương (19/11/2005)", front, null, "E10A_NEWS",
                            "Hỏa hoạn rạng sáng 18/11/2005 tại Bệnh viện từ thiện An Sinh: 13 người tử vong, trong đó có 01 nữ điều dưỡng trực ca đêm. Nguyên nhân sơ bộ được công bố là chập điện khu kỹ thuật lan sang khu lưu bệnh tạm thời thuộc diện bảo trợ nhân đạo. Cơ sở tạm đình chỉ tiếp nhận để phong tỏa, khám nghiệm hiện trường.",
                            ReturnToOpenEnvelope);
                    });
                    break;

                case EnvelopePartKind.WingCKey:
                    SaveCheckpoint(() => casework.InspectE10aLayerAsync(3, "KEYS", markSmsRead: false), () =>
                    {
                        var tagTex = Image("Doc_KeyCTag");
                        OpenPulledEnvelopeDocument("Thẻ chìa khóa cũ", tagTex, null, "E10A_KEYC",
                            "Thẻ giấy buộc trên chìa sắt cũ: «KHU C — CỬA SAU CŨ · ĐÃ THAY KHÓA (2006) · Thu hồi từ kho lưu trữ, không dùng cho cổng phụ. — Bảy»",
                            ReturnToOpenEnvelope);
                    });
                    break;
            }
        }

        private void OpenPulledEnvelopeDocument(string title, Texture front, Texture back, string obsId, string largePrintCopy, Action onReturn)
        {
            scene.atmosphere?.From("paperFlip", player.Eye.transform.position + player.Eye.transform.forward * 0.34f, 0.65f);
            void ShowLarge()
            {
                inspector?.Close();
                ui.Show(title.ToUpperInvariant(), title, largePrintCopy, new[]
                {
                    new ActionChoice("Quay lại xem tờ giấy 3D", () => OpenPulledEnvelopeDocument(title, front, back, obsId, largePrintCopy, onReturn)),
                    new ActionChoice("Cất lại vào phong bì", onReturn)
                }, onReturn, front, largePrint: true);
            }

            if (front != null && inspector != null && qaFolder == null)
            {
                inspector.Open(null, front, (float)front.width / Mathf.Max(1, front.height), ActiveVolume(),
                    backTexture: back, observationId: obsId, standaloneCenter: true, animateUnfold: true);
                var chips = new[]
                {
                    new ActionChoice("Chữ lớn [L]", ShowLarge),
                    new ActionChoice("Trả vào phong bì [Esc]", onReturn)
                };
                string hint = back != null
                    ? "Kéo xoay · Cuộn thu phóng · Chuột phải / Space lật mặt sau · L chữ lớn · Esc cất lại vào phong bì"
                    : "Kéo xoay · Cuộn thu phóng · L chữ lớn · Esc cất lại vào phong bì";
                ui.ShowStandaloneOverlay(hint, onReturn, onClose: () => inspector?.Close(),
                    microChips: chips, onLargePrintKey: ShowLarge);
            }
            else
            {
                ShowLarge();
            }
        }

        private void ShowEnvelopeLargePrintAccessible()
        {
            envelope3D?.Close();
            inspector?.Close();
            var choices = new List<ActionChoice>
            {
                new("Đọc giấy nhắn ngoài bì", () => OnEnvelopePartClicked(EnvelopePartKind.BayNote)),
                new("Mở đọc thư cũ của Lan (2005)", () => OnEnvelopePartClicked(EnvelopePartKind.LanLetter)),
                new("Đọc mẩu báo ngày 19/11/2005", () => OnEnvelopePartClicked(EnvelopePartKind.Newspaper)),
                new("Xem thẻ chìa khóa cũ", () => OnEnvelopePartClicked(EnvelopePartKind.WingCKey)),
            };
            if (qaFolder == null && envelope3D != null)
                choices.Add(new ActionChoice("Quay lại phong bì 3D", () => OpenPhysicalEnvelope3D(envelopeOnClose, envelopeReturnToBag, envelopeFlapKeptOpen)));
            choices.Add(new ActionChoice("Cất phong bì", ClosePhysicalEnvelope3D));

            ui.Show("PHONG BÌ CHỐNG ẨM", "Phong bì chú Bảy gửi lại",
                "Bên ngoài kẹp mảnh giấy nhắn viết tay. Mở nắp phong bì có lá thư cũ của Lan, mẩu báo ngày 19/11/2005 và chiếc chìa sắt cũ buộc thẻ giấy.",
                choices.ToArray(), ClosePhysicalEnvelope3D, Image("Doc_BayEnvelopeFront"), largePrint: true);
        }

        private void ClosePhysicalEnvelope3D()
        {
            envelope3D?.Close();
            inspector?.Close();
            var cb = envelopeOnClose;
            bool backToBag = envelopeReturnToBag;
            envelopeOnClose = null;
            envelopeReturnToBag = false;
            if (cb != null) cb();
            else if (backToBag) OpenBackpack(BackpackCompartment.Papers);
            else CloseModal();
        }

        private void TickDiegeticEnvelope3D()
        {
            if (envelope3D == null || !envelope3D.IsOpen || !ui.IsStandaloneOverlay) return;
            envelope3D.ProjectHotspots(projectedHotspotsBuf, kind => kind switch
            {
                EnvelopePartKind.BayNote => State != null && State.Happened(E10aParts.MailboxNoteViewed),
                EnvelopePartKind.LanLetter => State != null && State.Happened(E10aParts.Letter),
                EnvelopePartKind.Newspaper => State != null && State.Happened(E10aParts.PublicReportSummary),
                _ => false
            });
            ui.Update3DHotspotMarkers(projectedHotspotsBuf);
        }
    }
}
