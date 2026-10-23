using System.Drawing;
using LMHull.Rhino.Infrastructure.Cad;

namespace LMHull.Rhino.Features.Structure.Services;

/// <summary>
/// Cây layer chuẩn của kết cấu: LMHull_Structure/01_Bulkheads, 02_FrameRibs, 03_Decks, 04_LongBulkheads.
/// </summary>
public static class StructureLayers
{
    public const string RootName = "LMHull_Structure";

    public static readonly CadLayer Root = new(RootName, Color.DarkSlateGray);
    public static readonly CadLayer Bulkheads = Root.Child("01_Bulkheads", Color.Cyan);
    public static readonly CadLayer FrameRibs = Root.Child("02_FrameRibs", Color.Orange);
    public static readonly CadLayer Decks = Root.Child("03_Decks", Color.DeepSkyBlue);
    public static readonly CadLayer LongBulkheads = Root.Child("04_LongBulkheads", Color.Teal);

    public static readonly CadLayer SolidPlates = new("LMHull_SolidPlates", Color.MediumPurple);
    public static readonly CadLayer Stiffeners = new("LMHull_Stiffeners", Color.Goldenrod);
    public static readonly CadLayer Compartments = new("LMHull_Compartments", Color.Gray);
}
