using System;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

/// <summary>
/// Luồng dựng kết cấu từ nét vẽ GA dùng chung cho lệnh LMPickOnGA và panel cây kết cấu.
/// </summary>
public static class GAPlacementService
{
    /// <summary>
    /// Chiếu dựng tấm phẳng theo kiểu kết cấu: mặt boong (Z), vách ngang (X) hoặc vách dọc (Y).
    /// </summary>
    public static Brep? CreateFromGA(Brep hull, Curve gaCurve, StructuralNodeType kind, double tolerance) => kind switch
    {
        StructuralNodeType.DeckPlate => GAProjectionService.CreateDeckFromElevationLine(hull, gaCurve, tolerance),
        StructuralNodeType.LongitudinalBulkhead => GAProjectionService.CreateLongitudinalBulkheadFromLine(hull, gaCurve, tolerance),
        _ => GAProjectionService.CreateBulkheadFromLine(hull, gaCurve, tolerance)
    };

    public static CadLayer LayerFor(StructuralNodeType kind) => kind switch
    {
        StructuralNodeType.DeckPlate => StructureLayers.Decks,
        StructuralNodeType.LongitudinalBulkhead => StructureLayers.LongBulkheads,
        _ => StructureLayers.Bulkheads
    };

    /// <summary>
    /// Thêm tấm vào tài liệu trên layer tương ứng; nếu có node thì ghi metadata liên kết node vào đối tượng.
    /// </summary>
    public static Guid Bake(ICadHandle cad, Brep plate, StructuralNodeType kind, string name, StructuralNode? node = null)
    {
        Guid id = cad.AddBrep(plate, LayerFor(kind).Spec(name, node == null ? null : StructureObjectData.Meta(node)));
        cad.Redraw();
        return id;
    }
}
