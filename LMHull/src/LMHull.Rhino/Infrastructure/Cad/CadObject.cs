using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace LMHull.Rhino.Infrastructure.Cad;

/// <summary>
/// Ảnh chụp một đối tượng CAD: id, tên, đường dẫn layer ("Cha/Con"), hình học và metadata.
/// </summary>
public record CadObject(Guid Id, string? Name, string LayerPath, GeometryBase Geometry, IReadOnlyDictionary<string, object> Meta);
