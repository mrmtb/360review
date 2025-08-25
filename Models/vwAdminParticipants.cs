using System;

namespace reviews.Models
{
    public class vwAdminParticipants
    {
        public Guid ClientId { get; set; }
        public required string ClientName { get; set; }
        public Guid ParticipantId { get; set; }
        public required string ParticipantNameFirst { get; set; }
        public required string ParticipantNameLast { get; set; }
        public required string ParticipantEmail { get; set; }
        public required string IsRatee { get; set; }
        public required string IsActive { get; set; }
        public bool ParticipantIsActive { get; set; }
        public DateTime? EvaluationParticipantDateLastEmailed { get; set; }
    }
}
