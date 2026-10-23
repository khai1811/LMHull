using System.Drawing;

namespace LMHull.Rhino.Infrastructure.Cad;

/// <summary>
/// Định nghĩa một layer theo đường dẫn "Cha/Con" và màu của nó.
/// </summary>
public record CadLayer(string Path, Color Color)
{
    public CadLayer Child(string name, Color color) => new($"{Path}/{name}", color);

    public CadObjectSpec Spec(string? name = null, System.Collections.Generic.IReadOnlyDictionary<string, object>? meta = null) =>
        new(Path, Color, name, meta);
}
