using System.ComponentModel.DataAnnotations;

namespace reviews.Models;

public class SurveyDefinition
{
    [Key]
    public Guid SurveyDefinitionId { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }

    [MaxLength(100)]
    public string SurveyDefinitionKey { get; set; } = null!;

    [MaxLength(200)]
    public string SurveyDefinitionName { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Client? Client { get; set; }

    public ICollection<SurveyDefinitionVersion> Versions { get; set; } = new List<SurveyDefinitionVersion>();
}
