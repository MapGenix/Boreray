using System;
using System.Drawing.Drawing2D;
using DotSpatial.Symbology;
using DotSpatial.Topology;
using Point = System.Drawing.Point;

namespace DotSpatial.Controls.Extensions
{
	public static class PointLayerHelper
	{
		public static IPointSymbolizer CreatePointSymbolizer(IPointCategory pc, IDrawnState ds)
		{
			IPointSymbolizer ps = pc.Symbolizer;
			if (ds.IsSelected)
			{
				ps = pc.SelectionSymbolizer;
			}
			return ps;
		}

		public static IPointSymbolizer CreatePointSymbolizer(FastDrawnState state)
		{
			IPointCategory pc = state.Category as IPointCategory;
			IPointSymbolizer ps = null;
			if (pc != null && pc.Symbolizer != null)
				ps = pc.Symbolizer;
			return ps;
		}

		public static bool Validate(IPointCategory pc, IPointCategory category)
		{
			if (pc == null)
				return false;
			return pc == category;
		}

		public static bool Validate(IDrawnState ds)
		{
			if (ds == null)
				return false;
			if (!ds.IsVisible)
				return false;
			return ds.SchemeCategory != null;
		}

		public static Matrix CreateTranslateMatrix(Matrix origTransform, Point pt)
		{
			Matrix shift = origTransform.Clone();
			shift.Translate(pt.X, pt.Y);
			return shift;
		}

		public static Point CreatePoint(MapArgs e, Coordinate c)
		{
			return new Point
			{
				X = Convert.ToInt32((c.X - e.MinX) * e.Dx),
				Y = Convert.ToInt32((e.MaxY - c.Y) * e.Dy)
			};
		}

	}
}