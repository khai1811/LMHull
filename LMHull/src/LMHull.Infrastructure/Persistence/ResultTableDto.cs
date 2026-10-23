using System;
using System.Collections.Generic;

namespace LMHull.Infrastructure.Persistence;

public class ResultTableDto
{
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<string[]> Rows { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
