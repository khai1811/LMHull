using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Common;

/// <summary>
/// Các thao tác nhập liệu dùng chung trên dòng lệnh Rhino. Mọi hàm trả về Result.Success khi người dùng
/// nhập hợp lệ; trả về kết quả hủy (Esc) hoặc Result.Failure kèm thông báo lỗi trong các trường hợp còn lại.
/// </summary>
public static class RhinoInput
{
    public const ObjectType BrepFilter = ObjectType.Brep | ObjectType.PolysrfFilter;

    public static Result PickBrep(string prompt, out Brep brep, out ObjRef objRef, bool requireSolid = false, ObjectType filter = BrepFilter)
    {
        brep = null!;
        objRef = null!;

        var getObj = new GetObject();
        getObj.SetCommandPrompt(prompt);
        getObj.GeometryFilter = filter;
        getObj.SubObjectSelect = false;
        getObj.Get();
        if (getObj.CommandResult() != Result.Success) return getObj.CommandResult();

        objRef = getObj.Object(0);
        Brep? picked = objRef.Brep();
        if (picked == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Đối tượng được chọn không thể đọc cấu trúc Brep.");
            return Result.Failure;
        }
        if (requireSolid && !picked.IsSolid)
        {
            RhinoApp.WriteLine("[Lỗi]: Vui lòng chọn một khối kín (Solid Brep).");
            return Result.Failure;
        }

        brep = picked;
        return Result.Success;
    }

    public static Result PickBrep(string prompt, out Brep brep, bool requireSolid = false, ObjectType filter = BrepFilter) =>
        PickBrep(prompt, out brep, out _, requireSolid, filter);

    public static Result PickCurve(string prompt, out Curve curve)
    {
        curve = null!;

        var getObj = new GetObject();
        getObj.SetCommandPrompt(prompt);
        getObj.GeometryFilter = ObjectType.Curve;
        getObj.Get();
        if (getObj.CommandResult() != Result.Success) return getObj.CommandResult();

        Curve? picked = getObj.Object(0).Curve();
        if (picked == null) return Result.Failure;

        curve = picked;
        return Result.Success;
    }

    /// <summary>
    /// Nhập số nguyên trong khoảng [min, max] (bao gồm 2 đầu).
    /// </summary>
    public static Result GetInteger(string prompt, int defaultValue, int min, int max, out int value)
    {
        value = defaultValue;

        var getInt = new GetInteger();
        getInt.SetCommandPrompt(prompt);
        getInt.SetDefaultInteger(defaultValue);
        getInt.SetLowerLimit(min, false);
        getInt.SetUpperLimit(max, false);
        getInt.Get();
        if (getInt.CommandResult() != Result.Success) return getInt.CommandResult();

        value = getInt.Number();
        return Result.Success;
    }

    /// <summary>
    /// Nhập số thực trong khoảng [min, max] (bao gồm 2 đầu).
    /// </summary>
    public static Result GetNumber(string prompt, double defaultValue, double min, double max, out double value)
    {
        value = defaultValue;

        var getNum = new GetNumber();
        getNum.SetCommandPrompt(prompt);
        getNum.SetDefaultNumber(defaultValue);
        getNum.SetLowerLimit(min, false);
        getNum.SetUpperLimit(max, false);
        getNum.Get();
        if (getNum.CommandResult() != Result.Success) return getNum.CommandResult();

        value = getNum.Number();
        return Result.Success;
    }

    /// <summary>
    /// Chọn một trong các tùy chọn; index trả về theo thứ tự mảng options (0-based).
    /// </summary>
    public static Result GetOption(string prompt, string[] options, out int index)
    {
        index = -1;

        var getOpt = new GetOption();
        getOpt.SetCommandPrompt(prompt);
        var optionIndices = new int[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            optionIndices[i] = getOpt.AddOption(options[i]);
        }
        getOpt.Get();
        if (getOpt.CommandResult() != Result.Success) return getOpt.CommandResult();

        index = System.Array.IndexOf(optionIndices, getOpt.OptionIndex());
        return index >= 0 ? Result.Success : Result.Failure;
    }
}
