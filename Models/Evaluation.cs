using System;
using System.Collections.Generic;

namespace reviews.Models;

public partial class Evaluation
{
    public int EvaluationId { get; set; }

    public bool EvaluationActive { get; set; }

    public string EvaluationName { get; set; } = null!;

    public string EvaluationFolder { get; set; } = null!;

    public string? EvaluationHelpContact { get; set; } = "";

    public int EvaluationMaxRaters { get; set; } = 15;

    public DateTime? EvaluationOpenDate { get; set; }

    public DateTime? EvaluationRateeDueDate { get; set; }

    public DateTime? EvaluationRaterDueDate { get; set; }

    public string? EvaluationClientContact { get; set; }

    public bool EvaluationTestEmails { get; set; } = true;

    public Guid ClientId { get; set; }

    public Client? Client { get; set; }

    public ICollection<EvaluationRatee> EvaluationRatee { get; set; } = new List<EvaluationRatee>();
}
