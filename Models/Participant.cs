using System;
using System.Collections.Generic;

namespace reviews.Models;

public partial class Participant
{
    public Guid ParticipantId { get; set; }

    public string ParticipantNameFirst { get; set; } = null!;

    public string ParticipantNameLast { get; set; } = null!;

    public string ParticipantEmail { get; set; } = null!;

    public Guid ClientId { get; set; }

    public bool ParticipantHasRegistered { get; set; } = false;

    public bool ParticipantIsActive { get; set; } = true;

    public ICollection<EvaluationParticipant> EvaluationParticipants { get; set; } = null!;

    public ICollection<EvaluationRatee> EvaluationRatees { get; set; } = null!;

    public Client Client { get; set; } = null!;
}
