using System;
using System.Collections.Generic;

namespace reviews.Models;

public partial class EvaluationRateeStatus
{
    public int EvaluationRateeStatusId { get; set; }

    public string EvaluationRateeStatusName { get; set; } = null!;

    public int OnReminderNewStatus { get; set; }

    public int OnCompletionNewStatus { get; set; }

    public int OnOptOutNewStatus { get; set; }

    public ICollection<EvaluationRatee> EvaluationRatees { get; set; } = new List<EvaluationRatee>();
}
