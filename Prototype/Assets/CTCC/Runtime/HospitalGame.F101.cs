using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private enum FormPage { Draft, Review, Seal }
        private F101Draft reportDraft = new();
        private FormPage formPage;
        private string formFeedback;
        private bool pickingLink;
        private string linkStart;
        private Action journalReturn;

        private void Form()
        {
            if (!casework.CanRevise) { CloseModal(); return; }
            // Core v3.6.2 (Phase 2): CH01 routes 100% of F101 interactions through the 3D AN02 clipboard (zero 2-column Journal modal!).
            if (State.chapter == "CH01")
            {
                OpenAn02Clipboard3D();
                return;
            }
            inspector?.Close();
            ui.ShowNotebook(-1, null, BackFromForm, Options.alternateBindings, false, clipboard: true, putDown: PutDownForm);
            DrawFormSources();
            if (formPage == FormPage.Draft)
            {
                var decisions = new[]
                {
                    FormField(FormStatus.Affirmed, "Xác nhận số 0 theo bản kê"),
                    FormField(FormStatus.Unverified, "Để ngỏ, chờ kiểm tra"),
                    FormField(FormStatus.Refuted, "Ghi nhận sai lệch trong bàn giao")
                };
                var claims = new List<ActionChoice>();
                if (reportDraft.Decision == FormStatus.Refuted)
                    foreach (var claim in HypothesisService.F101Claims)
                        claims.Add(new ActionChoice(Check(reportDraft.ClaimId == claim.Id) + claim.Label, () =>
                        {
                            reportDraft.ClaimId = claim.Id;
                            formFeedback = null;
                            Form();
                        }));

                ui.NotebookFormDraft(State.f101.revision == 0 ? "Phiếu chưa ký" : $"Bản hiện hành · Lần {State.f101.revision} · {Status(State.f101.current)}",
                    new ActionChoice(Check(State.f101Annotations.MarkedHotspot(ObservationRegistry.F101Sheet) == F101Annotations.Zero) +
                        "Số bệnh nhân chưa bàn giao   |   0", () =>
                        SaveCheckpoint(() => casework.ToggleF101MarkAsync(ObservationRegistry.F101Sheet, F101Annotations.Zero), Form),
                        CanMarkF101Document(ObservationRegistry.F101Sheet)),
                    decisions, claims, ReviewF101,
                    new ActionChoice("Nhấc tờ phiếu", () => WorldF101(scene.f101WallRenderer?.transform)), formFeedback,
                    reportDraft.Decision != FormStatus.Pending);
            }
            else
            {
                var submission = reportDraft.Submission(State.f101Annotations);
                var copy = new StringBuilder(FormNotation(reportDraft.Decision));
                if (submission != null)
                {
                    copy.AppendLine().AppendLine().AppendLine(HypothesisService.GetClaim(submission.claimId)?.Label);
                    copy.AppendLine().AppendLine("Kèm theo");
                    foreach (string id in submission.selectedEvidenceIds)
                        if (ObservationRegistry.Get(id) is ObservationSpec spec) copy.Append("• ").AppendLine(ShortSourceName(spec));
                }
                ui.NotebookFormReview(copy.ToString(), Image("F101"), formPage == FormPage.Seal, BackFromForm, ArmF101Seal,
                    () => StartCoroutine(SignF101(reportDraft.Decision, submission)), formFeedback);
            }
            ui.FinishNotebook();
        }

        private ActionChoice FormField(FormStatus decision, string copy) =>
            new(Check(reportDraft.Decision == decision) + copy, () =>
            {
                reportDraft.Decision = decision;
                formFeedback = null;
                Form();
            });

        private static string FormNotation(FormStatus decision) => decision switch
        {
            FormStatus.Affirmed => "Xác nhận 0 bệnh nhân chưa bàn giao theo bản kê.",
            FormStatus.Unverified => "Để ngỏ số bàn giao. Giữ phiếu chờ kiểm tra.",
            _ => "Ghi nhận sai lệch trong bàn giao."
        };

        private static string Check(bool marked) => marked ? "☑  " : "□  ";

        private void DrawFormSources()
        {
            var rows = new List<NotebookEntry>();
            bool drafting = formPage == FormPage.Draft;
            if (drafting)
                foreach (var record in State.observationRecords)
                {
                    if (!JournalNote.HasSource(State, record.id)) continue;
                    var spec = ObservationRegistry.Get(record.id);
                    if (spec == null) continue;
                    bool attached = reportDraft.Includes(spec.Id);
                    if (pickingLink && !attached) continue;
                    string label = (linkStart == spec.Id ? "↳  " : Check(attached)) + ShortSourceName(spec);
                    rows.Add(new NotebookEntry(label, () => ChooseFormSource(spec.Id), pickingLink ? null : spec.Id));
                }

            var link = State.f101Annotations;
            int markCount = link.marks != null ? link.marks.Count : 0;
            string linkLabel = link.HasLink
                ? $"{ClipLabel(link.linkFrom.observationId, "")}   ↔   {ClipLabel(link.linkTo.observationId, "")}"
                : pickingLink ? (linkStart == null ? "Chọn đầu nối thứ nhất." : "Chọn đầu nối thứ hai.") :
                    markCount > 0 ? $"Đã đánh dấu {markCount} mục trên giấy · Chọn nguồn để kẹp hoặc nối." :
                    "Kéo nguồn sang phiếu hoặc chọn để kẹp.";
            ui.NotebookIndex(rows, drafting ? "Nguồn đã xem sẽ xuất hiện ở đây." : "",
                drafting ? "Nguồn kẹp vào phiếu" : "Bản gốc F101", .268f);
            ui.NotebookFormSourceControls($"{reportDraft.SourceCount} nguồn đã chọn", linkLabel, drafting,
                new ActionChoice("Mở trang đối chiếu", CompareFromForm, drafting),
                new ActionChoice(pickingLink ? "Hủy thao tác nối" : link.HasLink ? "Tháo liên kết" : "Nối hai nguồn",
                    ChangeFormLink, drafting && (pickingLink || link.HasLink || reportDraft.SourceCount >= 2)),
                id =>
                {
                    if (!drafting || pickingLink || !PinF101Source(id)) return;
                    Form();
                });
        }

        private static string ShortSourceName(ObservationSpec spec) => spec.Id switch
        {
            ObservationRegistry.F101Sheet => "F101 · Phiếu bàn giao",
            ObservationRegistry.Bed4Card => "AS-05B · Thẻ giường 4",
            ObservationRegistry.Bed4Traces => "Dấu sử dụng giường 4",
            _ => spec.Title
        };

        private bool PinF101Source(string id)
        {
            if (!casework.CanRevise || !JournalNote.HasSource(State, id) || ObservationRegistry.Get(id) == null) return false;
            reportDraft.Attach(id);
            formFeedback = null;
            return true;
        }

        private void ChooseFormSource(string id)
        {
            formFeedback = null;
            if (!pickingLink)
            {
                reportDraft.Toggle(id);
                Form();
                return;
            }
            if (linkStart == null || linkStart == id)
            {
                linkStart = linkStart == id ? null : id;
                Form();
                return;
            }
            var first = FormEndpoint(linkStart);
            var second = FormEndpoint(id);
            linkStart = null;
            pickingLink = false;
            StartCoroutine(PublishFormLink(first, second));
        }

        private EvidenceAnnotation FormEndpoint(string id) =>
            new(id, id == ObservationRegistry.Bed4Traces
                ? F101Annotations.TraceGroup : State.f101Annotations.MarkedHotspot(id));

        private void ChangeFormLink()
        {
            formFeedback = null;
            linkStart = null;
            if (State.f101Annotations.HasLink) StartCoroutine(PublishFormLink(default, default));
            else
            {
                pickingLink = !pickingLink;
                Form();
            }
        }

        private IEnumerator PublishFormLink(EvidenceAnnotation first, EvidenceAnnotation second)
        {
            if (busy) yield break;
            busy = true;
            ui.ModalInputBlocked = true;
            RefreshControls();
            try
            {
                while (savingDialogue) yield return null;
                var task = casework.SetF101LinkFromUiAsync(first, second);
                while (!task.IsCompleted) yield return null;
                if (task.IsFaulted || task.Result == CommitResult.SaveUnavailable)
                    formFeedback = "Chưa lưu được liên kết. Mình có thể thử lại.";
                else if (task.Result != CommitResult.Applied)
                    formFeedback = "Chưa nối được: cần đánh dấu chi tiết trên giấy trước khi nối hai nguồn.";
            }
            finally
            {
                busy = false;
                ui.ModalInputBlocked = false;
                RefreshControls();
            }
            Form();
        }

        private void CompareFromForm()
        {
            if (!IsJournalAllowedInBackpack) return;
            pickingLink = false;
            linkStart = null;
            journalReturn = Form;
            comparingDocuments = true;
            DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
        }

        private void ReviewF101()
        {
            var evaluation = HypothesisService.EvaluateF101LiveCommit(State, reportDraft.Decision, reportDraft.Submission(State.f101Annotations));
            formFeedback = evaluation.Accepted ? null : evaluation.Message;
            if (evaluation.Accepted)
            {
                formPage = FormPage.Review;
                pickingLink = false;
                linkStart = null;
            }
            Form();
        }

        private void BackFromForm()
        {
            formFeedback = null;
            if (formPage != FormPage.Draft || pickingLink)
            {
                formPage = FormPage.Draft;
                pickingLink = false;
                linkStart = null;
                Form();
                return;
            }
            PutDownForm();
        }

        private void PutDownForm()
        {
            formPage = FormPage.Draft;
            pickingLink = false;
            formFeedback = linkStart = null;
            reportDraft.Decision = FormStatus.Pending;
            reportDraft.ClaimId = null;
            CloseModal();
        }

        private void ArmF101Seal()
        {
            if (busy || formPage != FormPage.Review) return;
            formPage = FormPage.Seal;
            Form();
        }

        private IEnumerator SignF101(FormStatus choice, DeductionSubmission submission)
        {
            if (busy || formPage != FormPage.Seal) yield break;
            if (!casework.CanRevise || !transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard))
            {
                formFeedback = "Mình cần đứng cạnh bảng kẹp để ký.";
                Form();
                yield break;
            }

            int previousRevision = State.f101.revision;
            var paper = scene.f101WallRenderer;
            Vector3 paperPosition = paper != null ? paper.bounds.center : player.Feet + Vector3.up;
            bool close = false;
            busy = true;
            ui.ModalInputBlocked = true;
            ui.F101SignatureFeedback("Đang ghi bản lưu…");
            RefreshControls();
            try
            {
                while (savingDialogue || streaming) yield return null;
                var task = casework.SubmitF101DetailedAsync(choice, submission, player.Feet);
                while (!task.IsCompleted) yield return null;
                if (task.IsFaulted) Debug.LogException(task.Exception);
                var result = task.IsFaulted || task.IsCanceled ? CommitResult.SaveUnavailable : task.Result.Result;
                if (result != CommitResult.Applied)
                {
                    formFeedback = result switch
                    {
                        CommitResult.PadRequired => task.Result.Evaluation.Message,
                        CommitResult.Obstructed => "Có vật cản cạnh bảng kẹp. Bản trước vẫn còn nguyên.",
                        _ => "Chưa ghi được bản lưu. Nét ký và dấu vẫn đang chờ."
                    };
                }
                else
                {
                    close = true;
                    if (State.f101.revision != previousRevision)
                    {
                        dreadDirector?.NotifyF101Signing();
                        voice?.StopCurrent();
                        scene.atmosphere?.From("signaturePen", paperPosition);
                        ui.Caption("[Tiếng ngòi bút trên giấy]", .6f, aboveModal: true);
                        yield return ui.RevealF101Ink(Options.reduceMotion);
                        scene.atmosphere?.From("stamp", paperPosition);
                        scene.atmosphere?.LeaveSilenceAfterStamp(1.25f);
                        ui.Caption("[Dấu cao su chạm giấy]", 1.1f, aboveModal: true);
                        yield return ui.RevealF101Seal(Options.reduceMotion);
                        StartCoroutine(PlayF101Signature(State.f101.current, State.f101.revision));
                    }
                }
            }
            finally
            {
                ui.ModalInputBlocked = false;
                busy = false;
                RefreshControls();
            }
            if (close)
            {
                formPage = FormPage.Draft;
                reportDraft = new F101Draft();
                CloseModal();
            }
            else Form();
        }
    }
}
