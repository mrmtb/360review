using System;

namespace reviews.Models
{
    public class vwDefaultEvaluationByParticipant
    {
        public Guid ParticipantId { get; set; }
        public Guid EvaluationId { get; set; }
        public bool EvaluationActive { get; set; }
    }
}
