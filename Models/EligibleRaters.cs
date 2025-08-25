namespace reviews.Models;

public partial class EligibleRaters
{
    public Guid EvaluationRateeId { get; set; }

    public Guid ParticipantId { get; set; }

    public string ParticipantNameFirst { get; set; } = null!;

    public string ParticipantNameLast { get; set; } = null!;

    public string ParticipantEmail { get; set; } = null!;

    public bool ParticipantHasRegistered { get; set; } = false;
}
