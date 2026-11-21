using System.Collections.Generic;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private readonly struct TimelineCard
        {
            public readonly string Id, When, Title, Text;
            public TimelineCard(string id, string when, string title, string text)
            {
                Id = id;
                When = when;
                Title = title;
                Text = text;
            }
        }

        private string timelineSource, timelineFeedback;
        private TimelineConnection timelineDraft;

        private List<TimelineCard> ReadTimelineSources()
        {
            var cards = new List<TimelineCard>();
            if (JournalNote.HasSource(State, ObservationRegistry.Bed3Meds))
                cards.Add(new(ObservationRegistry.Bed3Meds, "17.11.2005 · 10:15", "Phiếu thuốc giường 03",
                    "Giờ khóa phiếu sau khi xuất viện."));
            if (JournalNote.HasSource(State, ObservationRegistry.Bed2Transfer))
                cards.Add(new(ObservationRegistry.Bed2Transfer, "17.11.2005 · 19:20", "Phiếu chuyển giường 02",
                    "Giờ chuyển đến Nội B, có chữ ký hai bên."));
            if (CorroborationService.HasClock(State))
                cards.Add(new(ObservationRegistry.StoppedClock0317, "03:17", "Đồng hồ ở quầy",
                    "Hai kim đứng yên. Mặt đồng hồ không ghi ngày hoặc nguyên nhân dừng."));
            if (CorroborationService.HasCardTimestamp(State))
                cards.Add(new(ObservationRegistry.Bed4Card, "03g17", "Vết hằn trên AS-05B",
                    "Vết hằn bên mặt sau ghi «03g17 · CHƯA CHỐT SỔ»."));
            foreach (var record in State.observationRecords)
            {
                if (!CorroborationService.IsShiftContext(record.id) || record.stage < ObservationStage.Examined) continue;
                var spec = ObservationRegistry.Get(record.id);
                cards.Add(new(record.id, record.id == ObservationRegistry.ReceptionDrawerLog ? "Đêm 17–18.11.2005" : null,
                    ClipLabel(record.id, spec.Title), spec.FactSummary));
            }
            if (State.Happened("E10a.SMSRead"))
            {
                cards.Add(new("E10a.NEWS", "18.11.2005 · Rạng sáng", "Mẩu báo",
                    "Tin công khai về vụ cháy và 13 người tử vong."));
                cards.Add(new("E10a.MEETING", "17.11.2012 · 23:00", "Tờ hẹn của Bảy",
                    "Tin nhắn sau đó báo hoãn cuộc gặp."));
            }
            if (State.Happened("CH01-T04"))
                foreach (var line in voice.Transcript)
                    if (line.LineId == "CH01-D04c")
                    {
                        cards.Add(new(line.LineId, null, "Lời Lan tại quầy", line.Text));
                        break;
                    }
            return cards;
        }

        private void DrawTimeline()
        {
            var cards = ReadTimelineSources();
            var choices = new List<NotebookEntry>(cards.Count);
            foreach (var card in cards)
                choices.Add(new NotebookEntry(card.When == null ? card.Title : card.When + "\n" + card.Title, () =>
                {
                    timelineSource = card.Id;
                    DrawJournal(JournalTab.Timeline);
                }, card.Id));
            bool signed = CorroborationService.IsCorroborated(State);
            bool editing = !signed && State.chapter == "CH01" && State.layer == Reality.B && !State.Happened("CH01-T08");
            var link = signed ? State.exception0317Link : timelineDraft;
            var source = cards.Find(c => c.Id == timelineSource);
            if (signed)
                choices.Insert(0, new NotebookEntry("03:17 · Mốc đã đối chiếu", () =>
                {
                    timelineSource = null;
                    DrawJournal(JournalTab.Timeline);
                }));
            ui.NotebookIndex(choices, "Các mốc sẽ có ở đây sau khi kiểm tra nguồn.", "Mốc và ghi chép");
            if (!editing)
            {
                bool showLink = signed && source.Id == null;
                string copy = showLink
                    ? "03:17 · Đồng hồ và vết hằn trên thẻ trùng giờ. Mốc này chưa xác định sự kiện, thời điểm tử vong hoặc số phận người bệnh."
                    : source.Text ?? "Chọn một mốc bên mép sổ để đọc.";
                if (showLink && link.HasValue)
                    copy += "\n\nGhi bên lề · " + cards.Find(c => c.Id == link.context).Title;
                ui.NotebookPage(showLink ? "Mốc đã đối chiếu" : source.Title ?? "Dòng thời gian", copy);
                return;
            }

            bool context = source.Id != null && CorroborationService.IsShiftContext(source.Id);
            ui.NotebookTimeline(
                IsPinned(link.left, "Giấy kẹp trái", 0),
                IsPinned(link.right, "Giấy kẹp phải", 1),
                IsPinned(link.context, "Ghi bên lề", 2),
                source.Id == null ? "Chọn một nguồn ở mép sổ, hoặc kéo thẻ vào trang." : (source.Title + "\n" + source.Text),
                new ActionChoice("Ghim mốc trái", () => PutTimelineCard(source.Id, 0), source.When != null && !context),
                new ActionChoice("Ghim mốc phải", () => PutTimelineCard(source.Id, 1), source.When != null && !context),
                new ActionChoice("Kẹp ghi chú ca trực", () => PutTimelineCard(source.Id, 2), context),
                new ActionChoice("Nối hai mốc", SubmitTimeline, !string.IsNullOrEmpty(link.left) && !string.IsNullOrEmpty(link.right)),
                PutTimelineCard, timelineFeedback);

            ActionChoice IsPinned(string id, string empty, int slot)
            {
                var card = cards.Find(c => c.Id == id);
                string label = card.Id == null ? empty : (card.When == null ? card.Title : card.When + "\n" + card.Title);
                return new ActionChoice(label, () => PutTimelineCard(null, slot), card.Id != null);
            }
        }

        private void PutTimelineCard(string id, int slot)
        {
            bool context = CorroborationService.IsShiftContext(id);
            if (id != null && ((slot == 2) != context ||
                !ReadTimelineSources().Exists(c => c.Id == id && (context || c.When != null))))
                return;
            if (slot == 0)
            {
                if (id != null && timelineDraft.right == id) timelineDraft.right = timelineDraft.left;
                timelineDraft.left = id;
            }
            else if (slot == 1)
            {
                if (id != null && timelineDraft.left == id) timelineDraft.left = timelineDraft.right;
                timelineDraft.right = id;
            }
            else timelineDraft.context = id;
            timelineFeedback = null;
            DrawJournal(JournalTab.Timeline);
        }

        private void SubmitTimeline()
        {
            if (!CorroborationService.Supported0317(State, timelineDraft))
            {
                timelineFeedback = string.IsNullOrEmpty(timelineDraft.context)
                    ? "Mình cần ghi rõ bối cảnh của ca trực trước khi lưu mối liên hệ."
                    : "Hai nguồn này chưa đủ để ghi cùng một mốc. Mình cần xem lại giờ ghi và nguồn của mỗi thẻ.";
                DrawJournal(JournalTab.Timeline);
                return;
            }

            SaveCheckpoint(() => casework.CrossCheck0317ExceptionAsync(timelineDraft), () =>
            {
                timelineDraft = default;
                timelineFeedback = timelineSource = null;
                DrawJournal(JournalTab.Timeline);
            });
        }
    }
}
