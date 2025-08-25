using System;

namespace reviews.Models
{
    public class vwEvaluationsByRatee
    {
        public int EvaluationId { get; set; }
        public bool EvaluationActive { get; set; }
        public required string EvaluationName { get; set; }
        public required string EvaluationFolder { get; set; }
        public string? EvaluationHelpContact { get; set; }
        public int EvaluationMaxRaters { get; set; }
        public DateTime? EvaluationOpenDate { get; set; }
        public DateTime? EvaluationRateeDueDate { get; set; }
        public DateTime? EvaluationRaterDueDate { get; set; }
        public string? EvaluationClientContact { get; set; }
        public Guid ClientId { get; set; }
        public Guid ParticipantId { get; set; }
    }
}
