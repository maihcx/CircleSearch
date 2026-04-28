using System.Windows;
using System.Windows.Media;

namespace CircleSearch.Overlay.Helpers;

public static class CircleHelper
{
    /// <summary>
    /// Get bounding rectangle that contains the drawn path with some padding.
    /// The path is a freehand stroke — we compute convex hull bounding box.
    /// </summary>
    public static Rect GetBoundingRect(IList<Point> points, double padding = 20)
    {
        if (points.Count == 0) return Rect.Empty;

        double minX = points.Min(p => p.X);
        double minY = points.Min(p => p.Y);
        double maxX = points.Max(p => p.X);
        double maxY = points.Max(p => p.Y);

        return new Rect(
            Math.Max(0, minX - padding),
            Math.Max(0, minY - padding),
            maxX - minX + padding * 2,
            maxY - minY + padding * 2);
    }

    /// <summary>
    /// Check if the drawn gesture looks like a circle/loop 
    /// (start and end points are close, and path is reasonably round).
    /// </summary>
    public static bool IsCircularGesture(IList<Point> points)
    {
        if (points.Count < 10) return false;

        var first = points[0];
        var last = points[^1];

        // Ends must be close together (closed shape)
        double closeDistance = Distance(first, last);
        var bounds = GetBoundingRect(points, 0);
        double diagonal = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height);

        return closeDistance < diagonal * 0.35 && diagonal > 40;
    }

    /// <summary>
    /// Creates a smooth path geometry from freehand points.
    /// </summary>
    public static PathGeometry CreateSmoothPath(IList<Point> points)
    {
        if (points.Count < 2) return new PathGeometry();

        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = points[0], IsClosed = false };

        // Use bezier smoothing — group into triplets
        for (int i = 1; i < points.Count - 2; i += 3)
        {
            var p1 = points[i];
            var p2 = i + 1 < points.Count ? points[i + 1] : p1;
            var p3 = i + 2 < points.Count ? points[i + 2] : p2;
            figure.Segments.Add(new BezierSegment(p1, p2, p3, true));
        }

        // Last segment
        if (points.Count >= 2)
            figure.Segments.Add(new LineSegment(points[^1], true));

        geometry.Figures.Add(figure);
        return geometry;
    }

    private static double Distance(Point a, Point b)
        => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
}
