namespace LMHull.Core.Enums;

public enum StructuralNodeType
{
    Project,
    ShellGroup,
    ShellPortSide,       // Mạn trái (PS)
    ShellStarboardSide,  // Mạn phải (SB)
    DeckGroup,
    DeckTier,            // Tầng boong (Tier)
    BulkheadGroup,
    TransverseBulkhead,  // Vách ngang
    LongitudinalBulkhead,// Vách dọc
    FrameGroup,
    WebFrame,            // Khung sườn gia cường
    TankCompartment,     // Khoang két
    DeckPlate            // Mặt boong
}
