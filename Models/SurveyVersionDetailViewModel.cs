namespace reviews.Models;

public class SurveyVersionDetailViewModel
{
    public Guid EvaluationId { get; set; }

    public Guid ClientId { get; set; }

    public Guid SurveyDefinitionVersionId { get; set; }

    public Guid SurveyDefinitionId { get; set; }

    public string EvaluationName { get; set; } = null!;

    public string SurveyDefinitionName { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string TemplateKey { get; set; } = null!;

    public string TemplatePath { get; set; } = null!;

    public string DraftJson { get; set; } = "{}";

    public string PublishedJson { get; set; } = "{}";

    public bool IsPublished { get; set; }

    public bool IsCurrentForEvaluation { get; set; }

    public int PinnedParticipantCount { get; set; }

    public int StartedResponseCount { get; set; }

    public int SubmittedResponseCount { get; set; }

    public string? LastEditedBy { get; set; }

    public DateTimeOffset? LastEditedAt { get; set; }

    public string? PublishedBy { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
}
