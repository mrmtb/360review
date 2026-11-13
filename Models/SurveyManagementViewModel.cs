namespace reviews.Models;

public class SurveyManagementViewModel
{
    public Guid EvaluationId { get; set; }

    public string EvaluationName { get; set; } = null!;

    public Guid ClientId { get; set; }

    public Guid? CurrentSurveyDefinitionVersionId { get; set; }

    public string? CurrentSurveyDisplayName { get; set; }

    public int CurrentPinnedParticipantCount { get; set; }

    public List<SurveyManagementVersionRow> Versions { get; set; } = new();
}

public class SurveyManagementVersionRow
{
    public Guid SurveyDefinitionVersionId { get; set; }

    public Guid SurveyDefinitionId { get; set; }

    public string SurveyDefinitionName { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string TemplateKey { get; set; } = null!;

    public string TemplatePath { get; set; } = null!;

    public bool IsPublished { get; set; }

    public bool IsCurrentForEvaluation { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int PinnedParticipantCount { get; set; }

    public int StartedResponseCount { get; set; }

    public int SubmittedResponseCount { get; set; }
}
