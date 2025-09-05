using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace reviews.Models;

public partial class ParticipantRole
{
    public int ParticipantRoleId { get; set; }

    public string ParticipantRoleName { get; set; } = null!;

    public ICollection<EvaluationParticipant> EvaluationParticipants { get; set; } = new List<EvaluationParticipant>();
}
