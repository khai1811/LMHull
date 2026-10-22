using System;
using LMHull.Rhino.Common;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

public class LMHydroCommand : Command
{
    // Tên lệnh người dùng sẽ gõ trên dòng lệnh Rhino
    public override string EnglishName => "LMHydro";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // ---------------------------------------------------------------------
        // BƯỚC 1: NGƯỜI DÙNG CHỌN VỎ TÀU (GetObject)
        // ---------------------------------------------------------------------
        var getHull = new GetObject();
        getHull.SetCommandPrompt("Chọn vỏ tàu cần tính toán thủy tĩnh (Brep / Polysurface)");

        // Đặt bộ lọc chỉ cho phép chọn Brep hoặc Polysurface (loại bỏ Point, Curve, Mesh)
        getHull.GeometryFilter = ObjectType.Brep | ObjectType.PolysrfFilter;
        getHull.SubObjectSelect = false; // Không cho chọn mặt lẻ
        getHull.Get();

        // Xử lý nếu người dùng bấm Esc hoặc hủy chọn
        if (getHull.CommandResult() != Result.Success)
        {
            return getHull.CommandResult();
        }

        // Trích xuất hình học từ ObjRef -> Brep
        ObjRef objRef = getHull.Object(0);
        Brep? hullBrep = objRef.Brep();

        if (hullBrep == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Đối tượng được chọn không thể đọc cấu trúc Brep.");
            return Result.Failure;
        }

        // Lấy thông số bao hình học của tàu được chọn
        BoundingBox bbox = hullBrep.GetBoundingBox(true);
        double totalLength = bbox.Max.X - bbox.Min.X;
        double totalBeam = bbox.Max.Y - bbox.Min.Y;
        double totalDepth = bbox.Max.Z - bbox.Min.Z;

        RhinoApp.WriteLine($"[LMHydro] Đã chọn: {objRef.Object().Name ?? "Vỏ tàu"} | Kích thước bao: L={totalLength:F2}m, B={totalBeam:F2}m, D={totalDepth:F2}m");

        // ---------------------------------------------------------------------
        // BƯỚC 2: NGƯỜI DÙNG NHẬP CHIỀU CHÌM / MỚN NƯỚC T (GetNumber)
        // ---------------------------------------------------------------------
        var getDraft = new GetNumber();
        getDraft.SetCommandPrompt("Nhập chiều chìm thiết kế T (mớn nước)");

        // Đặt giá trị mặc định bằng 60% chiều cao mạn và giới hạn an toàn
        double defaultDraft = Math.Round(totalDepth * 0.6, 2);
        getDraft.SetDefaultNumber(defaultDraft);
        getDraft.SetLowerLimit(0.1, false);
        getDraft.SetUpperLimit(totalDepth, true);

        getDraft.Get();

        // Xử lý nếu bấm Esc
        if (getDraft.CommandResult() != Result.Success)
        {
            return getDraft.CommandResult();
        }

        double draft = getDraft.Number();

        // ---------------------------------------------------------------------
        // BƯỚC 3: TÍNH THỂ TÍCH KHỐI VỎ VÀ CẮT LẤY PHẦN CHÌM
        // ---------------------------------------------------------------------
        // 1. Tính thể tích toàn bộ khối nếu là Brep kín
        double totalVolume = 0.0;
        if (hullBrep.IsSolid)
        {
            var massProps = VolumeMassProperties.Compute(hullBrep);
            if (massProps != null) totalVolume = massProps.Volume;
        }

        // 2. Cắt lấy phần chìm dưới mặt nước: Mặt phẳng đường nước tại Z = bbox.Min.Z + draft
        double waterLevelZ = bbox.Min.Z + draft;
        var waterPlane = new Plane(new Point3d(0, 0, waterLevelZ), Vector3d.ZAxis);

        // Cắt vỏ tàu bằng mặt phẳng nước (Brep.Trim)
        Brep[] trimmedParts = hullBrep.Trim(waterPlane, doc.ModelAbsoluteTolerance);

        double submergedVolumeApprox = 0.0;

        // Nếu cắt thành công, tìm phần nằm bên dưới Z
        if (trimmedParts != null && trimmedParts.Length > 0)
        {
            foreach (var part in trimmedParts)
            {
                BoundingBox partBox = part.GetBoundingBox(true);
                if (partBox.Max.Z <= waterLevelZ + doc.ModelAbsoluteTolerance)
                {
                    // Đóng nắp mặt cắt nước phẳng
                    Brep cappedSubmerged = part.CapPlanarHoles(doc.ModelAbsoluteTolerance) ?? part;
                    var subMass = VolumeMassProperties.Compute(cappedSubmerged);
                    if (subMass != null)
                    {
                        submergedVolumeApprox = subMass.Volume;
                    }
                    break;
                }
            }
        }

        // Nếu vỏ chưa kín hoàn toàn, ước tính nhanh theo thể tích hộp bao của phần chìm
        if (submergedVolumeApprox <= 0.0)
        {
            submergedVolumeApprox = totalLength * totalBeam * draft * 0.70; // Giả định hệ số béo Cb ~ 0.7
        }

        // Tính lượng chiếm nước với nước biển (tỷ trọng 1.025 T/m3)
        double displacement = submergedVolumeApprox * 1.025;

        // ---------------------------------------------------------------------
        // BƯỚC 4: IN KẾT QUẢ TÍNH TOÁN RA DÒNG LỆNH RHINO
        // ---------------------------------------------------------------------
        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine(" [LMHydro] KẾT QUẢ TÍNH TOÁN TƯƠNG TÁC:");
        RhinoApp.WriteLine($" - Trạng thái vỏ tàu : {(hullBrep.IsSolid ? "SOLID (Kín)" : "OPEN (Chưa kín hoàn toàn)")}");
        RhinoApp.WriteLine($" - Chiều chìm đã nhập: T = {draft:F2} m (Mức nước Z = {waterLevelZ:F2} m)");
        if (totalVolume > 0)
        {
            RhinoApp.WriteLine($" - Thể tích toàn bộ  : V_total = {totalVolume:F2} m3");
        }
        RhinoApp.WriteLine($" - Thể tích phần chìm: V_sub    = {submergedVolumeApprox:F2} m3");
        RhinoApp.WriteLine($" - Lượng chiếm nước  : Δ        = {displacement:F2} Tấn (Nước biển)");
        RhinoApp.WriteLine($" - Tỷ số kích thước  : L/B = {(totalLength / totalBeam):F2} | B/T = {(totalBeam / draft):F2}");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}