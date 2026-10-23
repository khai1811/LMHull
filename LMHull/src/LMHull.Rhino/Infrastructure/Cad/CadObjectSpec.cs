using System.Collections.Generic;
using System.Drawing;

namespace LMHull.Rhino.Infrastructure.Cad;

/// <summary>
/// Thông tin khi thêm đối tượng: đường dẫn layer ("Cha/Con"), màu layer, tên và metadata.
/// </summary>
public record CadObjectSpec(string LayerPath, Color LayerColor, string? Name = null, IReadOnlyDictionary<string, object>? Meta = null);
