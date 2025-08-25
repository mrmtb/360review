using System;

namespace reviews.Models
{
    public class vwAdminRatees
    {
        public Guid ClientId { get; set; }
        public required string ClientName { get; set; }
        public int EvaluationId { get; set; }
        public Guid ParticipantId { get; set; }
        public required string ParticipantNameFirst { get; set; }
        public required string ParticipantNameLast { get; set; }
        public required string ParticipantEmail { get; set; }
        public bool ParticipantIsActive { get; set; }
        public DateTime? EvaluationRateeDateAdded { get; set; }
        public DateTime? EvaluationParticipantDateLastEmailed { get; set; }
        public DateTime? LastDueDate { get; set; }
    }
}
