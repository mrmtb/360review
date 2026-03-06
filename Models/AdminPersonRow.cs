namespace reviews.Models;

public class AdminPersonRow
{
    public Guid ParticipantId { get; set; }
    public string ParticipantNameFirst { get; set; } = string.Empty;
    public string ParticipantNameLast { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public bool ParticipantIsActive { get; set; }
    public bool IsInActiveEvaluation { get; set; }
    public bool IsRatee { get; set; }
    public bool IsParticipant { get; set; }
    public bool HasOpenItems { get; set; }
    public DateTime? LastEmailedAt { get; set; }
    public DateTime? RateeDateAdded { get; set; }
}
