using System.Collections.Generic;

namespace CTCC
{
    internal sealed class F101Draft
    {
        public FormStatus Decision;
        public string ClaimId;
        private readonly List<string> sources = new();

        public int SourceCount => sources.Count;
        public bool Includes(string id) => sources.Contains(id);

        public void Attach(string id)
        {
            if (!Includes(id)) sources.Add(id);
        }

        public void Toggle(string id)
        {
            if (!sources.Remove(id)) sources.Add(id);
        }

        public DeductionSubmission Submission(F101Annotations journal)
        {
            if (Decision != FormStatus.Refuted) return null;
            var annotations = new F101Annotations();
            foreach (var mark in journal.marks)
                if (Includes(mark.observationId)) annotations.marks.Add(mark);
            if (journal.HasLink && Includes(journal.linkFrom.observationId) && Includes(journal.linkTo.observationId))
            {
                annotations.linkFrom = journal.linkFrom;
                annotations.linkTo = journal.linkTo;
            }
            return new DeductionSubmission
            {
                selectedEvidenceIds = new List<string>(sources),
                claimId = ClaimId ?? "",
                annotationVersion = F101Annotations.Version,
                annotations = annotations
            };
        }
    }
}