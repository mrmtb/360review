namespace reviews.Models;

public class SurveyVersionUsageViewModel
{
    public Guid EvaluationId { get; set; }

    public Guid SurveyDefinitionVersionId { get; set; }

    public string EvaluationName { get; set; } = null!;

    public string SurveyDisplayName { get; set; } = null!;

    public string UsageType { get; set; } = null!;

    public int Count { get; set; }

    public List<SurveyVersionUsageRow> Rows { get; set; } = new();

    public List<Guid> ParticipantIds => Rows
        .Select(r => r.RaterParticipantId)
        .Distinct()
        .ToList();

    public List<SurveyVersionUsageParticipantAction> InvitationTargets => Rows
        .Select(r => new SurveyVersionUsageParticipantAction(r.EvaluationRateeId, r.RaterParticipantId))
        .Distinct()
        .ToList();
}

public class SurveyVersionUsageRow
{
    public Guid EvaluationRateeId { get; set; }

    public Guid RaterParticipantId { get; set; }

    public string RaterName { get; set; } = null!;

    public string RaterEmail { get; set; } = null!;

    public string RateeName { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public string ParticipantStatusName { get; set; } = null!;

    public DateTime? LastSavedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public bool IsSubmitted { get; set; }

    public bool CanResendInvite => !IsSubmitted;

    public bool CanSendReminder => !IsSubmitted;
}

public record SurveyVersionUsageParticipantAction(Guid EvaluationRateeId, Guid ParticipantId);
