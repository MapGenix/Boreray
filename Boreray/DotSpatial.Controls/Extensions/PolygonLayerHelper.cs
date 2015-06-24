using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using DotSpatial.Data;


namespace DotSpatial.Controls.Extensions
{
	public static class PolygonLayerHelper
	{
		public static Rectangle ComputeClippingRectangle(MapArgs args)
		{
			const int maxSymbologyFuzz = 50;
			var clipRect = new Rectangle(args.ImageRectangle.Location.X, args.ImageRectangle.Location.Y, args.ImageRectangle.Width, args.ImageRectangle.Height);
			clipRect.Inflate(maxSymbologyFuzz, maxSymbologyFuzz);
			return clipRect;
		}

		public static List<double[]> CreatePointListPoints(double[] vertices, PartRange prtx, MapArgs args)
		{
			double minX = args.MinX;
			double maxY = args.MaxY;
			double dx = args.Dx;
			double dy = args.Dy;

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
			return points;
		}

		/// <summary>
		/// Appends the specified polygon to the graphics path.
		/// </summary>
		public static void BuildPolygon(double[] vertices, ShapeRange shpx, GraphicsPath borderPath, MapArgs args, SoutherlandHodgman shClip)
		{
			for (int prt = 0; prt < shpx.Parts.Count; prt++)
			{
				PartRange prtx = shpx.Parts[prt];

				var points = CreatePointListPoints(vertices, prtx, args);
				if (null != shClip)
				{
					points = shClip.Clip(points);
				}
				var intPoints = DuplicationPreventer.Clean(points).ToArray();
				if (intPoints.Length < 2)
				{
					continue;
				}

				borderPath.StartFigure();
				borderPath.AddLines(intPoints);
			}
		}


	}
}
