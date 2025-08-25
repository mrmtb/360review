using System;
using System.Collections.Generic;

namespace reviews.Models;

public partial class EvaluationParticipantStatus
{
    public int EvaluationParticipantStatusId { get; set; }

    public string EvaluationParticipantStatusName { get; set; } = null!;

    public int OnReminderNewStatus { get; set; }

    public int OnCompletionNewStatus { get; set; }

    public int OnOptOutNewStatus { get; set; }

    public int EvaluationParticipantStatusIsActive { get; set; }

    public int EvaluationParticipantStatusIsComplete { get; set; }

    public string EvaluationParticipantStatusDecoration { get; set; } = null!;

    public ICollection<EvaluationParticipant> EvaluationParticipants { get; set; } = new List<EvaluationParticipant>();
}
