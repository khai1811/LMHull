using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LMHull.ConsoleApp.Helpers;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=== MÁY TÍNH THÔNG SỐ TÀU - LMHull.Console (v0.1 & v0.2) ===");

var shipInputs = new List<ShipInput>
{
    new ShipInput("Phương án A (Sà lan 1)", 45.0, 10.0, 2.5, 3.2),
    new ShipInput("Phương án B (Sà lan 2)", 60.0, 12.0, 3.0),
    new ShipInput("Phương án C (Sà lan ngắn)", 30.0, 10.0, 2.0, 2.8)
};

var calculator = new ShipCalculator();
calculator.OnCalculated += msg => Console.WriteLine(msg);

// 1. Tính toán tất cả phương án qua LMHull.Core
List<ShipResult> allResults = shipInputs
    .Select(s => calculator.Calculate(s, WaterType.SeaWater))
    .ToList();

Console.WriteLine("\n1. TẤT CẢ CÁC PHƯƠNG ÁN:");
ConsoleTablePrinter.Print(allResults);

// 2. Lọc phương án bằng LINQ theo L/B từ 4.0 đến 5.5
var filter = new RangeFilter(4.0, 5.5);
var filtered = calculator.FilterByLBRatio(allResults, filter);

Console.WriteLine($"\n2. CÁC PHƯƠNG ÁN CÓ L/B TRONG KHOẢNG [{filter.Min} - {filter.Max}]:");
ConsoleTablePrinter.Print(filtered);