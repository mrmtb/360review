using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace reviews.Models;

public partial class EvaluationParticipant
{
    public Guid EvaluationRateeId { get; set; } // Removed [ForeignKey] attribute

    [ForeignKey(nameof(Participant))]
    public Guid ParticipantId { get; set; }

    public int? ParticipantRoleId { get; set; }

    public string EvaluationParticipantSurveyPassword { get; set; } = null!;

    public DateTime? EvaluationParticipantDateLastEmailed { get; set; }

    public int? EvaluationParticipantStatusId { get; set; }

    public DateTime EvaluationParticipantDateAdded { get; set; }

    public string? EvaluationParticipantSessionId { get; set; }

    public DateTime? EvaluationParticipantDateCompleted { get; set; }

    public EvaluationRatee EvaluationRatee { get; set; } = null!;

    public EvaluationParticipantStatus EvaluationParticipantStatus { get; set; } = null!;

    public Participant Participants { get; set; }  = null!;

    public ParticipantRole ParticipantRoles { get; set; } = null!;
}
