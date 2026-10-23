using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Infrastructure.Cad;

/// <summary>
/// Cài đặt ICadHandle trên RhinoDoc — nơi duy nhất trong plugin thao tác trực tiếp với doc.Objects / doc.Layers.
/// </summary>
public sealed class RhinoCadHandle : ICadHandle
{
    private const char PathSeparator = '/';
    private const string RhinoPathSeparator = "::";

    private readonly RhinoDoc _doc;

    private RhinoCadHandle(RhinoDoc doc)
    {
        _doc = doc;
    }

    public static ICadHandle For(RhinoDoc doc) => new RhinoCadHandle(doc ?? throw new ArgumentNullException(nameof(doc)));

    /// <summary>
    /// Handle của tài liệu đang mở; null nếu Rhino chưa có tài liệu.
    /// </summary>
    public static ICadHandle? ForActiveDoc() => RhinoDoc.ActiveDoc is RhinoDoc doc ? new RhinoCadHandle(doc) : null;

    public double Tolerance => _doc.ModelAbsoluteTolerance;

    public string DocumentName => _doc.Name ?? string.Empty;

    public string DocumentPath => _doc.Path ?? string.Empty;

    public int ObjectCount => CountObjects(ObjectType.AnyObject);

    public int CountObjects(ObjectType filter) => _doc.Objects.GetObjectList(filter).Count();

    public int LayerCount => _doc.Layers.Count;

    #region Đối tượng

    public Guid AddBrep(Brep brep, CadObjectSpec spec) => _doc.Objects.AddBrep(brep, CreateAttributes(spec));

    public Guid AddCurve(Curve curve, CadObjectSpec spec) => _doc.Objects.AddCurve(curve, CreateAttributes(spec));

    public Guid AddPoint(Point3d point, CadObjectSpec spec) => _doc.Objects.AddPoint(point, CreateAttributes(spec));

    public bool Delete(Guid id) => id != Guid.Empty && _doc.Objects.Delete(id, true);

    public bool Exists(Guid id) => id != Guid.Empty && _doc.Objects.FindId(id) is RhinoObject obj && !obj.IsDeleted;

    public CadObject? Find(Guid id)
    {
        var obj = id == Guid.Empty ? null : _doc.Objects.FindId(id);
        return obj == null || obj.IsDeleted ? null : ToCadObject(obj);
    }

    public T? GetGeometry<T>(Guid id) where T : GeometryBase => Find(id)?.Geometry as T;

    public IEnumerable<CadObject> FindBreps()
    {
        var settings = new ObjectEnumeratorSettings
        {
            ObjectTypeFilter = ObjectType.Brep | ObjectType.PolysrfFilter,
            HiddenObjects = false,
            DeletedObjects = false
        };
        return _doc.Objects.FindByFilter(settings).Select(ToCadObject).ToList();
    }

    public bool Replace(Guid id, Brep brep) => _doc.Objects.Replace(id, brep);

    public bool Transform(Guid id, Transform xform) => _doc.Objects.Transform(id, xform, true) != Guid.Empty;

    #endregion

    #region Layer

    public int Layer(string path, Color color)
    {
        string[] parts = path.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) throw new ArgumentException("Đường dẫn layer rỗng.", nameof(path));

        int index = -1;
        Guid parentId = Guid.Empty;
        for (int i = 0; i < parts.Length; i++)
        {
            string fullPath = string.Join(RhinoPathSeparator, parts.Take(i + 1));
            index = _doc.Layers.FindByFullPath(fullPath, -1);
            if (index < 0)
            {
                var layer = new Layer
                {
                    Name = parts[i],
                    Color = i == parts.Length - 1 ? color : Color.DarkGray,
                    ParentLayerId = parentId
                };
                index = _doc.Layers.Add(layer);
            }
            parentId = _doc.Layers[index].Id;
        }
        return index;
    }

    #endregion

    #region Metadata

    public object? GetMeta(Guid id, string key)
    {
        var obj = _doc.Objects.FindId(id);
        return obj != null && obj.Attributes.UserDictionary.TryGetValue(key, out object value) ? value : null;
    }

    public void SetMeta(Guid id, IReadOnlyDictionary<string, object> values)
    {
        var obj = _doc.Objects.FindId(id);
        if (obj == null) return;

        var attr = obj.Attributes.Duplicate();
        WriteMeta(attr.UserDictionary, values);
        _doc.Objects.ModifyAttributes(obj, attr, true);
    }

    #endregion

    #region Hiển thị

    public void Select(IEnumerable<Guid> ids) => _doc.Objects.Select(ids);

    public void UnselectAll() => _doc.Objects.UnselectAll();

    public void Redraw() => _doc.Views.Redraw();

    #endregion

    private ObjectAttributes CreateAttributes(CadObjectSpec spec)
    {
        var attr = new ObjectAttributes { LayerIndex = Layer(spec.LayerPath, spec.LayerColor), Name = spec.Name };
        if (spec.Meta != null) WriteMeta(attr.UserDictionary, spec.Meta);
        return attr;
    }

    private static void WriteMeta(ArchivableDictionary dict, IReadOnlyDictionary<string, object> values)
    {
        foreach (var (key, value) in values)
        {
            switch (value)
            {
                case Guid g: dict.Set(key, g); break;
                case double d: dict.Set(key, d); break;
                case int n: dict.Set(key, n); break;
                case bool b: dict.Set(key, b); break;
                default: dict.Set(key, value?.ToString() ?? string.Empty); break;
            }
        }
    }

    private CadObject ToCadObject(RhinoObject obj)
    {
        var layer = _doc.Layers[obj.Attributes.LayerIndex];
        var meta = obj.Attributes.UserDictionary.Keys.ToDictionary(k => k, k => obj.Attributes.UserDictionary[k]);
        return new CadObject(obj.Id, obj.Attributes.Name, layer.FullPath.Replace(RhinoPathSeparator, PathSeparator.ToString()), obj.Geometry, meta);
    }
}
