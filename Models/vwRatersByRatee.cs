using System;

namespace reviews.Models
{
    public class vwRatersByRatee
    {
        public Guid EvaluationRateeId { get; set; }
        public Guid Ratee { get; set; }
        public Guid EvaluationId { get; set; }
        public bool EvaluationActive { get; set; }
        public bool EvaluationTestEmails { get; set; }
        public int EvaluationMaxRaters { get; set; }
        public Guid ParticipantId { get; set; }
        public required string ParticipantNameFirst { get; set; }
        public required string ParticipantNameLast { get; set; }
        public required string ParticipantEmail { get; set; }
        public int ParticipantRoleId { get; set; }
        public required string ParticipantRoleName { get; set; }
        public DateTime? EvaluationParticipantDateLastEmailed { get; set; }
        public int EvaluationParticipantStatusId { get; set; }
        public required string EvaluationParticipantStatusName { get; set; }
        public int EvaluationParticipantStatusIsActive { get; set; }
        public int EvaluationParticipantStatusIsComplete { get; set; }
        public required string EvaluationParticipantStatusDecoration { get; set; }
    }
}
