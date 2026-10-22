using System;
using System.Collections.Generic;
using LMHull.Core.Models;

namespace LMHull.ConsoleApp.Helpers;

public static class ConsoleTablePrinter
{
    public static void Print(List<ShipResult> results)
    {
        string line = new string('-', 88);
        Console.WriteLine(line);
        Console.WriteLine($"| {"Tên phương án",-24} | {"L (m)",6} | {"B (m)",6} | {"T (m)",6} | {"L/B",5} | {"B/T",5} | {"V.Hộp(m3)",9} | {"Chiếm nước(T)",13} |");
        Console.WriteLine(line);

        foreach (var r in results)
        {
            Console.WriteLine($"| {r.Input.Name,-24} | {r.Input.Length,6:F1} | {r.Input.Beam,6:F1} | {r.Input.Draft,6:F1} | {r.LBRatio,5:F2} | {r.BTRatio,5:F2} | {r.BoxVolume,9:F2} | {r.Displacement,13:F2} |");
        }
        Console.WriteLine(line);
    }
}