using System;
using System.Collections.Generic;

namespace reviews.Models;

public class AdminDumpParticipantViewModel
    {
        public vwParticipant? Participant { get; set; }
        public List<vwRatersByRatee> Raters { get; set; } = new();
        public List<vwEvaluationsToComplete> Ratees { get; set; } = new();
        public List<vwEvaluationsByRatee> Evaluations { get; set; } = new();
}
