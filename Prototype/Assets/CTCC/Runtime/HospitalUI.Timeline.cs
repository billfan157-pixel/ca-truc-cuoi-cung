using System;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalUI
    {
        internal void NotebookTimeline(ActionChoice left, ActionChoice right, ActionChoice context,
            string source, ActionChoice pinLeft, ActionChoice pinRight, ActionChoice pinContext, ActionChoice link,
            Action<string, int> drop, string feedback)
        {
            NotebookLabel(notebookRight, "Dòng thời gian", 31, 0, .901f, 1, .988f, bold: true);
            var first = NotebookButton(notebookRight, .025f, .657f, .459f, .847f, left, Paper, true, 22);
            var second = NotebookButton(notebookRight, .541f, .657f, .975f, .847f, right, Paper, true, 22);
            first.Rect.name = "Timeline left pin";
            NotebookLabel(notebookRight, "↔", 24, .447f, .703f, .553f, .798f,
                color: NoteInk, align: TextAnchor.MiddleCenter);
            var margin = NotebookButton(notebookRight, .025f, .541f, .975f, .635f, context, Paper, size: 20);
            NotebookLabel(notebookRight, source, 22, 0, .228f, 1, .499f, color: NoteInk, align: TextAnchor.UpperLeft);
            if (pinContext.Enabled) NotebookButton(notebookRight, .025f, .143f, .418f, .217f, pinContext, Paper, size: 18);
            else if (pinLeft.Enabled)
            {
                NotebookButton(notebookRight, .025f, .143f, .289f, .217f, pinLeft, Paper, size: 18);
                NotebookButton(notebookRight, .302f, .143f, .576f, .217f, pinRight, Paper, size: 18);
            }
            NotebookButton(notebookRight, .716f, .143f, .975f, .217f, link, Paper, true, 20);
            NotebookLabel(notebookRight, feedback ?? "Kéo mốc hoặc chọn thẻ để kẹp. Chọn thẻ đã kẹp để rút.", 18,
                0, .03f, 1, .115f, color: feedback == null ? NoteInk : MarkInk);
            dropSlip = (id, point) =>
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(first.Rect, point)) drop(id, 0);
                else if (RectTransformUtility.RectangleContainsScreenPoint(second.Rect, point)) drop(id, 1);
                else if (RectTransformUtility.RectangleContainsScreenPoint(margin.Rect, point)) drop(id, 2);
            };
        }
    }
}
