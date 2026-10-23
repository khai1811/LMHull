using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Reporting;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Features.Structure.Calculations;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Features.Structure.Services;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.Structure.Commands;

public class LMCreateCompartmentsCommand : Command
{
    public override string EnglishName => "LMCreateCompartments";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn thân tàu kín
        var picked = RhinoInput.PickBrep("Chọn thân tàu kín (Solid Brep) để phân chia khoang rỗng", out Brep hull, requireSolid: true);
        if (picked != Result.Success) return picked;

        // 2. Nhập số lượng khoang muốn chia
        var gotNum = RhinoInput.GetInteger("Nhập tổng số khoang chứa cần chia", 5, 2, 20, out int totalTanks);
        if (gotNum != Result.Success) return gotNum;

        // 3. Tính toán các vị trí vách phân chia
        BoundingBox bbox = hull.GetBoundingBox(true);
        double minX = bbox.Min.X;
        double maxX = bbox.Max.X;
        double dx = (maxX - minX) / totalTanks;

        var xPositions = new List<double>();
        for (int i = 1; i < totalTanks; i++)
        {
            xPositions.Add(minX + i * dx);
        }

        RhinoApp.WriteLine("[LMCompartments] Đang chia khối thân tàu thành các khoang kín...");
        CalculationResult<List<CompartmentInfo>> calcResult;
        using (var progress = new RhinoProgressReporter("Chia khoang"))
        {
            var limits = ServiceRegistry.Settings.Limits;
            var input = new CompartmentInput(hull, xPositions.ToArray(), doc.ModelAbsoluteTolerance, limits.IsActive ? limits : null);
            calcResult = new CompartmentCalculation().Run(input, progress);
        }

        var compartments = calcResult.Value;
        if (!calcResult.IsSuccess || compartments == null || compartments.Count == 0)
        {
            string detail = calcResult.Errors.Count > 0 ? " " + calcResult.GetErrorMessage() : string.Empty;
            ServiceRegistry.Logger.Error("[LMCreateCompartments] Không thể phân chia khoang trên hình học này." + detail);
            return Result.Failure;
        }

        CompartmentService.BakeCompartments(RhinoCadHandle.For(doc), compartments);

        // 4. Xuất bảng thông số dung tích và trọng tâm từng khoang
        var table = new ResultTable("Dung tích khoang chứa", "LMCreateCompartments",
            "Tên khoang", "Thể tích (m³)", "CG X (m)", "CG Y (m)", "CG Z (m)");
        foreach (var c in compartments)
        {
            table.AddRow(c.Name, F(c.Volume), F(c.CenterOfGravity.X), F(c.CenterOfGravity.Y), F(c.CenterOfGravity.Z));
        }
        table.Notes.Add($"Tổng dung tích toàn bộ khoang: {compartments.Sum(c => c.Volume):F2} m³");
        table.Notes.Add($"Thời gian tính: {calcResult.Elapsed.TotalSeconds:F2} s – Layer: 'LMHull_Compartments'");
        string limitNote = ServiceRegistry.Settings.Limits.Describe();
        if (limitNote.Length > 0) table.Notes.Add(limitNote);
        ResultsHub.Publish(table);

        return Result.Success;
    }

    private static string F(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
}