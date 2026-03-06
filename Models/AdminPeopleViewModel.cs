using System.Collections.Generic;

namespace reviews.Models;

public class AdminPeopleViewModel
{
    public List<AdminPersonRow> People { get; set; } = new();
}
