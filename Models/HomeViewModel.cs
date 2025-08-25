using reviews.Models;

public class HomeViewModel
{
    public string? Role { get; set; }
    public string? Email { get; set; }
    public Guid LocalClientId { get; set; }
    public string? LocalClientName { get; set; }
    public string? LocalClientLogoLocation { get; set; }
    public int LocalDefaultEvaluationId { get; set; }
    public int LocalOpenRatings { get; set; }
    public string? EvaluationsText { get; set; }
    public string? RatersText { get; set; }
    public bool EmailTest { get; set; } = true;
    public List<Client>? Clients { get; set; }
    public Guid ParticipantId { get; set; }
    public string? ParticipantName { get; set; }
}