using System;
using System.Collections.Generic;

namespace reviews.Models
{
    public class AdminParticipantViewModel
    {
        public Participant? Participant { get; set; }
        public List<vwRatersByRatee> Raters { get; set; } = new();
        public List<vwEvaluationsToComplete> Ratees { get; set; } = new();
        public List<vwEvaluationsByRatee> Evaluations { get; set; } = new();
        public List<Evaluation> ActiveEvaluations { get; set; } = new();
        public vwEvaluationsByRatee? SelectedEvaluation { get; set; }
    }
}
