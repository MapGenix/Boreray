using DotSpatial.Data;
using DotSpatial.Symbology;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Point = System.Drawing.Point;

namespace DotSpatial.Controls.Extensions
{
	public static class LineLayerHelper
	{
		private const int Selected = 1;

		/// <summary>
		/// Builds a linestring into the graphics path, using minX, maxY, dx and dy for the transformations.
		/// </summary>
		public static void BuildLineString(GraphicsPath path, IBasicLineString ls, double minX, double maxY, double dx, double dy)
		{
			IList<Coordinate> cs = ls.Coordinates;
			List<Point> points = new List<Point>();
			Point previousPoint = new Point();
			for (int iPoint = 0; iPoint < ls.NumPoints; iPoint++)
			{
				Coordinate c = cs[iPoint];
				Point pt = new Point { X = Convert.ToInt32((c.X - minX) * dx), Y = Convert.ToInt32((maxY - c.Y) * dy) };
				if (previousPoint.IsEmpty == false)
				{
					if (pt.X != previousPoint.X || pt.Y != previousPoint.Y)
					{
						points.Add(pt);
					}
				}
				else
				{
					points.Add(pt);
				}

				previousPoint = pt;
			}

			if (points.Count < 2) return;
			Point[] pointArray = points.ToArray();
			path.StartFigure();
			path.AddLines(pointArray);
		}

		public static void BuildLineString(GraphicsPath path, double[] vertices, ShapeRange shpx, MapArgs args, Rectangle clipRect)
		{
			double minX = args.MinX;
			double maxY = args.MaxY;
			double dx = args.Dx;
			double dy = args.Dy;
			for (int prt = 0; prt < shpx.Parts.Count; prt++)
			{
				PartRange prtx = shpx.Parts[prt];
				int start = prtx.StartIndex;
				int end = prtx.EndIndex;
				var points = new List<double[]>(end - start + 1);

				for (int i = start; i <= end; i++)
				{
					var pt = new[]
                    {
                        (vertices[i*2] - minX)*dx,
                        (maxY - vertices[i*2 + 1])*dy
                    };
					points.Add(pt);
				}

				List<List<double[]>> multiLinestrings;
				if (!shpx.Extent.Within(args.GeographicExtents))
				{
					multiLinestrings = CohenSutherland.ClipLinestring(points, clipRect.Left, clipRect.Top,
																	  clipRect.Right, clipRect.Bottom);
				}
				else
				{
					multiLinestrings = new List<List<double[]>> { points };
				}

				foreach (List<double[]> linestring in multiLinestrings)
				{
					var intPoints = DuplicationPreventer.Clean(linestring).ToArray();
					if (intPoints.Length < 2)
					{
						continue;
					}

					path.StartFigure();
					path.AddLines(intPoints);
				}
			}
		}

		public static Rectangle ComputeClippingRectangle(MapArgs args, ILineSymbolizer ls)
		{
			// Compute a clipping rectangle that accounts for symbology
			int maxLineWidth = 2 * (int)Math.Ceiling(ls.GetWidth());
			Rectangle clipRect = new Rectangle(args.ImageRectangle.Location.X, args.ImageRectangle.Location.Y, args.ImageRectangle.Width, args.ImageRectangle.Height);
			clipRect.Inflate(maxLineWidth, maxLineWidth);
			return clipRect;
		}

		public static void FastBuildLine(GraphicsPath graphPath, double[] vertices, ShapeRange shpx, double minX, double maxY, double dx, double dy)
		{
			for (int prt = 0; prt < shpx.Parts.Count; prt++)
			{
				PartRange prtx = shpx.Parts[prt];
				int start = prtx.StartIndex;
				int end = prtx.EndIndex;
				List<Point> partPoints = new List<Point>();
				Point previousPoint = new Point();
				for (int i = start; i <= end; i++)
				{
					if (double.IsNaN(vertices[i * 2]) || double.IsNaN(vertices[i * 2 + 1])) continue;
					var pt = new Point
					{
						X = Convert.ToInt32((vertices[i * 2] - minX) * dx),
						Y = Convert.ToInt32((maxY - vertices[i * 2 + 1]) * dy)
					};

					if (i == 0 || (pt.X != previousPoint.X || pt.Y != previousPoint.Y))
					{
						// Don't add the same point twice
						partPoints.Add(pt);
						previousPoint = pt;
					}
				}
				if (partPoints.Count < 2) continue; // we need two distinct points to make a line
				graphPath.StartFigure();
				graphPath.AddLines(partPoints.ToArray());
			}
		}

		public static double DefineScale(IProj e, IFeatureSymbolizer ls)
		{
			double scale = 1;
			if (ls.ScaleMode == ScaleMode.Geographic)
			{
				scale = e.ImageRectangle.Width / e.GeographicExtents.Width;
			}
			return scale;
		}

		public static ILineSymbolizer CreateLineSymbolizer(ILineCategory category, int selectState)
		{
			ILineSymbolizer ls = category.Symbolizer;
			if (selectState == Selected)
				ls = category.SelectionSymbolizer;
			return ls;
		}
	}
}