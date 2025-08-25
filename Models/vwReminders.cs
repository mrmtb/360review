using System;

namespace reviews.Models
{
    public class vwReminders
    {
        public Guid ClientId { get; set; }
        public required string ClientName { get; set; }
        public required string ClientHelpContact { get; set; }
        public int EvaluationId { get; set; }
        public Guid EvaluationRateeId { get; set; }
        public bool EvaluationTestEmails { get; set; }
        public Guid RateeParticipantId { get; set; }
        public required string RateeNameFirst { get; set; }
        public required string RateeNameLast { get; set; }
        public required string RateeNameCombined { get; set; }
        public required string RateeEmail { get; set; }
        public Guid ParticipantId { get; set; }
        public required string ParticipantNameFirst { get; set; }
        public required string ParticipantNameLast { get; set; }
        public required string ParticipantNameCombined { get; set; }
        public required string ParticipantEmail { get; set; }
        public required string ParticipantRoleName { get; set; }
        public required string MessageToSend { get; set; }
        public DateTime? EvaluationParticipantDateAdded { get; set; }
        public DateTime? EvaluationParticipantDateLastEmailed { get; set; }
        public DateTime DueDate { get; set; }
    }
}
