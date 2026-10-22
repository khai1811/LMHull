using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Builders;

public static class SolidHullStitcher
{
    public static (Brep ClosedBrep, int NakedEdgeCount) CreateSolidHull(Brep openHullBrep, ShipInput input, double tolerance = 0.05)
    {
        // 1. Thử dùng CapPlanarHoles trước
        Brep? cappedBrep = openHullBrep.CapPlanarHoles(tolerance);
        if (cappedBrep != null && cappedBrep.IsSolid)
        {
            return (cappedBrep, 0);
        }

        // 2. Nếu CapPlanarHoles không thể đóng tự động, tiến hành tạo từng mặt biên và Join
        var brepsToJoin = new List<Brep> { openHullBrep };

        // Lấy tất cả các cạnh hở của mặt vỏ
        var nakedEdges = openHullBrep.Edges.Where(e => e.TrimCount == 1).ToList();

        // Tách các đường biên: mép đuôi (X ~ 0), mép mũi (X ~ L), mép boong mạn (Z ~ D)
        Curve? aftEdgeCurve = null;
        Curve? bowEdgeCurve = null;
        var deckSideCurves = new List<Curve>();

        double L = input.Length;
        double D = input.Depth ?? (input.Draft * 1.4);

        foreach (var edge in nakedEdges)
        {
            var midPt = edge.PointAt(edge.Domain.Mid);

            if (Math.Abs(midPt.X) < 1.0)
            {
                aftEdgeCurve = edge.DuplicateCurve(); // Đường sườn đuôi
            }
            else if (Math.Abs(midPt.X - L) < 1.0)
            {
                bowEdgeCurve = edge.DuplicateCurve(); // Đường sườn mũi
            }
            else if (Math.Abs(midPt.Z - D) < 1.0)
            {
                deckSideCurves.Add(edge.DuplicateCurve()); // 2 mép boong mạn
            }
        }

        // A. ĐÓNG VÁCH ĐUÔI (Transom at X = 0)
        if (aftEdgeCurve != null)
        {
            var topAftLine = new Line(aftEdgeCurve.PointAtStart, aftEdgeCurve.PointAtEnd);
            var aftBoundary = Curve.JoinCurves(new Curve[] { aftEdgeCurve, new LineCurve(topAftLine) }, tolerance);
            if (aftBoundary != null && aftBoundary.Length > 0 && aftBoundary[0].IsClosed)
            {
                var transomBreps = Brep.CreatePlanarBreps(aftBoundary[0], tolerance);
                if (transomBreps != null && transomBreps.Length > 0)
                {
                    brepsToJoin.Add(transomBreps[0]);
                }
            }
        }

        // B. ĐÓNG VÁCH MŨI (Bow at X = L)
        if (bowEdgeCurve != null)
        {
            var topBowLine = new Line(bowEdgeCurve.PointAtStart, bowEdgeCurve.PointAtEnd);
            var bowBoundary = Curve.JoinCurves(new Curve[] { bowEdgeCurve, new LineCurve(topBowLine) }, tolerance);
            if (bowBoundary != null && bowBoundary.Length > 0 && bowBoundary[0].IsClosed)
            {
                var bowBreps = Brep.CreatePlanarBreps(bowBoundary[0], tolerance);
                if (bowBreps != null && bowBreps.Length > 0)
                {
                    brepsToJoin.Add(bowBreps[0]);
                }
            }
        }

        // C. ĐÓNG MẶT BOONG (Deck at Z = D)
        // Tạo mặt boong phẳng phủ kín chu vi trên
        var deckBoundaryCurves = new List<Curve>(deckSideCurves);
        if (aftEdgeCurve != null)
            deckBoundaryCurves.Add(new LineCurve(aftEdgeCurve.PointAtStart, aftEdgeCurve.PointAtEnd));
        if (bowEdgeCurve != null)
            deckBoundaryCurves.Add(new LineCurve(bowEdgeCurve.PointAtStart, bowEdgeCurve.PointAtEnd));

        var joinedDeckLoop = Curve.JoinCurves(deckBoundaryCurves, tolerance);
        if (joinedDeckLoop != null && joinedDeckLoop.Length > 0 && joinedDeckLoop[0].IsClosed)
        {
            var deckBreps = Brep.CreatePlanarBreps(joinedDeckLoop[0], tolerance);
            if (deckBreps != null && deckBreps.Length > 0)
            {
                brepsToJoin.Add(deckBreps[0]);
            }
        }

        // 3. KHÂU TẤT CẢ CÁC MẶT LẠI (Brep.JoinBreps)
        Brep[] joinedResults = Brep.JoinBreps(brepsToJoin, tolerance);

        if (joinedResults != null && joinedResults.Length > 0)
        {
            var finalBrep = joinedResults[0];
            int nakedCount = finalBrep.Edges.Count(e => e.TrimCount == 1);
            return (finalBrep, nakedCount);
        }

        return (openHullBrep, openHullBrep.Edges.Count(e => e.TrimCount == 1));
    }
}