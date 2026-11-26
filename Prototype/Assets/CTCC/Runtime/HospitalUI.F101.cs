using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CTCC
{
    public sealed partial class HospitalUI
    {
        private RectTransform signatureMark, signatureMask, signingPen, signatureSeal;
        private Text signatureName, signatureStamp, signatureFeedback, signatureStage, signatureOwner;
        private RectTransform signatureEdit;
        private float signatureStart;
        private bool drawingSignature;

        internal void NotebookFormSourceControls(string count, string link, bool editing, ActionChoice compare,
            ActionChoice editLink, Action<string> attach)
        {
            if (!editing) return;
            NotebookLabel(notebookLeft, count, 17, 0, .214f, 1, .260f, bold: true, color: NoteInk);
            NotebookLabel(notebookLeft, link, 15, 0, .148f, 1, .214f, color: NoteInk);
            NotebookButton(notebookLeft, .041f, .068f, .488f, .140f, compare, PaperLight, true, 15);
            NotebookButton(notebookLeft, .506f, .068f, .958f, .140f, editLink, PaperLight, true, 15);
            dropSlip = (id, point) =>
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(notebookRight, point))
                    attach(id);
            };
        }

        internal void NotebookFormDraft(string revision, ActionChoice zero, IReadOnlyList<ActionChoice> decisions,
            IReadOnlyList<ActionChoice> claims, Action review, ActionChoice lift, string feedback, bool ready)
        {
            NotebookFormHeader(revision);
            NotebookButton(notebookRight, .767f, .908f, .976f, .978f, lift, Paper, true, 17);
            NotebookLabel(notebookRight, "GIƯỜNG   /   GHI NHẬN TRONG BẢN KÊ", 15, 0, .838f, 1, .882f, bold: true, color: NoteInk);
            NotebookLabel(notebookRight, "01   Đã xuất viện, dọn giường ngày 16/11\n02   Chuyển Nội B lúc 19g20, đủ hai chữ ký\n03   Xuất viện lúc 10g15, khóa phiếu thuốc ngày 17/11",
                16, 0, .728f, 1, .836f);
            NotebookButton(notebookRight, .022f, .668f, .977f, .722f, zero, Paper, size: 18);
            NotebookLabel(notebookRight, "GHI NHẬN CỦA NGƯỜI TRỰC", 15, 0, .618f, 1, .662f, bold: true, color: NoteInk);
            for (int i = 0; i < decisions.Count; i++)
            {
                float y = .562f - i * .055f;
                NotebookButton(notebookRight, .022f, y, .977f, y + .050f, decisions[i], PaperLight, size: 19);
            }
            if (claims.Count != 0)
            {
                NotebookLabel(notebookRight, "Lời ghi kèm phiếu", 16, 0, .398f, 1, .440f, bold: true, color: NoteInk);
                for (int i = 0; i < claims.Count; i++)
                {
                    float y = .340f - i * .055f;
                    NotebookButton(notebookRight, .022f, y, .977f, y + .050f, claims[i], PaperLight, size: 15).Label.color = NoteInk;
                }
            }

            NotebookLabel(notebookRight, feedback ?? "", 16, 0, .014f, .736f, .106f, color: MarkInk);
            NotebookButton(notebookRight, .751f, .022f, .976f, .096f,
                new ActionChoice("Đọc lại trên phiếu", review, ready), Paper, true, 16);
        }

        internal void NotebookFormReview(string copy, Texture original, bool sealArmed, Action back, Action sign, Action seal,
            string feedback)
        {
            signatureStage = NotebookFormHeader("Bản chờ ký");
            NotebookIllustration(notebookLeft, original, .056f, .091f, .948f, .855f);
            NotebookLabel(notebookRight, copy, 24, 0, .42f, 1, .86f, color: NoteInk, align: TextAnchor.UpperLeft);
            signatureOwner = NotebookLabel(notebookRight, sealArmed ? "Nét ký đang chờ đóng dấu" : "N. H. MINH   /   NGƯỜI GHI",
                17, 0, .284f, .68f, .347f, color: NoteInk);
            NotebookLabel(notebookRight, "DẤU CA TRỰC", 17, .706f, .284f, .988f, .347f, color: NoteInk, align: TextAnchor.MiddleCenter);
            var signLine = NotebookButton(notebookRight, .022f, .167f, .681f, .274f,
                new ActionChoice(sealArmed ? "" : "Kéo bút qua dòng ký   ·   Enter để ký", sign, !sealArmed), Paper, size: 19);
            signatureFeedback = NotebookLabel(notebookRight, feedback ?? "", 18, .213f, .02f, .987f, .129f, color: MarkInk);
            signatureEdit = NotebookButton(notebookRight, .023f, .048f, .215f, .121f,
                new ActionChoice("Sửa phần ghi", back), Paper, true, 18).Rect;
            var stamp = NotebookButton(notebookRight, .723f, .159f, .976f, .275f,
                new ActionChoice(sealArmed ? "Đóng dấu" : "Chờ chữ ký", seal, sealArmed), Paper, true, 22);
            notebookDefaultFocus = sealArmed ? stamp.Button : signLine.Button;

            if (sealArmed)
            {
                EnsureFormInk();
                signatureMark.SetAsLastSibling();
                signatureMark.gameObject.SetActive(true);
                signatureName.fontSize = Scaled(40);
                signatureName.color = new Color(NoteInk.r, NoteInk.g, NoteInk.b, .38f);
                ShowInkFraction(1, false);
                return;
            }

            if (signLine.SignatureDrag == null)
                signLine.SignatureDrag = signLine.Rect.gameObject.AddComponent<NotebookDrag>();
            var drag = signLine.SignatureDrag;
            drag.Begin = point =>
            {
                drawingSignature = !ModalInputBlocked && !sealArmed;
                signatureStart = point.x;
                if (!drawingSignature) return;
                EnsureFormInk();
                signatureMark.SetAsLastSibling();
                signatureMark.gameObject.SetActive(true);
                signatureName.color = new Color(NoteInk.r, NoteInk.g, NoteInk.b, .42f);
                ShowInkFraction(.07f, true);
            };
            drag.Move = point =>
            {
                if (!drawingSignature || ModalInputBlocked) return;
                ShowInkFraction(Mathf.Clamp01((point.x - signatureStart) / (Screen.width * .155f)), true);
            };
            drag.End = point =>
            {
                if (!drawingSignature) return;
                drawingSignature = false;
                signatureMark.gameObject.SetActive(false);
                if (!ModalInputBlocked && point.x - signatureStart > Screen.width * .05f) sign();
            };
        }

        private Text NotebookFormHeader(string status)
        {
            NotebookLabel(notebookRight, "Phiếu bàn giao ca đêm", 27, 0, .934f, .755f, .996f, bold: true);
            NotebookLabel(notebookLeft, "An Sinh  /  18.11.2005  /  Mẫu AS-05 · F101", 14, 0, .010f, 1, .056f, color: NoteInk);
            return NotebookLabel(notebookRight, status, 15, 0, .894f, .755f, .936f, color: NoteInk);
        }

        private void EnsureFormInk()
        {
            if (signatureMark != null) return;
            signatureMark = Rect("F101 ink", notebookRight, new Vector2(.031f, .169f), new Vector2(.66f, .266f));
            signatureMask = Rect("Revealing handwriting", signatureMark, Vector2.zero, new Vector2(0, 1));
            signatureMask.pivot = new Vector2(0, .5f);
            signatureMask.gameObject.AddComponent<RectMask2D>();
            signatureName = FloatText("Signature", signatureMask, new Vector2(0, .5f), new Vector2(16, -4),
                new Vector2(300, 64), 40, NoteInk, TextAnchor.MiddleLeft);
            signatureName.fontStyle = FontStyle.Italic;
            signatureName.text = "N. H. Minh";
            signingPen = Fixed("Fountain pen", signatureMark, new Vector2(0, .52f), new Vector2(0, 7), new Vector2(71, 7));
            Graphic(signingPen, BookInk, false);
            signingPen.localRotation = Quaternion.Euler(0, 0, 52);
            signatureSeal = Rect("F101 seal", notebookRight, new Vector2(.723f, .159f), new Vector2(.976f, .275f),
                new Vector2(8, 5), new Vector2(-8, -5));
            Graphic(signatureSeal, new Color(.89f, .77f, .64f), false);
            signatureStamp = FillText("Published stamp", signatureSeal, 25, MarkInk, TextAnchor.MiddleCenter, true);
            signatureStamp.text = "ĐÃ GHI SỔ";
            signatureSeal.gameObject.SetActive(false);
        }

        private void ShowInkFraction(float fraction, bool pen)
        {
            float width = Scaled(255) * fraction;
            signatureMask.sizeDelta = new Vector2(width, 0);
            signingPen.anchoredPosition = new Vector2(width, 0);
            signingPen.gameObject.SetActive(pen);
        }

        internal void F101SignatureFeedback(string copy)
        {
            if (signatureFeedback != null) signatureFeedback.text = copy;
        }

        internal IEnumerator RevealF101Ink(bool reducedMotion)
        {
            EnsureFormInk();
            signatureMark.SetAsLastSibling();
            signatureMark.gameObject.SetActive(true);
            signatureName.fontSize = Scaled(40);
            signatureName.color = NoteInk;
            signatureStage.text = "Đã lưu";
            signatureOwner.text = "N. H. MINH   /   NGƯỜI GHI";
            signatureEdit.gameObject.SetActive(false);
            F101SignatureFeedback("");
            if (!reducedMotion)
                for (float t = 0; t < .43f; t += Time.unscaledDeltaTime)
                {
                    ShowInkFraction(t / .43f, true);
                    yield return null;
                }
            ShowInkFraction(1, false);
        }

        internal IEnumerator RevealF101Seal(bool reducedMotion)
        {
            signatureStamp.fontSize = Scaled(25);
            signatureSeal.SetAsLastSibling();
            signatureSeal.gameObject.SetActive(true);
            if (!reducedMotion)
                for (float t = 0; t < .16f; t += Time.unscaledDeltaTime)
                {
                    signatureSeal.localScale = Vector3.one * Mathf.Lerp(1.14f, 1, t / .16f);
                    yield return null;
                }
            signatureSeal.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(.34f);
        }

        private void HideFormInk()
        {
            drawingSignature = false;
            signatureFeedback = null;
            if (signatureMark == null) return;
            signatureMark.gameObject.SetActive(false);
            signatureSeal.gameObject.SetActive(false);
        }
    }
}
