using System.ComponentModel.DataAnnotations;

namespace reviews.Models;

public class SurveyDefinitionVersion
{
    [Key]
    public Guid SurveyDefinitionVersionId { get; set; } = Guid.NewGuid();

    public Guid SurveyDefinitionId { get; set; }

    public int VersionNumber { get; set; }

    [MaxLength(100)]
    public string TemplateKey { get; set; } = null!;

    [MaxLength(500)]
    public string TemplatePath { get; set; } = null!;

    public string? DraftJson { get; set; }

    public string? PublishedJson { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(256)]
    public string? LastEditedBy { get; set; }

    public DateTimeOffset? LastEditedAt { get; set; }

    [MaxLength(256)]
    public string? PublishedBy { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public SurveyDefinition? SurveyDefinition { get; set; }

    public ICollection<Evaluation> CurrentForEvaluations { get; set; } = new List<Evaluation>();
}
