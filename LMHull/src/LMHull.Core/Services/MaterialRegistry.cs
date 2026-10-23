using System.Collections.Generic;
using LMHull.Core.Models;

namespace LMHull.Core.Services;

public static class MaterialRegistry
{
    public static readonly List<ShipMaterial> AvailableMaterials = new()
    {
        new ShipMaterial("Steel AH36", 7.85, 355),
        new ShipMaterial("Steel Grade A", 7.85, 235),
        new ShipMaterial("Aluminium 5083", 2.66, 125),
        new ShipMaterial("Stainless Steel 316", 8.00, 205)
    };

    public static ShipMaterial GetDefault() => AvailableMaterials[0];

    public static ShipMaterial? Find(string name) => AvailableMaterials.Find(m => m.Name == name);

    public static ShipMaterial FindOrDefault(string name) => Find(name) ?? GetDefault();
}