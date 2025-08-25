using System;
using System.Collections.Generic;

namespace reviews.Models;

public partial class ViewRatersViewModel
{
    public ICollection<vwRatersByRatee>? Raters { get; set; }

    public required ICollection<ParticipantRole> ParticipantRoles { get; set; }

    public required Guid EvaluationRateeId { get; set; } 
}
