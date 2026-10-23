using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Infrastructure.Cad;

/// <summary>
/// Mặt tiền duy nhất cho mọi thao tác CAD trên một tài liệu (đối tượng, layer, metadata, chọn, vẽ lại),
/// để service và command không gọi trực tiếp RhinoDoc.
/// </summary>
public interface ICadHandle
{
    double Tolerance { get; }

    /// <summary>
    /// Tên tài liệu (rỗng nếu chưa lưu).
    /// </summary>
    string DocumentName { get; }

    /// <summary>
    /// Đường dẫn đầy đủ của tệp .3dm (rỗng nếu chưa lưu).
    /// </summary>
    string DocumentPath { get; }

    int ObjectCount { get; }

    /// <summary>
    /// Số đối tượng (chưa xóa) thuộc các kiểu trong bộ lọc.
    /// </summary>
    int CountObjects(ObjectType filter);

    int LayerCount { get; }

    Guid AddBrep(Brep brep, CadObjectSpec spec);

    Guid AddCurve(Curve curve, CadObjectSpec spec);

    Guid AddPoint(Point3d point, CadObjectSpec spec);

    bool Delete(Guid id);

    bool Exists(Guid id);

    CadObject? Find(Guid id);

    T? GetGeometry<T>(Guid id) where T : GeometryBase;

    /// <summary>
    /// Các Brep / Polysurface đang hiển thị (không gồm đối tượng ẩn hoặc đã xóa).
    /// </summary>
    IEnumerable<CadObject> FindBreps();

    bool Replace(Guid id, Brep brep);

    bool Transform(Guid id, Transform xform);

    /// <summary>
    /// Index của layer theo đường dẫn "Cha/Con/..."; tự tạo các cấp còn thiếu (màu áp cho cấp cuối khi tạo mới).
    /// </summary>
    int Layer(string path, Color color);

    object? GetMeta(Guid id, string key);

    void SetMeta(Guid id, IReadOnlyDictionary<string, object> values);

    void Select(IEnumerable<Guid> ids);

    void UnselectAll();

    void Redraw();
}
