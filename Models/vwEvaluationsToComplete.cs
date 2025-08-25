using System;

namespace reviews.Models
{
    public class vwEvaluationsToComplete
    {
        public Guid Rater { get; set; }
        public required string RaterNameFirst { get; set; }
        public required string RaterNameLast { get; set; }
        public required string RaterEmail { get; set; }
        public int EvaluationId { get; set; }
        public required bool EvaluationActive { get; set; }
        public required bool EvaluationTestEmails { get; set; }
        public required string EvaluationName { get; set; }
        public required string EvaluationFolder { get; set; }
        public required string EvaluationParticipantSurveyPassword { get; set; }
        public Guid ParticipantId { get; set; }
        public required string ParticipantNameFirst { get; set; }
        public required string ParticipantNameLast { get; set; }
        public required string ParticipantEmail { get; set; }
        public int ParticipantRoleId { get; set; }
        public required string ParticipantRoleName { get; set; }
        public Guid EvaluationRateeId { get; set; }
        public DateTime? EvaluationParticipantDateLastEmailed { get; set; }
        public DateTime? DueDate { get; set; }
        public required string EvaluationParticipantStatusName { get; set; }
        public int EvaluationParticipantStatusIsActive { get; set; }
        public int EvaluationParticipantStatusIsComplete { get; set; }
        public required string EvaluationParticipantStatusDecoration { get; set; }
    }
}
