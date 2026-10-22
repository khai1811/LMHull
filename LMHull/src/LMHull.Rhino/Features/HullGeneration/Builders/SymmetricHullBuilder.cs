using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Builders;

public static class SymmetricHullBuilder
{
    public static Brep? BuildSymmetricSolidHull(ShipInput input, int stationCount = 7, double tolerance = 0.05)
    {
        // 1. Tạo các đường sườn nửa mạn phải (Starboard)
        double L = input.Length;
        double B = input.Beam;
        double D = input.Depth ?? (input.Draft * 1.6);
        double halfB = B / 2.0;

        var starboardCurves = new List<Curve>();
        double dx = L / (stationCount - 1);

        for (int i = 0; i < stationCount; i++)
        {
            double x = i * dx;
            double xi = (double)i / (stationCount - 1); // 0 -> 1

            // Hệ số bóp mũi và đuôi
            double factorY = Math.Sin(xi * Math.PI);
            double currentB = halfB * factorY;

            var pKeel = new Point3d(x, 0, 0);
            var pBilge = new Point3d(x, currentB * 0.85, D * 0.15);
            var pDeck = new Point3d(x, currentB, D);

            var crv = Curve.CreateInterpolatedCurve(new[] { pKeel, pBilge, pDeck }, 3);
            if (crv != null) starboardCurves.Add(crv);
        }

        if (starboardCurves.Count < 2) return null;

        // 2. Loft tạo mặt mạn phải
        var srfStarboard = Brep.CreateFromLoft(
            starboardCurves,
            Point3d.Unset,
            Point3d.Unset,
            LoftType.Normal,
            false
        );

        if (srfStarboard == null || srfStarboard.Length == 0) return null;
        Brep rightBrep = srfStarboard[0];

        // 3. Lấy đối xứng qua mặt phẳng Centerline (Y = 0) tạo mạn trái
        var mirrorXform = Transform.Mirror(Plane.WorldZX);
        Brep leftBrep = rightBrep.DuplicateBrep();
        leftBrep.Transform(mirrorXform);
        leftBrep.Flip(); // Đảo hướng pháp tuyến cho đúng chiều ngoài

        // 4. Ghép 2 nửa mạn thuyền
        var joinedHulls = Brep.JoinBreps(new[] { rightBrep, leftBrep }, tolerance * 2);
        Brep bottomHull = (joinedHulls != null && joinedHulls.Length > 0) ? joinedHulls[0] : rightBrep;

        // 5. Vá phẳng kín các lỗ hở (mặt boong Deck, vách mũi Bow, vách đuôi Stern)
        Brep solidHull = bottomHull.CapPlanarHoles(tolerance * 2) ?? bottomHull;

        // Nếu vẫn còn cạnh hở do dung sai loft mỏng, hợp nhất các cạnh biên trùng nhau
        if (!solidHull.IsSolid)
        {
            solidHull.Faces.ShrinkFaces();
            solidHull.CullUnusedFaces();
            solidHull.CullUnusedEdges();
            solidHull.CullUnusedLoops();
            solidHull.CullUnusedVertices();

            var joinedAgain = Brep.JoinBreps(new[] { solidHull }, 0.1);
            if (joinedAgain != null && joinedAgain.Length > 0 && joinedAgain[0].IsSolid)
            {
                solidHull = joinedAgain[0];
            }
        }

        return solidHull;
    }
}