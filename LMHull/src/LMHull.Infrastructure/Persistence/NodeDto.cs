using System;

namespace LMHull.Infrastructure.Persistence;

public class NodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public double ThicknessMm { get; set; }
    public string Material { get; set; } = string.Empty;
    public double WeightTonnes { get; set; }
    public Guid RhinoGeometryId { get; set; }
}
