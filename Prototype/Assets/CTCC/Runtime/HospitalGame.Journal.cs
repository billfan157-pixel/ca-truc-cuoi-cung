using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private enum JournalTab { Field, Documents, Reasoning, Timeline, Decisions }
        private JournalTab journalTab;
        private string fieldSelection, documentSelection, leftDocument, rightDocument;
        private bool comparingDocuments, leftBack, rightBack;
        private int editNoteIndex = -1, chooseNoteSource;
        private int selectedDecisionRevision;
        private JournalNote noteDraft;

        private void Journal() => DrawJournal(journalTab, pageTurn: true);

        private void ResetNotebookSession()
        {
            journalTab = JournalTab.Field;
            fieldSelection = documentSelection = leftDocument = rightDocument = null;
            comparingDocuments = leftBack = rightBack = false;
            noteDraft = null;
            chooseNoteSource = 0;
            selectedDecisionRevision = 0;
        }

        private void DrawJournal(JournalTab tab, bool pageTurn = false)
        {
            if (pageTurn) scene.atmosphere?.From("paperFlip", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.44f);
            journalTab = tab;
            ui.ShowNotebook((int)tab, index =>
            {
                noteDraft = null;
                chooseNoteSource = 0;
                DrawJournal((JournalTab)index, pageTurn: true);
            }, () =>
            {
                noteDraft = null;
                chooseNoteSource = 0;
                CloseModal();
            }, Options.alternateBindings, pageTurn && !Options.reduceMotion);

            switch (tab)
            {
                case JournalTab.Field: DrawFieldNotes(); break;
                case JournalTab.Documents: DrawDocuments(); break;
                case JournalTab.Reasoning: DrawReasoning(); break;
                case JournalTab.Timeline: DrawTimeline(); break;
                case JournalTab.Decisions: DrawDecisions(); break;
            }
            ui.FinishNotebook();
        }

        private void DrawFieldNotes()
        {
            var entries = new List<NotebookEntry>
            {
                new("Trang đầu", () => { fieldSelection = null; DrawJournal(JournalTab.Field); })
            };
            foreach (var rec in State.observationRecords)
            {
                if (rec.stage < ObservationStage.Examined) continue;
                var spec = ObservationRegistry.Get(rec.id);
                if (spec == null || spec.IsDocument) continue;
                entries.Add(new NotebookEntry(spec.Title, () => { fieldSelection = rec.id; DrawJournal(JournalTab.Field); }));
            }
            if (State.Happened("CH01-T04"))
                entries.Add(new NotebookEntry("Lời Lan tại quầy", () =>
                {
                    fieldSelection = "LAN_TALK";
                    DrawJournal(JournalTab.Field);
                }));
            if (voice.Transcript.Count > 0) entries.Add(new NotebookEntry("Bản ghi hội thoại", TranscriptPage));
            entries.Add(new NotebookEntry("Cách dùng sổ", () =>
            {
                fieldSelection = "HELP";
                DrawJournal(JournalTab.Field);
            }));
            ui.NotebookIndex(entries, "");

            var selected = State.FindObservation(fieldSelection);
            if (selected != null && ObservationRegistry.Get(selected.id) is ObservationSpec observation)
            {
                var body = new StringBuilder(PlaceName(selected.location)).Append(" · Lớp ").Append(selected.layer)
                    .Append("\n\n").Append(observation.FactSummary);
                AppendAuthoredNotes(body, selected.id);
                ui.NotebookPage(observation.Title, body.ToString(),
                    new ActionChoice("Ghi bên lề", () => EditJournalNote(-1, JournalNoteKind.Annotation, selected.id)));
            }
            else if (fieldSelection == "LAN_TALK")
            {
                var copy = new StringBuilder();
                foreach (var entry in voice.Transcript)
                    if (entry.LineId is "CH01-D03" or "CH01-D04" or "CH01-D05" or "CH01-D07" or "CH01-D04c" or "CH01-D16")
                        copy.Append(entry.Speaker).Append(" — ").AppendLine(entry.Text).AppendLine();
                ui.NotebookPage("Lời Lan tại quầy", copy.ToString());
            }
            else if (fieldSelection == "HELP") JournalHelp();
            else
            {
                var visited = new List<string> { "Cổng phụ" };
                if (State.Happened("CH01-T03")) visited.Add("Quầy tiếp nhận");
                if (State.observationRecords.Exists(record => record.location == "STAFF")) visited.Add("Bàn điều dưỡng");
                if (State.Happened("CH01-T06")) visited.Add("Phòng 101");
                string copy = string.Join("  ·  ", visited);
                if (State.chapter == "CH02" && State.Has("E12")) copy += "\n\nVị trí kê giường thứ tư đã đối chiếu tại hiện trường.";
                if (GateService.G02(State)) copy += "\n\nHồ sơ của An đã được xác thực từ vật chứng trong kho.";
                if (State.PatientConfirmed("C02")) copy += State.PatientIdentityVerified("C02") ? "\n\nC02 · Bà Lành." : "\n\nC02 · Đã ghi nhận người bệnh.";
                if (State.PatientConfirmed("C12")) copy += "\n\nC12 · Chưa biết danh tính.";
                if (GateService.H05(State)) copy += "\n\nĐã đối chiếu thẻ Lan với hồ sơ tiếp nhận và phiếu thu hồi.";
                ui.NotebookPage("Những nơi đã xem", copy);
            }
        }

        private static string PlaceName(string id) => id switch
        {
            "LOC01" => "Cổng phụ và sân",
            "LOC02" => "Quầy tiếp nhận",
            "STAFF" => "Bàn điều dưỡng",
            "HALL" => "Hành lang",
            "LOC03" => "Phòng 101",
            _ => id
        };

        private void JournalHelp()
        {
            ui.NotebookPage("Dùng sổ trực",
                "Ở Tài liệu, kéo một tờ vào ô trống trên trang đối chiếu hoặc dùng nút Kẹp trái / phải.\n\n" +
                "Ở Suy luận, tự viết một giả thuyết rồi chọn hai nguồn nếu muốn nối chúng. Có thể tháo liên kết bất cứ lúc nào.\n\n" +
                "Khi cầm F101 hoặc thẻ AS-05B, bấm phải trên dòng chữ để đánh dấu. Có thể chọn vùng bằng phím số rồi nhấn M. Bản chữ lớn có các thao tác tương đương.\n\n" +
                "Tab hoặc mũi tên chuyển mục đang chọn, Enter mở mục, Page Up / Page Down cuộn trang. Esc khép sổ.",
                new ActionChoice("Trở về trang đầu", () => { fieldSelection = null; DrawJournal(JournalTab.Field); }));
        }

        private void DrawDocuments()
        {
            var docs = CollectObservedDocuments();
            var entries = new List<NotebookEntry>();
            if (State.Happened("E10a.SMSRead"))
                entries.Add(new NotebookEntry("Bì thư và tin nhắn của Bảy", () =>
                {
                    documentSelection = "E10a";
                    comparingDocuments = false;
                    DrawJournal(JournalTab.Documents);
                }));
            foreach (var paper in docs)
                entries.Add(new NotebookEntry(paper.Title, () =>
                {
                    if (comparingDocuments) PlaceDocument(paper.Id, leftDocument == null ? 0 : 1);
                    else
                    {
                        documentSelection = paper.Id;
                        DrawJournal(JournalTab.Documents);
                    }
                }, paper.Id));

            if (comparingDocuments)
            {
                ui.NotebookCompare(ClipLabel(leftDocument, "Trái"), ClipImage(leftDocument, leftBack), ClipActions(leftDocument, 0),
                    ClipLabel(rightDocument, "Phải"), ClipImage(rightDocument, rightBack), ClipActions(rightDocument, 1),
                    PlaceDocument, new ActionChoice("Trở lại tài liệu", () => { comparingDocuments = false; DrawJournal(JournalTab.Documents); }));
                ui.NotebookIndex(entries, "Chưa lưu bản sao.");
                return;
            }
            ui.NotebookIndex(entries, "Tài liệu sẽ được kẹp vào đây sau khi đọc.");
            var actions = new List<ActionChoice>();
            var spec = docs.Find(d => d.Id == documentSelection);
            string title = "Bản sao đã đọc";
            string copy = "Chọn một nguồn bên trang trái để mở.";
            if (documentSelection == "E10a" && State.Happened("E10a.SMSRead"))
            {
                title = "Bì thư của Bảy";
                copy = "Túi vải cạnh cổng phụ · Lớp R\n\nTờ hẹn lúc 23 giờ, thư cũ, mẩu báo và tin nhắn báo hoãn.";
                actions.Add(new ActionChoice("Mở bì thư", Letter));
                actions.Add(new ActionChoice("Ghi bên lề", () => EditJournalNote(-1, JournalNoteKind.Annotation, "E10a")));
            }
            else if (spec != null)
            {
                title = spec.Title;
                var record = State.FindObservation(spec.Id);
                var body = new StringBuilder(spec.ProvenanceLabel).Append("\n\n").Append(ObservationSynopsis(spec, record));
                if (State.f101Annotations.MarkedHotspot(spec.Id) is string field && !string.IsNullOrEmpty(field))
                    body.Append("\n\nĐã đánh dấu · ").Append(AnnotationLabel(field));
                AppendAuthoredNotes(body, spec.Id);
                copy = body.ToString();
                actions.Add(new ActionChoice("Mở bản sao 3D / chữ lớn", () => OpenObservationInspector(spec, Journal)));
                actions.Add(new ActionChoice("Ghi bên lề", () => EditJournalNote(-1, JournalNoteKind.Annotation, spec.Id)));
                actions.Add(new ActionChoice("Kẹp sang trái", () => PlaceDocument(spec.Id, 0)));
                actions.Add(new ActionChoice("Kẹp sang phải", () => PlaceDocument(spec.Id, 1)));
            }
            actions.Add(new ActionChoice("So sánh hai tài liệu", () => { comparingDocuments = true; DrawJournal(JournalTab.Documents, true); }, docs.Count > 0));
            ui.NotebookPage(title, copy, actions.ToArray());
        }

        private static string ObservationSynopsis(ObservationSpec spec, ObservationRecord record)
        {
            string facts = spec.FactSummary;
            if (record?.flippedBack == true)
            {
                string back = ObservationBackText(spec.Id);
                if (!string.IsNullOrEmpty(back)) facts += "\n\nMặt sau\n" + back;
            }
            return facts;
        }

        private void AppendAuthoredNotes(StringBuilder copy, string source)
        {
            foreach (var note in State.journalNotes)
                if (note.source == source && note.kind == JournalNoteKind.Annotation)
                    copy.Append("\n\nGhi bên lề\n").Append(note.text);
        }

        private static string ClipLabel(string id, string empty) => id switch
        {
            ObservationRegistry.F101Sheet => "F101",
            ObservationRegistry.Bed4Card => "Thẻ AS-05B",
            ObservationRegistry.Bed2Transfer => "Phiếu chuyển giường 02",
            ObservationRegistry.Bed3Meds => "Phiếu thuốc giường 03",
            _ => ObservationRegistry.Get(id)?.Title ?? empty
        };

        private Texture ClipImage(string id, bool back)
        {
            var doc = ObservationRegistry.Get(id);
            return doc == null ? null : Image(back ? doc.BackTexture : doc.FrontTexture);
        }

        private ActionChoice[] ClipActions(string id, int side)
        {
            var doc = ObservationRegistry.Get(id);
            if (doc == null) return Array.Empty<ActionChoice>();
            bool isBack = side == 0 ? leftBack : rightBack;
            string name = ClipLabel(id, "");
            var choices = new List<ActionChoice>
            {
                new("Mở để đọc", () => OpenObservationInspector(doc, Journal))
            };
            if (doc.HasBackSide && State.FindObservation(id)?.flippedBack == true)
                choices.Add(new ActionChoice(isBack ? "Mặt trước" : "Mặt sau", () =>
                {
                    if (side == 0) leftBack = !leftBack; else rightBack = !rightBack;
                    DrawJournal(JournalTab.Documents);
                }));
            choices.Add(new ActionChoice("Rút " + name, () =>
            {
                if (side == 0) leftDocument = null; else rightDocument = null;
                DrawJournal(JournalTab.Documents);
            }));
            return choices.ToArray();
        }

        private void PlaceDocument(string id, int side)
        {
            if (!JournalNote.HasSource(State, id) || ObservationRegistry.Get(id)?.IsDocument != true) return;
            if (side == 0)
            {
                if (rightDocument == id) { rightDocument = leftDocument; rightBack = leftBack; }
                leftDocument = id;
                leftBack = false;
            }
            else
            {
                if (leftDocument == id) { leftDocument = rightDocument; leftBack = rightBack; }
                rightDocument = id;
                rightBack = false;
            }
            comparingDocuments = true;
            DrawJournal(JournalTab.Documents);
        }

        private static string NoteLabel(JournalNoteKind kind) => kind switch
        {
            JournalNoteKind.Annotation => "Ghi bên lề",
            JournalNoteKind.Contradiction => "Điểm mâu thuẫn",
            JournalNoteKind.Question => "Câu hỏi còn mở",
            _ => "Giả thuyết"
        };

        private string SourceTitle(string id) => id == "E10a" ? "Bì thư của Bảy" :
            ClipLabel(id, "Chưa kẹp nguồn");

        private void DrawReasoning()
        {
            var entries = new List<NotebookEntry>();
            if (chooseNoteSource != 0 && noteDraft != null)
            {
                AddSource("E10a");
                foreach (var record in State.observationRecords) AddSource(record.id);
                ui.NotebookIndex(entries, "Chưa có nguồn để kẹp.");
                ui.NotebookPage(chooseNoteSource == 1 ? "Chọn nguồn thứ nhất" : "Chọn nguồn thứ hai",
                    noteDraft.text ?? "", new ActionChoice("Trở lại dòng đang viết", () =>
                    {
                        chooseNoteSource = 0;
                        DrawJournal(JournalTab.Reasoning);
                    }));
                return;

                void AddSource(string id)
                {
                    if (!JournalNote.HasSource(State, id) ||
                        id == (chooseNoteSource == 1 ? noteDraft.related : noteDraft.source))
                        return;
                    var spec = ObservationRegistry.Get(id);
                    entries.Add(new NotebookEntry(spec?.Title ?? SourceTitle(id), () =>
                    {
                        if (chooseNoteSource == 1) noteDraft.source = id;
                        else noteDraft.related = id;
                        chooseNoteSource = 0;
                        DrawJournal(JournalTab.Reasoning);
                    }));
                }
            }

            entries.Add(new NotebookEntry("Trang suy luận", () => { noteDraft = null; DrawJournal(JournalTab.Reasoning); }));
            for (int i = 0; i < State.journalNotes.Count; i++)
            {
                int index = i;
                var note = State.journalNotes[i];
                string shortText = note.text.Replace('\n', ' ');
                if (shortText.Length > 43) shortText = shortText.Substring(0, 41) + "…";
                entries.Add(new NotebookEntry(NoteLabel(note.kind) + "\n" + shortText, () => EditJournalNote(index)));
            }
            ui.NotebookIndex(entries, "");
            if (noteDraft != null)
            {
                ShowNoteEditor();
                return;
            }

            var f101 = State.f101Annotations;
            var summary = new StringBuilder();
            if (f101.HasLink) summary.AppendLine("Liên kết đã ghi · Thẻ AS-05B và dấu sử dụng ở giường 4.").AppendLine();
            if (State.f101.current == FormStatus.Refuted && State.f101Argument?.claimId == HypothesisService.ClaimUnreliableZero)
                summary.AppendLine("Kết luận đã ký trên F101\nChưa đủ căn cứ xác nhận số 0.").AppendLine();
            if (summary.Length == 0)
                summary.Append("Viết điều mình đang nghĩ, hoặc giữ lại một câu hỏi để kiểm tra sau.");
            var actions = new List<ActionChoice>
            {
                new("Viết giả thuyết", () => EditJournalNote(-1, JournalNoteKind.Hypothesis), State.journalNotes.Count < JournalNote.MaxNotes),
                new("Ghi điểm mâu thuẫn", () => EditJournalNote(-1, JournalNoteKind.Contradiction), State.journalNotes.Count < JournalNote.MaxNotes),
                new("Để lại câu hỏi", () => EditJournalNote(-1, JournalNoteKind.Question), State.journalNotes.Count < JournalNote.MaxNotes)
            };
            if (casework.CanRevise && !string.IsNullOrEmpty(f101.MarkedHotspot(ObservationRegistry.Bed4Card)) &&
                HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Traces))
                actions.Add(new ActionChoice(f101.HasLink ? "Tháo liên kết thẻ và dấu vết" : "Nối thẻ AS-05B với dấu vết giường 4",
                    () => SaveCheckpoint(casework.ToggleF101TraceLinkAsync, Journal)));
            ui.NotebookPage("Suy luận của Minh", summary.ToString(), actions.ToArray());
        }

        private void EditJournalNote(int index, JournalNoteKind kind = JournalNoteKind.Hypothesis, string source = null)
        {
            if (index == -1 && State.journalNotes.Count >= JournalNote.MaxNotes)
            {
                ui.Caption("Các trang ghi chú đã đầy.", 3f, true, aboveModal: true);
                return;
            }
            editNoteIndex = index;
            var saved = index >= 0 ? State.journalNotes[index] : null;
            noteDraft = new JournalNote
            {
                kind = saved?.kind ?? kind,
                source = saved?.source ?? source,
                related = saved?.related,
                text = saved?.text ?? ""
            };
            chooseNoteSource = 0;
            DrawJournal(JournalTab.Reasoning);
            ui.FocusNotebookWriting();
        }

        private void ShowNoteEditor()
        {
            var note = noteDraft;
            var actions = new List<ActionChoice>
            {
                new("Viết tiếp", ui.FocusNotebookWriting),
                new("Chọn nguồn", () => PickSource(1)),
                new("Nối nguồn thứ hai", () => PickSource(2), JournalNote.HasSource(State, note.source)),
                new("Lưu dòng này", () =>
                {
                    note.text = note.text?.Trim();
                    if (!note.ValidFor(State))
                    {
                        ui.FocusNotebookWriting();
                        return;
                    }
                    SaveCheckpoint(() => casework.WriteJournalNoteAsync(editNoteIndex, note), Done);
                }),
                new("Hủy", Done)
            };
            if (!string.IsNullOrEmpty(note.related))
                actions.Add(new ActionChoice("Tháo liên kết", () => { note.related = null; DrawJournal(JournalTab.Reasoning); }));
            if (editNoteIndex >= 0)
                actions.Add(new ActionChoice("Xóa dòng này", () => SaveCheckpoint(() => casework.WriteJournalNoteAsync(editNoteIndex, null), Done)));
            ui.NotebookEditor(NoteLabel(note.kind), SourceTitle(note.source) +
                (string.IsNullOrEmpty(note.related) ? "" : "  ↔  " + SourceTitle(note.related)),
                note.text, value => note.text = value, actions.ToArray());

            void PickSource(int slot)
            {
                chooseNoteSource = slot;
                DrawJournal(JournalTab.Reasoning);
            }
            void Done()
            {
                noteDraft = null;
                chooseNoteSource = 0;
                DrawJournal(JournalTab.Reasoning);
            }
        }

        private void DrawTimeline()
        {
            var body = new StringBuilder();
            if (JournalNote.HasSource(State, ObservationRegistry.Bed3Meds))
                body.AppendLine("17.11.2005 · 10:15  /  Phiếu thuốc giường 03").AppendLine("Giờ ghi khóa phiếu sau khi xuất viện.").AppendLine();
            if (JournalNote.HasSource(State, ObservationRegistry.Bed2Transfer))
                body.AppendLine("17.11.2005 · 19:20  /  Phiếu chuyển giường 02").AppendLine("Có hai chữ ký và nơi tiếp nhận.").AppendLine();
            if (JournalNote.HasSource(State, ObservationRegistry.ReceptionDrawerLog))
                body.AppendLine("Đêm 17–18.11.2005  /  Sổ phân ca").AppendLine("Lời ghi ca trực đông bệnh nhân.").AppendLine();
            bool matchedTime = CorroborationService.IsCorroborated(State);
            if (matchedTime)
                body.AppendLine("03:17  /  Mốc giờ đã đối chiếu")
                    .AppendLine("Đồng hồ và vết hằn trên thẻ cùng ghi giờ này. Sự kiện cụ thể vẫn để ngỏ.").AppendLine();
            if (State.Happened("E10a.SMSRead"))
            {
                body.AppendLine("18.11.2005 · Rạng sáng  /  Mẩu báo").AppendLine("Tin công khai về vụ cháy và 13 người tử vong.").AppendLine();
                body.AppendLine("17.11.2012 · 23:00  /  Tờ hẹn của Bảy").AppendLine("Tin nhắn sau đó báo hoãn cuộc gặp.");
            }
            if (State.Happened("CH01-T04") && voice.Transcript.Count > 0)
            {
                foreach (var line in voice.Transcript)
                {
                    if (line.LineId != "CH01-D04c") continue;
                    body.AppendLine().AppendLine("Lời Lan tại quầy").AppendLine(line.Text);
                    break;
                }
            }
            if (!matchedTime && (CorroborationService.HasClock(State) || CorroborationService.HasCardTimestamp(State)))
                body.AppendLine().AppendLine("03:17  /  Chưa xác định ngày").AppendLine("Còn cần đối chiếu nguồn ghi giờ.");
            if (body.Length == 0) body.Append("Chưa đọc được nguồn nào có mốc giờ.");
            var actions = new List<ActionChoice>();
            if (matchedTime || State.chapter == "CH01" && State.layer == Reality.B &&
                CorroborationService.HasClock(State) && CorroborationService.HasCardTimestamp(State))
                actions.Add(new ActionChoice(matchedTime ? "Đọc ghi chú về 03:17" : "So khớp giờ ghi chép", () => Exception0317Page(Journal)));
            ui.NotebookIndex(Array.Empty<NotebookEntry>(), "Mốc giờ theo những gì Minh đã đọc và quan sát.");
            ui.NotebookPage("Dòng thời gian", body.ToString(), actions.ToArray());
        }

        private void DrawDecisions()
        {
            var entries = new List<NotebookEntry>();
            foreach (var item in State.f101History)
            {
                entries.Add(new NotebookEntry($"Lần {item.revision} · {Status(item.decision)}",
                    () => DrawDecision(item)));
            }
            ui.NotebookIndex(entries, "Chưa ký phiếu nào.");
            if (State.f101History.Count > 0)
            {
                int revision = selectedDecisionRevision == 0 ? State.f101History.Count :
                    Mathf.Min(selectedDecisionRevision, State.f101History.Count);
                ShowDecision(State.f101History[revision - 1]);
            }
            else ui.NotebookPage("Phiếu bàn giao 101", "Các bản đã ký sẽ nằm ở đây.");
        }

        private void DrawDecision(FormRevisionEntry entry)
        {
            selectedDecisionRevision = entry.revision;
            DrawJournal(JournalTab.Decisions);
        }

        private void ShowDecision(FormRevisionEntry h)
        {
            string label = h.revision == 1 ? "Lần ký đầu" : "Lần sửa " + (h.revision - 1);
            var body = new StringBuilder(label).Append(" · ").AppendLine(Status(h.decision))
                .AppendLine("Lớp " + h.layer).AppendLine();
            var claim = HypothesisService.GetClaim(h.claimId);
            if (claim != null) body.AppendLine(claim.Label).AppendLine();
            if (h.evidenceIds?.Count > 0)
            {
                body.AppendLine("Căn cứ kèm phiếu");
                foreach (string id in h.evidenceIds)
                    body.AppendLine(ObservationRegistry.Get(id)?.Title ?? id);
                body.AppendLine();
            }
            if (h.consequenceObserved)
                body.AppendLine("Đã thấy tại giường 4").Append(h.decision switch
                {
                    FormStatus.Affirmed => "Vách ngăn che khu vực giường.",
                    FormStatus.Unverified => "Rèm vẫn để lửng, khoảng kê giường còn mở.",
                    _ => "Rèm dạt sang bên, lộ nền và mảng tường phía sau."
                });
            string copy = body.ToString();
            string imageId = h.decision switch
            {
                FormStatus.Affirmed => "F101_SignedA",
                FormStatus.Unverified => "F101_SignedB",
                _ => "F101_SignedC"
            };
            ui.NotebookPage("Phiếu bàn giao 101", copy, new ActionChoice("Xem bản đã ký", () =>
                ui.Show("F101 · " + label, "Bản bàn giao", copy,
                    new[] { new ActionChoice("Trở về lịch sử", Journal) }, Journal, Image(imageId))));
        }
    }
}
