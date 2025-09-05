using System;
using System.Collections.Generic;

namespace reviews.Models;

public partial class EvaluationRatee
{
    public Guid EvaluationRateeId { get; set; }

    public Guid EvaluationId { get; set; }

    public Guid ParticipantId { get; set; }

    public int EvaluationRateeStatusId { get; set; }

    public DateTime? EvaluationRateeDueDateOverride { get; set; }

    public DateTime? EvaluationRaterDueDateOverride { get; set; }

    public DateTime? EvaluationRateeDateAdded { get; set; }

    public EvaluationRateeStatus EvaluationRateeStatus { get; set; } = null!;
    
    public ICollection<EvaluationParticipant> EvaluationParticipants { get; set; } = new List<EvaluationParticipant>();

    public Participant Participants { get; set; } = null!;

    public Evaluation Evaluations { get; set; }  = null!;
}
