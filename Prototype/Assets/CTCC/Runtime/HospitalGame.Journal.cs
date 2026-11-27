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
        private string fieldSelection, documentSelection;
        private readonly PaperPlacement[] comparison = new PaperPlacement[2];
        private bool comparingDocuments, returnNoteToPaper;
        private int editNoteIndex = -1, chooseNoteSource;
        private int selectedDecisionRevision;
        private JournalNote noteDraft;

        private void Journal() => DrawJournal(journalTab, pageTurn: !ui.IsModal);

        private void ResetNotebookSession()
        {
            journalTab = JournalTab.Field;
            fieldSelection = documentSelection = null;
            comparison[0] = comparison[1] = null;
            comparingDocuments = returnNoteToPaper = false;
            reportDraft = new F101Draft();
            formPage = FormPage.Draft;
            formFeedback = linkStart = null;
            pickingLink = false;
            journalReturn = null;
            noteDraft = null;
            chooseNoteSource = 0;
            selectedDecisionRevision = 0;
            timelineDraft = default;
            timelineFeedback = timelineSource = null;
            if (scene.PedalBin != null) scene.PedalBin.SetOpen(false, immediate: true);
        }

        private void DrawJournal(JournalTab tab, bool pageTurn = false)
        {
            // Core v3.6.2 Ch01JournalPolicy (Phase 2 100% lock): 5-tab Journal is completely disabled in CH01.
            // CH02-CH08 retain full 5-tab Journal unchanged.
            if (State != null && State.chapter == "CH01")
            {
                OpenBackpack(BackpackCompartment.PocketNote);
                return;
            }
            if (pageTurn) scene.atmosphere?.From("paperFlip", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.44f);
            journalTab = tab;
            ui.ShowNotebook((int)tab, index =>
            {
                noteDraft = null;
                chooseNoteSource = 0;
                DrawJournal((JournalTab)index, pageTurn: true);
            }, CloseNotebook, Options.alternateBindings, pageTurn && !Options.reduceMotion,
                wideComparison: tab == JournalTab.Documents && comparingDocuments, returnToClipboard: journalReturn != null);

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
                    .Append("\n\n").Append(selected.id == ObservationRegistry.Bed4Traces
                        ? ObservationSynopsis(observation, selected) : observation.FactSummary);
                AppendAuthoredNotes(body, selected.id);
                ui.NotebookPage(observation.Title, body.ToString(),
                    new ActionChoice("Ghi bên lề", () => EditJournalNote(-1, JournalNoteKind.Annotation, selected.id)),
                    new ActionChoice("Kẹp nguồn vào F101", () => PinFromJournal(selected.id), casework.CanRevise));
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
                if (State.Happened("CH05-T05")) copy += "\n\nĐã qua ký ức sơ tán Khu B (AN05 · 6/6 người qua cầu thang S1).";
                if (State.Happened("PZ07.Aligned")) copy += "\n\nĐã đối chiếu sơ đồ 2005 và bản hoàn công 06/2004 (E05) tại LOC09.";
                if (State.Has(EvidenceRegistry.SealedWallPhoto)) copy += "\n\nĐã kiểm chứng tường xây bít 2004 ngoài hố kỹ thuật LOC11 (E05b).";
                if (State.Happened("ValveSabotageFound")) copy += "\n\nGhi chú tùy chọn LOC09: vết cắt cơ học trên ổ khóa van dung môi và chốt chèn tấm chắn gió (nghi vấn đối chiếu E09, chưa đủ xác định thủ phạm).";
                if (GateService.G05(State)) copy += "\n\nH03 đã xác minh · Cửa Phòng Hành chính (LOC08 · G05) đã mở.";
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
            "LOC04" => "Khu Cấp cứu",
            "LOC05" => "Khu B · Tầng 2",
            "LOC06" => "Phòng trực Khu B",
            "LOC07" => "Kho hồ sơ · Tầng 3",
            "LOC08" => "Phòng Hành chính · Tầng 3",
            "LOC09" => "Khu kỹ thuật B1",
            "LOC10" => "Nhà xác B1",
            "LOC11" => "Hành lang bao B1 · Cửa Đông",
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
            if (!comparingDocuments && State.Happened("E10a.SMSRead"))
                entries.Add(new NotebookEntry("Bì thư và tin nhắn của Bảy", () =>
                {
                    documentSelection = "E10a";
                    DrawJournal(JournalTab.Documents);
                }));
            foreach (var paper in docs)
                entries.Add(new NotebookEntry(comparingDocuments ? ClipLabel(paper.Id, paper.Title) : paper.Title, () =>
                {
                    if (comparingDocuments) PlaceDocument(paper.Id, comparison[0] == null ? 0 : 1);
                    else
                    {
                        documentSelection = paper.Id;
                        DrawJournal(JournalTab.Documents);
                    }
                }, paper.Id));

            if (comparingDocuments)
            {
                ui.NotebookCompare(PlaceDocument,
                    new ActionChoice("Đổi hai tờ", SwapDocuments, comparison[0] != null || comparison[1] != null),
                    new ActionChoice("Về tập tài liệu", () => { comparingDocuments = false; DrawJournal(JournalTab.Documents); }));
                ui.NotebookIndex(entries, "Chưa đọc tờ nào.", "Tập giấy");
                for (int side = 0; side < comparison.Length; side++) DrawPaper(side);
                return;
            }

            ui.NotebookIndex(entries, "Tài liệu sẽ có ở đây sau khi đọc.", "Tập giấy");
            var actions = new List<ActionChoice>();
            var spec = docs.Find(d => d.Id == documentSelection);
            string title = "Tài liệu đã đọc";
            string copy = "Chọn tài liệu ở mép sổ để xem bản sao.";
            Texture image = null;
            if (documentSelection == "E10a" && State.Happened("E10a.SMSRead"))
            {
                title = "Phong bì chú Bảy gửi lại";
                copy = "Hộp thư sắt trụ cổng phụ · 17/11/2012\n\nGiấy nhắn ngoài bì, thư cũ năm 2005 của Lan, mẩu báo ngày 19/11/2005 và chìa khóa cũ.";
                actions.Add(new ActionChoice("Mở bì thư", Letter));
                actions.Add(new ActionChoice("Ghi bên lề", () => EditJournalNote(-1, JournalNoteKind.Annotation, "E10a")));
            }
            else if (spec != null)
            {
                title = spec.Title;
                image = Image(spec.FrontTexture);
                var record = State.FindObservation(spec.Id);
                var body = new StringBuilder(spec.ProvenanceLabel).Append("\n\n").Append(ObservationSynopsis(spec, record));
                if (State.f101Annotations.MarkedHotspot(spec.Id) is string field && !string.IsNullOrEmpty(field))
                    body.Append("\n\nNét bút · ").Append(AnnotationLabel(field));
                AppendAuthoredNotes(body, spec.Id);
                copy = body.ToString();
                actions.Add(new ActionChoice("Cầm bản sao", () => OpenObservationInspector(spec, Journal)));
                actions.Add(new ActionChoice("Ghi bên lề", () => EditJournalNote(-1, JournalNoteKind.Annotation, spec.Id)));
                actions.Add(new ActionChoice("Kẹp sang trái", () => PlaceDocument(spec.Id, 0)));
                actions.Add(new ActionChoice("Kẹp sang phải", () => PlaceDocument(spec.Id, 1)));
                if (casework.CanRevise) actions.Add(new ActionChoice("Kẹp nguồn vào F101", () => PinFromJournal(spec.Id)));
            }
            actions.Add(new ActionChoice("Mở trang đối chiếu", () => { comparingDocuments = true; DrawJournal(JournalTab.Documents, true); }, docs.Count > 0));
            ui.NotebookPage(title, copy, image, actions.ToArray());
        }

        private void DrawPaper(int side)
        {
            var placement = comparison[side];
            var spec = ObservationRegistry.Get(placement?.Id);
            if (spec == null)
            {
                ui.NotebookPaper(side, null, side == 0 ? "Kẹp trái" : "Kẹp phải", null, "", Array.Empty<DocumentHotspot>(),
                    Array.Empty<JournalPaperMark>(), null, null, null, null, null, null);
                return;
            }
            string id = spec.Id;
            var hotspots = new List<DocumentHotspot>();
            if (!placement.Back && CanMarkF101Document(id))
                foreach (var field in AnnotationFields(id))
                    hotspots.Add(new DocumentHotspot(field.Uv, State.f101Annotations.MarkedHotspot(id) == field.Id,
                        PaperFieldLabel(field.Id), () => SaveCheckpoint(() => casework.ToggleF101MarkAsync(id, field.Id), Journal)));

            var pins = new List<JournalPaperMark>();
            foreach (var note in State.journalNotes)
                if (note.source == id && note.paperMark.placed && note.paperMark.back == placement.Back) pins.Add(note.paperMark);

            string transcript = spec.ProvenanceLabel + "\n\n" +
                (placement.Back ? ObservationBackText(id) ?? spec.FactSummary : spec.FactSummary);
            if (placement.Back && State.FindObservation(id)?.HasDetail(CorroborationService.RakingDetailId) == true)
                transcript += "\n\n" + ObservationRakingText(id);

            Action flip = !spec.HasBackSide ? null : () =>
            {
                placement.Back = !placement.Back;
                if (placement.Back) SaveCheckpoint(() => casework.MarkObservationDetailAsync(id, "BACK", flippedBack: true),
                    Journal, onFailure: () => { placement.Back = false; Journal(); });
                else Journal();
            };
            ui.NotebookPaper(side, placement, ClipLabel(id, "") + (placement.Back ? " · Mặt sau" : " · Mặt trước"),
                Image(placement.Back ? spec.BackTexture : spec.FrontTexture), transcript, hotspots.ToArray(), pins, flip,
                () => OpenObservationInspector(spec, Journal),
                () => { placement.LargePrint = !placement.LargePrint; Journal(); },
                () => { comparison[side] = null; Journal(); },
                State.journalNotes.Count < JournalNote.MaxNotes
                    ? uv => EditJournalNote(-1, JournalNoteKind.Annotation, id,
                        new JournalPaperMark { placed = true, back = placement.Back, x = uv.x, y = uv.y }, returnToPaper: true) : null,
                casework.CanRevise ? () => PinFromJournal(id) : null);
        }

        private static string PaperFieldLabel(string field) => field switch
        {
            F101Annotations.Table => "Bảng kê",
            F101Annotations.Zero => "Dòng số 0",
            F101Annotations.CardStatus => "Tình trạng",
            _ => "Ô ký nhận"
        };

        private void PlaceDocument(string id, int side)
        {
            if (!JournalNote.HasSource(State, id) || ObservationRegistry.Get(id)?.IsDocument != true) return;
            int other = 1 - side;
            if (comparison[other]?.Id == id)
                (comparison[0], comparison[1]) = (comparison[1], comparison[0]);
            else if (comparison[side]?.Id != id)
                comparison[side] = new PaperPlacement(id);
            comparingDocuments = true;
            DrawJournal(JournalTab.Documents);
        }

        private void SwapDocuments()
        {
            (comparison[0], comparison[1]) = (comparison[1], comparison[0]);
            Journal();
        }

        private void CloseNotebook()
        {
            noteDraft = null;
            chooseNoteSource = 0;
            var back = journalReturn;
            journalReturn = null;
            if (back == null) CloseModal();
            else back();
        }

        private void PinFromJournal(string id)
        {
            if (PinF101Source(id))
                ui.Caption("Đã kẹp nguồn vào phiếu nháp F101.", 3, aboveModal: true);
        }

        private static string ObservationSynopsis(ObservationSpec spec, ObservationRecord record)
        {
            string facts = spec.FactSummary;
            if (spec.Id == ObservationRegistry.Bed4Traces && record != null)
            {
                var found = new List<string>(2);
                if (record.HasDetail(BlanketTrace)) found.Add(TraceDescription(BlanketTrace));
                if (record.HasDetail(SlipperTrace)) found.Add(TraceDescription(SlipperTrace));
                if (found.Count > 0) facts = string.Join("\n", found);
            }
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
            ObservationRegistry.Bed4Traces => "Dấu sử dụng giường 4",
            ObservationRegistry.Bed2Transfer => "Phiếu chuyển giường 02",
            ObservationRegistry.Bed3Meds => "Phiếu thuốc giường 03",
            ObservationRegistry.ShiftGuide => "Quy chế AS-05",
            ObservationRegistry.WardOverflowNotice => "Thông báo kê giường tạm",
            ObservationRegistry.ReceptionDrawerLog => "Sổ phân ca",
            _ => ObservationRegistry.Get(id)?.Title ?? empty
        };

        private static string NoteLabel(JournalNoteKind kind) => kind switch
        {
            JournalNoteKind.Annotation => "Ghi bên lề",
            JournalNoteKind.Contradiction => "Điểm mâu thuẫn",
            JournalNoteKind.Question => "Chưa xác thực",
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
                        if (chooseNoteSource == 1)
                        {
                            if (noteDraft.source != id) noteDraft.paperMark = default;
                            noteDraft.source = id;
                        }
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
            ui.NotebookPage("Suy luận của Minh", summary.ToString(), actions.ToArray());
        }

        private void EditJournalNote(int index, JournalNoteKind kind = JournalNoteKind.Hypothesis, string source = null,
            JournalPaperMark mark = default, bool returnToPaper = false)
        {
            if (index == -1 && State.journalNotes.Count >= JournalNote.MaxNotes)
            {
                ui.Caption("Các trang ghi chú đã đầy.", 3f, true, aboveModal: true);
                return;
            }
            editNoteIndex = index;
            returnNoteToPaper = returnToPaper;
            var saved = index >= 0 ? State.journalNotes[index] : null;
            noteDraft = new JournalNote
            {
                kind = saved?.kind ?? kind,
                source = saved?.source ?? source,
                related = saved?.related,
                text = saved?.text ?? "",
                paperMark = saved?.paperMark ?? mark
            };
            chooseNoteSource = 0;
            DrawJournal(JournalTab.Reasoning);
            ui.FocusNotebookWriting();
        }

        private void ShowNoteEditor()
        {
            var note = noteDraft;
            var footer = new List<ActionChoice>
            {
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
            bool hasSource = JournalNote.HasSource(State, note.source);
            bool linked = JournalNote.HasSource(State, note.related);
            if (casework.CanRevise && (ObservationRegistry.Get(note.source) != null || ObservationRegistry.Get(note.related) != null))
                footer.Add(new ActionChoice("Kẹp nguồn vào F101", () =>
                {
                    PinF101Source(note.source);
                    PinF101Source(note.related);
                    ui.Caption("Đã kẹp nguồn của ghi chú vào phiếu nháp F101.", 3, aboveModal: true);
                }));
            ActionChoice status = default;
            if (note.kind != JournalNoteKind.Annotation)
            {
                var next = note.kind switch
                {
                    JournalNoteKind.Hypothesis => JournalNoteKind.Question,
                    JournalNoteKind.Question => JournalNoteKind.Contradiction,
                    _ => JournalNoteKind.Hypothesis
                };
                status = new ActionChoice("Đổi sang " + NoteLabel(next).ToLowerInvariant(), () =>
                {
                    note.kind = next;
                    DrawJournal(JournalTab.Reasoning);
                });
            }
            if (editNoteIndex >= 0)
                footer.Add(new ActionChoice("Xóa dòng này", () => SaveCheckpoint(() => casework.WriteJournalNoteAsync(editNoteIndex, null), Done)));
            ui.NotebookEditor(NoteLabel(note.kind), note.text, value => note.text = value,
                chooseFirst: new ActionChoice(hasSource ? "Chọn nguồn · " + SourceTitle(note.source) : "Chọn nguồn", () => PickSource(1)),
                chooseSecond: new ActionChoice(linked ? "Nối nguồn thứ hai · " + SourceTitle(note.related) : "Nối nguồn thứ hai",
                    () => PickSource(2), hasSource),
                openFirst: new ActionChoice("Mở nguồn đang kẹp", () => OpenNoteSource(note.source), hasSource),
                openSecond: new ActionChoice("Mở nguồn thứ hai", () => OpenNoteSource(note.related), linked),
                unlink: new ActionChoice("Tháo liên kết", () => { note.related = null; DrawJournal(JournalTab.Reasoning); }, linked),
                status: status, footer: footer);

            void PickSource(int slot)
            {
                chooseNoteSource = slot;
                DrawJournal(JournalTab.Reasoning);
            }
            void Done()
            {
                noteDraft = null;
                chooseNoteSource = 0;
                DrawJournal(returnNoteToPaper ? JournalTab.Documents : JournalTab.Reasoning);
            }
        }

        private void OpenNoteSource(string id)
        {
            var source = ObservationRegistry.Get(id);
            Action back = () => DrawJournal(JournalTab.Reasoning);
            if (source?.IsDocument == true) OpenObservationInspector(source, back);
            else if (source != null || id == "E10a")
                ui.Show("NGUỒN CỦA GHI CHÚ", SourceTitle(id),
                    source?.FactSummary ?? "Tờ hẹn lúc 23 giờ, thư cũ, mẩu báo và tin nhắn báo hoãn của Bảy.",
                    new[] { new ActionChoice("Về ghi chú", back) }, back);
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
            ui.NotebookPage("Phiếu bàn giao 101", copy, Image(imageId));
        }
    }
}
